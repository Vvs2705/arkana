# MIXAMO — biblioteca humanoide do ARKANA (checklist de download + proveniência)

> Criado em 04/10/2026 (BLOCO C da missão de recuperação). Estado: **BLOCKED_MIXAMO_DOWNLOAD** — o download exige
> login Adobe e aceite dos termos no site, que é ato do Diretor (ou autorização explícita para eu fazer pelo navegador).
> Tudo que não depende dos arquivos já está pronto e testado (ver "O que já existe").

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

- Fonte (como baixou): `arte/personagens/mixamo-<funcao>.fbx` (LFS pega `*.fbx`).
- No Unity: `mobile-unity/Assets/_Arkana/Resources/magos/mixamo-<funcao>.fbx`, importado como **Humanoid**,
  Avatar "Create From This Model", Loop Time na locomoção, Root Transform Rotation/Position Y/XZ **Bake Into Pose**.

## Proveniência (preencher a cada download — uma linha por clipe)

| Clipe final | Nome no Mixamo | Fonte | Data | Arquivo original | Import (rig/loop/root) | Uso no controller |
|---|---|---|---|---|---|---|
| | | Mixamo (Adobe), conta do Diretor | | | | |

Não misturar proveniência: o que vier da Meshy, Blender ou Tripo tem linha própria com a fonte certa.
Licença: conferir os termos da Adobe no momento do download; não redistribuir o arquivo cru fora do projeto.
Nada de conta, sessão ou credencial no repositório (ele é público).

## O pilot (quando os FBX chegarem)

1. Importar os P0 como Humanoid (acima). Conferir no Inspector: Avatar válido, sem ossos vermelhos.
2. `Mago.cs`: caminho novo **preferencial** só para o Validation Set — `Animator` no visual com o Avatar de
   `EsqueletoHumano.Construir` (runtime; não muda a importação dos FBX da Meshy) e um `AnimatorController` enxuto:
   - Base: Locomoção = Blend Tree 2D (MoveX, MoveZ relativos ao corpo) com idle/run/strafe/trás; Pulo→Ar→Pouso;
     Derrubado; Nado. Velocidade separada da direção; histerese idle/run (a da `Locomocao`).
   - Camada do tronco (AvatarMask) para Cast e Hit leve — as pernas seguem a locomoção.
   - **O gameplay manda no tempo**: o tiro sai no relógio do `Pawn.Atirar`; nenhum `AnimationEvent` decide dano,
     mana, cooldown, revive ou Sintonia.
3. Medir foot sliding por clipe (passo por ciclo a `speed = 1`) — fim da constante única `Balance.Anim.RunStrideM`
   medida na Pyra antiga.
4. Fotos dos 3 (frente, lado, trás, diagonal, subida, descida, cast parado/correndo) → Poco F4 → só então os 20.
