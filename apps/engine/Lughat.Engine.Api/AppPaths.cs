namespace Lughat.Engine.Api;

/// <summary>
/// Per-OS app data location (spec §7). Overridable via LUGHAT_DATA_DIR so tests and a
/// future portable mode don't have to write into the real user profile.
/// </summary>
public static class AppPaths
{
    public static string GetAppDataRoot()
    {
        var overridePath = Environment.GetEnvironmentVariable("LUGHAT_DATA_DIR");
        if (!string.IsNullOrEmpty(overridePath))
        {
            return overridePath;
        }

        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Lughat");
        }

        if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "Lughat");
        }

        // Linux and other Unix-likes: XDG Base Directory spec.
        var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
        var configHome = string.IsNullOrEmpty(xdgConfigHome)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config")
            : xdgConfigHome;
        return Path.Combine(configHome, "lughat");
    }
}
