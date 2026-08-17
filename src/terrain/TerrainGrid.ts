// ============================================================================
// ARKANA v0.1 — Terreno Reativo (raia B — Engenharia de Simulação)
//
// Arquitetura = as 3 regras do "chemistry engine" de BotW (GDD 16.1):
//   (1) elementos mudam o ESTADO de materiais (fogo acende árvore);
//   (2) elementos mudam outros elementos (água apaga fogo, raio eletriza água);
//   (3) materiais NUNCA mudam materiais (o campo `material` é imutável).
// O fogo usa o modelo acadêmico de 4 estados de incêndio florestal (GDD 16.1):
//   sem combustível → intacto → QUEIMANDO → QUEIMADO (permanente).
//
// Performance (regras duras da raia):
//   - 1 única RenderTexture do mapa inteiro (60×60×32 = 1920px) em DEPTH.TERRAIN;
//     redesenho SÓ de células sujas (dirty) — nunca o mapa inteiro por frame;
//   - simulação por tick (BAL.terrain.burnTickMs) via acumulador de delta;
//   - Set de células ATIVAS (com timer) — jamais iteramos as 3600 células;
//   - overlays animados (chama/faísca) apenas em células ativas, guardados em
//     Map<índice, Sprite> e destruídos ao sair do estado (DEPTH.TERRAIN_FX).
// ============================================================================
import Phaser from 'phaser';
import { Element, TerrainMaterial, TerrainState } from '../core/types';
import type { HazardInfo, ITerrainGrid, TerrainCell } from '../core/types';
import { ARENA_COLS, ARENA_ROWS, DEPTH, TEX, TILE } from '../core/config';
import { BAL } from '../core/balance';
import { Audio } from '../core/audio';
import type { SfxName } from '../core/audio';
import { generateLayout } from './layout';

/** atalho: TODOS os números do terreno vêm daqui (fonte única: balance.ts) */
const T = BAL.terrain;

/**
 * Limite do flood fill de eletrização (~200 células, spec da raia B).
 * Não existe em BAL.terrain — constante local documentada, não é balanceamento
 * de combate e sim guarda de performance.
 */
const ELECTRIFY_FLOOD_LIMIT = 200;

/** vizinhança-4 (von Neumann) — propagação de fogo, vento e condução elétrica */
const NEI4: ReadonlyArray<readonly [number, number]> = [
  [1, 0], [-1, 0], [0, 1], [0, -1],
];

/** tile base por material (NORMAL/BURNING/ELECTRIFIED mantêm o tile do material) */
const MAT_TEX: Record<TerrainMaterial, string> = {
  [TerrainMaterial.GRASS]: TEX.TILE_GRASS,
  [TerrainMaterial.TALL_GRASS]: TEX.TILE_TALL_GRASS,
  [TerrainMaterial.DIRT]: TEX.TILE_DIRT,
  [TerrainMaterial.ROCK]: TEX.TILE_ROCK,
  [TerrainMaterial.WATER]: TEX.TILE_WATER,
  [TerrainMaterial.TREE]: TEX.TILE_TREE,
};

/** cor de minimapa por material em estado NORMAL (estados sobrepõem — ver getCellColor) */
const MAT_COLOR: Record<TerrainMaterial, number> = {
  [TerrainMaterial.GRASS]: 0x3f7a46,      // verde grama
  [TerrainMaterial.TALL_GRASS]: 0x33663d, // verde um pouco mais fechado
  [TerrainMaterial.DIRT]: 0x8a6a42,       // marrom terra
  [TerrainMaterial.ROCK]: 0x777c86,       // cinza rocha
  [TerrainMaterial.WATER]: 0x2a6fc4,      // azul água
  [TerrainMaterial.TREE]: 0x1d4526,       // verde-escuro floresta
};

export class TerrainGrid implements ITerrainGrid {
  readonly cols: number = ARENA_COLS;
  readonly rows: number = ARENA_ROWS;
  readonly tileSize: number = TILE;

  private scene: Phaser.Scene;
  /** camada base do mapa — desenhada uma vez; depois só células sujas */
  private rt: Phaser.GameObjects.RenderTexture;
  /** grade achatada (idx = cy * cols + cx) — acesso O(1) e Sets baratos */
  private cells: TerrainCell[] = [];
  /** células com timer rodando (BURNING/FROZEN/ELECTRIFIED/MUD) — a simulação SÓ itera aqui */
  private active = new Set<number>();
  /** células cujo tile mudou e precisa de redraw na RenderTexture */
  private dirtyCells = new Set<number>();
  /** overlays animados por célula ativa (chama de fogo ou faísca elétrica) */
  private overlays = new Map<number, Phaser.GameObjects.Sprite>();
  /** acumulador de delta → ticks de simulação em passo fixo */
  private acc = 0;

  constructor(scene: Phaser.Scene) {
    this.scene = scene;
    this.rt = scene.add
      .renderTexture(0, 0, this.cols * this.tileSize, this.rows * this.tileSize)
      .setOrigin(0, 0)
      .setDepth(DEPTH.TERRAIN);
  }

  // ------------------------------------------------------------- geração
  generate(): void {
    const layout = generateLayout(this.cols, this.rows); // determinístico (layout.ts)
    this.cells = new Array<TerrainCell>(this.cols * this.rows);
    for (let cy = 0; cy < this.rows; cy++) {
      for (let cx = 0; cx < this.cols; cx++) {
        this.cells[cy * this.cols + cx] = {
          material: layout[cy][cx],
          state: TerrainState.NORMAL,
          stateTimer: 0,
        };
      }
    }
    // pintura inicial completa — ÚNICA varredura total do mapa; depois disso,
    // toda escrita na RT passa pelo funil de dirty cells
    this.rt.beginDraw();
    for (let cy = 0; cy < this.rows; cy++) {
      for (let cx = 0; cx < this.cols; cx++) {
        const cell = this.cells[cy * this.cols + cx];
        this.rt.batchDraw(MAT_TEX[cell.material], cx * this.tileSize, cy * this.tileSize);
      }
    }
    this.rt.endDraw();
    this.dirtyCells.clear();
  }

  // ------------------------------------------------------------- consultas
  getCell(cx: number, cy: number): TerrainCell | null {
    if (cx < 0 || cy < 0 || cx >= this.cols || cy >= this.rows) return null;
    return this.cells[cy * this.cols + cx] ?? null;
  }

  worldToCell(x: number, y: number): { cx: number; cy: number } {
    return { cx: Math.floor(x / this.tileSize), cy: Math.floor(y / this.tileSize) };
  }

  /** centro da célula em coordenadas de mundo */
  cellToWorld(cx: number, cy: number): { x: number; y: number } {
    return { x: (cx + 0.5) * this.tileSize, y: (cy + 0.5) * this.tileSize };
  }

  isBlocking(cx: number, cy: number): boolean {
    const cell = this.getCell(cx, cy);
    if (!cell) return true; // fora do mapa = parede
    if (cell.state === TerrainState.WALL) return true;
    switch (cell.material) {
      case TerrainMaterial.ROCK:
        return true;
      case TerrainMaterial.TREE:
        // BURNED não bloqueia — a cobertura DESAPARECE (queimar floresta é jogada)
        return cell.state !== TerrainState.BURNED;
      case TerrainMaterial.WATER:
        // congelada vira ponte; líquida (mesmo eletrizada) bloqueia
        return cell.state !== TerrainState.FROZEN;
      default:
        return false;
    }
  }

  blocksProjectile(cx: number, cy: number): boolean {
    const cell = this.getCell(cx, cy);
    if (!cell) return true;
    if (cell.state === TerrainState.WALL) return true;
    switch (cell.material) {
      case TerrainMaterial.ROCK:
        return true;
      case TerrainMaterial.TREE:
        return cell.state !== TerrainState.BURNED; // árvore em pé (viva ou em chamas)
      default:
        return false; // água NÃO bloqueia projétil
    }
  }

  getSpeedMult(cx: number, cy: number): number {
    const cell = this.getCell(cx, cy);
    if (!cell) return 1;
    if (cell.state === TerrainState.MUD) return T.mudSlowMult;
    if (cell.material === TerrainMaterial.TALL_GRASS && cell.state === TerrainState.NORMAL) {
      return T.tallGrassSpeedMult;
    }
    return 1;
  }

  getHazard(cx: number, cy: number): HazardInfo {
    const cell = this.getCell(cx, cy);
    if (!cell) return { dps: 0, slowMult: 1 };
    let dps = 0;
    if (cell.state === TerrainState.BURNING) {
      // dps rampa de burnDmgMin a burnDmgMax conforme o tempo queimando
      const elapsed = T.burnDurationMs - cell.stateTimer;
      const f = Phaser.Math.Clamp(elapsed / T.burnDurationMs, 0, 1);
      dps = T.burnDmgMin + (T.burnDmgMax - T.burnDmgMin) * f;
    } else if (cell.state === TerrainState.ELECTRIFIED) {
      dps = T.electrifyDps;
    }
    return { dps, slowMult: this.getSpeedMult(cx, cy) };
  }

  isConcealing(cx: number, cy: number): boolean {
    const cell = this.getCell(cx, cy);
    // grama alta queimada (ou em chamas) perde a ocultação — fogo REVELA (GDD 14)
    return !!cell
      && cell.material === TerrainMaterial.TALL_GRASS
      && cell.state === TerrainState.NORMAL;
  }

  getCellColor(cx: number, cy: number): number {
    const cell = this.getCell(cx, cy);
    if (!cell) return 0x000000;
    switch (cell.state) {
      case TerrainState.BURNING: return 0xe8722c;     // laranja fogo
      case TerrainState.BURNED: return 0x191512;      // quase-preto carvão
      case TerrainState.FROZEN: return 0xa9e6ef;      // ciano-claro gelo
      case TerrainState.WALL: return 0x9095a0;        // cinza muro
      case TerrainState.MUD: return 0x5b4222;         // marrom lama
      case TerrainState.ELECTRIFIED: return 0xf2e14c; // amarelo — perigo elétrico
      default: return MAT_COLOR[cell.material];
    }
  }

  // ------------------------------------------------------------- reações
  /**
   * Tabela de reações (GDD 14 + 16.4). strong=true (tática) chega do chamador
   * já com raio maior; aqui strong só remove as chances (efeito garantido).
   * Moderação de áudio: no MÁXIMO 1 sfx por chamada, nunca por célula.
   */
  applyElement(worldX: number, worldY: number, element: Element, radiusCells: number, strong: boolean): void {
    const { cx, cy } = this.worldToCell(worldX, worldY);
    let sfx: SfxName | null = null;
    switch (element) {
      case Element.FIRE:
        if (this.applyFire(cx, cy, radiusCells, strong)) sfx = 'burn';
        break;
      case Element.WATER:
        if (this.applyWater(cx, cy, radiusCells)) sfx = 'freeze';
        break;
      case Element.EARTH:
        if (this.applyEarth(cx, cy, radiusCells, strong)) sfx = 'wall';
        break;
      case Element.WIND:
        this.applyWind(cx, cy, radiusCells); // vento não tem sfx de terreno próprio
        break;
      case Element.LIGHTNING:
        if (this.applyLightning(cx, cy, radiusCells)) sfx = 'electrocute';
        break;
    }
    if (sfx) Audio.playSfx(sfx);
    this.flushDirty(); // feedback visual imediato no impacto
  }

  /** FOGO: acende inflamáveis, derrete gelo, seca lama; água só "chia" */
  private applyFire(cx: number, cy: number, r: number, strong: boolean): boolean {
    let ignited = false;
    this.forDisc(cx, cy, r, (_x, _y, idx, cell) => {
      switch (cell.state) {
        case TerrainState.FROZEN:
          // fogo derrete a ponte de gelo — volta a ser água líquida
          cell.state = TerrainState.NORMAL;
          cell.stateTimer = 0;
          this.active.delete(idx);
          this.markDirty(idx);
          break;
        case TerrainState.MUD:
          // fogo seca o lamaçal — volta ao material
          cell.state = TerrainState.NORMAL;
          cell.stateTimer = 0;
          this.active.delete(idx);
          this.markDirty(idx);
          break;
        case TerrainState.NORMAL:
          if (cell.material === TerrainMaterial.TREE || cell.material === TerrainMaterial.TALL_GRASS) {
            this.igniteCell(idx); // combustível denso: acende sempre
            ignited = true;
          } else if (cell.material === TerrainMaterial.GRASS) {
            // grama rasteira: garantido na tática, senão ~30% (reusa propagateChance)
            if (strong || Math.random() < T.propagateChance) {
              this.igniteCell(idx);
              ignited = true;
            }
          }
          // WATER líquida: nada — só o chiado
          break;
        default:
          break; // BURNING/BURNED/WALL/ELECTRIFIED: sem reação ao fogo
      }
    });
    return ignited;
  }

  /** ÁGUA: congela lago (ponte!), apaga fogo, faz lamaçal em terra/carvão */
  private applyWater(cx: number, cy: number, r: number): boolean {
    let froze = false;
    this.forDisc(cx, cy, r, (_x, _y, idx, cell) => {
      if (cell.state === TerrainState.BURNING) {
        // água apaga o fogo — célula volta ao NORMAL, material preservado
        this.extinguish(idx);
        return;
      }
      if (cell.material === TerrainMaterial.WATER
        && (cell.state === TerrainState.NORMAL || cell.state === TerrainState.ELECTRIFIED)) {
        // congela a superfície — vira ponte; congelar também corta a condução
        this.clearOverlay(idx);
        cell.state = TerrainState.FROZEN;
        cell.stateTimer = T.freezeDurationMs;
        this.active.add(idx);
        this.markDirty(idx);
        froze = true;
        return;
      }
      if (cell.state === TerrainState.BURNED
        || (cell.material === TerrainMaterial.DIRT && cell.state === TerrainState.NORMAL)) {
        // água + terra (ou carvão) = lamaçal — lentidão severa na área
        cell.state = TerrainState.MUD;
        cell.stateTimer = T.mudDurationMs;
        this.active.add(idx);
        this.markDirty(idx);
      }
    });
    return froze;
  }

  /** TERRA: ergue muro destrutível (não sobrescreve WATER/ROCK/TREE) */
  private applyEarth(cx: number, cy: number, r: number, strong: boolean): boolean {
    let raised = false;
    const tryWall = (idx: number): void => {
      const cell = this.cells[idx];
      if (cell.state === TerrainState.WALL) return;
      const m = cell.material;
      if (m === TerrainMaterial.WATER || m === TerrainMaterial.ROCK || m === TerrainMaterial.TREE) return;
      // erguer o muro sufoca fogo e esmaga lama na célula (terra sela o chão)
      this.active.delete(idx);
      this.clearOverlay(idx);
      cell.state = TerrainState.WALL;
      cell.stateTimer = -1; // permanente até ser destruído
      cell.wallHp = T.wallHp;
      this.markDirty(idx);
      raised = true;
    };
    if (strong) {
      // tática: todo o disco vira muro onde o chão permite
      this.forDisc(cx, cy, r, (_x, _y, idx) => tryWall(idx));
    } else if (this.getCell(cx, cy)) {
      tryWall(cy * this.cols + cx); // básico: só a célula de impacto
    }
    return raised;
  }

  /**
   * VENTO — triângulo do fogo (GDD 16.4): fogo precisa de oxigênio.
   * Chama pequena (≤1 vizinho queimando) → o vento rouba o ar e APAGA.
   * Incêndio estabelecido (≥2 vizinhos) → o vento ALIMENTA e espalha na hora.
   * Decisão de risco real a cada uso; não afeta MUD.
   */
  private applyWind(cx: number, cy: number, r: number): void {
    const burning: number[] = [];
    this.forDisc(cx, cy, r, (_x, _y, idx, cell) => {
      if (cell.state === TerrainState.BURNING) burning.push(idx);
    });
    // snapshot dos contadores ANTES de agir — a rajada é simultânea
    const counts = burning.map((idx) => this.countBurningNeighbors(idx));
    burning.forEach((idx, i) => {
      if (counts[i] <= 1) {
        this.extinguish(idx); // sufoca a chama pequena
      } else {
        // alimenta o incêndio: espalha imediatamente p/ todos os vizinhos inflamáveis
        this.forEachNeighbor(idx, (nIdx) => {
          if (this.isFlammable(this.cells[nIdx])) this.igniteCell(nIdx);
        });
      }
    });
  }

  /**
   * RAIO — condução (GDD 16.4): água líquida conduz; gelo e rocha isolam.
   * Eletriza TODA a região de água conectada via flood fill (BFS) limitado.
   * Em outros materiais: nada além da faísca (visual é do chamador).
   */
  private applyLightning(cx: number, cy: number, r: number): boolean {
    const isLiquid = (cell: TerrainCell): boolean =>
      cell.material === TerrainMaterial.WATER && cell.state !== TerrainState.FROZEN;

    // fonte: prioriza a célula de impacto; senão a primeira água líquida no raio
    let source = -1;
    const center = this.getCell(cx, cy);
    if (center && isLiquid(center)) {
      source = cy * this.cols + cx;
    } else {
      this.forDisc(cx, cy, r, (_x, _y, idx, cell) => {
        if (source < 0 && isLiquid(cell)) source = idx;
      });
    }
    if (source < 0) return false;

    // BFS pela água conectada — ROCK isola naturalmente (flood só passa por água)
    const queue: number[] = [source];
    const seen = new Set<number>(queue);
    let head = 0;
    while (head < queue.length && head < ELECTRIFY_FLOOD_LIMIT) {
      const idx = queue[head++];
      const cell = this.cells[idx];
      cell.state = TerrainState.ELECTRIFIED; // refresca se já estava eletrizada
      cell.stateTimer = T.electrifyDurationMs;
      this.active.add(idx);
      this.addSpark(idx); // tile da água não muda — overlay é o aviso
      this.forEachNeighbor(idx, (nIdx) => {
        if (!seen.has(nIdx) && isLiquid(this.cells[nIdx])) {
          seen.add(nIdx);
          queue.push(nIdx);
        }
      });
    }
    return true;
  }

  // ------------------------------------------------------------- dano físico
  /**
   * Dano físico em células. v0.1: SÓ muros sofrem dano — árvore atingida por
   * projétil NÃO cai (remover cobertura natural é exclusividade do FOGO;
   * decisão de design: mantém o fogo como única chave anti-cobertura e
   * simplifica o tuning — documentado na spec da raia B).
   */
  damageCell(cx: number, cy: number, dmg: number): void {
    const cell = this.getCell(cx, cy);
    if (!cell || cell.state !== TerrainState.WALL) return;
    cell.wallHp = (cell.wallHp ?? T.wallHp) - dmg;
    if (cell.wallHp <= 0) {
      // muro desmorona — volta ao material base
      cell.state = TerrainState.NORMAL;
      cell.stateTimer = 0;
      delete cell.wallHp;
      this.markDirty(cy * this.cols + cx);
      this.flushDirty();
    }
  }

  // ------------------------------------------------------------- simulação
  update(_time: number, delta: number): void {
    this.acc += delta;
    while (this.acc >= T.burnTickMs) {
      this.acc -= T.burnTickMs;
      this.simTick(T.burnTickMs);
    }
    this.flushDirty();
  }

  /** um tick de simulação — itera SÓ as células ativas (nunca as 3600) */
  private simTick(dtMs: number): void {
    // snapshot: a propagação adiciona células novas (entram no próximo tick)
    const snapshot = Array.from(this.active);
    for (const idx of snapshot) {
      const cell = this.cells[idx];
      switch (cell.state) {
        case TerrainState.BURNING:
          this.tickBurning(idx, cell, dtMs);
          break;
        case TerrainState.FROZEN:
          cell.stateTimer -= dtMs;
          if (cell.stateTimer <= 0) {
            // gelo derrete sozinho — volta a ser água líquida (quem estava em cima cai)
            cell.state = TerrainState.NORMAL;
            cell.stateTimer = 0;
            this.active.delete(idx);
            this.markDirty(idx);
          }
          break;
        case TerrainState.ELECTRIFIED:
          cell.stateTimer -= dtMs;
          if (cell.stateTimer <= 0) {
            cell.state = TerrainState.NORMAL;
            cell.stateTimer = 0;
            this.active.delete(idx);
            this.clearOverlay(idx); // tile da água não mudou — sem redraw
          }
          break;
        case TerrainState.MUD:
          cell.stateTimer -= dtMs;
          if (cell.stateTimer <= 0) {
            // lama seca — volta ao material base
            cell.state = TerrainState.NORMAL;
            cell.stateTimer = 0;
            this.active.delete(idx);
            this.markDirty(idx);
          }
          break;
        default:
          this.active.delete(idx); // defensivo: estado sem timer não pertence ao set
          break;
      }
    }
  }

  /** tick de uma célula em chamas: propaga (chance por vizinho) e vira carvão no fim */
  private tickBurning(idx: number, cell: TerrainCell, dtMs: number): void {
    cell.stateTimer -= dtMs;
    // propagação: chance por tick para CADA vizinho-4 inflamável;
    // grama rasteira tem menos combustível → metade da chance
    this.forEachNeighbor(idx, (nIdx) => {
      const n = this.cells[nIdx];
      if (!this.isFlammable(n)) return;
      // grama rasteira NÃO pega fogo por propagação (só por acerto direto):
      // sem isso a reação em cadeia carboniza o campo aberto inteiro — o
      // incêndio deve consumir COBERTURA (floresta/grama alta), GDD seção 14
      if (n.material === TerrainMaterial.GRASS) return;
      if (Math.random() < T.propagateChance) this.igniteCell(nIdx);
    });
    if (cell.stateTimer <= 0) {
      // queimou tudo: carvão PERMANENTE — árvore some como cobertura e a
      // grama alta queimada perde a ocultação (isConcealing consulta o estado)
      cell.state = TerrainState.BURNED;
      cell.stateTimer = -1;
      this.active.delete(idx);
      this.clearOverlay(idx);
      this.markDirty(idx);
    }
  }

  // ------------------------------------------------------------- helpers de estado
  /** inflamável = TREE/GRASS/TALL_GRASS em estado NORMAL (modelo 4 estados) */
  private isFlammable(cell: TerrainCell): boolean {
    return cell.state === TerrainState.NORMAL
      && (cell.material === TerrainMaterial.TREE
        || cell.material === TerrainMaterial.GRASS
        || cell.material === TerrainMaterial.TALL_GRASS);
  }

  /** acende uma célula (chamador garante que é inflamável em NORMAL) */
  private igniteCell(idx: number): void {
    const cell = this.cells[idx];
    if (!this.isFlammable(cell)) return; // guarda defensiva (vento em cascata)
    cell.state = TerrainState.BURNING;
    cell.stateTimer = T.burnDurationMs;
    this.active.add(idx);
    this.addFlame(idx);
    // BURNING mantém o tile do material — a chama é overlay; sem redraw aqui
  }

  /** apaga uma célula em chamas antes de virar carvão — material preservado */
  private extinguish(idx: number): void {
    const cell = this.cells[idx];
    cell.state = TerrainState.NORMAL;
    cell.stateTimer = 0;
    this.active.delete(idx);
    this.clearOverlay(idx);
    // tile do material nunca mudou durante o BURNING — sem redraw necessário
  }

  // ------------------------------------------------------------- iteração espacial
  /** itera as células válidas de um disco de raio r (em células) */
  private forDisc(
    cx: number,
    cy: number,
    r: number,
    fn: (x: number, y: number, idx: number, cell: TerrainCell) => void,
  ): void {
    const ri = Math.max(0, Math.ceil(r));
    const rr = r * r;
    for (let dy = -ri; dy <= ri; dy++) {
      for (let dx = -ri; dx <= ri; dx++) {
        if (dx * dx + dy * dy > rr) continue;
        const x = cx + dx;
        const y = cy + dy;
        if (x < 0 || y < 0 || x >= this.cols || y >= this.rows) continue;
        const idx = y * this.cols + x;
        fn(x, y, idx, this.cells[idx]);
      }
    }
  }

  /** visita os índices válidos da vizinhança-4 de uma célula */
  private forEachNeighbor(idx: number, fn: (nIdx: number) => void): void {
    const cx = idx % this.cols;
    const cy = (idx - cx) / this.cols;
    for (const [dx, dy] of NEI4) {
      const x = cx + dx;
      const y = cy + dy;
      if (x < 0 || y < 0 || x >= this.cols || y >= this.rows) continue;
      fn(y * this.cols + x);
    }
  }

  private countBurningNeighbors(idx: number): number {
    let n = 0;
    this.forEachNeighbor(idx, (nIdx) => {
      if (this.cells[nIdx].state === TerrainState.BURNING) n++;
    });
    return n;
  }

  // ------------------------------------------------------------- renderização
  private markDirty(idx: number): void {
    this.dirtyCells.add(idx);
  }

  /** redesenha SÓ as células sujas na RenderTexture (nunca o mapa inteiro) */
  private flushDirty(): void {
    if (this.dirtyCells.size === 0) return;
    this.rt.beginDraw();
    for (const idx of this.dirtyCells) {
      const cx = idx % this.cols;
      const cy = (idx - cx) / this.cols;
      this.rt.batchDraw(this.texFor(this.cells[idx]), cx * this.tileSize, cy * this.tileSize);
    }
    this.rt.endDraw();
    this.dirtyCells.clear();
  }

  /** tile atual da célula: estados sobrepõem o material; BURNING/ELECTRIFIED mantêm o tile */
  private texFor(cell: TerrainCell): string {
    switch (cell.state) {
      case TerrainState.BURNED: return TEX.TILE_BURNED;
      case TerrainState.FROZEN: return TEX.TILE_FROZEN;
      case TerrainState.MUD: return TEX.TILE_MUD;
      case TerrainState.WALL: return TEX.TILE_WALL;
      default: return MAT_TEX[cell.material];
    }
  }

  // ------------------------------------------------------------- overlays animados
  /** chama animada sobre célula BURNING (tween de escala/alpha, variação por célula) */
  private addFlame(idx: number): void {
    if (this.overlays.has(idx)) return;
    const cx = idx % this.cols;
    const cy = (idx - cx) / this.cols;
    const { x, y } = this.cellToWorld(cx, cy);
    const s = this.scene.add.sprite(x, y - 4, TEX.FX_FLAME)
      .setDepth(DEPTH.TERRAIN_FX)
      .setScale(1.4);
    this.scene.tweens.add({
      targets: s,
      scaleX: 1.9,
      scaleY: 2.2,
      alpha: 0.72,
      duration: 220 + (idx % 5) * 45, // dessincroniza as chamas entre células
      yoyo: true,
      repeat: -1,
      ease: 'Sine.easeInOut',
    });
    this.overlays.set(idx, s);
  }

  /** faísca piscando sobre célula ELECTRIFIED */
  private addSpark(idx: number): void {
    if (this.overlays.has(idx)) return;
    const cx = idx % this.cols;
    const cy = (idx - cx) / this.cols;
    const { x, y } = this.cellToWorld(cx, cy);
    const s = this.scene.add.sprite(x, y, TEX.FX_SPARK)
      .setDepth(DEPTH.TERRAIN_FX)
      .setScale(1.3);
    this.scene.tweens.add({
      targets: s,
      alpha: 0.15,
      duration: 90 + (idx % 4) * 30,
      yoyo: true,
      repeat: -1,
      ease: 'Quad.easeInOut',
    });
    this.overlays.set(idx, s);
  }

  /** destrói o overlay (e seus tweens) de uma célula, se existir */
  private clearOverlay(idx: number): void {
    const s = this.overlays.get(idx);
    if (!s) return;
    this.scene.tweens.killTweensOf(s);
    s.destroy();
    this.overlays.delete(idx);
  }

  // ------------------------------------------------------------- limpeza
  destroy(): void {
    this.overlays.forEach((s) => {
      this.scene.tweens.killTweensOf(s);
      s.destroy();
    });
    this.overlays.clear();
    this.active.clear();
    this.dirtyCells.clear();
    this.rt.destroy();
    this.cells = [];
    this.acc = 0;
  }
}
