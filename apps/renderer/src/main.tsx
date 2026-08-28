import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { DirectionProvider, MantineProvider } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import '@mantine/core/styles.css';
import '@mantine/notifications/styles.css';
import './styles/fonts.css';
import './i18n';
import App from './App';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <DirectionProvider>
      <MantineProvider defaultColorScheme="auto">
        <Notifications />
        <App />
      </MantineProvider>
    </DirectionProvider>
  </StrictMode>,
);
