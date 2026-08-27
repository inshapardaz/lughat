using System.Net.WebSockets;
using System.Text.Json;

namespace Lughat.Engine.Api.Realtime;

/// <summary>
/// Broadcasts push events (indexing progress, etc.) to every connected WebSocket client —
/// spec §2 / §9's <c>/ws</c> endpoint.
/// </summary>
public sealed class EventHub
{
    private readonly List<WebSocket> _sockets = [];
    private readonly Lock _lock = new();

    public async Task HandleConnectionAsync(WebSocket socket, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            _sockets.Add(socket);
        }

        try
        {
            var buffer = new byte[1024];
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer, cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Connection closed as part of shutdown — nothing to report.
        }
        catch (WebSocketException)
        {
            // Client dropped the connection.
        }
        finally
        {
            lock (_lock)
            {
                _sockets.Remove(socket);
            }
        }
    }

    public async Task BroadcastAsync(object payload)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(payload);

        List<WebSocket> snapshot;
        lock (_lock)
        {
            snapshot = _sockets.Where(s => s.State == WebSocketState.Open).ToList();
        }

        foreach (var socket in snapshot)
        {
            try
            {
                await socket.SendAsync(json, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
            }
            catch (WebSocketException)
            {
                // Client disconnected between the snapshot and the send — safe to ignore.
            }
        }
    }
}
