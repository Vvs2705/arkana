# PROJETO — estado atual do Arkana

> **Este é o documento de memória do projeto.** Quem chegar agora deve conseguir
> ler só este arquivo e entender: onde o projeto está, o que já foi feito, o que
> falta e o que está bloqueado. Atualize-o ao fechar cada fase.
>
> **Atualizado em:** 12/09/2026, manhã (os 20 magos reais, o kit sem buracos com as 7 peças da oficina e o Altar de Sintonia no jogo; falta medir no aparelho)
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

### >>> COMECE POR AQUI — os 20 magos REAIS, o kit sem buracos e o Altar estão no jogo (foto); falta jogar no aparelho (12/09/2026, manhã)

**Próximo passo, na ordem:** instalar o APK novo no Poco F4 e medir FPS com o
elenco real e as 15 peças do kit (`--es arkana_auto partida`); se cair, o
primeiro corte é o LOD dos magos distantes. Depois: baú/luvas pela receita do
kit, luz e pós (passo D, que custa FPS: só com o aparelho medindo). Se o
procedural ainda destoar no celular, a troca boa é um remesh de 1K da rocha do
kit (grátis no site) para os pedregulhos. A seção "LEVA 4", depois
do passo A, tem a receita inteira do site.

**09/09/2026.** Ordem do Diretor: *"quero desistir da ideia de fazer para
Steam... tudo que estava sendo feito no Godot eu quero que seja adaptado para
ser feito com o Unity, porque se ele faz os dois projetos ao mesmo tempo não faz
sentido manter vários projetos... não pretendo mudar mais agora com o
conhecimento do Unity e como ele funciona. O que tiver que ser refeito ou
criamos do zero faz parte, são processos."* E em seguida: *"deixe para montar
um novo APK mais para frente quando fizermos mais coisas... avançar tanto
quanto avançamos no Godot."*

Uma pessoa, dois jogos (este e o Limiar), **uma engine**. Esta é a última troca
de engine do projeto.

#### O QUE FOI FEITO (09/09 → 11/09): `mobile-unity/` existe e passa no portão

| | |
|---|---|
| **Projeto** | `mobile-unity/` — Unity 6000.3.23f1, URP, Input System, uGUI, Test Framework; Android IL2CPP/ARM64, minSdk 26, `br.com.vstack.arkana` |
| **Portão** | `powershell -File mobile-unity\portao.ps1` → **315 testes, 0 falhas** (285 EditMode sobre classes puras, com a altura de cada um dos 20 magos medida na malha deformada + 30 PlayMode: os que montam a arena inteira e rodam 3 s sem um log sequer, e as fotos) |
| **Fotos** | `powershell -File mobile-unity\foto.ps1 [filtro]` → as fotos do jogo rodando em `mobile-unity/Logs/fotos/` + `diag.txt` (o que a câmera, o corpo e os kits tocam). Sem filtro roda todas (~15 min, as 20 folhas de clipes pesam); com filtro, só o que a leva mexeu, ex. `.\foto.ps1 "Foto_Kit\|Foto_Menu"`. **Toda leva visual termina olhando as fotos** |
| **Sistemas reescritos** | Core (Balance com todos os números, Kits dos 20, Combat num ponto só, Velocidade como produto único, Vitalidade, Textos, Bus) · Mundo (Relevo procedural de 600 m com 7 POIs em fração do raio e 14 nascimentos, Ilha com malha e colisor, Vegetação por célula, Castelo com N passageiros por seed, Sol) · Partida (Zona que nasce inerte e liga no pouso, Queda, luvas/loot/Baú Celestial, Derrubado/esvaecer/reerguer, Projétil, Efeitos, Água, Locomoção com dodge/pulo/flutuar, Pawn/Player/Bot com percepção de 4 canais, câmera no ombro, loop de partida e TREINO) · Kits (KitRunner com carga da suprema e telegrafia grampeada; Pyra, Véu, Tessa) · Terreno reativo (fogo por orçamento com 1 rolagem por aresta, carvão, gelo, elétrico por água conectada, muro, lama, vento) · Personagem (mago procedural com 10 clipes por código, identidade dos 20, luva visual) · UI (gesto único em dp, joystick, HUD completa observando o Bus, avisos, menu, config persistida, seleção dos 20, selo) · 48 timbres sintetizados |
| **APK** | **GERADO E JOGADO no Poco F4 em 12/09.** `build_apk.ps1` → 176 MB, 5 min 20 s a primeira vez, 1 min 33 s incremental; cópia datada em `mobile-unity/Builds/testes/` (fora do git) |
| **FPS no aparelho (a dívida de 25/08, PAGA)** | **60 FPS sustentados, quadro de 16,6 ms, pior quadro 33 ms, 36,9 °C**, sem erro nem exceção no logcat, numa partida inteira: castelo, queda, pouso, 12 bots em FFA, tempestade. A tela do Poco estava em 60 Hz: o vsync segura aí; a folga real só aparece com a tela em 120 Hz |
| **Visual** | seis shaders próprios (toon do chão com textura de detalhe, grama instanciada, água com ondas e espuma, céu de entardecer, bruma, toon do mago), grama/flores/juncos por célula, as 8 peças do kit plantadas, ruínas, mar até o horizonte, castelo e luvas `.glb` do Godot via glTFast, 20 retratos, loot/baú/tempestade/kits/terreno desenhados, sol do Godot, espaço de cor Linear |

**Como foi feito.** Sete raias em paralelo (Core, Mundo, Gameplay, UI; depois
Personagem, Pawn, Kits+Terreno; depois Cena e a costura), cada uma dona de uma
pasta, codificando contra um contrato escrito antes
(`mobile-unity/ARQUITETURA.md`) e lendo o `.gd` correspondente como referência
de COMO — nunca traduzindo. Como só cabe **um Unity por vez** no projeto, as
raias validaram com o Roslyn do próprio Unity fora do editor (`csc.dll` via
`NetCoreRuntime\dotnet.exe`) e rodaram os testes num mini-runner sobre o Mono; o
portão oficial foi sempre do coordenador. Toda lógica em classe pura com
`Tick(dt)`; `MonoBehaviour` só como casca — é isso que deixa 229 testes rodarem
headless em três minutos.

**Por quê assim.** O Godot deixou 21 mil linhas e 12 autotestes que diziam O QUE
se cobra; o Unity herdou os invariantes, não o código. Cada teste novo se provou
reintroduzindo o defeito (o próprio reporte das raias registra: "sem a guarda
de estado, o teste fica vermelho").

#### LEVA 3 (11/09): o que se VÊ — e a regra nova de olhar a foto

**A regra que esta leva deixou:** teste verde não diz se o mago saiu rosa, se a
ilha ficou preta ou se a HUD saiu da tela. `powershell -File mobile-unity\foto.ps1`
sobe o jogo com GPU e grava PNGs em `mobile-unity/Logs/fotos/` (menu, treino,
mago de perto, ilha do alto, chão rasante, castelo, queda, pouso, zona do alto,
jogador na zona), na proporção e na densidade de tela do Poco F4. **Toda leva
visual termina olhando as fotos.** Foi assim que o Godot fazia (`_shot.gd`), e
foi assim que esta leva achou onze defeitos que nenhum dos 257 testes pegava
(cada um virou teste, provado em vermelho, e o `diag.txt` nasceu porque dois
palpites sobre um quadro estranho erraram antes de o dado chegar):

| Defeito visto na foto | Causa | Conserto |
|---|---|---|
| Fogo, gelo e muro nunca aconteciam na partida | o terreno reativo só existia nos testes: nenhuma cena o criava | `Main.Montar` cria o `TerrenoReativoBehaviour` |
| Loot, baú e tempestade invisíveis | só existiam como dado; ninguém desenhava | `VisualDaPartida` (luvas `.glb`, coluna de luz por raridade, baú caindo com rastro, parede da tempestade, anel do próximo círculo) |
| Kits e terreno invisíveis | idem | `VisualDosKits` e `VisualDoTerreno` (e o muro virou cobertura: segura tiro de verdade) |
| Duas faixas escuras de borda dura cortando o mundo | a "grade de leitura" da HUD era retângulo chapado (o Godot fazia igual) | degradê suave |
| "VOLTE PARA A ZONA" logo após o pouso | a Zona contava "fora" durante os 70 s de abertura, quando a tempestade não existe (o Godot fazia igual) | só conta com a tempestade ativa; teste provado em vermelho |
| Mar acabava num vazio cinza | plano finito | mar de 6 km que segue a câmera; céu sem chão cinza |
| Ilha lavada de névoa vista do alto | névoa linear fixa — a lição G5 do Godot tinha se perdido | névoa acompanha a altura de cada câmera que desenha |
| Câmera colada na torre do castelo; na queda olhando o horizonte | braço de 26 m num castelo de 35,8 m; 7° de inclinação | 70 m e 43° no castelo; 9 m e 32° na queda (KNOB, calibrar no aparelho) |
| Peças do kit estilhaçadas, chão aparecendo por dentro | a malha decimada da Meshy tem triângulo com a volta trocada e o glTFast só desenha um lado | material URP Lit com os dois lados |
| Mago pousava DENTRO de uma rocha | a Queda só conhecia o terreno ("pouso sempre no terreno, sem telhado") | `ChaoComObstaculos`: a Queda vê o topo do que estiver no caminho; peças do kit com casco convexo (a malha crua deixava o raio atravessar o topo) |
| Câmera dentro da rocha com o mago encostado nela | o ombro da câmera (0,78 m ao lado) entrava na parede e o cast que começa dentro não a reporta como parede | colisão em três camadas + **câmera em canto sobe e olha de cima** (a saída clássica de toda 3ª pessoa) |

**O que entrou além disso:** seis shaders próprios em `Resources/` (toon do
terreno com textura de detalhe, grama instanciada, água com ondas e espuma de
margem, céu de entardecer com nuvens, bruma, toon do mago); grama, flores,
juncos e seixos por célula; as 8 peças do kit de cenário plantadas pelo mapa;
ruínas e rochedos no mar; o castelo e as luvas `.glb` do Godot (glTFast 6.20);
os 20 retratos do menu; o sol quente do Godot; projeto em espaço de cor Linear
(o Godot é Linear; em Gamma o toon saía saturado).

#### PASSO A FEITO (12/09, madrugada): o APK no Poco F4, sem dedo

**O que foi feito.** Três APKs em 25 minutos; o terceiro joga uma partida
inteira sozinho no aparelho e mede o FPS. O jogo passou: 60 FPS cravados,
HUD na escala certa (dp de verdade, 395 ppi), salto com altímetro, pouso na
campina, bots se matando (12 → 6 em dois minutos), tempestade contando.

**Como.** O MIUI do Poco F4 recusa três coisas do `adb`: toque injetado
(`INJECT_EVENTS`), `install -g` (permissões em lote) e instalação sem
"Instalar via USB" ligado. Então o jogo ganhou dois extras de intent e um
medidor:

```
adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity     --es arkana_auto partida --ei arkana_fps 300     # ou "treino"; sem extras = jogo normal
adb logcat -s Unity | grep "ARKANA FPS"              # media, pior segundo, pior quadro, a cada 5 s
adb exec-out screencap -p > foto.png                 # a tela do aparelho
```

O jogador automático salta a 45% da rota do castelo (na 1ª rodada ele não
saltava e o castelo o empurrou no FIM da rota, no mar). `targetFrameRate = -1`
**não é "sem teto" no Android: é 30 FPS** (o padrão do Unity) — pedir 300 é o
jeito de medir.

**O que o aparelho mostrou que a foto de PC não mostrava.** O boot tem dois
engasgos de 6 s (montar a ilha e a arena: grama, kit, colisores); os bots
duelam de verdade; a água de nado funciona (o mago caiu no mar e nadou); as
rochas do kit continuam "rachadas" de perto (asset).

**A avaliação do Diretor, na hora:** *"parte gráfica está muito amadora ainda,
muito longe de algo real; espero que em breve seja possível ver algo
visualmente melhor."* **Isso vira a prioridade.** O que está na tela é
placeholder: mago de primitivas, rochas decimadas com buracos, luz de primeira
passada, nenhum VFX de assinatura. O caminho para "real" está na ORDEM abaixo.

#### LEVA 4 (12/09, madrugada, sem o Diretor): o ELENCO REAL e o kit SEM BURACOS

**O que foi feito.** Os **20 magos** refeitos no SITE da Meshy (nunca pela API),
um por um, e dentro do jogo: `mobile-unity/Assets/_Arkana/Resources/magos/NN-slug.fbx`
+ `-cor.png` + `-normal.png`. E as **8 peças do kit** trocadas por versões com
topologia fechada (o "vidro estilhaçado" das rochas era a decimação).

**A receita do elenco (a que funcionou, para refazer um mago):**

1. **Modelo** → Meshy 7 Flagship, **Multi-View** com três vistas das
   referências: `master-reference-frente.png` (principal), `vista-costas.png`
   (Trás) e `vista-lateral.png` no slot do lado para onde o personagem OLHA
   (Esquerda = olha para a esquerda da imagem: 01, 03-08, 11-17, 19; Direita:
   02, 09, 10, 18, 20). Ultra 2K, Textura, **Pose T**, Melhoria de imagem,
   Privado → **35 créditos**. A Pyra ficou com a de 1 imagem (ordem do Diretor:
   "se funcionar, não refaz").
2. **Ilusionista:** as três vistas trazem o clone de cristal junto; recortado
   por polígono (fundo pintado com a cor da própria linha) →
   `_originais/multiview-*-sem-clone.png`. Saiu limpo.
3. **Animar → Rig** → pede remesh (300 K+ faces): **Corrigido 30K, Triângulo**
   (0 crédito). Rig **Humanoide**, **altura da ficha** (`IdentidadeMago`), e
   **conferir os marcadores**: a Meshy põe a **virilha no cinto** em quase
   todos — descer até onde as pernas se separam; tornozelo no tornozelo.
4. **Clipes** (biblioteca, grátis; o rig já traz Walking/Running):
   **Combate Ocioso** (idle), Mage Spell Cast, Outono 1 (queda), Coletar
   objeto, Alcance Prone Ajuda (derrubado), Nadar para frente, Nadar Parado,
   Andar Agachado com Cautela, e **Planar Arkana** (Texto para Movimento, 10
   créditos, uma vez só: vale para todos, mesmo id). **NUNCA** o "Planar
   horizontal v2" (movimento de IA antigo da conta): mergulha de cabeça para
   baixo — o Diretor viu e disse *"não é aceitável"*. **Nem** o "Parado 1"
   como idle: ergue o braço no meio do laço.
5. **Baixar**: fbx, **Todos Adicionados**, **Arquivo único**, 30 FPS → zip.
   `powershell -File mobile-unity\importar_mago.ps1 <zip> NN-slug` extrai e lista
   as takes.
6. Custo total da noite: **650 créditos** (1.569 → 919), com o remesh do kit
   (grátis). A geração do Gromm falhou no servidor uma vez (crédito devolvido);
   o "tentar de novo" gerou só a malha e a textura saiu pelo botão Textura (10).

**O que o Unity precisou (e os defeitos que só a foto mostrou):**

| Defeito | Causa | Conserto |
|---|---|---|
| Pyra entrou com **1 cm** (um ponto no chão) e o portão verde | o `SkinnedMeshRenderer.bounds` do FBX da Meshy vem ~100× maior (localBounds no espaço do Hips, que herda a escala 100 da Armature) e o Mago escalava por ele | altura pela malha de repouso levada pelo transform do renderer; teste `CharactersMagoExternoTests` mede a malha DEFORMADA (BakeMesh) de todo slug com FBX |
| FBX sem textura | a Meshy não grava o caminho das PNGs | `ImportacaoArkana` liga `-cor`/`-normal` no material NA IMPORTAÇÃO (`_NORMALMAP` ligado em runtime some no APK) |
| Elenco inteiro de **braço erguido** na foto | `Play(Idle)` sai cedo porque o clipe inicial já é Idle: o modelo ficava na pose congelada do FBX até correr a 1ª vez | o Mago começa o idle ao vestir o FBX; teste exige `Animation.isPlaying` |
| Planar de cabeça para baixo | clipe ruim da conta | trocado pelo Planar Arkana; alias por id no `PoseMago` |
| Troca de clipe "pulando" no externo | `Animation.Play` seco; disparo único em `Once` morria e a volta saía do nada | `CrossFade` com os tempos do procedural; disparo único em `ClampForever` e fim lido pelo tempo |
| Portão parado para sempre depois do EditMode | `Start-Process -Wait` espera a ÁRVORE de processos, e o Unity deixa o `VBCSCompiler` (Roslyn) vivo ~10 min | `WaitForExit()` só no Unity (portão, foto e build) |
| PlayMode vermelho com o kit novo ("partial hull") | o casco convexo tem teto de 255 faces no PhysX; a peça de 10 K estoura | `MeshCollider` com a malha REAL (não convexa): some o aviso e a ponte-raiz ganha o vão embaixo |
| Nuvens cortadas em **linhas retas** no céu (foto `12-elenco`) | o hash `frac(sin(x)*43758)` com x na casa dos milhares: o mesmo canto da grade de ruído, calculado por duas células vizinhas, dava valores diferentes | hash sem seno (Hoskins) no céu, no chão toon e na água — os três tinham o mesmo hash |

Fotos novas: `11-clipes-<slug>.png` (os 10 clipes do mago lado a lado, **toda
leva que mexer em clipe olha essa foto**) e `12-elenco.png` (os 20 juntos).

**O kit sem buracos.** A causa do "estilhaçado" era a decimação COLLAPSE do
`arte/tools/blender/otimizar.py` sobre o original de 3 M de faces. Agora a
topologia vem do **Remesh do site** (grátis): 10K triângulos nas 7 peças
grandes, 3K no piso de runa; e o `otimizar.py` ganhou `tris_alvo = 0` (não
decima) e `tex_max_px` (textura 1K JPEG, só a cor base — o `KitCenario` só lê
baseColor). Cada peça: ~0,3–0,9 MB. Mapa (nome na Meshy → peça): Runic Stone
Sanctuary → 17, Emberstone Outcrop → 18, Verdant Rune Arch → 19, Mossroot
Arch → 20, Stormspire Nexus → 21, Arcane Celestial Altar → 28, Emberbloom
Tree → 30, Ancient Keystone Slab → 32.

**As sete da oficina entraram (12/09, manhã).** As peças geradas em 04/09
passaram pela mesma receita (Remesh 10K no site, 0 crédito → glb →
`otimizar.py` com textura 1K). O glb do remesh fica em
`arte/cenario/ilha-fraturada/_originais-3d/NN-*-remesh10k.glb` (fora do git).
Onde cada uma nasce vem das fichas de `arte/prompts/06`, `07` e `08`:

| Peça (nome na Meshy) | Onde | Altura |
|---|---|---|
| 22 arco partido (Runes of the Broken Arch) | anel das Ruínas, de frente para o centro; colide (as pernas batem, o vão passa) | 5 m |
| 23 coluna-braseiro (Emerald Flame Reliquary) | anel das Ruínas | 2,05 m |
| 24 estátua-vigia (The Veiled Warden) | anel das Ruínas, vigiando o centro | 4,1 m |
| 26 torre arcana (Astral Spire) | POIs, 4 no mapa | 22 m |
| 27 braseiro (Elemental Crucible) · 33 obelisco (Crimson Obelisk) · 34 plataforma (Stonefoot Shrine) | **o Altar de Sintonia, montado como a ficha**: braseiro no centro, 5 obeliscos no pentágono de 9 m (**dois tombados**, gema no chão), as duas plataformas frente a frente. Um só, no vale | 1,35 · 2,85 · 1,2 m |

O kit agora recebe as **pegadas das Ruínas** (colunas e blocos da muralha) e não
planta em cima delas.

**O procedural perto da Meshy (mesma manhã).** Ao lado das peças texturizadas, o
que é código lia como maquete. Três ajustes sem triângulo a mais:
- A copa ganhou cacho de 4 massas e `MalhaProc.Blob` passou a pintar em
  degradê por vértice (escuro embaixo). Árvore, moita, pedregulho, seixo e
  rochedo ganham volume.
- O pedregulho passou a usar `CorPedregulho` (família do basalto); o lilás
  lia como gelo.
- As colunas das Ruínas passaram a usar `CorRuina`, de pedra gasta; o azulado
  lia como cano de PVC.

Fotos `13-altar`, `14-ruinas` e `15-torre` (novas no `FotoTests`).

**Passo D começado (12/09, tarde).** O Diretor liberou o custo de FPS: *"o FPS
vai rodar legal, disso tenho certeza"*.
- **Pós-processamento.** Um volume global criado em código (`Ilha.MontarPos`)
  aplica tonemapping neutro, bloom leve (limiar 1,1), contraste e saturação
  +10 e vinheta 0,2. A câmera do jogador liga o `renderPostProcessing`, que no
  URP vem desligado. O `GlobalSettings` não remove variantes de pós, então o
  perfil feito em código vale no APK.
- **Personagem legível.** No `ImportacaoArkana`, a emissão virou a própria cor
  × 0,16, uma luz de rebote que tira o mago do contraluz sem brilhar. A versão
  do importador subiu para 2 para os FBX reimportarem.
- **Magia acesa.** A partícula aditiva dos kits ganhou cor base 1,8 (HDR), e o
  miolo passa do limiar do bloom.
- **Baú e luvas.** Estavam decimados (a foto `16-bau-luvas` mostrava o baú
  estilhaçado) e foram trocados pelo remesh do site: baú em 10K, luvas em 3K, 0
  crédito. A emissão das gemas e das brasas ficou, graças ao 6º argumento do
  `otimizar.py`. O baú caiu de 9,8 MB para 0,8 MB e cada luva de ~8 MB para
  0,4 MB.
- **Mapa das luvas.** Varinha = Emberhand Gauntlet · cajado = Celestial
  Sovereign Gauntlet · manopla = Gemini Gauntlet. Os glb do remesh ficam em
  `arte/cenario/*/origem/` (fora do git).

**O kit em ação, pela primeira vez em foto (`17-kit-tatica`, `18-kit-suprema`).**
Nenhuma foto tinha mostrado um kit disparando, e o diagnóstico achou dois
defeitos de jogo que os testes não pegavam:

| Defeito | Causa | Conserto |
|---|---|---|
| A muralha da Pyra nascia **atrás** dela, fora da tela | os botões TÁTICA/SUPREMA não contavam como mira: o kit saía para a frente do CORPO; parado e com a câmera girada, ia para trás | `Player.Tatica/Suprema` miram como o disparo (`YawAlvo` = yaw da câmera). Teste `Treino_TaticaSaiNaMiraDaCamera` |
| No treino a suprema levava 50 s, não 5 | `Partida.SupremaCargaS` existia e tinha teste, mas o `KitRunner` lia `Dados.SupremaCarga` direto | `KitRunner.SupremaCargaS` pergunta à partida. Teste `Suprema_NoTreino_EncheEm5s` |
| A muralha era uma barra laranja lisa | teto de 48 partículas para 8 m de parede, chama miúda | chama grande e densa (teto 260) + faíscas que espirram |
| Tiro = bola chapada | primitiva `Unlit` de cor pura | cor HDR (acende no bloom) + rastro aditivo por elemento |

**Três frentes em paralelo (12/09, tarde; ordem do Diretor: "acelere, mais atividades
ao mesmo tempo").** Três agentes escreveram código em arquivos separados e
compilaram fora do Unity (Roslyn do próprio 6000.3 contra as DLLs). Houve **uma
rodada só** de portão e fotos para as três:
- **Menu com fundo 3D (`VitrineDoMenu`).** A ilha e o sol nascem no boot. O
  mago escolhido fica no pico, virado para a câmera, que orbita devagar; o fundo
  do menu virou translúcido. A foto `01-menu` usa a câmera real do menu.
- **VFX da Véu e da Tessa.** O eco ganhou névoa e partículas espectrais. O fio
  ficou mais grosso, com faíscas e âncoras acesas. O tear ganhou anel no chão e
  faíscas girando na cúpula. O revelado virou feixe de luz, e a poça da Pyra
  ganhou chamas. Material novo `MaterialVfx.DeLinha()` (faixa macia na largura)
  para fio, feixe e rastro de tiro. Fotos `19/20-kit-<slug>`.
- **HUD.**
  - Barras com moldura, degradê, rastro de dano e número.
  - Botões com anel na cor da ação, cooldown radial e pulso da suprema pronta.
  - Joystick escuro com anel.
  - As barras já nascem com o valor real, e a suprema não aparece mais "pronta"
    no nascimento.

Rochedos do mar: a rocha vulcânica do kit num remesh de 3K, grátis (22 × 3K),
no lugar do bloco facetado. **Decisão que volta ao Diretor:** o validador propôs "Altar
só depois do playtest" porque o Altar encosta na Sintonia. Ele entrou só como
CENÁRIO, sem sistema nenhum; se for para esperar, é tirar uma linha do
`KitCenario.Montar`.

#### O que o Unity AINDA NÃO TEM (registro honesto, 12/09)

Tudo acima **passa no teste, foi visto em foto e rodou no aparelho**. Faltam:

1. **Medir no aparelho** o elenco real (20 × ~30 K triângulos) e o kit novo
   (10 K por peça): FPS e temperatura. Nada disso foi medido ainda.
2. **VFX de assinatura, pós-processamento e luz**: o personagem de frente para
   a câmera fica escuro contra o sol (ambiente baixo); sem bloom/tonemapping.
3. **Baú e luvas** ainda são os `.glb` do Godot. O castelo (30 K da API, foto
   `06-castelo`) lê bem e fica. Se o baú/luvas estilhaçarem de perto: mesma
   receita do kit — "Gemforged Treasure Chest" é o baú na oficina.
4. **Modelo externo** entra como `Animation` legado (com crossfade);
   retargeting/Humanoid ficou desnecessário para os clipes da Meshy.
5. "Intangível" não muda a colisão; bots não usam kit (como no Godot).
6. O gesto de disparo é o do GDD §19.3 (pressionar-arrastar-soltar, cancelar
   voltando ao centro). O Godot R17 tinha "tocar dispara + segurar auto-fogo".
   **Qual dos dois fica é do Diretor.**
7. **Sombra da grama** e o **toon** ainda são de primeira versão: ajustar cor
   ambiente, faixas e alcance da sombra olhando as fotos no aparelho.

#### O que está NA MÁQUINA (11/09)

| | |
|---|---|
| **Unity 6000.3.23f1 + Hub 3.21** | `C:\Program Files\Unity\Hub\Editor\6000.3.23f1`, módulos Android e Windows |
| **Licença** | Personal, resolvida online: **o Hub precisa estar aberto** ou o batchmode morre com 198. Os scripts abrem o Hub sozinhos — pelo AppID, **nunca por `unityhub://`** (sem caminho, o Hub entende "instalar editor" e mostra "versão do Editor arquivado") |
| **Aparelho de teste** | Poco F4 (Snapdragon 870, Adreno 650, 120 Hz) |
| **Apagado em 09/09** | Unreal 5.8, Lyra, caches do Epic, Godot 4.4.1 (~48 GB liberados) |

#### O que FICOU e o que SAIU do repositório (09/09)

| Fica | Por quê |
|---|---|
| `design/` inteiro | decisão não tem engine |
| `arte/` inteiro, incluindo os originais de 3 M de faces fora do git | matéria-prima; a versão de celular se deriva no Blender |
| `mobile-godot/` | **referência de leitura**: cada sistema ali foi jogado e medido. Sai quando o Unity for visto no aparelho fazendo o mesmo |
| `roblox/` | intocado, como sempre |

| Saiu | Por quê |
|---|---|
| `pc-unreal/`, `design/referencias/PC-STEAM-ANALISE.md`, exportador de heightmap, CI do Godot | produto abandonado; o que valia atravessou para as lições abaixo |

#### A ORDEM daqui para a frente — o VISUAL primeiro (ordem do Diretor, 12/09)

| Passo | O quê | Portão |
|---|---|---|
| **A ✅** | APK no Poco F4, partida inteira, FPS medido | 60 FPS, 16,6 ms, sem erro |
| **B ✅ (foto)** | **Elenco real**: os 20 magos do SITE da Meshy no jogo, 11 clipes cada (leva 4). **Falta o aparelho** | o mago real anda, corre, conjura e cai no aparelho; foto lado a lado com a ficha |
| **C ½** | **Kit sem buracos**: as 8 peças com remesh do site (leva 4) ✅, a ponte-raiz com vão (colisor da malha real) ✅, as 7 peças da oficina (arco, coluna-braseiro, estátua-vigia, torre arcana e o Altar de Sintonia) ✅ (foto). Baú e luvas pela receita ✅ (foto 16). Árvore e pedregulho seguem procedurais (melhorados por degradê e cacho) | a rocha de perto lê como rocha; FPS mantido |
| **D ½** | **VFX de assinatura e pós** (pós ✅, partícula HDR ✅, preenchimento do mago ✅): braço de chama da Pyra, muralha de brasas, fio da Tessa, eco da Véu; bloom/tonemapping do URP; luz e ambiente afinados pelas fotos no aparelho; sombra da grama | jogo bonito de ver em vídeo — o Diretor aprova |
| **E** | Boot sem engasgo (montar ilha/arena em fatias por frame); tela em 120 Hz para medir a folga | sem quadro acima de 100 ms |
| **F** | Rede: continua não existindo e continua sendo o item mais caro. Netcode for GameObjects + servidor dedicado sem amarrar a fornecedor | dois celulares na mesma partida |

**Antes da rede, o jogo solo contra bots vale por si** — a decisão de gênero
que sobreviveu ao desvio (ver lições). Modelo de receita fica com o Diretor.

#### O que NÃO muda

- **O elenco continua a ser refeito no SITE da Meshy, um por um.** A causa e a
  receita estão na seção da leva 7, abaixo. Destino: FBX para o Unity.
- `design/` e `arte/` alimentam as implementações; as implementações **nunca
  cruzam código**. GDScript não se traduz: se relê a decisão e se escreve em C#.
- **FPS se mede, não se estima.** Medido: 60 no Poco F4 (12/09). Toda leva visual mede de novo.
- **Teste provado em vermelho, portão verde antes de entregar.** *"Sem
  atropelar as coisas."*

#### Decisões que esperam o Diretor

1. **Gesto de disparo**: GDD §19.3 (o que está no Unity) ou R17 do Godot
   (tocar dispara + segurar auto-fogo). É uma classe só (`GestoLogica`).
2. **Bots com kit?** Hoje não (como no Godot). Ligar é uma linha no `KitRunner`.
3. **Baú Celestial**: dano ao canalizador NÃO cancela (só sair do raio, cair ou
   morrer) — herdado do `.gd`, nunca carimbado. Hora fixa (45 s) vs sorteada.
4. **A tabela de fases da Zona é proposta, não spec** (70 s de abertura, 10 s
   de formação, 5 fases 60/50/40/30/15 s, frações .62/.42/.26/.13/0), calibrada
   contra PUBG/Apex/Fortnite, nunca sancionada por escrito.
5. Do papel de 26/08: **lista dos 10 magos do lançamento**, **nome e valores da
   moeda**, **quais pares de fusão estreiam**; e o **círculo final no platô e no
   pico** (o teto de altura caiu; final em terreno alto é padrão do gênero).
6. Package ID `br.com.vstack.arkana` está em uso; mudar é uma linha em
   `Editor/Build.cs` e no `ProjectSettings`.

---

## LIÇÕES DO DESVIO STEAM / UNREAL (27/08 → 09/09/2026)

Registrado para não repetir. **O que erramos, como consertamos, o que atravessou.**

### O que erramos

- **Decidimos pela feature mais distante.** Os argumentos de 27/08 (rede
  competitiva para 40+, anti-cheat nativo, Nanite) eram tecnicamente
  verdadeiros, mas rede não existia em nenhuma base e continua não existindo.
  Trocamos engine e plataforma por causa de um item que está a meses de
  distância, enquanto o item de hoje (FPS no aparelho) seguia sem medição.
- **Mudamos antes de fechar o jogo.** O Godot tinha 12/12 autotestes verdes e
  partida completa; o Unreal chegou a ter uma ilha bonita e nenhuma partida.
  Treze dias e ~45 GB de instalação para um mapa sem jogo.
- **Uma pessoa, duas engines.** O custo real não era a curva do Unreal: era
  manter dois jogos em ferramentas diferentes. Foi isso que decidiu a volta.

### Como consertamos

- Uma engine para os dois jogos (Unity), decidida em 09/09 com a frase *"não
  pretendo mudar mais"*. Tudo do Unreal apagado da máquina e do repositório.
- A regra "bases nunca cruzam código" foi mantida: o Unity não herda GDScript
  nem Blueprint. Herda decisão, arte e número medido.
- O portão do passo 0 é a medição que faltava desde 25/08. Nenhuma decisão de
  escopo (mapa maior, mais bots, mais peças) antes de FPS no aparelho.

### O que atravessou do Unreal e vale no Unity

1. **"O arquivo que entra não é o mundo que sai, e medida de ontem não mede o
   mundo de hoje."** Quem responde onde o chão está é o colisor, traçado
   AGORA. Um centro de ilha lido de uma paisagem antiga deslocou 1.200 m e
   afogou metade das peças no mar. Medida velha guardada em variável nova não
   parece chute, parece medição — é o defeito mais traiçoeiro que existe.
2. **Cobertura por composição.** As peças de parede/cânion não existem na
   oficina da Meshy. Uma fila de rochas de basalto encostadas lê como crista
   de pedra, com a peça de maior reuso do kit e custo de draw call zero. Quando
   a peça de parede existir, troca-se a malha e a regra continua.
3. **O mapa grande.** 2.400 m de lado, 1.396 peças assentadas no chão medido,
   6 nomes de área legíveis da queda, 5 pontos de partida (lago e alagado fora:
   o centro deles é água). O plano está em `design/cenario/MAPA-GRANDE-PLANO.md`
   e vale para o Unity — com o teto do celular medido antes.
4. **As sete peças novas** geradas no site em 04/09 (215 créditos) continuam na
   oficina da Meshy: `arte/cenario/ilha-fraturada/_originais-3d/00-LEIA.md`.
5. **Gênero, da análise de mercado:** battle royale de 60 com matchmaking global
   é a aposta mais arriscada possível para um projeto sem base. Lobby de 16 a
   20, **bots por padrão sempre** (a partida nunca espera) e **modo solo/PvE
   valendo por si** — o jogo pode lançar sem rede, e a rede entra depois,
   financiada. O Spellbreak provou que combate excelente não segura um BR sem
   base; retenção, não aquisição, foi o que o matou.
6. **Dois fornecedores de servidor dedicado fecharam em 2026** (Hathora e o
   Multiplay da Unity). Quando a rede vier: build headless em contêiner, sem
   amarrar a fornecedor.

---

### A leva 7 FOI DESFEITA: o elenco se faz no SITE (27/08, continua valendo)

**27/08, noite.** Esta secao substitui a que existia aqui e dizia "o elenco
inteiro no jogo (20 de 20)". Aquilo foi **apagado por ordem do Diretor**, e o que
segue e' o registro do porque — para ninguem tentar de novo.

#### 1. O QUE FOI APAGADO, E POR QUE

Os 18 magos foram gerados e riggados pela **API** da Meshy. No aparelho do
Diretor eles **animam como robo**: *"parecem robos, congelados na mesma pose
mesmo quando correm"*. Ordem dele: **apagar tudo e refazer no SITE, um por um,
mesmo perdendo o que foi feito.**

**A CAUSA MEDIDA, e sao duas:**

1. **O rig da API nao tem a etapa de MARCACAO DE ARTICULACOES.** No site, depois
   de escolher Humanoide e a altura, ha' uma tela em que se posiciona queixo,
   ombros, cotovelos, pulsos, virilha, joelhos e tornozelos sobre o corpo. E' essa
   tela que faz a animacao encaixar naquele corpo. A API pula isso: manda a malha
   e a altura, e o esqueleto sai chutado.
2. **A fusao de clipes produziu CLIPE DUPLICADO.** Conferido lendo as chaves de
   rotacao de `brok.glb`:
   - `mage_soell_cast` era **copia byte a byte de `cair`**
   - `derrubado` era copia de `Idle_02`
   - `nadar_parado` era copia de `Walking`

   **Dez clipes distintos vendidos como treze. Ao lancar magia, o mago tocava a
   animacao de CAIR.** Isso estava no `brok.glb` desde a fusao de 26/08 — ou seja,
   o defeito e' anterior aos 18 e foi COPIADO para todos eles.

**Por que o teste nao pegou:** ele cobrava que o clipe EXISTE e que o modelo e'
vestido. Clipe duplicado passa nas duas — existe, e o modelo veste. Cobrir
"o clipe X e' diferente do clipe Y" nunca ocorreu a ninguem porque ninguem
imaginou que a fusao pudesse renomear a mesma acao duas vezes.

#### 2. O QUE SAIU DO PROJETO (higienizacao, ordem do Diretor)

| O que | Por que |
|---|---|
| os 20 `.glb` de personagem + texturas | 18 da API animam como robo; os 2 do site tem clipe duplicado |
| o atelie da API (`godot/characters/modelos/NN-slug/`) | materia-prima do caminho errado |
| `tools/meshy/vestir.sh` | era a esteira do caminho errado |
| `docs/prompts/personagens/` (20 arquivos) | nasceram de uma auditoria mal lida; a arte SEMPRE existiu em `personagens/NN-slug/arte/_originais/` |
| o modo `manter` de `fundir_animacoes.py` | e' o que produziu o clipe duplicado |

**Barreira posta no codigo:** `tools/meshy/meshy.py` agora **recusa** `gerar` e
`riggar` de personagem (`PERSONAGEM_PELA_API = False`) com a explicacao na
mensagem de erro. `prop` (cenario) continua liberado — la' nao ha esqueleto.
`fundir_animacoes.py` ganhou um aviso no topo: **props e cenario apenas**.

**Estado do jogo agora:** 0 de 20 magos com `.glb`; todos rodam o **mago
procedural**, que e' o fallback e funciona. Portao **12/12 verde**.

**Dois testes foram corrigidos** para nao ficarem vermelhos por semanas (portao
vermelho cronico ensina a ignorar o portao):
- "todo mago tem .glb" virou **placar impresso**, nao portao — faltar modelo e' o
  estado esperado enquanto o elenco e' refeito.
- "player inicia como Pyra GLB" cravava o caminho do arquivo; virou "o player
  nasce com mago vestido (procedural ou externo)".

**O que CONTINUA sendo portao, e e' o que tem dentes:** modelo que EXISTE tem que
ser VESTIDO — um `.glb` no disco que o Mage rejeita em silencio e' o modo de
falhar mais caro do pipeline.

#### 3. O CAMINHO CERTO, PASSO A PASSO (site, um mago por vez)

1. **Imagem** -> subir `personagens/NN-slug/arte/_originais/master-reference-frente.png`
   -> Meshy 7 Flagship, **Modo Ultra**, **Textura**, **Pose T-Pose** -> Gerar (35 cr)
2. **Animar** -> selecionar o modelo -> **Rig** -> Humanoide -> **altura da ficha**
   -> **conferir a marcacao das articulacoes** -> Confirmar
3. aplicar os clipes da **biblioteca** (andar, correr, parado, cair, planar,
   nadar, pegar, derrubado e o **gesto de disparo**)
4. exportar **rigged + todos + arquivo unico**, em **FBX**, e importar no
   `mobile-unity/` como rig Humanoid (o Mecanim retargeta)
5. **conferir no motor** que os clipes sao DISTINTOS (nao repetir o erro da fusao)

⚠️ **O Ilusionista** precisa do `frente-recorte.png` (a mestra dele tem duas
figuras de proposito — ele e o reflexo). O `meshy.py` ja' da' precedencia a esse
arquivo, e ele foi mantido.

#### 4. CREDITOS

Gastos e perdidos no caminho errado: **~670**. Mais **30** de uma geracao que eu
disparei por engano ao testar a barreira depois da ordem de parar — erro meu,
registrado. Saldo: **~2.264**.

---

## HISTÓRICO DO GODOT (19/08 → 04/09/2026) — referência para a reescrita

O que segue é o registro das levas em Godot: o que foi feito, como e por quê,
com os defeitos medidos e as regras que nasceram de cada um. **Não é lista de
tarefas** — é o mapa do que o Unity precisa alcançar e das armadilhas já pagas.
Os caminhos `godot/...` apontam para `mobile-godot/godot/`.

### (leva 6, 27/08 — histórico do Godot) mapa 4x maior, tempestade de battle royale, nada mais fixo

**27/08, tarde (leva 6).** APK do teste:
`godot/build/testes/arkana-2026-08-27_1245.apk` (141 MB) — **tem a ilha nova**.
Portao 12/12 em cada passo, com vermelho provado.
Branch `claude/whatsapp-video-review-799b24`.

#### 1. A ILHA QUADRUPLICOU DE AREA — e ficou 27% mais BARATA na tela (`99bb7d8`)

300 -> **600 m de lado**, raio de terra 132 -> **264 m**. Um knob so':
`Island.ESCALA` (2.0). A escala e' **HORIZONTAL**: o relevo nao e' novo, e' a
MESMA ilha amostrada com metade da frequencia — colinas 2x mais largas, e a
malha de 132x132 quads continua acima do Nyquist (quad 2,27 -> 4,55 m; detalhe
mais fino 12 -> 24 m). Colisao: **exatamente as mesmas 34.848 faces**.

MEDIDO headless em 27/08 (nao estimado):

| escala | lado | area | build | colisao | tris NA TELA | draws NA TELA |
|---|---|---|---|---|---|---|
| 1.0 | 300 m | 1x | 610 ms | 34.848 | 137.432 | 67 |
| **2.0** | **600 m** | **4x** | **1403 ms** | **34.848** | **184.967** | **67** |
| 2.5 | 750 m | 6,2x | 2028 ms | 34.848 | 194.216 | 67 |
| 3.0 | 900 m | 9x | 2876 ms | 34.848 | 237.086 | 67 |

O APK anterior desenhava **253.562** tris por quadro. A ilha nova, com 4x a
area, desenha **184.967**. **2.5 e 3.0 estao medidos e a um caractere de
distancia** — o que segura nao e' o quadro, e' carga e memoria.

**O defeito que apareceu no caminho e valia por si so':** o culling por celula
da grama **nao funcionava na horizontal**. As celulas tinham a transformada do
mundo assada nas instancias e o NO' ficava em (0,0,0) — `visibility_range` mede
a distancia da camera ate' a ORIGEM DO NO'. O corte era tudo-ou-nada e o unico
eixo em que funcionava era o vertical. Invisivel numa ilha de 132 m (a camera
nunca fica a mais de 80 m do centro), fatal em qualquer mapa maior. Corrigido:
cada celula no seu lugar, instancias em coordenada local. Seixo, moita, flor e
junco entraram na mesma grade.

**A licao paga pela 4a vez nesta fase:** metro cravado envelhece calado.
Viraram fracao — as varinhas do loot, a espiral de busca do loot (fracao do
raio do POI: no mapa novo a beira seca do brejo foi para 96 m e **dois loots
deixaram de nascer, calados**), o anel do Bau Celestial e os 14 nascimentos do
`Island.tscn`. `Island.pois()` agora entrega **centro E raio**.

#### 2. A TEMPESTADE VIROU BATTLE ROYALE DE VERDADE (`1643168`)

Ordem do Diretor, e cada numero conferido contra PUBG, Apex e Fortnite antes de
entrar — o estudo esta' em **`docs/referencias/ZONA-BATTLE-ROYALE.md`** (as
tres tabelas reais, as seis leis que os tres compartilham, e o que ainda falta).

- **NA QUEDA NAO HA' LIMITE.** A zona nascia LIGADA no `_ready`: com o mago
  ainda no castelo, a fase 1 ja' contava. Agora nasce **inerte** (parede
  invisivel, nenhum cronometro, dps zero) e **liga no sinal de pouso**. Mesma
  lei que a suprema ja' seguia.
- **1:10 de mapa aberto**, a tempestade **se forma** em torno da ilha e para na
  costa, e so' entao comecam as janelas **1:00, 50, 40, 30**. A janela ENCOLHE
  de fase em fase — e' o que faz a partida acelerar (Fortnite faz igual).
- **OS CIRCULOS DEIXARAM DE SER OS MESMOS.** `SEED_ZONA` era fixo em 2707:
  toda partida da historia do jogo teve os mesmos cinco circulos nos mesmos
  lugares. **A rota do castelo tinha o mesmo defeito** (`SEED_ROTA := 3103`).
  Os dois sorteiam por partida agora. O determinismo mudou de escopo: era entre
  partidas, virou DENTRO da partida (um seed gera o plano inteiro de uma vez, e
  fica guardado em `Zona.seed_da_partida` / `Castelo.seed_da_rota` para a rede
  transmitir).
- **A ULTIMA FASE FECHA EM ZERO.** Antes parava em 4 m: existia refugio
  permanente e a partida podia acabar por cronometro com dois vivos.
- Os raios viraram **fracao do raio do mapa** e a regua morreu (`RAIO_INICIAL`,
  `ILHA_REF` e `escala_do_mapa()` sairam; entrou `raio_do_mapa()`, que le'
  `Island.LAND_R`). **0,62 e' 62% do mapa em 300 m e em 2.400 m.**
- `Balance.MATCH.duration_s`: 180 -> **480 s** (a tabela soma 396 s depois do
  pouso; com 180 a partida morria no meio da fase 2).

#### 3. A POPULACAO DOBROU, com a conta na mesa (`Balance.MATCH.bots` 6 -> 12)

Quadruplicar o mapa sem mexer na populacao divide a densidade de encontro por 4.
MEDIDO headless — CPU de fisica por passo, teto de 60 fps = 16,67 ms:
**6 bots 1,684 ms · 12 bots 2,592 ms · 20 bots 4,207 ms · 30 bots 7,630 ms**
(marginal ~0,25 ms/bot). Dobrar custa 0,9 ms = 5% do orcamento.
**12 tambem e' o teto estrutural de hoje:** o `Island.tscn` publica 14
nascimentos e o Main pede `1 + bots` pontos DISTINTOS; passar de 13 faz dois
magos nascerem um dentro do outro. Ha' teste vermelho provado para isso.

#### 4. O CAJADO VOLTOU PARA O SEU POI (`a96d8e6`) — tres defeitos vivos no APK

Achados ao planejar o mapa grande, e medidos com a ilha real:
- `Loot.POIS` era **copia congelada** dos POIs da ilha de 180 m: o cajado das
  ruinas nascia a **46 m** do centro de um plato de 20 m de raio, ou seja FORA
  dele. O contrato "1 cajado por POI" (GDD 14) estava quebrado.
- Um corte de raio cravado em **70 m** num mapa de raio 132 deixava **47% da
  ilha sem loot nenhum**.
- A janela de altura `1,4 a 8,5` estava cravada em TRES arquivos (Loot, Zona,
  BauCelestial), cada um comentando "mesmo corte do outro" — e as tres erravam
  identico: o teto proibia o **plato das ruinas (9,0 m exatos)** e o **topo do
  pico (28 m)**. Os dois unicos POIs com altura de verdade eram os dois onde
  nada podia nascer: nem loot, nem bau, nem circulo final.
Correcao: **`Island.pode_pousar(x, z)`** — chao seco acima da praia, DERIVADO
de `agua_y()`. Sem teto de altura, de proposito.
**Por que o teste nao pegou:** testava contra uma ilha FALSA E PLANA
(`height()` = 3,0 sempre). O teste novo instancia `world/Island.gd`.

---

### O QUE FALTA — em ordem de quem esta' bloqueando

**BLOQUEIO 1 — medir FPS no celular.** Nao e' codigo, e' um cabo USB
(`adb devices` vazio). E' o portao que este projeto deve desde 25/08 e o que
libera: `Island.ESCALA` 2.5/3.0 (ja' medidos), mais bots, e as ondas 3 e 4 do
`docs/cenario/MAPA-GRANDE-PLANO.md` (chunking ate' 2,4 km).

**BLOQUEIO 2 — creditos de imagem.** Os 18 prompts de personagem estao prontos
(`docs/prompts/personagens/`, 20 arquivos) e a fila e' Veu -> Tessa ->
Ceifadora -> ... A geracao 2D esta' **travada: a plataforma de imagem esta' sem
creditos** (saldo 0,07; ~2 creditos por vista, 3 vistas por mago = ~108 para os
18). Os ~2.964 creditos da Meshy sao de OUTRA conta e servem para 3D, nao para
as vistas. Caminhos: recarregar a plataforma de imagem, gerar as vistas no
Firefly (o Diretor tem acesso) ou ir direto de Texto-para-3D na Meshy (mais
barato em passos, pior em qualidade e contra o pipeline aprovado).

**PENDENCIA — o gesto de disparo da Pyra.** Continua o unico item aberto dela
(detalhe na leva 5, abaixo). As 6 variantes gratuitas de "Mage Spell Cast" sao
todas conjuracao de aura com bracos para cima — o diagnostico do Diretor esta'
certo. Melhor candidato pela biomecanica: **"Soco para Frente com Ambas as
Maos"**, aplicado mas nao julgado (a camera do viewer travou num angulo que nao
deixa avaliar a pose, e nao se aprova animacao que nao se viu). Video de
referencia preflightado em **32,5 creditos**, nao gasto. Prompt para o Firefly
entregue ao Diretor.

**DECISOES QUE ESPERAM O DIRETOR**
1. **Veto ou nao** do circulo final no plato e no pico (o teto de 8,5 m que
   proibia caiu; achei melhor liberar — final em terreno alto e' padrao do
   genero, mas e' decisao dele).
2. **Direcao de arte dos 18** (`docs/prompts/personagens/00-FILA.md`): a escolha
   por evidencia foi escultura 3D "stylized premium" — o grupo do Brok
   APROVADO; a Pyra REPROVADA estava no grupo de pintura. Um martelo so' libera
   as 18 geracoes.
3. **Chunking e 20 pawns** (`docs/cenario/MAPA-GRANDE-PLANO.md` §8), depois do
   BLOQUEIO 1.
4. Do papel de 26/08, ainda em aberto: **lista dos 10 magos do lancamento**,
   **nome e valores da moeda**, e **quais pares de fusao estreiam**.

---

### (leva 5, 27/08 — histórico do Godot: as 5 ordens do Diretor viraram codigo e papel)

**27/08, madrugada (leva 5).** APK do teste:
`godot/build/testes/arkana-2026-08-27_0836.apk` (141 MB). Portao 12/12 em cada
passo, com vermelho provado. Branch `claude/whatsapp-video-review-799b24`.

1. **A TELA DE BR** (`ec8b0cb`) — tres ordens numa raia:
   - o rotulo da arma equipada **SOME** depois de 2,5 s (era painel pendurado a
     partida inteira: *"pode apagar este texto da manopla para ser visto de
     cima"*). Confirmacao virou EVENTO; quem quer saber o que tem na mao olha o
     botao de ataque, que diz o elemento.
   - **passar por cima de uma luva/manopla anuncia NOME + ELEMENTO** ("Luva
     Comum · AGUA", "Manopla · AGUA + RAIO"). `Textos.arma_rotulo` e' o dono
     unico do formato — o botao de PEGAR e a confirmacao nunca divergem.
   - **de maos nuas NAO existe elemento na tela**: carrossel dos 5 escondido e
     botao de ataque CINZA e sem rotulo. Equipar e' o que acende. (O teste
     antigo cobrava a lei antiga — "carrossel visivel de maos nuas" — e foi
     reescrito para a nova.)
2. **A SUPREMA SO' CARREGA NO CHAO** (`52a2aa4`): a guarda mora no ponto unico
   (`_carregar_suprema`), entao fecha os dois canais (tempo e dano) de uma vez.
   O gancho `Queda.no_ar()` ja' existia escrito para isto.
3. **AS MANOPLAS REDESENHADAS NO PAPEL** (`992e0c3`) —
   `docs/design/MANOPLAS-FUSAO.md`: a manopla passa a conjurar UM ataque
   FUNDIDO por par (o **Tufao de Brasas** = fogo+vento, o exemplo do Diretor),
   nao dois tiros alternados. Os 10 pares herdam a identidade dos combos de
   Sintonia que o Roblox ja' tem, em escala de ARMA (~1/4 do raio): a manopla
   **vende** a Sintonia, nunca a substitui. Leitura pela lei do Spellbreak
   (FORMA de um + PALETA do outro); efeitos SO' com as reacoes de DANO.md.
4. **O ELENCO E A MOEDA** (`992e0c3`) — `docs/design/DESBLOQUEIO-ELENCO.md`:
   10 jogaveis no lancamento, 10 desbloqueaveis por moeda ganha em partida
   (a resposta direta a licao de retencao do Spellbreak), tudo offline/local —
   o jogo nao tem uma linha de rede. Inclui o plano de producao 3D dos 18 que
   faltam, com o portao de auditar as 4 vistas ANTES de gastar credito.
   **Os dois docs esperam o martelo do Diretor** (lista dos 10, nome da moeda,
   valores, e quais pares de fusao estreiam).

**PYRA — aprovada, riggada e com 11 clipes; falta SO' o gesto de disparo.**
Estado no site (conta dele): geracao nativa multi-view aprovada -> **remesh 10.360
faces / 4.992 vertices** (gratis) -> **rig Humanoide com altura 1,78** ->
**11 clipes aplicados**: Planar (glide), Planar horizontal v2, Agachar-se
Pegar, Andando, Caindo, Correndo, Gemido (derrubado), Mage Spell Cast, Nadar
Parado, Nadar para frente, Parado 1. **NAO foi baixada ainda** — falta o cast.

**O CAST E' O UNICO ITEM ABERTO (ordem do Diretor, 27/08):** *"o movimento de
spell magic e' mais para magias que vem do chao e nosso objetivo e' disparo de
magia... quero que ele fique com a mao estendida ou simulando jogando algo como
na vida real, de filmes, animes"*. Duas geracoes por Texto para Motion (20
creditos) NAO acertaram: a 1a fez arremesso por cima da cabeca + agachada, a 2a
levantou o braco ao lado da cabeca (aceno). As duas foram descartadas.
Caminhos que sobraram, em ordem de custo:
1. **as 5 variantes gratuitas** da biblioteca (Mage Spell Cast 1..5) e os
   vizinhos "Atacar" / "Correr e Atirar" / "Tiro Lateral" — aplicar e olhar
   custa ZERO credito;
2. **Video para Motion**: o Diretor grava 3s fazendo o gesto (ou um clipe de
   referencia) e o Meshy converte — e' o caminho mais certeiro para um gesto
   especifico;
3. outra tentativa de texto com fraseado diferente (10 creditos, incerto).
**Saldo: 2.964.**

**Ao baixar (depois do cast resolvido):** rigged + todos + arquivo unico. Ai vem
uma armadilha JA' conhecida: os clipes de IA (os planars e o cast novo) entram
no .glb com **nome UUID**, nao com o nome que se digita — o `Mage.gd` resolve
por ALIAS, entao e' obrigatorio um passe no Blender renomeando UUID -> alias
(cair/planar/pegar/derrubado/nadar/cast) antes de virar `pyra.glb`. Sem isso o
`cast` nao resolve, e sem `cast` o Mage REJEITA o modelo externo e cai no mago
procedural — a Pyra sumiria de novo. O `tools/blender/fundir_animacoes.py` ja'
faz esse mapeamento por substring; para ela e' SUBSTITUICAO (esqueleto novo),
nao fusao.

**Armadilha nova do navegador, anotada:** a janela do Chrome mudou de escala e
os cliques por coordenada passaram a desviar — no Meshy, clicar por `ref` do
elemento (find) e' o unico caminho confiavel. Foi assim que o painel do
remesh finalmente abriu.

---

### (leva 4, 26/08 — histórico do Godot: a leva Spellbreak aplicada; 4 raias, fila visual atacada, Pyra em rig no site

**26/08, madrugada (leva 4 — 4 raias em paralelo, todas com vermelho provado
e portão 12/12).** APK do teste: `godot/build/testes/arkana-2026-08-26_2249.apk`
(141 MB — modelos + tudo desta noite).

1. **NADAR tem animação própria** (`b7bf4fb`): a locomoção consulta a água na
   fonte (`locomotion_anim`) — bracada = `nadar`, boiar = `nadar_parado`;
   contrato completo no Mage com fallback `run` (quem não tem o clipe fica
   como era; o Brok já tem). E o **treino agora diz "TREINO"** no canto, não
   "3:00/BOTS 0" (pendência do 1º vídeo).
2. **O loot ACENDE** (`926dcbc`, receita Spellbreak): corpo do item emissivo
   na cor do elemento, POR INSTÂNCIA — a luva na mão nasce limpa (contra-prova
   no selftest). Achado: o glb Meshy vem com `emission_enabled=true` e emissão
   PRETA — "já é emissivo" era falso-positivo. Knob: `Loot.EMISSAO_CORPO 0.35`
   (se ainda ler escuro no aparelho, sobe o knob, não mexe em mais nada).
3. **Muralha de Brasas virou FOGO** (`8c8c096`): cunha emissiva com gradiente
   vermelho→amarelo, ≤48 partículas, flicker em PASSOS de 0,08 s (a lição dos
   12–15 fps), OmniLight removida. Gameplay intocado.
4. **Muro de Terra virou PEDRA** (`4404168`): ArrayMesh único compartilhado
   (noise determinístico seed 907, topo irregular, facetado como as rochas da
   ilha) + albedo NoiseTexture2D — zero binário. Colisão EXATA.

**PYRA RECRIADA NO SITE — aguardando o VEREDITO do Diretor.** O upload do
glb antigo foi riggado e chegou a receber clipes, mas o Diretor REPROVOU no
olho ("longe de ficar boa") e mandou recriar como as peças-herói. Feito:
geração nativa Multi-View na conta dele (30 créditos, saldo 2.994) com
frente + perfil esquerdo + costas das vistas aprovadas (a "3/4" NÃO entrou:
é a frontal repetida e ensinaria o perfil errado), Meshy 7 + Ultra + Textura
+ Pose A-Pose + licença PRIVADO. Resultado: 1,97 M faces, revisado no viewer
com zoom — braço-manopla de bronze em chamas no lado ESQUERDO certo, rosto
com as marcas de lava, capelete queimado nas costas, braçadeira no braço
direito. **Está na área de trabalho dele, primeira posição da galeria.**
Quando ELE aprovar: remesh ~15k (grátis no site) → rig (altura 1,78!) →
os mesmos clipes do Brok (biblioteca: Mage Spell Cast, Caindo, Nadar ×2,
Agachar-se Pegar, Gemido, Parado 1 + os 2 Movimentos de IA já pagos) →
download rigged/todos/arquivo único → SUBSTITUIÇÃO inteira do `pyra.glb`
(esqueleto novo ≠ repo) → characters/selftest (1,78 m) + clipes no motor.
Os DOIS uploads antigos dela (T-pose e riggado) seguem na galeria — remover
só se o Diretor mandar.

**Perguntas abertas ao Diretor:** (a) os rótulos de bússola ("Manopla · ÁGUA
+ RAIO") lendo do outro lado do mapa são wayfinding proposital ou poluição?
(b) manopla emissiva sai na cor do 1º elemento do par — quer a cor do par?

---

### (26/08, noite — leva 3: Brok animado, castelo voando, 3 defeitos, histórico)

**26/08, noite (leva 2 — atualiza o bloco abaixo).** Tudo na branch
`claude/whatsapp-video-review-799b24`, portão 12/12 em cada passo:

1. **O Brok tem os 13 clipes** (`888567f`). Da biblioteca Meshy (grátis):
   `cair` (falling_down), `nadar`, `nadar_parado`, `pegar` (agachar-pegar),
   `derrubado` (gemido no chão), `ande_agachado`. Por **Texto para Motion**
   (20 créditos, autorizados): `planar` (barriga, pose de paraquedismo — saiu
   ÓTIMO no retarget) e `mergulho` (de cabeça — guardado para o mergulhar da
   água). Fusão via `tools/blender/fundir_animacoes.py` (novo, headless,
   reutilizável): **o export "Todos Adicionados" da Meshy OMITIU o
   `mage_soell_cast`** — por isso NUNCA substituir o glb do repo pelo download;
   sempre fundir. Esqueletos conferidos idênticos (24 ossos). Aliases exatos:
   o `Mage.gd` adota sozinho, com laço automático em cair/planar/derrubado.
2. **A abertura agora é o castelo VOANDO** (`a04ec09`, ordem dele por texto):
   no trajeto, o pivô da câmera larga o ombro (o mago viaja pendurado SOB a
   rocha — era ESSA a causa do breu) e vai ao coração do castelo com o braço
   a 90 m e sem colisão; o arrastar de olhar orbita a ilha voadora. No salto,
   volta ao ombro. Provado no selftest da queda.
3. Os 3 defeitos do vídeo (kill feed batizado, BOTS 0 na vitória, pausa na
   tela) — ver leva 1 abaixo. **Saldo Meshy: 3.004.**

**Próximo (em ordem):**
- **APK novo para o Diretor testar** (build disparado no fecho desta leva —
  conferir `godot/build/`): abertura do castelo, queda/planar do Brok, pausa,
  kill feed com nome, BOTS 0.
- **Pyra: mesma receita do Brok** — ela NÃO está na galeria do site (página 1;
  provavelmente veio da API). Caminho: Carregar o `pyra.glb` do repo no
  webapp → Animar → mesmos clipes da biblioteca + os 2 Movimentos de IA já
  PAGOS na conta ("Planar horizontal v2" e "Planar (glide)" aplicam em
  qualquer modelo) → download → `fundir_animacoes.py`. ~15 min de navegador.
- **Código novo que os clipes destravam**: contrato `nadar`/`nadar_parado` no
  Pawn/Mage (a água hoje não troca animação), `mergulho` (botão novo, GDD),
  `ande_agachado` no agachar, e o gesto do baú sustentado com o `pegar` real.
- O quadro geral da leva 1 (respostas do teste, dossiê, fila visual) está no
  bloco logo abaixo — continua valendo.

---

### (26/08, noite — leva 1: 2º vídeo avaliado, dossiê Spellbreak, histórico)

**26/08, noite.** O Diretor testou o APK 16:50 (o primeiro com os modelos Meshy
dentro) e mandou vídeo de **partida completa** (130 s, VITÓRIA). Branch desta
leva: `claude/whatsapp-video-review-799b24` (commits `4fa9755` + `522ba4d`).

**As 3 respostas que a fase anterior pediu, tiradas do vídeo:**
- **Castelo na abertura: metade.** Os ~1,5 s iniciais são a rocha DELE em sombra
  ocupando a tela inteira (quase preto); olhando de baixo funciona como
  silhueta dramática e o brilho dourado central lê bem. Conserto sugerido:
  câmera nascer enquadrando o castelo contra o CÉU + luz de preenchimento/
  emissiva (clima com COR, nunca falta de luz).
- **Luvas no chão: de perto leem MUITO bem** (a manopla é a melhor peça do
  lote); **de longe viram vulto preto** — quem lê à distância é o feixe e o
  rótulo. E o rótulo lê longe DEMAIS (sem corte por distância, gruda na borda).
- **A queda "estranha" — dívida paga:** ela cai em pose de idle/corrida a queda
  inteira (os .glb não têm clipe de cair/planar; os aliases de `Mage.gd`
  esperam), câmera 90° para baixo (sem horizonte = sem velocidade), personagem
  em contra-luz. Correção por custo: clipes Meshy → inclinar câmera → vento/FOV.

**3 defeitos do vídeo consertados com prova em vermelho (12/12 no portão):**
1. Kill feed vazava `@CharacterBody3D@1718` → bots batizados com nome de mago
   no spawn (`Main.gd`, `Kits.de(slug).nome`).
2. VITÓRIA com "BOTS 1" congelado → `_end_match` leva o placar final à HUD.
3. **Botão de PAUSA nascia 58 dp ABAIXO da tela** (o `_canto` ancora tudo no
   rodapé e o `_layout` contava o topo do topo) → re-ancorado; o teste agora
   cobra o RETÂNGULO na tela (o `visible=true` era assert decorativo).

**Dossiê Spellbreak** em `docs/referencias/SPELLBREAK.md` (pedido do Diretor):
mecânicas, combos (referência da Sintonia: combo = FORMA de um elemento +
PALETA do outro), VFX toon com ramp + partículas 12–15 fps (resposta para
muros chapados), **loot emissivo/auto-iluminado (resolve o vulto preto)**, a
queda deles freia com a própria levitação, 4 lições do fracasso (retenção,
MMR de novato, exclusividade, dono do IP). Community Version oficial no
itch.io é referência jogável legal; extrair asset é proibido.

**Meshy (webapp, conta dele) — clipes de animação NO MEIO:**
- **"Caindo" foi ADICIONADO ao Brok de jogo** (o riggado de 15.424 faces).
- Já localizados na biblioteca (grátis): "Nadar Parado", "Nadar para frente",
  "Agachar-se, Pegar", "Pular para Pegar e Cair", "Cair", "Tiro e Cair p/
  Frente". **PLANAR NÃO EXISTE na biblioteca** → o Diretor autorizou gerar por
  "Texto para Motion" pagando créditos.
- **INTERROMPIDO no meio:** a janela do Chrome encolheu (Diretor na máquina).
  Retomar: janela restaurada → meshy.ai/workspace → Animar → Brok já fica
  selecionado. Saldo intacto: **2.984**. Depois de baixar: fundir clipes nos
  .glb (os aliases adotam por nome) e repetir para a Pyra (conferir se a Pyra
  está na conta do site — pode ser só da API, aí é Carregar o FBX riggado).
- Atenção ao baixar: apareceu um selo "+50" junto do botão de exportar no
  viewer — conferir O QUE custa 50 antes de clicar (a regra era rig/animação
  grátis no webapp).

**O vídeo também re-confirmou a fila visual (§10):** muro de terra caixote,
muralha de brasas chapada, volumes amarelos do baú, Pyra escura em contra-luz
(candidata a rim/fill light), horizonte lendo como prédios, e os ~3 s iniciais
quase pretos da queda.

---

### (26/08, noite — os modelos do Meshy entram no jogo, histórico)

**26/08, noite.** A esteira do Blender rodou e os 5 heróis entraram no APK:

- **`tools/blender/otimizar.py`** (headless): decima preservando UV (o
  normal/albedo do Meshy continua valendo — sem re-bake), escala para METROS
  reais e põe o pé em y=0. Resultados: castelo 30k tris/52 m · luvas 3,5–5k
  em tamanho real (0,32–0,46 m) · baú 8k **nas medidas do código** (1,15 m —
  colisão e canalização intocadas).
- **`core/Pbr.gd`** — o grampo do metal virou dono único (a lição do mago
  preto): `Pbr.domar()` em todo modelo Meshy instanciado.
- **Integração com fallback defensivo em tudo**: `Castelo._montar_visual`
  veste `world/modelos/castelo.glb` (primitivas viram `_montar_fallback`);
  `BauCelestial._montar` veste `bau.glb`; `ArmaSlot.modelo()` veste
  `gameplay/modelos/luva-<id>.glb` — sem arquivo, o procedural de sempre.
- **Teste da lei antiga atualizado**: o selftest da queda cobrava "zero
  binário" no castelo — a DIREÇÃO §10 mudou a lei; agora cobra o modelo
  vestido E o fallback vivo (chamado direto). Vermelhos: caminho quebrado (3),
  Pbr removido (1).
- **APK: 140 MB** (era 53) — os 5 GLB com texturas 2K. Ordem do Diretor de
  25/08 vale: tamanho não é critério agora; compressão vem na fase de loja.

**Pendências REGISTRADAS da fase:** recolor por elemento das luvas-modelo
(hoje usam as cores master; a runa procedural é quem colore por elemento no
fallback) · decal do bordado na palma da Conjurador (Blender manual) ·
castelo saiu 52 m de ALTURA (o Meshy esticou a rocha; ficha era 34 m — ajustar
escala/eixo na passada fina) · **FPS no Poco F4 continua sem medição — é o
portão da repaginação do chão (DIRECAO §10.1)**.

---

### (26/08, fim de tarde — 5 heróis em 3D na conta, histórico)

**26/08, fim de tarde.** A fila inteira da DIREÇÃO §10 virou modelo: Castelo,
Luva Comum, Luva de Conjurador, Manopla e Baú — todos pelo SITE, visíveis na
galeria dele, Multi-View das vistas ortogonais aprovadas. Placar e ressalvas em
`cenario/00-MODELOS-3D.md`. **210 créditos no dia · saldo 2.984.**

**A regra que nasceu no meio, por correção DELE:** revisar o modelo NO VIEWER
antes de baixar. Ele pegou dois defeitos na Conjurador v1 (gema extra no
anelar + palma sem os símbolos) que eu ia deixar passar. A v2 corrigiu a
geometria (4 cristais exatos, contados em zoom); a palma lisa persistiu em 2/2
gerações → **o bordado vira decal no Blender** (determinístico, grátis), não
terceira loteria de 30 créditos.

**Pergunta dele respondida:** as 5 cores da Luva Comum NÃO são 5 modelos — é
UM master (o vermelho) + recolor de material no motor, como as luvas
procedurais já fazem. 5 gerações custariam 5× e dariam 5 geometrias.

**Blender INSTALADO (winget).** A esteira da próxima fase, por peça: decimar
(~2 M → alvo de jogo) + bake de normal + decal da palma (conjurador) → GLB de
jogo em `godot/world/modelos/` (git/LFS) → pendurar nas cenas (castelo em
`Castelo.gd`; luvas em `ArmaSlot.modelo()` no lugar do procedural; baú em
`BauCelestial`) → colisão simples à mão → **FPS no aparelho** (DIRECAO §10.1).

**Aba do Meshy deixada ABERTA no Chrome dele** — os processos estão todos lá.

---

### (26/08, fim de tarde — o castelo 3D nasce, histórico)

**26/08, fim de tarde — o primeiro modelo de cenário nasceu.** E uma ordem do
Diretor mudou o processo no meio: *"quero ver todos os processos lá no site,
a arte e tudo disponível na minha área de trabalho da plataforma"*.

- **Verificado no navegador: tarefa de API NÃO aparece no workspace Meshy.**
  A política nova está em DIRECAO.md §10.2: **peça-herói gera no WEBAPP, na
  conta dele** (processo visível); API só para validação/scripts.
- O castelo saiu pelas DUAS vias no mesmo dia (60 créditos ao todo, saldo
  3.134): o do **site é o oficial** ("Aetherstone Citadel", 1,9 M tris,
  texturizado, visível na galeria dele); o da API (30.917 tris) é backup e
  candidato a base low-poly. Ambos em `cenario/01-castelo-voador/origem/`
  (fora do git — a nuvem do oficial é a própria conta Meshy dele).
- **Próximo passo do castelo:** Blender (decimar OU retopo com bake do
  hi-poly) → `godot/world/modelos/castelo.glb` → pendurar em `Castelo.gd` →
  FPS no aparelho. Detalhes em `cenario/01-castelo-voador/00-MODELO.md`.
- **A repaginação do CHÃO tem plano e portões** (pergunta dele, respondida):
  DIRECAO.md §10.1 — o chão é a 3ª onda, depois dos heróis e do FPS medido,
  em três sub-ondas (árvores/pedras → splat do terreno → água), cada uma com
  FPS antes/depois. Herói é um objeto; o chão é o mundo.
- Fila do Meshy no site: **Luvas** (3 modelos) → **Baú**.

---

### (26/08, tarde — concept arts validadas, histórico)

**O ciclo de arte fechou o primeiro loop completo** (26/08, tarde): prompts em
`docs/prompts/` → o Diretor gerou 55 imagens no ChatGPT → validação contra os
critérios de cada ficha → **54 aprovadas, 1 para regerar** → transportadas
para `cenario/<peça>/arte/` (o espelho de `personagens/`), 112 MB no LFS.

- **Veredito e lupa:** `cenario/00-VALIDACAO.md` — inclui a checagem legal
  anti-FMA da palma da Conjurador (passou) e o critério mais fino da Manopla
  (polegar+médio em ouro, os dedos do estalo — exato).
- **A reprovada:** a key art do Altar (48) sem os dois obeliscos tombados — o
  texto de ajuste está no 00-VALIDACAO, pronto para colar no gerador.
- **Custo LFS atualizado:** ~470 MB totais → ~2 clones limpos/mês na cota.
- **Próximo passo da arte:** Castelo → Meshy multi-imagem (frente-selo +
  lateral-porta-salto + costas + tres-quartos). Depois: Luvas → Baú.

---

### (26/08, tarde — o vídeo do Diretor avaliado, histórico)

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

**Concept art:** o Diretor gera as imagens por IA ANTES de qualquer crédito
Meshy. **Os prompts completos moram em `docs/prompts/`** (pasta única, ordem
dele): 8 peças, cada uma com ficha física em metros, prompt mestre, 4 vistas
separadas (a lição do painel plano de 21/08), negative e critérios de
aprovação. Regra: imagem aprovada → multi-image no Meshy.

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

### (26/08/2026, dia — histórico)

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


---

## O que o Godot já provou (resumo do que o Unity tem que alcançar)

O jogo era **jogável de ponta a ponta no Android**: menu, seleção de mago,
queda do castelo, partida contra 12 bots, zona que fecha, luvas e loot, Baú
Celestial, habilidades, escudo, derrubado e reerguer, terreno reativo com 6
reações, água que se nada, HUD completa, pausa, treino, áudio sintetizado.
Modelos 3D reais do castelo, das 3 luvas e do baú (decimados, em LFS). Os 20
magos rodavam o mago procedural — o elenco 3D é refeito no site.

A tabela de sistemas e onde cada um está: `mobile-godot/00-LEIA.md`. Os
contratos e as armadilhas de teste: `mobile-godot/godot/ARQUITETURA.md`.

**As regras duras não são estilo, são cicatrizes de defeitos medidos:**
dano passa por UM lugar e NaN se barra com `not (x > 0)`; velocidade é produto
único (base × terreno × status); número que o dedo sente vive em dp, do mundo
em metros; cancelar é estado de primeira classe; cor + FORMA sempre, clima com
COR e nunca com falta de luz; fogo propaga por ORÇAMENTO, nunca chance por
tique; metro cravado envelhece calado — raio, altura e distância viram fração
do mapa; a Zona nasce inerte e liga no pouso; todo teste novo se prova
reintroduzindo o defeito.

---

## Frente paralela: Roblox

`roblox/` é o **Campo de Provas**: o ambiente multiplayer para testar com pessoas
o que bots não validam — Sintonia, TTK, leitura do terreno, equilíbrio dos
elementos e vontade de jogar de novo. Tem servidor autoritativo, duplas, terreno
reativo, os dez combos de Sintonia, loop de BR, bots, acessibilidade e telemetria.
51/51 nos testes. **O bloqueio não é técnico:** falta o playtest humano, e login
e publicação são ato do Diretor. Intocado desde 19/08.

---

## O que foi encerrado

| Quando | O quê | Por quê |
|---|---|---|
| 19/08/2026 | Protótipo 2D (Phaser/TypeScript/Capacitor) | validou toque e mecânica; não era produto |
| 27/08/2026 | Godot 4.4.1 como produto | congelado quando o projeto foi para PC; hoje é referência da reescrita |
| 09/09/2026 | PC / Steam / Unreal 5 | desvio desfeito pelo Diretor — ver LIÇÕES DO DESVIO |

Tudo continua recuperável pelo histórico do git.

---

## Linha do tempo

| Data | Marco |
|---|---|
| 17/08 | Do zero ao protótipo jogável (2D), com 4 raias em paralelo |
| 19/08 | 2D encerrado; elenco cresce para 20 magos; pivô para Godot 3D |
| 20/08 | Consolidação em Godot 3D + Roblox; concept art dos 20 |
| 21/08 | R20: zona, habilidades, escudo, derrubado, armas arcanas, baú, HUD; Pyra em 3D |
| 24/08 | Novo lote de concept art dos 20 magos (8 vistas cada) |
| 25/08 | A QUEDA (castelo, salto, planeio, pouso); ilha 180 → 300 m; Brok em 3D; higienização |
| 26/08 | A DIREÇÃO (20 respostas do Diretor viram código); castelo, luvas e baú em 3D; vídeos avaliados; Spellbreak aplicado |
| 27/08 | Ilha 600 m, tempestade de BR de verdade, 12 bots; leva 7 desfeita (elenco no site); **virada para PC/Steam/Unreal** |
| 28/08 → 04/09 | Ilha Fraturada no Unreal: 2.400 m, 1.396 peças, 46 FPS; sete peças novas no site da Meshy |
| **09/09** | **Desvio desfeito. O produto é celular em Unity 6.** Unreal, Lyra e Godot apagados; `pc-unreal/` removido |
