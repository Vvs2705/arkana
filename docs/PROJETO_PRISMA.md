# PROJETO PRISMA — Direção técnica de arte do Arkana

> Documento de estratégia gráfica. Objetivo: sair do "protótipo funcional" e chegar
> num visual comparável a jogos 2D top-down comerciais, **sem trocar de engine** e
> **sem quebrar a lógica de jogo já validada**.
> Criado em 17/08/2026, após o v0.1 fechar o DoD da seção 15 do GDD.

---

## 1. Diagnóstico honesto do v0.1

O que já funciona e NÃO se toca: lógica de terreno reativo, balanceamento, FSM dos bots,
escudo evolutivo, contratos (`src/core/types.ts`), fluxo de cenas.

O que denuncia "protótipo" aos olhos, em ordem de gravidade:

| # | Problema | Por que mata a percepção de qualidade |
|---|---|---|
| 1 | **Tudo é chapado, sem luz** | Jogos comerciais 2D têm iluminação dinâmica. Sem luz não há volume, hora do dia, nem drama. É o maior gap isolado. |
| 2 | **Tiles com borda dura em quadrado** | Grama e terra se encontram num degrau reto de 32px. Nenhum jogo publicado faz isso — todos usam *autotiling* (transição por bitmask). |
| 3 | **Zero pós-processamento** | Sem bloom, o fogo não "brilha"; sem vinheta/grading, a cena parece um editor, não um jogo. |
| 4 | **Personagens estáticos** | Sprite parado, sem idle, sem inclinação, sem sombra: parece um ícone deslizando, não um mago andando. |
| 5 | **Repetição visível** | 1 variante por tile → padrão de xadrez perceptível a olho nu. |
| 6 | **VFX de 1 camada** | Fogo = triângulo laranja. Fogo real em jogo = brasas + fumaça + luz pulsante + distorção. |
| 7 | **Impacto sem peso** | Falta hitstop, flash, onda de choque, recuo — o "juice" que faz o combate parecer bom mesmo com arte simples. |

**Conclusão estratégica:** o salto NÃO vem de "desenhar melhor". Vem de **pipeline de
renderização**. Um tileset medíocre com luz, autotiling e bloom parece muito melhor que
um tileset bonito renderizado chapado. Por isso a ordem das fases abaixo é essa.

---

## 2. Decisão de engine: FICAR no Phaser 3 (por enquanto)

Avaliado e recusado por ora: migrar para Godot 4.

- Phaser 3.60+ tem **post-FX pipelines nativos** (Bloom, Blur, Vignette, ColorMatrix,
  Displacement) e o **Light2D pipeline** com normal maps. O teto para 2D top-down é alto
  o bastante para o objetivo — não é o Phaser que está limitando hoje, é o uso dele.
- O GDD (seção 15) já declara o código do protótipo descartável e o documento portátil.
  Trocar de engine agora custa a curva de GDScript e joga fora sistemas já validados
  em teste real. **Momentum vale mais que teto teórico nesta fase.**
- Gatilho para reavaliar Godot: quando a visão 3D/primeira pessoa (Degrau 4 do GDD)
  entrar em pauta, ou se o Prisma-1/2 bater num teto de performance mensurável.

---

## 3. Estratégia em 4 levas

### PRISMA-1 — Fundação de render *(maior ROI; nenhuma arte nova necessária)*
Roda como uma leva de 4 agentes paralelos. É o salto que se vê em screenshot.

1. **Luz & pós-processamento** — luz ambiente de entardecer arcano; luzes dinâmicas
   emitidas por projéteis, fogo no terreno, muros de raio e pelo cajado do jogador;
   Bloom (o fogo passa a sangrar luz), vinheta, color grading quente/frio por bioma.
2. **Terreno de verdade** — autotiling por bitmask (transições suaves grama↔terra↔água),
   4+ variantes por tile, decals (pedras, flores, rachaduras), água com onda animada e
   espuma na borda, resolução de textura dobrada (64px desenhados, mundo lógico intacto).
3. **Personagens vivos** — sprites 64px com idle respirando, caminhada, manto em 2
   segmentos com inércia, inclinação na direção do movimento, sombra elíptica projetada,
   squash & stretch no dash.
4. **VFX & juice** — trilha nos projéteis, impacto com onda de choque + flash + hitstop,
   fogo em 3 camadas (chama + brasas subindo + fumaça), partículas ambientais (pólen,
   faíscas), screen shake por camadas.

**Invariante das 4 raias:** proibido alterar `src/core/types.ts`, `balance.ts` (valores),
`layout.ts` e a lógica de simulação do terreno. Só a camada de apresentação muda.

### PRISMA-2 — Arte autoral com IA + curadoria dura
Onde a geração por IA realmente ganha (e onde ela falha):
- ✅ **Retratos dos magos** para o lobby (GDD seção 6) — é exatamente o caso de uso onde
  IA entrega qualidade de gacha game por fração do custo.
- ✅ **Key art** da tela de carregamento, fundos de menu, ilustrações de Presságio.
- ✅ **Ícones** premium de elemento, runa e cosmético.
- ❌ **Tilesets** — IA não produz tiles *seamless* com bitmask confiável. Tile continua
  procedural/CC0. Não insistir nisso.
- Regra: todo asset gerado entra em `docs/CREDITS.md` com origem e data.

### PRISMA-3 — Animação esqueletal (Spine/Live2D) *(só após tração)*
O GDD seção 6 já aponta: 1 artwork + rigging 2D = aparência premium por fração do 3D.
Entra quando houver arte autoral final, não antes.

### PRISMA-4 — Otimização mobile
O Prisma-1 assume PC. Antes do APK (Fase 2 do GDD): orçamento de partículas por
qualidade, luzes limitadas por distância, "Modo Névoa" (GDD 19.2) desligando post-FX.

---

## 4. Como retomar isto numa sessão nova

1. Ler este arquivo + `docs/GDD.md` seções 10, 14 e 15.
2. Memória da equipe: `C:\Users\ti\Videos\MEUS PROJETOS\Agentes\_memoria\arkana.md`
   (stack, armadilhas conhecidas, caminho do Node portátil).
3. Rodar: `preview_start` com a config `arkana-dev` (`.claude/launch.json`).
4. Typecheck: `node_modules\typescript\bin\tsc --noEmit -p tsconfig.json` usando o
   node do driver Playwright (a máquina não tem Node no PATH).
5. Padrão de trabalho que funcionou: **coordenador escreve contratos → 4 subagents em
   raias de arquivos disjuntas → typecheck isolado por raia ao chegar cada reporte →
   integração → teste real no navegador.**

---

## 5. Estado de execução

- [x] PRISMA-1 despachado em 17/08/2026 (4 raias paralelas)
- [ ] PRISMA-2 — arte autoral por IA
- [ ] PRISMA-3 — animação esqueletal
- [ ] PRISMA-4 — otimização mobile
