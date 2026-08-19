// ============================================================================
// ARKANA v0.1 — áudio procedural via WebAudio (dono: COORDENADOR)
// Zero assets externos: SFX sintetizados + música gerativa em loop.
// Direção (GDD seção 13): híbrido "épico moderno" — aqui em versão placeholder.
// Uso: Audio.init() no Loading; Audio.playSfx('cast_fire'); Audio.playMusic('menu').
// ============================================================================
import { Settings } from './settings';

export type SfxName =
  | 'ui_click' | 'ui_hover' | 'ui_slider' | 'ui_back'
  | 'cast_fire' | 'cast_water' | 'cast_earth' | 'cast_wind' | 'cast_lightning'
  | 'hit' | 'hit_shield' | 'dodge' | 'element_switch'
  | 'shield_up' | 'freeze' | 'burn' | 'wall' | 'electrocute'
  | 'player_hurt' | 'bot_die' | 'pause' | 'title_confirm';

export type MusicTrack = 'menu' | 'arena' | null;

interface SfxSpec {
  type: OscillatorType | 'noise';
  freq: number;
  /** Hz alvo do glide (0 = sem glide) */
  slide: number;
  dur: number;
  vol: number;
  /** categoria de volume: 'sfx' | 'ui' */
  cat: 'sfx' | 'ui';
}

const SFX: Record<SfxName, SfxSpec> = {
  ui_click:        { type: 'square',   freq: 660,  slide: 880,  dur: 0.06, vol: 0.25, cat: 'ui' },
  ui_hover:        { type: 'sine',     freq: 440,  slide: 520,  dur: 0.04, vol: 0.12, cat: 'ui' },
  ui_slider:       { type: 'sine',     freq: 520,  slide: 0,    dur: 0.03, vol: 0.15, cat: 'ui' },
  ui_back:         { type: 'square',   freq: 440,  slide: 300,  dur: 0.08, vol: 0.2,  cat: 'ui' },
  title_confirm:   { type: 'sawtooth', freq: 220,  slide: 880,  dur: 0.35, vol: 0.3,  cat: 'ui' },
  cast_fire:       { type: 'sawtooth', freq: 320,  slide: 160,  dur: 0.18, vol: 0.3,  cat: 'sfx' },
  cast_water:      { type: 'sine',     freq: 500,  slide: 300,  dur: 0.16, vol: 0.3,  cat: 'sfx' },
  cast_earth:      { type: 'square',   freq: 140,  slide: 90,   dur: 0.2,  vol: 0.32, cat: 'sfx' },
  cast_wind:       { type: 'noise',    freq: 2200, slide: 0,    dur: 0.18, vol: 0.22, cat: 'sfx' },
  cast_lightning:  { type: 'sawtooth', freq: 1400, slide: 500,  dur: 0.1,  vol: 0.28, cat: 'sfx' },
  hit:             { type: 'square',   freq: 220,  slide: 120,  dur: 0.09, vol: 0.3,  cat: 'sfx' },
  hit_shield:      { type: 'sine',     freq: 700,  slide: 500,  dur: 0.08, vol: 0.28, cat: 'sfx' },
  dodge:           { type: 'sine',     freq: 300,  slide: 900,  dur: 0.12, vol: 0.25, cat: 'sfx' },
  element_switch:  { type: 'square',   freq: 520,  slide: 700,  dur: 0.07, vol: 0.22, cat: 'sfx' },
  shield_up:       { type: 'sine',     freq: 440,  slide: 1100, dur: 0.5,  vol: 0.35, cat: 'sfx' },
  freeze:          { type: 'sine',     freq: 1200, slide: 1800, dur: 0.25, vol: 0.25, cat: 'sfx' },
  burn:            { type: 'noise',    freq: 900,  slide: 0,    dur: 0.25, vol: 0.2,  cat: 'sfx' },
  wall:            { type: 'square',   freq: 100,  slide: 60,   dur: 0.25, vol: 0.35, cat: 'sfx' },
  electrocute:     { type: 'sawtooth', freq: 1800, slide: 900,  dur: 0.2,  vol: 0.26, cat: 'sfx' },
  player_hurt:     { type: 'square',   freq: 180,  slide: 90,   dur: 0.15, vol: 0.32, cat: 'sfx' },
  bot_die:         { type: 'sawtooth', freq: 400,  slide: 60,   dur: 0.4,  vol: 0.3,  cat: 'sfx' },
  pause:           { type: 'sine',     freq: 600,  slide: 400,  dur: 0.1,  vol: 0.2,  cat: 'ui' },
};

// Tema do menu: arpejo lento em Lá menor (épico contido) — 8 passos.
const MENU_NOTES = [220.0, 261.63, 329.63, 440.0, 329.63, 261.63, 246.94, 196.0];
// Arena: pulso grave + pentatônica — mais tensa.
const ARENA_BASS = [110.0, 110.0, 130.81, 98.0];
const ARENA_LEAD = [440.0, 523.25, 587.33, 659.25, 523.25, 440.0, 392.0, 523.25];

class AudioManager {
  private ctx: AudioContext | null = null;
  private masterGain!: GainNode;
  private musicGain!: GainNode;
  private sfxGain!: GainNode;
  private uiGain!: GainNode;
  private noiseBuf: AudioBuffer | null = null;
  private musicTimer: number | null = null;
  private currentTrack: MusicTrack = null;
  private step = 0;

  /** chamar uma vez (LoadingScene). Seguro chamar de novo. */
  init(): void {
    if (this.ctx) return;
    const Ctx = window.AudioContext ?? (window as any).webkitAudioContext;
    if (!Ctx) return;
    this.ctx = new Ctx();
    this.masterGain = this.ctx.createGain();
    this.masterGain.connect(this.ctx.destination);
    this.musicGain = this.ctx.createGain();
    this.musicGain.connect(this.masterGain);
    this.sfxGain = this.ctx.createGain();
    this.sfxGain.connect(this.masterGain);
    this.uiGain = this.ctx.createGain();
    this.uiGain.connect(this.masterGain);
    // ruído branco 1s reutilizável
    this.noiseBuf = this.ctx.createBuffer(1, this.ctx.sampleRate, this.ctx.sampleRate);
    const d = this.noiseBuf.getChannelData(0);
    for (let i = 0; i < d.length; i++) d[i] = Math.random() * 2 - 1;
    this.applyVolumes();
    Settings.onChange(() => this.applyVolumes());
    // Navegadores exigem gesto do usuário p/ liberar o áudio — e o resume()
    // pode não completar (política do navegador, WebView entregando o contexto
    // 'interrupted', app voltando do segundo plano no Android). SEM `once`:
    // com uma única tentativa, uma falha no primeiro toque deixava a partida
    // inteira muda, sem sinal nenhum. Cada gesto é uma nova tentativa e o custo
    // é ler ctx.state — que também recupera o áudio suspenso no meio da sessão.
    const unlock = (): void => {
      if (this.ctx && this.ctx.state !== 'running') void this.ctx.resume().catch(() => undefined);
    };
    for (const ev of ['pointerdown', 'keydown', 'touchend'] as const) {
      window.addEventListener(ev, unlock);
    }
  }

  applyVolumes(): void {
    if (!this.ctx) return;
    const a = Settings.get().audio;
    const m = a.master / 100;
    this.masterGain.gain.value = m;
    this.musicGain.gain.value = (a.music / 100) * 0.5;
    this.sfxGain.gain.value = a.sfx / 100;
    this.uiGain.gain.value = a.ui / 100;
  }

  playSfx(name: SfxName): void {
    if (!this.ctx || this.ctx.state !== 'running') return;
    const s = SFX[name];
    const t0 = this.ctx.currentTime;
    const out = s.cat === 'ui' ? this.uiGain : this.sfxGain;
    const g = this.ctx.createGain();
    g.gain.setValueAtTime(s.vol, t0);
    g.gain.exponentialRampToValueAtTime(0.001, t0 + s.dur);
    g.connect(out);
    if (s.type === 'noise') {
      const src = this.ctx.createBufferSource();
      src.buffer = this.noiseBuf!;
      const f = this.ctx.createBiquadFilter();
      f.type = 'bandpass';
      f.frequency.value = s.freq;
      f.Q.value = 0.8;
      src.connect(f);
      f.connect(g);
      src.start(t0);
      src.stop(t0 + s.dur);
    } else {
      const o = this.ctx.createOscillator();
      o.type = s.type;
      o.frequency.setValueAtTime(s.freq, t0);
      if (s.slide > 0) o.frequency.exponentialRampToValueAtTime(s.slide, t0 + s.dur);
      o.connect(g);
      o.start(t0);
      o.stop(t0 + s.dur);
    }
  }

  playMusic(track: MusicTrack): void {
    if (this.currentTrack === track) return;
    this.stopMusic();
    this.currentTrack = track;
    if (!track || !this.ctx) return;
    this.step = 0;
    const stepMs = track === 'menu' ? 480 : 240;
    this.musicTimer = window.setInterval(() => this.tick(track), stepMs);
  }

  stopMusic(): void {
    if (this.musicTimer !== null) { clearInterval(this.musicTimer); this.musicTimer = null; }
    this.currentTrack = null;
  }

  private note(freq: number, dur: number, vol: number, type: OscillatorType): void {
    if (!this.ctx || this.ctx.state !== 'running') return;
    const t0 = this.ctx.currentTime;
    const o = this.ctx.createOscillator();
    const g = this.ctx.createGain();
    o.type = type;
    o.frequency.value = freq;
    g.gain.setValueAtTime(0.0001, t0);
    g.gain.exponentialRampToValueAtTime(vol, t0 + 0.02);
    g.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    o.connect(g);
    g.connect(this.musicGain);
    o.start(t0);
    o.stop(t0 + dur);
  }

  private tick(track: Exclude<MusicTrack, null>): void {
    if (track === 'menu') {
      this.note(MENU_NOTES[this.step % MENU_NOTES.length], 0.9, 0.16, 'triangle');
      if (this.step % 4 === 0) this.note(MENU_NOTES[this.step % MENU_NOTES.length] / 2, 1.8, 0.1, 'sine');
    } else {
      if (this.step % 2 === 0) this.note(ARENA_BASS[(this.step / 2) % ARENA_BASS.length], 0.22, 0.18, 'square');
      if (this.step % 4 === 2) this.note(ARENA_LEAD[this.step % ARENA_LEAD.length], 0.18, 0.08, 'triangle');
    }
    this.step++;
  }
}

/** singleton global */
export const Audio = new AudioManager();
