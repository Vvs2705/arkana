# 06 — OLHO-DE-ÉTER, o Observador

> **Fila:** posição 8 (lote 3). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 6) · `personagens/06-olho-de-eter.md`.

## 0. Quem é

Humano, Vidente, Longa distância / recon. **É SURDO** — a febre da infância
levou o som e abriu um olho no peito. As **mariposas-de-éter** que o orbitam são
os ouvidos dele: sentem vibração no ar e no chão e devolvem tudo em luz. Nunca
escutou uma palavra, e é sempre o primeiro a saber que você conjurou.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,83 m.** Teatral e hipnótico — **o mais ornamentado do elenco, por
  escolha dele**: "se o mundo não fala comigo, que ao menos me veja."
- **Pele escura** com **pó dourado nas maçãs do rosto**.
- **As marcas mágicas, as duas:**
  1. **OLHO-JOIA NO PEITO** — o terceiro olho que a febre abriu quando fechou os
     ouvidos. Fica no esterno, aberto, com íris de gema; material emissivo
     animado no jogo (pisca no "batimento" que ele vê). Precisa estar
     **descoberto e centrado** nas quatro vistas.
  2. **Colares de SINOS minúsculos** — ⚠️ **a auditoria de 25/08 mediu que eles
     FALTAM na arte de 24/08.** Eles não são enfeite: ele **não os ouve**, sente
     no esterno, e **é assim que percebe o próprio andar**. Sem os sinos, o
     personagem perde o mecanismo de existir.
- **Vestuário:** **torso ornamentado de gala** (aberto no esterno para o
  olho-joia); **saiote estruturado**; **mangas abertas e largas na boca** — para
  as mariposas pousarem nos antebraços. Magentas e dourados.
- **As mariposas são sistema de partículas no jogo** (3 sprites, nunca malha por
  mariposa): **não entram nas vistas de produção**; aparecem na apresentação e
  têm geração isolada, §5.

## 2. Paleta

| Uso | Hex |
|---|---|
| Magenta (dominante) | `#C24B8E` |
| Dourado (secundária) | `#F0C75E` |
| Éter (energia) | `#8A5CF0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A theatrical human seer, 1.83 m, dark-skinned with gold powder dusted across
his cheekbones, standing in a hall where thin rings of light ripple along the
floor — the vibrations he sees instead of hearing them. HE IS DEAF. Set in his
sternum is a THIRD EYE, a wide open jewel-eye with a gem iris, uncovered by his
clothing. Around his neck hang several strands of TINY BELLS that he cannot
hear — he feels them in his chest, and that is how he knows his own step. An
ornate ceremonial torso piece open at the sternum, a structured skirt-guard,
wide open sleeve mouths; magenta and gold, the most ornamented figure of the
cast, by his own choice. Geometric-winged ether moths orbit him and settle on
his forearms, translating the air into light. Palette #C24B8E, #F0C75E, #8A5CF0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Sem mariposas nas vistas de produção** — partícula escondendo peça é
motivo de reprovação (`BLOCO C`).

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Human man, 1.83 m, dark skin, gold powder on the cheekbones, ornate build. A
JEWEL EYE set in the sternum, wide open, uncovered and centred. Several strands
of TINY BELLS around the neck, clearly individual small bells, resting on the
chest without covering the jewel eye. Ornate ceremonial torso piece in magenta
#C24B8E with gold #F0C75E trim, open at the sternum; structured skirt-guard to
mid-thigh; wide open sleeve mouths at the forearms. Sandals or flat ornate
boots. NO moths, NO insects, NO particles, NO glow haze in frame.
```

- **FRENTE** — vista principal; olho-joia e sinos legíveis ao mesmo tempo.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica**: o saiote estruturado e as
  mangas abertas só informam volume de lado. Exigir que os sinos apareçam de
  perfil, pendurados, com espessura.
- **COSTAS** — o fecho do torso ornamentado e a queda do saiote.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The sternum jewel eye, isolated on plain dark grey: front and true 90-degree
   side. An open human eye with a faceted gem iris set in a gold bezel that
   meets skin. TWO versions: one with NO glow, one lit.
2) The bell strands, isolated: a cluster of tiny hollow metal bells on cord,
   front and side, each bell readable as a separate object.
3) An ETHER MOTH, isolated, three separate images (wings up / wings spread /
   wings down): geometric flat-planed wings, no fuzzy realistic moth body,
   violet #8A5CF0 and gold. Suitable as a sprite sheet reference.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
no bells, bells missing, closed chest, chest covered, robe over the sternum,
jewel eye on the forehead, jewel eye hidden, moths in frame, insects, butterfly
swarm, particles, glow haze, hearing aid, headphones, fur, armour, wizard hat,
staff, pale skin, light skin, tight sleeves, closed cuffs, third eye as a
tattoo or symbol instead of a real eye
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **OS SINOS EXISTEM** — é o defeito medido na arte de 24/08 e o motivo
   principal desta regeração. Sinos individuais, contáveis, no peito.
2. **Olho-joia no ESTERNO**, aberto, descoberto, com íris de gema — não na
   testa, não como tatuagem ou símbolo.
3. **Pele escura + pó dourado nas maçãs.**
4. **Mangas com boca aberta e larga** (as mariposas pousam ali) e **saiote
   estruturado**.
5. **Nenhuma mariposa nas quatro vistas de produção.**
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com o
   volume do saiote e a espessura dos colares visíveis.
7. Teste de 10% de tela: a 10% do tamanho, o olho do peito e o magenta ainda o
   identificam?
