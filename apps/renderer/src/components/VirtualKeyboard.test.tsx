import { describe, expect, it, vi } from 'vitest';
import { screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { renderWithProviders } from '../test/render';
import { VirtualKeyboard } from './VirtualKeyboard';

describe('VirtualKeyboard', () => {
  it('inserts a character from a non-default script tab on click', async () => {
    const onInsert = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(<VirtualKeyboard onInsert={onInsert} />);

    await user.click(screen.getByRole('button', { name: 'Show virtual keyboard' }));
    // Mantine's Popover positions itself via floating-ui, which relies on real layout
    // measurements jsdom never provides — the dropdown's content renders correctly but
    // stays marked `display: none` by an animation-mount state that never resolves in this
    // environment, so `hidden: true` is needed to see past that CSS-visibility check.
    await user.click(await screen.findByRole('tab', { name: 'اردو', hidden: true }));
    await user.click(await screen.findByRole('button', { name: 'ا', hidden: true }));

    expect(onInsert).toHaveBeenCalledWith('ا');
  });
});
