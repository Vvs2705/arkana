# 09 — VEX, o Alquimista da Peste

> **Fila:** posição 6 (lote 2). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 9) · `personagens/09-vex.md`.

## 0. Quem é

Humano, Dominador, Curta distância / área. **Não tem pulmões de carne**: perdeu
os dois na explosão da Grande Obra que devia purificar o ar de uma mina
soterrada. O que respira por ele é um **fole alquímico de latão** no peito, num
ritmo que nunca acelera — ele não pode ofegar, não pode correr, não pode ter
pressa. A auditoria de 25/08 apontou Vex como uma das **duas peças mais fiéis à
ficha** no lote antigo; o que falta é a vista certa.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,90 m.** O mais alto do elenco de lançamento.
- **Porte:** pesado, ombros caídos, movimentos econômicos. Silhueta de peso —
  larga na cintura e nos ombros, nunca atlética.
- **A marca mágica — o centro visual do personagem:** o **FOLE ALQUÍMICO DE
  LATÃO embutido no peito**, visível por fora, com **juntas de cobre**, subindo
  e descendo. É geometria própria com bone de animação — precisa estar
  **inteiro, desobstruído e legível nas quatro vistas**.
- **A máscara de bico com filtros borbulhantes não é figurino: é o pulmão
  externo.** Gerar a máscara **abaixada no peito ou pendurada no pescoço**, com
  o **rosto à mostra** — a ficha reformulada em 20/08 é explícita: **sem capuz e
  sem capacete fechado**. A máscara vira adereço separado.
- **Cabeça raspada**, com **marcas de queimadura química antigas atrás das
  orelhas**.
- **Vestuário:** **avental de couro pesado** sobre **colete de fivelas**;
  **frascos de vidro verde numerados no cinto — fora de ordem**, como o caderno
  dele; **luvas grossas de manuseio**.

## 2. Paleta

| Uso | Hex |
|---|---|
| Verde reagente (energia) | `#5A8A3C` |
| Latão (dominante do fole) | `#A8763E` |
| Vidro (secundária) | `#B8D8C0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A heavy human alchemist, 1.90 m, broad and slope-shouldered, standing still in
a bank of low green mist that he is exhaling. Set INTO his chest, visible from
outside, is a BRASS ALCHEMICAL BELLOWS with copper joints, rising and falling
at a rhythm that never quickens — it breathes for him, he has no lungs. A
beaked mask with bubbling filters hangs LOWERED on his chest, so his face is
fully visible: shaved head, old chemical burn scars behind the ears, economical
expression. Heavy leather apron over a buckled vest; numbered green glass vials
on his belt, the numbers OUT OF ORDER; thick handling gloves. Green mist in flat
cel bands, never realistic. Palette #5A8A3C, #A8763E, #B8D8C0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Heavy human man, 1.90 m, broad, slope-shouldered, thick waist, never athletic.
A BRASS BELLOWS with copper joints is set into his chest and fully exposed —
unobstructed, centred, readable. Beaked filter mask LOWERED onto the chest or
hanging at the neck, FACE FULLY VISIBLE, shaved head, old chemical burn scars
behind the ears, no hood, no closed helmet. Heavy leather apron over a buckled
vest; numbered green glass vials on the belt; thick handling gloves. No mist,
no smoke, no particles in frame. Palette #5A8A3C, #A8763E, #B8D8C0.
```

- **FRENTE** — vista principal; o fole é o assunto da imagem.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica para ele**: é a única vista
  que informa **quanto o fole avança à frente do peito** e a espessura do
  avental de couro. Sem ela, o fole nasce achatado contra o torso.
- **COSTAS** — o amarrio do avental e a curva dos ombros caídos.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The brass alchemical bellows, isolated on plain dark grey: front, TRUE
   90-degree side, and three-quarter — three separate images. Brass body with
   copper joints and a pleated leather concertina in the middle, sized to sit
   inside a man's chest cavity, with mounting flanges bolted to a ribcage.
2) The beaked filter mask with bubbling glass filters, isolated: front and true
   90-degree side.
3) One numbered green glass vial, isolated, held nothing — a small model of its
   own.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
closed helmet, full face mask, mask worn over the face, hood, plague doctor
hat, hair on the head, athletic build, slim waist, muscular hero body, bellows
hidden under clothing, chest covered, apron covering the chest, realistic
volumetric fog, mist hiding the body, particles, glass syringe, steampunk
goggles, biohazard symbol, rust, blood, wounds
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **O fole de latão no peito, exposto e inteiro, nas quatro vistas.** Se
   estiver coberto pelo avental ou pelo colete, reprovada — é o centro visual
   do personagem e o idle de assinatura dele no jogo.
2. **Rosto à mostra, máscara abaixada.** Sem capuz, sem capacete fechado.
3. **Cabeça raspada + queimaduras químicas atrás das orelhas.**
4. **Frascos verdes numerados fora de ordem** no cinto (o detalhe de
   personalidade — o caderno dele também é fora de ordem).
5. **Silhueta pesada de ombros caídos** — se sair atlético, reprovada.
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, e a
   projeção do fole visível de lado.
7. Névoa: **nenhuma** nas vistas de produção; na apresentação, em bandas cel,
   nunca realista.
