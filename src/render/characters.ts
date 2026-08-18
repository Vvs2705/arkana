// PRISMA-1 · raia 3 — personagens vivos: idle, caminhada, sombra, inclinação,
// squash&stretch (stub do coordenador; a raia 3 preenche e pode criar arquivos
// irmãos character*.ts — o grosso do trabalho vive em src/entities/*).
import { ArenaLike, RenderModule } from './contract';

export class CharacterFx implements RenderModule {
  init(_scene: ArenaLike): void {}
  update(_time: number, _delta: number): void {}
  destroy(): void {}
}
