# MIXAMO — biblioteca humanoide do ARKANA (download, proveniência, integração)

> Criado em 04/10/2026 (BLOCO C da missão de recuperação). **Estado (04/10, tarde): BAIXADO E INTEGRADO.** O Diretor
> autorizou no chat usar o Mixamo pelo Chrome dele; 77 clipes baixados pela conta Adobe dele, tocando nos 20 magos pelo
> Mecanim Humanoid (`Scripts/Characters/MagoMecanim.cs`), com o legado (Animation) e o procedural de reserva.

## Por que Mixamo + Mecanim (e o que NÃO muda agora)

- Hoje: `Animation` legado com os takes da Meshy por mago + `PoseMago` (procedural) de reserva. Funciona, mas cada
  movimento novo (strafe, hit, levantar) teria de ser gerado 20 vezes na Meshy.
- Mecanim **Humanoid**: um clipe serve nos 20 (retarget automático). O esqueleto da Meshy **fecha** um Avatar humano
  nos 20 magos — provado em `CharactersCorpoMagoTests.Humanoide_*` (mapa em `Scripts/Characters/EsqueletoHumano.cs`;
  a coluna da Meshy tem nome invertido: `Spine02` é a base). Relatório por mago em `mobile-unity/Logs/humanoide.txt`.
- **Nada do que o Diretor vê muda** até o pilot passar nos 3 do Validation Set. O legado fica como reserva.

## Animation Validation Set (escolhido por dado, 04/10)

| Papel | Mago | Por quê (medido no FBX) |
|---|---|---|
| Compacto | **16-fizz** | 0,95 m; quadril a 0,28 m (30% da altura); pior pé-escorregando entre quem pisa (recuo 0,46 m) |
| Mediano | **05-corvomante** | 1,80 m = mediana do elenco; escala 1,00; ombro na mediana (0,20 H) |
| Maior/largo | **18-basalto** | 2,30 m; ombro 75,6 cm (o maior absoluto e relativo); envergadura 2,65 m |

Fora: Pip (0,60 m, voa a 1,2 m — nunca pisa); Vitalis tem o braço caído no bind (precisa de "Enforce T-Pose").

**Medido em 04/10 (`Logs/humanoide.txt`): os 20 fecham Avatar humano válido.** Braço abaixo da horizontal no bind
(0 = T-pose, que o Mixamo espera): 14 magos entre 5° e 20°; **Basalto 34°, Gromm 36°, Corvus 43°** (A-pose) e
**Vitalis 72°** (braço caído). Antes de retargetar neles, girar o braço para a horizontal na descrição do esqueleto
(o "Enforce T-Pose" do importador, feito no `EsqueletoHumano`) — senão o braço do clipe sai ~35° mais baixo. O Basalto
está no Validation Set justamente por isso.

## Configuração de download (igual para todos)

- Personagem no site: **Y Bot** (não subir os magos ao Mixamo — o retarget é do Unity).
- Formato **FBX for Unity (.fbx)**, **Without Skin**, **30 fps**, **Keyframe Reduction: none**.
- Locomoção com **In Place** marcado (o gameplay move o corpo; root motion OFF).
- Nome do arquivo: `mixamo-<funcao>.fbx` (ex.: `mixamo-strafe-esq.fbx`).

## Lista canônica (termos de busca — os nomes exatos do site não foram confirmados por mim; conferir na busca)

| Função | Buscar no Mixamo | In Place | Hoje (take Meshy) | Prioridade |
|---|---|---|---|---|
| Strafe esquerda | Left Strafe (e "Left Strafe Walking") | sim | **não há** | P0 |
| Strafe direita | Right Strafe (e "Right Strafe Walking") | sim | **não há** | P0 |
| Correr para trás | Running Backward | sim | só andar (Walk_Backward) | P0 |
| Cast à frente (projétil) | Standing 1H Magic Attack 01 | — | mage_soell_cast (2,27 s, longo) | P0 |
| Hit frente/esq/dir/trás | Standing React Small From Front / Left / Right / Back | — | **não há** | P1 |
| Derrubar | Knocked Down | — | não há (só o caído) | P1 |
| Levantar / reviver | Getting Up | — | **não há** | P1 |
| Pegar / interagir | Picking Up | — | Collect_Object (6 s, longo) | P1 |
| Idle neutro | Breathing Idle | — | Idle_02 (só 12/20, ergue o braço) | P2 |
| Pulo início / ar / pouso | Jumping Up / Falling Idle / Falling To Landing | sim | Regular_Jump (bom) | P2 |
| Andar / correr frente | Walking / Running | sim | Walking / Running (bons) | manter Meshy |
| Nadar / boiar | Swimming / Treading Water | sim | Swim_Forward / Swim_Idle (bons) | manter Meshy |
| Idle de combate | — | — | Combat_Stance (bom) | manter Meshy |

Regra (ordem do Diretor, 12/09): **nenhum clipe entra só porque existe**. Cada clipe novo passa pela folha
`11-clipes-<slug>.png` (lado a lado) antes do aparelho. Clipe ruim → mantém o atual e anota a lacuna aqui.
O cast tem de ler como **disparo à frente**, nunca ritual no chão.

## Onde os arquivos ficam (sem pasta nova)

- No Unity: `mobile-unity/Assets/_Arkana/Resources/mixamo-<nome>.fbx` (o prefixo segue a convenção de `arq-`, `cc0-`,
  `tripo-` em Resources), Git LFS. Controller: `Resources/mixamo-mago.controller`; máscara do tronco: `mixamo-tronco.mask`.
- Os zips originais: `Downloads` do Diretor (não versionados; o FBX no projeto é o arquivo baixado, só renomeado).

## Proveniência (77 clipes, todos baixados em 04/10/2026)

Fonte: Mixamo (Adobe), conta do Diretor, autorizado no chat. Personagem X Bot, **sem personagem** no arquivo (pacote:
"No Character"), **FBX for Unity**, **30 fps**, **sem redução de quadros**, sem In Place (o andar vai para o root motion,
que o Animator descarta: a medida da passada sai daí). Import: `ImportacaoArkana` (Humanoid, Avatar do próprio arquivo,
clipe com o nome do arquivo, laço nas passadas, giro e altura assados pelos pés). Uso: `ControladorHumanoide` (controller).
Os zips originais ficaram em `Downloads` do Diretor ("Pro Magic Pack.zip", "Arkana Extras.zip", "Arkana Locomocao.zip");
nada de conta, sessão ou credencial no repositório. Licença: conferir os termos da Adobe; não redistribuir o arquivo cru.

| Arquivo no projeto | Nome no Mixamo | Origem | Data |
|---|---|---|---|
| `mixamo-boiar.fbx` | Floating | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-crouch-idle.fbx` | Crouch Idle | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-to-standing-idle.fbx` | Crouch To Standing Idle | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-turn-left-90.fbx` | Crouch Turn Left 90 | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-turn-right-90.fbx` | Crouch Turn Right 90 | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-walk-back.fbx` | Crouch Walk Back | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-walk-forward.fbx` | Crouch Walk Forward | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-walk-left.fbx` | Crouch Walk Left | Pro Magic Pack | 04/10/2026 |
| `mixamo-crouch-walk-right.fbx` | Crouch Walk Right | Pro Magic Pack | 04/10/2026 |
| `mixamo-derrubado.fbx` | Knocked Down To Stomach | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-escalar-beirada-agachar.fbx` | Climb Wall From Braced Hang To Crouch | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-levantar.fbx` | Getting Up From Being Knocked Down On The Ground | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-pegar.fbx` | Picking Up An Object With One Hand | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-pouso-da-queda.fbx` | Landing From Falling Idle | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-pular-obstaculo-1-mao.fbx` | Male Jumping Over An Obstacle With 1 Hand Planted | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-queda-no-ar.fbx` | Mid-Air Falling Idle | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-rastejar.fbx` | Crawling Forward On Hands And Knees | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-standing-1h-cast-spell-01.fbx` | standing 1H cast spell 01 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-1h-magic-attack-01.fbx` | Standing 1H Magic Attack 01 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-1h-magic-attack-02.fbx` | Standing 1H Magic Attack 02 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-1h-magic-attack-03.fbx` | Standing 1H Magic Attack 03 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-cast-spell-01.fbx` | Standing 2H Cast Spell 01 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-area-attack-01.fbx` | Standing 2H Magic Area Attack 01 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-area-attack-02.fbx` | Standing 2H Magic Area Attack 02 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-attack-01.fbx` | Standing 2H Magic Attack 01 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-attack-02.fbx` | Standing 2H Magic Attack 02 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-attack-03.fbx` | Standing 2H Magic Attack 03 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-attack-04.fbx` | Standing 2H Magic Attack 04 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-2h-magic-attack-05.fbx` | Standing 2H Magic Attack 05 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-block-end.fbx` | Standing Block End | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-block-idle.fbx` | Standing Block Idle | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-block-react-large.fbx` | Standing Block React Large | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-block-start.fbx` | Standing Block Start | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-idle.fbx` | standing idle | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-idle-02.fbx` | standing idle 02 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-idle-03.fbx` | Standing Idle 03 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-idle-04.fbx` | Standing Idle 04 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-idle-to-crouch.fbx` | Standing Idle To Crouch | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-jump.fbx` | Standing Jump | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-jump-running.fbx` | Standing Jump Running | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-jump-running-landing.fbx` | Standing Jump Running Landing | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-land-to-standing-idle.fbx` | Standing Land To Standing Idle | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-death-backward.fbx` | Standing React Death Backward | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-death-forward.fbx` | Standing React Death Forward | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-death-left.fbx` | Standing React Death Left | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-death-right.fbx` | Standing React Death Right | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-large-from-back.fbx` | Standing React Large From Back | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-large-from-front.fbx` | Standing React Large From Front | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-large-from-left.fbx` | Standing React Large From Left | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-large-from-right.fbx` | Standing React Large From Right | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-small-from-back.fbx` | Standing React Small From Back | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-small-from-front.fbx` | Standing React Small From Front | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-small-from-left.fbx` | Standing React Small From Left | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-react-small-from-right.fbx` | Standing React Small From Right | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-run-back.fbx` | Standing Run Back | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-run-forward.fbx` | Standing Run Forward | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-run-left.fbx` | Standing Run Left | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-run-right.fbx` | Standing Run Right | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-sprint-forward.fbx` | Standing Sprint Forward | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-turn-left-90.fbx` | Standing Turn Left 90 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-turn-right-90.fbx` | Standing Turn Right 90 | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-walk-back.fbx` | Standing Walk Back | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-walk-forward.fbx` | Standing Walk Forward | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-walk-left.fbx` | Standing Walk Left | Pro Magic Pack | 04/10/2026 |
| `mixamo-standing-walk-right.fbx` | Standing Walk Right | Pro Magic Pack | 04/10/2026 |
| `mixamo-subir-beirada.fbx` | Pulling Up To A Ledge | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-subir-e-pular-obstaculo.fbx` | Step Up To Jump Over Object | avulso (zip "Arkana Extras") | 04/10/2026 |
| `mixamo-loc-parado.fbx` | Unarmed Idle — Standing Idle- Loop | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-andar-frente.fbx` | Unarmed Walk Forward — Walking Forward | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-andar-tras.fbx` | Unarmed Walk Back — Walking Backwards | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-correr-frente.fbx` | Unarmed Run Forward — Running Forward | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-correr-tras.fbx` | Unarmed Run Back — Running Backwards | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-correr-esq.fbx` | Left Strafe — Running Strafe To The Left | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-correr-dir.fbx` | Right Strafe — Running Strafe To The Right | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-andar-esq.fbx` | Left Strafe Walking — Strafe Walking To The Left | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-andar-dir.fbx` | Right Strafe Walking — Strafe Walking To The Right | avulso (zip "Arkana Locomocao") | 04/10/2026 |
| `mixamo-loc-sprint.fbx` | Sprint — Standard Sprint | avulso (zip "Arkana Locomocao") | 04/10/2026 |

## O pilot — FEITO (04/10, tarde)

- **Validation Set validado** (Fizz, Corvomante, Basalto): Avatar humano válido, folhas `60-mecanim-*` (3/4 frente) e
  `61-mecanim-jogo-*` (câmera do jogo, atrás do ombro) com 14 situações cada. Os 20 fecham Avatar → `Balance.Anim.MecanimEmTodos = true`.
- **Retarget conferido por número** (`DiagMecanim`): o mesmo clipe no X Bot de origem e no mago dá as mesmas direções de braço.
- **Decisão (vetável): a PASSADA é a genérica** (`loc-*`). A do pacote de mago anda com o braço direito jogado para trás
  e, na câmera do jogo, ele aparece erguido ao lado da cabeça (folha 61 de 04/10). O pacote de mago segue nas magias
  (1 mão no tiro, 2 mãos na tática e na suprema), golpes, pulo, queda, derrubado/rastejar/levantar, boiar e pegar.
- **Passada sem patinar em qualquer tamanho:** blend 2D pela velocidade no corpo dividida pela escala humana (Fizz 0,39,
  Corvomante 0,99, Basalto 1,29); acima do clipe mais rápido a cadência acelera até `Balance.Anim.CadenciaMax` (2,2).
  O Fizz bate o teto correndo a 7,5 m/s (o pé dele escorrega um pouco: é um gnomo na velocidade de todo mundo).
- **Subir em obstáculo** (`Escalada`): `pular-obstaculo-1-mao` e `escalar-beirada-agachar` no relógio do jogo.
- **A animação não decide nada:** tiro, dano, mana, recarga, reviver e Sintonia seguem no relógio do Pawn.

**Falta:** medir o custo do Animator Humanoid no Poco (linha `ARKANA CUSTO`); nado para a frente (só há o boiar);
mortes do pacote ainda sem uso (o `VisualDoAbate` deita o corpo); remover o legado depois que os 20 passarem no aparelho.
