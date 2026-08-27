# 07 — VITALIS, a Mão que Cura

> **Fila:** posição 4 (lote 2). **Custo:** ~30 créditos (+~30 se a Lúmen for
> gerada em 3D pela Meshy — decisão do Diretor).
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 7) · `personagens/07-vitalis.md`.

## 0. Quem é

Humana, Guardião, Suporte / Vida. No afogamento do rio Claro segurou a mão da
irmã gêmea até o fim; a irmã não morreu — ficou **entre** os mundos. O que
restou dela deste lado é **Lúmen**, a pequena luz que nunca se afasta.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,68 m.** Porte prático, nada heroico: uma curandeira de campo.
- **As marcas do dia do rio, as duas:**
  1. **Uma mecha branca** no cabelo, nascida naquela noite.
  2. **Os dedos da mão esquerda permanentemente frios** — a mão que segurava.
     Por isso a **luva fina SÓ na mão esquerda**; a direita é nua. (Ela esconde
     o frio, não as pessoas.)
- **Mangas sempre arregaçadas** — é postura de personagem, não figurino.
- **Vestuário:** vestes de curandeira de campo em **branco e dourado**,
  **bandoleira com bolsas de poções** atravessando o tronco (arranjar de modo a
  **não** cobrir a luva nem a mão nua), **botas enlameadas** de quem atende
  onde precisa.
- **DOIS modelos:** Vitalis e **Lúmen**. Lúmen tem **silhueta de MENINA
  pequena**, não de inseto e não de vaga-lume — é a irmã gêmea, com **um laço
  de fita** no cabelo (o mesmo laço da foto de infância). Geração separada, §5.

## 2. Paleta

| Uso | Hex |
|---|---|
| Branco (dominante) | `#F5F2E8` |
| Dourado (secundária) | `#F0C75E` |
| Água (energia / cura) | `#2AA7FF` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A human field healer, 1.68 m, kneeling on churned ground beside a wounded
ally, sleeves rolled up to the elbow. One BRIGHT WHITE STREAK runs through her
hair. She wears a thin close-fitting glove on her LEFT HAND ONLY — the fingers
of that hand have been permanently cold since the night at the river; her
right hand is bare. White and gold field-healer robes, a bandolier of potion
pouches across her chest, mud-caked boots. Beside her floats LÚMEN: a small
figure of light the size of a child, clearly shaped like a LITTLE GIRL — arms,
legs, head, a ribbon bow in her hair — not an insect, not a fairy with wings,
not a firefly. Golden watercolour light blooms on the ground around them.
Palette #F5F2E8, #F0C75E, #2AA7FF.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Lúmen NÃO entra nas vistas da Vitalis** — uma figura por imagem, sempre. A
fada tem as vistas dela em §5.

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Human woman, 1.68 m, practical build, standing alone. Sleeves rolled up above
the elbows on BOTH arms. One bright white streak in her hair. A thin fitted
glove on the LEFT HAND ONLY; the right hand completely bare. White and gold
field-healer robes #F5F2E8 with #F0C75E trim, a bandolier of potion pouches
worn so that it does NOT cross in front of either hand, mud-caked flat boots.
No fairy, no companion, no floating light in frame. Palette #F5F2E8, #F0C75E,
#2AA7FF.
```

- **FRENTE** — vista principal; a assimetria das mãos precisa estar óbvia.
- **PERFIL ESQUERDO 90° VERDADEIRO** — é o lado da luva; exigir que a
  bandoleira apareça de canto (a espessura dela é informação de modelo).
- **COSTAS** — a queda das vestes e o fecho da bandoleira.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peça-assinatura isolada — LÚMEN (geração extra, 2D, própria)

Lúmen é modelo próprio (~400 vértices + glow, `personagens/07-vitalis.md`).
Gerar as **três** vistas dela em imagens separadas, mesmo enquadramento do
BLOCO B:

```
A small girl made of light, about the size of a child's doll, floating.
Complete HUMAN silhouette: head, arms, hands, legs, feet, and a RIBBON BOW in
her hair. Simple soft features, twin sister of the healer. She is luminous
white-gold, translucent at the edges. NOT an insect, NOT a winged fairy, NOT a
ball of light, NOT a firefly, NO wings at all. Plain dark grey background.
Front view / TRUE 90-degree left side view / back view — three separate images.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
gloves on both hands, bare both hands, glove on the right hand, sleeves down,
long sleeves, clean boots, armour, heavy robe, wizard hat, staff, halo, wings,
insect fairy, winged fairy, firefly, ball of light instead of a girl, ribbon
missing, companion in the character views, holy cross, church iconography,
nun habit, blood, wounds shown
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Luva fina SÓ na mão esquerda**; mão direita nua. É a assinatura corporal
   dela e a única forma de o modelo herdar o detalhe.
2. **Mecha branca** visível nas quatro vistas.
3. **Mangas arregaçadas** nos dois braços.
4. **Bandoleira sem cruzar as mãos** — se cobrir a luva, reprovada.
5. **Botas enlameadas** (não novas, não polidas).
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito.
7. **Lúmen (§5): silhueta de MENINA, com laço, SEM asas.** A arte de 24/08 foi
   gerada com a ficha antiga; a ficha reformulada em 20/08 manda. Se sair
   inseto, vaga-lume ou bola de luz, reprovada.
8. Nenhuma iconografia religiosa — cura aqui é aquarela dourada e pétalas.
