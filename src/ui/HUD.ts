// ============================================================================
// ARKANA v0.1 — HUD completo (raia D — UX de HUD)
// Vida/escudo/mana, carrossel de elementos + cooldowns, minimapa, FPS,
// Selo de Sintonia decorativo, números de dano, banner de evolução do escudo
// e overlay de morte. Tudo com scrollFactor 0 e DEPTH.HUD (números de dano
// vivem no MUNDO, com scrollFactor 1).
// Estética: painéis azul-noite (COLORS.night, alpha ~0.55) + dourado.
// ============================================================================
import Phaser from 'phaser';
import { type ArenaApi, Element, ELEMENTS, EVT, type PlayerStats } from '../core/types';
import {
  TEX, ICON_TEX, DEPTH, SHIELD_COLORS, ELEMENT_COLORS, COLORS, FONTS,
  GAME_WIDTH, GAME_HEIGHT, TILE, ARENA_COLS, ARENA_ROWS,
} from '../core/config';
import { Settings, touchControlsEnabled } from '../core/settings';
import { t } from '../core/strings';

// ---------------------------------------------------------------- layout fixo
const W = GAME_WIDTH;
const H = GAME_HEIGHT;

const L = {
  // painel inferior esquerdo (vida/escudo/mana)
  blX: 16, blY: H - 140, blW: 320, blH: 106,
  barX: 28, barW: 240,
  hpY: H - 130, hpH: 16,
  hpTextX: 274,
  shieldLabelY: H - 108,
  shieldY: H - 92, shieldH: 12,
  evoY: H - 77, evoH: 4,
  manaLabelY: H - 70,
  manaY: H - 54, manaH: 12,

  // painel inferior direito (elementos + cooldowns)
  brX: W - 244, brY: H - 158, brW: 228, brH: 142,
  activeX: W - 68, activeY: H - 62,
  rowStartX: W - 114, rowStepX: 32, rowY: H - 58,
  tacticX: W - 68, tacticY: H - 124, tacticR: 17,
  dodgeX: W - 114, dodgeY: H - 124, dodgeR: 13,

  // minimapa (topo direito)
  mapX: W - 166, mapY: 16, mapSize: 150,
  /** amostra o terreno a cada 2 células → 30×30 amostras de 5px */
  mapStep: 2, mapCellPx: 5,
  mapRedrawMs: 500,

  // topo esquerdo / centro
  fpsX: 16, fpsY: 12, fpsRefreshMs: 250,
  sealX: W / 2, sealY: 44,

  // banner central
  bannerY: 180,
} as const;

/** números de dano (spec da raia D) */
const DMG = {
  poolSize: 20,
  /** dano ≥ 15 vira dourado com fonte maior */
  bigAt: 15,
  riseBy: 30,
  durationMs: 700,
} as const;

const HP_GREEN = 0x35c94a;
const SEAL_SPIN_RAD_PER_MS = 0.0002;

function hex(c: number): string {
  return '#' + c.toString(16).padStart(6, '0');
}

function lerpColor(a: number, b: number, tt: number): number {
  const k = Phaser.Math.Clamp(tt, 0, 1);
  const ar = (a >> 16) & 0xff, ag = (a >> 8) & 0xff, ab = a & 0xff;
  const br = (b >> 16) & 0xff, bg = (b >> 8) & 0xff, bb = b & 0xff;
  return (Math.round(ar + (br - ar) * k) << 16)
    | (Math.round(ag + (bg - ag) * k) << 8)
    | Math.round(ab + (bb - ab) * k);
}

export class HUD {
  private api: ArenaApi;
  private scene: Phaser.Scene;
  /** todos os objetos fixos do HUD (p/ destroy em lote) */
  private all: Phaser.GameObjects.GameObject[] = [];

  /**
   * Modo toque (Fase 2 — GDD 19.3): o TouchControls assume o canto inferior
   * direito (carrossel + cooldowns nos botões) e o joystick vive embaixo à
   * esquerda — o painel de vida/mana sobe p/ o topo-esquerdo (zona segura).
   */
  private readonly touchMode = touchControlsEnabled();
  /** deslocamento vertical do painel esquerdo no modo toque */
  private readonly blDy = this.touchMode ? -(L.blY - 34) : 0;

  // barras (redesenhadas por frame num único Graphics)
  private barsG!: Phaser.GameObjects.Graphics;
  private hpText!: Phaser.GameObjects.Text;
  private shieldLabel!: Phaser.GameObjects.Text;
  private manaLabel!: Phaser.GameObjects.Text;

  // elementos
  private icons!: Record<Element, Phaser.GameObjects.Image>;
  private glowG!: Phaser.GameObjects.Graphics;
  private lastElement: Element | null = null;

  // minimapa
  private mapTerrainG!: Phaser.GameObjects.Graphics;
  private mapDotsG!: Phaser.GameObjects.Graphics;
  private mapTimer: number = L.mapRedrawMs; // força a 1ª amostragem

  // fps
  private fpsText!: Phaser.GameObjects.Text;
  private fpsTimer: number = L.fpsRefreshMs;

  // selo decorativo (placeholder do medidor de Sintonia — v0.2)
  private seal!: Phaser.GameObjects.Image;

  // números de dano (pool no MUNDO — scrollFactor 1)
  private dmgPool: Phaser.GameObjects.Text[] = [];
  private dmgIdx = 0;

  // banner de evolução + flash
  private banner!: Phaser.GameObjects.Text;
  private flashRect!: Phaser.GameObjects.Rectangle;

  // overlay de morte
  private deathRect!: Phaser.GameObjects.Rectangle;
  private deathTitle!: Phaser.GameObjects.Text;
  private deathSub!: Phaser.GameObjects.Text;

  private destroyed = false;

  constructor(api: ArenaApi) {
    this.api = api;
    this.scene = api.scene;

    this.buildPanels();
    this.buildBars();
    this.buildElements();
    this.buildMinimap();
    this.buildTop();
    this.buildDamagePool();
    this.buildBannerAndOverlay();

    const ev = this.scene.events;
    ev.on(EVT.DAMAGE_NUMBER, this.onDamageNumber);
    ev.on(EVT.SHIELD_EVOLVED, this.onShieldEvolved);
    ev.on(EVT.PLAYER_DIED, this.onPlayerDied);
  }

  // ------------------------------------------------------------------ builders
  private fix<T extends Phaser.GameObjects.GameObject & Phaser.GameObjects.Components.ScrollFactor & Phaser.GameObjects.Components.Depth>(obj: T, depth: number = DEPTH.HUD): T {
    obj.setScrollFactor(0);
    obj.setDepth(depth);
    this.all.push(obj);
    return obj;
  }

  private makeText(x: number, y: number, size: number, color: string, family: string = FONTS.ui): Phaser.GameObjects.Text {
    return this.fix(this.scene.add.text(x, y, '', {
      fontFamily: family,
      fontSize: `${size}px`,
      color,
    }));
  }

  private buildPanels(): void {
    // azul-noite alpha ~0.55, borda dourada fina
    const panels: (readonly [number, number, number, number])[] = [
      [L.blX, L.blY + this.blDy, L.blW, L.blH],
    ];
    // modo toque: o canto inferior direito pertence aos botões de toque
    if (!this.touchMode) panels.push([L.brX, L.brY, L.brW, L.brH]);
    for (const [x, y, w, h] of panels) {
      const panel = this.fix(this.scene.add.rectangle(x, y, w, h, COLORS.night, 0.55).setOrigin(0));
      panel.setStrokeStyle(1, COLORS.goldDim, 0.8);
    }
  }

  private buildBars(): void {
    this.barsG = this.fix(this.scene.add.graphics());
    this.hpText = this.makeText(L.hpTextX, L.hpY + this.blDy + L.hpH / 2, 12, hex(COLORS.text));
    this.hpText.setOrigin(0, 0.5);
    this.shieldLabel = this.makeText(L.barX, L.shieldLabelY + this.blDy, 11, hex(COLORS.textDim));
    this.manaLabel = this.makeText(L.barX, L.manaLabelY + this.blDy, 11, hex(COLORS.textDim));
  }

  private buildElements(): void {
    if (this.touchMode) return; // o carrossel de toque substitui estes ícones
    this.glowG = this.fix(this.scene.add.graphics());
    this.icons = {} as Record<Element, Phaser.GameObjects.Image>;
    for (const el of ELEMENTS) {
      this.icons[el] = this.fix(this.scene.add.image(0, 0, ICON_TEX[el]));
    }
  }

  private buildMinimap(): void {
    this.mapTerrainG = this.fix(this.scene.add.graphics());
    this.mapDotsG = this.fix(this.scene.add.graphics());
    // moldura fina dourada por cima dos dots
    const frame = this.fix(this.scene.add.graphics());
    frame.lineStyle(1.5, COLORS.gold, 0.9);
    frame.strokeRect(L.mapX - 1, L.mapY - 1, L.mapSize + 2, L.mapSize + 2);
  }

  private buildTop(): void {
    this.fpsText = this.makeText(L.fpsX, L.fpsY, 13, hex(COLORS.gold));
    this.fpsText.setVisible(Settings.get().video.showFps);
    // Selo de Sintonia decorativo (v0.2 vira o medidor real — GDD seção 10)
    this.seal = this.fix(
      this.scene.add.image(L.sealX, L.sealY, TEX.SEAL).setScale(0.6).setAlpha(0.5),
    );
  }

  private buildDamagePool(): void {
    for (let i = 0; i < DMG.poolSize; i++) {
      // MUNDO: scrollFactor 1 (padrão) — só o depth é de HUD
      const txt = this.scene.add.text(0, 0, '', {
        fontFamily: FONTS.ui,
        fontSize: '14px',
        color: '#ffffff',
      }).setOrigin(0.5).setDepth(DEPTH.HUD).setVisible(false);
      txt.setStroke(hex(COLORS.nightDeep), 3);
      this.dmgPool.push(txt);
    }
  }

  private buildBannerAndOverlay(): void {
    // flash colorido da evolução do escudo
    this.flashRect = this.fix(
      this.scene.add.rectangle(0, 0, W, H, COLORS.gold, 1).setOrigin(0).setAlpha(0).setVisible(false),
    );
    this.banner = this.makeText(W / 2, L.bannerY, 30, hex(COLORS.gold));
    this.banner.setOrigin(0.5).setStroke(hex(COLORS.nightDeep), 5).setVisible(false);

    // overlay de morte (por cima de todo o resto do HUD)
    this.deathRect = this.fix(
      this.scene.add.rectangle(0, 0, W, H, COLORS.nightDeep, 1).setOrigin(0).setAlpha(0).setVisible(false),
      DEPTH.HUD + 1,
    );
    this.deathTitle = this.fix(
      this.scene.add.text(W / 2, H / 2 - 40, t('hud.dead'), {
        fontFamily: FONTS.title,
        fontSize: '64px',
        color: hex(COLORS.danger),
      }).setOrigin(0.5).setAlpha(0).setVisible(false),
      DEPTH.HUD + 1,
    );
    this.deathSub = this.fix(
      this.scene.add.text(W / 2, H / 2 + 30, t('hud.dead.sub'), {
        fontFamily: FONTS.ui,
        fontSize: '18px',
        color: hex(COLORS.text),
      }).setOrigin(0.5).setAlpha(0).setVisible(false),
      DEPTH.HUD + 1,
    );
  }

  // ------------------------------------------------------------------ update
  update(_time: number, delta: number): void {
    if (this.destroyed) return;
    const stats = this.api.getPlayerStats();

    this.drawBarsAndCooldowns(stats);
    if (!this.touchMode && stats.element !== this.lastElement) {
      this.refreshElementIcons(stats.element);
    }

    // minimapa: terreno a cada ~500ms, dots por frame
    this.mapTimer += delta;
    if (this.mapTimer >= L.mapRedrawMs) {
      this.mapTimer = 0;
      this.redrawMinimapTerrain();
    }
    this.drawMinimapDots();

    // FPS + rótulos de texto: 4×/s
    this.fpsTimer += delta;
    if (this.fpsTimer >= L.fpsRefreshMs) {
      this.fpsTimer = 0;
      const show = Settings.get().video.showFps;
      this.fpsText.setVisible(show);
      if (show) {
        this.fpsText.setText(String(Math.round(this.scene.game.loop.actualFps)));
      }
      this.refreshLabels(stats);
    }

    // selo gira lentamente
    this.seal.rotation += delta * SEAL_SPIN_RAD_PER_MS;
  }

  private refreshLabels(stats: PlayerStats): void {
    const hp = `${Math.ceil(Math.max(0, stats.hp))}/${stats.maxHp}`;
    if (this.hpText.text !== hp) this.hpText.setText(hp);
    const sh = `${t('hud.shield')} Nv ${stats.shieldLevel}`;
    if (this.shieldLabel.text !== sh) this.shieldLabel.setText(sh);
    const mn = t('hud.mana');
    if (this.manaLabel.text !== mn) this.manaLabel.setText(mn);
  }

  private drawBarsAndCooldowns(stats: PlayerStats): void {
    const g = this.barsG;
    const dy = this.blDy;
    g.clear();

    // VIDA: verde → vermelha conforme cai
    const hpFrac = stats.maxHp > 0 ? stats.hp / stats.maxHp : 0;
    this.drawBar(g, L.barX, L.hpY + dy, L.barW, L.hpH, hpFrac, lerpColor(COLORS.danger, HP_GREEN, hpFrac));

    // ESCUDO: cor do nível atual
    const shFrac = stats.shieldMax > 0 ? stats.shield / stats.shieldMax : 0;
    const shColor = SHIELD_COLORS[stats.shieldLevel] ?? SHIELD_COLORS[1];
    this.drawBar(g, L.barX, L.shieldY + dy, L.barW, L.shieldH, shFrac, shColor);

    // mini-barra de progresso de evolução (só quando há próximo nível)
    if (stats.evoNext > 0) {
      const evoFrac = stats.evoProgress / stats.evoNext;
      g.fillStyle(COLORS.nightDeep, 0.8);
      g.fillRect(L.barX, L.evoY + dy, L.barW, L.evoH);
      g.fillStyle(COLORS.gold, 0.9);
      g.fillRect(L.barX, L.evoY + dy, Math.max(0, L.barW * Phaser.Math.Clamp(evoFrac, 0, 1)), L.evoH);
    }

    // MANA: azul (tom do elemento Água — paleta do config)
    const mnFrac = stats.manaMax > 0 ? stats.mana / stats.manaMax : 0;
    this.drawBar(g, L.barX, L.manaY + dy, L.barW, L.manaH, mnFrac, ELEMENT_COLORS[Element.WATER]);

    // cooldowns circulares: TÁTICA (cor do elemento) e ESQUIVA (dourado).
    // Modo toque: os próprios botões de toque desenham os cooldowns.
    if (!this.touchMode) {
      this.drawCooldown(g, L.tacticX, L.tacticY, L.tacticR,
        stats.tacticCdRemaining, stats.tacticCdTotal, ELEMENT_COLORS[stats.element]);
      this.drawCooldown(g, L.dodgeX, L.dodgeY, L.dodgeR,
        stats.dodgeCdRemaining, stats.dodgeCdTotal, COLORS.gold);
    }
  }

  private drawBar(g: Phaser.GameObjects.Graphics, x: number, y: number, w: number, h: number, frac: number, color: number): void {
    g.fillStyle(COLORS.nightDeep, 0.85);
    g.fillRect(x, y, w, h);
    g.fillStyle(color, 1);
    g.fillRect(x + 1, y + 1, Math.max(0, (w - 2) * Phaser.Math.Clamp(frac, 0, 1)), h - 2);
    g.lineStyle(1, COLORS.goldDim, 0.6);
    g.strokeRect(x, y, w, h);
  }

  /** disco colorido + "pizza" escura com a fração restante do cooldown */
  private drawCooldown(g: Phaser.GameObjects.Graphics, x: number, y: number, r: number, remaining: number, total: number, color: number): void {
    g.fillStyle(COLORS.night, 0.7);
    g.fillCircle(x, y, r + 3);
    g.lineStyle(2, COLORS.goldDim, 0.9);
    g.strokeCircle(x, y, r + 3);
    g.fillStyle(color, 0.9);
    g.fillCircle(x, y, r);
    if (total > 0 && remaining > 0) {
      const frac = Phaser.Math.Clamp(remaining / total, 0, 1);
      g.fillStyle(COLORS.nightDeep, 0.78);
      g.slice(x, y, r, -Math.PI / 2, -Math.PI / 2 + frac * Math.PI * 2, false);
      g.fillPath();
    }
  }

  /** carrossel: ativo grande com brilho + os outros 4 menores acinzentados */
  private refreshElementIcons(current: Element): void {
    this.lastElement = current;
    const idx = ELEMENTS.indexOf(current);

    const active = this.icons[current];
    active.setPosition(L.activeX, L.activeY).setScale(1.4).clearTint().setAlpha(1);

    for (let i = 1; i < ELEMENTS.length; i++) {
      const el = ELEMENTS[(idx + i) % ELEMENTS.length];
      this.icons[el]
        .setPosition(L.rowStartX - (i - 1) * L.rowStepX, L.rowY)
        .setScale(0.62)
        .setTint(0x777788)
        .setAlpha(0.75);
    }

    const g = this.glowG;
    g.clear();
    g.lineStyle(3, ELEMENT_COLORS[current], 0.9);
    g.strokeCircle(L.activeX, L.activeY, 32);
    g.lineStyle(1, COLORS.gold, 0.8);
    g.strokeCircle(L.activeX, L.activeY, 35);
  }

  // ------------------------------------------------------------------ minimapa
  private redrawMinimapTerrain(): void {
    const g = this.mapTerrainG;
    const terr = this.api.terrain;
    g.clear();
    g.fillStyle(COLORS.nightDeep, 1);
    g.fillRect(L.mapX, L.mapY, L.mapSize, L.mapSize);
    for (let cy = 0; cy < ARENA_ROWS; cy += L.mapStep) {
      for (let cx = 0; cx < ARENA_COLS; cx += L.mapStep) {
        g.fillStyle(terr.getCellColor(cx, cy), 1);
        g.fillRect(
          L.mapX + (cx / L.mapStep) * L.mapCellPx,
          L.mapY + (cy / L.mapStep) * L.mapCellPx,
          L.mapCellPx,
          L.mapCellPx,
        );
      }
    }
  }

  private drawMinimapDots(): void {
    const g = this.mapDotsG;
    g.clear();
    const worldW = ARENA_COLS * TILE;
    const worldH = ARENA_ROWS * TILE;
    for (const e of this.api.getEntities()) {
      if (!e.alive) continue;
      const mx = L.mapX + (e.x / worldW) * L.mapSize;
      const my = L.mapY + (e.y / worldH) * L.mapSize;
      if (e.isPlayer) {
        g.fillStyle(COLORS.gold, 1);
        g.fillRect(mx - 2, my - 2, 4, 4);
      } else if (e.id === 'dummy') {
        g.fillStyle(0xffffff, 1);
        g.fillRect(mx - 1.5, my - 1.5, 3, 3);
      } else {
        g.fillStyle(COLORS.danger, 1);
        g.fillRect(mx - 1.5, my - 1.5, 3, 3);
      }
    }
  }

  // ------------------------------------------------------------------ eventos
  private onDamageNumber = (p: { x: number; y: number; amount: number }): void => {
    if (this.destroyed || p.amount <= 0) return;
    if (!Settings.get().game.damageNumbers) return;

    const txt = this.dmgPool[this.dmgIdx];
    this.dmgIdx = (this.dmgIdx + 1) % this.dmgPool.length;

    const big = p.amount >= DMG.bigAt;
    txt.setFontSize(big ? 18 : 14);
    txt.setColor(big ? hex(COLORS.gold) : '#ffffff');
    txt.setText(String(Math.max(1, Math.round(p.amount))));
    const x = p.x + Phaser.Math.Between(-6, 6);
    txt.setPosition(x, p.y);
    txt.setAlpha(1).setVisible(true);

    this.scene.tweens.killTweensOf(txt);
    this.scene.tweens.add({
      targets: txt,
      y: p.y - DMG.riseBy,
      alpha: 0,
      duration: DMG.durationMs,
      ease: 'Cubic.easeOut',
      onComplete: () => txt.setVisible(false),
    });
  };

  private onShieldEvolved = (level: number): void => {
    if (this.destroyed) return;
    const color = SHIELD_COLORS[level] ?? COLORS.gold;

    // flash na cor do novo nível
    this.scene.tweens.killTweensOf(this.flashRect);
    this.flashRect.setFillStyle(color, 1).setAlpha(0.25).setVisible(true);
    this.scene.tweens.add({
      targets: this.flashRect,
      alpha: 0,
      duration: 450,
      onComplete: () => this.flashRect.setVisible(false),
    });

    // banner central: entra com pop, some em ~1,5s
    this.scene.tweens.killTweensOf(this.banner);
    this.banner.setText(`${t('hud.evolved')}  Nv ${level}`);
    this.banner.setColor(hex(color));
    this.banner.setVisible(true).setAlpha(0).setScale(1.6);
    this.scene.tweens.add({
      targets: this.banner,
      alpha: 1,
      scale: 1,
      duration: 180,
      ease: 'Back.easeOut',
    });
    this.scene.tweens.add({
      targets: this.banner,
      alpha: 0,
      delay: 1100,
      duration: 400,
      onComplete: () => this.banner.setVisible(false),
    });
  };

  private onPlayerDied = (): void => {
    if (this.destroyed) return;
    this.deathRect.setVisible(true);
    this.deathTitle.setVisible(true);
    this.deathSub.setVisible(true);
    this.scene.tweens.add({ targets: this.deathRect, alpha: 0.7, duration: 400 });
    this.scene.tweens.add({
      targets: [this.deathTitle, this.deathSub],
      alpha: 1,
      duration: 500,
      delay: 200,
    });
    // o restart via ENTER é responsabilidade da ArenaScene
  };

  // ------------------------------------------------------------------ destroy
  destroy(): void {
    if (this.destroyed) return;
    this.destroyed = true;

    const ev = this.api.scene.events;
    ev.off(EVT.DAMAGE_NUMBER, this.onDamageNumber);
    ev.off(EVT.SHIELD_EVOLVED, this.onShieldEvolved);
    ev.off(EVT.PLAYER_DIED, this.onPlayerDied);

    for (const txt of this.dmgPool) {
      this.scene.tweens.killTweensOf(txt);
      txt.destroy();
    }
    this.dmgPool = [];

    for (const obj of this.all) {
      this.scene.tweens.killTweensOf(obj);
      obj.destroy();
    }
    this.all = [];
  }
}
