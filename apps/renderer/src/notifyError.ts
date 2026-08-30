import { notifications } from '@mantine/notifications';
import i18n from './i18n';
import { EngineApiError } from './api';

/**
 * Surfaces a failed engine call as a toast instead of letting it fail silently (the store's
 * mutating actions previously just let a rejected promise vanish into an unhandled rejection —
 * from the UI's perspective, indistinguishable from the action doing nothing at all).
 * EngineApiError.code maps to `errors.<code>` (spec §9's localisation boundary — the engine
 * only ever sends a stable code, this is where it becomes a human message); anything else
 * (a network failure, the engine process not running) falls back to `errors.generic`.
 */
export function notifyError(error: unknown, titleKey: string): void {
  const message =
    error instanceof EngineApiError && i18n.exists(`errors.${error.code}`)
      ? i18n.t(`errors.${error.code}`)
      : i18n.t('errors.generic');

  notifications.show({ color: 'red', title: i18n.t(titleKey), message });
}
