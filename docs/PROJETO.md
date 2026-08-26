# PROJETO — estado atual do Arkana

> **Este é o documento de memória do projeto.** Quem chegar agora deve conseguir
> ler só este arquivo e entender: onde o projeto está, o que já foi feito, o que
> falta e o que está bloqueado. Atualize-o ao fechar cada fase.
>
> **Atualizado em:** 25/08/2026 (higienizacao pos-R20.1)
>
> **REGRA PERMANENTE (ordem do Diretor, 20/08):** ao finalizar QUALQUER fase,
> atualize este documento com **o que foi feito, como e por que**, e reescreva
> a secao CONTINUAR DAQUI com tudo que ainda falta. Esta secao nunca pode
> apontar para uma fase antiga. Este e' o UNICO documento de memoria do
> projeto — nao crie outro.

---

## DIRETRIZ PERMANENTE DO DIRETOR (25/08/2026)

*"O tamanho do APK não importa hoje. Para testar e validar isso é irrelevante,
meu celular suporta. O importante é validar com o que é real, mais importante
do que deixar o arquivo comprimido e sem qualidade. Com esse excesso de
segurança e travas de lógica sem sentido... eu quero correr os riscos, fazer
acontecer com qualidade e funcionalidade, claro com lógica, sem atropelar as
coisas, mas sem esse medo excessivo."*

**Como isso muda o trabalho, em concreto:**

- **PARE de usar peso de arquivo como argumento** para recusar asset melhor,
  textura maior ou modelo mais detalhado. Ele tem 20+ GB e o aparelho aguenta.
- **Qualidade e funcionalidade reais valem mais que otimização precoce.**
  Compressão, atlas e formato de loja entram quando as projeções fecharem.
- **O que CONTINUA valendo**, e não é a mesma coisa: **quadros por segundo no
  aparelho**. Pacote grande é irrelevante; jogo travando é o jogo não
  funcionando. Se um asset derrubar o FPS, o problema é o FPS — diga isso com
  MEDIÇÃO no aparelho, nunca com estimativa de disco.
**O APARELHO DE TESTE, agora conhecido (25/08):** **Poco F4** — Snapdragon 870,
GPU Adreno 650, tela 120 Hz. Nao e' aparelho de entrada: e' alto desempenho de
2022 e roda jogo pesado. Ate' hoje o projeto mirava "aparelho intermediario"
generico, o que era chute. **A regua de desenvolvimento pode ser bem mais
generosa do que a que estava escrita** nos orcamentos de TECH_ART.
Duas coisas seguem verdadeiras mesmo assim: o aparelho do Diretor NAO e' o
aparelho do jogador final (isso volta a importar em G6, na loja), e **FPS se
mede, nao se estima** — e nunca foi medido neste projeto, porque nenhum
aparelho apareceu em `adb devices`.

- **O que também continua valendo:** teste provado em vermelho, portão verde
  antes de entregar, e nada de atropelar etapa. Ele pediu risco com lógica, não
  ausência de método. *"sem atropelar as coisas"* são as palavras dele.

---

## CONTINUAR DAQUI

### >>> COMECE POR AQUI — o vídeo do Diretor avaliado, e o que ele rendeu

O Diretor testou o APK 12:20 e mandou vídeo (55 s, treino). **Quatro defeitos
reais saíram dele, três já corrigidos com teste vermelho:**

1. **"BRAÇO FRIO" preso na tela** (do segundo 2 ao 55). Causa: a Pyra ligava o
   chip com `avisar_estado` direto — fora do relógio de estados, ninguém
   desligava. Agora `ligar_estado(esfria_dur)`: expira e avisa a HUD.
2. **A personagem anda como SILHUETA PRETA.** Causa MEDIDA: o PBR da Meshy vem
   com `metallic = 1.00`, e metal é quase todo reflexo — sem reflection probe o
   mobile devolve breu. `Mage._domar_pbr` grampeia metallic ≤ 0.2 / roughness
   ≥ 0.45 na importação.
3. **O carrossel MENTIA**: mostrava VENTO e o tiro saía FOGO (o elemento agora
   é da luva). Luva de elemento travado esconde o carrossel; mãos nuas devolvem.
4. **"Caixote bege gigante"** no 0:11 — não é bug: é o MURO do elemento TERRA.
   Feio de doer; entra na fila visual (Meshy §10).

**O que mais entrou nesta leva:**
- **O elemento MORA NA LUVA** (decisão dele): loot cicla os 5 pelo mapa,
  pegar/trocar viaja com o elemento, o disparo ignora o carrossel.
- **Bastões viraram LUVAS** na tela (punho/dorso/dedos procedurais, cor do
  elemento, silhueta por tier). Modelo definitivo: Meshy (peça nº 2).
- **PAUSA em partida** (pedido do vídeo + GDD §12): ícone no canto superior
  direito → RETOMAR / CONFIGURAÇÕES (o MESMO menu/Config.gd) / ABANDONAR.
- **O braço desce**: pedir `cast` de novo REINICIA o clipe (antes o disparo
  contínuo subia o braço no 1º tiro e congelava — a queixa literal dele).

**Pendências NOVAS do vídeo/feedback (por ordem do que ele falou):**
- A QUEDA "ainda está estranha" — sem detalhe suficiente; pedir o que
  exatamente (pose? velocidade? câmera?) ou vídeo da queda.
- Gesto do baú: agachar e FICAR (canalizar sustentado), não agachar-levantar
  em loop. Pede clipe próprio (Meshy anim library / Blender).
- Biblioteca de animação Meshy: baixar cair/planar/pegar/derrubado/nadar para
  Pyra e Brok (grátis no webapp); o que não existir, animar no Blender.
- Layout dos botões AJUSTÁVEL pelo jogador (edição de posição na tela).
- Timer "3:00" e "BOTS 0" aparecem no treino — esconder/trocar por "TREINO".
- Sons com profundidade: magias, água, corpo (o Diretor pediu; hoje é síntese).
- Silhueta do cenário de fundo lê como PRÉDIOS no horizonte (q04/q06) —
  quebra fantasia; revisar as rochas de fundo.

**Concept art:** o Diretor vai gerar imagens por IA com prompts nossos ANTES de
qualquer crédito Meshy — prompts entregues no chat de 26/08 (castelo, luvas,
baú). Regra: imagem aprovada por ele → multi-image no Meshy.

---

### (26/08, tarde — as 4 fases da DIREÇÃO, histórico)

Ordem do Diretor: *"faça todas as fases necessárias"*. Quatro fases entraram,
cada uma com teste provado em vermelho:

**1. A Lei das Luvas.** Todo slot nascia com varinha — "desarmado" não existia
no jogo. Agora TODOS caem de mãos nuas; sem luva não há ataque básico (tática/
suprema são natas); o bot desarmado CAÇA a luva mais próxima (grupo
`loot_arma`) e o auto-upgrade equipa na chegada; o player NÃO tem auto-pegar
(decisão nº 5 — o botão vira TROCAR com luva na mão); loot pego de mãos nuas é
CONSUMIDO (o swap deixaria "arma vazia" fantasma no chão). Nomes de exibição:
Luva Comum / Luva de Conjurador / Manopla — ids internos intactos.

**2. O lobby de TREINO** (decisão nº 19). Botão no menu; mesma cena da partida
com pedido via static consumida no `_ready` do Main (sobrevive à troca de cena;
o consumo impede herança — a prova em vermelho derrubou 5 testes em cascata sem
ele). Sem zona, sem relógio, sem queda; as 3 luvas expostas no spawn; 2 bonecos
que regeneram; suprema em 5 s. Tutorial não-obrigatório: fase futura.

**3. O castelo cruza o céu VAZIO** (decisão nº 14). O corpo fica invisível de
`_ready` até `saltar()` — surge do nada, sancionado. Vale para player e bots
pelo mesmo caminho.

**4. A água deixou de ser cenário.** Lâmina no peito (1,2 m) → nadar: 55% da
velocidade no PRODUTO ÚNICO, corpo FLUTUA (gravidade não puxa ao leito). Sair
= roupa encharcada (80% por 2,5 s). Quem responde onde há água é a ilha
(`agua_y`), pelas MESMAS cotas que desenham a lâmina (agora constantes).
**Pendente da água:** MERGULHAR para esconder (pede botão novo), atirar só na
superfície, animação de nado.

**Ainda sem código, da DIREÇÃO:** braço ergue/abaixa + recarga da luva
(animação); bots usando tática/suprema; sons de passos/respiração (os canais de
percepção já existem — falta o áudio); tutorial básico.

**Armadilha de teste registrada:** `queue_free()` num Main de teste deixa a
ilha REAL viva (adiada) no grupo "ilha" até o fim do frame — o teste seguinte
pergunta a lâmina para a ilha errada. Em teste, `free()` imediato.

---

### (26/08, meio-dia — a DIREÇÃO nasce, histórico)

O Diretor respondeu a entrevista de 20 perguntas sobre luvas, habilidades,
castelo, água e bots. **Tudo está em [DIRECAO.md](DIRECAO.md)** — até ele colar
as emendas no GDD, aquele arquivo é a palavra dele por escrito e nenhuma
implementação pode contradizê-lo. O que já virou código, com teste vermelho:

**a) FFA + percepção dos bots.** `Main.gd` cravava `b.target = player` no
nascimento — "mesmo sem me mexer eles já me notam" não era IA agressiva, era
ausência de percepção. Agora o bot nasce cego e NOTA por quatro canais com
contra-jogada: VISTO (< 12 m), OUVIDO (< 18 m só em movimento — ficar parado
esconde), DISPARO (< 30 m, `Bus.disparo` emitido por `Projectile.launch`, o
ponto único de todo tiro) e REVIDE (tomar dano ensina quem bateu). Todos contra
todos: bot caça bot.

**b) A suprema virou CARGA 0→100%.** Cooldown morreu. Começa VAZIA na queda
("senão fica muito roubado"), enche com o tempo (`suprema_carga` s por mago) e
acelera com dano CAUSADO (`Kits.CARGA_POR_DANO`, modelo Apex por ordem dele).
O botão da HUD mostra a porcentagem (`AcaoButton.mostra_carga`). Dano RECEBIDO
não enche — apanhar não é bateria.

**c) Táticas na faixa 5–10 s** (agressão no teto): Pyra 14→9, Véu 12→6, Tessa 7
(mantido — decisão anterior deliberada). Tabela dos 20 em DIRECAO.md §4.

**Um teste passava VACUAMENTE e foi consertado:** o "braço frio" da Pyra exigia
`tatica_cd >= 4 s` após a suprema — passava porque o cooldown antigo de 14 s
ainda estava correndo (sobrava 6,4 s) e mascarava o esfriamento. Com a tática em
9 s a sobra é 1,4 s e o `maxf` do esfria é quem levanta para 4 — o check agora
prova o que sempre disse provar (a tolerância virou 1 tique, não 0,01).

**O que a DIREÇÃO manda e AINDA NÃO tem código** (ordem sugerida):
1. Luvas como forma física (varinha/cajado → luva; PEGAR vira TROCAR com arma
   na mão; braço ergue ao disparar/abaixa no cansaço; recarga da luva).
2. Lobby de treino (decisão nº 19 — destrava o teste do próprio Diretor).
3. Castelo sem bonecos + surgir ao acionar (decisão nº 14).
4. Água: nadar do peito, tiro na superfície, mergulho esconde, saída lenta.
5. Bots nas leis do jogador: caem sem luva, acham loot, usam tática/suprema.
6. Sons de passos/respiração (a percepção já tem os canais; falta o áudio).

---

### (26/08, noite — animação derrubado, histórico)

**ACHADO QUE MUDA PRIORIDADE — leia antes de planejar arte 3D.**

Lendo o JSON dos `.glb` direto: **a Pyra tem TRÊS animações** (idle, run, cast) e
**o Brok tem CINCO** (Idle_02, Run_02, Running, Walking, mage_soell_cast).
**Nenhum dos dois tem `cair`, `planar` nem `pegar`.** Nos modelos 3D reais essas
animações caem no fallback: `pegar` vira `cast`, `cair` vira `idle`.

Consequência: **os dois personagens "prontos" em 3D são os mais pobres em
animação do elenco.** O gesto de agachar e a queda que o Diretor elogiou em
26/08 são do **mago procedural**, que 18 dos 20 personagens rodam. Cada modelo
novo da Meshy, do jeito que o pipeline está hoje, *piora* a expressividade em
troca de melhorar a malha.

**O que fazer com isso:** baixar os clipes que faltam da biblioteca de animação
da Meshy (grátis no webapp, segundo `docs/MESHY.md`) e reexportar. Os aliases de
`cair`/`planar`/`pegar`/`derrubado` já estão escritos em `Mage.gd` — no dia em
que o clipe chegar com qualquer nome conhecido, ele é adotado sozinho.

**O estado DERRUBADO agora tem corpo.** Antes o caído usava a animação de
locomoção: rasteja devagar, cai no lado "idle" da histerese, e ficava **de pé e
parado** no meio da partida. O aliado só descobria quem dava para reerguer pela
HUD. Agora: corpo no chão, apoiado num braço, e a cabeça sobe uma vez por volta
— o beat que diz "ainda dá tempo".
- **Onde a decisão mora:** `Pawn.locomotion_anim()`. Player e Bot reescrevem a
  animação todo frame; pedido feito de fora seria apagado no frame seguinte.
  `ArmaSlot.gesto` resolve isso para gesto **transitório** (volta por relógio);
  derrubado é **sustentado** e não tem quando voltar — estado sustentado se
  resolve na fonte da decisão, não driblando ela.

**Dois defeitos nas FERRAMENTAS, achados por usá-las:**
1. `_shot_mago.gd` fotografava só idle/run/cast. As animações escritas à mão
   (`cair`, `planar`, `pegar`) **nunca tinham sido olhadas renderizadas**.
2. A altura do personagem era a **maior malha isolada** — respondia 1,02 m para
   um mago de 1,83 m, e o enquadramento **cortava a cabeça**. Descoberto tentando
   julgar a pose nova: não dava para dizer se o mago estava caído ou de pé porque
   a régua estava errada. São dois casos e não há fórmula única: **malha skinada
   é desenhada pelo esqueleto** e o `global_transform` dela não participa do
   desenho (multiplicar por ele deu 0,018 m para a Pyra, que renderiza em 1,78).
   Regressão conferida: Pyra 1,78 e Brok 1,40, exatamente as fichas.

**A pose de `derrubado` foi reescrita DUAS vezes depois de fotografada** — a
primeira afundava no terreno, a segunda lia como em pé. Calibrar no escuro não
funciona; foi a régua consertada que permitiu acertar.

---

### >>> 26/08/2026 (dia)

**Duas decisões suas foram tomadas e executadas hoje:**
1. **PR #2 mesclado na `main`** (`f03f97e`). O CI rodou de verdade e passou —
   `12 selftests headless`, SUCCESS. Era a única coisa que nem você nem eu
   tínhamos observado até então.
2. **As 160 referências de arte foram para o Git LFS.** Elas existiam em UM lugar
   só, sem cópia, desde que os .zip da entrega foram apagados na higienização.
   Agora têm histórico e backup.
   **O custo, dito de frente:** um clone limpo passou a puxar ~350 MB de LFS. A
   cota gratuita do GitHub é 1 GB de armazenamento e 1 GB de banda por mês —
   cabe, mas são ~3 clones limpos por mês. Se estourar: pacote de dados pago ou
   storage externo. Não há terceira saída.

**O que foi feito depois disso, na branch `feat/artes-em-lfs`:**

**a) O Arkana passou a abrir com a cara dele.** Até hoje a PRIMEIRA coisa que o
jogador via era o **logo do Godot** — `project.godot` não tinha nenhuma chave de
`boot_splash`. E o ícone na gaveta era o retrato de corpo inteiro da Pyra num
quadrado de 96 px. Agora os dois são o **Selo**, o mesmo pentágono da tela de
título. *(Correção de registro: a nota antiga dizia que o ícone era o padrão do
Godot. Não era. Conferido abrindo o APK.)*
- **Como:** o Selo saiu de dentro de `Menu.gd` para `menu/Selo.gd`. Era classe
  aninhada, e quem gera os PNG roda como `--script`, antes dos autoloads — um
  preload de `Menu.gd` ali morre em "Identifier not found: Bus". `menu/_marca.gd`
  renderiza os 4 PNG a partir desse mesmo Selo, para o desenho continuar tendo
  um dono só. É ferramenta de ateliê, fora do APK.
- **Por quê assim:** a alternativa era redesenhar o selo num editor de imagem —
  e no dia em que você mudasse o dourado existiriam duas verdades divergindo em
  silêncio.
- A frente do ícone adaptativo sai em 64% do quadro: o Android mascara os
  432×432 e só garante o miolo.

**b) A sombra voltou a existir na fase que abre a partida.** Vista do castelo a
200 m, a ilha inteira não projetava UMA sombra. `directional_shadow_max_distance`
valia 60 m — calibrado quando a ilha tinha 180 m e ninguém caía do céu.
- **Errei duas hipóteses antes de achar**, e ambas foram descartadas por medida:
  o toon shader (cena de isolamento mostrou que ele recebe sombra igual ao
  material padrão) e o balanço de energia (subir o sol de 1,55 para 4,5 clareou
  tudo e não trouxe sombra nenhuma).
- **O conserto não é aumentar o número:** o atlas tem 2048 px e cobre o alcance
  inteiro. A 60 m o texel é ~6 cm; a 400 m vira ~20 cm e borra a sombra PERTO,
  onde o jogador passa 95% da partida. `world/Sol.gd` faz o alcance **seguir a
  altura** — abre no ar, fecha ao pousar. Um split só; nada reabre PSSM.
- A ferramenta de calibragem também mentia: mantinha o sol em repouso nas
  tomadas aéreas. Corrigida.

**c) `docs/infra/` — a pasta técnica que você pediu.** Servidores, contas, custos
e a regra dos 10+. Ela começa por um fato que muda todas as respostas: **o Arkana
não tem uma linha de rede.** Nenhum `@rpc`, nenhum `multiplayer`, nenhum
`HTTPRequest`. Contratar servidor hoje é alugar garagem para um carro que ainda
não foi construído.
- **O item que ATRASA o lançamento se ficar para o fim:** a conta de loja tem
  verificação de identidade e período de teste fechado que se contam em
  **semanas**. O relógio dela corre em paralelo ao desenvolvimento.
- **Quatro decisões suas ficaram em aberto lá** (`04-DADOS-E-MENORES.md` §5):
  conta de jogador, apelido visível, amizade/grupo, compra dentro do app.

**Por que a Sintonia NÃO foi implementada, mesmo sendo o maior buraco:** o GDD §9
trava — *"mudanças em combate, `Balance` e formato de partida dependentes de
V1–V5 esperam o playtest humano do Roblox"*. Dar um parceiro ao jogador
transforma 1×6 em 2×5: é mudança de formato. A mesma linha libera o que foi
feito: *"personagens, ambientes, pipeline e apresentação do Godot podem
avançar"*. A Sintonia já está inteira no Roblox (1.314 linhas) e o GDD §274 já
decidiu o caso solo — falta o playtest, não o código.

---

### (25/08) — auditoria externa executada, mantida como histórico

Um plano de auditoria em 12 tarefas foi executado na branch
`chore/auditoria-organizacao-arkana`. **Nada foi mesclado na `main`.**

**O que ele consertou de verdade:**
- O auditor de modelos media VERTICE num orcamento definido em TRIANGULO, e
  reprovava os DOIS modelos reais do jogo estando ambos dentro do alvo.
- `Bus.damage_dealt` parecia ter consumidor, mas o gancho vivia num ramo `else`
  que nunca rodava. Removido, junto com o mecanismo de troco que so' existia
  para arredondar o sinal.
- `weapon_equipped` e `bau_canalizando` nao diziam de QUEM eram: a HUD
  adivinhava pelo id da arma, e um bot com a mesma arma mudava o icone do
  jogador. Os dois passaram a carregar o `pawn`.
- Os 6 bots passaram a CAIR do castelo, sob a mesma lei do jogador.
- Os 12 autotestes viraram um comando so' (`godot/tests/run_all.sh`) com CI no
  GitHub. Antes eram 12 linhas copiadas a mao — e comando copiado a mao e' como
  se esquece um teste.

**O que ficou BLOQUEADO, por falta de entrada externa:**
- As 160 referencias de arte: falta o documento mestre da direcao aprovada e o
  manifesto/checksums da entrega. Os 160 PNGs estao integros e com manifesto
  gerado, mas **existem em UM lugar so', sem copia**.

**O que NAO foi executado:** teste no Poco F4 — nenhum aparelho apareceu em
`adb devices`. FPS segue sem medicao neste projeto.

### (25/08, manha — o APK anterior, mantido como historico)

O APK esta' em `godot/build/testes/`. **O que mudou e o que olhar:**

1. **A partida agora comeca NO AR.** O castelo cruza o mapa, um toque salta,
   o corpo cai (~9,5 s de ar, 183 m de alcance horizontal), plana e pousa.
   **Nao ha' magia durante a queda** — ordem dele, e esta' testada.
2. **A ilha quase triplicou** (180 -> 300 m) e ficou mais legivel DO ALTO, que
   e' a vista nova que a queda criou. 7 POIs agora (era 4).
3. **A patinacao dos pes**: as DUAS causas achadas foram corrigidas — o modelo
   entrava virado 180 graus e a animacao importada entrava sem laco. **Falta
   medir no aparelho** se o casamento entre cadencia da animacao e velocidade
   real ainda satura (ver item 12 das dividas). Nao declare resolvido antes.
4. **Pegar item e abrir bau viraram GESTO**, com a faisca saindo no quadro em
   que a mao chega ao chao.

**Pendencias conhecidas, ditas sem maquiagem:** os bots ainda nascem no chao
(nao caem); nao ha' colisao no ar (nao da' para pousar em cima de arvore); quem
mergulhar reto no mar pousa na agua, porque natacao nao existe; e o FPS nunca
foi medido em aparelho nenhum.

---

### (25/08 — RESOLVIDO, mantido como historico)

**O defeito da corrida esta' DIAGNOSTICADO e NAO corrigido.** Deixei parado de
proposito; e' a primeira coisa da manha.

**Sintoma, nas palavras do Diretor apos testar o APK no aparelho:** *"da' apenas
os tres primeiros passos e depois patina, mas agora pelo menos esta' na direcao
correta, ja' fez diferenca"*.

**A direcao ja' foi corrigida** (commit `a6efe3f`): o modelo entrava virado 180
graus porque glTF olha para +Z e o Godot anda para -Z. Isso esta' resolvido e
tem teste de regressao.

**O que sobrou — a patinacao — tem causa confirmada:**
`Mage.gd` so' define `loop_mode` nas animacoes PROCEDURAIS (ver `_new_anim`,
que recebe `looped` e aplica `LOOP_LINEAR`). Para o modelo EXTERNO (.glb) nao
existe nenhum tratamento de laco, e o importador glTF do Godot traz as
animacoes com `LOOP_NONE` por padrao. Resultado: o clipe "Running" toca UMA vez
— sao uns tres passos —, congela no ultimo quadro, e o corpo continua
deslizando. Isso e' exatamente a patinacao descrita.

**O conserto (nao aplicado):** em `_build_imported_model()`, depois de resolver
os aliases, marcar `LOOP_LINEAR` nas animacoes de `idle` e `run`. **`cast` NAO
pode entrar no laco** — e' disparo unico e o `cast_fired` depende do fim dela.
Deixar teste de regressao que fique vermelho sem o laco, como manda a regra.

**Cuidado ao medir depois:** havia um SEGUNDO suspeito para a patinacao — o
teto de `Balance.ANIM` saturando num anao de 1,40 m que usa clipe feito para
~1,70 m (`Pawn._sync_anim_speed`). Com o laco consertado, medir de novo ANTES
de mexer nesse numero: pode ja' nao ser necessario.

---

### O que ja' foi feito
O jogo e' **jogavel de ponta a ponta no Android**: menu, selecao de mago,
partida contra 6 bots, zona que fecha, habilidades, escudo, estado derrubado,
armas arcanas, bau, terreno reativo com 6 reacoes, HUD completa e audio
sintetizado. **A Pyra tem modelo 3D real** (Meshy, versionado em LFS). Os 20
magos tem ficha aprofundada e **conjunto completo de concept art** (8 vistas
cada, entregue em 24/08). O Roblox segue como campo de provas multiplayer.

### Como foi feito
Raias paralelas com **donos exclusivos de pastas**: o coordenador escreve os
contratos (`core/Balance.gd`, `core/Bus.gd`) ANTES de despachar e faz as
costuras na integracao. **Todo teste novo se prova reintroduzindo o defeito** —
se nao fica vermelho, e' decorativo. Fiacao defensiva: cada cena boota sem as
outras. Gate de entrega: selftests + boot + APK com o conteudo LISTADO dentro
do pacote (nunca confiar em timestamp).

### Por que foi feito assim
O pivo de 19/08 ("quero ver realidade de jogo, nao mapa de teste") levou o
projeto para Godot 3D. **O GDD e' a unica ponte**: o Roblox valida regras com
gente, e nenhum numero de combate muda sem dado humano. As regras duras
(fogo por orcamento, dano num ponto so', dp para o dedo, cancelar como estado
de primeira classe) nao sao estilo — sao cicatrizes de defeitos medidos.

### Ainda falta

**So' o Diretor pode:**
1. **Decidir a direcao de arte dos personagens** (ver secao 4, item 4).
2. Testar o APK no aparelho e dar o veredito.
3. Aprovar ou cortar os kits 11–20 para subirem ao GDD.
4. Playtest do Roblox com gente de verdade (V1–V5). Login e publicacao sao ato dele.
5. Contratar servidor e contas de loja quando chegar a hora (G5/G6) e responder o IARC.

**A equipe faz.** As pendencias estao separadas em TRES GRUPOS, porque
misturar correcao com decisao de produto foi o que fez o proximo agente
implementar por inferencia:

### GRUPO 1 — correcoes tecnicas reproduziveis (pode fazer)
1.1. **Medir a patinacao no aparelho.** As duas causas achadas foram corrigidas
   (modelo virado e animacao sem laco). Falta medir se o casamento entre
   cadencia e velocidade satura o teto de `Balance.ANIM` num anao de 1,40 m
   usando clipe feito para ~1,70 m.
1.2. **Corrigir os angulos das vistas** da concept art: nao ha' perfil de 90
   graus no lote, e o "3/4" e' a frontal repetida. Trava o multi-imagem da Meshy.
1.3. **Texturas do Brok**: duas de 2048. Consolidar SO' se material e comparacao
   visual provarem que uma e' redundante.

### GRUPO 2 — funcionalidades ja' especificadas (pode fazer, com teste)
2.1. ~~Bots caem do castelo~~ **[FEITO 25/08]** — os 6 caem sob a mesma lei do
   jogador, com 8 checagens no selftest da queda.
2.2. **Animacao `derrubado`**: hoje o caido usa a de locomocao mais lenta. Entra
   como OPCIONAL, com o mesmo mecanismo de `cair`/`planar`/`pegar`, para nao
   derrubar modelo que nao tenha o clipe.
2.3. **Os 17 kits que faltam** — o maior buraco de jogabilidade.
2.4. **A Sintonia** — um dos dois pilares, sem uma linha em codigo.
2.5. **Modelo 3D dos magos restantes.** Caminho provado: 30 creditos cada.

### GRUPO 3 — DECISOES DO DIRETOR (nao implementar por inferencia)
Cada uma esta' na secao 4 com as opcoes e o impacto. Enquanto nao houver
resposta, o comportamento atual permanece e NAO deve ser "melhorado" sozinho:
dano cancelar o bau, resultado do timeout, tabela da zona, colisao aerea,
mergulho no mar, manopla da Pyra, armazenamento das artes, remocao da branch
antiga, protecao da main e contas de publicacao.

### O que sobrou fora dos grupos
 — e' o maior buraco de jogabilidade (secao 3).
- ~~A Sintonia~~ (ver 2.4) — um dos dois pilares de identidade, sem uma linha em codigo.
- ~~Angulos das vistas~~ (ver 1.2) da concept art: nao existe perfil de 90
   graus no lote, e o "3/4" e' a frontal repetida. Isso trava o multi-imagem
   da Meshy e independe da decisao de estilo.
- ~~Modelo 3D dos restantes~~ (ver 2.5) **O caminho esta provado de ponta a ponta**
   (25/08): concept -> Meshy multi-imagem -> remesh 15k -> rig -> animacoes ->
   .glb -> jogo. Custo medido: **30 creditos** por personagem (remesh, rig e
   animacao saem de graca no webapp; na API o remesh custa 5).
- **[FEITO 25/08]** ~~teto de vertices do model_audit~~ — agora mede TRIANGULO: ele reprova em 20.000 e
   **os dois modelos reais reprovam** (Pyra 22.561, Brok 29.212) mesmo estando
   dentro do orcamento de 15k triangulos. Costura de UV multiplica vertice — o
   teto foi escrito na metrica errada.
- ~~Texturas do Brok~~ (ver 1.3): o Brok trouxe duas de 2048 (13,6 MB no
   pacote). TECH_ART pede 2K para hero, entao passa, mas duas e' desperdicio.
5. Vozes (560 falas escritas, nenhuma gravada) e arte de UI (68 prompts prontos).
   **Bloqueante para publicar:** o APK ainda usa o icone padrao do Godot.

---

## 1. O produto

Arkana é um **battle royale de magos** em terceira pessoa, para Android. Dois
pilares de identidade:

1. **Sintonia** — dois jogadores combinam elementos numa magia conjunta.
2. **Terreno reativo** — fogo, água, gelo, raio, terra e vento mudam rotas,
   cobertura e risco durante a partida.

O [GDD](GDD.md) é a **fonte da verdade de produto**: toda decisão de design entra
nele antes de virar código. Os números de jogo vivem em `godot/core/Balance.gd`
(combate, terreno, movimento) e `godot/core/Kits.gd` (habilidades).

---

## 2. Onde o projeto está HOJE

O jogo **é jogável de ponta a ponta** num celular Android: menu → seleção de
personagem → partida com bots → fim de partida. O APK de debug sai em
`godot/build/arkana3d.apk`.

### Funcionando e testado

| Sistema | Estado |
|---|---|
| Ilha 3D, terceira pessoa, partida contra 6 bots | ✅ |
| Controles de toque (joystick, mira por gesto, esquiva) | ✅ |
| 5 elementos + terreno reativo | ✅ |
| **Zona que fecha** (5 fases, 90m → 4m em 165s) | ✅ |
| **Habilidades**: passiva/tática/suprema | ✅ sistema + 3 kits (Pyra, Véu, Tessa) |
| **Escudo de Magia Evolutivo** (GDD §5) | ✅ |
| **Estado derrubado + reerguer** | ✅ |
| **Armas arcanas** (varinha, cajado, manopla) + loot no mapa | ✅ |
| **Baú Celestial** (única fonte da manopla) | ✅ |
| Efeitos elementais no alvo + 6 reações | ✅ |
| HUD completa (cooldowns, escudo, zona, baú, derrubado, dano) | ✅ |
| Menu, Configurações (GDD §12), seleção dos 20 magos | ✅ |
| Áudio sintetizado (48 timbres, zero arquivo de áudio) | ✅ |
| **Pyra em 3D** (15.492 tris, PBR, riggada, 3 animações) | ✅ |
| **Brok em 3D** (15.424 tris, 1 malha, 1 material, 24 ossos, 5 animações) | ✅ |

### Verificação

Cada pasta tem seu `selftest.gd`. Rodar todos antes de qualquer entrega:

```bash
bash godot/export/build_apk.sh
```

Self-tests individuais:
```bash
"$LOCALAPPDATA/Programs/godot/Godot_v4.4.1-stable_win64.exe" --headless --path godot --script res://gameplay/selftest.gd
```
Existem: `gameplay`, `gameplay/selftest_kits`, `gameplay/selftest_zona`,
`gameplay/selftest_derrubado`, `ui`, `menu`, `characters`, `world`, `terrain`,
`audio`, `juice`.

---

## 3. O que FALTA (em ordem de importância)

### Bloqueiam a experiência
1. **17 dos 20 magos não têm kit.** O sistema está pronto e cadastrar um mago são
   3 passos (`core/Kits.gd` → `gameplay/habilidades/<nome>.gd` → registrar em
   `KitRunner.IMPL`). Hoje quem não tem kit joga sem tática nem suprema.
2. **A Sintonia não existe** (GDD §9) — é um dos dois pilares de identidade do
   jogo e não há uma linha dela em código.
3. **Só Pyra e Brok tem modelo 3D.** Os outros 18 usam o mago procedural.
   Pipeline pronto em `tools/meshy/` (ver [MESHY.md](MESHY.md)).
   ⚠️ **As vistas da concept art tem defeito de angulo** (nao ha perfil de 90
   graus; o "3/4" e' a frontal repetida), entao o fluxo multi-imagem da Meshy
   perde a espessura lateral do corpo. Corrigir antes de escalar o elenco —
   ver `personagens/00-LEIA.md`.

### Qualidade e conteúdo
4. **Vozes**: 560 falas escritas em `audio/vozes/`, **nenhum áudio gerado**
   (conferido: `grep -hcE '^[0-9]+ \[' audio/vozes/*.txt` — 20 magos x 28). O
   fluxo está no `00-LEIA.txt` de lá (ElevenLabs).
5. **Arte de UI**: 68 prompts prontos em `docs/prompts-arte/`, nada gerado.
   ⚠️ **Bloqueante para publicar**: o APK ainda usa o **ícone padrão do Godot** —
   faltam o ícone do app e a splash (peças L6/L7 e M2 dos prompts).
6. **Runas de aprimoramento** (GDD §16.3): estrutura declarada, não implementada.
7. **Espírito Errante** (§18.6), **Presságios** (§18.4), **Grimório** (§18.3):
   ideias aprovadas, nada em código.

### Dívidas técnicas conhecidas
8. `Bus.damage_dealt` está **sem consumidor de produção** (áudio e HUD migraram
   para `damage_applied`). Pode ser removido junto com `Combat._emitir_legado`.
9. `weapon_equipped` e `bau_canalizando` **não dizem de quem são** — a HUD filtra
   por gambiarra comparando com o slot do player.
10. `Balance.PLAYER.jump` é KNOB **órfão**: nenhum código usa. Ou entra um botão
    de pulo, ou o número sai.
11. Falta animação `"derrubado"`; hoje o caído usa a de locomoção mais lenta.
12. **A patinação dos pés: duas causas corrigidas, uma por medir.** Corrigidos
    a meia-volta do modelo (corria de costas) e o laço ausente da animação
    importada (tocava três passos e congelava). Falta medir se o casamento entre
    cadência da animação e velocidade real (`Pawn._sync_anim_speed`) é limitado
    por um teto em `Balance.ANIM`. O Brok tem 1,40 m e usa um clipe de corrida
    da biblioteca da Meshy provavelmente feito para ~1,70 m: se a razão
    necessária estourar o teto, a correção satura e a patinação volta. **Medir
    com o Diretor no aparelho antes de mexer no número.**
13. **Não há física de corpo ainda** (observação do Diretor, 25/08): parte do
    que parece defeito de animação pode ser o corpo escorregando no terreno.

---

## 3.5 Decisão de sequência: FUNCIONALIDADE antes de ARTE DE CENÁRIO

**Decidido pelo Diretor em 25/08/2026**, com estas palavras: *"depois de
validarmos funcionalidades, seria interessante começar a desenvolver mais em 3D
a ilha, árvores, mais ambientes, deixar mais bonito assim como estamos
melhorando os personagens"*.

**A ordem, portanto:** primeiro o jogo funciona (queda, interação, kits,
Sintonia); só depois o cenário ganha modelagem 3D de verdade.

**Por que isso não é adiar por preguiça** — os quatro custos foram levantados
antes da decisão:

1. **Desenhos por quadro.** As 158 árvores custam praticamente UM desenho hoje,
   porque compartilham a mesma malha procedural via MultiMesh. Modelo importado
   traz material próprio e quebra isso. É o risco número um no celular.
2. ~~**Tamanho do pacote.**~~ **ARGUMENTO DERRUBADO PELO DIRETOR em 25/08.**
   Palavras dele: *"o tamanho do APK não importa hoje... o importante é validar
   com o que é real... eu tenho mais de 20 GB disponíveis"*. **Não use peso de
   pacote como motivo para recusar qualidade enquanto o jogo está em
   desenvolvimento.** Compressão e formato de publicação viram tema quando as
   projeções fecharem, não antes.
   O fato técnico continua verdadeiro (a ilha é zero binário; foi por isso que
   o APK ficou em 54 MB) — o que mudou é que ele **não decide nada agora**.
3. **O fogo depende das árvores atuais.** `tree_count()/tree_pos()/
   set_tree_burned()` são o que permite queimar a floresta e abrir caminho —
   pilar do GDD §14. Trocar a árvore obriga a refazer essa fiação.
4. **Coerência.** O elenco já tem três linguagens visuais (auditoria de 25/08).
   Um cenário esculpido ao lado de terreno cel-shaded arrisca criar a quarta.

**Quando chegar a hora, o plano é UMA FAMÍLIA POR VEZ, começando pelas
RUÍNAS** — não pelas árvores. As ruínas dão o maior salto de "parece jogo
publicado" por crédito e **não estão presas a sistema nenhum**: são cobertura e
cenário, e se derem errado joga-se fora sem quebrar nada. Árvore é o oposto:
maior impacto visual, mas é a peça que o fogo usa.

**Antes de gastar crédito, olhar o Discover CC0** — o próprio pacote de
pipeline manda (`docs/pipeline-arte/MESHY/01-DISCOVER_CURADORIA.md`) e já lista
URLs de ruínas, muro de pedra e cristal arcano em CC0. Custo zero.

**Meshy NÃO gera terreno.** Forma da ilha, alturas, biomas e cores são código
procedural e continuam sendo. Meshy faz os OBJETOS que vestem o terreno.

**O teste que decide, sempre:** uma família, dentro do jogo, no celular do
Diretor, medindo desenhos por quadro e tamanho do pacote antes e depois.

---

## 4. Decisões que precisam do Diretor

1. **A tabela de fases da zona é PROPOSTA, não spec.** O GDD *pressupõe* a zona
   em três lugares (roadmap, kit do Vidente, resgate de espírito) mas nunca a
   especificou. Os números em `gameplay/Zona.gd` foram projetados pela equipe.
2. **Fim de partida**: com a zona, o timer de 180s virou rede de segurança e o
   fim de verdade é "último em pé". O que acontece ao esgotar o tempo (empate?
   vitória por sobrevivência?) continua não decidido.
3. **Fidelidade dos modelos 3D**: a geração por IA acertou o corpo da Pyra mas
   **perdeu a manopla de bronze**, que é a assinatura dela. Ver
   [PASSOS_GRAFICOS.md](PASSOS_GRAFICOS.md) §9 para os três caminhos possíveis.
4. **A direção de arte dos personagens.** O Diretor pediu sair do "muito
   realismo" para um "3D mais detalhado de alto padrão". A auditoria de 25/08
   mediu que o elenco tem **três linguagens** (7 escultura 3D, 7 pintura
   semi-realista, 6 anime) e que **o eixo não é realismo**: o rosto do Brok,
   que ele aprovou, é mais realista que o da Pyra, que ele rejeitou. O eixo
   real é superfície pintada × esculpida, sombra de contato e proporção
   exagerada. **Perigo:** pedir "menos realismo" a um gerador empurra para
   anime, que já é o grupo que mais quebra o elenco. Evidência completa em
   `personagens/00-LEIA.md` e `docs/ART.md`. **Nada foi decidido.**

---

## 5. Como o repositório está organizado

```
godot/          o jogo (produto principal, Godot 4.4.1 → Android)
  core/         contratos e números: Balance, Kits, Bus, Textos
  gameplay/     partida, combate, zona, loot, baú, derrubado, habilidades/
  characters/   o mago (procedural + modelo externo .glb)
  world/        a ilha procedural e os shaders
  ui/           HUD, botões de toque, área segura
  menu/         menu, configurações, seleção de personagem
  audio/        SFX sintetizados
  export/       build_apk.sh e o setup de máquina
roblox/         "Campo de Provas" — frente ativa de validação humana
personagens/    as 20 fichas + o ateliê de arte de cada um
docs/           GDD (fonte da verdade) e os estudos
  pipeline-arte/  o pacote de pipeline Meshy (concept -> 3D), material de apoio
audio/vozes/    as 560 falas para gerar no ElevenLabs (20 magos x 28)
tools/meshy/    pipeline concept art → personagem 3D riggado
infra/          planejamento de servidores, custos e distribuição
```

### O que NÃO é versionado (e por quê)
- `personagens/*/arte/_originais/` — o **ateliê de arte**: 8 vistas por mago,
  307 MB. É matéria-prima da modelagem, não produto. O produto versionado é o
  retrato de 512px em `godot/menu/art/NN.png`, que entra no APK.
- `godot/build/` — APKs, artefato gerado.
- `godot/characters/modelos/<slug>/` — a **oficina 3D** (malha crua, FBX,
  texturas soltas, animações separadas). São ~102 MB por personagem e
  **regeneráveis** com dois comandos do `tools/meshy/`. Só o `<nome>.glb` que o
  jogo carrega é versionado.
- `tools/meshy/.env` — a chave da API Meshy. **Nunca commitar.**

---

## 6. Frente paralela: Roblox

`roblox/` é o **Campo de Provas**: o ambiente multiplayer para testar com pessoas
o que bots não validam — Sintonia, TTK, leitura do terreno, equilíbrio dos
elementos e vontade de jogar de novo. Tem servidor autoritativo, duplas, terreno
reativo, os dez combos de Sintonia, loop de BR, bots, acessibilidade e telemetria.

**O bloqueio não é técnico:** falta executar o playtest humano descrito em
[ROBLOX.md](ROBLOX.md).

---

## 7. O que foi encerrado

O protótipo 2D em Phaser/TypeScript/Capacitor foi descontinuado em 19/08/2026.
Validou as primeiras mecânicas e o toque, mas não é frente de produto e não
recebe manutenção. Continua recuperável pelo histórico do Git.

---

## 8. Linha do tempo

| Data | Marco |
|---|---|
| 17/08 | Do zero ao protótipo jogável (2D), com 4 raias em paralelo |
| 19/08 | 2D encerrado; elenco cresce para 20 magos |
| 20/08 | Consolidação em Godot 3D + Roblox; concept art dos 20 |
| **21/08** | **R20/R20.1** — elenco descolado do Apex com marcas mágicas próprias; Pyra em 3D pela Meshy; zona, habilidades, escudo, derrubado, armas arcanas, baú e HUD completa; área segura corrigida; estudo de dano |

| 24/08 | Novo lote de concept art dos 20 magos (8 vistas cada) |
| **25/08** | **A QUEDA**: castelo voador, salto, planeio e pouso · ilha 180 -> 300 m com 7 POIs e leitura aerea · gesto de pegar item e abrir bau · fim da patinacao · zona e terreno passam a ESCALAR com o mapa |
| **25/08** | **Brok em 3D pela Meshy** (multi-imagem + rig + 5 animações), APK de 106 MB → 54 MB ao excluir o ateliê da exportação; **higienização**: -398 MB em duplicatas e material superseded; ateliê de arte fora do git; pipeline Meshy achatado em `docs/pipeline-arte/`; `meshy.py` consertado; auditoria visual dos 20 e da validade do pacote de pipeline |

Detalhe de cada mudança: `git log` e o [CHANGELOG](../CHANGELOG.md).
