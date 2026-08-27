# Como o circulo fecha nos outros — e o que a ARKANA copia

> Estudo pedido pelo Diretor em 27/08/2026, para calibrar a tempestade contra
> quem ja resolveu o problema em vez de contra o nosso proprio chute.
> Fontes ao final. Tudo abaixo foi **lido**, nao lembrado.

---

## 1. As tres tabelas de verdade

### PUBG — 9 fases (Erangel)

| Fase | Espera | Fecha | Diametro final | Dano/s |
|---|---|---|---|---|
| 1 | 2:00 | 4:30 | 3.994 m | 0,4% da vida |
| 2 | 0 | 3:00 | 2.396 m | 0,6% |
| 3 | 0 | 2:10 | 1.318 m | 0,8% |
| 4 | 0 | 2:00 | 725 m | 1% |
| 5 | 0 | 1:40 | 362 m | 3% |
| 6 | 0 | 1:30 | 181 m | 5% |
| 7 | 0 | 1:10 | 90 m | 7% |
| 8 | 0 | 1:00 | 45 m | 9% |
| 9 | 0:30 | 0:30 | **0 m** | 11% |

### Apex Legends — 6 aneis

| Anel | Espera | Fecha | Diametro final | Dano/tique |
|---|---|---|---|---|
| 1 | **1:15** | ~4:20 | ~1.200 m | 3 |
| 2 | 2:00 | 1:05 | 650 m | 4 |
| 3 | 1:30 | 0:45 | 400 m | 10 |
| 4 | 1:30 | 0:40 | 200 m | 15 |
| 5 | 1:15 | 0:50 | 100 m | 20 |
| 6 | 1:00 | 2:00 | **0,05 m** | 25 |

### Fortnite — 8 circulos

| Circulo | Espera | Fecha | Dano/s |
|---|---|---|---|
| 1 | 2:30 | 1:30 | 1 |
| 2 | 1:50 | 1:15 | 2 |
| 3 | 1:30 | 1:00 | 5 |
| 4 | 1:00 | 0:50 | 7 |
| 5 | 0:45 | 0:40 | 10 |
| 6 | 0:30 | 0:30 | 12 |
| 7 | 0:20 | 0:25 | 15 |
| Final | 0:10 | 0:20 | 20 |

---

## 2. As seis leis que os tres compartilham

**Lei 1 — A primeira janela e longa e o mapa esta inteiro aberto.**
Apex 1:15, PUBG 2:00, Fortnite 2:30. Nenhum dos tres liga a tempestade enquanto
alguem ainda esta caindo. E exatamente a ordem do Diretor: *"na queda nao temos
limite"*. A ARKANA fazia o contrario — a zona nascia contando no `_ready`,
antes do pouso.

**Lei 2 — A janela ENCOLHE, o fechamento tambem.**
Fortnite e o caso mais limpo: 2:30 -> 1:50 -> 1:30 -> 1:00 -> 45 -> 30 -> 20 -> 10.
A partida **acelera**. Janela constante daria dois ritmos: um comeco lento e um
fim lento. O Diretor deu 1:10 -> 1:00 -> 50 -> 40 -> 30, que e a mesma curva.

**Lei 3 — O dano cresce de forma brutal, nao linear.**
PUBG vai de 0,4% a 11% da vida por segundo (**27x**). Apex, 3 -> 25 (8x).
Fortnite, 1 -> 20 (20x). A razao e economica: no comeco, ficar fora tem que ser
uma **decisao carissima porem possivel** (da para atravessar por um loot); no
fim tem que ser **suicidio**. Dano linear faz a zona ser sugestao ate o fim.
Vale registrar que os tres medem em **% da vida maxima**, nao em pontos — o que
faz a tempestade escalar sozinha se a vida do personagem mudar.

**Lei 4 — O circulo novo esta SEMPRE contido no anterior.**
> *"After the first circle every following circle change will happen entirely
> inside the new play zone."*

A implementacao canonica e a mesma em todos: calcula-se a folga
(`raio_velho - raio_novo`) e sorteia-se o centro novo dentro dela. A propria
documentacao do Zone Wars da Epic da a regra em forma de knob: **o deslocamento
maximo nao pode passar de 1/2 do raio anterior.**
Sem essa lei existiria ponto seguro AGORA que fica fora depois de a parede
andar — e planejar rotacao viraria sorte.

**Lei 5 — O centro e sorteado por AREA e o terreno e filtrado.**
A literatura de patente da mecanica e explicita nos dois pontos: a
probabilidade de o centro cair na regiao central e na periferia e **a mesma**
(ou seja, uniforme por area, o que exige `sqrt(random)` e nao `random`), e o
sorteio **evita terreno ruim para combate — mar, montanha, penhasco**.

**Lei 6 — A ultima fase fecha em ZERO.**
PUBG fase 9 -> 0 m. Apex anel 6 -> 0,05 m. Fortnite final -> espaco de 1 jogador.
Nenhum dos tres permite que a partida acabe por cronometro com dois vivos.
Esse e o unico final que o genero nao aceita, e e o que o Diretor pediu:
*"ate todo o mapa ser tomado pela tempestade de dano para forcar finalizar a
batalha"*.

---

## 3. O que a ARKANA tinha errado — e o que mudou

| | Antes de 27/08 | Depois | Lei violada |
|---|---|---|---|
| Inicio | zona contando no `_ready`, durante a queda | **inerte** ate o pouso; entao 1:10 de mapa aberto | 1 |
| Janelas | 25, 20, 18, 15, 10 s (total 165 s) | **70 · 60 · 50 · 40 · 30 · 15** | 2 |
| Seed | **FIXO em 2707** — os mesmos 5 circulos em toda partida que o jogo ja rodou | sorteado por partida | — |
| Raios | metros calibrados contra uma "regua" de 180 m | **fracao do raio do mapa** | — |
| Ultima fase | para em 4 m: existia refugio permanente | **fecha em 0** | 6 |
| Contencao | ja correta | mantida | 4 |
| Sorteio por area | ja correto (`sqrt(randf())`) | mantido | 5 |
| Filtro de terreno | X — o teto de altura de 8,5 m proibia plato e pico | `Island.pode_pousar()` | 5 |
| Dano | 1,5 -> 18 dps (12x) | 1,5 -> **26** dps (17x) | 3 |

### O seed fixo era o defeito mais grave

Nao e desequilibrio, e o fim do genero. Com `SEED_ZONA := 2707` cravado,
**decorar o mapa uma vez ganhava para sempre**: o quinto circulo caia no mesmo
lugar em toda partida da historia do jogo. A Respawn descreve o proprio trabalho
no anel do Apex como *"alteracoes para reduzir a previsibilidade"*.

O determinismo nao se perdeu — **mudou de escopo**. Era determinismo *entre*
partidas; agora e *dentro* da partida: um seed gera o plano inteiro de uma vez,
no instante da criacao. E o unico escopo de que o teste e a futura rede
precisam (o servidor sorteia, transmite o numero, todos desenham os mesmos
circulos sem trocar uma coordenada). O seed fica guardado em
`Zona.seed_da_partida` justamente para isso.

---

## 4. A tabela da ARKANA, e por que cada numero e esse

```
ABERTURA_S = 70   # 1:10 — ordem do Diretor; Apex usa 1:15
FORMACAO_S = 10   # a parede vem de fora e para na costa ("ele para")
```

| Fase | Espera | Fecha | Raio (fracao do mapa) | dps |
|---|---|---|---|---|
| 1 | 60 s | 30 s | 0,62 | 1,5 |
| 2 | 50 s | 26 s | 0,42 | 3 |
| 3 | 40 s | 22 s | 0,26 | 6 |
| 4 | 30 s | 18 s | 0,13 | 11 |
| 5 (colapso) | 15 s | 25 s | **0,00** | 26 |

Total depois do pouso: **396 s (6:36)**. `Balance.MATCH.duration_s` foi de 180
para **480 s**, deixando 84 s para a queda do castelo, que corre no mesmo relogio.

**Razao de encolhimento:** 0,62 -> 0,42 -> 0,26 -> 0,13 e x0,68, x0,62, x0,50 —
a mesma curva do PUBG, que divide o diametro por 2 a partir da fase 5.

**Por que fracao e nao metro:** era metro com regua, e uma multiplicacao
consertava a diferenca. Duas verdades sobre o mesmo numero e como a zona ficou
com um terco do mapa quando a ilha cresceu em 26/08. Fracao tem uma verdade so:
**0,62 e 62% do mapa em 300 m e em 2.400 m.** Nada para envelhecer — que e o que
importa agora que o mapa vai a 2,4 km.

**dps final 26:** 26 x 25 s de colapso = 650 de dano contra 100 de vida. O
colapso mata mesmo quem entra nele com vida cheia e cura na mao.

---

## 5. O que os outros tem e a ARKANA (ainda) nao

1. **Dano em % da vida maxima**, nao em pontos. Hoje e equivalente (vida = 100
   fixa), mas no dia em que houver personagem com vida diferente, ou buff de
   vida, a tabela toda precisa ser relida. E uma linha de codigo e esta
   registrado como pendencia, nao como esquecimento.
2. **Dano que cresce com o tempo fora**, nao so com a fase. O PUBG passou a
   fazer isso no revamp da blue zone. Pune o atravessador cronico sem endurecer
   a primeira travessia — exatamente o que a lei 3 quer.
3. **Dano que cresce com a DISTANCIA ate a borda.** Tambem PUBG: *"each circle
   after the first does more damage the further outside the playzone you are"*.
   Transforma "voltar" numa decisao de gradiente em vez de binaria.
4. **Anel de recuperacao / consolo** (o "Ring Console" do Apex). Fora de escopo
   aqui, mas e o padrao para dar um segundo ato a quem foi pego pela zona.
5. **Sorteio que evita o mesmo local em partidas consecutivas.** O seed
   aleatorio ja resolve na media; um historico curto resolveria no pior caso.

---

## 6. O risco que este estudo NAO resolve

Um mapa maior com poucos jogadores fica vazio, e vazio e pior que lento —
**desempenho se otimiza, tedio nao**. Nas tres referencias a zona e o
compressor de um mapa com 60 a 100 jogadores. A ARKANA tem 1 + 6 bots. A
tabela nova aperta o mapa, mas o botao certo para o encontro acontecer e
**apertar a zona antes de somar bots**, e a medicao que importa e *tempo ate o
primeiro contato*. Esta no plano do mapa grande (`docs/cenario/MAPA-GRANDE-PLANO.md`).

---

## Fontes

- [The Playzone — PUBG Wiki](https://pubg.wiki.gg/wiki/The_Playzone) — tabela das 9 fases de Erangel
- [Dev Letter: Blue Zone Revamp — PUBG](https://pubg.com/en/news/10280) — dano crescente por tempo dentro da zona
- [The Ring — Apex Legends Wiki](https://apexlegends.wiki.gg/wiki/The_Ring) — tabela dos 6 aneis, colapso a 0,05 m, "reduce predictability"
- [Fortnite Zone Timer — FortniteTools](https://www.fortnitetools.com/tools/zone-timer) — tabela dos 8 circulos
- [The Storm — Fortnite Wiki](https://fortnite.fandom.com/wiki/The_Storm)
- [Zone Wars in Fortnite Creative — Epic Developer Community](https://dev.epicgames.com/documentation/fortnite/zone-wars-in-fortnite-creative?lang=en-US) — a regra do deslocamento <= 1/2 do raio anterior
- [Understanding circles and ending locations — Steam Community](https://steamcommunity.com/sharedfiles/filedetails/?id=891913197) — a lei da contencao
- [Region adjustment method and apparatus — USPTO 12017144](https://image-ppubs.uspto.gov/dirsearch-public/print/downloadPdf/12017144) — sorteio uniforme por area e filtro de terreno (mar, montanha, penhasco)
- [Battle Royale Game — Rishi Khanna](https://rishikhanna.dev/pages/portfolio/battle_royale.html) — a implementacao canonica do sorteio pela folga de raio
