// ============================================================================
// ARKANA v0.1 — configurações persistentes (dono: COORDENADOR)
// GDD seção 12. Persistência: localStorage (equivalente web do settings.json;
// no empacotamento Tauri/Electron da Fase 2 migra para arquivo real).
// ============================================================================
import { setLanguage, type Lang } from './strings';

export type KeyAction =
  | 'up' | 'down' | 'left' | 'right'
  | 'dodge' | 'element' | 'supreme' | 'scoreboard';

export interface GameSettings {
  video: {
    fullscreen: boolean;
    /** 'auto' usa o tamanho da janela */
    resolution: '1280x720' | '1600x900' | '1920x1080';
    quality: 'low' | 'medium' | 'high';
    /** 0 = ilimitado */
    fpsLimit: 0 | 30 | 60 | 120;
    vsync: boolean;
    showFps: boolean;
  };
  audio: {
    /** 0..100 */
    master: number;
    music: number;
    sfx: number;
    ui: number;
  };
  controls: {
    /** 0.1 .. 10.0 */
    mouseSens: number;
    /** multiplicador ao mirar */
    aimSens: number;
    invertY: boolean;
    /** KeyboardEvent.code por ação (ex.: 'KeyW', 'Space') */
    keys: Record<KeyAction, string>;
  };
  game: {
    language: Lang;
    colorblind: 'off' | 'protanopia' | 'deuteranopia' | 'tritanopia';
    damageNumbers: boolean;
    comboTips: boolean;
  };
}

export const DEFAULT_SETTINGS: GameSettings = {
  video: {
    fullscreen: false,
    resolution: '1280x720',
    quality: 'high',
    fpsLimit: 0,
    vsync: true,
    showFps: true,
  },
  audio: { master: 80, music: 60, sfx: 80, ui: 70 },
  controls: {
    mouseSens: 1.0,
    aimSens: 1.0,
    invertY: false,
    keys: {
      up: 'KeyW',
      down: 'KeyS',
      left: 'KeyA',
      right: 'KeyD',
      dodge: 'Space',
      element: 'KeyQ',
      supreme: 'KeyR',
      scoreboard: 'Tab',
    },
  },
  game: { language: 'pt', colorblind: 'off', damageNumbers: true, comboTips: true },
};

const STORAGE_KEY = 'arkana.settings.v1';

type Listener = (s: GameSettings) => void;

function deepMerge<T>(base: T, patch: Partial<T> | undefined): T {
  if (patch === undefined || patch === null) return structuredClone(base);
  const out: any = structuredClone(base);
  for (const k of Object.keys(patch as object) as (keyof T)[]) {
    const pv = (patch as any)[k];
    if (pv !== null && typeof pv === 'object' && !Array.isArray(pv)) {
      out[k] = deepMerge((base as any)[k], pv);
    } else if (pv !== undefined) {
      out[k] = pv;
    }
  }
  return out;
}

class SettingsManager {
  private data: GameSettings;
  private listeners: Listener[] = [];

  constructor() {
    this.data = this.load();
    setLanguage(this.data.game.language);
  }

  private load(): GameSettings {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return structuredClone(DEFAULT_SETTINGS);
      return deepMerge(DEFAULT_SETTINGS, JSON.parse(raw));
    } catch {
      return structuredClone(DEFAULT_SETTINGS);
    }
  }

  save(): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.data));
    } catch { /* modo privado etc. — segue em memória */ }
  }

  get(): GameSettings { return this.data; }

  /** aplica um patch parcial, persiste e notifica ouvintes */
  update(patch: Partial<GameSettings>): void {
    this.data = deepMerge(this.data, patch);
    setLanguage(this.data.game.language);
    this.save();
    for (const fn of this.listeners) fn(this.data);
  }

  /** restaura os padrões de UMA aba */
  resetTab(tab: keyof GameSettings): void {
    (this.data as any)[tab] = structuredClone(DEFAULT_SETTINGS[tab]);
    if (tab === 'game') setLanguage(this.data.game.language);
    this.save();
    for (const fn of this.listeners) fn(this.data);
  }

  onChange(fn: Listener): () => void {
    this.listeners.push(fn);
    return () => { this.listeners = this.listeners.filter((f) => f !== fn); };
  }

  key(action: KeyAction): string { return this.data.controls.keys[action]; }
}

/** singleton global — importar de qualquer raia */
export const Settings = new SettingsManager();
