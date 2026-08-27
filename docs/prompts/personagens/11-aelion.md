# 11 — AELION, o Arco do Crepúsculo

> **Fila:** posição 13 (lote 5). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 11) · `personagens/11-aelion.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Alto Elfo, função **Longa distância**. Guardião das Torres Arcanas por mais
tempo do que dura uma linhagem humana, com uma obrigação só: que nenhuma torre
caísse na vigília dele. Uma caiu. Não fala disso — aliás, fala pouco de qualquer
coisa: guarda as palavras como guarda as flechas, e erra menos que ambas.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,95 m.** Alto e **esguio** — o mais fino do elenco. Ombros
  estreitos, membros longos, silhueta vertical.
- **Pele pálida com brilho perolado** (subsuperfície leve, não metálica).
- **Orelhas longas e finas inclinadas PARA TRÁS** (não para os lados).
- **Olhos âmbar SEM PUPILA.**
- **Cabelo prateado liso até a cintura**, preso num **rabo alto por um anel de
  ouro**.
- **Move-se como quem não pesa** — postura leve, peso na ponta dos pés.
- **Vestuário:** **túnica de gala élfica azul-crepúsculo** com **bordado de
  constelações em fio dourado**; **OMBREIRA ÚNICA no braço do arco**
  (assimetria obrigatória — braço direito, o que puxa a corda é o esquerdo);
  **luva de falcoaria no antebraço ESQUERDO**; **botas de cano alto SEM SALTO**
  (silêncio); **capa curta assimétrica presa por broche de estrela**.
- **O ARCO LONGO CURVO + ALJAVA nas costas** é o adereço-assinatura. **Nas
  vistas de produção: só a ALJAVA nas costas; o ARCO fica de fora** (arco
  atravessando o corpo estraga o multi-view). Arco isolado em §5.

## 2. Paleta

| Uso | Hex |
|---|---|
| Azul-crepúsculo (dominante) | `#2B2E63` |
| Dourado (bordado / acento) | `#F0C75E` |
| Violeta (energia) | `#8A5CF0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A high elf archer, 1.95 m, tall and very slender, standing at the top of an
arcane tower at dusk, weightless in his bearing. Pale skin with a pearl sheen;
LONG THIN EARS angled BACKWARDS; amber eyes with NO pupil; straight silver hair
to the waist, gathered in a high ponytail by a gold ring. A twilight-blue elven
ceremonial tunic embroidered with CONSTELLATIONS in gold thread; a SINGLE
pauldron on his bow arm only; a falconry gauntlet on his left forearm;
high-shafted boots with no heel; a short asymmetric cape pinned by a star
brooch. He holds a long curved bow, a quiver of arrows on his back. Thin
gold-violet tracers streak the sky behind him. Palette #2B2E63, #F0C75E,
#8A5CF0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Sem o arco.** Aljava sim, arco não — §5.

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
High elf man, 1.95 m, tall and very slender, narrow shoulders, long limbs. Pale
pearl-sheen skin; long thin ears angled BACKWARDS; amber eyes with no pupil;
straight silver hair to the waist in a high ponytail with a gold ring.
Twilight-blue #2B2E63 elven tunic with gold #F0C75E constellation embroidery; a
SINGLE pauldron on the RIGHT shoulder only; a falconry gauntlet on the LEFT
forearm; high boots with no heel; a short asymmetric cape pinned at one
shoulder, hanging BEHIND and never crossing the body. A quiver of arrows on his
back. NO BOW in frame, no weapon in the hands, no tracers, no particles.
```

- **FRENTE** — vista principal; a assimetria ombreira/luva precisa ficar óbvia.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **é o lado da luva de falcoaria**;
  exigir que a **aljava apareça de perfil** com a espessura real e que o cabelo
  longo não esconda a linha das costas.
- **COSTAS** — o cabelo até a cintura, a aljava e o corte da capa curta. Vista
  crítica: é onde a aljava é o assunto.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) THE LONGBOW, isolated on plain dark grey: flat side view, edge-on view, and a
   close view of the grip — three separate images. A long, deeply curved elven
   bow of pale wood and gold fittings, the limbs carved with the same
   constellation motif as the tunic, a slim string.
2) The quiver, isolated: front, true 90-degree side, back, with fletched arrows.
3) The star brooch and the gold hair ring, isolated together, front and side.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
bow in frame, bow in hand, weapon in hand, arrow drawn, pauldrons on both
shoulders, symmetrical armour, gauntlets on both arms, ears pointing sideways,
short ears, pupils, round pupils, coloured irises, short hair, loose hair,
braided hair, high heels, heeled boots, heavy plate armour, robe with a hood,
wizard hat, muscular build, broad shoulders, cape covering the body, tracers,
particles, glowing arrows
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Ombreira ÚNICA** (um ombro só) + **luva de falcoaria num antebraço só**. A
   assimetria é a identidade; simetria reprova.
2. **Orelhas longas inclinadas para TRÁS**, não para os lados.
3. **Olhos âmbar sem pupila.**
4. **Cabelo prateado liso até a cintura, em rabo alto com anel de ouro.**
5. **Botas sem salto** (ele anda em silêncio — é caracterização, não estilo).
6. **Bordado de constelações em fio dourado** visível na túnica.
7. **Aljava nas costas presente nas quatro vistas; arco AUSENTE.**
8. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito.
9. **1,95 m com porte esguio** — alto sem virar musculoso; se ganhar massa,
   reprovada.
