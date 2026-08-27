# 02 — CEIFADORA, a Voz do Vazio

> **Fila:** posição 3 (lote 1). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 2) · `personagens/02-ceifadora.md`.

## 0. Quem é

Humana (tocada pelo Vazio), Vanguarda, Ataque / Perseguição. Morreu afogada
numa fenda do Vazio e voltou remendada. Classificação 10+: **bonito, nunca
mórbido** — sem caveira, sem gore, sem podridão.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,82 m.** Silhueta afiada e ereta — a mais vertical do lote 1.
- **A marca mágica (o que a auditoria de 25/08 mediu como ERRADO na arte
  antiga):** **kintsugi do Vazio** — a pele tem **rachaduras finas de
  PORCELANA QUEBRADA** no rosto e nos braços, emendadas com **luz violeta**,
  como louça consertada com ouro. ⚠️ A arte de 24/08 transformou isso numa
  **malha regular**, quase um padrão de tecido. **Não é malha, não é rede, não
  é padrão repetido:** são fraturas irregulares, ramificadas, poucas e grandes,
  cada uma com uma linha de luz violeta correndo no meio.
- **A segunda assinatura: ela NÃO projeta sombra.** A sombra ficou do outro
  lado. Na imagem de apresentação isso é visível no chão iluminado; nas vistas
  de produção, o **BLOCO B pede sombra de contato** (o Meshy precisa dela para
  entender que ela pisa no chão) — a ausência de sombra é feature de render
  no jogo (`personagens/02-ceifadora.md`, notas de 3D), não da concept.
- Olhos cinza-claros, **quase brancos**. Cabelo preto com **uma mecha
  descolorida** pela travessia.
- **Vestuário — elegância fúnebre:** casaca longa de **gola alta preta**,
  **meia-máscara cobrindo o maxilar** (o queixo e a boca ficam cobertos, os
  olhos e as maçãs à mostra), luvas de aparência líquida. **Detalhe dourado
  mínimo:** um único fio na costura, ecoando a emenda das rachaduras.
- **A FOICE NÃO É CARREGADA.** Ela se condensa na mão quando chamada e evapora
  depois. **Gerar as quatro vistas SEM a foice** — mãos vazias e visíveis. A
  foice tem geração isolada própria (§5).

## 2. Paleta

| Uso | Hex |
|---|---|
| Preto-vazio (dominante) | `#1A1A22` |
| Violeta (energia / kintsugi) | `#8A5CF0` |
| Fio dourado (acento mínimo) | `#F0C75E` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A tall human woman, 1.82 m, sharp and upright, standing on a lit stone floor
in a void-torn hall — and SHE CASTS NO SHADOW while every other object does.
Her skin is cracked like broken porcelain across the cheekbones and the
forearms: few, large, IRREGULAR branching fractures, each mended with a thin
seam of VIOLET LIGHT, in the manner of kintsugi. Not a mesh, not a net, not a
repeating pattern. Pale grey, almost white eyes. Black hair with ONE bleached
streak. Funeral elegance: a long high-collared black coat, a half-mask
covering the jaw and mouth while the eyes stay bare, gloves that look liquid.
One single golden thread runs along a seam of the coat, echoing the mended
cracks. Her hands are EMPTY — no weapon. Rated 10+: beautiful, never morbid,
no skull, no gore. Palette #1A1A22, #8A5CF0, #F0C75E.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Tall human woman, 1.82 m, sharp upright silhouette. Skin cracked like broken
porcelain on the face and forearms, few large irregular fractures mended with
thin violet light seams — NOT a regular mesh or repeating pattern. Pale grey
almost white eyes; black hair with one bleached streak. Long high-collared
black coat #1A1A22, a half-mask covering the jaw and mouth, liquid-looking
gloves, one single golden thread along a coat seam. HANDS EMPTY, no scythe, no
weapon of any kind, arms clear of the torso. Flat boots. Palette #1A1A22,
#8A5CF0, #F0C75E.
```

- **FRENTE** — vista principal; as rachaduras do rosto acima da meia-máscara
  precisam ser legíveis.
- **PERFIL ESQUERDO 90° VERDADEIRO** — a gola alta vista de lado é o que mais
  engana o gerador: exigir que a curva das costas e a profundidade do peito
  fiquem visíveis **por dentro** da gola, não escondidas por ela.
- **COSTAS** — a queda da casaca longa e a gola por trás.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peça-assinatura isolada (geração extra, 2D)

```
1) The spectral scythe, isolated on plain dark grey, side view and
   three-quarter: a long curved blade of condensed violet smoke on a dark
   shaft, the blade half-dissolving into vapour at the tip. It is summoned,
   not carried — show it floating, with no hand holding it.
2) The kintsugi marks: face and forearms alone on plain dark grey, TWO
   versions — one flat black-and-white crack pattern with NO glow, one
   coloured with the violet seams. Do not bake strong glow into the albedo.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
regular mesh pattern, net pattern, fishnet, grid on the skin, repeating
geometric pattern, cracked mud texture, skull face, skeleton, grim reaper
robe, hooded skull, gore, rotting flesh, zombie, blood, scythe in her hands,
weapon held, full face mask, mask covering the eyes, shadow under her feet in
the presentation image, heavy gold ornament, gold armour
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Kintsugi de PORCELANA:** fraturas grandes, poucas, irregulares e
   ramificadas — **não** malha, rede ou padrão repetido. Este é o erro medido
   na arte de 24/08 e é o motivo principal desta regeração.
2. **A emenda é de luz VIOLETA `#8A5CF0`**, correndo dentro da fratura.
3. **Meia-máscara cobrindo o maxilar**, olhos e maçãs à mostra.
4. **Mãos vazias** nas quatro vistas — a foice é condensável e não entra.
5. **Um único fio dourado** na costura. Se houver ouro em quantidade, reprovada
   (o mínimo é a identidade dela).
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, e a
   profundidade do corpo visível apesar da gola alta.
7. 10+: nada de caveira, gore ou sangue. Bonito, nunca mórbido.
