// ============================================================================
// ARKANA Fase 2 — controles de toque (GDD 19.3)
// Joystick virtual à esquerda (movimento) · arrasto no lado direito (mira) ·
// botões de Ataque (segurar = fogo contínuo), Tática e Esquiva · carrossel dos
// 5 elementos (cor + forma — regra de daltonismo) · botão de pausa.
//
// Alvos ≥ 48dp: 1dp ≈ 1px CSS e o devicePixelRatio já está embutido no tamanho
// CSS do canvas — a conversão dp→px do canvas usa scale.displaySize.
// Layout/escala persistidos em Settings.controls.touch; o modo edição
// (api === null) torna os controles arrastáveis e salva as posições.
//
// A camada NÃO duplica lógica de gameplay: o Player consome getMove()/
// getAimAngle()/attackHeld/tacticHeld/consumeDodge() pelo mesmo caminho de
// input do teclado/mouse.
// ============================================================================
import Phaser from 'phaser';
import { Element, ELEMENTS, type ArenaApi, type EntityRef } from '../core/types';
import {
  GAME_WIDTH, GAME_HEIGHT, TOUCH, TEX, ICON_TEX, ELEMENT_COLORS, COLORS,
  DEPTH, type TouchControlId,
} from '../core/config';
import { BAL } from '../core/balance';
import { Settings } from '../core/settings';
import { Audio } from '../core/audio';

/** fração da largura que pertence ao joystick (o resto do chão livre é mira) */
const MOVE_ZONE_FRAC = 0.45;
/** profundidade: acima dos painéis do HUD, abaixo do overlay de morte */
const TOUCH_DEPTH = DEPTH.HUD;
/** escala do botão enquanto pressionado (feedback tátil visual) */
const PRESS_SCALE = 0.9;

export interface TouchControlsOptions {
  /** modo edição: controles arrastáveis, sem gameplay (api deve ser null) */
  edit?: boolean;
  onPause?: () => void;
  onElement?: (el: Element) => void;
}

export class TouchControls {
  private readonly scene: Phaser.Scene;
  private readonly api: ArenaApi | null;
  private readonly edit: boolean;
  private readonly opts: TouchControlsOptions;

  // sprites
  private stickBase!: Phaser.GameObjects.Image;
  private stickThumb!: Phaser.GameObjects.Image;
  private attackBtn!: Phaser.GameObjects.Image;
  private tacticBtn!: Phaser.GameObjects.Image;
  private dodgeBtn!: Phaser.GameObjects.Image;
  private pauseBtn: Phaser.GameObjects.Image | null = null;
  private carousel!: Phaser.GameObjects.Container;
  private carouselIcons: Phaser.GameObjects.Image[] = [];
  /** overlay p/ anel do elemento ativo + "pizzas" de cooldown + destaque */
  private fxG!: Phaser.GameObjects.Graphics;

  // estado de input
  private movePtr: number | null = null;
  private moveVec = { x: 0, y: 0 };
  private aimPtr: number | null = null;
  private aimStart = { x: 0, y: 0 };
  /** última direção de arrasto (radianos) — persiste após soltar o dedo */
  private rawAim: number | null = null;
  private attackPtr: number | null = null;
  private tacticPtr: number | null = null;
  private dodgeQueued = false;

  private lastElement: Element | null = null;
  private destroyed = false;
  /** escalas base pós-setDisplaySize (feedback de pressão sem acumular) */
  private attackBaseScale = 1;
  private tacticBaseScale = 1;

  constructor(scene: Phaser.Scene, api: ArenaApi | null, opts: TouchControlsOptions = {}) {
    this.scene = scene;
    this.api = api;
    this.edit = opts.edit === true;
    this.opts = opts;

    this.build();
    if (this.edit) this.enableEditing();
    else this.enableGameplay();
  }

  // ------------------------------------------------------------- geometria
  private get userScale(): number {
    const s = Settings.get().controls.touch.scale;
    return Phaser.Math.Clamp(s, TOUCH.scaleMin, TOUCH.scaleMax);
  }

  /** raio mínimo em px do canvas equivalente a 48dp na tela do jogador */
  private minHitRadius(): number {
    const dispW = this.scene.scale.displaySize.width || GAME_WIDTH;
    return (TOUCH.minDp * (GAME_WIDTH / dispW)) / 2;
  }

  private pos(id: TouchControlId): { x: number; y: number } {
    const layout = Settings.get().controls.touch.layout;
    return layout[id] ?? TOUCH.defaults[id];
  }

  /**
   * Hit area do ícone do carrossel: RETÂNGULO, não círculo.
   * O piso de 48dp (minHitRadius) fica MAIOR que o espaçamento entre os ícones
   * no telefone. Com círculos, o toque cai dentro de dois ícones ao mesmo tempo
   * e o `topOnly` do Phaser entrega o de índice MAIOR — o jogador toca Fogo e
   * recebe Água. Medido: tela de 640px css ⇒ raio de 48px de canvas contra 54px
   * de espaçamento, ou seja, tudo além de 6px do centro (o ícone visível tem 22px
   * de raio) já pertencia ao vizinho. No desktop o raio cai para 24px e o defeito
   * some — por isso só apareceu no aparelho real (17/08, docs/ANDROID.md).
   * Solução: 48dp na vertical, onde há espaço livre, e largura limitada ao
   * espaçamento, para nenhum ícone roubar o toque do vizinho.
   */
  private hitCarouselIcon(img: Phaser.GameObjects.Image, gapPx: number): void {
    const min = this.minHitRadius() * 2;
    const w = Math.min(Math.max(img.displayWidth, min), gapPx);
    const h = Math.max(img.displayHeight, min);
    const sx = img.displayWidth / img.width;
    const sy = img.displayHeight / img.height;
    const lw = w / sx;
    const lh = h / sy;
    img.setInteractive(
      new Phaser.Geom.Rectangle((img.width - lw) / 2, (img.height - lh) / 2, lw, lh),
      Phaser.Geom.Rectangle.Contains,
    );
  }

  /** hit circle da imagem em coordenadas locais da textura (origem 0,0) */
  private hitImage(img: Phaser.GameObjects.Image, displayRadius: number): void {
    const r = Math.max(displayRadius, this.minHitRadius());
    const localR = r / (img.displayWidth / img.width);
    img.setInteractive(
      new Phaser.Geom.Circle(img.width / 2, img.height / 2, localR),
      Phaser.Geom.Circle.Contains,
    );
  }

  // ----------------------------------------------------------------- build
  private build(): void {
    const s = this.userScale;
    const sz = TOUCH.size;
    const fix = <T extends Phaser.GameObjects.Image | Phaser.GameObjects.Container | Phaser.GameObjects.Graphics>(o: T): T => {
      o.setScrollFactor(0).setDepth(TOUCH_DEPTH);
      return o;
    };

    // joystick
    const sp = this.pos('stick');
    this.stickBase = fix(this.scene.add.image(sp.x, sp.y, TEX.STICK_BASE))
      .setDisplaySize(sz.stickBase * 2 * s, sz.stickBase * 2 * s)
      .setAlpha(0.85);
    this.stickThumb = fix(this.scene.add.image(sp.x, sp.y, TEX.STICK_THUMB))
      .setDisplaySize(sz.stickThumb * 2 * s, sz.stickThumb * 2 * s)
      .setAlpha(0.9);

    // botões de ação
    const mk = (id: TouchControlId, tex: string, r: number): Phaser.GameObjects.Image => {
      const p = this.pos(id);
      return fix(this.scene.add.image(p.x, p.y, tex))
        .setDisplaySize(r * 2 * s, r * 2 * s)
        .setAlpha(0.92);
    };
    this.attackBtn = mk('attack', TEX.BTN_ATTACK, sz.attack);
    this.tacticBtn = mk('tactic', TEX.BTN_TACTIC, sz.tactic);
    this.dodgeBtn = mk('dodge', TEX.BTN_DODGE, sz.dodge);

    // carrossel dos 5 elementos (acima dos botões)
    const cp = this.pos('carousel');
    this.carousel = fix(this.scene.add.container(cp.x, cp.y));
    this.carouselIcons = ELEMENTS.map((el, i) => {
      // setScrollFactor(0) no FILHO, não só no container: o Phaser DESENHA o
      // filho com o scrollFactor do pai (0 = grudado na tela), mas o hit-test
      // de input usa o scrollFactor do PRÓPRIO filho
      // (InputManager.hitTest: `px = worldX + csx * gameObject.scrollFactorX - csx`).
      // Com o padrão 1, a área de toque do ícone ficava deslocada pelo scroll da
      // câmera — que na Arena segue o jogador — e o toque na troca de elemento
      // caía na zona livre, virando arrasto de mira. Defeito de 17/08
      // (docs/ANDROID.md, Fase 3). NÃO remover.
      const img = this.scene.add.image((i - 2) * sz.iconGap * s, 0, ICON_TEX[el])
        .setDisplaySize(sz.icon * s, sz.icon * s)
        .setScrollFactor(0);
      this.carousel.add(img);
      return img;
    });

    // botão de pausa (só em jogo — sem ESC no celular; posição fixa)
    if (!this.edit) {
      this.pauseBtn = fix(this.scene.add.image(GAME_WIDTH - 200, 44, TEX.BTN_PAUSE))
        .setDisplaySize(sz.pause * 2, sz.pause * 2)
        .setAlpha(0.8);
    }

    this.fxG = fix(this.scene.add.graphics());
  }

  // -------------------------------------------------------------- gameplay
  private enableGameplay(): void {
    const s = this.userScale;
    const sz = TOUCH.size;

    // botões: pointerdown no objeto, release pelo pointerup global (o dedo
    // pode escorregar p/ fora do botão sem "prender" o disparo)
    this.attackBaseScale = this.attackBtn.scaleX;
    this.hitImage(this.attackBtn, sz.attack * s);
    this.attackBtn.on('pointerdown', (p: Phaser.Input.Pointer) => {
      this.attackPtr = p.id;
      this.attackBtn.setScale(this.attackBaseScale * PRESS_SCALE);
    });

    this.tacticBaseScale = this.tacticBtn.scaleX;
    this.hitImage(this.tacticBtn, sz.tactic * s);
    this.tacticBtn.on('pointerdown', (p: Phaser.Input.Pointer) => {
      this.tacticPtr = p.id;
      this.tacticBtn.setScale(this.tacticBaseScale * PRESS_SCALE);
    });

    this.hitImage(this.dodgeBtn, sz.dodge * s);
    this.dodgeBtn.on('pointerdown', () => { this.dodgeQueued = true; });

    this.carouselIcons.forEach((img, i) => {
      this.hitCarouselIcon(img, sz.iconGap * s);
      img.on('pointerdown', () => {
        this.opts.onElement?.(ELEMENTS[i]);
      });
    });

    if (this.pauseBtn) {
      this.hitImage(this.pauseBtn, TOUCH.size.pause);
      this.pauseBtn.on('pointerdown', () => {
        Audio.playSfx('ui_click');
        this.opts.onPause?.();
      });
    }

    // zonas livres: esquerda = joystick flutuante · direita = arrasto de mira
    this.scene.input.on(Phaser.Input.Events.POINTER_DOWN, this.onDown);
    this.scene.input.on(Phaser.Input.Events.POINTER_MOVE, this.onMove);
    this.scene.input.on(Phaser.Input.Events.POINTER_UP, this.onUp);
  }

  private readonly onDown = (p: Phaser.Input.Pointer, over: Phaser.GameObjects.GameObject[]): void => {
    if (over.length > 0) return; // o toque pertence a um botão
    if (p.x < GAME_WIDTH * MOVE_ZONE_FRAC && this.movePtr === null) {
      // joystick flutuante: a base recentra no ponto do toque
      this.movePtr = p.id;
      this.stickBase.setPosition(p.x, p.y);
      this.stickThumb.setPosition(p.x, p.y);
      this.applyMove(p);
    } else if (this.aimPtr === null) {
      this.aimPtr = p.id;
      this.aimStart.x = p.x;
      this.aimStart.y = p.y;
    }
  };

  private readonly onMove = (p: Phaser.Input.Pointer): void => {
    if (p.id === this.movePtr) this.applyMove(p);
    else if (p.id === this.aimPtr) {
      const dx = p.x - this.aimStart.x;
      const dy = p.y - this.aimStart.y;
      if (Math.hypot(dx, dy) >= BAL.touch.aimDeadzonePx) {
        this.rawAim = Math.atan2(dy, dx);
      }
    }
  };

  private readonly onUp = (p: Phaser.Input.Pointer): void => {
    if (p.id === this.movePtr) {
      this.movePtr = null;
      this.moveVec.x = 0;
      this.moveVec.y = 0;
      const sp = this.pos('stick');
      this.stickBase.setPosition(sp.x, sp.y);
      this.stickThumb.setPosition(sp.x, sp.y);
    }
    if (p.id === this.aimPtr) this.aimPtr = null;
    if (p.id === this.attackPtr) {
      this.attackPtr = null;
      this.attackBtn.setScale(this.attackBaseScale);
    }
    if (p.id === this.tacticPtr) {
      this.tacticPtr = null;
      this.tacticBtn.setScale(this.tacticBaseScale);
    }
  };

  private applyMove(p: Phaser.Input.Pointer): void {
    const r = (TOUCH.size.stickBase * this.userScale) || 1;
    let vx = (p.x - this.stickBase.x) / r;
    let vy = (p.y - this.stickBase.y) / r;
    const len = Math.hypot(vx, vy);
    if (len > 1) { vx /= len; vy /= len; }
    if (len < BAL.touch.stickDeadzone) { vx = 0; vy = 0; }
    this.moveVec.x = vx;
    this.moveVec.y = vy;
    // posição do polegar: limitada ao raio da base
    const tl = Math.min(1, len);
    const ang = Math.atan2(p.y - this.stickBase.y, p.x - this.stickBase.x);
    this.stickThumb.setPosition(
      this.stickBase.x + Math.cos(ang) * tl * r,
      this.stickBase.y + Math.sin(ang) * tl * r,
    );
  }

  // -------------------------------------------------------- API p/ o Player
  /** vetor de movimento analógico (módulo ≤ 1); {0,0} = parado */
  getMove(): { x: number; y: number } {
    return this.moveVec;
  }

  /**
   * Ângulo de mira efetivo (radianos) ou null se o jogador nunca mirou.
   * Esquema "Simples": gruda no inimigo mais próximo dentro do cone na
   * direção do arrasto/última mira (GDD 19.3). "Avançado": 100% manual.
   */
  getAimAngle(px: number, py: number): number | null {
    const angle = this.rawAim;
    if (angle === null) return null;
    if (Settings.get().controls.touch.scheme !== 'simple' || !this.api) return angle;

    const cone = Phaser.Math.DegToRad(BAL.touch.aimAssist.coneDeg);
    let best: EntityRef | null = null;
    // ": number" obrigatório — BAL é `as const` e inferiria o literal 520
    let bestDist: number = BAL.touch.aimAssist.rangePx;
    for (const e of this.api.getEntities()) {
      if (e.isPlayer || !e.alive) continue;
      const d = Math.hypot(e.x - px, e.y - py);
      if (d > bestDist) continue;
      const da = Math.abs(Phaser.Math.Angle.Wrap(Math.atan2(e.y - py, e.x - px) - angle));
      if (da <= cone) {
        best = e;
        bestDist = d;
      }
    }
    return best ? Math.atan2(best.y - py, best.x - px) : angle;
  }

  get attackHeld(): boolean { return this.attackPtr !== null; }
  get tacticHeld(): boolean { return this.tacticPtr !== null; }

  /** edge-trigger: true uma única vez por toque no botão de esquiva */
  consumeDodge(): boolean {
    const q = this.dodgeQueued;
    this.dodgeQueued = false;
    return q;
  }

  /** solta tudo (chamar ao retomar da pausa — pointerups podem ter se perdido) */
  clearState(): void {
    this.movePtr = null;
    this.aimPtr = null;
    this.attackPtr = null;
    this.tacticPtr = null;
    this.dodgeQueued = false;
    this.moveVec.x = 0;
    this.moveVec.y = 0;
    const sp = this.pos('stick');
    this.stickBase.setPosition(sp.x, sp.y);
    this.stickThumb.setPosition(sp.x, sp.y);
    if (this.attackBaseScale > 0) this.attackBtn.setScale(this.attackBaseScale);
    if (this.tacticBaseScale > 0) this.tacticBtn.setScale(this.tacticBaseScale);
  }

  // ------------------------------------------------------------------ update
  /** overlays dependentes de estado: anel do elemento, cooldowns, destaque */
  update(): void {
    if (this.destroyed || !this.api) return;
    const stats = this.api.getPlayerStats();
    const s = this.userScale;
    const g = this.fxG;
    g.clear();

    // anel do elemento ativo no botão de ataque
    g.lineStyle(4, ELEMENT_COLORS[stats.element], 0.95);
    g.strokeCircle(this.attackBtn.x, this.attackBtn.y, TOUCH.size.attack * s + 5);

    // "pizzas" de cooldown (tática na cor do elemento; esquiva dourada)
    this.drawCooldown(this.tacticBtn.x, this.tacticBtn.y, TOUCH.size.tactic * s,
      stats.tacticCdRemaining, stats.tacticCdTotal);
    this.drawCooldown(this.dodgeBtn.x, this.dodgeBtn.y, TOUCH.size.dodge * s,
      stats.dodgeCdRemaining, stats.dodgeCdTotal);

    // destaque do elemento atual no carrossel
    if (stats.element !== this.lastElement) {
      this.lastElement = stats.element;
      this.carouselIcons.forEach((img, i) => {
        if (ELEMENTS[i] === stats.element) img.clearTint().setAlpha(1);
        else img.setTint(0x777788).setAlpha(0.8);
      });
    }
    const idx = ELEMENTS.indexOf(stats.element);
    const icon = this.carouselIcons[idx];
    g.lineStyle(3, ELEMENT_COLORS[stats.element], 0.95);
    g.strokeCircle(this.carousel.x + icon.x, this.carousel.y + icon.y, (TOUCH.size.icon / 2) * s + 5);
  }

  private drawCooldown(x: number, y: number, r: number, remaining: number, total: number): void {
    if (total <= 0 || remaining <= 0) return;
    const frac = Phaser.Math.Clamp(remaining / total, 0, 1);
    this.fxG.fillStyle(COLORS.nightDeep, 0.72);
    this.fxG.slice(x, y, r, -Math.PI / 2, -Math.PI / 2 + frac * Math.PI * 2, false);
    this.fxG.fillPath();
  }

  // -------------------------------------------------------------- edição
  private enableEditing(): void {
    const s = this.userScale;
    const sz = TOUCH.size;

    const drag = (
      obj: Phaser.GameObjects.Image | Phaser.GameObjects.Container,
      id: TouchControlId,
      onDrag?: (x: number, y: number) => void,
    ): void => {
      this.scene.input.setDraggable(obj);
      obj.on('drag', (_p: Phaser.Input.Pointer, dragX: number, dragY: number) => {
        const x = Phaser.Math.Clamp(dragX, 40, GAME_WIDTH - 40);
        const y = Phaser.Math.Clamp(dragY, 40, GAME_HEIGHT - 40);
        obj.setPosition(x, y);
        onDrag?.(x, y);
      });
      obj.on('dragend', () => {
        const cur = Settings.get().controls.touch;
        Settings.update({
          controls: {
            ...Settings.get().controls,
            touch: {
              ...cur,
              layout: { ...cur.layout, [id]: { x: Math.round(obj.x), y: Math.round(obj.y) } },
            },
          },
        });
        Audio.playSfx('ui_click');
      });
      // pulso sutil indica "editável"
      this.scene.tweens.add({
        targets: obj, alpha: 0.6, duration: 700, yoyo: true, repeat: -1,
      });
    };

    this.hitImage(this.stickBase, sz.stickBase * s);
    drag(this.stickBase, 'stick', (x, y) => this.stickThumb.setPosition(x, y));

    this.hitImage(this.attackBtn, sz.attack * s);
    drag(this.attackBtn, 'attack');
    this.hitImage(this.tacticBtn, sz.tactic * s);
    drag(this.tacticBtn, 'tactic');
    this.hitImage(this.dodgeBtn, sz.dodge * s);
    drag(this.dodgeBtn, 'dodge');

    const cw = sz.iconGap * s * ELEMENTS.length;
    const ch = sz.icon * s + 16;
    this.carousel.setInteractive(
      new Phaser.Geom.Rectangle(-cw / 2, -ch / 2, cw, ch),
      Phaser.Geom.Rectangle.Contains,
    );
    drag(this.carousel, 'carousel');
  }

  // -------------------------------------------------------------- destroy
  destroy(): void {
    if (this.destroyed) return;
    this.destroyed = true;
    this.scene.input.off(Phaser.Input.Events.POINTER_DOWN, this.onDown);
    this.scene.input.off(Phaser.Input.Events.POINTER_MOVE, this.onMove);
    this.scene.input.off(Phaser.Input.Events.POINTER_UP, this.onUp);
    const objs: (Phaser.GameObjects.GameObject | null)[] = [
      this.stickBase, this.stickThumb, this.attackBtn, this.tacticBtn,
      this.dodgeBtn, this.pauseBtn, this.carousel, this.fxG,
    ];
    for (const o of objs) {
      if (!o) continue;
      this.scene.tweens.killTweensOf(o);
      o.destroy();
    }
    this.carouselIcons = [];
  }
}
