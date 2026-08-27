# 00 — FILA DE PRODUÇÃO DOS 18 MAGOS QUE FALTAM

> **Ordem do Diretor (27/08):** *"também já deixar criado as artes dos demais
> personagens"* — a fila abaixo é a arte pronta para gerar, para a esteira 3D
> não parar entre um veredito e outro.
>
> **Escopo:** 18 magos. **Pyra (01)** está gerada e riggada; **Brok (13)** está
> pronto e é a referência de clipes. Os dois ficam fora desta fila.
>
> **Onde os arquivos vivem:** aqui, `docs/prompts/personagens/`, por ordem do
> Diretor de 26/08 (`docs/prompts/00-LEIA.md`: *"todo prompt solicitado vive
> AQUI"*). Cada arquivo se chama `<id>-<slug>.md` para casar exatamente com a
> pasta do ateliê `personagens/<id>-<slug>/`, que é a chave de junção real do
> pipeline (`tools/meshy/meshy.py` lê por slug).

---

## 1. A fila (a ordem de `docs/design/DESBLOQUEIO-ELENCO.md` §4.2)

3 magos por lote, veredito do Diretor no viewer entre lotes. **~30 créditos por
mago** (multi-view no site; remesh, rig e animação são grátis no webapp).

| # | Lote | Id | Mago | Por que nesta posição | Crédito acum. |
|---|---|---|---|---|---|
| 1 | 1 | 03 | **Véu** | Kit IMPLEMENTADO (`Kits.gd`) — mago completo testa de verdade. | 30 |
| 2 | 1 | 10 | **Tessa** | Kit IMPLEMENTADO — o 3º e último kit pronto sem modelo. | 60 |
| 3 | 1 | 02 | **Ceifadora** | 2ª Vanguarda do lançamento; a ausência de sombra é feature de render grátis. | 90 |
| 4 | 2 | 07 | **Vitalis** | A curandeira: todo BR precisa do suporte de vida no dia 1. | 120 |
| 5 | 2 | 04 | **Corvus** | Silhueta curvada — quebra a fila de silhuetas eretas do elenco. | 150 |
| 6 | 2 | 09 | **Vex** | Silhueta pesada + fole no peito: 3ª leitura distinta do lote. | 180 |
| 7 | 3 | 05 | **Corvomante** | Fecha os Videntes; o corvo é peça-assinatura separada. | 210 |
| 8 | 3 | 06 | **Olho-de-Éter** | 2º Vidente — counterplay do meta de magia. | 240 |
| 9 | 3 | 08 | **Ilusionista** | Fecha o elenco de LANÇAMENTO (ids 1–10 completos). | 270 |
| 10 | 4 | 12 | **Umbra** | 1ª leva de desbloqueáveis: perseguição, silhueta ágil. | 300 |
| 11 | 4 | 14 | **Gromm** | O maior humanoide (2,05 m) — testa o rig alto antes dos extremos. | 330 |
| 12 | 4 | 19 | **Noctus** | Vampiro arcano: casaca de gola alta, silhueta vertical elegante. | 360 |
| 13 | 5 | 11 | **Aelion** | 2ª leva: arco longo + aljava, adereço rígido nas costas. | 390 |
| 14 | 5 | 15 | **Maris** | Vestido-armadura é 60% do modelo — pesa no skinning, não no rig. | 420 |
| 15 | 5 | 17 | **Sylva** | Cabelo-estação trocável por material: gerar só o estado FLORIDO. | 450 |
| 16 | 6 | 16 | **Fizz** | 0,95 m — escala extrema, deixada por último de propósito. | 480 |
| 17 | 6 | 18 | **Basalto** | 2,30 m, golem sem boca: o pior caso do auto-rig humanoide. | 510 |
| 18 | 6 | 20 | **Pip** | 0,60 m e NUNCA pousa: retarget de clipe é o maior risco do elenco. | 540 |

**Por que os 3 de escala extrema (16, 18, 20) ficam no fim:** são os que mais
desafiam rig e retarget de clipe — a lição da patinação do anão (pendência 1.1
de `docs/PROJETO.md`). Quando chegarem, a biblioteca de clipes já terá sido
validada em 15 corpos de proporção normal.

### Custo

| Item | Créditos |
|---|---|
| 18 magos × ~30 (multi-view no site) | ~540 |
| Margem de regeração (~30% medido: a Luva de Conjurador precisou de v2) | ~160 |
| **Total estimado** | **~700** |
| Saldo (depois dos 30 da Pyra recriada) | **~2.964** |
| **Sobra projetada** | **~2.264** |

⚠️ **Não incluído:** as peças-assinatura que são MODELO PRÓPRIO — o corvo do
Corvomante, a Lúmen da Vitalis, a forma de lobisomem do Corvus, o cajado-totem
do Gromm, a torreta/bobina do Fizz. Em 2D são geração de imagem (fora do
crédito Meshy); se o Diretor quiser cada uma em 3D pela Meshy, some ~30
créditos por peça. **Decisão dele.**

---

## 2. O que cada arquivo traz

`<id>-<slug>.md`, todos com a mesma estrutura:

1. **Ficha física em metros** — altura, porte, silhueta.
2. **Paleta** em hex.
3. **Prompt mestre** (inglês) — a imagem de apresentação, para o Diretor
   julgar clima. Pode ter cenário.
4. **As 4 vistas SEPARADAS** — frente, perfil ESQUERDO de 90° verdadeiro,
   costas, três-quartos real. Fundo neutro, uma figura por imagem.
5. **Peça-assinatura isolada** — a geração extra que protege o adereço.
6. **Negative prompt** — BLOCO C de `00-REGRA-DAS-VISTAS.md` + os extras do
   mago.
7. **Critérios de aprovação** — objetivos, verificáveis a olho.

**A lição do ângulo não está repetida em 18 arquivos:** vive uma vez em
[`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md), com os três blocos
(âncora de estilo, enquadramento de vista, negative base) que cada arquivo
manda colar.

---

## 3. A direção visual escolhida para os 18 — e por quê

**Escolhida: ESCULTURA 3D — "stylized premium"**, exatamente como está descrita
em `docs/pipeline-arte/PERSONAGENS/00-DIRECAO_VISUAL_PERSONAGENS.md`. É o
BLOCO A de `00-REGRA-DAS-VISTAS.md`, e é a única linguagem aplicada nos 18.

Por quê, com a evidência:

A auditoria de 25/08 mediu que o elenco fala **três** linguagens visuais — 7 em
escultura 3D (04, 09, 13, 14, 16, 17, 18), 7 em pintura semi-realista (01, 03,
05, 06, 07, 10, 12) e 6 em anime (02, 08, 11, 15, 19, 20). Escolher uma é o
que acaba com isso. E a escolha não é arbitrária: **o Brok (13), que o Diretor
aprovou, está no grupo de escultura; a Pyra (01), que ele reprovou no olho
("longe de ficar boa"), estava no grupo de pintura.** A aprovação e a
reprovação apontam para o mesmo lado.

O que a auditoria também mediu, e que **inverte a intuição**: o eixo não é
realismo — o rosto do Brok aprovado é **mais** realista e enrugado que o da
Pyra reprovada. O que separa de verdade os grupos é (1) superfície esculpida ×
pintada, (2) sombra de contato — os 7 do grupo alvo pisam num chão, os outros
13 flutuam — e (3) proporção exagerada em mãos e botas.

**O perigo, registrado em `docs/ART.md` e obedecido aqui:** instruir o gerador
com *"menos realismo"* empurra o resultado para **anime**, que é justamente o
grupo que mais quebra o elenco. Por isso nenhum prompt desta pasta contém a
palavra "realism" em pedido negativo solto: o BLOCO A pede **superfície
esculpida, plano legível, sombra de contato e proporção exagerada** — o que o
Diretor aprovou — e o BLOCO C lista `anime, manga, manhwa, chibi, cel-shaded
outlines` como negativo explícito.

⚠️ **Ressalva honesta:** `docs/ART.md` registra que a direção de arte dos
personagens está **PENDENTE DE DECISÃO do Diretor**, e que o documento mestre
da direção aprovada por ele **não chegou ao repositório**. A escolha acima é a
melhor inferência a partir do que está medido (Brok aprovado, Pyra reprovada) e
do único documento de direção que existe aqui — que é **proposta da equipe**,
não norma. Se o Diretor entregar o documento mestre e ele disser outra coisa,
troca-se o BLOCO A em um arquivo só e os 18 seguem coerentes.

---

## 4. Fontes de cada campo (nada foi inventado)

| Campo | Vem de |
|---|---|
| nome, título, raça, função, história, kit, limitadores | `godot/menu/Elenco.gd` |
| **altura em metros**, aparência física, vestuário, VFX, paleta, notas de 3D | `personagens/<id>-<slug>.md` |
| ordem de produção e custo | `docs/design/DESBLOQUEIO-ELENCO.md` §4 |
| regras de vista e de crédito | `docs/MESHY.md` §3, `docs/PROJETO.md` 1.2 |
| direção visual | `docs/pipeline-arte/PERSONAGENS/00-DIRECAO_VISUAL_PERSONAGENS.md` |

⚠️ **Correção de premissa:** a altura **não** está no `Elenco.gd` — aquela
ficha não tem campo de altura (o contrato dela são 13 campos, listados em
`Elenco.CAMPOS`). A altura de cada mago vive na primeira linha de "Aparência
física" de `personagens/<id>-<slug>.md`, e é de lá que `tools/meshy/meshy.py`
já a lê para passar ao rigger (`docs/MESHY.md` §3: *"lê a altura na ficha do
personagem e passa ao rigger"*).

⚠️ **Os magos 11–20 têm `classe`, `passiva`, `tatica`, `suprema` e
`limitadores` VAZIOS no `Elenco.gd`** — de propósito: são proposta da equipe
até o Diretor aprovar. Os arquivos deles dizem isso em vez de preencher, e
nenhum prompt desenha habilidade não aprovada.

---

## 5. Divergências arte × ficha que estes prompts corrigem

A auditoria de 25/08 mediu que a arte de 24/08 diverge da ficha em cinco
pontos. **A ficha é a fonte da verdade; onde divergem, a arte está errada.**
Cada um virou critério de aprovação no arquivo do mago:

| Mago | O que a arte errou | Onde está corrigido |
|---|---|---|
| 02 Ceifadora | kintsugi virou malha regular em vez de porcelana rachada | `02-ceifadora.md` §6 |
| 03 Véu | aparenta 25 anos; a ficha diz 19 sem ter envelhecido | `03-veu.md` §6 |
| 06 Olho-de-Éter | faltam os sinos (é assim que ele percebe o próprio andar) | `06-olho-de-eter.md` §6 |
| 12 Umbra | faltam as lâminas gêmeas | `12-umbra.md` §6 |
| 15 Maris | olhos com esclera branca; deveriam ser verde-mar inteiros | `15-maris.md` §6 |

Mais fiéis à ficha no lote antigo: Tessa e Vex.
