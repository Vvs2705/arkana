// ============================================================================
// ARKANA v0.1 — cena da arena (raia C: gameplay)
// Liga terreno (raia B), entidades/HUD (raia D) e o mago/projéteis (raia C).
// Implementa a ArenaApi do contrato: bots atacam via spawnProjectile e o
// HUD lê getPlayerStats/getEntities; hitEntity roteia dano e alimenta a
// evolução do escudo.
// ============================================================================
import Phaser from 'phaser';
import {
  ArenaApi,
  Element,
  EntityRef,
  EVT,
  ITerrainGrid,
  PlayerStats,
  ProjectileSpawn,
} from '../core/types';
import { ARENA_COLS, ARENA_ROWS, SCENE, TILE } from '../core/config';
import { Audio } from '../core/audio';
import { touchControlsEnabled } from '../core/settings';
import { TerrainGrid } from '../terrain/TerrainGrid';
import { Bot } from '../entities/Bot';
import { Dummy } from '../entities/Dummy';
import { HUD } from '../ui/HUD';
import { TouchControls } from '../ui/TouchControls';
import { Player } from '../entities/Player';
import { ProjectilePool } from '../entities/Projectile';
import { RenderModule } from '../render/contract';
import { TerrainSkin } from '../render/terrainSkin';
import { LightingFx } from '../render/lighting';
import { CharacterFx } from '../render/characters';
import { CombatVfx } from '../render/vfx';

/** suavização da câmera ao seguir o player (briefing: ~0.12) */
const CAM_LERP = 0.12;
/** célula inicial do player — centro exato do mapa 60×60 (briefing) */
const PLAYER_CELL = { cx: 30, cy: 30 };
/** dummy a ~5 células do player, em área aberta (briefing) */
const DUMMY_OFFSET_CELLS = 5;
/** posições fixas dos 5 bots (um por elemento), afastadas do centro */
const BOT_SPOTS: ReadonlyArray<readonly [number, number, Element]> = [
  [12, 12, Element.FIRE],
  [47, 12, Element.WATER],
  [12, 47, Element.EARTH],
  [47, 47, Element.WIND],
  [30, 8, Element.LIGHTNING],
];

export class ArenaScene extends Phaser.Scene implements ArenaApi {
  /**
   * CONTRATO vs PHASER: a ArenaApi exige `scene: Phaser.Scene` (Bot/Dummy/HUD
   * usam api.scene.add, api.scene.events etc.), mas o Phaser injeta o
   * ScenePlugin exatamente nesta propriedade. Solução: preservamos o plugin
   * em `scenePlugin` e apontamos `scene` para a própria cena no create()
   * (restaurando no shutdown). REGRA INTERNA: pause/launch/restart SEMPRE
   * via this.scenePlugin — nunca via this.scene.
   */
  declare scene: Phaser.Scene & Phaser.Scenes.ScenePlugin;
  private scenePlugin!: Phaser.Scenes.ScenePlugin;

  /** exposto pelo contrato ArenaApi (consumido por Bot/Dummy/HUD/pool) */
  terrain!: ITerrainGrid;

  /** camada de toque (Fase 2 — GDD 19.3); undefined = desktop puro */
  touch?: TouchControls;

  private player!: Player;
  private dummy!: Dummy;
  private bots: Bot[] = [];
  private pool!: ProjectilePool;
  private hud!: HUD;
  /** modo de toque com que o HUD atual foi construído (ele muda de layout) */
  private hudTouchMode = false;
  /** PRISMA-1: módulos de apresentação, na ordem de init/update */
  private fx: RenderModule[] = [];

  /** morte congela novos spawns (projéteis em voo terminam o trajeto) */
  private playerDead = false;
  /** update só roda depois do create completo */
  private ready = false;

  constructor() {
    super(SCENE.ARENA);
  }

  // ----------------------------------------------------------------- create
  create(): void {
    // preserva o ScenePlugin nativo antes de cumprir o contrato (scene = this)
    const injected: unknown = this.scene;
    if (injected !== this) this.scenePlugin = injected as Phaser.Scenes.ScenePlugin;
    (this as unknown as { scene: Phaser.Scene }).scene = this;

    this.playerDead = false;
    Audio.playMusic('arena');
    // botão direito é a tática — sem menu de contexto do navegador
    this.input.mouse?.disableContextMenu();

    // terreno (raia B) primeiro: tudo consulta o grid
    this.terrain = new TerrainGrid(this);
    this.terrain.generate();

    const worldW = ARENA_COLS * TILE;
    const worldH = ARENA_ROWS * TILE;
    this.cameras.main.setBounds(0, 0, worldW, worldH);

    // player no centro do mapa (busca a célula andável mais próxima por via
    // das dúvidas — o layout é fixo, mas a raia B é dona dele)
    const pc = this.findOpenCell(PLAYER_CELL.cx, PLAYER_CELL.cy);
    const pw = this.terrain.cellToWorld(pc.cx, pc.cy);
    this.player = new Player(this, pw.x, pw.y);
    this.cameras.main.startFollow(this.player.sprite, true, CAM_LERP, CAM_LERP);

    // pool antes das entidades: bots podem atacar já no primeiro update
    this.pool = new ProjectilePool(this);

    // dummy de treino em área aberta perto do player
    const dc = this.findOpenCell(pc.cx + DUMMY_OFFSET_CELLS, pc.cy);
    const dw = this.terrain.cellToWorld(dc.cx, dc.cy);
    this.dummy = new Dummy(this, dw.x, dw.y);

    // 5 bots espalhados, um por elemento, ids estáveis bot-1..bot-5
    this.bots = BOT_SPOTS.map(([cx, cy, el], i) => {
      const c = this.findOpenCell(cx, cy);
      const w = this.terrain.cellToWorld(c.cx, c.cy);
      return new Bot(this, w.x, w.y, `bot-${i + 1}`, el);
    });

    // HUD por último: lê getPlayerStats/getEntities já populados
    this.hud = new HUD(this);
    this.hudTouchMode = touchControlsEnabled();

    // camada de toque (Auto detecta o hardware; "Ligado" testa no desktop).
    // Desligado = nenhum overlay, nenhum listener — desktop 100% intacto.
    this.syncTouchLayer();

    // PRISMA-1: módulos de apresentação (4 raias de render — só visual).
    // Ordem: pele do terreno → luz → personagens → vfx.
    this.fx = [new TerrainSkin(), new LightingFx(), new CharacterFx(), new CombatVfx()];
    for (const m of this.fx) m.init(this);

    // pausa (a PauseScene de outra raia cuida do resto)
    this.input.keyboard?.on('keydown-ESC', this.onEsc, this);
    this.events.on(Phaser.Scenes.Events.RESUME, this.onResume, this);
    this.events.on(EVT.PLAYER_DIED, this.onPlayerDied, this);
    this.events.once(Phaser.Scenes.Events.SHUTDOWN, this.onShutdown, this);

    this.ready = true;
  }

  // ----------------------------------------------------------------- update
  update(time: number, delta: number): void {
    if (!this.ready) return;
    this.terrain.update(time, delta);
    this.player.update(time, delta);
    // Bot cuida do próprio respawn — instâncias vivem para sempre;
    // refs de mortos somem via filtro em getEntities()
    for (const b of this.bots) b.update(time, delta);
    this.dummy.update(time, delta);
    this.pool.update(time, delta);
    this.hud.update(time, delta);
    this.touch?.update();
    for (const m of this.fx) m.update(time, delta);
  }

  // --------------------------------------------------------------- ArenaApi
  spawnProjectile(opts: ProjectileSpawn): void {
    if (this.playerDead) return; // morte do jogador para os spawns
    this.pool.spawn(opts);
  }

  getPlayerRef(): EntityRef {
    return this.player.ref;
  }

  /** player + bots vivos + dummy (dummy sempre presente) */
  getEntities(): EntityRef[] {
    const refs: EntityRef[] = [this.player.ref];
    for (const b of this.bots) if (b.ref.alive) refs.push(b.ref);
    refs.push(this.dummy.ref);
    return refs;
  }

  getPlayerStats(): PlayerStats {
    return this.player.getStats();
  }

  // ------------------------------------------------------------------- dano
  /**
   * Usado pelo pool de projéteis: roteia o dano para a entidade certa e,
   * quando o autor é o jogador, alimenta a evolução do escudo (GDD §5).
   */
  hitEntity(ref: EntityRef, dmg: number, fromPlayer: boolean): void {
    let routed = false;
    if (ref.isPlayer) {
      this.player.takeDamage(dmg);
      routed = true;
    } else if (ref.id === this.dummy.ref.id) {
      this.dummy.takeDamage(dmg);
      routed = true;
    } else {
      const bot = this.bots.find((b) => b.ref === ref || b.ref.id === ref.id);
      if (bot) {
        bot.takeDamage(dmg);
        routed = true;
      }
    }
    if (routed && fromPlayer) this.player.addEvoProgress(dmg);
  }

  // --------------------------------------------------------------- handlers
  private onEsc(): void {
    if (this.playerDead) return;
    this.scenePlugin.launch(SCENE.PAUSE);
    this.scenePlugin.pause();
  }

  /** ao acordar da pausa, re-zera estados de input pendurados */
  private onResume(): void {
    this.player.clearInputState();
    this.syncTouchLayer(); // Configurações podem ter mudado no meio da partida
    this.touch?.clearState(); // pointerups perdidos durante a pausa
  }

  /**
   * Pausa → Configurações → voltar NÃO recria a Arena (PauseScene faz
   * scene.start(SETTINGS) e o retorno é scene.resume(ARENA)). Este é o único
   * ponto em que a Arena volta a rodar, então é aqui que a camada de toque se
   * acerta com o que o jogador mudou: escala, posição dos botões, esquema de
   * mira — e ligar/desligar o toque, que também vira o layout do HUD.
   */
  private syncTouchLayer(): void {
    const want = touchControlsEnabled();

    // o HUD decide o layout pelo modo de toque (o carrossel substitui os ícones
    // de elemento, o painel de vida sobe): trocar o modo exige refazê-lo.
    // Antes da camada de toque — mesma profundidade, quem nasce depois fica por cima.
    if (this.hudTouchMode !== want) {
      this.hud.destroy();
      this.hud = new HUD(this);
      this.hudTouchMode = want;
    }

    if (want === (this.touch !== undefined)) {
      this.touch?.refresh();
    } else if (want) {
      this.touch = new TouchControls(this, this, {
        onPause: () => this.onEsc(),
        onElement: (el) => this.player.setElement(el),
      });
    } else {
      this.touch?.destroy();
      this.touch = undefined;
    }
  }

  private onPlayerDied(): void {
    this.playerDead = true;
    // `once` DENTRO do handler de morte: só existe 1 listener por morte e
    // o shutdown do teclado limpa tudo entre restarts (sem duplicados)
    this.input.keyboard?.once('keydown-ENTER', () => this.scenePlugin.restart());
    // com toque ativo não há ENTER: qualquer toque na tela reinicia
    if (this.touch) {
      this.time.delayedCall(600, () => {
        if (this.ready) this.input.once('pointerdown', () => this.scenePlugin.restart());
      });
    }
  }

  private onShutdown(): void {
    this.ready = false;
    for (const m of this.fx) m.destroy();
    this.fx = [];
    this.events.off(EVT.PLAYER_DIED, this.onPlayerDied, this);
    this.events.off(Phaser.Scenes.Events.RESUME, this.onResume, this);
    // teclado limpa os próprios listeners no shutdown do Phaser
    this.touch?.destroy();
    this.touch = undefined;
    this.hud.destroy();
    this.pool.destroy();
    for (const b of this.bots) b.destroy();
    this.bots = [];
    this.dummy.destroy();
    this.player.destroy();
    this.terrain.destroy();
    // devolve o ScenePlugin nativo p/ o próximo ciclo start/create
    (this as unknown as { scene: Phaser.Scenes.ScenePlugin }).scene = this.scenePlugin;
  }

  // ---------------------------------------------------------------- helpers
  /**
   * Célula andável mais próxima da desejada (busca em anéis). O layout da
   * raia B é fixo e tem áreas abertas garantidas; isto é rede de segurança
   * contra spawn dentro de rocha/água.
   */
  private findOpenCell(cx: number, cy: number): { cx: number; cy: number } {
    const clampX = (v: number): number => Phaser.Math.Clamp(v, 1, this.terrain.cols - 2);
    const clampY = (v: number): number => Phaser.Math.Clamp(v, 1, this.terrain.rows - 2);
    cx = clampX(cx);
    cy = clampY(cy);
    if (!this.terrain.isBlocking(cx, cy)) return { cx, cy };
    for (let r = 1; r <= 12; r++) {
      for (let dy = -r; dy <= r; dy++) {
        for (let dx = -r; dx <= r; dx++) {
          if (Math.max(Math.abs(dx), Math.abs(dy)) !== r) continue; // só o anel
          const nx = clampX(cx + dx);
          const ny = clampY(cy + dy);
          if (!this.terrain.isBlocking(nx, ny)) return { cx: nx, cy: ny };
        }
      }
    }
    return { cx, cy }; // fallback defensivo — não deve acontecer
  }
}
