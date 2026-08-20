# CHANGELOG — Arkana

Cada versão do protótipo documentada (regra do GDD, seção 15).

## v1.3.0-3d — 2026-08-20 — R19: o mundo que reage, o jogo que soa e o G2 fechado

Quatro raias paralelas + integração. O G2 do ROADMAP_3D ("queimar a floresta
abre caminho num APK") está cumprido.

**TERRENO REATIVO (o pilar, GDD §14)** — grade lógica 60×60 (célula 3 m) sobre a
ilha, e as 6 reações: fogo acende a floresta e propaga por ORÇAMENTO (1 rolagem
por aresta, `fuel_budget` por frente — nunca chance por tique; o selftest
REINTRODUZ o modelo reprovado e prova 220 células acesas contra teto de 25);
água apaga (sem carbonizar) e congela o lago em plataforma com colisão real que
derrete; raio eletrifica só a água conectada (flood 4-vizinhos, gelo isola);
terra ergue muro destrutível que NÃO nasce em célula ocupada (2º vermelho
provado: sem a guarda, o muro nasce em cima do corpo); vento espalha fogo
pagando do MESMO orçamento. Árvore queimada vira carvão permanente: copa some,
tronco deixa de colidir — queimar a floresta ABRE CAMINHO e linha de tiro.
63 checagens verdes. Custo ocioso: +2 draw calls; 1 luz por frente de fogo,
nunca por célula.

**DANO AMBIENTAL** — costura no tique único que player e bot compartilham
(`Pawn.move_velocity`): `TerrainSystem.dps_at()` publica o perigo (queimando =
`burn_dps`, eletrizado = `electrify_dps`, 0 se o sistema nem montou) e SÓ
`Combat.deal` aplica — o terreno nunca toca vida, contrato que segurou 8 fases
no Roblox.

**ELEMENTOS COMPLETOS (5/5)** — Terra (pedra pesada, lenta, forte) e Vento
(crescente rápido, leve) entram com cor+FORMA distintas (GDD §10) e números de
`Balance`. TODO impacto de projétil emite `Bus.terrain_hit` — qualquer magia
conversa com o mundo. 82 checagens; vermelho provado removendo o emit.

**ÁUDIO (25 checagens)** — 23 sons 100% procedurais (PCM sintetizado em
GDScript, zero binário no repo): disparo por elemento, impacto, dano, esquiva,
UI, contagem, vitória/derrota, ambiente. Buses Sfx/-8dB e Ambient/-18dB.
Áudio OBSERVA o Bus e nunca decide jogo. Só o player emite `spell_cast` por
ora — 6 bots sem atenuação por distância seria cacofonia (decisão registrada).

**JUICE (27 checagens)** — contagem 3·2·1·LUTE!, kill feed, flash de dano
(vermelho provado: flash com vida SUBINDO fica vermelho), +1 no abate, shake
curto, sting de fim. Instanciado defensivamente no boot do gameplay.

Gates: import limpo · 6 selftests verdes (gameplay 82 · characters · menu 13 ·
audio 25 · juice 27 · terreno 63) · boot 300 frames do menu E da arena sem
erro · APK 26 MB com as 4 raias verificadas DENTRO do pacote (lição da R17:
listar o conteúdo, nunca confiar no timestamp). Nenhum número de `Balance`
mudou fora do bloco `TERRAIN` que a fase introduziu.

## v1.2.0-3d — 2026-08-20 — R18: gráficos, mecânicas, a porta de entrada e a pasta técnica

Quatro frentes numa fase, pelas ordens do Diretor pós-teste em aparelho.

**GRÁFICOS** — sol com halo e nuvens estilizadas, glow nos emissivos (as magias
brilham de graça), ~3.000 tufos de grama ao vento em 1 draw call (colapsam além
de 60m — LOD no vertex shader), flores nos POIs, vagalumes ao entardecer, névoa
baixa no alagado, água com reflexo dourado e espuma na margem. Draw calls +27%
(régua: não dobrar). Contorno e vinheta RECUSADOS com medição: cada um custaria
um passe de tela inteiro no mobile.

**MECÂNICAS** — carrossel de elementos (Fogo esfera · Água crescente com arco ·
Raio dardo serrilhado, o mais rápido), cada um com cor + FORMA (GDD §10) e
custo/cadência de `Balance`; esquiva com i-frames e cooldown desenhado no botão;
knockback leve; bots com elemento fixo e esquiva ocasional; disparo contínuo da
R17 valendo para os 3. `move_velocity()` virou o caminho ÚNICO da velocidade
(produto único + dash + knockback). Selftest: 63 checagens, i-frames provados em
vermelho.

**APRESENTAÇÃO** — o APK deixa de abrir na partida: tela título (Selo vivo com 5
gemas pulsando, wordmark ARKANA, "toque para começar"), menu JOGAR / PERSONAGENS
/ SAIR, vitrine dos 20 personagens com silhuetas e "EM BREVE" nos 11–20.
**A arte do Diretor entra sozinha**: caiu `res://menu/art/NN.png`, o card troca.
Fim de partida ganhou botão MENU (costura do coordenador). Selftest 13/13.

**INFRA** — pasta `infra/` (7 arquivos): servidores de jogo (Godot dedicated
server; recomendação G5 = 1 VPS em São Paulo ~US$6–12/mês ou Oracle free tier;
provedores sem região BR descartados — 120–200ms mata jogo de tiro), backend
(login anônimo, sem rastreamento, anti-cheat honesto), pipeline, distribuição,
custos por fase (**R$ 0 até o G5**) e ferramentas locais. Preço mutável marcado
"verificar vigente"; contratar é ato do Diretor.

Gates: import limpo · boot do menu e da partida sem erro · selftests 3/3 verdes
(63 + 13 + 12 checagens) · APK 26M com menu, carrossel e grama DENTRO do pacote.
As 4 raias caíram por limite de sessão no meio e RETOMARAM do ponto exato sem
perder nada (transcript preservado + git status antes de reescrever).

## v1.1.0-3d — 2026-08-19 — R17: disparo contínuo aprovado no dedo, e o elenco de 20

**O Diretor aprovou o APK 3D no aparelho** ("ficou ótimo") e pediu dois ajustes.

### O botão de Fogo virou gatilho de verdade
Clicar **dispara na hora**; **segurar mantém disparando** por cadência enquanto
houver mana; arrastar direciona a magia sem parar o fogo; soltar para. O disparo
saiu do "soltar" e foi para o apertar + repetição — e o X de cancelamento morreu
junto com o disparo-no-soltar (não há mais o que cancelar; o anel aceso = disparando).
A mana continua sendo barrada só no Player (autoridade única de custo — a máquina
do gesto PEDE, nunca decide). Selftest reescrito: 12 verificações do novo fluxo,
incluindo "um disparo por cadência, nunca um por frame".

### `personagens/` — o elenco completo, um arquivo por mago
- **01–10**: o elenco de LANÇAMENTO do GDD §3 (base Apex), ficha completa + espaço
  para a arte do Diretor (`personagens/NN-slug/arte/`).
- **11–20**: o elenco de TEMPORADAS (ordem do Diretor): raças fantásticas — Alto
  Elfo sniper, Drow assassina, Anão de runas, Orc xamã, Nereida, Gnomo artífice,
  Dríade, Golem rúnico, Vampiro arcano (drena ÉTER, não sangue — 10+), Fada da
  tempestade. Cada um com aparência física, vestuário, personalidade, kit proposto
  com ⚖️ limitadores, VFX de assinatura, paleta e notas de modelagem. Funções na
  taxonomia do Diretor: Suporte/Vida · Ataque/Perseguição · Longa · Curta.
- Kits 11–20 são **proposta** até o Diretor aprovar; aprovados, sobem ao GDD.

### Pipeline
Corrida de flush do Windows cercada no `build_apk.sh`: o primeiro `unzip` logo
após o export podia ler o zip vazio e reprovar um APK íntegro — agora retenta.

Gates: import limpo · boot 300 frames sem erro · selftests verdes · APK compilado
com o auto-fogo dentro. Roblox intacto.

## v1.0.0-3d — 2026-08-19 — R16: o pivô — Arkana 3D no Android, régua Spellbreak

**Ordem do Diretor:** abandonar o roadmap incremental, descontinuar o 2D (Roblox
mantido) e ir direto ao 3D — *"o próximo APK que eu testar quero ver realidade de
jogo, não mapa de teste"*. Plano novo em `docs/ROADMAP_3D.md` (G0–G6); motor
**Godot 4.4.1** (a saída que o GDD §19.4 já previa), projeto em `godot/`.

**Da ordem ao APK jogável: uma sessão.** G0 e G1 fechados juntos:

- **Mundo** (`world/`): ilha 180×180m procedural com os 4 POIs do GDD em 3D —
  floresta (94 árvores), lago com água animada, ruínas, baixada alagada. Céu de
  entardecer, cel-shading 3 bandas + rim dourado, cores por vértice na paleta §10.
  30.811 verts / 9 draw calls — folga para 60fps em aparelho médio.
- **Personagem** (`characters/`): mago low-poly de manto azul-noite com debrum
  dourado, capuz aberto com dois olhos dourados no vazio, gema emissiva. 2.058
  verts. Animações idle/run/cast em código, disparo sincronizado com a mão
  (t=0,22s < fire_rate 0,27s). `set_tint` dá identidade aos bots.
- **Gameplay** (`gameplay/` + `ui/`): 3ª pessoa sobre o ombro (SpringArm com
  colisão), joystick + **gesto único do GDD §19.3** (anel = dispara, X = cancela,
  deadzone em dp), Fogo com tempo de viagem + números de dano + VFX, 6 bots
  (vagar→perseguir→atacar), **partida de 3 min com vitória/derrota e replay
  limpo**. Dano num ponto único (`Combat.gd`, NaN barrado com `not (x > 0)`);
  velocidade é produto único. Selftests: personagem 12/12 · gameplay 32/32, com
  guarda provada em vermelho.
- **Pipeline** (`export/`): `build_apk.sh` → APK assinado (debug) em segundos,
  sem gradle. Armadilha achada e cercada: sem
  `textures/vram_compression/import_etc2_astc=true` o export Android **falha em
  silêncio** no 4.4.1 — a linha entrou no `project.godot` e o script falha cedo
  apontando para ela se alguém a remover.

**Padrões que atravessaram** (via `docs/PONTE.md`, só o PROVADO): dano num ponto
só · NaN · velocidade-produto · dp para o dedo · cancelar de 1ª classe · clima com
COR · fiação defensiva (cada cena boota sem as outras).

APK integrado: `godot/build/arkana3d.apk` (25,8 MB), as três raias verificadas
DENTRO do pacote. Roblox intacto (51/51). 2D descontinuado — nenhuma manutenção.

## v0.9.0 — 2026-08-19 — R15: o pedido do primeiro playtest em aparelho

**O Diretor instalou o APK e jogou** — primeiro teste em aparelho real desde a
reprovação de 17/08. O veredito dele: magias OK (números de dano visíveis),
movimentação "polida", e **o seletor de magia funcionando** — que era exatamente o
defeito reprovado em 17/08; o conserto da R12 está confirmado em aparelho, por ele.
Dois pedidos saíram do teste, e esta fase os entrega.

### O botão de disparo virou a mira (pedido textual do Diretor)

> *"o botão de disparo tem dupla funcionalidade: movimenta a direção da magia e
> aciona o disparo"* — é o gesto único do GDD §19.3, implementado no 2D.

Dedo desce no botão → arrastar mira como joystick (o cajado do mago acompanha o
dedo) → **soltar dispara**. Toque curto = tiro rápido na última direção. **Voltar ao
centro cancela**, e o cancelar é estado de primeira classe: anel branco = soltar
dispara; **X vermelho** = soltar cancela (cor + FORMA, GDD §10). Tática segue o mesmo
padrão; esquiva fica toque simples (a direção dela já vem do movimento, e é botão de
pânico — arrasto só atrasaria a reação).

**Medido no jogo rodando:** 8 arrastos em 8 direções = 8 projéteis com erro angular
máximo de **0,41°**; cancelamento = **0 projéteis**; re-armar depois de cancelar
dispara exato. Provado por defeito reintroduzido (a guarda do cancelamento ficou
vermelha sozinha).

O esquema clássico **não morreu**: vive atrás de `gesture: 'legacy'` no menu, e saves
antigos migram sem perder o esquema de assistência. Knobs de playtest declarados:
`tapMaxMs: 220` (mais folgado que o 0,18s do Roblox — WebView soma latência) e
`aimDeadzoneDp: 14`, calibráveis **sempre juntos**.

### A barra de status por cima da vida — e por que o documento mentia

`docs/BUILD_ANDROID.md` dizia "tela cheia" porque o tema tinha `windowFullscreen=true`
— **flag legada que o Android ignora a partir do targetSdk 35** (o projeto usa 36).
O app já foi tela cheia; uma atualização de targetSdk o quebrou em silêncio, e a
barra de status passou a cobrir o HUD.

Conserto sem plugin novo: imersivo canônico na `MainActivity` (reaplicado ao voltar
de outro app; swipe da borda mostra a barra translúcida por ~3s sem empurrar o
layout), `windowLayoutInDisplayCutoutMode=shortEdges` (o recorte de câmera cai na
faixa preta do letterbox, nunca sobre o jogo) e **margem de área segura no HUD**
(painel vital 34→48px do topo; minimapa com folga do canto, que o Diretor notou
colado). Provado no APK compilado: flags no tema extraído via aapt2, código imersivo
no dex.

Gates: typecheck limpo · os dois portões do build 2D verdes · APK compilado com o
gesto e as flags DENTRO do pacote · harness Roblox **51/51** (nada tocado lá).
**Nenhum número de `Balance` do Roblox tocado.**

## v0.8.0 — 2026-08-19 — R14: os alvos que sumiam, e a pergunta do celular com resposta

### O 2D: girar o aparelho tornava os controles inoperantes

Medido com 15 toques sintéticos passando pelo hit-test do próprio Phaser:

| | acertos | menor alvo |
|---|---|---|
| tela original | 15/15 | 48,0dp |
| **depois de girar para 375 CSS** | **0/15** | **14,1dp** |
| depois do conserto, em 5 resoluções | **15/15 em todas** | 48,0dp |

**Zero de quinze**, sem sinal visível nenhum. Nada em `src/**` reagia a
`Phaser.Scale.Events.RESIZE` — `minHitRadius()` era calculado uma vez no `build()`.
A autoridade virou `TouchControls.refresh()`, método único e idempotente, com três
gatilhos chamando o **mesmo** código.

**Armadilha que teria feito o conserto falhar em silêncio:** chamar `setInteractive()`
de novo **não troca a área** de um objeto que já tem input — o Phaser reaproveita o
`InteractiveObject`. O recálculo seria ignorado calado.

**E um defeito grave fora da lista:** `CombatVfx.destroy()` estourava no encerramento
da cena (a câmera principal já não existe no SHUTDOWN). Consequência: **toda saída de
partida** abortava a limpeza no meio, e TouchControls, HUD, pool, bots, terreno e
player **nunca eram destruídos**.

Também fechados: mudança de controle na pausa não pegava (a Arena nunca é recriada), e
o destravamento de áudio era de **tiro único** — se o primeiro gesto falhasse, a
partida inteira saía muda.

### Os controles de toque eram a única tela sem acessibilidade

**As quatro opções de tamanho de UI davam o mesmo alvo de dedo.** Quem precisa de alvo
grande é exatamente quem foi mexer na opção, e não recebia nada. Agora uiSize 1,3 com
o texto ampliado do Roblox dá **83px** contra os 49px de antes.

A rota óbvia (`A11y.attach`) estava errada nos **dois** eixos: o `repaint` escreveria em
`UIStroke.Transparency`, que é onde essa tela guarda **estado** (anel armado, elemento
ativo) — em alto contraste todos acenderiam juntos; e o `attach` estampa um `UIScale`
por filho, que cresce sem mexer na posição, jogando os botões uns por cima dos outros.

De brinde: o nome do elemento no carrossel era **português cravado no código**.

### A pergunta do celular ganhou resposta

O relatório dizia *quanto* do toque usava o gesto — não se ele **funciona**. Agora:

```
gestos ARMADOS no toque ........ 41 (drag 6 + tap 1 + CANCELADOS 34)
   CANCELADOS: 83% dos gestos armados foram ABORTADOS
-- O GESTO UNICO RESOLVEU O CELULAR? (acerto por modo de entrada) ----
   drag (o gesto unico)     11 acertos /   20 disparos  55%
   key  (PC: a REGUA)       12 acertos /   20 disparos  60%
   RESPOSTA: SIM — o arrasto empata com o PC (-5 pp)
```

Três coisas que essa resposta exigiu:

1. **O `input` deixou de morrer no `spell_cast`** e passou a viajar no projétil até o
   `spell_hit`. `drag%` alto diz que usam o gesto, não que ele acerta.
2. **O PC entrou como grupo de controle.** Sem régua, "tão bom quanto" não tem contra o
   quê. `n` é a **menor** das duas pernas, nunca a soma.
3. **O CANCELAR virou dado.** Gesto armado e abortado não emitia nada: quem desiste
   cinco vezes por abate era idêntico a quem nunca hesita.

**Por que o cancelamento é um contador e não um 4º rótulo de `INPUT_KINDS`:** rótulo
pega carona no disparo, e cancelamento **não tem disparo** — precisaria de um
`spell_cast` falso, que entraria em `castsByElement`, o **denominador da dominância
elemental**. Medir o cancelamento assim **quebraria a V4**.

Quando o campo não chega, o relatório diz **`CEGO`**, nunca `0%`.

### A maratona passou a ver os módulos que faltavam

O S7 não carregava `Telemetry`, `Grimoire` e `Prefs` — justamente os de mais tabelas
por jogador. O **S8** os sobe atrás de um booleano (`boot(tag, { full = true })`), e não
num boot paralelo: *um boot copiado começa igual e diverge em silêncio, e no dia em que
alguém consertar um e esquecer o outro, S1–S6 medem um servidor que não existe.*
Provado com `diff` vazio contra as ferramentas anteriores.

Resultado com 12 passantes distintos: **nenhum vazamento**. A única tabela que cresce
(`sessHumans`) é o denominador que impede o acumulado de fingir conclusão.

Dois métodos que valem além deste caso:
- **A declaração virou asserção viva.** "Não dá para medir chave fraca porque o harness
  fixa toda Instance" era um comentário; agora é um teste que fica **vermelho** se o
  harness parar de fixar — obrigando a reler a declaração em vez de deixar um cenário
  medindo o coletor de lixo em silêncio.
- **Conexões se medem por DELTA**, não em absoluto: o número absoluto precisa de um
  contrato que ninguém tem; o delta não — doze pessoas entrando e saindo têm de deixar
  a conta como acharam. Medido: 9 conexões, constante.

### As decisões silenciosas foram reconciliadas no GDD

A auditoria da R13 achou nove casos em que o documento manda uma coisa e o código faz
outra. Como o GDD é a **única ponte** para o Android, quem implementar lá lê o
documento. Cinco foram corrigidas, incluindo as três de gravidade alta:

- **§14 ainda descrevia o modelo de fogo medido e REPROVADO** ("chance por tick": 30%
  por vizinho × 16 tiques = 99,67% acumulado, acendendo 380 de 380 células em 100% das
  rodadas). Quem implementasse o §14 no Android hoje **reimplementaria a carbonização**.
- **O anti-farm do §5, como escrito, deixava o exploit aberto** — o recorte era "aliados
  *sendo revividos*"; medido, uma dupla subia os dois escudos ao nível 4 atirando um no
  outro num canto.
- **A Runa de Eco não existe**; o solo é resolvido pareando com bot. Junto foi o aviso
  operacional: **número ímpar de humanos entrega o último a um bot, e essa dupla não
  responde V1**.

### `aimDeadzonePx` variava 7× conforme a tela
Estava em px de canvas: ~4dp de arrasto num telefone, ~28dp num monitor. Virou dp, pela
mesma função do piso de 48dp, com a regra escrita: *número que o **dedo** sente vive em
dp; número que o **mundo** sente vive em unidade de mundo.*

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (980 KB) · harness **51/51** ·
`typecheck` limpo · dois portões no build do 2D (toque e offline). **Nenhum número de
`Balance` do Roblox tocado.**

## v0.7.0 — 2026-08-19 — R13: as cinco decisões, e a ponte deixando de ser teórica

O Diretor delegou as decisões travadas à equipe (*"você toma a decisão mais viável e
segue"*) e fixou o rumo: **Roblox no centro, e o que funciona vai sendo registrado para
atravessar ao Android**. As cinco foram tomadas e coladas no `docs/GDD.md`.

### As cinco decisões

1. **As duas réguas de TTK sempre estiveram certas — medem alvos diferentes.** A
   varredura passou a medir contra os dois e mostrou: **5/5 dentro nas DUAS leituras**
   (Fogo 1,88 s contra vida base, 2,95 s contra vida + escudo nv1). O escudo soma 50
   sobre 100 de vida: +50% de alvo, 1,5× mais tempo com a mesma arma. O defeito era o
   relatório comparar a faixa do GDD com o número do escudo. **Nenhum número de
   `Balance` mudou** — e a hipótese que a equipe tinha registrado (de que a faixa
   antiga fora calibrada antes do escudo existir) estava **errada**.
2. **Gesto único promovido ao §19.3** — e, junto, o que o Diretor apontou: **teclado e
   mouse são um esquema DIFERENTE, não uma adaptação do de toque**. *"O que se
   compartilha é a REGRA do jogo — dano, mana, cooldown, alcance —, nunca o gesto."*
3. **A frente Android começa pelo Degrau 3 (mini-BR top-down)**, não pelo first-person:
   a escada da §8 vale, e o first-person continua sendo o destino, não o próximo passo.
4. **Antecipação registrada em duas velocidades**: o que não depende de V1–V5 anda; o
   design de combate espera o playtest.
5. **Público-alvo 10+**, com o registro explícito de que o questionário IARC é
   respondido pelo Diretor e a classificação é atribuída pelos órgãos.

### `docs/PONTE.md` — a auditoria que dá sentido ao "salvar o que funciona"

40 achados nas duas direções (21 que o Roblox faz e o GDD não descreve; 19 que o GDD
descreve e o Roblox não faz), cada um classificado como **PROVADO · PLAUSÍVEL · NÃO
VALIDADO**.

**O formato do resultado é a conclusão:** dos 10 PROVADOS, **nenhum é um número de
jogo**. São regras de sistema, arquitetura, medição e acessibilidade. Todo número de
combate, de Sintonia e de ritmo caiu em PLAUSÍVEL ou NÃO VALIDADO — porque **o jogo
nunca foi tocado por terceiros**. As regras atravessam agora; os números esperam a
sessão.

Também mapeou **9 decisões silenciosas** (o GDD manda uma coisa, o projeto fez outra,
ninguém registrou), e separou as **15 armadilhas que valem para qualquer engine** das
**8 que morrem no Roblox** — com o critério escrito: se a frase sobrevive trocando
"Humanoid" por "personagem", atravessa.

### O servidor nunca soube qual esquema de mira o jogador usa

Achado da auditoria, verificado e corrigido. O §19.3 promete dois esquemas (Simples
assistido × Avançado manual) **com pareamento competitivo por esquema**. No Roblox os
dois existem — `aimAssist = true`, sem tela para trocar — e a assistência é aplicada
**100% no cliente antes do envio**: o servidor recebe a direção já corrigida.

Num playtest com metade dos jogadores assistidos, o `p50` de TTK humano é a média de
duas populações e a pergunta **V2** sai sem sentido. O disparo passou a carimbar `aim`
("assist"/"manual") no `spell_cast` que já existia — mesma solução do `input`, sem
remote novo, telemetria pura (a assistência já aconteceu antes do envio, então mentir
não concede nada).

### A V3 parou de ter buraco de autoria

A Sintonia passa autor no terreno: a fusão é o pilar do jogo, e toda mudança de mapa
que ela causava era invisível para a V3. O autor é **quem conjurou primeiro** — o
mesmo sujeito dos eventos `channel_start`/`fired`, para um único ato não aparecer sob
dois donos, e **um autor por fusão** é o que segura a razão do funil abaixo de 100%.

**Os Bots ficaram de fora de propósito**, e virou decisão registrada em vez de
esquecimento: a V3 pergunta se *gente* transforma o mapa em jogada, e a FSM do bot só
conhece terreno para fugir do fogo.

E o **tutorial parou de contaminar**: os passos de queimar e congelar são magias reais
e chegavam à coorte humana: o aluno queimando a árvore que *mandaram* queimar entrava
no numerador de "usou o terreno de propósito", junto com quem descobriu sozinho. Isso
media obediência e chamava de descoberta. Agora sai carimbado e o coletor exclui da V3
— mesmo raciocínio da R11.1 com o ping, e pela mesma razão: **carimbar, não suprimir**.

### PC × celular: três defeitos que a separação expôs

- **Herança cruzada na detecção de aparelho** — havia **três critérios diferentes**
  para "é celular?". Notebook com tela sensível casava com dois e recebia o esquema de
  celular por cima do mouse. Agora é uma regra só, e o campo `touch` do relatório passou
  a significar *"jogou no esquema de toque"* em vez de *"tem digitalizador"*.
- **Clique de interface virava conjuração** — o disparo básico do PC era lido por
  sondagem todo frame, fora da porta que filtra clique consumido por UI: clicar em
  Grimório ou Opções soltava magia junto.
- **O cancelar era invisível no polegar** — a lógica existia, mas o anel do botão só
  dizia "tem dedo em cima". Uma conta só decide agora as duas coisas.

Um defeito real foi **deixado em paz com argumento**: `pcAimDirection` mistura duas APIs
com convenções diferentes de inset de GUI (viés de ~36 px). Não foi tocado porque o
Diretor disse que a mira está ajustada — e se foi ajustada no olho, foi ajustada **com**
esse viés. Registrado como candidato, só com número de playtest na mão.

### S7 — a maratona, e o harness que mentia

Todos os cenários mediam **uma** partida. O S7 encadeia **6 provas** com gente entrando
e saindo pelas duas portas de partida nova, e mede por **contagem de chaves**, nunca por
memória. Nenhum vazamento no `Match` — mas o cenário achou algo pior:

**O harness nunca desparentava o Player que saía.** `player.Parent` é o teste de "ainda
conectado" usado em quatro ramos do `Match` — e **nenhum deles jamais rodou num teste**,
sendo o caminho que um alpha percorre o dia inteiro. E o corpo nunca era destruído: 3
Models órfãos por prova (43 em 14 provas) continuavam aparecendo em consultas espaciais.

Custo: portão de 3,05 s → 4,08 s (+34%), justificado no relatório — e uma otimização
que economizava 100 ms foi **revertida** por não valer 20 linhas de harness.

### Higiene
Os seis rótulos dos botões de toque (`ATQ`, `TÁT`, `ESQ`, `PULAR`, `CORRER`, `AGACHAR`)
eram os **únicos textos de UI fora do `Strings`** no projeto — a auditoria de i18n
olhava as telas, não os botões. Migrados, PT e EN.

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (935 KB) · harness **49/49** ·
`npm run typecheck` limpo. **Nenhum número de `Balance` tocado.**

## v0.6.0 — 2026-08-19 — R12: as frentes que faltavam do roadmap

Quatro frentes avançadas em paralelo, na ordem escrita em `docs/ANDROID.md` §5. As
Fases 1, 4, 7 e 8 são atos do Diretor (aparelho físico, conta de loja, testadores
externos, decisão de produto) e não têm como ser avançadas pela equipe.

### Android — Fase 2 fechada: "Parece o Arkana"

Ícone e splash eram **os padrões do Capacitor** (símbolo azul sobre branco). Agora o
**Selo de Arkana** em VectorDrawable: adaptativo (API 26+), camada `<monochrome>`
para o tema do Android 13+, e fallback próprio para 24–25. **Zero binário novo** no
repositório — e −180 KB de PNG do template removidos.

A forma não foi inventada: veio de `src/core/textures.ts` (`uiSeal`) e
`roblox/src/client/Hud.luau` (`buildSeal`). Onde as duas divergem, ficou o **losango**
do Roblox em vez do círculo do 2D — o losango é o que carrega a FORMA que o GDD §10
exige junto da cor, para daltônico.

**Prova, não intenção:** o APK foi extraído e não contém **nenhum PNG** de ícone ou
splash; `colorPrimary` saiu de `#3F51B5` (lib do Capacitor) para `#0B1026`. Foi
preciso apagar os `mipmap-*dpi/*.png` porque qualificador de densidade venceria o
sem-qualificador em 24–25 e o ícone do Capacitor continuaria aparecendo — exatamente
a falha calada que o portão procura.

### 2D — offline de verdade, e o defeito de toque de 17/08 tinha causa-raiz

**Fontes empacotadas** (4 `.woff2`, ~69 KB, subconjunto `latin`, licenças OFL junto).
Era a **única chamada de rede do protótipo inteiro** — sem ela, a ficha de Segurança
de Dados da Play declara "nenhuma coleta", e o jogo abre em modo avião com a
tipografia certa (GDD §19.6 quer o Modo Treino Offline como produto). Um portão no
`vite.config.ts` quebra o `npm run build` se alguém reintroduzir CDN de fonte —
testado que falha de verdade.

Achado que teria matado o critério: **o canvas do Phaser nunca dispara o download de
um `@font-face`** (não há texto no DOM que peça a fonte, e cada `Text` é rasterizado
uma vez). O jogo abriria com a fonte local instalada e o título em fonte de sistema.
Resolvido com `document.fonts.load()` no boot, com `.catch()` — falha de fonte nunca
impede o jogo de abrir.

**A troca de elemento que não respondia ao toque: dois defeitos, e o segundo explica
por que só o aparelho do Diretor via.**

1. `setScrollFactor(0)` era aplicado ao *container*, nunca aos ícones. O Phaser
   restaura o `scrollFactor` do filho depois de desenhar, mas o **hit-test usa o do
   próprio filho** — a área de toque ficava deslocada pelo scroll da câmera, que na
   Arena **segue o jogador**. O toque caía na zona de mira.
2. O piso de 48dp brigava com o espaçamento de 54px: em tela de 640px o raio de toque
   vira 48px e **três quartos da superfície do próprio ícone** entregavam o toque ao
   vizinho. Em desktop o raio é 24px e nada acontece — por isso era invisível fora do
   celular.

Verificado no pior caso (pane de 306px): 15 toques sintéticos, 15 acertos. Com o
código anterior, os mesmos 15 devolviam o vizinho.

**O gesto único NÃO foi implementado**, de propósito: o GDD §19.3 ainda especifica
dois gestos e o §9 trava que o GDD é a única ponte. Sem meio-termo, sem flag.

### V3 finalmente tem métrica de desfecho

`terrain_tactical_outcome` estava no schema desde a R5, era consumido pelo coletor e
**ninguém o emitia**. O relatório sabia dizer que *o mapa queimou* e não que *alguém
queimou de propósito e ganhou algo*.

Três desfechos, cada um com o que o servidor sabe que o PROVA: **abate** (vida ≤0 no
tique de perigo, na célula cujo estado tem autor), **travessia** (jogador de pé em
água `Frozen` — líquida barra o andar, logo aquela posição não existia) e **cobertura
destruída** (projétil atravessou célula de árvore cuja copa o fogo derrubou — a prova
não é "queimou", é ter passado tiro por onde ela barrava).

**`fuga` não é emitida**: exigiria contrafactual (saber que o alvo *ia* morrer e que o
terreno foi *a causa* de não ter morrido). Número bonito e inauditável.

Anti-inflação: ponte de gelo de 6 células = **1** travessia; queimada que derrubou 20
árvores = **1** cobertura. A razão não tem como passar de 100%. E o dano de terreno
**continua com `fromPlayer = nil`** — creditar o autor ali seria telemetria decidindo
jogo (evoluiria o escudo dele com dano de ambiente e jogaria o elemento na dominância
sem `spell_cast` no denominador).

Relatório: o desfecho passou a ser quebrado por tipo (**abates · travessias ·
coberturas**) e a separar quem se queimou sozinho — arma de dois gumes é evidência do
GDD §14, não jogada bem-sucedida. E a pendência parou de mentir: acusava "ninguém
emite", quando agora zero é **resposta legítima** ("ninguém usou o terreno de
propósito"); ela passou a acusar o que de fato impede a leitura — terreno mexido por
Sintonia/Bots/Tutorial, que não passam autor.

### O relatório do playtest parou de evaporar

`matchEnd` imprimia e `matchStart` zerava: num servidor público há ~35 s entre uma
prova e a próxima, e relatório não copiado nessa janela sumia. Agora há **acumulado
da sessão de servidor** (várias provas somadas, com o TAMANHO DA AMOSTRA declarado
antes de qualquer número) e **persistência em DataStore** dos 5 relatórios mais
recentes, de todos os servidores, recuperáveis com **uma linha colável** que o próprio
relatório imprime.

Guarda o TEXTO e não os números: quem agrega máquina a máquina já tem
`Telemetry.snapshot()`; o que se perdia era a leitura do Diretor. `UpdateAsync` e não
`SetAsync` — dois servidores fechando juntos apagariam o relatório um do outro. E
quando nada foi gravado o relatório **grita** `!! NADA GRAVADO AINDA`, porque no
Studio o DataStore só existe com "Enable Studio Access to API Services" ligado.

### `docs/DECISOES_PENDENTES.md` (novo)

As cinco decisões que só o Diretor pode tomar viraram cinco linhas para preencher —
duas já com **o texto pronto para colar no GDD**. A equipe não edita o GDD: ele é a
fonte da verdade e emendá-lo é ato dele (§19.5).

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (905 KB) · harness **48/48** ·
`npm run typecheck` limpo · APK debug recompilado e verificado (o conserto do
carrossel e as fontes estão DENTRO do pacote). **Nenhum número de `Balance` tocado.**

## v0.5.0-roblox — 2026-08-19 — R11: tela mais limpa, tutorial opcional e o Android de volta

Três ordens do Diretor, e a frente mobile retomada.

**"Informação demais na tela".** A raia mediu antes de cortar: **65 004 px² permanentes
= 8,9% de um celular em paisagem** — e, pior que a área, **72 px de mobília girando na
coluna da mira** (o Selo de Arkana, em 3ª pessoa, bem onde o jogador aponta). O Selo
some no modo ESSENCIAL porque suas duas informações eram **100% duplicadas**: "combo
disponível" já é dito pela pílula com **palavra + losango** (canal mais acessível que
gema colorida) e "meu elemento" pelo slot do carrossel, permanente em cor+forma, onde o
dedo já está. Nome do parceiro e `FASE x/y` viraram sob demanda — texto que nunca muda
durante um tiroteio não paga o espaço que ocupa; o nome volta **no instante em que ele
cai**, que é a única hora em que decide algo. **Permanente depois: 55 140 px², 15% menos**,
coluna da mira zerada. Preferência `hudDensity` (ESSENCIAL padrão × COMPLETO), persistida
e trocável ao vivo — a queixa foi de excesso, então o excesso é que virou opt-in.

**"Tutorial não é obrigatório."** Ele era pior que obrigatório-com-botão-de-pular: o
`Main` o empurrava em todo corpo novo, **e o lobby segurava por até 240 s** enquanto
alguém treinava — quem nem escolheu o tutorial esperava por quem escolheu. Agora é
CONVITE (`Tutorial.offer`): duas saídas do mesmo peso, a sessão só abre com aceite, e
recusar não é porta de mão única (o botão TREINAR volta no lobby, no mesmo slot do PULAR,
sem mobília nova). **O teto de 240 s deixou de existir** — o conserto não era mexer no
número, era não ter espera — e um assert trava a volta dele. Quem entra no minuto 4 de
uma prova não perde o convite: ele fica pendente e sai no lobby seguinte.

**O cenário S1 trocou de pergunta, não foi apagado.** Ele cobrava que o lobby SEGURASSE
— exatamente a regra derrubada. Mas o defeito que ele existe para pegar continua
possível: o perigo nunca foi "a partida começa", foi a **sessão do onboarding ficar
órfã**, com o jogador preso num Campo de Provas que o `startMatch` já desmontou. O S1
agora cobra: ninguém entra sem aceitar · o aceite chega pelo payload REAL do cliente ·
o lobby não espera · e quem treinava sai limpo. Provado com dois defeitos reintroduzidos.

### Android — a frente mobile saiu do papel, com prova
**O APK compila.** Debug 4,4 MB, release 3,4 MB e **AAB 3,3 MB** (o formato que a Play
exige de app novo), build frio em 59 s, `typecheck` limpo. O `docs/BUILD_ANDROID.md`
estava errado em cinco pontos: dizia que "falta o SDK para compilar" (compila), mandava
instalar **JDK 17** quando o projeto **exige 21**, pedia `platforms;android-34` com
`compileSdk` 36, e instruía a referenciar um bloco `signingConfigs` **que não existia**.
Agora existe, lê `keystore.properties` (fora do versionamento, com `.gitignore` cobrindo
`*.jks`/`*.keystore`/`keystore.properties`) e o release compila sem chave em vez de travar.

Armadilha documentada: `bundleRelease` termina com `BUILD SUCCESSFUL` e o `.aab` sai
**não assinado** — a Play recusa. A fiação de assinatura foi provada apontando para a
keystore de *debug* do próprio SDK (`apksigner verify` → `Verifies`), e depois apagada.
**Nenhuma chave existe no repositório.** `versionName` era `"1.0"` (default do Capacitor,
anunciando protótipo como versão final) → `0.1.0`.

**`docs/ANDROID.md`** (novo): qual produto vai para a loja, o que falta para publicar
(existe/falta/não verificado) e as diretrizes que ESTE jogo precisa cumprir — ECA Digital,
zero caixa aleatória, classificação etária.

### Decisões que ficaram para o Diretor (não tomadas aqui)
- **O gesto único não pode atravessar para o Android.** O GDD §19.3 ainda especifica
  DOIS gestos ("mirar arrastando + botões"); o gesto único nasceu da reprovação do teste
  de APK e vive no Roblox e no PRISMA. Pela regra travada do §9 ("nada técnico migra do
  Roblox — o GDD é a única ponte"), ele precisa ser **promovido ao §19.3** para valer.
- **Não está decidido qual build Android vem primeiro**: §9 aponta first-person; a escada
  do §8 põe o mini-BR top-down (Degrau 3) antes; o PRISMA está "pendente de 1 palavra".
- **Antecipar o Android contraria o §9**, que trava o início para depois da validação do
  Roblox — e nenhuma das 5 perguntas foi respondida com jogador real.
- **Público-alvo etário nunca foi declarado** em documento nenhum — sem isso não há
  questionário IARC nem política de dados de menores.

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (852 KB) · harness **48/48** ·
APK/AAB compilados e verificados. **Nenhum número de `Balance` foi tocado.**

## v0.4.0-roblox — 2026-08-18 — R10: a medição que faltava para o playtest valer

O produto existe para responder **cinco perguntas** (`docs/ROBLOX.md` §1). Esta fase
foi auditar se ele consegue — e a resposta era não.

**A V5 ("o combate segura o jogador até a próxima prova?") não tinha uma linha de
instrumentação**, e `Events.Name.SessionEnd` estava declarado no schema desde a R5
**sem nunca ter sido emitido**. Agora há sessão que ATRAVESSA partidas (o único
recorte que `matchStart`/`matchEnd` não zeram): quanto tempo ficou, quantas provas
jogou, e **em que ponto desistiu** — lobby, onboarding, meio da prova ou depois
dela. São quatro consertos diferentes, e por isso não viram um número só.

**`device_tier` saía "unknown" por um bug, não por falta de feature.** A pendência
dizia "falta o cliente reportar plataforma"; o cliente reporta desde sempre
(`Device.luau` dispara `Net.Names.DeviceInfo`) e **nunca existiu handler no
servidor**. O dado era enviado e jogado fora — por duas fases, com o diagnóstico
errado registrado. Fiado, e o portão agora dispara o remote de verdade e reprova se
a ponta a ponta abrir.

**O disparo passou a carimbar o gesto** (`drag` / `tap` / `key`) — a régua do
requisito de celular do §4, que reprovou no teste do APK 2D e até aqui não tinha
como ser medido. Viaja como campo do `spell_cast` que já existia: é telemetria
pura, e um remote a mais seria superfície de ataque a mais num lugar público.

**O relatório passou a responder as cinco perguntas por nome**, com o número e a
decisão que ele muda. Amostra pequena sai como `n=4, NÃO CONCLUI` em vez de
percentil de anedota.

### A régua do TTK estava contraditória — e o relatório afirmava sucesso contra a mais frouxa
`docs/GDD.md:168`, fonte da verdade, define **TTK ~1,5–2,5s**. A varredura media
contra **2,75–3,5s** ("alvo da auditoria") e vinha concluindo **"5/5 DENTRO do
alvo" desde a R7** — contra a régua do GDD, nenhum dos cinco está dentro. As duas
réguas são legítimas e ninguém as reconciliou. **Qual vale é decisão do Diretor**:
os relatórios passaram a mostrar as duas, com a origem no nome, e a avisar quando
discordam. Nenhum número de `Balance` foi tocado.

### Também nesta fase
- **Preferências atravessam o rejoin** (`server/Prefs.luau`): as 11 opções de
  conforto morriam na saída — quem baixava o volume levava o susto de novo a cada
  entrada. O servidor guarda só o TIPO de cada chave, nunca o valor: os padrões
  continuam num lugar só e não há dois números para divergir. Zero escritas durante
  a partida (uma por sessão, na saída, mais `BindToClose`).
- **`Playtest.luau` parou de mentir**: o cabeçalho dizia que não dava para encerrar
  partida em curso e o módulo empurrava o lobby escrevendo em `Balance.match`.
  `Match.forceStart/forceEnd` existem desde a R8 — agora `P.duo()` chama o Match, há
  **`P.stop()`**, e a ferramenta do Diretor não escreve mais em balanceamento.
- **Protocolo do playtest alfa** (`docs/ROBLOX.md` §11): arranjo, duração, o que o
  Diretor NÃO pode dizer (a V1 é "a dupla aperta COMBO sem você mandar"), como o
  relatório chega num lugar publicado, limiar de "bom" por pergunta e as
  contradições encontradas entre documento e código.
- **`Discovery` era coletado e nunca impresso** — a melhor proxy de "descobriu a
  química sozinho" (V3) estava invisível. E a distribuição de dano por jogador
  humano entrou no relatório: sem ela o gatilho pós-playtest do escudo era
  inacionável justamente quando houvesse dado humano.

### Continua aberto, declarado no próprio relatório
`Events.Name.TerrainTacticalOutcome` está no schema e é consumido, mas **ninguém o
emite** — a V3 fica sem métrica de DESFECHO e sobra a descoberta do Grimório como
proxy honesta. E o funil da V1 é contaminado pelo tutorial, que usa o ping de
verdade: a mitigação é ter todo mundo dentro antes da largada.

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (822 KB) · harness **46/46**.
**Nenhum número de `Balance` foi tocado.**

## v0.3.0-roblox — 2026-08-18 — R9: robustez com teste, anti-griefing e a tela do estranho

Fase de **preparo para o alpha público**. Nada de feature nova: o alvo foi o que
quebra quando o jogador é um desconhecido e o servidor é de verdade.

**Os 6 travamentos da R8 viraram teste** (`roblox/tools/scenarios.luau`) — eles
estavam corrigidos e sem uma linha de cobertura, ou seja, a próxima fase podia
reintroduzir qualquer um em silêncio. S1 tutorial atropelado pela largada · S2
spam de arrepio · S3 bot preso em geometria · S4 servidor esvazia (seguia
`Playing` com 8 bots para plateia nenhuma) · S5 `Playing` eterno (a fase final da
zona PARA e segura; o teto tem de **derivar** do cronograma, e o teste separa
"tem um número grande" de "acompanha a variante de ritmo") · S6 `forceStart` de
`Ended` vazando escudo e cooldown. Cada cenário foi validado **reintroduzindo o
defeito**: 10 mutações, 10 vermelhos. Portão do projeto: 35 → **43 verificações**.

**Griefing entre estranhos pareados em dupla** — mesma razão que desligou o fogo
amigo na R8 (um desconhecido arruína a sessão do outro e a pergunta V1 fica
ilegível):
- A canalização de Sintonia só é cancelável morrendo ou saindo, e o cooldown
  compartilhado é cobrado no INÍCIO — então bastava o parceiro morrer de
  propósito para tirar 24 s do pilar do jogo do outro. Agora a interrupção
  **devolve o cooldown a quem continua vivo**; quem caiu ou saiu segue pagando.
  O limitador do GDD §9 fica inteiro: **o combo que DISPARA continua cobrando os
  dois** — é esse que impede o spam. (Corrige a regra como descrita em R2.)
- O muro de Terra nascia sem olhar quem estava de pé na célula: dava para
  enterrar o parceiro dentro de 10 studs de pedra por 20 s. Muro não sobe em
  célula ocupada — regra uniforme, porque o GDD §14 define o muro como
  **cobertura destrutível**, não como botão de deletar alguém.

**A primeira impressão de quem entra no meio da prova.** Num servidor público
essa é a experiência da maioria, e ela estava muda: o jogador nascia espectador a
120 studs do chão sem uma palavra na tela. Agora há três estados distintos —
jogando · **espírito** (fora da contagem, não da partida: ainda volta pelo altar)
· espectador — e quem chegou agora lê "você entra na próxima" em vez de "você
está fora". Sem relógio de próxima partida: o teto de duração é teto, não
previsão, e erraria por minutos; a tela mostra fase da zona e times restantes,
que são exatos.

**Volume.** O jogo saiu do silêncio na R8 mas o jogador não tinha como baixá-lo —
`Audio.setVolume` era loja órfã, sem controle na tela. A preferência passou a
morar no `Accessibility` (uma loja só; o `Audio` apenas aplica) com dois
controles em opções. Zerar o volume liga as legendas dos avisos: mudo não pode
custar informação que só existe no som.

**Laje das ruínas e lodo ganharam cor própria** (`Slab`/`Silt`): os dois POIs mais
característicos da arena se pintavam com a cor da estrada e a do barro comum. As
regras são as mesmas — inclusive "Água + terra = lamaçal" (§14) no lodo, que é a
jogada da casa da baixada e agora tem teste próprio.

Gates: 39 arquivos Luau sem erro de sintaxe · `rojo build` limpo · harness
**43/43** · varredura de 12 partidas sem regressão (TTK 5/5 no alvo, régua de
dano/mana 1,17× contra teto de 1,25×). **Nenhum número de `Balance` foi tocado.**

## v0.2.0-roblox — 2026-08-18 — "Campo de Provas" no Roblox (R0–R3)

Primeira versão da vertente **Roblox** (produto de validação — GDD §9 "Decisões
travadas", plano em `docs/ROBLOX.md`). Terceira pessoa, estética blocky
(referência Pixel Gun 3D), servidor autoritativo. Código em `roblox/`,
sincronizado por **Rojo**; `rojo build` gera o arquivo do lugar.

**R0 · Fundação** — projeto Rojo, `Shared/Balance` (porte fiel dos números do
protótipo 2D + conversão px→studs), `Shared/Elements` (5 elementos com cor +
FORMA e a matriz dos 10 combos), `Shared/Grid` (materiais imutáveis, estados
mutáveis) e `Shared/Net` (remotes com sanitização — o cliente manda intenção,
nunca dano).

**R1 · Os pilares**
- Arena 60×60 determinística: lago de 205 células contíguas, floresta com
  densidade **acima do limiar de percolação** (238 de 282 árvores conectadas —
  abaixo disso o incêndio morre em bolsões e o pilar do GDD §14 não acontece).
- Terreno reativo completo: fogo propaga e derruba a copa, lago **congela
  erguendo a lâmina até o chão** (ponte literal), raio eletrocuta toda a água
  conectada, terra ergue muro, água+terra vira lamaçal.
- Combate 100% autoritativo: projéteis com tempo de viagem (zero hitscan),
  mana/cooldown/dano decididos no servidor, Escudo Evolutivo 1→4, i-frames.
- **Controles com mira e disparo em UM gesto** (correção da reprovação do teste
  em aparelho real): arrasta do botão da magia e solta; toque curto atira
  rápido; voltar ao centro cancela. Carrossel de elementos com alvos de 58dp.
- HUD com nível de escudo em número **e** pips (acessibilidade além da cor),
  Selo de Arkana, VFX blocky, números de dano e i18n PT-BR/EN.

**R2 · Sintonia (o pilar de inovação, GDD §9)** — os 10 combos com dano em área
e reação de terreno coerente; janela de 1,5s, canalização interrompível de 1s e
**cooldown compartilhado cobrado no início** (combo interrompido não devolve o
custo — é o que impede spam; **revisto na R9**: quem continua VIVO é reembolsado,
senão o parceiro tira 24 s do outro de graça — o combo que dispara segue cobrando
os dois, que é onde o anti-spam realmente mora); Ping de Sintonia (GDD §18.7) para combinar sem
microfone; mana dos dois conjuradores drenada.

**R3 · Loop de battle royale** — `Lobby → Playing → Ended`, zona arcana em 5
fases com dano crescente, eliminações com crédito de abate, **bots de
preenchimento** montados em blocos por código (FSM do protótipo: vagar,
perseguir, atacar, fugir do fogo) e **Selo do Campeão** com abates, dano,
combos e elementos usados.

### Verificação

`rojo build` limpo e **19 arquivos Luau sem erro de sintaxe** (`luau-compile`).
Autotestes embutidos: `Combat.selfTest()`, `Sintonia.selfTest()`,
`Match.selfTest()`. **Playtest é humano** — nada foi jogado ainda.

### Limitações conhecidas

- Sem times: dois jogadores hostis podem disparar um combo entre si (queima o
  cooldown de ambos). Resolve-se quando houver squads.
- Atordoamento dos combos elétricos pendente (exige `Combat.applyStatus`).
- Bots limitados a 11 até haver medição de performance em aparelho fraco.
- Jogador eliminado ainda consegue conjurar do poleiro de espectador.
- Fontes Cinzel/Chakra Petch do GDD §10 exigem upload pelo Diretor.

## v0.1.2 — 2026-08-17 — PRISMA-1: fundação de render

Primeira leva do Projeto Prisma (docs/PROJETO_PRISMA.md §3), executada em
4 raias paralelas sobre o contrato `RenderModule` (`src/render/contract.ts`),
fiado na ArenaScene. Só a camada de apresentação mudou — simulação,
balanceamento e contratos intactos.

1. **Luz & pós-processamento** (`src/render/lighting.ts`) — entardecer arcano
   (Light2D + luz ambiente), luzes dinâmicas com pool por distância (projéteis
   na cor do elemento, fogo/eletricidade pulsantes, cajado dourado com
   flicker), Bloom + vinheta + color grading na câmera. No-op gracioso em
   Canvas; reage à mudança de qualidade ao vivo.
2. **Pele do terreno** (`src/render/terrainTextures.ts` + `terrainSkin.ts` +
   TerrainGrid visual) — autotiling por bitmask com bordas onduladas
   (água>areia>terra>grama), 4 variantes por tile a 64px (RT 2×, mundo lógico
   intacto), decals determinísticos, água com ondulação e espuma na borda
   (gelo congela a animação — estado legível), praia em terra vizinha de água.
3. **Personagens vivos** (`src/render/characterRig.ts` + `characterTextures.ts`
   + entidades) — sprites 64px, idle respirando, bob de passo, inclinação na
   direção do movimento, manto em 2 segmentos com inércia, sombra elíptica,
   squash & stretch na esquiva; identidade de elemento dos bots preservada.
4. **VFX & juice** (`src/render/vfx.ts` + `vfxTextures.ts` + Projectile
   visual) — trilhas por elemento (cor+forma), impacto com onda de choque +
   flash + hitstop (40–60ms com cooldown) + recuo de câmera, fogo do terreno
   em 3 camadas (chama + brasas + fumaça), pólen ambiental, screen shake por
   trauma decaindo.

Tudo com gate pela config de Qualidade (Baixa/Média/Alta) — antecipando o
PRISMA-4 (mobile). Validado em runtime: WebGL, 10 luzes ativas, 3
post-pipelines, zero texturas ausentes, zero erros de console.

### Limitações conhecidas

- RT do terreno usa 3840×3840px — GPUs com `MAX_TEXTURE_SIZE < 4096`
  precisarão de fallback 1× (PRISMA-4).
- Teto de 10 luzes simultâneas (default do Phaser) — subir exige config em
  `main.ts` (PRISMA-4).
- Feel do hitstop/shake e densidade do fogo merecem ajuste fino com controle
  humano (sondas validam presença e orçamento, não gosto).

## v0.1.1 — 2026-08-17 — Fase 2: pipeline Android + controles de toque

Entrega o escopo da Fase 2 (GDD seções 19.3–19.5), executada em 2 raias paralelas:

1. **Pipeline web→APK (Capacitor 8.5)** — `capacitor.config.ts`
   (`br.com.vstack.arkana`), projeto nativo em `android/` com orientação
   landscape travada e tela cheia, script `npm run build:android` e guia
   completo de compilação em `docs/BUILD_ANDROID.md`.
2. **Controles de toque (GDD 19.3)** — `src/ui/TouchControls.ts`: joystick
   virtual (zona esquerda), mira por arrasto (zona direita), botões de
   Ataque (segurar = fogo contínuo), Tática e Esquiva com cooldown desenhado
   no próprio botão, carrossel dos 5 elementos (cor + forma), botão de pausa
   e alvos ≥ 48dp.
3. **Dois esquemas de mira** — Simples (assistida por cone) e Avançado
   (manual), escolhíveis em Configurações › Controles.
4. **Layout editável e escalável** — slider de escala (0.7–1.5) e modo
   "Editar layout" (`src/scenes/TouchLayoutScene.ts`) com arrastar-e-soltar,
   persistência e "Restaurar padrão".
5. **Ativação Auto/Ligado/Desligado** — auto-detecção por `maxTouchPoints`;
   "Ligado" permite testar toque no desktop. Com toque desligado, o desktop
   permanece 100% intacto.
6. **HUD mobile** — painéis realocados para zonas seguras dos polegares;
   `index.html` endurecido para WebView (viewport fixo, sem long-press/zoom).

### Limitações conhecidas

- **APK não compilado nesta máquina** — falta JDK/Android SDK; o passo a
  passo está em `docs/BUILD_ANDROID.md` (o build é 1 comando após instalar).
- Mudanças de modo/escala/layout de toque aplicam ao (re)entrar na arena.
- Ergonomia real dos polegares deve ser conferida no aparelho físico.

## v0.1.0 — 2026-08-17 — Protótipo "Campo de Provas" (Fase 1)

Primeira versão jogável. Entrega o Definition of Done da seção 15 do GDD:

1. **Boot completo** — splash do estúdio → carregamento (key art + logo +
   barra dourada + dicas rotativas) → título → menu.
2. **Menu principal** navegável com música gerativa e fundo animado
   (partículas elementais + Selo de Arkana).
3. **Configurações funcionais e persistidas** — todas as abas da seção 12
   (Vídeo, Áudio, Controles com remapeamento, Jogo com PT-BR/EN), com
   "Restaurar padrão" por aba.
4. **Arena top-down 60×60 tiles** com biomas: lago, floresta, grama alta,
   rochas e campo aberto.
5. **1 mago jogável (Evocador)** — WASD + mira no mouse, ataque básico (M1),
   magia tática (M2) e esquiva com i-frames curtos (ESPAÇO).
6. **5 elementos no Q** — Fogo, Água, Terra, Vento e Raio, cada um com
   projétil, cor, ícone e som próprios (regra de acessibilidade cor+forma).
7. **Terreno reativo** — árvore queima e propaga; lago congela (ponte) e
   eletrocuta com Raio; água + terra = lamaçal; Terra ergue muro de pedra
   destrutível (tabela da seção 14).
8. **Escudo de Magia Evolutivo** — dano causado acumula e sobe o nível
   (1→4, branco→azul→roxo→dourado) com banner e flash no HUD.
9. **5 bots de treino + 1 dummy** — bots com FSM (vagam, perseguem, atacam
   com erro de mira e FOGEM do fogo — reagem ao terreno como o player);
   dummy imóvel com números de dano e janelinha de DPS que reseta sozinha.
10. **HUD completo** — vida, escudo (com progresso de evolução), mana,
    carrossel do elemento ativo + cooldowns de tática/esquiva, minimapa com
    terreno e entidades, Selo de Sintonia decorativo e números de dano.
11. **Pausa (ESC) + contador de FPS** (ligável nas configurações de vídeo).

### Limitações conhecidas

- **Settings em `localStorage`** — equivalente web do `settings.json`; o
  arquivo real chega no empacotamento Tauri da Fase 2.
- **VSync e resolução** efetivamente controlados pelo navegador; as opções
  existem e persistem, mas o navegador tem a palavra final.
- **Sintonia decorativa** — o Selo no topo do HUD é placeholder do medidor
  real de Conjuração Combinada (v0.2, com 1 bot aliado).
- **Suprema reservada** — a tecla R está mapeada nas configurações, mas a
  magia suprema é escopo do v0.2.

### Fora do escopo (anotado para o v0.2)

Sintonia jogável, queda do castelo, multiplayer, cosméticos e passe — ver GDD
seção 15 ("Fora do escopo do v0.1").
