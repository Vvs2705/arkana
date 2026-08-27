# 14 — GROMM, o Xamã da Tempestade

> **Fila:** posição 11 (lote 4). **Custo:** ~30 créditos (+~30 se o cajado-totem
> for gerado em 3D pela Meshy — decisão do Diretor).
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 14) · `personagens/14-gromm.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Orc, função **Suporte / Vida**. Era o mais novo da tribo na noite em que a
tempestade levou os mais velhos e deixou os feridos com ele. Desde então cura
primeiro e discute justiça depois. A chuva que ele chama apaga fogo — inclusive
o dos amigos, e ele avisa antes, todas as vezes. Fala devagar. Tem tempo.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 2,05 m** — **o maior humanoide do elenco** (só o Basalto, golem, é
  maior). É a altura que vai para o rigger.
- **Porte:** massivo, com **corcunda leve** de quem carrega o mundo — ombros à
  frente, pescoço baixo. Silhueta curvada e larga.
- **Pele verde-oliva** com **pintura branca ritual**: **raios estilizados no
  peito e no rosto**. Padrão gráfico simples e grande, legível a 10% de tela.
- **Presas inferiores curtas** (orc, não monstro), **olhos amarelos**.
- **Crina negra com contas de osso e penas.**
- **Vestuário:** **torso nu** sob **colares de contas** e um **manto de pele de
  lobo jogado num ombro só** (assimetria obrigatória); **saiote de couro com
  placas de bronze**; **braceletes de tempestade** — anéis de cobre nos
  antebraços.
- **O CAJADO-TOTEM:** crânio de bisão coroado de raios. **Fora das quatro vistas
  de produção** (peça separada, §5) — cajado atravessando o corpo confunde o
  multi-view.

## 2. Paleta

| Uso | Hex |
|---|---|
| Verde-oliva (pele / dominante) | `#5C6B3C` |
| Céu de tempestade (secundária) | `#4A5A7A` |
| Raio (energia / acento) | `#F5D90A` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
An orc storm shaman, 2.05 m, massive, standing slightly hunched under a
breaking sky on the open steppe, golden-green rain falling in a cone around
him. Olive-green skin painted with WHITE RITUAL LIGHTNING marks across his
chest and face — simple, large, graphic shapes. Short lower tusks, yellow eyes,
a black mane threaded with bone beads and feathers. Bare torso under strands of
bead necklaces, a wolf-pelt cloak thrown over ONE shoulder only; a leather
skirt-guard studded with bronze plates; copper storm bracelets crackling with
sparks at his forearms. In one hand a TOTEM STAFF crowned with a bison skull
wreathed in lightning. Thunder rings on the ground at his feet. Palette
#5C6B3C, #4A5A7A, #F5D90A.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Sem o cajado, sem faíscas, sem chuva** nas vistas de produção.

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Orc man, 2.05 m tall, massive heavy build with a SLIGHT FORWARD HUNCH — the
shoulders carried ahead of the hips, the neck low. This stoop appears in every
view. Olive-green skin #5C6B3C with white ritual lightning paint on the chest
and face. Short lower tusks, yellow eyes, black mane with bone beads and
feathers. Bare torso under bead necklaces; a wolf pelt over the LEFT shoulder
only; leather skirt-guard with bronze plates; copper bracelets on both
forearms. Bare feet or simple hide wraps. NO staff, NO totem, NO sparks, NO
rain, NO particles in frame. Arms clear of the torso.
```

- **FRENTE** — vista principal; a pintura ritual do peito é o assunto.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica**: é a única vista que mede a
  **corcunda** e a profundidade do peitoral. Sem ela o modelo nasce ereto e
  perde o personagem. Atenção: o manto de lobo está no **ombro esquerdo**, então
  este perfil o mostra inteiro — exigir que ele **não** cubra a linha do peito.
- **COSTAS** — o manto, a crina e a curvatura da coluna.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) THE TOTEM STAFF, isolated on plain dark grey: front, TRUE 90-degree side,
   and a close view of the head — three separate images. A long dark wooden
   shaft bound with hide, crowned by a BISON SKULL with a ring of stylised
   lightning bolts around the horns, bone beads and feathers hanging from the
   binding.
2) The ritual paint: chest and face alone on plain dark grey, TWO versions —
   flat white pattern with NO glow, and lit.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
staff in frame, weapon in frame, upright military posture, straight back,
slim build, human proportions, upper tusks, huge tusks, monster face, boar
face, full armour, chest covered, shirt, cloak over both shoulders, symmetrical
cloak, rain, sparks, lightning bolts, particles, war banner, skull on the face,
green skin cartoon shade, gore, blood
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **2,05 m lido na proporção** — massivo, ombros largos, mãos grandes. Se sair
   com proporção humana média, reprovada.
2. **Corcunda leve nas quatro vistas** (é a silhueta dele).
3. **Manto de pele de lobo num ombro SÓ** — a assimetria é obrigatória.
4. **Pintura branca ritual de raios** no peito e no rosto, em formas grandes e
   simples (teste de 10% de tela).
5. **Presas inferiores curtas** — orc, não monstro; sem presas superiores.
6. **Braceletes de cobre nos dois antebraços** e **contas de osso na crina**.
7. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com a
   corcunda visível.
8. **Sem cajado nas vistas de produção**; o totem existe e está em §5.
