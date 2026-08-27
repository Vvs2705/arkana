# 18 — BASALTO, o Desperto

> **Fila:** posição 17 (lote 6 — escala extrema, por último de propósito).
> **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 18) · `personagens/18-basalto.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Golem rúnico, função **Curta distância**. Ninguém o acordou de propósito: abriu
o olho de âmbar dentro da ruína a noroeste e não havia mais ninguém para
explicar por quê. Duas toneladas de pedra rúnica, uma palavra por vez, e um
cuidado quase cômico com tudo que é pequeno.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 2,30 m** — **o maior do elenco**. Massa ~2 toneladas. É a altura que
  vai para o rigger.
- ⚠️ **RESTRIÇÃO TÉCNICA DURA:** o auto-rigging da Meshy **exige humanoide
  bípede com membros claros** (`docs/MESHY.md` §2). O prompt precisa entregar
  **duas pernas, dois braços, cabeça e tronco separados e inequívocos** —
  **nenhum bloco de pedra fundido, nada de quadrúpede, nada de forma amorfa**.
  É o pior caso de rig do elenco, e é por isso que ele está no lote 6.
- **Corpo:** **rocha viva cinza-basalto**, **placas como músculos** com **magma
  visível nas JUNTAS** (ombro, cotovelo, joelho, quadril — onde o corpo dobra).
- **UM OLHO de gema âmbar no centro do rosto**, e **rosto SEM BOCA** — toda a
  emoção mora no brilho do olho.
- **Runas de criação gravadas no PEITO** — é o "coração"; brilha quando ele usa
  poder (versão sem glow obrigatória, §5).
- **OMBROS ASSIMÉTRICOS:** o **direito carrega um FRAGMENTO DE OBELISCO antigo
  cravado** — é ele que quebra a simetria e faz a silhueta.
- **Não veste nada — ele É a própria armadura.** Mas carrega:
  - **CORRENTES CERIMONIAIS no ombro ESQUERDO**, com **placas de bronze** — uma
    para cada mestre que já serviu, e **a última está EM BRANCO** (ele procura um
    propósito digno de ser gravado). A placa em branco é obrigatória e legível.
  - **Musgo e pequenas flores crescendo nas fendas das COSTAS** (a Sylva planta;
    ele finge que não gosta).

## 2. Paleta

| Uso | Hex |
|---|---|
| Basalto (dominante) | `#4A4A50` |
| Magma (energia / juntas) | `#FF5A2A` |
| Âmbar (olho / acento) | `#F0C75E` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A runic stone golem, 2.30 m tall and about two tonnes, standing inside a
moonlit ruin, holding something very small with almost comic care. A CLEARLY
BIPEDAL HUMANOID: two legs, two arms, a distinct head and torso, all limbs
separate and unambiguous. His body is living grey basalt in plates shaped like
muscle, with MAGMA GLOWING IN THE JOINTS — shoulders, elbows, knees, hips.
Creation RUNES are carved across his chest, his heart, lit from within. His face
has ONE AMBER GEM EYE at its centre and NO MOUTH. His shoulders are asymmetric:
the RIGHT one carries a shard of ancient OBELISK driven into the stone.
Ceremonial CHAINS hang from his LEFT shoulder, strung with small bronze plates —
one for each master he has served, and THE LAST PLATE IS BLANK. Moss and tiny
flowers grow in the cracks of his back. Palette #4A4A50, #FF5A2A, #F0C75E.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Runic stone golem, 2.30 m, CLEARLY BIPEDAL HUMANOID with two legs, two arms, a
separate head and a separate torso — every limb distinct, no fused stone mass,
not quadrupedal, not amorphous, arms held clear of the body in a neutral A-pose.
Living grey basalt #4A4A50 in muscle-shaped plates; magma #FF5A2A visible in the
shoulder, elbow, knee and hip joints only. ONE amber gem eye at the centre of
the face, NO MOUTH, no nose, no ears. Creation runes carved across the chest.
The RIGHT shoulder carries a shard of ancient obelisk driven into the stone; the
LEFT shoulder carries ceremonial chains strung with small bronze plates, THE
LAST PLATE BLANK. Moss and small flowers in the cracks of the back. Stone feet
flat on the ground with a firm contact shadow. No dust, no embers, no particles.
```

- **FRENTE** — vista principal; olho-gema, runas do peito e a assimetria dos
  ombros no mesmo quadro.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica**: é a única vista que informa
  a **profundidade brutal** do peito e das placas, e mostra as correntes do
  ombro esquerdo de lado, com espessura.
- **COSTAS** — o musgo e as flores nas fendas são o assunto; e é onde o
  fragmento de obelisco mostra o quanto entra na pedra.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The obelisk shard, isolated on plain dark grey: front, true 90-degree side,
   back. Weathered carved stone, broken at one end, runes worn smooth.
2) The ceremonial chains with bronze plates, isolated: laid flat and hanging,
   each plate readable, one plate visibly BLANK and unengraved.
3) The chest creation runes: chest plate isolated, TWO versions — one flat
   carved relief with NO glow, one lit. Do not bake strong glow into the albedo.
4) The amber gem eye, isolated: front and side, faceted amber in a stone socket,
   TWO versions (dim and bright) — the emotion of the character is this eye.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
mouth, lips, teeth, nose, ears, two eyes, human face, quadruped, four legs,
crawling, fused stone block, amorphous rock pile, no distinct limbs, arms merged
with the torso, symmetrical shoulders, obelisk on both shoulders, chains on both
shoulders, all plates engraved, no blank plate, armour plates like metal,
rusted iron, robot, mecha, sci-fi machine, lava covering the whole body, glowing
whole body, dust, embers, particles, gore, blood
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **HUMANOIDE BÍPEDE INEQUÍVOCO** — dois braços, duas pernas, cabeça e tronco
   separados, braços afastados do corpo. **Se o auto-rigger não puder ler
   membros, a peça é inútil por mais bonita que seja** (`docs/MESHY.md` §2).
2. **UM olho de gema âmbar** e **rosto SEM BOCA** — sem nariz, sem orelhas, sem
   segundo olho.
3. **Magma SÓ nas juntas** — corpo inteiro brilhando reprova.
4. **Runas de criação no peito**, e entregues também sem glow (§5).
5. **Fragmento de obelisco no ombro DIREITO** e **correntes no ESQUERDO** — a
   assimetria é a silhueta.
6. **A última placa de bronze EM BRANCO** e legível como em branco. É a
   história inteira dele num detalhe.
7. **Musgo e flores nas fendas das costas.**
8. **Perfil de 90° real** — um olho visível (ele só tem um), nenhum pedaço do
   peito, com a profundidade das placas medida.
9. Teste da silhueta: todo preto, o ombro do obelisco ainda o identifica?
