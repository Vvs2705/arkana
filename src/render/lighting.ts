// ============================================================================
// PRISMA-1 · raia 1 — luz dinâmica e pós-processamento (Tech Artist Iluminação)
//
// Estratégia:
//   - Light2D aplicado por VARREDURA da display list + listener 'addedtoscene'
//     (nenhum arquivo de outra raia é editado). Objetos "emissivos" (projéteis,
//     overlays de fogo/faísca, UI) ficam FORA do pipeline — não escurecem.
//   - Emissores de luz detectados por textura: 'proj-*' (cor do elemento),
//     'fx-flame' (fogo pulsante), 'fx-spark' (eletricidade). Pool de luzes
//     atribuído por distância à câmera — nunca estoura o maxLights do Phaser.
//   - Post-FX na câmera principal: Bloom + Vinheta + ColorMatrix (grading).
//
// WebGL only: em CANVAS o módulo inteiro vira no-op (init retorna cedo).
// Qualidade (Settings.video.quality):
//   low    → só luz ambiente (sem luzes dinâmicas, sem post-FX)
//   medium → ambiente + cajado + 7 luzes dinâmicas + vinheta + grading
//   high   → tudo: 9 luzes dinâmicas + bloom
// ============================================================================
import Phaser from 'phaser';
import { ArenaLike, RenderModule } from './contract';
import { DEPTH, ELEMENT_COLORS, PROJ_TEX, TEX } from '../core/config';
import { Settings } from '../core/settings';
import { ELEMENTS } from '../core/types';

type Quality = 'low' | 'medium' | 'high';

// --- números visuais próprios da raia (invariante da leva: consts locais) ---
/** entardecer arcano — azul-noite (#0B1026) clareado p/ manter leitura de jogo */
const AMBIENT_COLOR = 0x8087b0;
/** luz fraca do cajado do jogador — dourado do GDD §10 (#F0C75E) */
const STAFF_COLOR = 0xf0c75e;
const STAFF_RADIUS = 150;
const STAFF_INTENSITY = 0.55;
/** flicker sutil do cajado (senoidal) */
const STAFF_FLICKER = 0.06;
/** luz de projétil — cor do elemento (ELEMENT_COLORS) */
const PROJ_RADIUS = 95;
const PROJ_INTENSITY = 0.9;
/** célula queimando — laranja quente, pulsante */
const FLAME_COLOR = 0xff7a2e;
const FLAME_RADIUS = 125;
const FLAME_INTENSITY = 0.85;
/** célula eletrizada — amarelo raio, pulso mais rápido embutido no mesmo canal */
const SPARK_COLOR = 0xf5d90a;
const SPARK_RADIUS = 100;
const SPARK_INTENSITY = 0.75;
/** velocidade/amplitude do pulso das luzes de terreno */
const PULSE_SPEED = 6.5;
const PULSE_AMP = 0.22;
// ponytail: teto de 1 (cajado) + 9 pool = 10 = render.maxLights default do
// Phaser; subir exige GameConfig em main.ts (fora desta raia — PRISMA-4)
const POOL_SIZE: Record<Quality, number> = { low: 0, medium: 7, high: 9 };
/** post-FX (câmera) */
const VIGNETTE_RADIUS = 0.92;
const VIGNETTE_STRENGTH = 0.32;
const BLOOM_STRENGTH = 1.15;
const BLOOM_BLUR = 1.0;
const GRADING_CONTRAST = 0.06;
const GRADING_SATURATE = 0.12;

/** emissivos/UI que NUNCA entram no Light2D (não devem ser escurecidos) */
const UNLIT_KEY_PREFIXES = ['proj-', 'fx-', 'vfx-', 'ui-', 'touch-', 'icon-'];

/** cor de luz por textura de projétil (derivado dos contratos, não duplicado) */
const PROJ_LIGHT_COLOR: Record<string, number> = {};
for (const el of ELEMENTS) PROJ_LIGHT_COLOR[PROJ_TEX[el]] = ELEMENT_COLORS[el];

interface LightEmitter {
  x: number;
  y: number;
  color: number;
  radius: number;
  intensity: number;
  /** 0 = luz fixa; ≠0 = fase do pulso senoidal */
  pulse: number;
  /** distância² ao centro da câmera (critério do pool) */
  d2: number;
}

export class LightingFx implements RenderModule {
  private scene?: ArenaLike;
  private enabled = false;

  private staff?: Phaser.GameObjects.Light;
  private pool: Phaser.GameObjects.Light[] = [];

  private bloom?: Phaser.FX.Bloom;
  private vignette?: Phaser.FX.Vignette;
  private grading?: Phaser.FX.ColorMatrix;

  private offSettings?: () => void;

  // ------------------------------------------------------------------- init
  init(scene: ArenaLike): void {
    // degradação graciosa: sem WebGL não há Light2D nem post-FX — no-op total
    if (scene.sys.game.renderer.type !== Phaser.WEBGL) return;
    this.scene = scene;
    this.enabled = true;

    scene.lights.enable().setAmbientColor(AMBIENT_COLOR);

    // aplica Light2D no que já existe e em tudo que outras raias adicionarem
    for (const obj of scene.children.list) this.applyLightPipeline(obj);
    scene.events.on(Phaser.GameObjects.Events.ADDED_TO_SCENE, this.onAdded, this);

    this.applyQuality(Settings.get().video.quality);
    this.offSettings = Settings.onChange((s) => this.applyQuality(s.video.quality));
  }

  // ----------------------------------------------------------------- update
  update(time: number, _delta: number): void {
    if (!this.enabled || !this.scene) return;
    const t = time * 0.001;

    // cajado do jogador segue a ref viva (contrato ArenaApi)
    if (this.staff) {
      const p = this.scene.getPlayerRef();
      this.staff.setPosition(p.x, p.y);
      this.staff.setIntensity(STAFF_INTENSITY + Math.sin(t * 2.3) * STAFF_FLICKER);
    }
    if (this.pool.length === 0) return;

    // uma única varredura da display list coleta todos os emissores do frame
    const cam = this.scene.cameras.main;
    const cx = cam.midPoint.x;
    const cy = cam.midPoint.y;
    const emitters: LightEmitter[] = [];
    for (const obj of this.scene.children.list) {
      if (!(obj instanceof Phaser.GameObjects.Image || obj instanceof Phaser.GameObjects.Sprite)) continue;
      if (!obj.visible || !obj.active) continue;
      const key = obj.texture ? obj.texture.key : '';
      const projColor = PROJ_LIGHT_COLOR[key];
      let e: LightEmitter | null = null;
      if (projColor !== undefined) {
        e = { x: obj.x, y: obj.y, color: projColor, radius: PROJ_RADIUS, intensity: PROJ_INTENSITY, pulse: 0, d2: 0 };
      } else if (key === TEX.FX_FLAME) {
        e = { x: obj.x, y: obj.y, color: FLAME_COLOR, radius: FLAME_RADIUS, intensity: FLAME_INTENSITY, pulse: obj.x * 0.37 + obj.y * 0.19, d2: 0 };
      } else if (key === TEX.FX_SPARK) {
        e = { x: obj.x, y: obj.y, color: SPARK_COLOR, radius: SPARK_RADIUS, intensity: SPARK_INTENSITY, pulse: obj.x * 0.51 + obj.y * 0.23, d2: 0 };
      }
      if (e) {
        const dx = e.x - cx;
        const dy = e.y - cy;
        e.d2 = dx * dx + dy * dy;
        emitters.push(e);
      }
    }

    // as N luzes do pool vão para os N emissores mais próximos da câmera
    emitters.sort((a, b) => a.d2 - b.d2);
    for (let i = 0; i < this.pool.length; i++) {
      const light = this.pool[i];
      const e = emitters[i];
      if (!e) {
        light.setVisible(false);
        continue;
      }
      light.setVisible(true);
      light.setPosition(e.x, e.y);
      light.setRadius(e.radius);
      light.setColor(e.color);
      const k = e.pulse !== 0 ? 1 - PULSE_AMP + PULSE_AMP * Math.sin(t * PULSE_SPEED + e.pulse) : 1;
      light.setIntensity(e.intensity * k);
    }
  }

  // ---------------------------------------------------------------- destroy
  destroy(): void {
    if (!this.enabled || !this.scene) return;
    this.offSettings?.();
    this.offSettings = undefined;
    this.scene.events.off(Phaser.GameObjects.Events.ADDED_TO_SCENE, this.onAdded, this);

    // remove SÓ os nossos post-FX (clear() apagaria FX de outras raias)
    const cam = this.scene.cameras?.main;
    if (cam) {
      if (this.bloom) cam.postFX.remove(this.bloom);
      if (this.vignette) cam.postFX.remove(this.vignette);
      // typings do Phaser: FX.ColorMatrix não declara Controller, mas é um FX
      if (this.grading) cam.postFX.remove(this.grading as unknown as Phaser.FX.Controller);
    }
    this.bloom = this.vignette = this.grading = undefined;

    const lights = this.scene.lights;
    if (lights) {
      for (const l of this.pool) lights.removeLight(l);
      if (this.staff) lights.removeLight(this.staff);
      lights.disable();
    }
    this.pool = [];
    this.staff = undefined;
    this.scene = undefined;
    this.enabled = false;
  }

  // ---------------------------------------------------------------- helpers
  private onAdded(obj: Phaser.GameObjects.GameObject): void {
    this.applyLightPipeline(obj);
  }

  /**
   * Light2D em objetos de MUNDO (terreno/entidades/decorações de outras raias),
   * nunca em HUD (depth ≥ 100) nem em emissivos (projéteis, chamas, UI).
   */
  private applyLightPipeline(obj: Phaser.GameObjects.GameObject): void {
    if (
      !(obj instanceof Phaser.GameObjects.Image) &&
      !(obj instanceof Phaser.GameObjects.Sprite) &&
      !(obj instanceof Phaser.GameObjects.RenderTexture)
    ) return;
    if (obj.depth >= DEPTH.HUD) return;
    const key = obj.texture ? obj.texture.key : '';
    if (UNLIT_KEY_PREFIXES.some((p) => key.startsWith(p))) return;
    obj.setPipeline('Light2D');
  }

  /** monta/desmonta post-FX e pool conforme a qualidade (reage a mudanças ao vivo) */
  private applyQuality(q: Quality): void {
    if (!this.enabled || !this.scene) return;
    const cam = this.scene.cameras.main;

    if (this.bloom) { cam.postFX.remove(this.bloom); this.bloom = undefined; }
    if (this.vignette) { cam.postFX.remove(this.vignette); this.vignette = undefined; }
    if (this.grading) { cam.postFX.remove(this.grading as unknown as Phaser.FX.Controller); this.grading = undefined; }

    if (q !== 'low') {
      this.vignette = cam.postFX.addVignette(0.5, 0.5, VIGNETTE_RADIUS, VIGNETTE_STRENGTH);
      this.grading = cam.postFX.addColorMatrix();
      this.grading.contrast(GRADING_CONTRAST, true);
      this.grading.saturate(GRADING_SATURATE, true);
    }
    if (q === 'high') {
      this.bloom = cam.postFX.addBloom(0xffffff, 1, 1, BLOOM_BLUR, BLOOM_STRENGTH, 4);
    }

    // pool de luzes dinâmicas — cresce/encolhe até o alvo da qualidade
    const want = POOL_SIZE[q];
    while (this.pool.length > want) {
      const l = this.pool.pop();
      if (l) this.scene.lights.removeLight(l);
    }
    while (this.pool.length < want) {
      const l = this.scene.lights.addLight(-9999, -9999, PROJ_RADIUS, 0xffffff, 0);
      l.setVisible(false);
      this.pool.push(l);
    }

    // luz do cajado só a partir de 'medium' (low = só ambiente)
    if (q === 'low') {
      if (this.staff) {
        this.scene.lights.removeLight(this.staff);
        this.staff = undefined;
      }
    } else if (!this.staff) {
      this.staff = this.scene.lights.addLight(0, 0, STAFF_RADIUS, STAFF_COLOR, STAFF_INTENSITY);
    }
  }
}
