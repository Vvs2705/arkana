# CENÁRIO — validação das 55 concept arts (26/08/2026)

**Origem:** `Downloads/concept arts` (ChatGPT, 55 PNGs ~2K), geradas pelo
Diretor com os prompts de `docs/prompts/`. **Validadas uma a uma contra os
critérios de aprovação de cada arquivo de prompt**, com lupa nos pontos
críticos. Resultado: **54 aprovadas · 1 para regerar** (texto de ajuste no fim).

## O que a lupa conferiu (os pontos que reprovariam)

| Peça | Critério crítico | Veredito |
|---|---|---|
| Castelo (5 vistas) | as 5 gemas nas cores dos elementos | ✅ fogo/água/vento/raio/terra presentes |
| Castelo | Selo (pentágono+A) no portão · ninguém a bordo | ✅ ambos |
| Baú (frente) | 5 micro-gemas do pentágono nas cores certas | ✅ vermelho/azul/âmbar/teal/amarelo |
| Baú | mais largo que alto · sem clichê pirata | ✅ |
| Luva Comum | couro surrado, costura visível, runa única, pontas abertas | ✅ tudo |
| Luva Conjurador (palma) | **checagem legal anti-FMA** | ✅ anéis geométricos, sem serpente/símbolos |
| Luva Conjurador | dedais fechados, cristais nos nós, cordão com contas | ✅ |
| Manopla | 2 gemas fogo+vento lado a lado · **polegar e médio em ouro** (os dedos do estalo) | ✅ exato |
| Manopla | Selo "A" patinado no antebraço, sem luz | ✅ |
| Ruínas | friso de mãos dadas no pedestal · fraturas · chama espectral azul | ✅ |
| Torre | coroa + cristal octaédrico + facho · porta-cortina de luz | ✅ |
| Altar (peças) | esfera TRANÇADA em 2 cores · pegadas polidas · obelisco partido com árvore | ✅ |

## Ressalvas registradas (não reprovam)

1. **Estilo mais ilustrado-realista** que a âncora "hand-painted/painterly"
   (sobretudo o castelo). Para referência de MODELO no Meshy isso é até
   melhor — mais informação de forma; quem estiliza no jogo é o toon shader.
2. **Manopla: as duas gemas acesas iguais** (a ficha pedia ativa/amortecida).
   Irrelevante para o modelo 3D — a alternância é material dinâmico no jogo.
3. **Âncora do castelo inteira** (a ficha pedia corrente partida). Cosmético.

## ❌ A ÚNICA REPROVADA — texto de ajuste

**Arquivo:** `08-altar-sintonia/arte/keyart-REGERAR.png` (a nº 48 do lote).

**O que está errado:** os CINCO obeliscos aparecem de pé com as gemas acesas.
A ficha (docs/prompts/08, item 1) exige **DOIS obeliscos tombados com gemas
MORTAS** — um partido em três tambores com a árvore jovem, um caído inteiro —
porque essa é a história que o altar conta: *o mundo esqueceu a Sintonia*.
Sem os mortos, o altar vira só um monumento bonito.

**Prompt de ajuste (colar por cima do prompt mestre do arquivo 08):**

> Same attunement altar scene, but TWO of the five obelisks have FALLEN and
> their gems are DARK and dead: one broken into three drums with a young
> golden-leaf tree growing between the drums, one lying whole on the grass.
> Their floor light-grooves are dry halfway to the center. Only THREE
> obelisks remain standing with lit gems. Keep everything else identical.

**As peças modulares do altar (49–55) estão aprovadas** — inclusive o
obelisco partido (51), que existe separado. Só a key art de conjunto regera.

## Estrutura das pastas

`cenario/<peça>/arte/*.png` — nomes por vista (frente/lado/costas/
tres-quartos/apresentacao). O espelho da estrutura de `personagens/`. Tudo em
LFS (regra em `.gitattributes`, custo anotado lá).

## Próximo passo

Peça nº 1 da fila (DIRECAO.md §10): **Castelo** → Meshy multi-imagem com
`frente-selo` + `lateral-porta-salto` + `costas` + `tres-quartos`.
