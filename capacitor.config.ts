import type { CapacitorConfig } from '@capacitor/cli';

const config: CapacitorConfig = {
  appId: 'br.com.vstack.arkana',
  appName: 'Arkana',
  webDir: 'dist',
  android: {
    // Azul-noite do GDD §10 por trás da WebView. Sem isto o Capacitor pinta
    // branco entre a splash e o primeiro frame do jogo — um flash claro no meio
    // de uma identidade que é escura de ponta a ponta.
    backgroundColor: '#0B1026',
  },
};

export default config;
