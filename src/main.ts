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
  ],
};

const game = new Phaser.Game(config);
// gancho de QA/debug: permite inspecionar o estado real do jogo no console
(window as unknown as { __ARKANA_GAME: Phaser.Game }).__ARKANA_GAME = game;
