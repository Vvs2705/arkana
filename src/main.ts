// ============================================================================
// ARKANA v0.1 — bootstrap (dono: COORDENADOR)
// Ordem de cenas: Splash → Loading → Title → Menu → (Settings/Credits) → Arena ⇄ Pause
// ============================================================================
import Phaser from 'phaser';
import { GAME_WIDTH, GAME_HEIGHT, COLORS } from './core/config';
import { Settings } from './core/settings';

import { SplashScene } from './scenes/SplashScene';
import { LoadingScene } from './scenes/LoadingScene';
import { TitleScene } from './scenes/TitleScene';
import { MenuScene } from './scenes/MenuScene';
import { SettingsScene } from './scenes/SettingsScene';
import { CreditsScene } from './scenes/CreditsScene';
import { ArenaScene } from './scenes/ArenaScene';
import { PauseScene } from './scenes/PauseScene';
import { TouchLayoutScene } from './scenes/TouchLayoutScene';

const s = Settings.get();
const fpsLimit = s.video.fpsLimit;

const config: Phaser.Types.Core.GameConfig = {
  type: Phaser.AUTO,
  parent: 'app',
  width: GAME_WIDTH,
  height: GAME_HEIGHT,
  backgroundColor: COLORS.nightDeep,
  scale: {
    mode: Phaser.Scale.FIT,
    autoCenter: Phaser.Scale.CENTER_BOTH,
  },
  // multi-toque: joystick + mira + botão simultâneos (Fase 2 — GDD 19.3)
  input: { activePointers: 4 },
  fps: fpsLimit > 0 ? { limit: fpsLimit } : undefined,
  physics: {
    default: 'arcade',
    arcade: { debug: false },
  },
  scene: [
    SplashScene,
    LoadingScene,
    TitleScene,
    MenuScene,
    SettingsScene,
    CreditsScene,
    ArenaScene,
    PauseScene,
    TouchLayoutScene,
  ],
};

// ---------------------------------------------------------------------------
// Fontes locais (public/fonts, @font-face no index.html — zero rede).
// O canvas do Phaser NÃO dispara o download de @font-face sozinho: não existe
// texto no DOM que peça a fonte, e cada Text é rasterizado uma única vez. Se a
// fonte chegasse depois do primeiro desenho, o título ficaria no fallback para
// sempre. Por isso o boot espera as fontes; falha nunca impede o jogo de abrir.
// ---------------------------------------------------------------------------
const FONT_PRELOAD = [
  '500 16px "Cinzel"',
  '700 16px "Cinzel"',
  '400 16px "Chakra Petch"',
  '700 16px "Chakra Petch"',
  '400 16px "Inter"',
];

function boot(): void {
  const game = new Phaser.Game(config);
  // gancho de QA/debug: permite inspecionar o estado real do jogo no console
  (window as unknown as { __ARKANA_GAME: Phaser.Game }).__ARKANA_GAME = game;
}

Promise.all(FONT_PRELOAD.map((f) => document.fonts.load(f)))
  .catch(() => undefined)
  .finally(boot);
