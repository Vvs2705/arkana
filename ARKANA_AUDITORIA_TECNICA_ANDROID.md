# ARKANA — AUDITORIA TÉCNICA COMPLETA (ambiente Android / preparação para Android Emulator)

**Data:** 2026-09-23 · **Modo:** SOMENTE LEITURA — nada foi instalado, desinstalado, atualizado, buildado, configurado, criado como AVD, commitado ou enviado. Único efeito colateral registrado: o comando `adb devices` (inspeção) subiu o servidor `adb` local (PID 17160); `sdkmanager --list_installed` consultou o repositório do Google (leitura).
**Onde este arquivo está:** `ARKANA_AUDITORIA_TECNICA_ANDROID.md` na raiz do repositório — ARQUIVO ÚNICO (relatório consolidado + 4 anexos com a evidência linha a linha). Não commitado, por ordem do brief.
**Como foi produzido:** orquestrador (coordenador) + 4 raias de inspeção paralelas. As seções 1–31 consolidam; os Anexos A–D (no fim deste mesmo arquivo) trazem comandos, paths e resultados de cada raia.

| Arquivo | Conteúdo |
|---|---|
| Anexo A | SO, CPU, RAM, GPU, disco, virtualização, SDKs, JDKs, Android Studio, adb, AVD, Unity instalado, outras ferramentas, 36 comandos |
| Anexo B | ProjectSettings, Build.cs, pipeline de build, deploy, debug, perf, rede, testes, dependências, 21 problemas |
| Anexo C | linha do tempo, deploy/debug documentados, medições no Poco F4, 35 problemas resolvidos, 24 abertos, 28 decisões, regras de trabalho |
| Anexo D | git, tamanhos, mapa, 26 APKs (anatomia do último), segredos, higiene P0–P3, 45 arquivos importantes |

Convenção: `M/` = clone principal `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA\`; `W/` = worktree desta auditoria `M/.claude/worktrees/auditoria-tecnica-completa-282af1/` (mesmo commit).

---

## 1. Identificação do projeto

| Item | Valor |
|---|---|
| Nome | **ARKANA** — battle royale de magos, 3ª pessoa, celular Android |
| Caminho local | `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA` (clone principal, `main`) |
| Branch / commit | `main` = `0a94c2f` (2026-09-21 14:54) "docs: PROJETO — FPS de 16 para 34 no Poco F4…"; worktree `claude/auditoria-tecnica-completa-282af1` no mesmo commit |
| Estado do Git | árvore **limpa** nos dois checkouts; **`main` está 93 commits à frente de `origin/main`** (`0694794`, 09/09) — verificado pelo coordenador com `git rev-list --count origin/main..main` = 93 |
| Alterações não commitadas | nenhuma além deste arquivo (não rastreado) |
| Tamanho | ≈ 20,6 GB em disco (Library 9,8 GB, APKs 5,4 GB, `.git` 1,8 GB, arte 1,6 GB); clone limpo ≈ 1,3 GB (243 MB objetos + 1.055 MB LFS em 392 arquivos) |
| Estrutura | `design/` (decisão), `arte/` (matéria-prima), `mobile-unity/` (produto), `mobile-godot/` (referência morta), `roblox/` (referência morta), `swarm.yaml`, `CLAUDE.md`, `README.md`, `CHANGELOG.md` |
| Módulos (`mobile-unity/Assets/_Arkana/Scripts/`) | Core (Bus, Balance, Kits, Combat, Sintonia), World (Ilha procedural, Castelo, Mata, Praia), Gameplay (Partida, Player, Bot, Zona, Queda, Loot, KitRunner + 26 Habilidades), Terrain (reativo), Characters (Mago, LuvaVisual), UI (Hud, Joystick, Minimapa, Selo), Menu (Config, Elenco, Grimório), Audio (Sfx sintetizado), Editor (Build, MainSceneBuilder) |
| Código | 189 `.cs` (125 produção + 64 teste), 7 shaders HLSL, 4 `.ps1`, 1 cena gerada por código |

**Estágio: vertical slice jogável / pré-alpha solo contra bots.** Evidências: 20 magos reais com kit completo; partida inteira automática (castelo → queda → 12 bots → tempestade → fim); 620 testes no portão (534 executados, 85 fotos puladas sem GPU), 0 falhas; APK jogado no Poco F4 em 12/09, 16/09 e 21/09 (34,6 FPS na Média). Contra "alpha": **rede não existe** (G5 não começou), nenhum terceiro jogou, sem loja/conta/keystore de release, sem CI, sem crash report. Fases: G0–G3 fechadas, **G4 aberta** (dupla + Sintonia), G5 rede e G6 loja dependem do Diretor.

## 2. Engine

| Item | Valor | Como foi obtido |
|---|---|---|
| Engine | **Unity 6 — 6000.3.23f1**, changeset `09d2ecc7fb28`, URP 17.3.0 | `mobile-unity/ProjectSettings/ProjectVersion.txt` (único commit, nunca mudou) |
| Instalada na máquina | **6000.3.23f1** (única versão), `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`, 17,6 GB, módulos Android Build Support (mono+il2cpp), OpenJDK, SDK & NDK, Windows | listagem de `Hub\Editor`, `PlaybackEngines`, VersionInfo do `Unity.exe`; Hub 3.21.3 (MSIX) |
| Compatibilidade | **idêntica** (projeto = instalado = cravado nos 3 `.ps1` = `build.log:2`) | — |
| Indícios conflitantes | `mobile-godot/` (Godot 4.4.1, desinstalado 09/09) e `roblox/` (Rojo/Luau) são bases anteriores, declaradas referência; `design/pipeline/ANDROID.md` cita método/paths errados do build; `design/infra/servidor-03/06` descrevem a era Godot; `test-framework` pedido 1.5.1, resolvido 1.6.0 (builtin) | grep em `.md/.ps1/.cs`; `packages-lock.json` |

## 3. Linguagens e stack

- **Linguagens:** C# 9 (.NET Standard 2.1), HLSL (7 shaders à mão, sem ShaderGraph), PowerShell 5.1 (4 scripts), Python (ferramentas de arte em `arte/tools`), GDScript e Luau só em referência morta.
- **Build:** Unity `BuildPipeline` → **Gradle 9.1.0 / AGP 9.0.0 embutidos** → **IL2CPP**; NDK r27c (27.2.12479018), CMake 3.22.1, OpenJDK Temurin 17.0.18 — todos os do módulo Android do Unity (sem override no registro). Sem templates Gradle nem manifesto custom (`Assets/Plugins/Android` não existe). Sem CI. Assemblies: `Arkana`, `Arkana.Editor`, `Arkana.Tests`, `Arkana.PlayTests`.
- **Dependências que afetam Android/execução/gráficos/input:**

| Pacote | Versão | Papel |
|---|---|---|
| `com.unity.render-pipelines.universal` | 17.3.0 | URP, Forward, Render Graph; SSAO serializado mas desligado |
| `com.unity.inputsystem` | 1.14.0 | toque/joystick (`activeInputHandler: 2` = ambos, gera 1 warning por build) |
| `com.unity.cloud.gltfast` | 6.20.0 | importador `.glb` no editor (0 uso em runtime); arrasta burst 1.8.30, collections, mathematics, `unitywebrequest` |
| `com.unity.ugui` | 2.0.0 | HUD/menu |
| `com.unity.test-framework` | 1.6.0 (resolvido) | portão |
| `androidx.games:games-activity 4.4.0`, `games-frame-pacing 2.1.2` | via Gradle gerado | GameActivity + Swappy |
| Rede / anúncios / compras / auth / analytics / crash | **nenhum** | — |

Armazenamento: só `PlayerPrefs` (config, grimório, modo, mago). Áudio: 48 timbres sintetizados, 0 arquivos. `Resources/` = 477 MB (magos 437 MB), tudo entra no APK.

## 4. Ambiente do computador

| Item | Valor | Veredito |
|---|---|---|
| SO | Windows 10 Pro 22H2 (10.0.19045) x64, Acer Nitro ANV15-51 | 🟢 |
| CPU | Intel i7-13620H, 10C/16T; VT-x/EPT ligado (hipervisor detectado) | 🟡 ver virtualização |
| RAM | **8 GB** (1×8 DDR5, 1 slot livre, máx. 64 GB); **< 1 GB livre** durante a auditoria; commit 20,5/24 GB (paginando) | 🔴 |
| GPU | NVIDIA RTX 3050 6 GB Laptop (driver 591.74, Vulkan 1.4) + Intel UHD (Optimus); 4/6 GB VRAM em uso por outros apps | 🟢 |
| Disco | 1 SSD NVMe 476 GB, **34 GB livres**; projeto na mesma unidade do Unity (17,6 GB), SDK, `.gradle` (4,4 GB) e pagefile (16 GB) | 🟡 |
| Virtualização | hipervisor da Microsoft **ativo** (VirtualMachinePlatform + WSL2/Docker Desktop + VBS status 2, HVCI desligado); **`HypervisorPlatform` (WHPX) DESABILITADO**; Hyper-V completo desabilitado | 🔴 |

**Veredito para Android Emulator:** a máquina está no pior ponto intermediário — com o hipervisor ativo, **HAXM e AEHD não funcionam**; o único caminho de aceleração seria **WHPX, que está desligado**. Sem aceleração o emulador roda em software, inutilizável para URP 3D. Habilitar WHPX exige admin + reinício (não executado). Mesmo com WHPX, **8 GB de RAM** não comportam Unity Editor + emulador ao mesmo tempo. Detalhes e comandos: Anexo A §2–5 e "Virtualização — veredito".

## 5. Android SDK

Duas instalações. **Nenhuma tem `emulator/`, `system-images/` ou AVD.**

```
Ferramenta: Android SDK (embutido do Unity)          Ferramenta: Android SDK (avulso, era Capacitor)
Versão: platforms 34/35/36; build-tools 36.0.0;      Versão: platforms 35/36; build-tools 35.0.0+36.0.0;
        platform-tools 36.0.0; cmdline-tools 16.0;           platform-tools 37.0.1; cmdline-tools 19.0
        cmake 3.22.1; NDK r27c ao lado                        sem NDK, sem cmake
Local: ...\6000.3.23f1\Editor\Data\PlaybackEngines\   Local: %LOCALAPPDATA%\Android\Sdk (ANDROID_HOME)
       AndroidPlayer\SDK
Estado: completo para BUILD; sem emulator/images     Estado: funcional para adb/sdkmanager; licenças aceitas
Usada pelo projeto? SIM (build do APK)               Usada pelo projeto? só o adb do PATH (manual)
```

`sdkmanager`/`avdmanager` existem só no avulso (19.0). Gradle avulso: não instalado; cache `.gradle` 4,4 GB com daemons 8.14.3 (Capacitor) e 9.1.0 (Unity). Detalhe: Anexo A §6.

## 6. Java / JDK

Três JDKs Temurin: **17.0.18 do Unity** (o que o build usa, sem override), **21.0.12+8 em Program Files** (JAVA_HOME de máquina, 1º do PATH) e **21.0.12+8 duplicado em `%LOCALAPPDATA%\Java`** (JAVA_HOME de usuário, que prevalece no processo). `java -version` no PATH → 21. Compatível com `sdkmanager`/`avdmanager`/`emulator` (exigem 17+). Situação: redundância inofensiva; o Unity ignora ambos. Detalhe: Anexo A §7.

## 7. Android Studio

**Não está instalado** (sem pasta, sem registro, sem winget). Achado paralelo: workload `Microsoft.NET.Sdk.Android` do .NET 10 no registro (não é SDK Android nem emulador).

## 8. ADB

| | adb do PATH | adb do Unity |
|---|---|---|
| Versão | 1.0.41 / platform-tools **37.0.1** | 1.0.41 / platform-tools **36.0.0** |
| Local | `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe` | `...\AndroidPlayer\SDK\platform-tools\adb.exe` (fora do PATH) |
| Funciona | sim (`adb version`; `adb devices -l` subiu o servidor) | sim (`adb version`) |

`adb devices -l` em 23/09 11:45: **lista vazia** — Poco F4 não conectado; nenhum emulador. **Risco real:** duas versões de platform-tools → cada cliente mata e reinicia o servidor do outro (bate com a lição "o Unity reinicia o servidor adb e corta o logcat", 21/09). Chaves `adbkey`/`debug.keystore` em `~/.android` só registradas, não abertas.

## 9. Emuladores / AVD existentes

**Nenhum AVD configurado.** `~/.android/avd` não existe; `emulator.exe` não existe em lugar nenhum; sem system-images; sem BlueStacks/LDPlayer/Nox/Genymotion/VirtualBox/VMware/WSA/Windows Sandbox. Único virtualizador presente: Docker Desktop 4.69 (WSL2, distro parada).

## 10. Build Android do ARKANA

```
Comando:            powershell -File mobile-unity\build_apk.ps1
  (interno)         "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe" -batchmode -nographics
                    -projectPath <mobile-unity> -buildTarget Android
                    -executeMethod Arkana.EditorTools.Build.Android -logFile <mobile-unity>\Builds\build.log -quit
Pré-condições:      Unity Hub aberto (licença Personal; sem ele exit 198 — o script abre pelo AppID e dorme 25 s);
                    um único Unity por projeto (trava de instância); módulo Android instalado.
Resultado esperado: "BuildSummary: result=Succeeded" no build.log; script imprime "APK: <caminho> (<MB> MB)"; exit = do Unity.
Local do APK:       <mobile-unity>\Builds\arkana.apk (sobrescrito) + cópia datada em
                    M\mobile-unity\Builds\testes\arkana-AAAA-MM-DD_HHMM.apk (SEMPRE no clone principal, via git-common-dir)
Duração:            1 min 36 s incremental (21/09); 5 min 20 s limpo (12/09). Gradle 27,7 s.
```

O que `Build.cs` força a cada build (e regrava `ProjectSettings.asset`): URP em todos os níveis, Linear, `V-STACK`/`Arkana`, `br.com.vstack.arkana`, **IL2CPP**, **ARM64 apenas**, minSdk 26, targetSdk 35, paisagem, 6 shaders em `AlwaysIncludedShaders`, `BuildOptions.None` (**release, sem Development Build**). Último build (21/09 14:46): Succeeded, 0 erros, 1 warning (input handling), `launcher-release.apk`, 228 MB.

**Nenhum build foi executado nesta auditoria.** Detalhe: Anexo B §3.2 e §4.

## 11. APK / AAB existentes

**26 APKs, 0 AAB**, todos em `M/mobile-unity/Builds/` (ignorados pelo git): `arkana.apk` (228 MB, 21/09 14:47) + 25 datados em `testes/` de 12/09 00:20 (176 MB) a 21/09 14:47 (228 MB), total 5,4 GB. Nenhum APK do ARKANA fora do repositório. Tabela completa: Anexo D §4.1.

**Anatomia do mais recente** (`arkana.apk` = `testes/arkana-2026-09-21_1447.apk` = `Library/.../launcher-release.apk`, SHA-256 `c6b6b199…`), via `unzip -l`, `aapt2 dump badging`, `apksigner verify --print-certs`:

| Campo | Valor |
|---|---|
| Package / versão | `br.com.vstack.arkana` — versionCode **1**, versionName **1.0** (nunca incrementados) |
| minSdk / targetSdk / compileSdk | 26 / 35 / 35 |
| ABI | **só `arm64-v8a`** (sem armeabi-v7a, sem x86_64) |
| Backend / entrada | IL2CPP (`libil2cpp.so` 61 MB); **GameActivity** (`libgame.so`, `com.unity3d.player.UnityPlayerGameActivity`); Swappy; Burst |
| Assinatura | v2 apenas, **`CN=Android Debug`** (keystore de debug `~/.android/debug.keystore`, 17/08) |
| Debuggable | **não** (variante release) |
| Permissões | `INTERNET` (vinda do módulo `unitywebrequest`, não do código) + `DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` (androidx) |
| Requisitos | `glEsVersion 0x30000` (GLES 3.0 obrigatório); Vulkan `required=false`; `screenOrientation="userLandscape"` |
| Descomprimido | 476 MB (assets 387 MB; maior entrada 65,6 MB = castelo) |

Manifesto e Gradle gerados lidos pelo coordenador em `M/mobile-unity/Library/Bee/Android/Prj/IL2CPP/Gradle/` (só leitura): `abiFilters "arm64-v8a"`, `debugSymbolLevel "none"`, `signingConfig signingConfigs.debug` **também no release**, `useLegacyPackaging true`, `-DANDROID_SUPPORT_FLEXIBLE_PAGE_SIZES=ON`, `org.gradle.jvmargs=-Xmx4096M`, `unityTemplateVersion=22`.

## 12. Deploy atual

```
Código  →  portao.ps1 (EditMode+PlayMode headless)  →  foto.ps1 (PlayMode com GPU, PNG + diag.txt)
        →  build_apk.ps1  →  M\mobile-unity\Builds\testes\arkana-<data>.apk
        →  INSTALAÇÃO (manual, sem script):
             caminho 1 (usual): o Diretor sobe o APK no Google Drive ("JOGOS EM DESENVOLVIMENTO") e instala no Poco F4
             caminho 2 (quando há cabo): USB + adb install (o comando exato não está escrito em nenhum doc);
             MIUI exige "Instalar via USB"; adb shell input só com "Depuração USB (configurações de segurança)"
        →  EXECUÇÃO: ícone, ou partida automática sem dedo:
             adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity
                 --es arkana_auto partida --ei arkana_fps 300 [--ei arkana_bancada 1 --es arkana_cortes "..."]
        →  LEITURA: adb logcat -s Unity | grep "ARKANA FPS" / "ARKANA BANCADA"; adb exec-out screencap
```

**Não existe script de deploy**: `adb` aparece só em comentários de 3 `.cs` e em `design/PROJETO.md`. O aparelho ficou indisponível de 25/08 a 21/09 em vários períodos ("quando o celular voltar"). Detalhe: Anexo B §5, Anexo C §2.

## 13. Debug atual

| Pergunta | Resposta |
|---|---|
| Onde aparecem os erros | PC: `Logs/portao*.log/.xml`, `foto.log`, `fotos/*.png` + `diag.txt`, `carregamento-*.txt`. Aparelho: **só `adb logcat -s Unity`** |
| Como se identifica crash | critério documentado: "sem erro nem exceção no logcat numa partida inteira". **Sem crash reporting** (Crashlytics/Sentry/Cloud Diagnostics: 0), sem `logMessageReceived`, sem Development Build (Profiler não conecta), `enableCrashReportAPI: 0` |
| Problemas Android | vídeo do WhatsApp do Diretor a 0,5–1 quadro/s; "procure a variável que muda com o aparelho" (dp vs px, MIUI) |
| Performance | `MedidorDeFps` (`ARKANA FPS media/min/dt_max` a cada 5 s, só fora do editor), `BancadaDeCortes` (15 cortes de GPU × 10 s, `ARKANA BANCADA <corte> fps= ms= pior=`), `adb shell top -H` (mostrou gargalo GPU) |
| Gráficos | `foto.ps1` no PC (na proporção do Poco F4) + foto/vídeo do aparelho; o Diretor julga pelo visual |
| Logger | prefixo `"ARKANA` em só 4 classes; sem logger central |

**Automação por intent (o ativo mais reaproveitável para um emulador):** `Assets/_Arkana/Scripts/PartidaPeloAdb.cs` lê `arkana_auto` (`partida`/`treino`), `arkana_fps` (int; `-1` = 30 FPS no Android, por isso 300), `arkana_bancada` (1) e `arkana_cortes` (CSV, `+` combina) via `AndroidJavaClass` sob `#if UNITY_ANDROID && !UNITY_EDITOR`. Semântica completa: Anexo B §6.

## 14. Testes

| Item | Realidade |
|---|---|
| Unitários (EditMode) | 59 arquivos, **502 `[Test]`**; lógica pura sem cena; último: 526 total / 525 passou / 1 skipped, 6,5 s |
| PlayMode | 5 arquivos, **58 `[UnityTest]`**; `BootTests` (5, monta a arena headless e reprova log inesperado), `CarregamentoTests`, `AbateVisualTests`, `VooVisualTests`, `FotoTests` (85 fotos, **puladas sem GPU**) |
| Portão | `portao.ps1` → "ARKANA: 620 testes, 0 falhas" (21/09) — **534 executados + 85 skipped** (a linha soma `total` sem descontar) |
| Fotos | `foto.ps1` com GPU; última rodada 3/3 com filtro, 8 PNG |
| Mutação | "105 mutantes mortos" = processo manual ("todo teste se prova reintroduzindo o defeito"); nenhuma ferramenta |
| Android / smoke / integração | **nenhum automatizado**; partida automática por intent + logcat lido por humano |
| CI / cobertura | **nenhum** |

## 15. Configuração Android do projeto

| Campo | Valor | Fonte |
|---|---|---|
| Package | `br.com.vstack.arkana` | `ProjectSettings.asset:179`, `Build.cs:42` |
| minSdk / targetSdk / compileSdk | 26 / 35 / 35 | `.asset:181-182`, `Build.cs:45-46`, Gradle gerado |
| Arquitetura / ABI | **ARM64 apenas** (`AndroidTargetArchitectures: 2`, forçado em `Build.cs:44`) | |
| Scripting | IL2CPP, configuração Release, stripping padrão, .NET Standard 2.1 | `.asset:689-694` |
| Orientação | AutoRotation só paisagem (`userLandscape`) | `.asset:62-65`, manifesto |
| Entrada | GameActivity (`androidApplicationEntry: 2`) | `.asset:86` |
| Permissões | nenhuma pedida no código; `INTERNET` entra pelo módulo `unitywebrequest` | manifesto gerado |
| Render | **Graphics API Auto** (`[]` → Vulkan, fallback GLES3); Linear; URP Forward + Render Graph; HDR on; MSAA off; sombras 4096×4 cascatas no asset, soft Low; SSAO desligado; Swappy on; render scale 1.0 no asset | `.asset:402,50`, `URP_Base*.asset` |
| Qualidade | 6 níveis built-in, Android padrão Medium, **um único asset URP**; `Config.cs` muta o asset em runtime (Baixa 0,70/2/2048, Média 0,85/2/2048, Alta 1,0/4/4096) só fora do editor; padrão Média, 60 FPS, vSync on | `Config.cs:81-84,176-195` |
| Debug/release | só release (`BuildOptions.None`); sem variante debug | `Build.cs:110` |
| Assinatura | `androidUseCustomKeystore: 0`, nome/alias vazios, **campos de senha não existem no `.asset`**; keystore de debug do Gradle assina debug e release | `.asset:276-289`, `launcher/build.gradle` |
| Arquivos sensíveis | `arte/tools/meshy/.env` (56 B, ignorado), `~/.android/debug.keystore` e `adbkey` — **existem, não abertos**; nenhum keystore/jks/pem no repo | Anexo D §5 |

## 16. Rede e multiplayer

**Não existe nada.** 0 usos de `UnityWebRequest/Socket/WebSocket/Netcode/Mirror/Photon/FishNet/Firebase/PlayFab/Unity Services` no código (única ocorrência é um comentário de VFX "mirror"). Nenhum pacote de rede; `cloudEnabled: 0`; `MultiplayerManager.asset` é o padrão do Unity 6. `design/infra/` registra a **intenção**: Netcode for GameObjects + servidor dedicado em contêiner (G5), contas/loja (G6), tudo "ato do Diretor". Jogo 100 % local (jogador + parceiro bot + 12 bots). Nenhuma conexão externa foi feita nesta auditoria.

## 17. Performance — ferramentas disponíveis

| Métrica | Existe? |
|---|---|
| FPS | sim — `MedidorDeFps` (logcat, 5 s) e `BancadaDeCortes` (por corte de GPU) |
| CPU | só manual (`adb shell top -H`) |
| GPU | só por inferência (bancada: custo em ms por efeito); sem Snapdragon Profiler, sem RenderDoc |
| RAM / VRAM | não |
| Draw calls / batches | não (sem `ProfilerRecorder`, sem `FrameTiming`; `enableFrameTimingStats: 0`) |
| Garbage collection | `gcIncremental: 1`; sem medição |
| Temperatura / energia | não (44–47 °C lidos a olho no aparelho) |
| Latência de rede | não se aplica |
| Unity Profiler remoto | **impossível** com o APK atual (sem Development Build) |

Números medidos no Poco F4 (Adreno 650): 60 FPS (12/09, tela em 60 Hz) → 17–20 FPS no chão (21/09 manhã, gargalo GPU) → SSAO ~12 ms, sombra macia alta ~10 ms, escala 70 % tira ~22 ms → **34,6 FPS na Média** (padrão), ~25 Alta, ~40 Baixa, 47 °C (21/09 tarde). Tabela: Anexo C §4.

## 18. Git

| Item | Valor |
|---|---|
| Branch / último commit | `main` / `0a94c2f` 2026-09-21 |
| Alterações locais / não rastreados | nenhuma / só este arquivo |
| Tags / releases | **nenhuma** |
| Remoto | `https://github.com/Vvs2705/arkana.git` (sem token na URL); **`origin/main` = `0694794` de 09/09 → 93 commits, 738 arquivos, +78.881 linhas e 134 arquivos LFS (~504 MB) só neste disco** |
| Branches | `main`, `claude/auditoria-tecnica-completa-282af1` (esta); nenhuma não mesclada; 2 worktrees; stash vazio |
| Histórico | 231 commits desde 17/08 (pico 50 em 12/09); conventional commits pt-BR; 6 commits "Main.unity regravada pelo build" (ruído) |
| LFS | 392 arquivos / 1.055 MB; `.git/lfs` 1,6 GB (≈550 MB de versões velhas); 85,8 MB de binários **fora** do LFS (5 `.glb` em `arte/cenario/_glb` anteriores à regra + 15 `.jpg` do Godot sem regra) |
| Ignorados | `Library`, `Builds`, `Logs`, `*.apk/*.aab`, `.env*`, `*.keystore/*.jks`, originais Meshy, `InitTestScene*` — correto; 0 `.meta` órfãos |
| Hooks / CI | só os do git-lfs; sem CI |
| Lixo | 13 `.idx` órfãos em `.git/objects/pack` (warnings) |

Nada foi commitado, enviado ou alterado.

## 19. Problemas encontrados

Os mais relevantes (lista completa: Anexo B §11 com 21 itens, Anexo C §6 com 24, Anexo D §6 com P0–P3):

| # | Problema | Evidência | Impacto | Causa provável | Solução atual? | Risco |
|---|---|---|---|---|---|---|
| P-01 | **93 commits e ~504 MB de LFS nunca enviados ao GitHub** (toda a reescrita Unity) | `git rev-list --count origin/main..main` = 93; `origin/main` de 09/09 | falha de disco = perda do produto; push consome ~metade da cota LFS mensal (1 GB) | trabalho local sem push desde 09/09 | não | 🔴 P0 |
| P-02 | **WHPX desabilitado com hipervisor ativo** (WSL2/Docker + VBS) | `Win32_OptionalFeature HypervisorPlatform = 2`; `HypervisorPresent = True` | emulador só em software; HAXM/AEHD inviáveis | Docker Desktop ligou VirtualMachinePlatform; WHPX nunca foi ligado | não (exige admin + reboot) | 🔴 para emulador |
| P-03 | **8 GB de RAM, < 1 GB livre, paginando** | `Win32_OperatingSystem`, `Win32_PageFileUsage` | Unity + emulador não cabem juntos | hardware | não (1 slot DDR5 livre) | 🔴 |
| P-04 | **APK só ARM64** | `AndroidTargetArchitectures: 2`; `Build.cs:44`; `lib/` do APK | AVD x86_64 não instala (`INSTALL_FAILED_NO_MATCHING_ABIS`); imagem ARM64 em host x86 é lenta | alvo era só o Poco F4; motivo de excluir x86_64 não identificado no repositório | não | 🔴 para emulador |
| P-05 | `Build.cs` **sobrescreve** `ProjectSettings.asset` a cada build | `Build.cs:21-56` | ajuste manual (ABI, API gráfica, dev build) é desfeito no próximo build | receita "settings por script" | não | 🟡 |
| P-06 | Graphics API em **Auto** (Vulkan primeiro) | `.asset:402` | em GPU virtual (ANGLE/SwiftShader) Vulkan é o caminho frágil; forçar GLES3 exige mudar settings | nunca precisou no Poco F4 | não | 🟡 |
| P-07 | **Sem Development Build / variante debug** | `Build.cs:110` | Profiler não conecta; stack IL2CPP reduzido; diagnóstico só por logcat | otimizar para FPS real | não | 🟡 |
| P-08 | **Nenhum script de deploy**; `adb install` nem está escrito | grep `adb` em `.ps1/.cs` = só comentários | instalação/execução/leitura 100 % manuais | aparelho raramente no cabo | não | 🟡 |
| P-09 | **Dois adb de versões diferentes** (36.0.0 Unity, 37.0.1 PATH) | `adb version` nos dois | servidor adb reiniciado a cada troca; logcat/scrcpy/emulador caem | SDK avulso do Capacitor + SDK do Unity | não | 🟡 |
| P-10 | **Disco: 34 GB livres** | `Get-PSDrive` | emulador + imagem + AVD ≈ 10–14 GB; sobra pouca folga para pagefile/cache | 5,4 GB de APKs + 9,8 GB de Library + 4,4 GB `.gradle` | regeneráveis limpáveis | 🟡 |
| P-11 | Licença Personal exige **Hub aberto**; `Start-Sleep 25` fixo; um Unity por vez | `portao.ps1:8-16` | sem CI headless; build/portão/foto seriais | licenciamento Unity | contornado nos scripts | 🟡 |
| P-12 | **UAC não chega ao agente** | `LICOES.md` 08/09 | qualquer instalação de SDK/AVD/WHPX vira `.bat` para o Diretor | sessão sem elevação | `.bat` | 🟡 |
| P-13 | APK assinado com **keystore de debug** (release também), versionCode 1 fixo | `apksigner`; `launcher/build.gradle` | ok para teste; bloqueia loja; outra máquina = outra assinatura | fase | documentado como pendência G6 | 🟡 |
| P-14 | **Sem crash reporting, sem logger central, sem profiler/draw calls/temperatura** | Anexo B §6–7 | crash em emulador vai só para o tombstone do logcat | fase | não | 🟡 |
| P-15 | Docs desatualizados: `design/pipeline/ANDROID.md` (método `Build.Android` e `Assets/Editor/Build.cs` errados), `design/infra/servidor-03/06` (era Godot), `swarm.yaml` (paths `Assets/Scripts`), `.claude/launch.json` (`npm run dev` morto) | leitura | quem monta ambiente pelo doc erra | migração Godot→Unity sem varrer docs | não | 🟢 P3 |
| P-16 | Contagem do portão infla com 85 fotos puladas ("620" = 534 rodados) | `portao-playmode-resultados.xml skipped=85` | número de marketing; não é falso | `Assert.Ignore` sem GPU | `foto.ps1` cobre parcialmente | 🟢 |
| P-17 | 85,8 MB de binários fora do LFS; `castelo.glb`/`bau.glb`/luvas triplicados; 5,4 GB de APKs; 13 `.idx` órfãos; JDK 21 duplicado | Anexo D §6 | custo de clone/disco/ruído | histórico | não (não reescrever histórico) | 🟢 |
| P-18 | `mobile-godot/` e `roblox/` referência morta com LFS duplicado | `swarm.yaml`, `00-LEIA.md` | custo de clone | decisão do Diretor de manter | — | 🟢 |
| P-19 | `targetFrameRate = -1` = 30 FPS no Android (opção "Ilimitado" limita) | `Config.cs:71`; `PROJETO.md:368` | UX | semântica do Unity | `arkana_fps 300` só na automação | 🟢 |

## 20. Soluções já implementadas

Resumo (35 itens detalhados em Anexo C §5):

| Problema original | Como foi resolvido | Quando | Onde | Por quê | Limitações |
|---|---|---|---|---|---|
| Licença Personal: batchmode morre com exit 198 sem o Hub | scripts abrem o Hub pelo AppID (`shell:AppsFolder\UnityTechnologies.UnityHub_…!UnityHub`) e esperam 25 s; nunca `unityhub://` (abre "instalar editor") | 09–11/09 | `portao.ps1`, `build_apk.ps1`, `foto.ps1` | licença resolvida online pelo cliente do Hub | sleep fixo; AppID fixo; sem sessão sem desktop |
| `Start-Process -Wait` trava (VBCSCompiler vive 10 min); `ExitCode` vazio | `$P.WaitForExit()` + `$null = $P.Handle` | 12/09 | os 3 `.ps1` | quirks PowerShell/Roslyn | só em comentário |
| Um Unity por vez | raias compilam fora do Unity (Roslyn), coordenador roda um portão | 11–12/09 | `ARQUITETURA.md` | trava de instância | tudo serial |
| Unity reinicia o servidor adb e corta logcat | ler com `adb logcat -d` depois | 21/09 | `LICOES.md` | — | sem streaming durante build |
| MIUI recusa `INJECT_EVENTS`/`install -g` | extras de intent (`arkana_auto`, `arkana_fps`) + jogador automático; "Depuração USB (segurança)" libera o `input` | 12/09, 21/09 | `PartidaPeloAdb.cs`; `PROJETO.md:70,355-365` | testar sem dedo | depende de opção MIUI |
| `targetFrameRate=-1` = 30 FPS | pedir 300 na automação | 12/09 | `Main.cs:298` | padrão do Unity | opção "Ilimitado" da Config continua 30 |
| 60 → 16 FPS (ondas nunca medidas) | `BancadaDeCortes` mediu; SSAO off, sombra macia baixa, Qualidade real (Média 85 %/2×2048) | 21/09 `9749474` | `URP_Base*.asset`, `Config.cs`, `BancadaDeCortes.cs` | "FPS se mede" (25/08) | 34,6 FPS, não 60; perfil não se aplica no editor |
| `Shader.Find` null no aparelho; keywords `_EMISSION`/`_NORMALMAP` somem no APK | `AlwaysIncludedShaders` por código; molde-asset com keyword; ligar na importação | 09–16/09 | `Build.cs:58-93`, `ImportacaoArkana.cs`, `.mat` | build só leva variante referenciada | cada shader/keyword novo repete |
| `InitTestScene*`, `.utmp` no git | `.gitignore` | 12/09 | `.gitignore` | regeneráveis | — |
| APK espalhado por worktrees; `*.apk` no git | cópia datada só no clone principal; `*.apk` ignorado | 27/08 | `build_apk.ps1:27-34` | ordem do Diretor | sem etiqueta de branch |
| UAC não chega ao agente; Hub MSIX sem CLI | instalador elevado vira `.bat`; editor baixado direto da API de releases da Unity | 08–09/09 | `LICOES.md` | — | **vale para SDK/AVD/WHPX** |
| Ateliê 3D vazando no APK (Godot); arte sem cópia | portão de artefato; LFS para 160 referências | 25–26/08 | `.gitattributes` | — | cota LFS ≈ 2–3 clones/mês |
| Rig da Meshy com lossyScale 100; luva gigante; sem pulo/recuo | soquete que cancela escala; clipes do site; `BakeMesh` | 12–16/09 | `LuvaVisual`, `Mago` | FBX da Meshy | — |
| Boot com engasgos de 6 s | tela de carregamento em fatias de 12 ms | 13/09 | `TelaDeCarregamento` | — | `MontarEmFatias`/`ShaderVariantCollection` na fila |

## 21. Decisões técnicas existentes

28 decisões em Anexo C §7. As que governam a próxima etapa:

| Decisão | Motivo | Alternativas | Consequência |
|---|---|---|---|
| **Unity 6 e só Unity** (09/09) — "não pretendo mudar mais" | uma pessoa, dois jogos, uma engine; o desvio custou 13 dias | Godot (19/08–27/08), Unreal/Steam (27/08–09/09), ambos descartados | **nunca propor Unreal/Steam/Godot de novo** |
| **Android, celular; Poco F4 como régua** (25/08) | aparelho do Diretor, alto desempenho de 2022 | "intermediário genérico" (chute) | aparelho do Diretor ≠ jogador final (volta em G6) |
| **"Tamanho do APK não importa; FPS se mede, não se estima"** (25/08) | validar com o real; compressão na fase de loja | otimizar cedo | 176 → 228 MB; bancada de cortes |
| **IL2CPP + ARM64 somente, minSdk 26, targetSdk 35, Linear** | receita herdada do Limiar (08/09) | **motivo de excluir x86_64 não identificado no repositório** | APK não roda em AVD x86 |
| **Release sem Development Build** | otimizar para FPS real | — | Profiler remoto impossível |
| Lógica pura + `MonoBehaviour` casca; teste provado em vermelho; portão headless; fotos como critério | testável sem cena; "teste verde não diz se o mago saiu rosa" | — | 620 testes; 85 fotos |
| `design/` fonte da verdade; GDScript não se traduz | decisão não tem engine | traduzir | reescrita em 2 dias |
| **Sem pacote novo sem pedir; sem SDK/ferramenta sem fase ativa** (`ARQUITETURA.md`, `servidor-06`) | custo e manutenção | — | **instalar emulador/AVD/WHPX é pedido ao Diretor** |
| **Regras do repositório** (`CLAUDE.md` 01/09): sem pastas novas, `main` por ff-only, nunca reescrever histórico, regeneráveis apagáveis | disciplina | — | scripts de emulador teriam de morar em pasta existente |
| APK datado sempre no clone principal (27/08) | uma pasta só para o Diretor | — | `git-common-dir` no script |
| Qualidade Média padrão; DUPLA padrão; SSAO off + sombra baixa (21/09) — todos **vetáveis** | medição | Alta/SOLO | — |
| LFS para arte (26/08) | existia num lugar só | storage pago | cota ≈ 2–3 clones/mês |

## 22. Preparação para Android Emulator (análise, não implementação)

**Já pronto**
- SDK avulso com `cmdline-tools 19.0` (`sdkmanager`/`avdmanager`), `platform-tools`, `build-tools 35/36`, `platforms 35/36`, licenças aceitas, `ANDROID_HOME` definido; JDK 21 no PATH; `aapt2`/`apksigner` presentes.
- APK funcional de release (`arkana.apk`, 228 MB), package/activity conhecidos, **automação por intent** (`arkana_auto`, `arkana_fps`, `arkana_bancada`, `arkana_cortes`) e telemetria `ARKANA FPS/BANCADA` no logcat — tudo agnóstico de aparelho.
- GPU RTX 3050 com Vulkan 1.4 e OpenGL; VT-x ligado; CPU forte.
- Jogo 100 % local: nada de rede a simular.

**Falta**
- `emulator` (pacote do SDK), uma `system-image` (API ≥ 26; com Google APIs para tradução ARM se for x86_64), um AVD.
- **WHPX** (`HypervisorPlatform`) habilitado — admin + reinício.
- **RAM**: 8 GB não bastam para Unity + emulador; ou upgrade (1 slot DDR5 livre) ou usar o emulador só com o APK pronto e o Unity fechado.
- Um build com **x86_64** (ou aceitar imagem ARM64), o que exige mudar `Build.cs:44`.
- Script de deploy (`adb install -r` + `am start` + `logcat`) — hoje não existe nem para o aparelho físico.

**Compatível / reutilizável**
- `PartidaPeloAdb`, `MedidorDeFps`, `BancadaDeCortes`, os comandos `adb shell am start … --es arkana_auto partida` e `adb logcat -s Unity`; `build_apk.ps1` (com variante); `foto.ps1` como referência visual no PC.
- `sdkmanager`/`avdmanager` do SDK avulso (JDK 21 compatível).

**Possíveis conflitos**
- **ABI**: APK ARM64-only vs imagem x86_64 (não instala) — ou imagem ARM64 (sem aceleração de CPU em host x86: muito lenta).
- **Vulkan em Auto** vs GPU virtual do emulador (ANGLE/SwiftShader): risco de tela preta; GLES3 é o caminho seguro, mas o manifesto exige `glEsVersion 3.0` (a maioria das imagens atende).
- **Dois adb** (36 vs 37): reinícios de servidor derrubam o emulador e o logcat.
- **`Build.cs` regrava settings**: qualquer variante para emulador precisa viver no próprio `Build.cs` (ou num método novo), não no inspector.
- **Docker Desktop/WSL2 + VBS** mantêm o hipervisor ativo: WHPX é a única opção (HAXM/AEHD descartados).
- **Optimus**: o emulador pode cair na Intel UHD.
- **Disco**: +10–14 GB em 34 GB livres.
- **Um Unity por vez**: buildar variante x86_64 concorre com portão/fotos e faz `Library/Bee` (7,2 GB) crescer.

**Dependências a configurar depois (todas pedem decisão do Diretor)**
- `HypervisorPlatform` (admin, `.bat`), `sdkmanager "emulator" "system-images;android-3x;google_apis;x86_64"`, `avdmanager create avd`, unificar `adb` (PATH), variante de build (`ARM64 | X86_64`, e possivelmente `Development Build` + GLES3 explícito), script de deploy em pasta existente (`mobile-unity/`).

**Riscos**
- Emulador **não mede a GPU alvo** (Adreno 650): qualquer FPS de emulador é irrelevante para a régua do projeto ("FPS se mede no aparelho"); serve para crash, fluxo, Android-isms, automação sem cabo.
- Screenshot de emulador **não é "foto do aparelho"** (o Diretor julga pelo visual do Poco F4).
- Push de `main` + LFS (P0) e clone de ambiente novo competem pela mesma cota LFS mensal.
- Tempo: um segundo alvo IL2CPP dobra o build limpo (~5 min → ~10 min) nesta máquina.

## 23. Possíveis stacks futuras (sem escolher)

| Ferramenta | Finalidade | Necessária? | Custo | Compatibilidade | Vantagem | Desvantagem | Risco |
|---|---|---|---|---|---|---|---|
| **Android Emulator oficial** (pacote `emulator` + system-image + AVD via `sdkmanager`/`avdmanager`, sem Android Studio) | rodar o APK sem cabo | para o objetivo, sim | grátis; 10–14 GB disco; 2–3 GB RAM | exige WHPX ligado; imagem x86_64 exige build x86_64 (ou tradução ARM lenta) | oficial, headless (`-no-window`), snapshots, `adb` normal | RAM/WHPX; não mede Adreno; Vulkan frágil | alto na máquina atual |
| **ADB** (já existe) | instalar/executar/logcat | sim | 0 | 2 versões → unificar | já dominado no projeto | conflito de servidor | baixo |
| **Android Studio** | SDK Manager GUI, Device Manager, profiler | não (cmdline-tools bastam) | grátis; ~3 GB + RAM | — | GUI, profiler CPU/GPU/memória do app | pesado em 8 GB; redundante | médio |
| **SDK Command-line Tools** (já existe, 19.0) | instalar emulator/imagens, criar AVD | sim | 0 | JDK 21 ok | leve, scriptável | licenças/downloads de 1–2 GB | baixo |
| **scrcpy** (ausente) | espelhar/controlar o Poco F4 no PC | opcional | grátis, ~30 MB | adb | vídeo do aparelho real em vez de WhatsApp a 1 quadro/s; toque pelo PC | precisa cabo; sofre com reinício do adb | baixo |
| **logcat** (já usado) | erros/FPS/bancada | sim | 0 | — | única telemetria existente | sem persistência; cortado pelo Unity | baixo |
| **Ferramentas da engine**: Development Build + Unity Profiler, Frame Debugger, `ProfilerRecorder` | por quê do FPS (draw calls, GC, GPU time) | opcional, alta utilidade | 0 (variante de build) | requer `BuildOptions.Development` (mudar `Build.cs`) | números por sistema no aparelho real | build maior/mais lento; Profiler pesa em 8 GB | baixo |
| **Profiling de GPU** (Snapdragon Profiler para Adreno; RenderDoc/AGI) | custo real de shaders no Poco F4 | opcional | grátis | só aparelho físico | mede a GPU alvo | curva de aprendizado; cabo | baixo |
| **Automação de build** (já existe `build_apk.ps1`; variante x86_64/dev) | APKs por alvo | necessária se emulador | 0 | `Build.cs` | reproduzível | segundo alvo IL2CPP | baixo |
| **Automação de deploy** (script `adb install -r` + `am start` + `logcat -d`, em `mobile-unity/`) | fechar o ciclo build → aparelho/emulador | necessária | 0 | adb | hoje é 100 % manual | regra "sem pastas novas" | baixo |
| **CI/CD** (GitHub Actions self-hosted ou nuvem) | portão/build automático | não agora (`servidor-03`: "em G5") | runner na própria máquina ou minutos pagos | licença Personal exige Hub aberto → runner self-hosted com desktop | — | inviável headless na nuvem sem licença | médio |
| Alternativas de terceiros (BlueStacks, LDPlayer, Genymotion, WSA) | rodar APK | não | grátis/pagos | WSA descontinuado; BlueStacks/LDPlayer usam o próprio hipervisor (conflito com WSL2) | tradução ARM embutida | não oficial; sem `adb` limpo; RAM | médio |

Decisão definitiva fica para a próxima etapa.

## 24. Fluxo ideal futuro (proposta — NÃO implementado)

```
DESENVOLVIMENTO (Unity fechado ou aberto, um por vez)
      ↓  portao.ps1 (620 testes) · foto.ps1 (fotos no PC)
BUILD  build_apk.ps1 [variante: ARM64 para o Poco F4 | ARM64+x86_64 (+dev) para emulador]
      ↓  Builds/testes/arkana-<data>[-x86].apk
APK
      ↓  script de deploy (a criar, em mobile-unity/): adb -s <serial> install -r
ANDROID EMULATOR (AVD x86_64 API 34/35, WHPX, -gpu host, Unity fechado)  ─ou─  Poco F4 por USB
      ↓
INSTALL → EXECUTE  adb shell am start … --es arkana_auto partida --ei arkana_fps 300 [--ei arkana_bancada 1]
      ↓
TEST  partida automática (crash? fluxo? Android-isms?)  ·  no aparelho real: FPS e visual (a régua)
      ↓
LOGS / DEBUG  adb logcat -d -s Unity | grep ARKANA  ·  screencap  ·  (dev build) Profiler
      ↓
CORREÇÃO → NOVO BUILD
```

> Isso é uma proposta futura e **NÃO foi implementado nesta etapa.** O emulador entra como "quando não houver cabo"; a medição de FPS e o veredito visual continuam no Poco F4.

## 25. Comandos utilizados na auditoria

Cada raia lista os seus com `Comando / Objetivo / Resultado` (Anexo A "COMANDOS EXECUTADOS" — 36; Anexo B "COMANDOS EXECUTADOS"; Anexo C §10.1; Anexo D §8.2). Comandos executados **pelo coordenador** (todos de leitura, Git Bash):

```
Comando:   ls "C:/Users/VINICIUS/Videos/MEUS PROJETOS/Agentes"; sed -n 1,420p ORQUESTRADOR.md; head -150 manifest.yaml; tail -40 _memoria/LICOES.md; cat _memoria/arkana.md
Objetivo:  descobrir a biblioteca de agentes, regras do orquestrador e memória do projeto
Resultado: 30 agentes; swarm.yaml do ARKANA limita 4 subagents/leva; memória aponta design/PROJETO.md

Comando:   ls -la; git status --short; git log --oneline -1; git branch --show-current; git worktree list; du -sh --exclude=.git *
Objetivo:  estado inicial do worktree e tamanhos de 1º nível
Resultado: limpo; 0a94c2f; 2 worktrees; arte 531M, mobile-unity 481M, mobile-godot 101M (sem Library/Builds no worktree)

Comando:   find . -maxdepth 2 -iname "*.md"; find . -maxdepth 3 -iname "*.ps1" …; cat mobile-unity/ProjectSettings/ProjectVersion.txt
Objetivo:  localizar docs, scripts e versão do Unity
Resultado: 10 .md de entrada; 4 .ps1; 6000.3.23f1 (09d2ecc7fb28)

Comando:   cat swarm.yaml README.md CHANGELOG.md mobile-unity/00-LEIA.md mobile-godot/00-LEIA.md design/00-LEIA.md; head -120 mobile-unity/ARQUITETURA.md; sed -n 1,140p design/PROJETO.md
Objetivo:  contexto do projeto para decompor as raias
Resultado: Unity-only desde 09/09; CONTINUAR DAQUI de 21/09; portão/foto/build documentados

Comando:   cat mobile-unity/build_apk.ps1 portao.ps1 foto.ps1 Packages/manifest.json; ls mobile-unity Assets
Objetivo:  pipeline real e pacotes
Resultado: Hub por AppID, WaitForExit, -executeMethod Arkana.EditorTools.Build.Android; 5 pacotes diretos

Comando:   find design -maxdepth 2 -type f; find arte -maxdepth 2 -type d; find mobile-unity/Assets/_Arkana -maxdepth 2 -type d; find … -name '*.cs' | wc -l; git tag; git branch -a; git lfs ls-files | wc -l; cat .gitignore; ls -la M/mobile-unity/Builds M/mobile-unity/Builds/testes
Objetivo:  mapa de pastas, contagens, LFS, APKs existentes
Resultado: 189 .cs (64 teste); 0 tags; 392 LFS; 26 APKs 176–279 MB

Comando:   cat design/pipeline/ANDROID.md design/infra/servidor-06-ferramentas-locais.md
Objetivo:  intenção documentada para Android e ferramentas locais
Resultado: ANDROID.md com método/paths errados; servidor-06 da era Godot

Comando:   ls M/mobile-unity/Library/Bee/Android/Prj/IL2CPP/Gradle; cat …/launcher/build.gradle …/unityLibrary/build.gradle …/build.gradle …/gradle.properties …/settings.gradle …/gradle/wrapper/gradle-wrapper.properties
Objetivo:  projeto Gradle gerado (compileSdk, NDK, abiFilters, assinatura) — lacuna apontada pela Raia B
Resultado: compileSdk 35, buildTools 36.0.0, NDK 27.2.12479018, JDK 17 do Unity, abiFilters arm64-v8a, signingConfigs.debug no release, AGP 9.0.0, Gradle 9.1.0

Comando:   cat …/Gradle/launcher/src/main/AndroidManifest.xml …/unityLibrary/src/main/AndroidManifest.xml; grep -E "uses-permission|uses-feature|activity|screenOrientation" …/launcher/build/intermediates/merged_manifest*/release/*/AndroidManifest.xml
Objetivo:  manifesto efetivo (permissões, features, activity, orientação)
Resultado: INTERNET + DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION; glEsVersion 3.0; Vulkan required=false; UnityPlayerGameActivity; userLandscape

Comando:   git rev-parse --short main origin/main; git rev-list --count origin/main..main; git log -1 --date=short origin/main   (no clone principal)
Objetivo:  confirmar o achado P0 da Raia D
Resultado: main 0a94c2f, origin/main 0694794 (2026-09-09), 93 commits à frente

Comando:   cat > _memoria/STATUS_arkana-auditoria-android.md; sed -i (atualizar estado das raias); concatenação dos 5 relatórios neste arquivo
Objetivo:  única escrita: pasta de saída pedida pelo Diretor e placar da missão (fora do projeto)
Resultado: arquivo único na raiz do clone principal; STATUS na biblioteca de agentes
```

## 26. Ferramentas utilizadas na auditoria

| Ferramenta | Versão | Finalidade | Localização |
|---|---|---|---|
| Git | 2.54.0.windows.1 | status, log, rev-list, lfs, ls-tree, diff, cat-file | `C:\Program Files\Git\` |
| git-lfs | 3.7.1 | inventário LFS | `C:\Program Files\Git\cmd\` |
| Git Bash (coreutils 8.32) | — | cat, grep, find, du, sed, sha256sum, unzip 6.00 | `C:\Program Files\Git\usr\bin\` |
| Windows PowerShell | 5.1.19041.6456 | CIM/WMI, registro, variáveis, tamanhos, `where.exe` | System32 |
| cmd.exe / systeminfo / wsl / bcdedit (falhou: admin) | 10.0.19045 | SO, hipervisor, WSL | System32 |
| adb (SDK avulso) | 1.0.41 / 37.0.1 | `adb version`, `adb devices -l` | `%LOCALAPPDATA%\Android\Sdk\platform-tools\` |
| adb (Unity) | 1.0.41 / 36.0.0 | `adb version` | `…\AndroidPlayer\SDK\platform-tools\` |
| sdkmanager | 19.0 | `--list_installed`, `--version` | `%LOCALAPPDATA%\Android\Sdk\cmdline-tools\latest\bin\` |
| aapt2 / apksigner | build-tools 36.0.0 | `dump badging`, `verify --print-certs` (metadados e certificado público) | `…\AndroidPlayer\SDK\build-tools\36.0.0\` |
| keytool / java | Temurin 17.0.18 (Unity) e 21.0.12 | `-printcert`, `-version` | OpenJDK do Unity; Eclipse Adoptium |
| vulkaninfo / nvidia-smi | Vulkan 1.4.321 / driver 591.74 | GPU | System32 / driver NVIDIA |
| winget / python / node / docker / dotnet | 1.29.380 / 3.14.4 / 24.15.0 / 29.4.0 / 10.0.400 | `--version`, `winget list` | — |
| Claude Code (orquestrador + 4 subagents) | Fable 5.1 | leitura, síntese, redação | — |

## 27. Arquivos importantes

Lista completa (45 itens) em Anexo D §7 e Anexo B §12. Os indispensáveis para o agente do emulador:

| Arquivo | Função | Por que é importante |
|---|---|---|
| `design/PROJETO.md` | memória do projeto, CONTINUAR DAQUI, comandos `adb` reais (l. 355–372), medições | único registro do processo de deploy/medição |
| `mobile-unity/Assets/_Arkana/Editor/Build.cs` | força ABI, IL2CPP, minSdk/targetSdk, package, `BuildOptions` | **único lugar** para variante x86_64 / dev build / GLES3 |
| `mobile-unity/build_apk.ps1` | invoca o Unity; APK datado no clone principal | pipeline de build; dependência do Hub |
| `mobile-unity/portao.ps1`, `foto.ps1` | testes headless / fotos com GPU | critério de verde e de visual |
| `mobile-unity/Assets/_Arkana/Scripts/PartidaPeloAdb.cs`, `MedidorDeFps.cs`, `BancadaDeCortes.cs` | extras de intent, FPS no logcat, bancada de cortes | automação sem toque, reaproveitável em emulador |
| `mobile-unity/Assets/_Arkana/Scripts/Menu/Config.cs` | perfis de qualidade, FPS, vSync | define o que o emulador vai renderizar por padrão |
| `mobile-unity/ProjectSettings/ProjectSettings.asset`, `ProjectVersion.txt`, `QualitySettings.asset` | estado serializado (regravado pelo Build.cs) | leitura do que o último build usou |
| `mobile-unity/Assets/_Arkana/Settings/URP_Base.asset`, `URP_Base_Renderer.asset` | pipeline URP, SSAO off | custo de GPU |
| `mobile-unity/Packages/manifest.json`, `packages-lock.json` | dependências | sem rede; burst transitivo |
| `M/mobile-unity/Builds/build.log`, `Library/Bee/Android/Prj/IL2CPP/Gradle/**` | log e projeto Gradle gerado | toolchain efetiva, manifesto, assinatura |
| `mobile-unity/00-LEIA.md`, `ARQUITETURA.md` | manual operacional; contratos; "sem pacote novo sem pedir" | regras para mexer |
| `CLAUDE.md`, `swarm.yaml`, `.gitignore`, `.gitattributes` | regras do repositório, LFS | onde um script novo pode morar; cota LFS |
| `design/pipeline/ANDROID.md`, `design/infra/servidor-06-ferramentas-locais.md` | identidade do app / ferramentas locais | úteis, mas **desatualizados** |
| `C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\LICOES.md` | lições (UAC, Hub, adb, licença) | armadilhas já pagas |

## 28. Mapa do projeto

```
ARKANA/
├── README.md · CLAUDE.md · CHANGELOG.md · swarm.yaml · .gitignore · .gitattributes     [docs/config]
├── .claude/  launch.json (npm run dev — morto) · worktrees/<esta auditoria>            [config/ignorado]
├── ARKANA_AUDITORIA_TECNICA_ANDROID.md (este arquivo — não commitado)                    [docs]
├── design/   PROJETO.md (memória) · PONTE.md · gdd/ · personagens/ (20) · cenario/ · infra/ · pipeline/ (ANDROID.md) · referencias/   [decisão]
├── arte/     personagens/ (LFS) · cenario/ (originais Meshy ignorados) · audio/ · prompts/ · tools/{meshy,blender} (.env ignorado)   [assets/scripts]
├── mobile-unity/                                                                        [PRODUTO — Unity 6000.3.23f1, Android]
│   ├── 00-LEIA.md · ARQUITETURA.md · portao.ps1 · foto.ps1 · build_apk.ps1 · importar_mago.ps1   [docs/build/scripts]
│   ├── Packages/ manifest.json · packages-lock.json                                     [config]
│   ├── ProjectSettings/ ProjectSettings.asset · ProjectVersion.txt · QualitySettings · GraphicsSettings …   [config/android]
│   ├── Assets/ URP GlobalSettings · _Arkana/{Editor (Build.cs, MainSceneBuilder), Scenes (Main.unity), Settings (URP_Base*),
│   │            Resources (477 MB: magos, glb, shaders), Scripts (Core, World, Gameplay+Habilidades, Terrain, Characters, UI, Menu, Audio,
│   │            Main.cs, PartidaPeloAdb.cs, MedidorDeFps.cs, BancadaDeCortes.cs), Tests (EditMode 59, PlayMode 5)}   [código/gameplay/assets/testes]
│   ├── Builds/  arkana.apk · build.log · testes/ (25 APKs) · Arkana_BurstDebugInformation_DoNotShip/   [ignorado — só no clone principal]
│   ├── Library/ (9,8 GB; Bee/Android/Prj/IL2CPP/Gradle = projeto Gradle gerado) · Logs/ · UserSettings/ · .utmp/   [ignorado/regenerável]
├── mobile-godot/  Godot 4.4.1 — referência de leitura, não roda (desinstalado 09/09)   [referência morta]
└── roblox/        Campo de Provas (Rojo/Luau) — intocado                                 [referência morta]
```

## 29. Matriz de estado

| Item | Estado | Versão | Observação |
|---|---|---|---|
| Engine | 🟢 OK | Unity 6000.3.23f1 (09d2ecc7fb28), URP 17.3.0 | projeto = instalado = scripts |
| Android SDK (Unity) | 🟢 OK | platforms 34/35/36, build-tools 36.0.0, cmdline-tools 16.0, cmake 3.22.1 | usado no build; sem emulator |
| Android SDK (avulso) | 🟡 ATENÇÃO | platforms 35/36, build-tools 35/36, cmdline-tools 19.0 | `ANDROID_HOME`; sem emulator/images; resquício Capacitor |
| Platform Tools | 🟡 ATENÇÃO | 36.0.0 (Unity) / 37.0.1 (PATH) | duas versões → conflito de servidor adb |
| Build Tools | 🟢 OK | 36.0.0 (+35.0.0) | aapt2/apksigner disponíveis |
| JDK | 🟡 ATENÇÃO | Temurin 17.0.18 (Unity, usado) / 21.0.12 ×2 (PATH, duplicado) | compatível; redundância |
| ADB | 🟡 ATENÇÃO | 1.0.41 | funciona; aparelho não conectado; 2 cópias |
| Android Studio | ⚪ NÃO EXISTE | — | — |
| Emulator | ⚪ NÃO EXISTE | — | nenhum `emulator.exe`; sem system-images |
| AVD | ⚪ NÃO EXISTE | — | `~/.android/avd` ausente |
| Gradle | 🟢 OK | 9.1.0 / AGP 9.0.0 (embutidos) | sem templates custom; sem Gradle avulso |
| NDK | 🟢 OK | r27c (27.2.12479018) | só no Unity |
| CMake | 🟢 OK | 3.22.1 | só no SDK do Unity |
| Build Android | 🟢 OK | `build_apk.ps1` + `Build.cs`; último 21/09 Succeeded 1:36 | release, ARM64 only, sem dev build, Hub obrigatório |
| Deploy | 🔴 PROBLEMA | — | manual (Drive/USB), sem script, aparelho intermitente |
| Debug | 🟡 ATENÇÃO | logcat + intents + fotos no PC | sem crash report, sem profiler |
| Logs | 🟡 ATENÇÃO | `ARKANA FPS/BANCADA` no logcat; `Logs/` no PC | nada persiste no aparelho |
| Git | 🔴 PROBLEMA | 231 commits, limpo, sem tags | **93 commits + 504 MB LFS não enviados** |
| Virtualização (WHPX) | 🔴 PROBLEMA | hipervisor ativo, WHPX off | bloqueia aceleração do emulador |
| RAM | 🔴 PROBLEMA | 8 GB, < 1 GB livre | 1 slot livre |
| Disco | 🟡 ATENÇÃO | 34 GB livres | Library/Builds regeneráveis |
| GPU | 🟢 OK | RTX 3050 6 GB, Vulkan 1.4 | Optimus |
| Rede/multiplayer | ⚪ NÃO EXISTE | — | intenção G5 |
| Testes | 🟢 OK | 502 EditMode + 58 PlayMode; 534 rodados + 85 fotos puladas; 0 falhas | sem CI, sem testes no aparelho |
| CI | ⚪ NÃO EXISTE | — | licença Personal + Hub dificultam |
| Keystore de release | ⚪ NÃO EXISTE | debug `CN=Android Debug` | pendência G6 |
| scrcpy | ⚪ NÃO EXISTE | — | útil para o cabo |

## 30. Classificação final

- 🟢 **OK**: Engine, SDK do Unity, Build Tools, Gradle, NDK, CMake, Build Android, GPU, Testes, segredos (nenhum versionado), `.gitignore`/`.meta`.
- 🟡 **ATENÇÃO**: SDK avulso, Platform Tools (2 versões), JDK (duplicado), ADB, Debug, Logs, Disco (34 GB), Graphics API em Auto, `Build.cs` regravando settings, licença/Hub/um Unity por vez, keystore de debug, docs desatualizados, contagem do portão, LFS fora de regra.
- 🔴 **PROBLEMA**: Git não enviado (P0), WHPX desabilitado, RAM 8 GB, APK ARM64-only (para emulador), Deploy manual sem script.
- ⚪ **NÃO EXISTE**: Android Studio, Emulator, System images, AVD, Rede, CI, keystore de release, scrcpy, crash reporting, profiler no APK.

Critérios: 🟢 existe, versão coerente, funciona sem ressalva; 🟡 existe mas com risco/duplicidade/limitação documentada; 🔴 bloqueia ou põe em risco o objetivo (emulador) ou o projeto; ⚪ ausente.

## 31. Recomendações — somente análise

### Recomendação imediata
1. **Enviar `main` e o LFS para o GitHub** (P0) antes de qualquer outra coisa — com o Diretor ciente de que consome ~504 MB da cota LFS mensal (1 GB). É a única ação que protege 12 dias de trabalho; não é sobre emulador.
2. **Decidir o que o emulador deve responder.** Os docs são explícitos: FPS e visual se julgam no Poco F4. Se o objetivo é "testar sem cabo" (crash, fluxo, partida automática, Android-isms), o emulador serve; se é medir desempenho, não serve, e a alternativa é ter o aparelho mais tempo no cabo (scrcpy + script de deploy).
3. **Ligar WHPX** (`HypervisorPlatform`) — passo de admin + reinício, feito pelo Diretor (`.bat`), não pelo agente. Sem isso nada do resto vale.

### Recomendação seguinte
4. Unificar o `adb` (uma versão no PATH) — resolve o "Unity reinicia o servidor" de tabela.
5. Escrever o **script de deploy** (`adb install -r` + `am start … --es arkana_auto` + `logcat -d`) em `mobile-unity/` (pasta existente, regra do CLAUDE.md) — vale para aparelho e emulador.
6. Só então: `sdkmanager "emulator" "system-images;…"`, `avdmanager create avd` (AVD enxuto: 1,5–2 GB RAM, `-gpu host`, `-no-window` quando for automação) e uma **variante de build** no `Build.cs` (`ARM64 | X86_64`, avaliar `Development Build` e GLES3 explícito) — cada uma dessas é "pacote/ferramenta nova" e pede o sim do Diretor.
7. Considerar upgrade de RAM (1 slot DDR5 livre): é o único jeito de rodar Unity e emulador juntos.

### O que NÃO fazer agora
- Não instalar Android Studio (pesado, redundante com cmdline-tools, 8 GB de RAM).
- Não instalar HAXM/AEHD (inviáveis com o hipervisor ativo) nem desligar WSL2/Docker/VBS para "liberar" HAXM.
- Não trocar a ABI do build principal para x86_64 (o Poco F4 é ARM64; x86_64 é variante de teste, não o produto).
- Não usar FPS de emulador como medição; não usar screenshot de emulador como "foto do aparelho".
- Não apagar `Library/` (7,2 GB de cache IL2CPP que faz o build incremental sair em 1:36) nem `Builds/testes/` sem o Diretor (regra: APK fica na pasta).
- Não reescrever histórico do git para tirar os 86 MB fora do LFS (CLAUDE.md).
- Não criar pastas novas para scripts de emulador (CLAUDE.md).
- Não propor Unreal/Steam/Godot (decisão de 09/09).

### Perguntas em aberto (para decidir antes de implementar)
1. O emulador é para **testar sem cabo** (funcional) ou o Diretor espera **medir** nele? (define se vale o custo)
2. Quem executa os passos com admin (WHPX, eventual instalação): o Diretor via `.bat`? Quando?
3. Aceita-se um **segundo alvo de build** (x86_64, +~5 min por build limpo, `Library/Bee` maior) ou prefere-se imagem ARM64 lenta / só aparelho?
4. Deve a variante de emulador ser **Development Build** (Profiler, stack traces) mesmo sendo diferente do APK do Diretor?
5. Onde mora o script de deploy sem criar pasta: `mobile-unity/` ao lado dos `.ps1`?
6. Cota LFS: o push de `main` (P0) e um eventual clone limpo cabem no mesmo mês?
7. Upgrade de RAM está na mesa? Sem ele, a regra prática é "Unity fechado enquanto o emulador roda".
8. Os docs desatualizados (`ANDROID.md`, `servidor-03/06`, `swarm.yaml`, `launch.json`) devem ser corrigidos nesta fase ou na fase do emulador?
9. O aparelho do Diretor pode ficar mais tempo no cabo (scrcpy + deploy automático) — o que talvez resolva o problema real (indisponibilidade) sem emulador?

---

**Nenhuma alteração de implementação foi realizada.** Nada instalado, nada configurado, nenhum AVD, nenhum build, nenhum commit.


---

# ANEXO A — AMBIENTE DA MÁQUINA (relatório da raia devops-sre)


- **Projeto:** ARKANA (Unity 6 → Android)
- **Data:** 2026-09-23
- **Autor:** agente devops-sre (Principal SRE / Release Engineering)
- **Aviso:** auditoria SOMENTE LEITURA. Nada foi instalado, desinstalado, configurado, aceito ou alterado. Únicos efeitos colaterais inevitáveis de comandos de inspeção, ambos registrados no final: (a) `adb devices` subiu o servidor adb (processo `adb.exe` PID 17160, do SDK avulso); (b) `sdkmanager --list_installed` consultou o repositório remoto do Google (só leitura de rede). Um arquivo temporário foi gravado em `C:\Users\VINICIUS\AppData\Local\Temp\claude\tamanhos_arkana.txt`.
- **Finalidade:** dar ao próximo agente o retrato fiel da máquina para decidir a estratégia de Android Emulator local.

---

## 1. Sistema operacional

| Item | Valor |
|---|---|
| Nome/edição | Microsoft Windows 10 Pro |
| Versão/build | 10.0.19045 (22H2) — `[Environment]::OSVersion` = `Microsoft Windows NT 10.0.19045.0` |
| Arquitetura | 64 bits |
| Fabricante/modelo | Acer Nitro ANV15-51 (notebook) |
| PowerShell | 5.1.19041.6456 (Windows PowerShell) |

## 2. CPU e virtualização

| Item | Valor |
|---|---|
| Modelo | 13th Gen Intel(R) Core(TM) i7-13620H (GenuineIntel) |
| Núcleos / threads | 10 núcleos / 16 threads (híbrido P+E) |
| Clock base | 2400 MHz |
| `VirtualizationFirmwareEnabled` | **False** (ver nota) |
| `VMMonitorModeExtensions` | **False** (ver nota) |
| `SecondLevelAddressTranslationExtensions` | **False** (ver nota) |
| `Win32_ComputerSystem.HypervisorPresent` | **True** |
| `systeminfo` → Requisitos do Hyper-V | "Hipervisor detectado. Recursos necessários para o Hyper-V não serão exibidos." |

> **Nota (importante):** os três `False` acima NÃO significam VT-x desligado na BIOS. Quando o Windows já roda em cima do hipervisor da Microsoft (`HypervisorPresent = True`), o WMI reporta esses campos como `False` porque o SO convidado não enxerga as extensões diretamente. O `systeminfo` confirma: "Hipervisor detectado". Conclusão: **VT-x/EPT está ligado na BIOS e o hipervisor do Windows está ATIVO no boot.**

### Recursos opcionais do Windows (`Win32_OptionalFeature`, não exige admin; InstallState 1 = habilitado, 2 = desabilitado)

| Recurso | Estado |
|---|---|
| `HypervisorPlatform` (**WHPX** — Windows Hypervisor Platform) | **2 = DESABILITADO** 🔴 |
| `Microsoft-Hyper-V` / `-All` / `-Hypervisor` / `-Services` / `-Management-*` | 2 = desabilitado |
| `VirtualMachinePlatform` | **1 = HABILITADO** (é o que liga o hipervisor no boot — usado pelo WSL2/Docker Desktop) |
| `Microsoft-Windows-Subsystem-Linux` (WSL) | 1 = habilitado |
| `Containers`, `Containers-DisposableClientVM` (Windows Sandbox) | 2 = desabilitado |

### Serviços

| Serviço | Estado | Início |
|---|---|---|
| `HvHost` | Running | Manual |
| `vmcompute` | Running | Manual |
| `hns` | Running | Manual |
| `WSLService` | Running | Automatic |
| `LxssManager` | Stopped | Manual |
| `com.docker.service` | Stopped | Disabled |
| `WindowsHypervisorPlatform` / `vmms` | não existem como serviço (esperado: WHPX é recurso, não serviço; `vmms` só existe com Hyper-V completo) |

### VBS / Core Isolation (`Win32_DeviceGuard`)

| Campo | Valor | Leitura |
|---|---|---|
| `VirtualizationBasedSecurityStatus` | **2** | VBS **ligado e rodando** |
| `SecurityServicesRunning` | `{0}` | nenhum serviço (HVCI/Memory Integrity e Credential Guard **desligados**) |
| `SecurityServicesConfigured` | `{0}` | nenhum configurado |
| Registro `HKLM:\...\Control\DeviceGuard` (`EnableVirtualizationBasedSecurity`, `RequirePlatformSecurityFeatures`, `HypervisorEnforcedCodeIntegrity`) | vazios | VBS não foi forçado por política — está rodando pelo padrão do Windows + VirtualMachinePlatform |

### WSL / Docker (explicam por que o hipervisor está ativo)

- `wsl --status`: distribuição padrão `docker-desktop`, versão padrão 2.
- `wsl -l -v`: `docker-desktop` — **Stopped** — v2.
- Docker Desktop 4.69.0 instalado (`C:\Program Files\Docker`), `AutoStart = False` no `settings-store.json`, mas há entrada `Docker Desktop` na chave `HKCU\...\Run` (inicia com o login). No momento **nenhum** processo `vmmem`/`Docker Desktop`/`com.docker.backend` está rodando.

### NÃO verificado
- `Get-WindowsOptionalFeature -Online` → **"A operação solicitada requer elevação."** (substituído por `Win32_OptionalFeature`, que dá o mesmo resultado sem admin).
- `bcdedit /enum {current}` e `bcdedit` → **"Acesso negado."** (exige admin). Portanto `hypervisorlaunchtype` não foi lido diretamente; a evidência indireta (`HypervisorPresent=True`, `HvHost`/`vmcompute` rodando, VBS status 2, VirtualMachinePlatform habilitado) é suficiente para concluir que está em `Auto`.

## 3. RAM

| Item | Valor |
|---|---|
| Total física | **8 GB** (8.279.416.832 bytes; `TotalVisibleMemorySize` = 8.085.368 KB ≈ 7,7 GB visíveis) |
| Livre no momento (1ª leitura) | 852.728 KB ≈ **0,83 GB** |
| Livre no momento (2ª leitura, minutos depois) | **486 MB** |
| Commit total / limite | 20.573 MB / 24.031 MB (pagefile de 16.135 MB alocado, uso atual 3.645 MB, pico 4.440 MB) |
| Módulos | **1 × 8 GB** A-DATA DDR5 (SMBIOS tipo 34 = DDR5), 5600 MT/s nominal, configurado a 5200, slot `Controller1-ChannelA-DIMM0` |
| Slots | 2 (`MemoryDevices = 2`), máximo 64 GB (`MaxCapacityEx = 67108864` KB) → **há 1 slot livre** |

> Leitura: a máquina está com **8 GB e menos de 1 GB livre** durante a auditoria (Lightroom 960 MB, 3 processos `claude` ~885 MB, Chrome, Defender). Está paginando (commit 20,5 GB em 24 GB de limite). Um Android Emulator padrão pede 2 GB para o convidado + ~1 GB de overhead; junto com Unity Editor (tipicamente 3–6 GB neste projeto) **não cabe com folga em 8 GB**.

## 4. GPU

| GPU | VRAM (WMI) | Driver | Data | Vulkan |
|---|---|---|---|---|
| NVIDIA GeForce RTX 3050 6GB Laptop GPU | 4.293.918.720 (WMI trunca em 4 GB; `nvidia-smi` mostra **6144 MiB**, 4046 MiB em uso agora) | 32.0.15.9174 (`nvidia-smi`: **591.74**, CUDA 13.1, modo WDDM) | 29/12/2025 | apiVersion 1.4.325, driverName NVIDIA |
| Intel(R) UHD Graphics (iGPU) | 2.147.479.552 (compartilhada) | 32.0.101.7088 | 16/06/2026 | apiVersion 1.4.323, driver 101.7088 |
| Vulkan runtime | `C:\Windows\System32\vulkaninfo.exe` presente; **Vulkan Instance Version 1.4.321** | | | |

> Leitura: a GPU **serve** ao emulador em modo `-gpu host` (OpenGL/ANGLE ou Vulkan via NVIDIA). Vulkan 1.4 disponível nas duas GPUs. Ponto de atenção: notebook híbrido (Optimus) — o emulador pode cair na Intel UHD se o perfil gráfico do Windows não fixar a NVIDIA para `emulator.exe`/`qemu-system-x86_64.exe`. Modo de vídeo atual 1920×1080. A RTX 3050 está com **4 GB de 6 GB de VRAM ocupados** (Lightroom/Chrome) no momento da leitura.

## 5. Armazenamento

| Unidade | Tipo | FS | Total | Livre | Observação |
|---|---|---|---|---|---|
| C: | SSD NVMe `SM2P41C8-512GC5` (Healthy) | NTFS | 476,3 GB | **34,0 GB** 🟡 | única unidade; projeto, Unity, SDK e pagefile (16 GB) estão nela |

### Tamanho das pastas (medido com `Get-ChildItem -Recurse -File | Measure-Object Length -Sum`)

| Pasta | Tamanho | Arquivos |
|---|---|---|
| `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA` (sem `.git`) | **18.666 MB (~18,2 GB)** | 34.215 |
| └ `.git` | 1.834 MB | 1.480 |
| └ `mobile-unity` | 15.898 MB | 31.785 |
| &nbsp;&nbsp;&nbsp;└ `mobile-unity\Library` (regenerável) | **9.947 MB** | 30.923 |
| &nbsp;&nbsp;&nbsp;└ `mobile-unity\Builds` (APKs) | **5.452 MB** | 28 |
| &nbsp;&nbsp;&nbsp;└ `mobile-unity\Assets` | 479 MB | 702 |
| &nbsp;&nbsp;&nbsp;└ `mobile-unity\Logs` | 18 MB | 45 |
| &nbsp;&nbsp;&nbsp;└ `mobile-unity\Temp`, `obj` | ausentes | — |
| └ `arte` | 1.557 MB | 401 |
| └ `.claude\worktrees` (este worktree de auditoria) | 1.110 MB | 1.555 |
| └ `mobile-godot` (descartado, ainda no repositório) | 100 MB | 330 |
| └ `design`, `roblox` | 0,8 MB / 1,1 MB | 93 / 43 |
| `C:\Program Files\Unity\Hub\Editor\6000.3.23f1` | **17.610 MB** | 95.004 |
| └ `...\PlaybackEngines\AndroidPlayer` (SDK+NDK+JDK+Gradle do Unity) | 9.683 MB | 45.948 |
| `%LOCALAPPDATA%\Android\Sdk` (SDK avulso) | 664 MB | 23.026 |
| `%USERPROFILE%\.gradle` (caches) | 4.397 MB | 9.695 |

> Leitura: **34 GB livres é pouco** para adicionar um emulador: `emulator` (~400 MB) + 1 system image x86_64 API 34/35 com Google APIs (~1,5–2 GB) + AVD com disco de 6–8 GB + snapshots (2–4 GB) ≈ **10–14 GB**. Cabe, mas deixaria ~20 GB, e o Windows/Unity precisam de folga para pagefile e cache de build. Regeneráveis limpáveis a qualquer momento (CLAUDE.md): `mobile-unity\Library` (9,9 GB) e APKs velhos em `Builds` (5,4 GB — verificar antes; APK "fica na pasta" por regra do Diretor).

## 6. Android SDK

Existem **duas** instalações de SDK na máquina. Nenhuma tem `emulator`, `system-images` ou `ndk`.

### 6.1 SDK avulso (do protótipo Capacitor) — `C:\Users\VINICIUS\AppData\Local\Android\Sdk`

```
Ferramenta: Android SDK (avulso, instalado via cmdline-tools)
Versão: cmdline-tools 19.0; platform-tools 37.0.1; build-tools 35.0.0 e 36.0.0; platforms android-35 (rev 2) e android-36 (rev 2)
Local: C:\Users\VINICIUS\AppData\Local\Android\Sdk  (ANDROID_HOME do usuário aponta aqui; ANDROID_SDK_ROOT não definido)
Estado: funcional para adb/sdkmanager; SEM emulator, SEM system-images, SEM ndk, SEM cmake; 664 MB
Usada pelo projeto? NÃO pelo Unity (Unity usa o SDK embutido). SIM indiretamente: é o adb que está no PATH e o que os scripts do projeto chamam por "adb".
```

| Componente | Presente? | Versão (`source.properties` / `sdkmanager --list_installed`) |
|---|---|---|
| `platforms\` | sim | android-35 (rev 2), android-36 (rev 2) |
| `build-tools\` | sim | 35.0.0, 36.0.0 |
| `platform-tools\` | sim | **37.0.1** (adb 1.0.41, `37.0.1-15733141`) |
| `cmdline-tools\latest` | sim | **19.0** (`sdkmanager --version` = 19.0) |
| `emulator\` | **não** ⚪ | — |
| `system-images\` | **não** ⚪ | — |
| `ndk\` / `ndk-bundle\` | não | — |
| `cmake\` | não | — |
| `licenses\` (só nomes) | sim | `android-googletv-license`, `android-googlexr-license`, `android-sdk-arm-dbt-license`, `android-sdk-license`, `android-sdk-preview-license`, `google-gdk-license`, `mips-android-sysimage-license` |
| `.temp`, `.knownPackages` | sim | — |

`sdkmanager --list_installed` (saída literal):
```
build-tools;35.0.0 | 35.0.0 | Android SDK Build-Tools 35
build-tools;36.0.0 | 36.0.0 | Android SDK Build-Tools 36
platform-tools     | 37.0.1 | Android SDK Platform-Tools
platforms;android-35 | 2    | Android SDK Platform 35
platforms;android-36 | 2    | Android SDK Platform 36
```
> `android-sdk-license` já aceita nesta instalação (arquivo existe), o que permitiria `sdkmanager "emulator" "system-images;..."` sem re-aceitar — **NÃO foi executado**; só registro.

### 6.2 SDK embutido do Unity — `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK`

```
Ferramenta: Android SDK (módulo "Android SDK & NDK Tools" do Unity)
Versão: cmdline-tools 16.0; platform-tools 36.0.0; build-tools 36.0.0; platforms android-34 (rev 2), android-35 (rev 1), android-36 (rev 2); cmake 3.22.1
Local: ...\PlaybackEngines\AndroidPlayer\SDK
Estado: completo para BUILD; SEM emulator, SEM system-images, SEM licenses\ (o Unity aceita as licenças internamente); pasta tools\ vazia
Usada pelo projeto? SIM — é o SDK que o Unity 6000.3.23f1 usa por padrão (nenhum override no registro).
```

| Componente | Presente? | Versão |
|---|---|---|
| `platforms\` | sim | android-34 (rev 2), android-35 (rev 1), android-36 (rev 2) |
| `build-tools\` | sim | 36.0.0 |
| `platform-tools\` | sim | **36.0.0** (adb 1.0.41, `36.0.0-13206524`) |
| `cmdline-tools\` | sim | 16.0 |
| `cmake\` | sim | 3.22.1 |
| `emulator\` | **não** ⚪ | — |
| `system-images\` | **não** ⚪ | — |
| `ndk\` (dentro do SDK) | não — o NDK fica ao lado, em `AndroidPlayer\NDK` | — |
| `licenses\` | não | — |
| `tools\` | pasta vazia | — |

### 6.3 NDK (só no Unity)

```
Ferramenta: Android NDK
Versão: r27c — 27.2.12479018 (Pkg.ReleaseName = r27c)
Local: C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\NDK
Estado: OK (build, toolchains, prebuilt, simpleperf, shader-tools presentes)
Usada pelo projeto? SIM (IL2CPP ARM64)
```

### 6.4 Gradle

```
Ferramenta: Gradle (embutido no Unity)
Versão: 9.1.0 (gradle-core-api-9.1.0.jar em Tools\gradle\lib)
Local: ...\AndroidPlayer\Tools\gradle  (+ GradleTemplates, bundletool-all-1.17.2.jar, sdktools.jar)
Estado: OK
Usada pelo projeto? SIM
```
- Gradle avulso: **não instalado** (`where.exe gradle` vazio; `GRADLE_HOME` vazio; sem `C:\Gradle`, `Program Files\Gradle`).
- `%USERPROFILE%\.gradle`: 4,4 GB de cache; daemons registrados para **8.14.3** (19/08/2026, resquício do Capacitor) e **9.1.0** (21/09/2026, do Unity). `wrapper\dists` vazio (só `CACHEDIR.TAG`). Registro do Unity: `AndroidGradleStopDaemonsOnExit = 1` (Unity mata os daemons ao sair).

## 7. Java / JDK

Existem **três** JDKs Temurin na máquina.

```
Ferramenta: OpenJDK embutido do Unity
Versão: Temurin 17.0.18+8 (java -version: openjdk 17.0.18 2026-01-20)
Local: C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK
Estado: OK
Usada pelo projeto? SIM — o Unity usa este por padrão; NÃO há override JdkPath* no registro (chave HKCU\Software\Unity Technologies\Unity Editor 5.x tem 38 valores; nenhum JdkPath*/AndroidSdkRoot*/AndroidNdkRoot*/GradlePath*).
```
```
Ferramenta: Eclipse Temurin JDK 21 (instalação de máquina, MSI/winget)
Versão: 21.0.12+8 LTS (javac 21.0.12)
Local: C:\Program Files\Eclipse Adoptium\jdk-21.0.12.8-hotspot\
Estado: OK; é o PRIMEIRO "java" do PATH (PATH de máquina) e JAVA_HOME de MÁQUINA
Usada pelo projeto? NÃO pelo Unity. Serviria ao sdkmanager/avdmanager/emulator do SDK avulso.
```
```
Ferramenta: Eclipse Temurin JDK 21 (cópia por usuário)
Versão: 21.0.12+8 LTS (mesmo build)
Local: C:\Users\VINICIUS\AppData\Local\Java\jdk-21.0.12+8
Estado: OK, duplicado; é o JAVA_HOME de USUÁRIO (sobrepõe o de máquina no processo) e o 2º java do PATH
Usada pelo projeto? NÃO. Redundante com o de Program Files.
```

- `java -version` no PATH → **21.0.12** (Temurin). `where.exe java` retorna os dois JDK 21 (Program Files primeiro, LocalAppData depois).
- `JAVA_HOME`: usuário = `C:\Users\VINICIUS\AppData\Local\Java\jdk-21.0.12+8`; máquina = `C:\Program Files\Eclipse Adoptium\jdk-21.0.12.8-hotspot\`. Processo herda o de usuário.
- `Program Files\Java`, `Program Files\Microsoft`, Zulu, Corretto, BellSoft: ausentes.
- Registro de desinstalação: `Eclipse Temurin JDK with Hotspot 21.0.12+8 (x64)` 21.0.12.8; winget informa atualização disponível 21.0.12.101 (só informativo).

> Nota: os JDKs 21 são plenamente compatíveis com o Android Emulator/`avdmanager`/`sdkmanager` 19.0 (que exigem JDK 17+).

## 8. Android Studio

```
Ferramenta: Android Studio
Versão: —
Local: —
Estado: NÃO INSTALADO (sem C:\Program Files\Android\Android Studio, sem %LOCALAPPDATA%\Google\AndroidStudio*, sem %APPDATA%\Google, sem entrada no registro Uninstall, sem entrada no winget list)
Usada pelo projeto? Não
```
Achado paralelo: `Microsoft.NET.Sdk.Android.Manifest-10.0.100 (x64)` 36.1.2 aparece no registro Uninstall (workload Android do .NET 10 — vem do dotnet 10.0.400; não é SDK Android nem emulador).

## 9. ADB

```
Ferramenta: adb (Android Debug Bridge)
Versão: 1.0.41 — platform-tools 37.0.1 (37.0.1-15733141)
Local: C:\Users\VINICIUS\AppData\Local\Android\Sdk\platform-tools\adb.exe  (o do PATH)
Estado: funciona; servidor subiu na porta 5037 durante a auditoria (PID 17160)
Usada pelo projeto? SIM — scripts do mobile-unity chamam "adb" pelo PATH
```
```
Ferramenta: adb (embutido do Unity)
Versão: 1.0.41 — platform-tools 36.0.0 (36.0.0-13206524)
Local: C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe
Estado: funciona (adb version OK)
Usada pelo projeto? SIM — o Unity Editor usa este ao fazer Build & Run / detectar aparelhos
```

- `adb devices -l` (23/09/2026 11:45): **"List of devices attached" vazio** → o Poco F4 **NÃO está conectado** agora (nem `unauthorized`, nem `offline`). Os `getprop` não foram executados por isso.
- `where.exe adb` retorna **só** o do SDK avulso (o do Unity não está no PATH).
- Só dois `adb.exe` na máquina (busca recursiva em Program Files\Unity, LocalAppData\Android, LocalAppData\Programs, chocolatey).
- **Risco de conflito de versão de servidor adb:** as duas cópias têm protocolo 1.0.41 mas versões diferentes (36.0.0 vs 37.0.1). Quando um cliente adb encontra um servidor de versão diferente na porta 5037, ele **mata e reinicia** o servidor com a própria versão ("adb server version (X) doesn't match this client (Y); killing..."). Na prática: toda vez que o Unity faz Build & Run (adb 36) e depois um script/terminal usa o adb 37 (ou vice-versa), o servidor é derrubado — o aparelho "some" por 1–3 s, `adb logcat`/`scrcpy` abertos caem, e um emulador em execução perde a conexão adb momentaneamente. Isso bate com a memória do projeto ("o Unity reinicia o servidor adb"). Mitigação simples (não aplicada): usar um único adb — pôr o do Unity no PATH antes do avulso, ou atualizar o platform-tools do SDK avulso para a mesma versão do Unity (36.0.0), ou vice-versa. Decisão do coordenador.
- `%USERPROFILE%\.android`: `adbkey`/`adbkey.pub` (20/08/2026), `debug.keystore` (17/08/2026), `analytics.settings`, `cache`. **Arquivos sensíveis (chave adb e keystore de debug): só registrados, não abertos.**

## 10. Emuladores / AVD

- `%USERPROFILE%\.android\avd\` → **pasta não existe** → **Nenhum AVD configurado.**
- `emulator.exe` → **não existe em lugar nenhum** da máquina (busca recursiva em Program Files\Unity, LocalAppData\Android, Program Files\Android, LocalAppData\Programs; `where.exe emulator` vazio). Logo `emulator -list-avds` e `emulator -accel-check` **não puderam ser executados**.
- `system-images\` → ausente nos dois SDKs.
- `ANDROID_AVD_HOME` → não definido.
- Emuladores de terceiros: BlueStacks, LDPlayer, Nox, MEmu, Genymotion, VirtualBox, VMware → **nenhum** (pastas ausentes, nada no registro Uninstall, nada no winget). Windows Subsystem for Android (`Get-AppxPackage *WindowsSubsystemForAndroid*`) → **ausente**. Windows Sandbox → recurso desabilitado.
- Único "virtualizador" presente: **Docker Desktop 4.69.0** (backend WSL2, distro `docker-desktop` parada agora).

## 11. Unity instalado

```
Ferramenta: Unity Editor
Versão: 6000.3.23f1 (revisão 09d2ecc7fb28; FileVersion 6000.3.23.643820) — ÚNICA versão em C:\Program Files\Unity\Hub\Editor\
Local: C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe  (existe; 16/08/2026)
Estado: OK; 17,6 GB, 95.004 arquivos. Bate com mobile-unity\ProjectSettings\ProjectVersion.txt (m_EditorVersion: 6000.3.23f1) e com os scripts build_apk.ps1, foto.ps1 e portao.ps1 ($Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe")
Usada pelo projeto? SIM
```
- **Módulos instalados** (inferidos pelas pastas — `modules.json` NÃO existe na raiz do editor nem `editors-v2.json` em `%APPDATA%\UnityHub`, que só tem `logs` e `languageConfig.json`; o Hub MSIX guarda o estado em outro lugar não inspecionado):
  - `PlaybackEngines\AndroidPlayer` → **Android Build Support** ✔ (variações `mono` e `il2cpp` — Development/Release/Release_Thumb… presentes)
  - `AndroidPlayer\OpenJDK` → **OpenJDK** ✔
  - `AndroidPlayer\SDK` + `AndroidPlayer\NDK` → **Android SDK & NDK Tools** ✔
  - `PlaybackEngines\windowsstandalonesupport` → **Windows Build Support** ✔
  - Nenhum outro PlaybackEngine (sem iOS, WebGL, Linux, Mac).
- Overrides no registro (`HKCU\Software\Unity Technologies\Unity Editor 5.x`): **nenhum** `JdkPath*`, `AndroidSdkRoot*`, `AndroidNdkRoot*`, `GradlePath*` → Unity usa **todos os embutidos**. Único valor Android relevante: `AndroidGradleStopDaemonsOnExit_h2951841473 = 1`.
- Configuração Android do projeto (`mobile-unity\ProjectSettings\ProjectSettings.asset`, só leitura): `AndroidMinSdkVersion: 26`, `AndroidTargetSdkVersion: 35`, **`AndroidTargetArchitectures: 2` = ARM64 somente** (bitmask 1=ARMv7, 2=ARM64, 4=x86, 8=x86_64), `androidSplitApplicationBinary: 0`, `androidUseCustomKeystore: 0` (keystore de debug), `AndroidEnableArmv9SecurityFeatures: 0`.

```
Ferramenta: Unity Hub
Versão: 3.21.3.65535 (MSIX)
Local: C:\Program Files\WindowsApps\UnityTechnologies.UnityHub_3.21.3.65535_x64__2vrhnee42bhxm
Estado: OK (Get-AppxPackage); não está em execução agora
Usada pelo projeto? Sim (gerencia o editor)
```
- Licença: `%LOCALAPPDATA%\Unity\licenses` existe mas está **vazia** na listagem; `%PROGRAMDATA%\Unity` contém só `packages` (cache de pacotes). Há `Unity.Licensing.Client.log` e `Unity.Entitlements.Audit.log` em `%LOCALAPPDATA%\Unity` (não abertos). **Tipo de licença não determinável só pelos nomes de arquivos** — presume-se Personal (licença na nuvem, formato novo), sem confirmação.

## 12. Outras ferramentas

| Ferramenta | Versão | Local | Estado | Usada pelo projeto? |
|---|---|---|---|---|
| scrcpy | — | — | ⚪ **não instalado** (sem PATH, sem Program Files, sem winget/choco) | Não (seria útil para espelhar o Poco F4) |
| Python | 3.14.4 | `%LOCALAPPDATA%\Programs\Python\Python314\python.exe` | OK | Não diretamente |
| Node.js / npm | v24.15.0 / 11.12.1 | `C:\Program Files\nodejs\` | OK | Não (resquício Capacitor) |
| Git | 2.54.0.windows.1 | `C:\Program Files\Git\` | OK | Sim |
| git-lfs | 3.7.1 | `C:\Program Files\Git\cmd\git-lfs.exe` | OK | Sim (arte) |
| winget | v1.29.380 | WindowsApps | OK | — |
| PowerShell | 5.1.19041.6456 | System32 | OK | Sim (scripts .ps1) |
| Vulkan runtime | 1.4.321 (`vulkaninfo.exe` em System32) | System32 | OK | Indireto (GPU host do emulador) |
| Docker Desktop | 4.69.0 (engine 29.4.0) | `C:\Program Files\Docker` | instalado, parado, mas na chave Run | Não — porém é quem mantém VirtualMachinePlatform/WSL2 ligados |
| .NET SDK | 10.0.400 | `C:\Program Files\dotnet` | OK | Não |
| Gradle avulso | — | — | ⚪ não instalado | — |
| CMake / Ninja avulsos | — | — | ⚪ não no PATH (só o CMake 3.22.1 do SDK do Unity) | — |
| VS Build Tools | 2022 17.14.39 e 2026 18.5.1 | Program Files (x86) | OK | Indireto (IL2CPP Windows) |
| GitHub CLI | 2.91.0 | `C:\Program Files\GitHub CLI` | OK | — |
| VS Code / Insiders / Windsurf / Antigravity | 1.137.0 / 1.105.0 / — / — | LocalAppData\Programs | OK | editores |
| Chocolatey | presente (`C:\ProgramData\chocolatey`) | — | só pacotes VS/vcredist | — |
| ffmpeg | 9.0.1 (winget) | LocalAppData\Microsoft\WinGet | OK | Não |

## 13. Processos relevantes rodando agora (23/09/2026 ~11:45)

- **Unity.exe, Unity Hub, VBCSCompiler, UnityShaderCompiler, emulator, qemu, studio64, gradle, scrcpy: NENHUM em execução.**
- `adb.exe` PID 17160 (SDK avulso, 11 MB) — subiu por causa do `adb devices` desta auditoria; não existia antes.
- `wslservice` PID 4960 (7 MB). Nenhum `vmmem`/`vmwp` (nenhuma VM WSL2 ativa).
- `node.exe` ×3 (dois do Claude Code, um do Adobe Creative Cloud).
- Top memória: Lightroom 960 MB, `claude` 399+292+194 MB, Chrome 333+163 MB, MsMpEng 330 MB. 252 processos.

---

## Virtualização — veredito para Android Emulator

| Pergunta | Resposta | Evidência |
|---|---|---|
| VT-x/EPT ligado na BIOS? | **Sim** | `HypervisorPresent = True`; `systeminfo`: "Hipervisor detectado" |
| Hipervisor do Windows ativo no boot? | **Sim** | `HvHost` e `vmcompute` Running; VBS status 2; `VirtualMachinePlatform` habilitado (para WSL2/Docker) |
| Hyper-V completo (`Microsoft-Hyper-V`) habilitado? | **Não** (InstallState 2) | `Win32_OptionalFeature` |
| **WHPX (`HypervisorPlatform`) habilitado?** | **NÃO** 🔴 (InstallState 2) | `Win32_OptionalFeature` |
| VBS (Core Isolation) ligado? | **Sim, rodando** (status 2), mas sem HVCI/Memory Integrity (`SecurityServicesRunning = {0}`) | `Win32_DeviceGuard` |
| Intel HAXM viável? | **Não** — descontinuado, e não instala com hipervisor ativo | — |
| AEHD (Android Emulator Hypervisor Driver) viável? | **Não como está** — AEHD também exige que o hipervisor da Microsoft NÃO esteja ativo; aqui ele está (VirtualMachinePlatform + VBS) | idem |
| CPU | Intel (não AMD) — sem a restrição AMD/WHPX; 10C/16T é bom | `Win32_Processor` |

**Conclusão técnica:** a máquina está no pior ponto intermediário: o hipervisor da Microsoft **está** rodando (por causa de WSL2/Docker Desktop + VBS padrão), o que **bloqueia** HAXM e AEHD, mas o recurso **WHPX está desabilitado**, que é justamente o único caminho de aceleração do emulador nessa condição. Sem aceleração, o `emulator` cai em modo software (`-accel off`) — inutilizável para um jogo 3D URP.

**Caminho mínimo para viabilizar (NÃO executado — exige admin e reinício; decisão do coordenador):** habilitar o recurso `HypervisorPlatform` (`Windows Hypervisor Platform`) em "Ativar ou desativar recursos do Windows" (ou `Enable-WindowsOptionalFeature -Online -FeatureName HypervisorPlatform`, admin) e reiniciar. Nada mais muda: Hyper-V completo NÃO é necessário para WHPX; VBS pode ficar como está (HVCI já está desligado, que era o que mais degrada WHPX). Depois disso, `emulator -accel-check` deveria reportar WHPX.

**Limitações mesmo com WHPX ligado:**
1. **RAM 8 GB, <1 GB livre** — é o gargalo real. Unity Editor + emulador (2 GB guest + overhead) + Chrome/Claude/Lightroom não cabem; a máquina já pagina (commit 20,5/24 GB). Há 1 slot DDR5 livre (máx. 64 GB) — upgrade de hardware é a única solução estrutural. Sem upgrade: fechar Lightroom/Chrome, AVD com 1,5 GB de RAM guest, não rodar Unity e emulador ao mesmo tempo (usar o APK já buildado).
2. **Disco: 34 GB livres** — emulador + imagem + AVD ≈ 10–14 GB. Viável, mas com `Library` (9,9 GB) e `Builds` (5,4 GB) sendo candidatos a limpeza se apertar.
3. **APK é ARM64-only** (`AndroidTargetArchitectures = 2`). Imagens x86_64 do emulador rodam APK ARM64 só via tradução ARM→x86 (Android 11+ com Google APIs), que é lenta e não representativa para medir FPS. Para emulador útil, ou (a) adicionar x86_64 aos alvos numa variante de build de teste (IL2CPP suporta), ou (b) aceitar que o emulador serve só a testes funcionais de UI/fluxo, não de desempenho.
4. **GPU**: OK (RTX 3050 6 GB, Vulkan 1.4, driver 591.74). Cuidado com Optimus: fixar NVIDIA para o processo do emulador se ele cair na Intel UHD. VRAM já 4/6 GB ocupada por outros apps.
5. Docker Desktop na chave `Run`: mesmo com `AutoStart=False` no settings, o executável inicia no login e mantém WSL2 pronto; não consome RAM enquanto a distro está parada (confirmado: sem `vmmem`).
6. **Aparelho físico (Poco F4, Snapdragon 870) é hoje o único ambiente de teste real** e é ARM64 nativo — para medição de FPS ele continua insubstituível; o emulador só agregaria testes rápidos sem cabo.

## Matriz de estado (máquina)

| Item | Estado | Versão | Observação |
|---|---|---|---|
| SO | 🟢 OK | Windows 10 Pro 10.0.19045 x64 | — |
| CPU / virtualização | 🟡 ATENÇÃO | i7-13620H 10C/16T; hipervisor ativo | VT-x ligado; **WHPX desabilitado**; HAXM/AEHD inviáveis com hipervisor ativo |
| RAM | 🔴 PROBLEMA | 8 GB (1×8 DDR5), <1 GB livre | Insuficiente para Unity + emulador; 1 slot livre (máx. 64 GB) |
| GPU | 🟢 OK | RTX 3050 6 GB (591.74) + Intel UHD; Vulkan 1.4 | 4/6 GB VRAM em uso agora; Optimus |
| Disco | 🟡 ATENÇÃO | SSD NVMe 476 GB, **34 GB livres** | Library 9,9 GB e Builds 5,4 GB regeneráveis/limpáveis |
| Android SDK (avulso, `%LOCALAPPDATA%\Android\Sdk`) | 🟡 ATENÇÃO | cmdline-tools 19.0 | Sem emulator/system-images; licenças já aceitas; `ANDROID_HOME` aponta aqui |
| Android SDK (Unity, `AndroidPlayer\SDK`) | 🟢 OK | cmdline-tools 16.0 | Completo para build; sem emulator |
| Platform-tools | 🟡 ATENÇÃO | 37.0.1 (avulso) / 36.0.0 (Unity) | Duas versões → conflito de servidor adb |
| Build-tools | 🟢 OK | 35.0.0 + 36.0.0 (avulso) / 36.0.0 (Unity) | — |
| cmdline-tools | 🟢 OK | 19.0 (avulso) / 16.0 (Unity) | `sdkmanager`/`avdmanager` funcionam no avulso |
| Emulator | ⚪ NÃO EXISTE | — | Nenhum `emulator.exe` na máquina |
| System images | ⚪ NÃO EXISTE | — | — |
| NDK | 🟢 OK | r27c (27.2.12479018) — só no Unity | — |
| CMake | 🟢 OK | 3.22.1 — só no SDK do Unity | Sem CMake avulso |
| JDK (Unity OpenJDK) | 🟢 OK | Temurin 17.0.18+8 | Usado pelo Unity (sem override) |
| JDK (Temurin, Program Files) | 🟢 OK | 21.0.12+8 | JAVA_HOME de máquina; 1º do PATH |
| JDK (Temurin, LocalAppData) | 🟡 ATENÇÃO | 21.0.12+8 | Duplicado; JAVA_HOME de usuário sobrepõe o de máquina |
| Gradle | 🟢 OK | 9.1.0 (Unity) | Sem Gradle avulso; cache `.gradle` 4,4 GB com daemon 8.14.3 antigo |
| Android Studio | ⚪ NÃO EXISTE | — | Não instalado |
| ADB | 🟡 ATENÇÃO | 1.0.41 (36.0.0 e 37.0.1) | Funciona; aparelho **não conectado** agora; servidor ativo PID 17160 |
| AVD | ⚪ NÃO EXISTE | — | `.android\avd` ausente |
| Unity 6000.3.23f1 | 🟢 OK | 6000.3.23f1 (09d2ecc7fb28) | Android+OpenJDK+SDK/NDK+Windows; 17,6 GB |
| Unity Hub | 🟢 OK | 3.21.3.65535 (MSIX) | — |
| scrcpy | ⚪ NÃO EXISTE | — | — |
| Docker Desktop / WSL2 | 🟡 ATENÇÃO | 4.69.0 / WSL 2 | Mantém VirtualMachinePlatform ativo → decide o modelo de aceleração do emulador |

## COMANDOS EXECUTADOS

Todos em Windows PowerShell 5.1, sem elevação, a partir de `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA\.claude\worktrees\auditoria-tecnica-completa-282af1`.

1. **Comando:** `$PSVersionTable.PSVersion; [Environment]::OSVersion.VersionString; Get-CimInstance Win32_OperatingSystem | select Caption,Version,BuildNumber,OSArchitecture,TotalVisibleMemorySize,FreePhysicalMemory`
   **Objetivo:** SO, build, RAM total/livre.
   **Resultado:** PS 5.1.19041.6456; Windows 10 Pro 10.0.19045 64 bits; 8.085.368 KB total, 852.728 KB livres.
2. **Comando:** `Get-CimInstance Win32_Processor | select Name,Manufacturer,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed,VirtualizationFirmwareEnabled,VMMonitorModeExtensions,SecondLevelAddressTranslationExtensions`
   **Objetivo:** CPU e flags de virtualização.
   **Resultado:** i7-13620H, 10C/16T, 2400 MHz; os três flags = False (mascarados pelo hipervisor ativo).
3. **Comando:** `Get-CimInstance Win32_ComputerSystem | select Manufacturer,Model,HypervisorPresent,TotalPhysicalMemory`
   **Objetivo:** modelo e presença de hipervisor.
   **Resultado:** Acer Nitro ANV15-51; HypervisorPresent = True; 8.279.416.832 bytes.
4. **Comando:** `systeminfo | Select-String "Hyper-V","Hyper","Hipervisor","virtualiza"`
   **Objetivo:** seção Requisitos do Hyper-V.
   **Resultado:** "Hipervisor detectado. Recursos necessários para o Hyper-V não serão exibidos."
5. **Comando:** `Get-Service vmcompute,HvHost,WindowsHypervisorPlatform,hns,vmms -ErrorAction SilentlyContinue`
   **Objetivo:** serviços de virtualização.
   **Resultado:** hns/HvHost/vmcompute Running (Manual); WindowsHypervisorPlatform e vmms inexistentes.
6. **Comando:** `Get-ItemProperty "HKLM:\SYSTEM\CurrentControlSet\Control\DeviceGuard"` (+ subchave `Scenarios\HypervisorEnforcedCodeIntegrity`)
   **Objetivo:** política de VBS/HVCI.
   **Resultado:** valores vazios (sem política explícita).
7. **Comando:** `Get-CimInstance -ClassName Win32_DeviceGuard -Namespace root\Microsoft\Windows\DeviceGuard`
   **Objetivo:** estado real do VBS.
   **Resultado:** VirtualizationBasedSecurityStatus = 2 (rodando); SecurityServicesRunning = {0}; SecurityServicesConfigured = {0}.
8. **Comando:** `Get-WindowsOptionalFeature -Online`
   **Objetivo:** recursos Hyper-V/WHPX.
   **Resultado:** ERRO "A operação solicitada requer elevação." — não contornado.
9. **Comando:** `bcdedit /enum {current}` (três variantes: PowerShell direto, `--%`, via `cmd /c`) e `bcdedit` sem argumentos
   **Objetivo:** `hypervisorlaunchtype`.
   **Resultado:** "Parâmetro incorreto" (escape das chaves) e, sem argumentos, "Acesso negado." (exige admin) — não contornado.
10. **Comando:** `Get-CimInstance Win32_OptionalFeature | where Name -match "Hyper|Virtual|Container|HypervisorPlatform|Sandbox|Subsystem" | select Name,InstallState`
    **Objetivo:** mesmo do item 8, sem admin.
    **Resultado:** HypervisorPlatform = 2 (desab.); Microsoft-Hyper-V* = 2; VirtualMachinePlatform = 1 (hab.); Microsoft-Windows-Subsystem-Linux = 1; Containers* = 2.
11. **Comando:** `Get-CimInstance Win32_VideoController | select Name,AdapterRAM,DriverVersion,DriverDate,VideoModeDescription,Status`
    **Objetivo:** GPUs.
    **Resultado:** RTX 3050 6GB Laptop (32.0.15.9174, 29/12/2025) e Intel UHD (32.0.101.7088, 16/06/2026), 1920×1080.
12. **Comando:** `Get-CimInstance Win32_PhysicalMemory | select Manufacturer,Capacity,Speed,ConfiguredClockSpeed,SMBIOSMemoryType,DeviceLocator; Get-CimInstance Win32_PhysicalMemoryArray | select MemoryDevices,MaxCapacityEx`
    **Objetivo:** módulos e slots de RAM.
    **Resultado:** 1×8 GB A-DATA DDR5 5600/5200; 2 slots; máx. 64 GB.
13. **Comando:** `Get-PSDrive -PSProvider FileSystem; Get-CimInstance Win32_LogicalDisk; Get-PhysicalDisk`
    **Objetivo:** unidades, espaço, tipo de disco.
    **Resultado:** só C:, NTFS, 476,3 GB total, 34,0 GB livres; SSD NVMe SM2P41C8-512GC5 Healthy.
14. **Comando:** `[Environment]::GetEnvironmentVariable(<var>,'User'|'Machine'|'Process')` para ANDROID_HOME, ANDROID_SDK_ROOT, ANDROID_NDK_HOME, ANDROID_AVD_HOME, JAVA_HOME, GRADLE_HOME, UNITY_PATH; PATH de usuário e de máquina.
    **Objetivo:** variáveis e PATH.
    **Resultado:** ANDROID_HOME (User) = `%LOCALAPPDATA%\Android\Sdk`; JAVA_HOME User = `%LOCALAPPDATA%\Java\jdk-21.0.12+8`, Machine = `C:\Program Files\Eclipse Adoptium\jdk-21.0.12.8-hotspot\`; demais vazias. PATH contém platform-tools e cmdline-tools do SDK avulso e os dois JDK 21.
15. **Comando:** `cmd /c "where.exe <tool>"` para adb, java, javac, emulator, sdkmanager, avdmanager, scrcpy, python, py, node, npm, git, git-lfs, gradle, vulkaninfo, winget, cmake, ninja, fastboot, Unity, code
    **Objetivo:** localizar binários no PATH.
    **Resultado:** adb/fastboot/sdkmanager/avdmanager no SDK avulso; java/javac nos dois JDK 21; emulator, scrcpy, gradle, cmake, ninja, Unity ausentes do PATH; vulkaninfo em System32.
16. **Comando:** `Get-ChildItem "C:\Program Files\Unity\Hub\Editor"; Get-Item ...\6000.3.23f1\Editor\Unity.exe | select VersionInfo; Test-Path modules.json; Get-AppxPackage *UnityHub*; Get-ChildItem ...\PlaybackEngines e ...\AndroidPlayer`
    **Objetivo:** versões do Unity, módulos, Hub.
    **Resultado:** só 6000.3.23f1; Unity.exe 6000.3.23f1_09d2ecc7fb28; modules.json ausente; Hub 3.21.3.65535 MSIX; PlaybackEngines = AndroidPlayer + windowsstandalonesupport; AndroidPlayer tem NDK, OpenJDK, SDK, Tools, Variations.
17. **Comando:** `Get-Process | where ProcessName -match "^(Unity|UnityHub|adb|emulator|qemu-system|VBCSCompiler|java|studio64|gradle|scrcpy|node|vmmem|vmwp|BlueStacks|LDPlayer|Nox...)"`; top 10 por WorkingSet; `(Get-Process).Count`
    **Objetivo:** processos relevantes.
    **Resultado:** só node ×3; nenhum Unity/adb/emulator; top: Lightroom 960 MB, claude 399 MB; 252 processos.
18. **Comando:** `Get-Item`/`Get-ChildItem` em ~40 caminhos candidatos (LocalAppData\Android\Sdk, C:\Android, Program Files\Android, Android Studio, LocalAppData\Google, Program Files\Java, Eclipse Adoptium, Microsoft, Zulu, Corretto, BellSoft, .android, .android\avd, .gradle, scoop, LocalAppData\Programs, BlueStacks, LDPlayer, Genymotion, Nox, Microvirt, scrcpy, chocolatey\lib, Gradle, Docker, VirtualBox, VMware)
    **Objetivo:** localizar SDKs, JDKs, Studio, emuladores.
    **Resultado:** existem: LocalAppData\Android\Sdk, Eclipse Adoptium\jdk-21.0.12.8-hotspot, .android (sem avd), .gradle, Docker, chocolatey (só VS/vcredist). Ausentes: Android Studio, Program Files\Java, todos os emuladores de terceiros, VirtualBox, VMware, scrcpy, Gradle.
19. **Comando:** `Get-AppxPackage *WindowsSubsystemForAndroid*`
    **Objetivo:** WSA.
    **Resultado:** ausente.
20. **Comando:** `Get-ItemProperty HKLM/HKLM-WOW6432Node/HKCU ...\Uninstall\* | where DisplayName -match "Android|Java|JDK|Studio|BlueStacks|...|Unity|Gradle|Python|Node|Git"`
    **Objetivo:** programas instalados.
    **Resultado:** Temurin JDK 21.0.12.8, Git 2.54.0, GitHub CLI 2.91.0, Node 24.15.0, Python 3.14.4, Unity 6000.3.23f1, VS Build Tools 2022/2026, Microsoft.NET.Sdk.Android.Manifest 36.1.2, Krita, Roblox Studio. Nenhum Android Studio/emulador.
21. **Comando:** listagem de `AndroidPlayer\SDK\{platforms,build-tools,platform-tools,cmdline-tools,emulator,ndk,cmake,system-images,licenses,tools,extras}` com leitura de `Pkg.Revision` de cada `source.properties`; `Get-Content SDK\platform-tools\source.properties`; `& SDK\platform-tools\adb.exe version`; `Get-Content NDK\source.properties`; `Get-Content OpenJDK\release`; `& OpenJDK\bin\java.exe -version`; `Get-ChildItem Tools`, `Tools\gradle\lib -Filter gradle-core-api-*.jar`
    **Objetivo:** inventário do SDK/NDK/JDK/Gradle do Unity.
    **Resultado:** platforms 34(2)/35(1)/36(2); build-tools 36.0.0; platform-tools 36.0.0 (adb 36.0.0-13206524); cmdline-tools 16.0; cmake 3.22.1; sem emulator/system-images/licenses; NDK r27c 27.2.12479018; OpenJDK Temurin 17.0.18+8; Gradle 9.1.0; bundletool 1.17.2.
22. **Comando:** mesma listagem em `%LOCALAPPDATA%\Android\Sdk`; `Get-Content platform-tools\source.properties`, `cmdline-tools\latest\source.properties`; `sdkmanager.bat --list_installed`; `sdkmanager.bat --version` (com JAVA_HOME apontando ao Temurin 21 só no processo)
    **Objetivo:** inventário do SDK avulso.
    **Resultado:** platforms 35(2)/36(2); build-tools 35.0.0+36.0.0; platform-tools 37.0.1; cmdline-tools 19.0; licenses com 7 arquivos (só nomes); sem emulator/system-images/ndk/cmake. `--list_installed` confirmou os 5 pacotes (fez fetch do repositório remoto — leitura).
23. **Comando:** `java -version; javac -version; Get-Content <jdk>\release | Select-String IMPLEMENTOR|JAVA_VERSION|OS_ARCH` para os dois JDK 21; `& %LOCALAPPDATA%\Java\jdk-21.0.12+8\bin\java.exe -version`
    **Objetivo:** versões dos JDKs.
    **Resultado:** ambos Temurin 21.0.12+8 LTS x86_64; javac 21.0.12.
24. **Comando:** `Get-Item "HKCU:\Software\Unity Technologies\Unity Editor 5.x"` → `GetValueNames()` filtrado por `JdkPath|AndroidSdkRoot|AndroidNdkRoot|GradlePath|*UseEmbedded|AndroidJdk|AndroidGradle|AndroidSdk|AndroidNdk` → `GetValue()`
    **Objetivo:** overrides de SDK/JDK/NDK/Gradle do Unity. (A primeira tentativa com `Get-ItemProperty` falhou com InvalidCastException por valor binário; refeita com `GetValue`.)
    **Resultado:** 38 valores na chave; único match: `AndroidGradleStopDaemonsOnExit_h2951841473 = 1`. Nenhum override → embutidos.
25. **Comando:** `Get-ChildItem %PROGRAMDATA%\Unity -Recurse -Depth 1; Get-ChildItem %LOCALAPPDATA%\Unity\licenses -Force; Get-ChildItem %LOCALAPPDATA%\Unity; Get-ChildItem %APPDATA%\UnityHub; Get-ChildItem %APPDATA%\UnityHub -Filter editors*.json`
    **Objetivo:** tipo de licença (só nomes) e módulos via Hub.
    **Resultado:** ProgramData\Unity = `packages`; `licenses` vazia; logs de licensing presentes (não abertos); UnityHub = `logs` + `languageConfig.json`; nenhum `editors*.json`.
26. **Comando:** `Get-ChildItem AndroidPlayer\Variations -Recurse -Depth 1 -Directory; Get-ChildItem SDK\tools -Force; Get-Content SDK\cmake\3.22.1\source.properties`
    **Objetivo:** variações de build e CMake.
    **Resultado:** il2cpp{Common,Development,Managed,Release,Release_Thumb…} e mono{Development,Managed,Release}; tools vazia; CMake 3.22.1.
27. **Comando:** `adb version; adb devices -l; (getprop condicionado a haver aparelho)`
    **Objetivo:** adb e aparelho.
    **Resultado:** adb 1.0.41 / 37.0.1-15733141; "daemon not running; starting now at tcp:5037" → servidor iniciado; lista de aparelhos VAZIA; getprop não executado.
28. **Comando:** `Get-ChildItem <5 raízes> -Recurse -Filter adb.exe`; idem `emulator.exe`; `Test-Path %USERPROFILE%\.android\avd`; `Get-ChildItem %USERPROFILE%\.android -Force`
    **Objetivo:** cópias de adb/emulator e AVDs.
    **Resultado:** 2 adb.exe (Unity 36.0.0, avulso 37.0.1); 0 emulator.exe; `.android\avd` ausente; `.android` tem adbkey, adbkey.pub, debug.keystore (não abertos).
29. **Comando:** `python --version; node --version; npm --version; git --version; git lfs version; winget --version; winget list --accept-source-agreements | Select-String "Android|scrcpy|Java|JDK|Temurin|Studio|Gradle|Vulkan|Docker|Unity|BlueStacks|..."; docker --version; dotnet --version; vulkaninfo --summary; nvidia-smi`
    **Objetivo:** demais ferramentas, Vulkan, driver NVIDIA.
    **Resultado:** Python 3.14.4; Node 24.15.0/npm 11.12.1; Git 2.54.0; git-lfs 3.7.1; winget 1.29.380; Docker 29.4.0 (Desktop 4.69.0); dotnet 10.0.400; Vulkan 1.4.321 (Intel 1.4.323 / NVIDIA 1.4.325); NVIDIA 591.74, 4046/6144 MiB. (`--accept-source-agreements` só suprime o prompt do winget para a fonte de leitura; não aceita licença de software.)
30. **Comando:** `Get-ChildItem -Recurse -Force -File | Measure-Object Length -Sum` por pasta de 1º nível de `ARKANA`, subpastas `mobile-unity\{Library,Builds,Assets,Temp,Logs,obj}`, `.claude\worktrees`, total sem `.git`, `Hub\Editor\6000.3.23f1`, `AndroidPlayer`, SDK avulso, `.gradle` (em background, ~1 min; saída em `%TEMP%\claude\tamanhos_arkana.txt`)
    **Objetivo:** tamanhos.
    **Resultado:** ver tabela da seção 5 (projeto 18,7 GB sem .git; Library 9,9 GB; Builds 5,5 GB; Unity 17,6 GB; SDK avulso 0,66 GB; .gradle 4,4 GB).
31. **Comando:** `cmd /c "wsl --status"; cmd /c "wsl -l -v"`
    **Objetivo:** WSL2.
    **Resultado:** padrão `docker-desktop`, versão 2, estado Stopped (saída em UTF-16 exibida com espaços).
32. **Comando:** `ConvertFrom-Json` de `%APPDATA%\Docker\settings-store.json` lendo só as chaves `WslEngineEnabled/AutoStart/MemoryMiB/Cpus`; `Get-ItemProperty HKCU:\...\CurrentVersion\Run | where Name -match "Docker|Unity|Android"`; `Get-Service com.docker.service,LxssManager,WSLService,vmcompute,HvHost`; `Get-Process vmmem,vmwp,wslservice,"Docker Desktop",com.docker.backend,adb`
    **Objetivo:** por que o hipervisor está ativo; consumo.
    **Resultado:** AutoStart = False; entrada `Docker Desktop` na chave Run; com.docker.service Stopped/Disabled; WSLService Running; só `wslservice` (7 MB) e `adb` (11 MB) rodando; sem vmmem.
33. **Comando:** `Get-CimInstance Win32_OperatingSystem` (2ª leitura) e `Win32_PageFileUsage`
    **Objetivo:** pressão de memória.
    **Resultado:** 7.896 MB total, 486 MB livres, commit 20.573/24.031 MB; pagefile 16.135 MB alocado, uso 3.645 MB, pico 4.440 MB.
34. **Comando:** `Get-ChildItem %USERPROFILE%\.gradle\{wrapper\dists,daemon,android}`
    **Objetivo:** Gradle avulso/caches.
    **Resultado:** dists vazio; daemons 8.14.3 (19/08/2026) e 9.1.0 (21/09/2026); android\FakeDependency.jar.
35. **Comando:** `Get-ChildItem <worktree> -Recurse -Include *.ps1,*.bat,*.cmd,*.sh | Select-String "Unity\.exe|Hub\\Editor|adb|platform-tools|ANDROID_HOME|JAVA_HOME|emulator"` e `Select-String` direto em `mobile-unity\{build_apk,foto,portao}.ps1`
    **Objetivo:** confirmar o caminho do Unity usado pelos scripts.
    **Resultado:** os três `.ps1` definem `$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"`; `mobile-godot\godot\export\build_apk.sh` também referencia adb/SDK (vertente descartada).
36. **Comando:** `Select-String -Path mobile-unity\ProjectSettings\ProjectSettings.asset -Pattern "AndroidTargetSdkVersion|AndroidMinSdkVersion|AndroidTargetArchitectures|..."; Get-Content ProjectSettings\ProjectVersion.txt; Get-ChildItem <worktree> -Force`
    **Objetivo:** alvo Android do projeto e versão do editor.
    **Resultado:** MinSdk 26, TargetSdk 35, TargetArchitectures 2 (ARM64), sem keystore custom; m_EditorVersion 6000.3.23f1 (09d2ecc7fb28); pastas: .claude, arte, auditoria, design, mobile-godot, mobile-unity, roblox.

### O que NÃO foi possível verificar (e por quê)
- `hypervisorlaunchtype` via `bcdedit` — **exige admin** ("Acesso negado"). Concluído indiretamente.
- `Get-WindowsOptionalFeature -Online` — **exige admin**; substituído por `Win32_OptionalFeature` (equivalente).
- `emulator -version`, `emulator -list-avds`, `emulator -accel-check` — **não existe `emulator.exe`** na máquina.
- `adb shell getprop ...` do Poco F4 — **aparelho não conectado** no momento.
- Lista oficial de módulos do Unity (`modules.json`/`editors-v2.json`) — **arquivos não existem** nas localizações conhecidas (Hub MSIX); módulos inferidos pelas pastas de `PlaybackEngines`.
- Tipo de licença do Unity — `%LOCALAPPDATA%\Unity\licenses` vazia; logs de licensing não foram abertos por regra (só nomes).
- Ocupação real da NVIDIA por app e se o emulador cairia na iGPU — só verificável com o emulador rodando.
- Conteúdo de `adbkey`, `debug.keystore`, `licenses\*` — **arquivos sensíveis ou de licença: propositalmente não abertos**.

## FERRAMENTAS USADAS NA AUDITORIA

| Ferramenta | Versão | Finalidade | Localização |
|---|---|---|---|
| Windows PowerShell | 5.1.19041.6456 | todos os comandos de inspeção (CIM/WMI, registro, arquivos) | `C:\Windows\System32\WindowsPowerShell\v1.0\` |
| cmd.exe | 10.0.19045 | `where.exe`, `bcdedit`, `wsl`, `vulkaninfo`, `nvidia-smi`, `sdkmanager.bat` (para capturar stderr sem quebrar o PS) | `C:\Windows\System32\` |
| systeminfo.exe | 10.0.19045 | seção Requisitos do Hyper-V | `C:\Windows\System32\` |
| adb (SDK avulso) | 1.0.41 / 37.0.1 | `adb version`, `adb devices -l` | `%LOCALAPPDATA%\Android\Sdk\platform-tools\adb.exe` |
| adb (Unity) | 1.0.41 / 36.0.0 | `adb version` apenas | `...\AndroidPlayer\SDK\platform-tools\adb.exe` |
| sdkmanager | 19.0 | `--list_installed`, `--version` (leitura; fez fetch remoto) | `%LOCALAPPDATA%\Android\Sdk\cmdline-tools\latest\bin\sdkmanager.bat` |
| java (Temurin) | 21.0.12 / 17.0.18 | `-version` | Program Files\Eclipse Adoptium; LocalAppData\Java; AndroidPlayer\OpenJDK |
| winget | 1.29.380 | `winget list` (leitura) | WindowsApps |
| vulkaninfo | Vulkan 1.4.321 | `--summary` | `C:\Windows\System32\vulkaninfo.exe` |
| nvidia-smi | 591.74 | estado da GPU/driver | PATH do driver NVIDIA |
| wsl.exe | WSL 2 | `--status`, `-l -v` | `C:\Windows\System32\` |
| git / node / python / docker / dotnet | 2.54.0 / 24.15.0 / 3.14.4 / 29.4.0 / 10.0.400 | `--version` apenas | ver seção 12 |
| Arquivo temporário | — | `tamanhos_arkana.txt` (saída da medição de pastas) | `C:\Users\VINICIUS\AppData\Local\Temp\claude\` |

**Nenhum comando de escrita, instalação, aceitação de licença, criação de AVD, `adb install`/`adb shell` de alteração, edição de PATH/registro ou build foi executado.**


---

# ANEXO B — PROJETO UNITY / ANDROID (relatório da raia enterprise-architect)


**Data:** 2026-09-23
**Escopo:** `mobile-unity/` do ARKANA (worktree `auditoria-tecnica-completa-282af1`) + logs não versionados do clone principal (`mobile-unity/Builds/`, `mobile-unity/Logs/`).
**Aviso:** auditoria **somente leitura**. Nenhum Unity foi aberto, nenhum script `.ps1` rodou, nenhum build foi executado, nenhum arquivo do projeto foi alterado. Tudo abaixo vem de `cat`/`grep`/`find`/`ls` e da leitura de logs já existentes. Campos de senha não foram lidos.

Convenção de caminhos: `U/` = `mobile-unity/` do worktree; `M/` = `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA\mobile-unity\` (clone principal, onde vivem `Builds/`, `Logs/`, `Library/`).

---

## 1. Engine

| Item | Valor | Evidência |
|---|---|---|
| Versão do editor | **6000.3.23f1** | `U/ProjectSettings/ProjectVersion.txt:1` `m_EditorVersion: 6000.3.23f1` |
| Changeset | **09d2ecc7fb28** | `ProjectVersion.txt:2` `m_EditorVersionWithRevision: 6000.3.23f1 (09d2ecc7fb28)` |
| Como foi obtida | Instalação pelo Unity Hub em `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe` (caminho cravado nos 3 `.ps1`); módulo Android com SDK/NDK/JDK/Gradle **embutidos** (ver §4) | `portao.ps1:5`, `foto.ps1:11`, `build_apk.ps1:6`; `M/Builds/build.log:566-569` |
| Build do editor que gerou o último APK | `Built from '6000.3/staging' branch; Version is '6000.3.23f1 (09d2ecc7fb28) revision 643820'` | `M/Builds/build.log:2` |
| Licença | Personal, resolvida pelo cliente de licença do Hub (exit 198 sem o Hub) | `portao.ps1:8-16`; `build.log:5,17` (`LicenseClient-VINICIUS`) |
| Versão desde quando | O `ProjectVersion.txt` tem um único commit (`d327e75 feat(unity): esqueleto do mobile-unity`), nunca mudou | `git log -- mobile-unity/ProjectSettings/ProjectVersion.txt` |

**Indícios conflitantes:**
- Nenhum documento cita outra versão `6000.x` (grep por `6000\.[0-9]+\.[0-9]+f[0-9]+` excluindo `6000.3.23f1` → 0 ocorrências em `.md/.ps1/.txt/.cs`).
- `Packages/manifest.json` pede `com.unity.test-framework: 1.5.1`, mas `packages-lock.json` resolve **1.6.0 (builtin)**. O contexto que recebi dizia 1.5.1; o que roda é 1.6.0. Não é erro (pacote builtin do 6000.3), só divergência entre pedido e resolvido.
- `design/pipeline/ANDROID.md` está **desatualizado**: cita `-executeMethod Build.Android` (o real é `Arkana.EditorTools.Build.Android`), `Assets/Editor/Build.cs` (o real é `Assets/_Arkana/Editor/Build.cs`) e `-logFile mobile-unity/Logs/build.log` (o real é `Builds/build.log`). Quem seguir esse doc ao pé da letra falha.
- `design/infra/servidor-03-pipeline-build.md` e `servidor-06-ferramentas-locais.md` são **inteiramente da era Godot** (Godot 4.4.1, `godot/export/build_apk.sh`, JDK 21 em `%LOCALAPPDATA%`, Android SDK em `%LOCALAPPDATA%/Android/Sdk`). Nada disso vale para o Unity: o Unity usa SDK/NDK/JDK próprios. São **intenção histórica**, não estado.

## 2. Linguagens e stack

| Camada | O que é | Evidência |
|---|---|---|
| C# | **C# 9** declarado no contrato; 189 arquivos `.cs` (122 em `Scripts/`, 3 em `Editor/`, 59 em `Tests/EditMode/`, 5 em `Tests/PlayMode/`). `apiCompatibilityLevel: 6` (= .NET Standard 2.1). Sem `.csproj` versionado (regenerável). | `U/ARQUITETURA.md:6`; `find Assets -name '*.cs' | wc -l`; `ProjectSettings.asset:781` |
| Shaders | 7 shaders HLSL escritos à mão (não ShaderGraph), todos em `Resources/`: `ArkanaAgua`, `ArkanaCeu`, `ArkanaGrama`, `ArkanaMago`, `ArkanaNevoa`, `ArkanaSintoniaZona`, `ArkanaToon` (44 KB). 0 `.shadergraph`, 0 `.hlsl` separado, 0 `.compute`. | `find Assets -name '*.shader'` |
| PowerShell | 4 scripts na raiz de `mobile-unity/`: `portao.ps1`, `foto.ps1`, `build_apk.ps1`, `importar_mago.ps1` | `ls U/` |
| Outros | `arte/tools/{blender,meshy}` existe (ferramentas de arte; fora do escopo, só citado) | `ls arte/tools` |
| Pipeline de build | Unity `BuildPipeline.BuildPlayer` → Gradle **embutido** do Unity (9.1.0 / AGP 9.0.0) → IL2CPP. **Não há** `Assets/Plugins/Android/` — nem `mainTemplate.gradle`, `AndroidManifest.xml`, `gradleTemplate.properties`, `settingsTemplate.gradle`, `launcherTemplate.gradle`. O projeto usa os **templates padrão do Unity**. | `ls Assets/Plugins` → não existe; `find Assets -iname 'AndroidManifest.xml' -o -iname '*.gradle'` → vazio |
| Plugins nativos | Nenhum `.aar`, `.jar`, `.so` em `Assets/` | idem |

**Assemblies (`*.asmdef`):**

| Nome | Root namespace | Referências | Plataformas | Obs. |
|---|---|---|---|---|
| `Arkana` (`Assets/_Arkana/Scripts/Arkana.asmdef`) | `Arkana` | `Unity.InputSystem`, `Unity.RenderPipelines.Universal.Runtime`, `Unity.RenderPipelines.Core.Runtime` | todas | runtime único; `allowUnsafeCode: false` |
| `Arkana.Editor` (`Assets/_Arkana/Editor/`) | `Arkana.EditorTools` | `Arkana`, `Unity.RenderPipelines.Universal.Runtime` | **Editor** apenas | `Build`, `MainSceneBuilder`, `ImportacaoArkana` |
| `Arkana.Tests` (`Tests/EditMode/`) | `Arkana.Tests` | `Arkana`, `UnityEngine.TestRunner`, `UnityEditor.TestRunner`, `nunit.framework.dll` | **Editor** apenas | `defineConstraints: UNITY_INCLUDE_TESTS` |
| `Arkana.PlayTests` (`Tests/PlayMode/`) | `Arkana.Tests` | `Arkana`, `Unity.RenderPipelines.Core.Runtime`, `UnityEngine.UI`, TestRunner ×2, `nunit.framework.dll` | todas (`includePlatforms: []`) | `UNITY_INCLUDE_TESTS` — entra só em builds com testes; o APK normal não o leva |

## 3. Configuração Android

### 3.1 `ProjectSettings/ProjectSettings.asset` (valores lidos)

| Campo | Valor | Tradução / Obs. | Linha |
|---|---|---|---|
| `productName` | `Arkana` | | 16 |
| `companyName` | `V-STACK` | | 15 |
| `applicationIdentifier.Android` | **`br.com.vstack.arkana`** | `overrideDefaultApplicationIdentifier: 1` | 172-173, 179 |
| `bundleVersion` | `1.0` | versionName | 150 |
| `AndroidBundleVersionCode` | **1** | versionCode nunca incrementado (8 APKs gerados, todos versionCode 1) | 180 |
| `AndroidMinSdkVersion` | **26** (Android 8.0) | | 181 |
| `AndroidTargetSdkVersion` | **35** (Android 15) | explícito, não "auto"(0) | 182 |
| `AndroidTargetArchitectures` | **2 = ARM64 apenas** | bitmask: 1=ARMv7, 2=ARM64, 4=x86, 8=x86_64. **Não há x86_64** → emulador x86_64 padrão não instala este APK (ver §11) | 272 |
| `scriptingBackend.Android` | **1 = IL2CPP** | | 689-690 |
| `il2cppCompilerConfiguration` | `{}` (vazio → padrão **Release**) | | 691 |
| `il2cppCodeGeneration` | `{}` (padrão: Faster runtime) | | 692 |
| `il2cppStacktraceInformation` | `{}` (padrão) | | 693 |
| `managedStrippingLevel` | `{}` (padrão para IL2CPP no 6000.x: **Minimal**) | `stripEngineCode: 1` | 694, 186 |
| `apiCompatibilityLevel` | 6 (.NET Standard 2.1) | | 781 |
| `androidSplashScreen` | `{fileID: 0}` (nenhum próprio); `m_ShowUnitySplashScreen: 1` (splash do Unity ligado — Personal) | | 275, 20 |
| `defaultScreenOrientation` | 4 = AutoRotation | | 11 |
| `allowedAutorotateToPortrait / UpsideDown` | 0 / 0 | | 62-63 |
| `allowedAutorotateToLandscapeLeft / Right` | 1 / 1 | **paisagem obrigatória** | 64-65 |
| `androidAutoRotationBehavior` | 1 | | 84 |
| `m_BuildTargetGraphicsAPIs` | `[]` (**vazio = Auto Graphics API**) | No Unity 6 Android auto = **Vulkan primeiro, OpenGLES3 como fallback**. Nada foi fixado. | 402 |
| `m_BuildTargetGraphicsJobs` / `JobMode` | `[]` (padrão) | | 400-401 |
| `m_MTRendering` | 1 (multithreaded rendering ligado) | | 54 |
| `vulkanNumSwapchainBuffers` | 3 | `vulkanEnableSetSRGBWrite: 0`, `PreTransform: 0`, `LateAcquire: 0`, `CommandBufferRecycling: 1` | 142-146 |
| `m_ActiveColorSpace` | **1 = Linear** | forçado também por `Build.cs:39` | 50 |
| `androidStartInFullscreen` | 1 | | 73 |
| `androidRenderOutsideSafeArea` | 1 | | 74 |
| `androidUseSwappy` | **1 = Frame Pacing ligado** | | 75 |
| `androidBlitType` | 0 (Always) | | 77 |
| `androidFullscreenMode` | 1 | | 83 |
| `androidApplicationEntry` | **2 = GameActivity** (bitmask: 1=Activity, 2=GameActivity) | confirma a atividade `com.unity3d.player.UnityPlayerGameActivity` que o `adb shell am start` usa | 86 |
| `androidResizeableActivity` | 1 | | 78 |
| `androidSupportedAspectRatio` | 1 | `androidMaxAspectRatio: 2.4`, `androidMinAspectRatio: 1` | 169-171 |
| `AndroidTVCompatibility` | 0 | | 281 |
| `androidGamepadSupportLevel` | 0 | | 294 |
| `AndroidEnableTango` | 0 | | 286 |
| `AndroidFilterTouchesWhenObscured` | 0 | | 9 |
| `AndroidEnableArmv9SecurityFeatures` | 0 | | 278 |
| `androidSplitApplicationBinary` | 0 (sem OBB/asset packs; `useAPKExpansionFiles` não existe mais nesse formato) | | 192 |
| `AndroidMinifyRelease / Debug` | 0 / 0 (sem R8/ProGuard) | | 295-296 |
| `AndroidReportGooglePlayAppDependencies` | 1 | | 299 |
| `AndroidValidateAppBundleSize` | 1 (limite 200 MB) — irrelevante, não gera AAB | | 297-298 |
| `AndroidExportGradleProject` / `AndroidBuildAppBundle` | **não estão no `.asset`** (esses são `EditorUserBuildSettings`, não `PlayerSettings`); o `Build.cs` não os toca → APK direto, sem export de projeto Gradle | — |
| `AndroidTargetDevices` | não presente (padrão: todos) | — |
| `androidEnableSustainedPerformanceMode` | não presente (padrão: desligado) | — |
| `strictShaderVariantMatching` | 0 | | 195 |
| `StripUnusedMeshComponents` | 0 | | 194 |
| `gcIncremental` | 1 | | 701 |
| `enableFrameTimingStats` | 0 | `FrameTimingManager` desligado | 160 |
| `useHDRDisplay` | 0 | | 163 |
| `ForceInternetPermission` / `ForceSDCardPermission` | 0 / 0 | | 187-188 |
| `insecureHttpOption` | 0 | | 795 |
| `runInBackground` | 0 | | 89 |
| `activeInputHandler` | **2 = ambos (Input System + legado)** | gera o aviso do build (§4) | 783 |
| `submitAnalytics` | 1 | apenas telemetria do editor | 96 |
| `enableCrashReportAPI` | 0 | | 427 |
| `cloudProjectId` / `cloudEnabled` | vazio / 0 | Unity Cloud não ligado | 785, 790 |
| `m_StackTraceTypes` | `01..` ×6 | ScriptOnly para todos os tipos de log | 58 |

**Keystore / assinatura:**

| Campo | Estado |
|---|---|
| `androidUseCustomKeystore` | **0** (linha 289) |
| `AndroidKeystoreName` | **vazio** (linha 276) |
| `AndroidKeyaliasName` | **vazio** (linha 277) |
| `AndroidKeystorePass` / `AndroidKeyaliasPass` | **campos não existem** neste `.asset` (grep `Keystore\|Keyalias` retorna só as 3 linhas acima). Nada de senha versionada. |

Conclusão: o APK sai assinado pela **keystore de debug do Unity/Gradle** (`launcher-release.apk` sem keystore custom). Serve para `adb install`; não serve para Play Store (o próprio `design/pipeline/ANDROID.md` lista isso como pendência G6).

### 3.2 O que `Assets/_Arkana/Editor/Build.cs` força em tempo de build

`Build.Android()` (`Build.cs:95-122`) chama `ApplySettings()` (`:21-56`) e `MainSceneBuilder.Build()` antes de `BuildPipeline.BuildPlayer`:

| O que força | Linha |
|---|---|
| `GraphicsSettings.defaultRenderPipeline = URP_Base.asset`; para **cada** nível de qualidade: `QualitySettings.renderPipeline = urp` e `vSyncCount = 0` | 24-36 |
| `colorSpace = Linear` | 39 |
| `companyName = "V-STACK"`, `productName = "Arkana"` | 40-41 |
| `SetApplicationIdentifier(Android, "br.com.vstack.arkana")` | 42 |
| `SetScriptingBackend(Android, IL2CPP)` | 43 |
| `Android.targetArchitectures = AndroidArchitecture.ARM64` (**só ARM64**) | 44 |
| `minSdkVersion = AndroidApiLevel26`, `targetSdkVersion = AndroidApiLevel35` | 45-46 |
| `startInFullscreen = true`; orientação AutoRotation só paisagem | 47-52 |
| `IncluirShadersDoCodigo()` — insere 6 shaders URP em `GraphicsSettings.m_AlwaysIncludedShaders` (Lit, Simple Lit, Unlit, Particles ×3), porque material criado em runtime + `Shader.Find` não entra no build sozinho | 53, 64-93 |
| `BuildPlayerOptions { scenes = [Main.unity], locationPathName = <raiz>/Builds/arkana.apk, target = Android, options = **BuildOptions.None** }` | 105-111 |
| Em falha: imprime cada `BuildStepMessage` de erro e `EditorApplication.Exit(1)` em batchmode | 115-121 |

**Consequências (fatos, não opinião):**
- `BuildOptions.None` → **não** é Development Build, **sem** Script Debugging, **sem** Autoconnect Profiler, **sem** Deep Profiling. É um build de **release** (Gradle produz `launcher-release.apk`, `build.log:6852`) com IL2CPP Release.
- `Build.cs` **não** toca `EditorUserBuildSettings.buildAppBundle`, `androidBuildSubtarget`, `exportAsGoogleAndroidProject`, `AndroidCreateSymbols` → tudo no padrão (APK, sem símbolos).
- Como `ApplySettings()` regrava `ProjectSettings.asset` a cada build e faz `AssetDatabase.SaveAssets()`, qualquer ajuste manual no Player Settings que colida com essas linhas é **sobrescrito** no próximo `build_apk.ps1`. Para mudar ABI/minSdk/targetSdk é preciso mudar o `Build.cs`, não o inspector.

### 3.3 Permissões

- Não há `AndroidManifest.xml` em `Assets/`. O manifesto é o **gerado pelo Unity**.
- `grep` por `Permission.RequestUserPermission`, `INTERNET`, `EXTERNAL_STORAGE`, `RequestUserPermission` em `.cs` → **0 ocorrências**.
- `ForceInternetPermission: 0`, `ForceSDCardPermission: 0`.
- O código não usa `UnityWebRequest` (0 ocorrências, §8), então o Unity **não** adiciona `INTERNET` por análise de código; o módulo `com.unity.modules.unitywebrequest` está no manifest como dependência transitiva do gltfast, o que **pode** fazer o Unity incluir `android.permission.INTERNET` no manifesto gerado — **não verificado** (exigiria abrir o APK; ver "não verificado" no fim).
- `PlayerPrefs` (Config, Grimório, modo Dupla, mago escolhido) → armazenamento interno do app, sem permissão.

### 3.4 Renderização (URP) e níveis de qualidade

**`Assets/_Arkana/Settings/URP_Base.asset`** (o único asset URP; `QualitySettings.customRenderPipeline` aponta para ele em todos os 6 níveis, guid `4a441f00d284ff64bbc4c885105fd6f8`):

| Campo | Valor | Linha |
|---|---|---|
| `m_RendererDataList` → `URP_Base_Renderer` (default index 0) | | 19-21 |
| `m_RequireDepthTexture` / `m_RequireOpaqueTexture` | 0 / 0 | 22-23 |
| `m_SupportsHDR` | **1** (HDR ligado; precisão 0 = 32 bits) | 26-27 |
| `m_MSAA` | 1 (= desligado) | 28 |
| `m_RenderScale` | **1.0** (o asset é o perfil "Alta") | 29 |
| `m_MainLightShadowmapResolution` | **4096** | 46 |
| `m_ShadowCascadeCount` | **4** | 58 |
| `m_ShadowDistance` | 50 | 57 |
| `m_SoftShadowsSupported` | 1; `m_SoftShadowQuality` **1 = Low** (o corte de 21/09) | 66, 69 |
| `m_AdditionalLightsRenderingMode` | 1 (per-pixel), limite 4, sem sombra de luz adicional | 47-49 |
| `m_UseSRPBatcher` | 1; `m_SupportsDynamicBatching` 0 | 72-73 |
| `m_GPUResidentDrawerMode` | 0 | 86 |
| `m_ShadowDepthBias` / `NormalBias` | 1 / 1 | 63-64 |

**`URP_Base_Renderer.asset`:** Forward (`m_RenderingMode: 0`), `m_DepthPrimingMode: 0`, `m_CopyDepthMode: 1`, `m_IntermediateTextureMode: 1`, `m_UseNativeRenderPass: 0`. Renderer feature **`ScreenSpaceAmbientOcclusion` presente mas `m_Active: 0`** (linhas 65-77: `Downsample: 1`, `Intensity: 1.2`). Ou seja, o SSAO está desligado no asset, mas continua serializado — a `BancadaDeCortes` o encontra por reflexão e pode religar (`BancadaDeCortes.cs:104-113, 119`).

**`Assets/UniversalRenderPipelineGlobalSettings.asset`:** `m_StripUnusedVariants: 1`, `m_StripDebugVariants: 1`, `m_StripScreenCoordOverrideVariants: 1`, `m_StripUnusedPostProcessingVariants: 0`, `m_EnableRenderCompatibilityMode: 0` (Render Graph ligado). `ProjectSettings/GraphicsSettings.asset:46` aponta `m_CustomRenderPipeline` para o mesmo `URP_Base`.

**`ProjectSettings/QualitySettings.asset`:** 6 níveis (`Very Low`, `Low`, `Medium`, `High`, `Very High`, `Ultra`), `m_PerPlatformDefaultQuality.Android: 2` (**Medium**), todos com `vSyncCount: 0` e o mesmo `customRenderPipeline`. Os campos built-in (shadows, antiAliasing, lodBias, skinWeights…) variam entre os níveis, mas no URP o que manda é o asset — e é **um só**. Por isso "Qualidade" não fazia nada até 21/09.

**Como "Qualidade" muda de verdade hoje** (`Assets/_Arkana/Scripts/Menu/Config.cs`):
- `K_QUALIDADE` = 0 Baixa / 1 Média / 2 Alta; padrão **1** (`Config.cs:54, 90`).
- `Perfil(q)` (`:81-84`): Baixa = renderScale **0.70**, 2 cascatas, sombra 2048; Média = **0.85**, 2 cascatas, 2048; Alta = **1.0**, 4 cascatas, 4096.
- `Aplicar()` (`:176-195`): `Application.targetFrameRate = {30,60,120,-1}[K_FPS_LIMITE]` (padrão índice 1 = **60**), `QualitySettings.vSyncCount = K_VSYNC ? 1 : 0` (padrão **1**), `SetQualityLevel(q * (n-1) / 2)` (mapeia para Very Low / Medium / Ultra dos 6 built-in), e **só fora do editor** (`!Application.isEditor`) escreve `renderScale`, `shadowCascadeCount`, `mainLightShadowmapResolution` no `UniversalRenderPipeline.asset` em runtime. No editor o asset é o arquivo do projeto e não é tocado — as fotos saem na "Alta".
- Nota: `Build.cs:33` zera `vSyncCount` em todos os níveis, mas `Config.Aplicar` religa (`vsync = 1`) no boot, porque o padrão de `K_VSYNC` é `true`. O que vale no aparelho é o da Config.

## 4. Pipeline de build — como o APK é gerado hoje

```
Comando:            powershell -File mobile-unity\build_apk.ps1
                    (internamente: "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
                       -batchmode -nographics -projectPath "<mobile-unity>" -buildTarget Android
                       -executeMethod Arkana.EditorTools.Build.Android -logFile "<mobile-unity>\Builds\build.log" -quit)
Pré-condições:      Unity Hub aberto (o script abre via shell:AppsFolder\UnityTechnologies.UnityHub_..., espera 25 s);
                    nenhum outro Unity no mesmo projeto (trava de instância); módulo Android instalado.
Resultado esperado: linha "BuildSummary: result=Succeeded ..." no build.log; "Exiting batchmode successfully now!";
                    o script imprime as 25 últimas linhas do log e "APK: <caminho> (<MB> MB)". Exit code = do Unity.
Local do APK:       1) <projeto>\Builds\arkana.apk (sempre sobrescrito)
                    2) cópia datada em <raiz do clone PRINCIPAL>\mobile-unity\Builds\testes\arkana-yyyy-MM-dd_HHmm.apk
                       (o script resolve `git rev-parse --git-common-dir` para achar a raiz principal — funciona de worktree)
Duração conhecida:  último build (21/09 14:46 local): BuildPlayer 00:01:36 (build.log:8730), Gradle 27,7 s (build.log:6843);
                    processo inteiro ~1-2 min (log aberto 17:46:06Z, APK gravado 14:47 local). PROJETO.md:296 registra
                    "5 min 20 s a primeira vez, 1 min 33 s incremental".
```

**O que o `build.log` mais recente (`M/Builds/build.log`, 605 941 bytes, 21/09 14:47) diz:**

| Item | Valor | Linha |
|---|---|---|
| Editor | 6000.3.23f1 (09d2ecc7fb28), Release, Windows 10 19045, 7 895 MB RAM | 2-3 |
| JDK | `…\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK` (o do Unity) | 566 |
| Android SDK | `…\AndroidPlayer\SDK` (o do Unity) | 567 |
| Android NDK | `…\AndroidPlayer\NDK` (o do Unity) | 568 |
| Gradle | `…\AndroidPlayer\Tools\gradle` — **Gradle 9.1.0, Android Gradle Plugin 9.0.0** | 569 |
| `Arkana.Build: settings aplicados (URP, Android IL2CPP/ARM64, minSdk 26, landscape).` | confirma o `Build.cs` rodou | 451 |
| ABI | pasta `Arkana_BurstDebugInformation_DoNotShip/tempburstlibs/arm64-v8a/` → só **arm64-v8a**; nenhuma linha `x86_64`/`armeabi` | `ls M/Builds/…` |
| Variante Gradle | `launcher\build\outputs\apk\release\launcher-release.apk` → **release** | 6852 |
| Build Report | Texturas 270,0 MB (72,3 %), Meshes 85,5 MB, Animações 12,3 MB, Shaders 4,5 MB, Sons **0 KB**; Total User Assets 373,3 MB; "Complete build size 1.2 gb" (descomprimido) | 6863-6874 |
| Maior asset | `Resources/castelo.glb` 65,6 MB descomprimido | 6875 |
| Resultado | `Build Finished, Result: Success.` / `BuildSummary: result=Succeeded time=00:01:36.1398115 errors=0 warnings=1 output=…/Builds/arkana.apk` | 8717, 8730 |
| Único warning | `CheckPrerequisites:ValidateInputHandling` — "select a single active input handling method" (por `activeInputHandler: 2`) | 574-581 |
| Erros | 0 (`grep -ci "^error|error CS|Exception"` = 3, todos nomes de arquivo `*Stripping*.cs` do Build Report, não erros) | — |
| buildToolsVersion / compileSdk efetivos | **não aparecem** no `build.log` (o Unity 6 não ecoa o `build.gradle`). Só dá para afirmar minSdk 26 / targetSdk 35 pelo `ProjectSettings` + linha 451. | — |
| Tamanho do APK | 239 004 494 bytes (**228 MB**) | `ls -la M/Builds/arkana.apk` |

**Histórico de APKs em `M/Builds/testes/`** (não versionados; `.gitignore:16 *.apk`): 16/09 10:23 (221 MB), 16/09 14:45, 19/09 17:25, 21/09 13:46, 14:18, 14:29, 14:41, 14:47 — 8 APKs, todos `versionCode 1`.

**CI/CD:** não existe. Raiz do worktree não tem `.github/`, `.gitlab-ci.yml`, `azure-pipelines.yml`, `Jenkinsfile`, `.circleci/`, `bitrise.yml` (`ls` confirmou cada um). `servidor-03-pipeline-build.md:40-44` diz que GitHub Actions "passa a valer em G5" — intenção.

## 5. Deploy atual (Código → Build → APK → aparelho)

**Não existe script de deploy.** `grep -rnE "\badb\b" --include=*.ps1 --include=*.cs` em todo o repositório retorna apenas **comentários** em 3 `.cs` (`Main.cs:266`, `MedidorDeFps.cs:7`, `PartidaPeloAdb.cs:6,8`). Nenhum `.ps1` chama `adb`. Nenhum `adb install`, `adb push`, `adb devices` em código ou script.

Onde `adb` aparece em `.md` (todos como instrução manual ou registro):
- `design/PROJETO.md:363-365` — o bloco de automação: `adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity --es arkana_auto partida --ei arkana_fps 300`, `adb logcat -s Unity | grep "ARKANA FPS"`, `adb exec-out screencap -p > foto.png`.
- `design/PROJETO.md:357-360` — MIUI do Poco F4 recusa `INJECT_EVENTS`, `install -g` e instalação sem "Instalar via USB"; `:70` — com "Depuração USB (segurança)" ligada, `adb shell input` funciona.
- `design/PROJETO.md:44, 213, 872, 1502`; `design/cenario/MAPA-GRANDE-PLANO.md:40, 541`; `design/gdd/PASSOS_GRAFICOS.md:382`; `design/pipeline/MESHY.md:225`; `mobile-godot/00-LEIA.md:45` — todos "nenhum aparelho apareceu em `adb devices`" (bloqueio histórico, resolvido em 21/09: "O aparelho voltou ao `adb` (21/09) e MEDIU", `PROJETO.md:76`).
- `design/infra/servidor-06-ferramentas-locais.md:18` — "Android SDK ausente → bloqueia `adb`" (era Godot; hoje o SDK do Unity traz `platform-tools`, mas **não verifiquei** se `adb` está no PATH do Windows — ver fim).

**Processo manual que os docs descrevem:**
1. `build_apk.ps1` → APK datado em `mobile-unity/Builds/testes/`.
2. "o Diretor sobe ele mesmo no Drive ('JOGOS EM DESENVOLVIMENTO')" (`PROJETO.md:129`) — ou cabo USB + `adb install` (implícito em `:357-360`; o comando exato de instalação **não está escrito** em lugar nenhum).
3. No Poco F4: "Instalar via USB" ligado; toque por `adb shell input` só com "Depuração USB (segurança)".
4. Execução: pelo ícone, ou `adb shell am start … --es arkana_auto partida` para partida automática.
5. Leitura: `adb logcat -s Unity` (FPS e bancada), `adb exec-out screencap`.

## 6. Debug atual

| Mecanismo | Existe? | Evidência |
|---|---|---|
| Prefixo `"ARKANA` em `Debug.Log` | Sim, mas **só em 4 arquivos de runtime**: `BancadaDeCortes.cs` (5), `MedidorDeFps.cs` (2), `Menu/Logo.cs` (2), `Core/Textos.cs` (1). Não é um logger central; o resto do código não loga com prefixo. | `grep -rc '"ARKANA' --include=*.cs` |
| `Application.logMessageReceived` | **Não** (0 ocorrências) | grep |
| `Debug.developerConsoleVisible` | **Não** | grep |
| Crash reporting (Crashlytics, Sentry, Backtrace, CloudDiagnostics, `CrashReport`, `UnhandledException`) | **Não** (0 ocorrências); `enableCrashReportAPI: 0`; `cloudEnabled: 0` | grep; `ProjectSettings.asset:427, 790` |
| HUD de debug / contador na tela | `K_CONTADOR_FPS` existe na Config (padrão `false`, `Config.cs:90`) — **não verifiquei** onde a HUD o desenha | `Config.cs:90` |
| Development Build / Script Debugging | **Não** (`BuildOptions.None`, `Build.cs:110`) → sem Managed Debugger, sem Profiler autoconnect, IL2CPP Release | `Build.cs:110` |
| Stack traces | `m_StackTraceTypes` = ScriptOnly em todos os tipos; `il2cppStacktraceInformation` padrão (método + arquivo/linha) | `ProjectSettings.asset:58, 693` |
| `diag.txt` | Gerado **só pelo `foto.ps1`** (PlayMode com GPU) em `M/Logs/fotos/diag.txt` — diz o que a câmera e o corpo tocam. Não existe no aparelho. | `M/Logs/fotos/diag.txt` (21/09 14:53) |
| `Logs/` do clone principal | `portao.log`, `portao-resultados.xml`, `portao-playmode.log`, `portao-playmode-resultados.xml`, `foto.log`, `foto-resultados.xml`, `fotos/*.png` (8), `diag-mago-NN.txt` (20), `diag-luva.txt`, `carregamento-{partida,treino}.txt`, logs antigos de compilação (`compila-*.log`, 09/09-11/09) | `ls M/Logs` |
| No aparelho | Só `adb logcat -s Unity` (comentários em `MedidorDeFps.cs:7`, `PROJETO.md:364`) | — |

**Automação por intent (o ouro para emulador)** — classe `Arkana.PartidaPeloAdb` (`Assets/_Arkana/Scripts/PartidaPeloAdb.cs`), lê extras do intent via `AndroidJavaClass("com.unity3d.player.UnityPlayer").currentActivity.getIntent()` **só sob `#if UNITY_ANDROID && !UNITY_EDITOR`** (`:35-49`); qualquer exceção devolve `default` (sem pedido):

| Extra | Tipo | Lido por | Efeito | Evidência |
|---|---|---|---|---|
| `arkana_auto` | string: `partida` \| `treino` | `PartidaPeloAdb.Pedido()` → `Main._pedidoAdb` | Após 1,5 s no menu (um quadro desenhado), `Bus.EmitGameStartRequested()`; `treino` liga `ArkMenu.PedidoDeTreino`. O jogador automático **salta a 45 % da rota do castelo** (`SALTO_EM = 0.45f`) e depois fica parado; bots jogam. | `PartidaPeloAdb.cs:15,19,22`; `Main.cs:266, 288-305` |
| `arkana_fps` | int: N \| -1 | `FpsPedido()` → `Main._fpsAdb` | Depois da Config, `Application.targetFrameRate = N`. **Nota do projeto:** `-1` no Android = 30 FPS (padrão do Unity), por isso se pede 300 para medir a folga real. | `PartidaPeloAdb.cs:17,25`; `Main.cs:267, 298`; `PROJETO.md:368-370` |
| `arkana_bancada` | int: 1 | `BancadaPedida()` em `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` | Cria `BancadaDeCortes` (DontDestroyOnLoad). Ao `Bus.QuedaFase == POUSOU`, espera 6 s, e para cada corte: restaura, aplica, 2 s assentar, 8 s medindo → `Debug.Log("ARKANA BANCADA <corte> fps=.. ms=.. pior=..ms")`; segura o time do jogador vivo e de pé por quadro. Fim: `"ARKANA BANCADA FIM"`. | `BancadaDeCortes.cs:22, 32-39, 52-88` |
| `arkana_cortes` | string CSV, `+` combina | `CortesPedidos()` | Substitui a lista padrão de 15 cortes (`base, sem_ssao, sombra_2cascatas_2048, sem_sombra, escala_085, escala_070, sem_hdr, sem_pos, sem_grama, sem_vegetacao, chao_simples, sem_agua, sem_nuvens, ssao+sombra2+escala085, base_de_novo`). Cortes aceitos: `sem_ssao/ssao`, `sombra_2cascatas_2048/sombra2`, `sem_sombra`, `escala_085/080/075/070`, `sombra_dura`, `sombra_suave_baixa/media`, `sem_hdr`, `sem_pos`, `sem_grama`, `sem_vegetacao`, `sem_agua`, `chao_simples`, `sem_nuvens`. | `BancadaDeCortes.cs:24-30, 67-68, 127-151` |

Sem nenhum extra, nada disso existe — o jogo boota normal.

## 7. Performance — o que existe no código

| Ferramenta | Estado | Evidência |
|---|---|---|
| **`MedidorDeFps`** (MonoBehaviour) | Existe; adicionado ao `Main` **só fora do editor** (`#if !UNITY_EDITOR`). A cada 5 s: `"ARKANA FPS media=.. min=.. quadros=.. dt_max=..ms"` via `Debug.Log` (média = quadros/tempo; min = pior segundo; dt_max = pior quadro). Usa `Time.unscaledDeltaTime`. | `MedidorDeFps.cs:13-35`; `Main.cs:240-242` |
| **`BancadaDeCortes`** | Existe (§6). Mede FPS médio, ms médio e pior quadro por corte de GPU, com restauração entre cortes. Acha o SSAO por **reflexão** no campo privado `m_RendererDataList` (`ponytail:` comentado). | `BancadaDeCortes.cs` |
| `Application.targetFrameRate` | Config: `{30, 60, 120, -1}`, padrão 60; `arkana_fps` sobrescreve | `Config.cs:71, 180`; `Main.cs:298` |
| `QualitySettings.vSyncCount` | Config: padrão 1 (ligado). `Build.cs` zera nos assets, mas a Config religa no boot. | `Config.cs:90, 181`; `Build.cs:33` |
| `androidUseSwappy` (Frame Pacing) | Ligado (1) | `ProjectSettings.asset:75` |
| `Profiler` / `ProfilerRecorder` / `FrameTiming` / `FrameTimingManager` | **Não** (0 ocorrências); `enableFrameTimingStats: 0` | grep; `ProjectSettings.asset:160` |
| Unity Profiler remoto | **Impossível com o APK atual**: sem Development Build (`BuildOptions.None`) o Profiler não conecta | `Build.cs:110` |
| Adaptive Performance / `AndroidPerformance` | **Não** (pacote não está no manifest; 0 ocorrências) | `manifest.json` |
| `OnDemandRendering` | **Não** | grep |
| Draw calls / batches / SetPass | **Nenhuma** estatística no código (não há `UnityStats`, que é editor-only, nem `ProfilerRecorder`) | grep |
| Temperatura (`SystemInfo.batteryTemperature`, `/sys/class/thermal`) | **Não** (0 ocorrências). Os "44 °C / 47 °C" do PROJETO.md foram lidos **manualmente** no aparelho. | grep; `PROJETO.md:56, 77` |
| `SystemInfo.*` | Só `supportsInstancing` (grama/mata/praia/vegetação/terreno caem para "só colisor" sem instancing) e `graphicsDeviceType == Null` (detecta `-nographics`) | `Grama.cs:428`, `Mata.cs:374`, `Praia.cs:418`, `Vegetacao.cs:883,910`, `VisualDoTerreno.cs:347`, `Main.cs:581`, `FotoTests.cs:97` |
| Burst | `com.unity.burst 1.8.30` entra **transitivo** (gltfast → collections/burst). Nenhum `[BurstCompile]` no código do projeto (0 ocorrências). O build ainda gera `Arkana_BurstDebugInformation_DoNotShip/tempburstlibs/arm64-v8a/lib_burst_generated.txt` (Burst compilou os jobs dos pacotes). | `packages-lock.json`; grep; `ls M/Builds` |
| `Stopwatch` de carregamento | `Main.cs:225-226` (`_quadro`, `_parede`) — mede a montagem em fatias; gera `Logs/carregamento-{partida,treino}.txt` no PlayMode | `Main.cs:225`; `M/Logs/carregamento-*.txt` |

Fatos medidos que os docs registram (não medi nada): Poco F4 no chão 17-20 FPS antes dos cortes; após 21/09, Média = 34,6 FPS (pior 33 ms), Alta ~25, Baixa ~40; custo SSAO ~12 ms, sombra macia Alta ~10 ms, escala 70 % tira ~22 ms (`PROJETO.md:52-61`).

## 8. Rede e multiplayer

**Não existe nada.** Evidência:
- `grep -rniE "UnityWebRequest|System\.Net|Socket|WebSocket|Netcode|Mirror|Photon|FishNet|Transport|Multiplayer|HttpClient|Firebase|PlayFab|Authentication|Unity\.Services|Analytics|Relay|Lobby" --include=*.cs` (excluindo `Tests/`) → **1 linha**, e é um comentário sobre "mirror no eixo X" de um VFX (`VisualDosKits.GrupoB.cs:17`). Zero uso real.
- `Packages/manifest.json`: nenhum pacote de rede/serviço. Só `com.unity.modules.unitywebrequest` (módulo built-in, exigido pelo gltfast — `packages-lock.json` → `com.unity.cloud.gltfast.dependencies`). `com.unity.modules.androidjni` está no manifest e é o que `PartidaPeloAdb` usa (`AndroidJavaClass`).
- `ProjectSettings/MultiplayerManager.asset` existe (arquivo padrão do Unity 6), sem pacote Multiplayer instalado.
- `cloudProjectId` vazio, `cloudEnabled: 0` → sem Unity Gaming Services.
- Única menção a rede no código: `ARQUITETURA.md:150` — "Seeds sorteados POR PARTIDA, guardados para a rede" (preparação de determinismo, não rede).
- `design/infra/servidor-03-pipeline-build.md:13-16` fala em "servidor Linux" para G5 → **intenção**, era Godot. `PROJETO.md:97`: "G5 (multijogador) exige servidor e contas: decisão do Diretor, e só depois da G4".

Conclusão para o emulador: **não há tráfego de rede a simular**; o jogo é 100 % local (jogador + 12 bots).

## 9. Testes

| Item | Valor | Evidência |
|---|---|---|
| Arquivos | EditMode **59** `.cs` (58 de teste + `GameplayFakes.cs`), PlayMode **5** (`AbateVisualTests`, `BootTests`, `CarregamentoTests`, `FotoTests`, `VooVisualTests`) = 64 | `ls Tests/*` |
| Atributos | EditMode: **502** `[Test]`/`[TestCase]`, 0 `[UnityTest]`. PlayMode: 0 `[Test]`, **58** `[UnityTest]` | `grep -rhoE '\[(Test\|TestCase)\b' \| wc -l`; `grep -rho '\[UnityTest'` |
| asmdefs | `Arkana.Tests` (Editor-only) e `Arkana.PlayTests` (todas as plataformas, `UNITY_INCLUDE_TESTS`) — §2 | |
| Último portão registrado (21/09 14:51-14:52, clone principal) | EditMode: `total=526 passed=525 failed=0 skipped=1` (6,5 s). PlayMode: `total=94 passed=9 failed=0 skipped=85` (41,8 s). **Soma 620 = o "620 testes, 0 falhas" do PROJETO.md — mas 85 desses são `FotoTests` PULADOS** (`Assert.Ignore("sem GPU (-nographics): foto so' pelo foto.ps1")`, `FotoTests.cs:95-99`). Executados de fato: **534**. | `M/Logs/portao-resultados.xml`, `portao-playmode-resultados.xml` (`<test-run …>`), breakdown por `classname` |
| PlayMode que rodam no portão | `BootTests` 5, `CarregamentoTests` 2, `AbateVisualTests` 1, `VooVisualTests` 1 | idem |
| `foto.ps1` (com GPU) | Último: 21/09 14:53, filtro parcial, `total=3 passed=3` (19,7 s), 8 PNGs em `M/Logs/fotos/` + `diag.txt` | `M/Logs/foto-resultados.xml` |
| Mutantes | "105 mutantes mortos" (`PROJETO.md:99`, onda 17). **Nenhuma ferramenta de mutação existe**: grep `mutante\|mutant` em `.ps1/.cs/.md` só acha as 2 linhas do PROJETO.md; o `scratchpad/onda17-contrato.md` citado **não existe** no worktree. Foi processo manual ("todo teste novo se prova reintroduzindo o defeito", `00-LEIA.md:43`). | grep; `ls scratchpad` → não existe |
| Testes no aparelho | **Não automatizados.** `Arkana.PlayTests` inclui todas as plataformas, mas não há build de testes Android, nem `-runTests -testPlatform Android` em script algum. O que há é a partida automática por intent + logcat, lida por humano. | `grep` nos `.ps1` |
| Smoke test | `BootTests` (5) monta a arena inteira em cena, headless, e "reprova log inesperado" (`MedidorDeFps.cs:9`) | `Tests/PlayMode/BootTests.cs` |
| CI | Não existe (§4) | |
| Cobertura de código | Nenhuma (`com.unity.testtools.codecoverage` ausente) | `manifest.json` |

## 10. Dependências que afetam Android / execução / gráficos / input / armazenamento

**`Packages/manifest.json` (diretas) → `packages-lock.json` (resolvidas):**

| Pacote | Pedido | Resolvido | Papel |
|---|---|---|---|
| `com.unity.cloud.gltfast` | 6.20.0 | 6.20.0 | **Importador de `.glb` em tempo de editor** (`ScriptedImporter` guid `715df937…` = `GltfImporter.cs` do gltfast, confirmado em `Library/PackageCache/com.unity.cloud.gltfast@…/Editor/Scripts/GltfImporter.cs.meta`). **Nenhum `using GLTFast` no código** → não carrega glTF em runtime; no APK os `.glb` já são prefabs/meshes serializados. Arrasta `burst`, `collections`, `mathematics`, `unitywebrequest`. |
| `com.unity.inputsystem` | 1.14.0 | 1.14.0 | Toque/joystick virtual; `activeInputHandler: 2` (ambos). |
| `com.unity.render-pipelines.universal` | 17.3.0 | 17.3.0 (builtin) | URP + `shadergraph 17.3.0` + `universal-config 17.0.3` |
| `com.unity.test-framework` | 1.5.1 | **1.6.0** (builtin) | + `ext.nunit 2.0.5`, `test-framework.performance 3.5.0` (transitivo via collections, não usado) |
| `com.unity.ugui` | 2.0.0 | 2.0.0 | HUD/menu |
| `com.unity.burst` | — | 1.8.30 (transitivo) | compila jobs dos pacotes; gera `BurstDebugInformation` no build |
| `com.unity.collections` / `mathematics` | — | 2.6.8 / 1.3.3 | transitivos |
| Módulos built-in | `androidjni`, `animation`, `audio`, `imageconversion`, `imgui`, `jsonserialize`, `particlesystem`, `physics`, `screencapture`, `terrain`, `terrainphysics`, `ui`, `uielements`, `umbra`, `unitywebrequest` | | `screencapture` + `imageconversion` = as fotos do `foto.ps1`; `androidjni` = intent extras |

**`Assets/`:**
- `Assets/Plugins/` — **não existe**. `Assets/StreamingAssets/` — **não existe**.
- **`Assets/_Arkana/Resources/` = 477 MB no disco** (`du -sh`): `magos/` **437 MB** (20 magos × {`.fbx` ~11-13 MB, `-cor.png` 6-8 MB, `-normal.png` 3,7-5,4 MB}), `castelo.glb` 11 MB, `Retratos/` 11 MB (20 PNG), 44 `.glb` de cenário (50 KB-900 KB), `bau.glb`, 3 luvas `.glb`, 7 shaders, 4 materiais, 2 PNG de detalhe. Tudo em `Resources/` **entra inteiro no APK** (é a regra do Unity); o Build Report mostra 373 MB de assets descomprimidos e o APK sai com 228 MB. `Main.cs:380-402` pré-carrega por `Resources.LoadAsync` só os modelos da partida; `Mago.cs:190` carrega `magos/<slug>` por demanda.
- Git LFS: `.gitattributes:49-53` põe `mobile-unity/**/*.{fbx,glb,png,wav,ogg}` em LFS (131 objetos LFS em `mobile-unity`).
- **Armazenamento em runtime:** só `PlayerPrefs` — `Config.cs:24-29` (`PrefsPlayer`), `Grimorio.cs:47` (`grimorio.v1`), `Menu.cs:61,234` (modo dupla), `SelecaoPersonagem.cs:45-49` (mago escolhido). `Application.persistentDataPath`, `File.*`: **0 ocorrências**. Sem save em arquivo, sem permissão de storage.
- **Áudio:** `Sfx` sintetiza 48 timbres em `AudioClip` por código; **0 arquivos de áudio** (Build Report: Sounds 0 KB).
- **Cena:** uma só, `Assets/_Arkana/Scenes/Main.unity` (10,9 KB), regenerada por `MainSceneBuilder` a cada build (3 objetos: `Main`, `Directional Light`+`Sol`, `Main Camera` desligada; fog linear ligado na cena para o build levar a variante).
- Física: `DynamicsManager.asset` padrão; `TimeManager`: fixed timestep padrão, `Maximum Allowed Timestep 0.333`.

## 11. Problemas e gambiarras encontrados (relevantes para build/emulador)

| # | Problema | Evidência | Impacto | Causa provável | Solução atual? | Risco |
|---|---|---|---|---|---|---|
| 1 | **ABI só ARM64** (`AndroidTargetArchitectures: 2`; `Build.cs:44` força `AndroidArchitecture.ARM64`) | `ProjectSettings.asset:272`; `Build.cs:44`; `Builds/…/tempburstlibs/arm64-v8a` | Emulador Android **x86_64** (o padrão do Android Studio no Windows) **não instala** o APK (`INSTALL_FAILED_NO_MATCHING_ABIS`). Só imagem de sistema **ARM64** (lenta em host x86, sem aceleração completa) ou adicionar `X86_64` ao `Build.cs` (o Unity 6 suporta x86_64 no IL2CPP Android — Chrome OS/emulador) | Alvo era só o Poco F4 físico | Não | **Alto** para a estratégia de emulador |
| 2 | `Build.cs.ApplySettings()` **sobrescreve** `ProjectSettings.asset` a cada build (ABI, minSdk, targetSdk, package, IL2CPP) | `Build.cs:39-52` | Qualquer ajuste feito no inspector para o emulador (ex.: incluir x86_64) é desfeito no próximo `build_apk.ps1` | Receita "settings por script" herdada do Limiar (`Build.cs:13`) | Não | Médio |
| 3 | **Graphics API em Auto** (`m_BuildTargetGraphicsAPIs: []`) → Vulkan primeiro | `ProjectSettings.asset:402` | Em emulador, Vulkan em GPU virtual (SwiftShader/ANGLE) é o caminho mais frágil; sem lista explícita não há como forçar GLES3 sem mexer no `Build.cs`/settings | Nunca foi necessário no Poco F4 | Não | Médio |
| 4 | **APK de release sem Development Build** (`BuildOptions.None`) | `Build.cs:110` | Unity Profiler não conecta; sem Managed Debugger; stack de exceção IL2CPP reduzido | Otimizar para FPS real | Não há variante debug | Médio (para diagnóstico em emulador) |
| 5 | **Caminho absoluto do Unity cravado** em 3 `.ps1` (`C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe`) | `portao.ps1:5`, `foto.ps1:11`, `build_apk.ps1:6` | Qualquer outra máquina/versão quebra; não há variável de ambiente nem detecção | Simplicidade | Não | Baixo |
| 6 | **Licença Personal exige Unity Hub aberto**; scripts abrem o Hub por AppID e dormem 25 s fixos | `portao.ps1:8-16` | Não roda em sessão sem desktop (serviço/CI/agendado); `Start-Sleep 25` é palpite | Licenciamento do Unity | Contornado nos scripts | Médio (bloqueia CI headless) |
| 7 | **Um Unity por vez** (trava de instância) | `00-LEIA.md:22`; `ARQUITETURA.md:26` | Build e portão e foto são seriais; não dá para buildar enquanto o editor está aberto no mesmo projeto | Unity | Não | Baixo |
| 8 | **APK vai sempre para o clone principal** via `git rev-parse --git-common-dir` | `build_apk.ps1:29-35` | Um build de worktree copia o APK para `M/Builds/testes/`, mesmo que o código seja de branch experimental; sem etiqueta de branch/commit no nome | Ordem do Diretor 27/08 | Não (só o timestamp) | Baixo |
| 9 | **`Resources/` com 477 MB** (magos 437 MB: FBX + PNG 4K sem compressão de fonte) | `du -sh Assets/_Arkana/Resources` | APK 228 MB; build lento na 1ª vez (5 min 20 s); emulador com disco/RAM apertados sofre na instalação e no `Resources.Load` (o próprio `Main.cs:355` fala de mago ~10 MB + 2 texturas 2K) | Elenco importado da Meshy direto em `Resources/` | `importar_mago.ps1` já exclui metallic/roughness | Médio |
| 10 | `versionCode` fixo em 1 e `bundleVersion 1.0` | `ProjectSettings.asset:150, 180` | `adb install -r` funciona (mesma assinatura debug), mas não há como distinguir builds instalados; troca de assinatura no futuro exigirá desinstalar | Nunca publicou | Não | Baixo |
| 11 | **Sem keystore custom** (debug do Gradle) | `ProjectSettings.asset:276-277, 289` | OK para teste/emulador; bloqueia loja (G6). Debug keystore do Unity é por máquina — outra máquina gera assinatura diferente → `INSTALL_FAILED_UPDATE_INCOMPATIBLE` sem desinstalar antes | Fase | Documentado como pendência G6 | Baixo agora |
| 12 | **Sem templates Gradle / manifest custom** | `Assets/Plugins/Android` ausente | Não dá para injetar `android:debuggable`, permissões, `<uses-feature>` ou `abiFilters` sem criar os templates; por outro lado, zero manutenção | Não precisou | — | Baixo |
| 13 | `activeInputHandler: 2` (ambos) | `ProjectSettings.asset:783`; `build.log:574` | Aviso em todo build; custo mínimo de runtime; polui o log | Setup inicial | Não | Baixo |
| 14 | **Contagem de testes infla com skips**: "620 testes" inclui 85 `FotoTests` pulados em `-nographics` | `portao-playmode-resultados.xml` (`skipped="85"`) | A linha de sucesso `ARKANA: 620 testes, 0 falhas` soma `total` sem descontar `skipped` (`portao.ps1:45-49`) — não é falso, mas 534 rodaram | `Assert.Ignore` sem GPU | `foto.ps1` cobre com GPU (e só com filtro, 3 de 85 na última rodada) | Baixo |
| 15 | **Sem crash reporting, sem `logMessageReceived`, sem logger central**; prefixo `ARKANA` só em 4 classes | §6 | No emulador, um crash IL2CPP vai só para o `logcat` (tombstone) — nada captura, nada persiste | Fase | Não | Médio |
| 16 | **Sem profiler nem estatísticas de GPU no código** (draw calls, batches, temperatura) | §7 | A bancada mede FPS por corte, mas não diz "por quê" em nível de draw call; temperatura é lida a olho | Fase | `BancadaDeCortes` (parcial) | Médio |
| 17 | `BancadaDeCortes.AcharSsao` usa **reflexão** no campo privado `m_RendererDataList` do URP | `BancadaDeCortes.cs:106-108` | Quebra silenciosamente (lista vazia → corte `sem_ssao` vira no-op) se o URP renomear o campo | API não pública no URP 17 | `ponytail:` documentado | Baixo |
| 18 | `Config.Aplicar` **muta o asset URP em runtime** fora do editor (`renderScale`, cascatas, resolução da sombra) | `Config.cs:186-192` | Comportamento esperado no aparelho; no editor é desligado de propósito para não sujar o git. `BancadaDeCortes` também muta e restaura. Se um teste de PlayMode rodar em player (não editor), o asset da build é mutado — sem efeito em disco, OK | Um único asset URP para 3 perfis | Sim (guarda `isEditor`) | Baixo |
| 19 | **`design/pipeline/ANDROID.md` e `design/infra/servidor-0{3,6}` desatualizados** (método, paths, era Godot) | §1 | Quem monta ambiente novo pelo doc erra o comando; o doc de ferramentas locais lista SDK/JDK "ausentes" que hoje vêm com o Unity | Migração Godot→Unity sem varrer `design/infra` | Não | Baixo |
| 20 | `targetFrameRate = -1` no Android = 30 FPS (não "sem teto") | `PROJETO.md:368-370`; `Config.cs:71` (opção "Ilimitado" = -1) | A opção "Ilimitado" da Config **limita a 30** no aparelho (quando vSync desligado). Conhecido, contornado por `arkana_fps 300` só na automação | Semântica do Unity Android | Parcial | Baixo (UX) |
| 21 | **`adb` não está em nenhum script**; instalação, execução e leitura de log são 100 % manuais | §5 | Qualquer estratégia de emulador começa do zero em automação de deploy; nada para reaproveitar além dos extras de intent | Aparelho só "voltou ao adb" em 21/09 | Não | — (é a lacuna que a próxima fase deve fechar) |

## 12. Mapa da pasta `mobile-unity/` e arquivos importantes

```
mobile-unity/
├── 00-LEIA.md                      como rodar (portão, foto, APK), estrutura, a regra "lógica pura, MonoBehaviour casca"
├── ARQUITETURA.md                  contrato entre raias: pastas/donos, C# 9, contrato do Core, regras que atravessaram
├── portao.ps1                      EditMode + PlayMode headless (-nographics), soma totais, exit 1 em falha
├── foto.ps1                        PlayMode COM GPU, filtro -testFilter (padrão "FotoTests"), fotos + diag.txt
├── build_apk.ps1                   APK Android por -executeMethod; cópia datada no clone principal
├── importar_mago.ps1               zip da Meshy → Resources/magos/<slug>.fbx + -cor.png + -normal.png
├── Packages/  manifest.json, packages-lock.json
├── ProjectSettings/                ProjectSettings.asset, QualitySettings.asset, GraphicsSettings.asset, ProjectVersion.txt, …
└── Assets/
    ├── UniversalRenderPipelineGlobalSettings.asset, DefaultVolumeProfile.asset
    └── _Arkana/
        ├── Editor/        Build.cs, MainSceneBuilder.cs, ImportacaoArkana.cs   (Arkana.Editor)
        ├── Scenes/        Main.unity (gerada por código)
        ├── Settings/      URP_Base.asset, URP_Base_Renderer.asset
        ├── Resources/     477 MB: magos/ (20×fbx+2png), Retratos/ (20 png), castelo.glb, bau.glb, luvas, 44 glb de cenário,
        │                  7 shaders Arkana*, 4 materiais, 2 png de detalhe
        ├── Scripts/       Arkana.asmdef · Main.cs · PartidaPeloAdb.cs · MedidorDeFps.cs · BancadaDeCortes.cs
        │   ├── Core/ (17)  Gameplay/ (31) + Habilidades/ (26)  World/ (10)  Terrain/ (3)
        │   ├── Characters/ (4)  UI/ (16)  Menu/ (10)  Audio/ (1)
        └── Tests/
            ├── EditMode/  Arkana.Tests.asmdef + 58 arquivos de teste + GameplayFakes.cs   (502 [Test])
            └── PlayMode/  Arkana.PlayTests.asmdef + BootTests, FotoTests, CarregamentoTests, AbateVisualTests, VooVisualTests (58 [UnityTest])

(fora do git, só no clone principal)
mobile-unity/Builds/   arkana.apk (228 MB), build.log, testes/arkana-<data>.apk ×8, Arkana_BurstDebugInformation_DoNotShip/
mobile-unity/Logs/     portao*.log/.xml, foto*.log/.xml, fotos/*.png + diag.txt, diag-mago-NN.txt, carregamento-*.txt
mobile-unity/Library/  cache do Unity (PackageCache, Bee/Android/Prj/IL2CPP/Gradle = projeto Gradle gerado)
```

**Arquivos importantes:**

| Arquivo | Função | Por que é importante |
|---|---|---|
| `Assets/_Arkana/Editor/Build.cs` | Força todos os Player Settings Android e chama `BuildPlayer` | **Único lugar** que decide ABI, minSdk/targetSdk, IL2CPP, package, orientação, shaders incluídos e `BuildOptions`. Mudar ABI/GLES/dev-build para emulador = mudar aqui |
| `build_apk.ps1` | Invoca o Unity em batchmode com `-executeMethod` | Define onde o APK cai e a dependência do Hub |
| `portao.ps1` / `foto.ps1` | Testes headless / com GPU | O critério de "verde" do projeto; `foto.ps1` é o único que exercita a GPU no PC |
| `ProjectSettings/ProjectSettings.asset` | Estado serializado (é regravado pelo `Build.cs`) | Fonte para ler o que o último build usou; **não** é a fonte de mudança |
| `ProjectSettings/ProjectVersion.txt` | 6000.3.23f1 (09d2ecc7fb28) | Versão exata do editor a instalar |
| `Packages/manifest.json` / `packages-lock.json` | Dependências | gltfast (importador), Input System, URP 17.3, test-framework 1.6.0 resolvido |
| `Assets/_Arkana/Settings/URP_Base.asset` + `URP_Base_Renderer.asset` | Único asset URP + renderer (SSAO desligado) | Todo custo de GPU nasce aqui; a Config muta em runtime |
| `Assets/_Arkana/Scripts/PartidaPeloAdb.cs` | Lê extras do intent (`arkana_auto`, `arkana_fps`, `arkana_bancada`, `arkana_cortes`) | **O gancho de automação sem toque** — reaproveitável em emulador via `adb shell am start` |
| `Assets/_Arkana/Scripts/BancadaDeCortes.cs` | Mede FPS por corte de GPU e imprime `ARKANA BANCADA` | Única ferramenta de perf com números por hipótese |
| `Assets/_Arkana/Scripts/MedidorDeFps.cs` | `ARKANA FPS media/min/dt_max` a cada 5 s no logcat | Única telemetria de FPS no aparelho |
| `Assets/_Arkana/Scripts/Menu/Config.cs` | Perfis Baixa/Média/Alta, targetFrameRate, vSync, `PlayerPrefs` | Define o que o jogador (e o emulador) vê por padrão: Média, 60 FPS, vSync ligado |
| `Assets/_Arkana/Scripts/Main.cs` | Ciclo de vida Menu → Partida → Fim; liga `MedidorDeFps` fora do editor e consome os extras | Ponto de entrada do runtime |
| `Assets/_Arkana/Editor/MainSceneBuilder.cs` | Regenera `Main.unity` a cada build | A cena não é fonte; o código é |
| `Assets/_Arkana/Tests/PlayMode/FotoTests.cs` | 85 fotos; `Assert.Ignore` sem GPU | Explica os 85 "skipped" do portão |
| `M/Builds/build.log` | Log do último build (21/09) | Gradle 9.1.0/AGP 9.0.0, SDK/NDK/JDK do Unity, 1:36, release, warning único |
| `design/PROJETO.md` (§"Como", linhas 355-372; §21/09) | Comandos `adb` da automação, achados do Poco F4, custos por corte | O único registro escrito do processo manual de deploy/medição |
| `design/pipeline/ANDROID.md` | Identidade do app + pendências de loja | Útil, mas comando e paths desatualizados |

---

## Matriz de estado (projeto)

| Item | Estado | Versão | Observação |
|---|---|---|---|
| Engine | 🟢 | 6000.3.23f1 (09d2ecc7fb28) | Único commit em `ProjectVersion.txt`; caminho cravado nos scripts |
| Pacotes | 🟢 | gltfast 6.20.0, inputsystem 1.14.0, URP 17.3.0, test-framework 1.6.0 (pedido 1.5.1), ugui 2.0.0, burst 1.8.30 (transitivo) | Sem pacote de rede/serviço/adaptive performance |
| Build Android | 🟢 | `Build.cs` + `build_apk.ps1`; último build 21/09 Succeeded, 1:36, 0 erros, 1 warning | Release, `BuildOptions.None`; sem variante debug |
| Gradle (templates) | ⚪ | Gradle 9.1.0 / AGP 9.0.0 embutidos no Unity; **sem** templates custom em `Assets/Plugins/Android` | Padrão do Unity; nada para manter, nada para injetar |
| NDK/IL2CPP | 🟢 | NDK/JDK/SDK do próprio módulo Android do Unity; IL2CPP Release, stripping padrão | Sem `additionalIl2CppArgs` |
| ABI | 🔴 | **ARM64 apenas** (`AndroidTargetArchitectures: 2`, forçado em `Build.cs:44`) | Emulador x86_64 não instala; precisa imagem ARM64 ou `X86_64` no `Build.cs` |
| Graphics API | 🟡 | Auto (`m_BuildTargetGraphicsAPIs: []` → Vulkan, fallback GLES3); Linear; Swappy ligado | Não há como forçar GLES3 para emulador sem mudar settings |
| minSdk/targetSdk | 🟢 | 26 / 35 (explícitos em `Build.cs:45-46` e `.asset:181-182`) | Emulador precisa API ≥ 26 |
| Keystore/assinatura | 🟡 | Debug keystore do Gradle; `androidUseCustomKeystore: 0`, campos de nome vazios, campos de senha ausentes | OK para emulador; bloqueia loja (G6) |
| Deploy | 🔴 | Nenhum script; `adb` só em comentários e docs | Manual: Drive ou cabo USB + `adb install` (comando nem está escrito) |
| Debug | 🟡 | Sem Development Build, sem crash reporting, sem `logMessageReceived`; extras de intent para partida automática | `adb logcat -s Unity` é o único canal |
| Logs | 🟡 | `Debug.Log` com prefixo `ARKANA` em 4 classes (FPS, BANCADA, Logo, Textos); `Logs/` só no PC (portão/foto/diag) | Nada persiste no aparelho |
| Perf tooling | 🟡 | `MedidorDeFps` (5 s) + `BancadaDeCortes` (custo por corte); sem Profiler, FrameTiming, draw calls, temperatura | O que existe é bom e reaproveitável; o que falta é o "por quê" |
| Rede | ⚪ | Inexistente (0 usos; só módulo `unitywebrequest` transitivo do gltfast) | G5 é decisão futura do Diretor |
| Testes | 🟢 | 502 `[Test]` EditMode + 58 `[UnityTest]` PlayMode; último portão 526+94 (534 executados, 85 fotos puladas), 0 falhas; `foto.ps1` com GPU | Sem testes no aparelho, sem cobertura, mutação manual |
| CI | 🔴 | Nenhum (`.github`, GitLab, Azure, Jenkins, CircleCI, Bitrise ausentes) | Licença Personal + Hub aberto + trava de instância dificultam headless |

## COMANDOS EXECUTADOS (somente leitura)

```
Comando:   ls -la mobile-unity; cat ProjectSettings/ProjectVersion.txt; cat Packages/manifest.json; find Assets -name "*.asmdef";
           find Assets -name "*.shader" -o -name "*.hlsl" -o -name "*.shadergraph" -o -name "*.compute";
           ls Assets/Plugins Assets/StreamingAssets; find Assets -iname AndroidManifest.xml -o -iname "*.gradle" -o -iname "*.aar" -o -iname "*.jar" -o -iname "*.so"
Objetivo:  estrutura, versão, pacotes, assemblies, shaders, plugins/templates Android
Resultado: 6000.3.23f1 (09d2ecc7fb28); 5 pacotes diretos + 15 módulos; 4 asmdefs; 7 .shader em Resources; Plugins/StreamingAssets/templates: inexistentes

Comando:   cat 00-LEIA.md ARQUITETURA.md portao.ps1 foto.ps1 build_apk.ps1 importar_mago.ps1
Objetivo:  fluxo documentado e o que os scripts fazem de fato
Resultado: caminho absoluto do Unity, abertura do Hub por AppID + sleep 25 s, WaitForExit, cópia do APK para o clone principal via git-common-dir

Comando:   cat -n Assets/_Arkana/Editor/Build.cs; cat <cada .asmdef>; cat Packages/packages-lock.json
Objetivo:  o que o build força; referências dos assemblies; versões resolvidas
Resultado: IL2CPP/ARM64/minSdk26/targetSdk35/Linear/landscape/BuildOptions.None; test-framework resolvido 1.6.0; burst 1.8.30 transitivo

Comando:   grep -nE "^\s*(productName|companyName|bundleVersion|AndroidBundleVersionCode|AndroidMinSdkVersion|AndroidTargetSdkVersion|AndroidTargetArchitectures|androidUseCustomKeystore|AndroidKeystoreName|AndroidKeyaliasName|androidApplicationEntry|androidUseSwappy|m_BuildTargetGraphicsAPIs|m_BuildTargetGraphicsJobs|gcIncremental|scriptingBackend|il2cpp\w+|managedStrippingLevel|apiCompatibilityLevel|m_ActiveColorSpace|activeInputHandler|enableCrashReportAPI|cloudEnabled|…)\s*:" ProjectSettings/ProjectSettings.asset
           sed -n '170,200p' …; grep -nA6 "scriptingBackend:" …; grep -n "Keystore\|Keyalias" … | sed -E 's/(Pass:).*/\1 [valor nao lido]/'
Objetivo:  extrair a configuração Android sem expor segredos
Resultado: tabela da §3.1; campos AndroidKeystorePass/KeyaliasPass não existem no arquivo; keystore custom desligada

Comando:   cat ProjectSettings/QualitySettings.asset ProjectSettings/EditorBuildSettings.asset; grep -n "m_AlwaysIncludedShaders|m_CustomRenderPipeline" -A1 ProjectSettings/GraphicsSettings.asset
           ls Assets/_Arkana/Settings; grep -nE "m_RenderScale|m_MSAA|m_SupportsHDR|m_MainLightShadowmapResolution|m_ShadowCascadeCount|m_SoftShadowQuality|…" URP_Base.asset; grep -nE "m_RenderingMode|m_RendererFeatures|m_Active|AOMethod|…" URP_Base_Renderer.asset
           grep -nE "m_Strip\w+|m_EnableRenderCompatibilityMode" Assets/UniversalRenderPipelineGlobalSettings.asset; cat ProjectSettings/TimeManager.asset
Objetivo:  níveis de qualidade, URP, SSAO, stripping
Resultado: 6 níveis, Android padrão Medium, um único asset URP (renderScale 1, 4096×4, soft Low, HDR on, MSAA off); SSAO serializado mas m_Active: 0

Comando:   find Assets -name "*.cs" | wc -l (+ por pasta); grep -rhoE '\[(Test|TestCase)\b' Tests/EditMode | wc -l; grep -rho '\[UnityTest' Tests/PlayMode | wc -l; ls Tests/EditMode Tests/PlayMode
           du -sh Assets/_Arkana/Resources (+ /*, magos/*); ls -la Assets/_Arkana/Resources Assets/_Arkana/Resources/magos; ls Assets/_Arkana/Scenes
Objetivo:  contagens de código/teste; peso de Resources
Resultado: 189 .cs (122+3+59+5); 502 [Test] + 58 [UnityTest]; Resources 477 MB (magos 437 MB); 1 cena

Comando:   grep -rn "arkana_" --include=*.cs Assets; grep -rn "AndroidJavaObject\|AndroidJavaClass\|getIntent\|getStringExtra\|getIntExtra" …; grep -rc '"ARKANA' …;
           grep -rn "logMessageReceived\|developerConsoleVisible\|Crashlytics\|Sentry\|Backtrace\|CloudDiagnostics\|CrashReport\|UnhandledException" …;
           grep -rn "targetFrameRate\|vSyncCount\|OnDemandRendering\|ProfilerRecorder\|FrameTiming\|Profiler\.\|batteryTemperature\|thermal\|AdaptivePerformance\|SystemInfo\.\|QualitySettings\." … | grep -v /Tests/
Objetivo:  automação por intent, logging, crash, perf
Resultado: PartidaPeloAdb/BancadaDeCortes/MedidorDeFps/Main/Config (§6-7); 0 crash reporting; 0 Profiler/FrameTiming/thermal

Comando:   cat -n PartidaPeloAdb.cs BancadaDeCortes.cs MedidorDeFps.cs; sed -n '140,230p' Menu/Config.cs; grep -n "PerfilDeVideo|FpsOpcoes|K_QUALIDADE|K_VSYNC" Menu/Config.cs; sed -n '225,320p' Main.cs; sed -n '85,100p' World/Sol.cs
Objetivo:  semântica exata dos extras, perfis de vídeo, ligação no boot
Resultado: §6-7 e §3.4

Comando:   grep -rniE "UnityWebRequest|System\.Net|Socket|WebSocket|Netcode|Mirror|Photon|FishNet|Transport|Multiplayer|HttpClient|Firebase|PlayFab|Authentication|Unity\.Services|Analytics|Relay|Lobby" --include=*.cs Assets | grep -v /Tests/
           grep -rn "Permission\.\|RequestUserPermission\|INTERNET\|EXTERNAL_STORAGE" …; grep -rn "GLTFast\|GltfImport\|Unity.Burst\|BurstCompile\|Addressables" …; grep -rn "Resources.Load" … | wc -l;
           grep -rn "PlayerPrefs\|persistentDataPath\|grimorio.v1\|File.WriteAllText\|File.ReadAllText\|File.Exists" …; head -10 Resources/bau.glb.meta; grep -rl <guid do importador> M/Library/PackageCache
Objetivo:  rede, permissões, gltfast em runtime, armazenamento
Resultado: rede = 1 comentário de VFX; 0 permissões; 0 GLTFast em código (importador do gltfast confirmado no PackageCache); 38 Resources.Load; só PlayerPrefs

Comando:   ls -la M/Builds M/Builds/testes M/Logs M/Logs/fotos; ls -R M/Builds/Arkana_BurstDebugInformation_DoNotShip
           grep -nE "Gradle Version|JDK:|Android SDK:|Android NDK:|BuildSummary|Build Finished|Complete build size|arm64-v8a|x86_64|assembleRelease|launcher-release|Build Report|warning|Exception|error CS" M/Builds/build.log (filtrando CopyFiles/Bee)
           head -5 / tail -20 / sed -n '574,582p;6838,6862p;6863,6900p' M/Builds/build.log; grep -oE "2026-09-21T[0-9:.]+Z" … | head/tail
Objetivo:  como o último build correu (toolchain, tempo, resultado, ABI, tamanho, avisos)
Resultado: §4

Comando:   grep -oE '<test-run [^>]*' M/Logs/portao-resultados.xml | grep -oE '(total|passed|failed|skipped|start-time|duration)="[^"]*"' (idem playmode e foto);
           grep -oE 'result="[A-Za-z]+"' portao-playmode-resultados.xml | sort | uniq -c; … | grep -oE 'classname="[^"]*"' | sort | uniq -c;
           grep -rnE "\[Explicit|\[Ignore|Assert\.Ignore|graphicsDeviceType.*Null" Tests/PlayMode; sed -n '95,99p' FotoTests.cs; tail -3 portao.log foto.log; head -12 M/Logs/fotos/diag.txt
Objetivo:  o que o último portão de fato executou; por que 85 pulados
Resultado: EditMode 526/525/0/1 skipped; PlayMode 94/9/0/85 skipped (todos FotoTests, Assert.Ignore sem GPU); foto 3/3

Comando:   ls -la <raiz do worktree>; ls -d .github .gitlab-ci.yml azure-pipelines.yml Jenkinsfile .circleci bitrise.yml;
           grep -rlE "\badb\b" --include=*.ps1 --include=*.md --include=*.cs --include=*.txt .; grep -rnE "\badb\b" --include=*.ps1 --include=*.cs .; grep -rnE "adb (install|shell|logcat|devices|push|pull)" --include=*.md .
           grep -rniE "mutante|mutant" --include=*.ps1 --include=*.cs --include=*.md .; ls -d scratchpad .claude/scratchpad
           grep -rnoE "6000\.[0-9]+\.[0-9]+f[0-9]+" … | grep -v 6000.3.23f1; git log --oneline -- mobile-unity/ProjectSettings/ProjectVersion.txt
Objetivo:  CI, deploy, mutação, versões conflitantes
Resultado: sem CI; adb só em comentários/docs; mutação sem ferramenta (scratchpad citado não existe); nenhuma outra versão de Unity; 1 commit no ProjectVersion

Comando:   cat design/pipeline/ANDROID.md design/infra/servidor-03-pipeline-build.md design/infra/servidor-06-ferramentas-locais.md; sed -n '36,80p;95,102p;355,372p' design/PROJETO.md; grep -nE "Drive|instala|cabo|USB|Builds/testes|adb install" design/PROJETO.md
           head -40 README.md; cat swarm.yaml; ls arte arte/tools; grep -n "lfs\|mobile-unity" .gitattributes; git lfs ls-files | grep -c mobile-unity; grep -nE "Builds|Logs|Library|apk" .gitignore
Objetivo:  intenção vs. realidade; processo manual; LFS
Resultado: ANDROID.md com comando/paths errados; servidor-03/06 era Godot; deploy manual (Drive/USB); LFS cobre fbx/glb/png/wav/ogg do mobile-unity (131 objetos)
```

## O que NÃO consegui verificar (e por quê)

1. **Manifesto efetivo do APK** (permissões reais — se `INTERNET` entrou por causa do módulo `unitywebrequest`; `android:debuggable`; `<uses-feature>` de GLES/Vulkan; `abiFilters`). Exigiria `aapt dump badging`/`apkanalyzer` sobre `M/Builds/arkana.apk` ou abrir `M/Library/Bee/Android/Prj/IL2CPP/Gradle/unityLibrary/src/main/AndroidManifest.xml`. Eram ações de ferramenta sobre binário/cache, fora do "só cat/grep" do mandato; deixo como primeiro passo do próximo agente (é leitura, sem risco).
2. **`compileSdk`, `buildToolsVersion`, versão exata do NDK e do JDK** — o `build.log` do Unity 6 só imprime os *caminhos*; os números estão em `…\AndroidPlayer\SDK\platforms\`, `…\NDK\source.properties` e `…\OpenJDK\release` (fora do repositório; não li a instalação do Unity).
3. **Se `adb` está no PATH do Windows** e qual versão de `platform-tools` — não executei `adb` (nem `where adb`).
4. **Onde a HUD desenha `K_CONTADOR_FPS`** — não li `Hud.cs` (1 300+ linhas) para isso; a chave existe na Config com padrão `false`.
5. **Duração real do primeiro build limpo** — só o que o PROJETO.md registra (5 min 20 s em 12/09); o `build.log` atual é incremental (1:36).
6. **Comportamento do APK em emulador** — nada foi rodado; a afirmação "x86_64 não instala" vem da ABI declarada (`arm64-v8a` apenas) e do comportamento padrão do `PackageManager`, não de um teste.
7. **Conteúdo de `M/Library/Bee/Android/Prj/IL2CPP/Gradle/`** (o projeto Gradle gerado, com `build.gradle` real, `minSdk`/`targetSdk`/`ndkVersion` efetivos) — existe (path aparece no `build.log:6842`), mas não o li: é cache regenerável e está fora do que o mandato pediu; é a segunda leitura mais útil para o próximo agente depois do manifesto.


---

# ANEXO C — HISTÓRICO, DECISÕES E PROBLEMAS (relatório da raia documentador)


**Data:** 2026-09-23 · **Raia C (documentador)** · **SOMENTE LEITURA** — nada foi alterado, buildado, instalado ou commitado. Tudo abaixo vem de documento ou de `git log`; onde a fonte não diz o motivo, está escrito "Motivo não identificado no repositório".

**Raiz das fontes:** `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA\.claude\worktrees\auditoria-tecnica-completa-282af1\` (worktree da branch `claude/auditoria-tecnica-completa-282af1`, HEAD `0a94c2f`, árvore limpa). Memória externa: `C:\Users\VINICIUS\Videos\MEUS PROJETOS\Agentes\_memoria\`.

**Para que serve este arquivo:** alimentar a decisão sobre um Android Emulator local. Por isso cada seção termina apontando o que toca esse tema.

---

## 1. Linha do tempo e estágio de maturidade

### 1.1 Marcos (data → marco → evidência)

| Data | Marco | Evidência |
|---|---|---|
| 17/08/2026 | Protótipo 2D "Campo de Provas" (Phaser/TypeScript) e pipeline Android por **Capacitor** + controles de toque (APK antigo) | commits `14e334e`, `9b4e1d8`, `49ac89e`; `CHANGELOG.md` linhas 92-99 |
| 17–19/08 | **Roblox** "Campo de Provas" (Rojo, servidor autoritativo, Sintonia, BR, R0–R15) | `52c93f9`, `18c375c`…`5ced4c0`; `design/PONTE.md` (auditoria Roblox×GDD, 19/08) |
| 19/08 | **Pivô para Godot 4.4 3D no Android** (R16, G0+G1: APK, ilha, mago procedural, toque) | `34668f7`; `CHANGELOG.md` v1.0.0-3d |
| 20/08 | Godot G2 fechado (terreno reativo, 5 elementos, áudio procedural); pasta `infra/` criada | `40b9986`; `CHANGELOG.md` v1.3.0-3d; `design/infra/servidor-00-LEIA.md` |
| 21/08 | R20: zona, habilidades, escudo, derrubado, baú; Pyra em 3D (Meshy) | `663ccec`; `CHANGELOG.md` |
| 25/08 | Diretriz "tamanho do APK não importa"; **Poco F4** identificado como aparelho de teste; a queda do castelo; Brok 3D; higienização; auditoria externa — **"nenhum aparelho apareceu em `adb devices`"** | `design/PROJETO.md` linhas 17-48, 1480-1502; `b1729ef`, `f0a2aaf` |
| 26/08 | A DIREÇÃO (20 respostas do Diretor); castelo/luvas/baú em 3D pelo site da Meshy; 160 referências para o **Git LFS**; dois vídeos do Diretor (WhatsApp) avaliados; docs de infra | `e7fcc64`, `fe9a52d`, `4fa9755`; `PROJETO.md` linhas ~1010-1420 |
| 27/08 | Ilha 600 m, tempestade de BR, 12 bots; **leva 7 desfeita** (18 magos da API animavam "como robô"); **APK sempre numa pasta só, venha de worktree ou não**; **virada para PC/Steam/Unreal** | `99bb7d8`, `6b383f0`, `80fb42e`, `33f3ac0`, `ee7d868` |
| 28/08 → 04/09 | Ilha Fraturada no Unreal 5: 2.400 m, 1.396 peças, 3,1 M tris (Nanite), 46 FPS no PC; sete peças novas na Meshy | `b9952ee`, `0a58410`, `60ab530`, `5d5a64b`; `CHANGELOG.md` 09/09 |
| 01/09 | Regra permanente do repositório (pasta central, `main` por ff, sem pastas novas) | `2f508f7`; `CLAUDE.md` |
| **09/09** | **Desvio desfeito. O produto é celular em Unity 6.** Unreal, Lyra e Godot apagados (~48 GB); `pc-unreal/` removido; esqueleto de `mobile-unity/` | `8a33037`, `d327e75`; `README.md` "A decisão de 09/09"; `PROJETO.md` linhas 276-286 |
| 11/09 | Levas 1–3 da reescrita (portão 146 → 232 → 257 testes); ferramenta de foto | `ab856d1`, `431f280`, `ba6839a`, `01402d7` |
| **12/09** | **Primeiro APK Unity jogado no Poco F4, 60 FPS medidos**; partida pelo adb sem dedo; 20 magos reais + kits; ondas 3–11; 420 testes | `1f89429`, `e4a616b`, `51d4802`, `0f813fd`; `PROJETO.md` linhas 288-298 |
| 13/09 | Costa, céu, praia, ruínas Meshy, tela de carregamento; 461 testes | `bff563e`, `c4da78e`; `PROJETO.md` 145-185 |
| 16/09 | O corpo do mago (luva, pulo, recuo), logo, botões do menu, mata; APK `arkana-2026-09-16_1023.apk` (221 MB); 509 testes | `e23c46f`, `699857b`, `ae92b02`, `55dc82d`; `PROJETO.md` 115-143 |
| 19/09 | **G4 aberta**: dupla + Sintonia (onda 17, 4 raias); 562 testes | `9f81a71`; `PROJETO.md` 95-113 |
| 21/09 manhã | **Aparelho voltou ao adb**: 17–20 FPS no chão (gargalo GPU); `BancadaDeCortes` | `2d8510f`, `789bc46`; `PROJETO.md` 74-77 |
| 21/09 tarde | **16 → 34,6 FPS** (SSAO off, sombra baixa, Qualidade real); gelo sustenta; onda 18; **620 testes, 0 falhas** | `9749474`, `b9831a0`, `0a94c2f`; `PROJETO.md` 54-72 |
| 23/09 | Esta auditoria (somente leitura) | `_memoria/STATUS_arkana-auditoria-android.md` |

### 1.2 Estágio atual e classificação de maturidade

**Fases do roadmap (avaliação de 19/09, `PROJETO.md` linha 97):** G0–G2 fechadas; G3 "praticamente fechada"; **G4 é a fase aberta** (dupla e Sintonia entraram em 19/09, acabamento na onda 18 de 21/09); **G5 (rede) exige servidor e contas — não começou**; **G6 (loja) é ato do Diretor**.

**Classificação (minha, com evidência): vertical slice jogável / pré-alpha solo contra bots.** Não é alpha porque:

| Evidência a favor de "jogável e sólido" | Evidência contra "alpha" |
|---|---|
| 20 magos reais (FBX do site da Meshy) com kit completo (`PROJETO.md` 187-194) | **Rede não existe** — "o item mais caro e ainda não começou" (`README.md` Estado; `design/infra/00-LEIA.md`) |
| Portão headless: **620 testes, 0 falhas** (21/09) | **Nenhum terceiro jogou**: só o Diretor e o jogador automático via adb (`PONTE.md` §0: "o jogo nunca foi tocado por terceiros") |
| APK jogado no aparelho em 12/09, 16/09 (pelo Diretor) e 21/09 (via adb) | Sem loja, sem conta Play, sem keystore de release, sem AAB (`design/pipeline/ANDROID.md` 30-43) |
| Partida inteira roda sozinha (castelo → queda → 12 bots → tempestade) | Sem CI, sem crash report, sem telemetria no Unity (`servidor-03-pipeline-build.md` "CI passa a valer em G5"; `02-SERVIDORES.md` §4) |
| FPS medido e otimizado por bancada no aparelho (34,6 FPS Média) | Números da Sintonia "NÃO validados" (variante A do Roblox) — `PROJETO.md` 104 |
| Fotos de regressão (71 fotos, 38 testes) e `diag.txt` | Presságios, Espírito Errante, bots com kit ainda na fila (`PROJETO.md` 93) |

**Relevância para o emulador:** o produto hoje é *um processo só, no aparelho*, sem rede — um emulador cobre 100% do escopo atual (nada depende de servidor). O que ele não cobre é o que os docs elegem como critério: FPS real na GPU Adreno e o "veredito pelo visual" do Diretor.

---

## 2. Processo de deploy documentado (Código → Build → APK → aparelho)

### 2.1 A cadeia, como os docs descrevem

| Etapa | Como está documentado | Fonte |
|---|---|---|
| **Código** | Lógica em classe pura, `MonoBehaviour` só como casca; raias escrevem em cópias no rascunho e compilam fora do Unity (Roslyn `csc.dll` contra as DLLs do `Library`), porque **só cabe um Unity por vez** (trava de instância) | `mobile-unity/00-LEIA.md` 40-46; `ARQUITETURA.md` 25-32; `PROJETO.md` 300-309, 243-248 |
| **Portão** | `powershell -File mobile-unity\portao.ps1` → EditMode + PlayMode headless (`-batchmode -nographics -runTests`); sucesso = `ARKANA: N testes, 0 falhas` | `portao.ps1` linhas 1-55; `00-LEIA.md` 9-11 |
| **Fotos** | `foto.ps1 [filtro]` → PlayMode **com GPU** (sem `-nographics`) grava PNGs em `Logs/fotos/` + `diag.txt`; "toda leva visual termina olhando as fotos" | `foto.ps1`; `00-LEIA.md` 14-17; `PROJETO.md` 316-326 |
| **Build** | `build_apk.ps1` → Unity `-batchmode -nographics -buildTarget Android -executeMethod Arkana.EditorTools.Build.Android` → `Builds/arkana.apk` → **cópia datada em `mobile-unity/Builds/testes/arkana-AAAA-MM-DD_HHMM.apk` do CLONE PRINCIPAL** (resolvido por `git rev-parse --git-common-dir`), fora do git | `build_apk.ps1` 20-37; `Build.cs` 95-122; `ANDROID.md` 16-28; `.gitignore` (`*.apk`, `Builds/`) |
| **Tempo/tamanho** | 5 min 20 s a primeira vez, 1 min 33 s incremental; 176 MB (12/09) → 221 MB (16/09) | `PROJETO.md` 296, 129 |
| **Instalação — caminho 1 (o usual)** | **O Diretor instala ele mesmo**: "o Diretor sobe ele mesmo no Drive ('JOGOS EM DESENVOLVIMENTO')" | `PROJETO.md` 129 |
| **Instalação — caminho 2 (quando há cabo)** | adb via USB. O MIUI **recusa** `install -g` e instalação sem "Instalar via USB" ligado; o comando `adb install` em si não está escrito nos docs (está implícito em "Três APKs em 25 minutos") | `PROJETO.md` ~355-361 (bloco "PASSO A FEITO") |
| **Execução** | `adb shell am start -n br.com.vstack.arkana/com.unity3d.player.UnityPlayerGameActivity --es arkana_auto partida --ei arkana_fps 300` (ou `treino`; sem extras = jogo normal). Extras novos de 21/09: `--ei arkana_bancada 1 --es arkana_cortes "base,escala_070,..."` | `PROJETO.md` 363, 77, 64; código em `Assets/_Arkana/Scripts/PartidaPeloAdb.cs`, `Main.cs:298` |
| **Toque pelo computador** | MIUI: só com "Depuração USB (configurações de segurança)" ligada o `adb shell input tap/swipe` funciona (descoberto 21/09); antes disso `INJECT_EVENTS` era recusado | `PROJETO.md` 70, 77 |
| **Disponibilidade do aparelho** | "nenhum aparelho em `adb devices`" em 25/08, 12/09 tarde, 16/09; voltou em 21/09. A fila diz "prioridade 1 **quando o celular voltar**" | `PROJETO.md` 44, 213, 143, 76, 93 |

### 2.2 Identidade do aplicativo (para um AVD)

| Campo | Valor | Fonte |
|---|---|---|
| Engine | Unity 6000.3.23f1 (URP), Hub 3.21, módulos Android e Windows | `ANDROID.md` 8-14; `PROJETO.md` ~430 |
| Package | `br.com.vstack.arkana` (mudar é uma linha em `Editor/Build.cs` + `ProjectSettings`) | `Build.cs:42`; `PROJETO.md` "Decisões que esperam o Diretor" nº 6 |
| Activity | `com.unity3d.player.UnityPlayerGameActivity` | `PROJETO.md` 363 |
| Backend / arquitetura | **IL2CPP, ARM64 somente** | `Build.cs:43-44`; `ANDROID.md` 12 |
| API | minSdk 26, targetSdk 35; paisagem obrigatória | `Build.cs:45-52` |
| Espaço de cor | Linear (calibrado no Godot) | `Build.cs:38-39` |
| Shaders | `m_AlwaysIncludedShaders` preenchido por código (`Shader.Find` devolve null no aparelho se não incluído) | `Build.cs:58-93` |

**Relevância para o emulador:** o APK é **ARM64-only**; os docs não registram nenhum build x86/x86_64. Um AVD x86_64 só rodaria com tradução ARM (não documentada). O Package/Activity e os extras de intent permitem repetir a "partida sem dedo" em qualquer dispositivo adb.

---

## 3. Processo de debug documentado

| Ferramenta | O que faz / quando usar | Fonte |
|---|---|---|
| `foto.ps1` + `diag.txt` | Fotos do jogo com GPU no PC, na proporção e densidade do Poco F4; `diag.txt` diz o que câmera/corpo/kits tocam ("dois palpites sobre um quadro estranho erraram antes de o dado chegar"). Filtro por regex evita a rodada inteira (~15 min) | `foto.ps1` 1-7; `PROJETO.md` 294, 316-326 |
| `adb logcat -s Unity \| grep "ARKANA FPS"` | Média, pior segundo, pior quadro a cada 5 s (`MedidorDeFps.cs`) | `PROJETO.md` 364 |
| `adb logcat -s Unity \| grep "ARKANA BANCADA"` | 15 cortes de GPU de 10 s cada depois do pouso, FPS por corte (`BancadaDeCortes.cs`, commit `2d8510f`) | `PROJETO.md` 77, 64 |
| `adb logcat -d` | **O Unity reinicia o servidor adb e corta o `logcat` em curso: ler com `-d` depois** | `_memoria/LICOES.md` 21/09 (linha ~258) |
| `adb shell top -H` | CPU por thread: mostrou `UnityMain` 17% e render 10% com o jogo a 17 FPS → **gargalo é GPU** | `PROJETO.md` 77 |
| `adb exec-out screencap -p > foto.png` | Tela do aparelho | `PROJETO.md` 365 |
| Vídeo do WhatsApp a 0,5–1 quadro/s | O Diretor grava; ffmpeg extrai frames; cada achado é conferido contra o código antes de reportar; descontar overlays do Android (ponto verde do microfone) | `LICOES.md` 26/08 (~166) e 08/09 (~243) |
| HUD / chips de estado | Estados dos kits viram chip na HUD (`52270d0`); HUD de debug foi o diagnóstico no Limiar | `git log`; `LICOES.md` 08/09 |
| Critério de crash | "sem erro nem exceção no logcat, numa partida inteira" (12/09) | `PROJETO.md` 297 |
| Critério de problema Android | "defeito que só aparece num aparelho tem causa que só existe naquele aparelho — procure a variável que muda com o aparelho" (dp vs px, MIUI) | `LICOES.md` 19/08 (~112); `PONTE.md` T5 |
| Critério de performance | "FPS se mede, não se estima"; medir no aparelho antes de cortar; a bancada transforma "caiu" em ms por efeito | `PROJETO.md` 32-35, 56-58 |
| Critério gráfico | O Diretor julga pelo visual; foto no PC + foto/vídeo no aparelho; teste que fica vermelho reintroduzindo o defeito | `PROJETO.md` 316-326; memória `diretor-julga-pelo-visual` |
| Testes PlayMode "silenciosos" | `BootTests` montam a arena e rodam 3 s sem um log sequer (falha em qualquer `Debug.Log`) | `PROJETO.md` 293 |

**Não existe nos docs:** crash reporting em produção, Unity Profiler remoto, Android Studio profiler, GPU profiler (Snapdragon Profiler), `adb bugreport`, systrace, Firebase. A telemetria descrita em `infra/02-SERVIDORES.md` §4 ("relatório de erro") é planejada para o degrau 3.

**Relevância para o emulador:** tudo que está documentado é **adb + logcat + intent extras**, agnóstico de aparelho — funciona igual num AVD. O único método que só existe no PC (`foto.ps1`) já é o substituto de "tela do aparelho" hoje.

---

## 4. Medições no aparelho (Poco F4) e outras medições registradas

| Data | Onde | APK / build | Medição | Fonte |
|---|---|---|---|---|
| 25/08 → 04/09 | Godot | vários (141 MB) | **"FPS no aparelho nunca foi medido. Nenhum aparelho apareceu em `adb devices` em três semanas."** | `mobile-godot/00-LEIA.md` 44-46; `PROJETO.md` 872, 1502 |
| 27/08 | Godot headless (PC) | — | ilha 600 m: 184.967 tris, 67 draw calls na tela; colisão 34.848 faces; bots ~0,25 ms/bot/passo (12 = 2,6 ms) | `mobile-godot/00-LEIA.md` 36-43 |
| 28/08 | Unreal 5 (PC) | — | 3,1 M tris com Nanite; ilha de 1.396 peças a 46 FPS | `b9952ee`; `CHANGELOG.md` 09/09 |
| 12/09 madrugada | **Poco F4** | 3º APK Unity (176 MB) | **60 FPS sustentados, 16,6 ms, pior quadro 33 ms, 36,9 °C**, sem erro no logcat; **tela estava em 60 Hz (vsync segura aí)**; boot com dois engasgos de 6 s; `targetFrameRate=-1` = 30 FPS no Android | `PROJETO.md` 297, 340-372 |
| 12/09 | build | — | 5 min 20 s primeira vez; 1 min 33 s incremental | `PROJETO.md` 296 |
| 13/09 | editor | — | tela de carregamento: 7 quadros, 254 ms, pior 151 ms | `PROJETO.md` 177 |
| 16/09 | — | `arkana-2026-09-16_1023.apk` (221 MB) | sem aparelho no adb; o Diretor jogou o APK de 13/09 e reclamou de luva/pulo/recuo (eram código, não modelo) | `PROJETO.md` 117, 129, 143 |
| 21/09 manhã | **Poco F4** | `arkana-2026-09-19_1725` | castelo/queda **36–40 FPS**; **chão 17–20 FPS a partida inteira**, pior quadro 66–165 ms; 44 °C; `top -H`: UnityMain 17%, render 10% → **GPU** | `PROJETO.md` 77 |
| 21/09 tarde | **Poco F4** | `arkana-2026-09-21_1418` (bancada) | base 15,7–17 FPS; **SSAO ~12 ms**; **sombra macia ALTA ~10 ms**; **escala 70% tira ~22 ms**; grama/água/nuvens/HDR/pós 0–2,6 ms cada | `PROJETO.md` 56-58 |
| 21/09 tarde | **Poco F4** | commit `9749474` | **Alta** (100%, 4×4096) ~25 FPS · **Média (padrão)** 85% + 2×2048 = **34,6 FPS, pior quadro 33 ms, 47 °C** · **Baixa** 70% ~40 FPS · castelo/queda 42–52 | `PROJETO.md` 59-62 |

**Relevância para o emulador:** todo número de FPS acima é da **GPU Adreno 650 real**; os docs são explícitos que o gargalo é GPU. Um emulador com GPU do host (RTX/GTX via ANGLE/SwiftShader) mede outra coisa — os docs não autorizam substituir a medição do aparelho ("FPS se mede no aparelho").

---

## 5. Problemas já encontrados e resolvidos

Formato: **Problema / Como foi resolvido / Quando / Onde / Por quê / Limitações.**

### 5.1 Toolchain, build e scripts (o que um emulador herda diretamente)

| # | Problema original | Como foi resolvido | Quando | Onde está a solução | Por que essa solução | Limitações |
|---|---|---|---|---|---|---|
| 1 | **Licença Personal exige Hub aberto**: batchmode morre com "No valid Unity Editor license found" (**exit 198**) | Os três scripts checam `Get-Process "Unity Hub"` e abrem o Hub, esperando 25 s | medido 09/09 e 11/09 | `portao.ps1` 8-16; `build_apk.ps1` 13-18; `foto.ps1` 12-16 | A Personal é resolvida online pelo cliente de licença que o Hub sobe | Espera fixa de 25 s; depende do Hub instalado como MSIX; nenhum fallback para licença offline/serial |
| 2 | **`unityhub://` abre "instalar editor"** ("versão do Editor arquivado", visto pelo Diretor 11/09) | Abrir o Hub pelo **AppID**: `shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub` | 11/09 | `portao.ps1` 12-14 | Sem caminho, o Hub entende o protocolo como pedido de instalação | AppID fixo do MSIX; se o Hub mudar de pacote, o script quebra |
| 3 | **`Start-Process -Wait` trava para sempre**: espera a árvore e o `VBCSCompiler` (Roslyn) fica vivo ~10 min | `$P.WaitForExit()` só no processo do Unity | 12/09 | `portao.ps1` 27-31 e os outros dois | O compilador é servidor separado; só o Unity interessa | — |
| 4 | **`ExitCode` volta vazio** | `$null = $P.Handle` antes do `WaitForExit` | 12/09 | mesmos scripts (linha "quirk do PowerShell") | Quirk do PowerShell 5.1 | Documentado só em comentário |
| 5 | **Um Unity por vez** (trava de instância): raias paralelas não rodam o portão | Raias compilam fora do Unity (Roslyn) e testam num mini-runner Mono; o coordenador roda UM portão + UMA rodada de fotos | 11–12/09 | `ARQUITETURA.md` 25-32; `PROJETO.md` 300-309 | Trava do projeto Unity | `foto.ps1` e `portao.ps1` também não podem rodar juntos; build serializa tudo |
| 6 | **Unity reinicia o servidor adb e corta o `logcat`** | Ler com `adb logcat -d` depois | 21/09 | `LICOES.md` 21/09 | — | Perde-se o streaming ao vivo durante o build |
| 7 | **`InitTestScene*` entrou no commit** (cena temporária do Test Framework) | `.gitignore`: `mobile-unity/Assets/InitTestScene*` | 12/09 (`f374bf3`) | `.gitignore` | Existe só enquanto um PlayMode roda | — |
| 8 | **`.utmp`** (temporário do build Android) no git | `.gitignore` | 12/09 (`f846ea9`) | `.gitignore` | Regenerável | — |
| 9 | **`BuildSummary.totalSize` não é o tamanho do APK** | Ler o `.Length` do arquivo | 08/09 (Limiar) | `LICOES.md` 08/09; `build_apk.ps1` 34 | — | — |
| 10 | **UAC não chega à sessão do agente** ("cancelado pelo usuário" imediato); `winget install Unity.UnityHub` instala MSIX em WindowsApps | Instalador elevado vira `.bat` para o usuário; procurar Hub com `Get-AppxPackage`; baixar editor direto do `download.unity3d.com` via API pública de releases | 08/09 e 09/09 | `LICOES.md` 08/09 e 09/09 | — | **Qualquer instalação de SDK/AVD que peça admin terá o mesmo bloqueio** |
| 11 | **APK espalhado por worktrees** | Cópia datada sempre em `mobile-unity/Builds/testes/` do clone principal (ordem do Diretor 27/08) | 27/08 (`80fb42e`), refeito no Unity | `build_apk.ps1` 27-34 | Uma pasta só para o Diretor achar | — |
| 12 | **`*.apk` / `*.aab` no git** | `.gitignore` | 25/08 em diante | `.gitignore` | Artefato local | — |
| 13 | **Ateliê 3D vazando no APK** (54 → 106 MB sem nada acusar; Godot) | Portão do build reprova se o ateliê entrar | 25/08 | `CHANGELOG.md` 25/08 | Provar dentro do artefato, não no código | Era Godot; no Unity o equivalente é `importar_mago.ps1` deixar metallic/roughness fora de `Resources/` |
| 14 | **LFS para 160 referências de arte** (custo de banda) | Aceito: ~350–470 MB por clone limpo, cota GitHub 1 GB/mês ≈ 2–3 clones/mês | 26/08 (`e7fcc64`) | `.gitignore` comentário; `PROJETO.md` ~1412-1420 | Existiam num lugar só, sem cópia | **Clone novo consome cota**; sem terceira saída além de pacote pago |
| 15 | **Nunca editar `.cs` com o Unity rodando** (portão repetido) | Regra de trabalho | 16/09 | `PROJETO.md` 141 | — | — |
| 16 | **Bancada sem segurar o time mediu a tela de fim** | Anotado como falha; correção não descrita | 21/09 | `LICOES.md` 21/09 | — | Motivo não identificado no repositório de como ficou |

### 5.2 Problemas do aparelho / Android

| # | Problema original | Como foi resolvido | Quando | Onde | Por quê | Limitações |
|---|---|---|---|---|---|---|
| 17 | **MIUI recusa `adb shell input`** (`INJECT_EVENTS`), `install -g` e instalação sem "Instalar via USB" | Jogo ganhou extras de intent (`arkana_auto`, `arkana_fps`) e jogador automático; em 21/09 descobriu-se que "Depuração USB (configurações de segurança)" libera o `input` | 12/09 e 21/09 | `PartidaPeloAdb.cs`; `PROJETO.md` 355-365, 70, 77 | Testar sem dedo | Depende de opção MIUI; num AVD não existe essa recusa |
| 18 | **`targetFrameRate = -1` é 30 FPS no Android**, não "sem teto" | Pedir 300 via `--ei arkana_fps 300`; Config oferece opções | 12/09 | `Main.cs:298`; `Config.cs:180` | Padrão do Unity | Tela em 60 Hz segura em 60 (vsync) |
| 19 | **Jogador automático não saltava** e o castelo o empurrava no mar no fim da rota | Salta a 45% da rota | 12/09 | `PROJETO.md` ~366 | — | Ver problema aberto "salto no começo da rota cai no mar" |
| 20 | **60 FPS (12/09) → 16 FPS (21/09)**: ondas 5–17 nunca medidas | Bancada mediu; SSAO desligado (`URP_Base_Renderer`), sombra macia BAIXA; Qualidade real | 21/09 (`9749474`) | `PROJETO.md` 56-62 | Regra de 25/08: medir; o Diretor tinha mandado ignorar FPS e a medição contradisse | 34,6 FPS na Média, não 60; aparelho a 47 °C |
| 21 | **Opção Qualidade não fazia nada** (3 níveis com o mesmo asset) | Alta = asset; Média = 85% + 2×2048; Baixa = 70% | 21/09 | `Config.cs`; `PROJETO.md` 60-63 | — | **No editor o perfil não se aplica** (fotos saem na Alta) |
| 22 | **Boot com dois engasgos de 6 s** (montar ilha e arena) | Tela de carregamento monta em fatias de 12 ms (12B) | 13/09 | `TelaDeCarregamento`; `PROJETO.md` 177 | — | `Ilha.MontarEmFatias` e `ShaderVariantCollection` ainda na fila (linha 221) |

### 5.3 Problemas de conteúdo/render que só apareceram no APK ou na foto

| # | Problema original | Como foi resolvido | Quando | Onde | Por quê | Limitações |
|---|---|---|---|---|---|---|
| 23 | **Keyword `_EMISSION` some no APK** com "Strip Unused" (ligada só em runtime) | Molde-asset `ArkanaMeshyBrilhoInstancing.mat` (Lit + instancing + `_EMISSION`); teste `MoldeDoBrilho_AssetComEmissaoEInstancing` | 16/09 (`55dc82d`) | `PROJETO.md` 139, 141 | O build só leva variante referenciada por asset | Vale para toda keyword nova |
| 24 | `_NORMALMAP` ligado em runtime some no APK | Ligar na importação (`ImportacaoArkana`) | 12/09 | `PROJETO.md` tabela da leva 4 | idem | idem |
| 25 | **`Shader.Find` devolve null no aparelho** (mago rosa/invisível) | `m_AlwaysIncludedShaders` preenchido em `Build.ApplySettings` | 09–11/09 | `Build.cs` 58-93 | Material criado em runtime não conta como referência | Quem usar `Shader.Find` novo precisa acrescentar o nome |
| 26 | **`Logo` invisível** (Graphic do UGUI 2 nasce sem `CanvasRenderer`) | `[RequireComponent(typeof(CanvasRenderer))]` | 16/09 (`ae92b02`) | `Scripts/Menu/Logo.cs` | — | — |
| 27 | **lossyScale 100 do rig Meshy**: luva de 10–17 m cobrindo a câmera; Pyra com 1 cm | Soquete que cancela a escala; altura pela malha deformada (`BakeMesh`) | 12/09 e 16/09 | `LuvaVisual`; `CharactersMagoExternoTests` | A Armature do FBX da Meshy tem escala 100 | — |
| 28 | Sombra macia desligada no asset URP; depois cara demais | Ligada (5C, 12/09); baixada para BAIXA (21/09) | 12/09 → 21/09 | `URP_Base(_Renderer).asset`; `Sol` | Medição | — |
| 29 | **Gelo não sustentava** (corpo nadava por baixo) | Colisor por célula congelada com topo na lâmina | 21/09 (`b9831a0`) | `VisualDoTerreno` | GDD §14 "lago congela e vira rota" | Visual ainda indistinguível da água (aberto) |
| 30 | Lista de Configurações 100 px mais larga que a máscara (`sizeDelta` padrão) | Zerar `sizeDelta` | 16/09 (`8921aa1`) | `PROJETO.md` 136, 141 | RectTransform nasce 100×100 | — |
| 31 | Terreno reativo só existia nos testes (nenhuma cena o criava) | `Main.Montar` cria o behaviour | 11/09 | `PROJETO.md` 330 | Achado por foto | — |
| 32 | Casco convexo do PhysX estoura em 255 faces | `MeshCollider` com malha real | 12/09 | leva 4 | — | — |
| 33 | Nuvens cortadas em linhas retas (hash `frac(sin)`) | Hash sem seno (Hoskins) | 12/09 | shaders | precisão em floats grandes | — |
| 34 | Metal da Meshy (`metallicFactor` 1) espelha o zênite | `KitCenario.Domado` em todo modelo | 13/09 | `KitCenario` | Sem reflection probe, metal = breu/azul | Todo modelo novo precisa passar |
| 35 | **FileProvider para compartilhar o Selo do Campeão** | **NÃO feito** — registrado como pendência | 21/09 | `PROJETO.md` 86; `SeloDoCampeao.cs` | — | Botão compartilhar não funciona de verdade |

### 5.4 Histórico Godot/Unreal (referência; não afeta o Unity diretamente)

| Problema | Solução | Fonte |
|---|---|---|
| Modelo glTF entra virado (+Z) e animação sem laço ("patina") | meia-volta + `LOOP_LINEAR` | `LICOES.md` 25/08; `CHANGELOG.md` 26/08 |
| 18 magos da API "animam como robô"; clipes duplicados | Elenco refeito no SITE, um por um; `meshy.py` recusa personagem pela API | `PROJETO.md` seção "leva 7 desfeita" |
| Centro de ilha lido de paisagem antiga deslocou 1.200 m (Unreal) | "medida de ontem não mede o mundo de hoje" — colisor traçado agora | `PROJETO.md` Lições do desvio |
| Godot headless não devolve leitura de MultiMesh (falso verde) | prova por estado consultável do dono | `LICOES.md` R19 |

---

## 6. Problemas em aberto e limitações atuais admitidas pelos docs

Formato: **Problema / Evidência / Impacto / Causa provável / Existe solução atual? / Risco.**

| # | Problema | Evidência | Impacto | Causa provável | Solução atual? | Risco |
|---|---|---|---|---|---|---|
| A1 | **Rede inexistente** | `README.md` Estado; `infra/00-LEIA.md` "não tem UMA linha de rede"; `PROJETO.md` passo F | G5 não começou; nada multiplayer testável | Decisão: solo contra bots primeiro | Não; Netcode for GameObjects + servidor dedicado é intenção | Alto para o produto; **zero para o emulador** (nada depende de servidor) |
| A2 | **FPS 34,6 na Média** (47 °C), não 60 | `PROJETO.md` 59-62 | Aparelho de alto desempenho a 34 FPS; jogador médio pior | Escala/sombra/SSAO; GPU | Bancada + Qualidade | Médio; o aparelho ≠ jogador final |
| A3 | **Aparelho do Diretor ≠ jogador final** | `PROJETO.md` 41-42 ("volta a importar em G6") | Sem régua de aparelho intermediário | Só um aparelho de teste | Não | Alto em G6 |
| A4 | **Medição depende de cabo USB e do celular estar disponível** ("quando o celular voltar") | `PROJETO.md` 44, 93, 143, 213; `mobile-godot/00-LEIA.md` 44 | Três semanas sem medir no Godot; 12→21/09 sem medir no Unity | Aparelho é do Diretor, não fica na máquina | Não | **Alto — é o problema que um emulador atacaria** |
| A5 | **Sem CI** | `servidor-03-pipeline-build.md` "CI passa a valer em G5"; CI do Godot removido em 09/09 | Portão só local, um Unity por vez | Decisão de fase | Não | Médio |
| A6 | **Sem crash report / telemetria no Unity** | `infra/02-SERVIDORES.md` §4 (degrau 3); critério de crash = "logcat sem exceção" | Crash em aparelho do Diretor só se ele contar | Fase | Não | Médio |
| A7 | **Laje de gelo indistinguível da água** (foto 59) | `PROJETO.md` 67-68 | Jogador "anda sobre a água" | Visual do `VisualDoTerreno` | Não | Baixo |
| A8 | **Marca do parceiro cobre a mira** (no castelo) | `PROJETO.md` 71 | Legibilidade | HUD | Não (18D corrigiu só a marca sobre o botão SUPREMA) | Baixo |
| A9 | **Salto no começo da rota cai no mar longe da ilha** | `PROJETO.md` 72 | Partida perdida | Rota do castelo | "decidir se o salto espera terra" | Baixo |
| A10 | **Presságios sem spec** (GDD só 3 exemplos) | `PROJETO.md` 93, 113 | Bloqueia a feature | Regra `design/00-LEIA.md`: spec antes | Não | Baixo |
| A11 | **Bots sem kit** | `PROJETO.md` 93; "Decisões que esperam o Diretor" nº 2 | Bots menos perigosos | Decisão do Diretor | Uma linha no `KitRunner` | Baixo |
| A12 | **Compartilhar sem FileProvider** | `PROJETO.md` 86 | Botão do Selo não compartilha | Manifesto Android | Não | Baixo |
| A13 | **Perfil de Qualidade não se aplica no editor** (fotos saem na Alta) | `PROJETO.md` 63 | Foto do PC ≠ o que o aparelho mostra na Média | O asset é o arquivo do projeto | Não | Médio para julgar visual no PC |
| A14 | **Tela do Poco em 60 Hz durante a medição de 12/09** | `PROJETO.md` 297 | A folga real (120 Hz) nunca foi vista | vsync | Passo E "tela em 120 Hz" na fila | Baixo |
| A15 | **Boot: `Ilha.MontarEmFatias` e aquecer shaders (`ShaderVariantCollection`)** na fila | `PROJETO.md` 221 | engasgos no aparelho | — | Parcial (12B) | Baixo |
| A16 | `Interromper()` do baú e `RegredirNivel()` da Vitalidade anotados, não feitos | `PROJETO.md` 221 | — | — | Não | Baixo |
| A17 | **Números da Sintonia não validados** (variante A do Roblox) | `PROJETO.md` 104; `PONTE.md` §4 nº 1 | Pilar do jogo sem playtest humano | Nunca houve playtest | Não | Alto para produto |
| A18 | **Playtest humano nunca aconteceu** (Roblox e Unity) | `PONTE.md` §0; `PROJETO.md` Roblox | V1–V5 sem resposta | Login/publicação são ato do Diretor | Não | Alto para produto |
| A19 | **Publicação**: sem keystore de release, versionCode, AAB, política de privacidade, IARC, teste fechado | `ANDROID.md` 30-43; `servidor-04-distribuicao.md` | G6 bloqueada | Ato do Diretor | Não | Alto em G6 |
| A20 | **Gesto de disparo** (GDD §19.3 vs R17 do Godot) — decisão pendente | "Decisões que esperam o Diretor" nº 1 | Controle pode mudar | — | `GestoLogica` isolado | Baixo |
| A21 | `48-capim-duna-lod1.glb` com 1,28 K tris; 1 tronco encostando na rocha (KIT x MATA) | `PROJETO.md` 143, 107 | Custo/visual | — | Não | Baixo |
| A22 | **Portão/foto/build serializados por um Unity só** | `ARQUITETURA.md` 25-32 | Ciclo lento (~15 min de fotos) | Trava de instância | Filtro de foto | Médio |
| A23 | **Cota LFS** (~2–3 clones/mês) | `PROJETO.md` ~1418 | Clone novo pode estourar banda | LFS gratuito | Não | Médio para qualquer ambiente novo que clone o repo |
| A24 | **Instalação elevada não funciona pelo agente** (UAC) | `LICOES.md` 08/09, 09/09 | SDK/AVD/HAXM/Hyper-V pedem admin | Sessão sem elevação | `.bat` para o usuário | **Alto para montar emulador pelo agente** |

**Os três abertos mais relevantes para um emulador:** A4 (medição presa ao cabo e ao celular do Diretor), A24 (UAC não chega ao agente — qualquer instalação de SDK/AVD/hipervisor vira `.bat`), e a soma A2+A13 (FPS é medição de GPU Adreno e o perfil Média não roda no editor — um emulador com GPU de PC não substitui essa régua, mas cobre crash/fluxo/Android-isms).

---

## 7. Decisões técnicas

Formato: **Decisão / Motivo / Alternativas consideradas / Consequência.**

| # | Decisão | Motivo (como os docs dizem) | Alternativas consideradas | Consequência |
|---|---|---|---|---|
| D1 | **Unity 6 e só Unity** (09/09) — "não pretendo mudar mais" | Uma pessoa, dois jogos (Arkana e Limiar), uma engine; "uma pessoa, duas engines" foi o custo real do desvio | Godot 4.4 (produto 19/08–27/08), Unreal 5/Steam (27/08–09/09), ambos descartados | `mobile-godot/` vira referência; Unreal apagado; **nunca propor Unreal/Steam/Godot de novo** (`_memoria/arkana.md`) |
| D2 | **Android como plataforma; celular** | Ordem do Diretor 09/09; o jogo já era jogável no Android em Godot | PC/Steam (desfeito) | iOS "só quando existir um Mac" (`servidor-04`) |
| D3 | **Poco F4 como régua** (25/08) | É o aparelho do Diretor; "não é aparelho de entrada"; a régua pode ser mais generosa que TECH_ART | "aparelho intermediário genérico" (era chute) | O aparelho do Diretor ≠ jogador final (volta em G6) |
| D4 | **"Tamanho do APK não importa hoje"** (25/08) | Diretriz do Diretor: validar com o real; compressão na fase de loja | Otimizar peso cedo | APK 141 → 176 → 221 MB; **o que continua valendo é FPS medido** |
| D5 | **Lógica pura + `MonoBehaviour` como casca** | Testável sem cena; portão headless em minutos | Motivo não identificado no repositório para alternativas | 620 testes headless |
| D6 | **Teste provado em vermelho** | "Se não fica vermelho, é decorativo" — cicatriz de R9/R14 | — | 105–140 mutantes por onda |
| D7 | **Portão headless em `-batchmode`** antes de qualquer entrega | `swarm.yaml` regra 4 | — | Exige Hub aberto (licença) |
| D8 | **Fotos como critério visual** (11/09) | "teste verde não diz se o mago saiu rosa"; o Godot fazia igual (`_shot.gd`) | Só teste | `foto.ps1`, 71 fotos, `diag.txt` |
| D9 | **Um assembly (`Arkana.asmdef`)** + `Arkana.Tests` + `Arkana.Editor` | `ARQUITETURA.md` 9-11 | Motivo não identificado no repositório | Compila de uma vez |
| D10 | **`design/` como fonte da verdade** | Decisão não tem engine; "muda aqui primeiro, implementa depois" | — | Presságios esperam spec |
| D11 | **GDScript não se traduz** para C# | "As implementações nunca cruzam código" (27/08, mantido 09/09) | Traduzir | Reescrita completa em 2 dias (11–12/09) |
| D12 | **LFS para arte** (26/08) | 160 referências existiam num lugar só | Storage externo; pacote pago | Cota de banda ~2–3 clones/mês |
| D13 | **APK datado no clone principal** (27/08) | Ordem do Diretor: uma pasta só | — | `build_apk.ps1` resolve `git-common-dir` |
| D14 | **Meshy pelo SITE para personagem; API só para props** | Rig da API pula a marcação de articulações → "robô" | API (18 magos perdidos, ~670 créditos) | `meshy.py` recusa personagem |
| D15 | **Áudio sintetizado** (48 timbres, zero arquivo) | Motivo não identificado no repositório (o Diretor pediu "sons com profundidade" em 26/08) | Arquivos de áudio | Pendência de áudio real |
| D16 | **Sem pacote novo sem pedir** | `ARQUITETURA.md` 5; `servidor-06` "não instalar engines extras/SDKs sem fase ativa" | — | **Instalar SDK/AVD/pacote é decisão que precisa ser pedida** |
| D17 | **Medir FPS antes de otimizar** ("FPS se mede, não se estima") | Diretriz 25/08; lição do desvio: "o portão é a medição pendente mais antiga" | Estimar por disco/tris | Bancada de cortes |
| D18 | **SSAO off + sombra macia baixa** (21/09) | Bancada: ~12 ms + ~10 ms; a dura mede igual à baixa | Manter alta | 16 → 34 FPS |
| D19 | **Qualidade Média padrão** (85% + 2×2048), vetável | 34,6 FPS vs ~25 na Alta | Alta padrão | Não se aplica no editor |
| D20 | **Modo DUPLA padrão**, vetável | GDD §9: solo pareado com parceiro bot | SOLO padrão | — |
| D21 | **Bots por padrão, partida nunca espera; solo/PvE vale por si** | Análise de mercado: BR de 60 sem base é a aposta mais arriscada; Spellbreak morreu por retenção | BR 60 global | Lançar sem rede é opção |
| D22 | **Rede: Netcode for GameObjects + servidor dedicado em contêiner, sem amarrar a fornecedor** | Hathora e Multiplay fecharam em 2026 | Host-cliente no degrau 2 (`01-MULTIJOGADOR.md`) | Intenção; nada escrito |
| D23 | **Contas/loja/servidor: nada se contrata antes da fase** | `servidor-00-LEIA.md` | — | G5/G6 são ato do Diretor |
| D24 | **IL2CPP + ARM64 somente, minSdk 26, targetSdk 35, Linear** | Receita herdada do Limiar (08/09) | Motivo não identificado no repositório para excluir x86_64 | APK não roda em AVD x86 sem tradução ARM |
| D25 | **Espaço de cor Linear** | Números de cor calibrados no Godot; em Gamma o toon saía saturado | Gamma | — |
| D26 | **Package `br.com.vstack.arkana`** | Em uso; mudar é decisão do Diretor | — | — |
| D27 | **Regra permanente do repositório** (01/09): sem pastas novas, `main` por ff-only, nunca reescrever histórico | `CLAUDE.md` | — | Vale para qualquer script/pasta de emulador |
| D28 | **Nunca propor Unreal/Steam/Godot de novo** | `_memoria/arkana.md`; memória `arkana-engine-unity` | — | — |

---

## 8. O que os docs dizem sobre emulador / ambiente Android

**Resultado do grep** (`emulador|emulator|AVD|Android Studio|scrcpy|WSA|BlueStacks|Genymotion|x86_64|virtual device`) em `design/`, `mobile-unity/*.md`, `mobile-unity/*.ps1`, `mobile-godot/*.md`, `README.md`, `CHANGELOG.md` e `_memoria/LICOES.md`: **zero ocorrências.**

O que existe de mais próximo:

| Fonte | O que diz | Leitura |
|---|---|---|
| `design/infra/servidor-06-ferramentas-locais.md` (20/08, Godot) | Android SDK esperado em `%LOCALAPPDATA%/Android/Sdk` (ausente na verificação); JDK 21; "**WSL2 ou Docker: somente em G5**"; "**Não instalar engines extras, SDKs de anúncios ou ferramentas de servidor sem uma fase ativa que justifique**" | A única menção a ambiente Android fora do aparelho é contrária a instalar coisa sem fase |
| `design/pipeline/ANDROID.md` | "O Unity está instalado com o módulo Android (SDK, NDK e JDK próprios)" | O SDK usado é o **embutido no Unity**, não um Android Studio |
| `mobile-unity/ARQUITETURA.md` 5 | "Sem pacote novo sem pedir" | — |
| `mobile-unity/foto.ps1` | Fotos com GPU no PC "na proporção e densidade do Poco F4" | É o **substituto atual do aparelho** para julgar visual; não roda o APK, roda o editor |
| `PROJETO.md` 32-35, 77 | "FPS se mede no aparelho"; gargalo GPU | Um emulador não mede a GPU alvo |
| `PROJETO.md` 355-361, 70 | Recusas do MIUI (`INJECT_EVENTS`, `install -g`) | Peculiaridades que **não existem** num AVD — a automação por intent extra é agnóstica |
| `Build.cs:44` | `AndroidArchitecture.ARM64` | APK ARM64-only |
| `LICOES.md` 08/09 | UAC não chega ao agente; Hub MSIX não roda por linha de comando | Instalação de hipervisor/SDK/AVD pelo agente esbarra nisso |

**Conclusão documental:** o projeto nunca considerou, rejeitou ou planejou emulador; toda a validação Android foi projetada em torno do aparelho físico via USB, e o problema recorrente registrado é a **indisponibilidade do aparelho**, não a falta de ferramenta.

---

## 9. Regras de trabalho vigentes que afetam a próxima etapa

| Regra | Fonte | Efeito sobre um emulador |
|---|---|---|
| **Jamais criar pastas novas** (só necessidade real e clara) | `CLAUDE.md` | Scripts/configs de AVD teriam de morar em pasta existente (`mobile-unity/` ao lado dos `.ps1`, ou fora do repo) |
| **Trabalhar na pasta central, branch `main`; nada de worktrees aninhados** | `CLAUDE.md` | Esta auditoria já está num worktree; o resultado volta por ff |
| **`main` só por fast-forward; nunca reescrever histórico** | `CLAUDE.md` | — |
| **Regeneráveis podem ser apagados** (`Library`, `Builds`, `Logs`, `.utmp`) | `CLAUDE.md`; `.gitignore` | AVD/imagens de sistema seriam regeneráveis, mas ocupam dezenas de GB |
| **APK nunca no chat**; o caminho só se menciona quando muda | memória `apk-nunca-no-chat` | — |
| **Responder em pt-BR** | memória | — |
| **O Diretor julga pelo visual, com foto do aparelho** | memória `diretor-julga-pelo-visual`; `PROJETO.md` 316-326 | Screenshot de emulador não é "foto do aparelho" |
| **Validar com evidência, não concordar** | memória `validar-nao-concordar` | A recomendação sobre emulador precisa vir com o que ele mede e o que não mede |
| **Medir antes de otimizar; FPS se mede no aparelho** | `PROJETO.md` 32-35; D17 | Emulador não substitui a régua de FPS |
| **Sem pacote novo sem pedir; sem SDK/ferramenta sem fase ativa** | `ARQUITETURA.md`; `servidor-06` | Instalar Android Studio/AVD/hipervisor é pedido ao Diretor |
| **Toda onda visual termina com rodada no aparelho quando houver cabo** | `LICOES.md` 21/09 | O emulador seria o "quando não houver cabo" |
| **Um Unity por vez** | `00-LEIA.md`; `ARQUITETURA.md` | Build para emulador concorre com portão/fotos |
| **Nunca editar `.cs` com o Unity rodando** | `PROJETO.md` 141 | — |
| **Teste provado em vermelho; portão verde antes de entregar** | `swarm.yaml`; `PROJETO.md` 46-48 | — |
| **Toda afirmação sobre artefato (tamanho, data) se confere no disco** | `LICOES.md` 19/08 R12 | — |
| **Instalador elevado vira `.bat` para o usuário** | `LICOES.md` 08/09 | Qualquer passo admin do emulador é do Diretor |
| **Fecho de fase atualiza `PROJETO.md` e a seção CONTINUAR DAQUI** | `PROJETO.md` 9-13; memória `fecho-de-fase` | Se o emulador entrar, precisa ir para o PROJETO.md |

---

## 10. Comandos executados e arquivos lidos

### 10.1 Comandos (todos somente leitura)

| Comando | Objetivo | Resultado |
|---|---|---|
| `git log --date=short --format="%h %ad %s" \| head -150` | Cronologia recente | 150 commits de 26/08 a 21/09 |
| `git log --date=short --format=... \| wc -l; ... \| tail -30` | Total e commits mais antigos | **231 commits**; primeiro `14e334e` 17/08 (v0.1 2D) |
| `git log --date=short --grep="adb\|APK\|apk\|FPS\|Poco\|build\|Gradle\|licen\|Hub" -i` | Commits ligados a build/aparelho | 120 linhas; sem commit citando Gradle/licença/Hub por nome (essas lições estão só nos `.ps1` e LICOES) |
| `git log --stat --date=short -- mobile-unity/*.ps1 mobile-unity/Assets/_Arkana/Editor/Build.cs` | Evolução do pipeline | 7 commits: `d327e75` (09/09, esqueleto) → `ab856d1`, `431f280`, `ba6839a`, `01402d7` (11/09) → `51d4802`, `5a8d43e` (12/09); os quirks de Hub/`WaitForExit` já estão em `d327e75`/12/09 |
| `git log --date=short -- .gitignore` | Quando `InitTestScene`/`.utmp` saíram | `f374bf3`, `f846ea9` (12/09) |
| `grep -rn -i "emulador\|emulator\|AVD\|Android Studio\|scrcpy\|WSA\|BlueStacks\|Genymotion\|x86_64\|virtual device"` em `design/`, `mobile-unity/*.md`, `*.ps1`, `mobile-godot/*.md`, `README.md`, `CHANGELOG.md`, `LICOES.md` | Menções a emulador | **Nenhuma** |
| `grep -rl "ARKANA FPS\|arkana_fps\|arkana_bancada\|targetFrameRate\|FileProvider"` em `mobile-unity/Assets` | Paths do medidor/bancada | `Scripts/MedidorDeFps.cs`, `PartidaPeloAdb.cs`, `BancadaDeCortes.cs`, `Main.cs:298`, `Menu/Config.cs:180`, `UI/SeloDoCampeao.cs` |
| `grep -n -i "Drive\|JOGOS EM DESENV\|adb devices\|logcat -d\|top -H\|FileProvider\|InitTestScene\|crash\|CI \|screencap" design/PROJETO.md` | Localizar linhas citadas | linhas 44, 77, 86, 129, 213, 365, 872, 1412, 1492, 1502 |
| `ls mobile-unity mobile-unity/Builds` ; `cat .gitignore` | Estrutura e ignorados | `Builds/` não existe no worktree (é do clone principal); `*.apk` ignorado |

### 10.2 Arquivos lidos (linhas aproximadas)

| Arquivo | Linhas | Como |
|---|---|---|
| `design/PROJETO.md` | 1.706 (inteiro, 4 blocos) | fonte principal |
| `design/PONTE.md` | 841 (1–420 inteiro; 421–841 por grep de termos técnicos) | decisões Roblox→GDD |
| `design/pipeline/ANDROID.md` | 43 | identidade do app, estado de publicação |
| `design/pipeline/00-LEIA_PRIMEIRO.md` | 83 | pipeline de arte (Godot-era) |
| `design/pipeline/01-DECISOES_E_FONTE_DA_VERDADE.md` | 105 | fonte da verdade |
| `design/infra/00-LEIA.md`, `01-MULTIJOGADOR.md`, `02-SERVIDORES.md` | 70 + 136 + 148 | rede = intenção futura |
| `design/infra/servidor-00-LEIA.md`, `-03-pipeline-build.md`, `-04-distribuicao.md`, `-06-ferramentas-locais.md` | 54 + 44 + 87 + 34 | fases G5/G6, CI, loja, ferramentas |
| `CHANGELOG.md`, `README.md`, `CLAUDE.md`, `swarm.yaml` | 100 + 69 + 24 + 39 | — |
| `mobile-unity/00-LEIA.md`, `ARQUITETURA.md` | 47 + 114 | como rodar, contratos |
| `mobile-unity/build_apk.ps1`, `portao.ps1`, `foto.ps1`, `importar_mago.ps1` | 38 + 55 + 41 + 22 | quirks do pipeline |
| `mobile-unity/Assets/_Arkana/Editor/Build.cs` | 124 | settings Android |
| `mobile-godot/00-LEIA.md`, `arte/00-LEIA.md` | 59 + 38 | orçamento medido; "adb devices em três semanas" |
| `.gitignore` | ~60 | — |
| `_memoria/LICOES.md` | só entradas ARKANA/Unity/APK/adb/Poco/Hub/licença (~200 linhas filtradas) | lições de 17/08 a 21/09 |
| `_memoria/STATUS_arkana-onda17.md`, `arkana.md`, `STATUS_arkana-auditoria-android.md` (60 primeiras linhas) | 11 + 185 + 60 | — |

**Não lidos, de propósito:** `.env*`, keystores, senhas, `design/infra/03-CONTAS-E-CUSTOS.md`, `04-DADOS-E-MENORES.md`, `servidor-01/02/05` (fora do escopo pedido; não citados aqui).


---

# ANEXO D — GIT, MAPA DO PROJETO E ARTEFATOS (relatório da raia code-reviewer)


**Data:** 2026-09-23 · **Agente:** code-reviewer (worker da auditoria técnica)
**Aviso:** auditoria **somente leitura** — nada foi apagado, alterado, commitado, enviado ou construído. Nenhum `.env`/keystore foi aberto (só caminho e tamanho). O APK foi inspecionado com `unzip -l`, `aapt2 dump badging` e `apksigner verify --print-certs` (só metadados e certificado público).

**Alvos:** clone principal `C:\Users\VINICIUS\Videos\MEUS PROJETOS\ARKANA\` (`main`) e worktree `...\ARKANA\.claude\worktrees\auditoria-tecnica-completa-282af1\` (`claude/auditoria-tecnica-completa-282af1`). Os dois apontam para o mesmo commit `0a94c2f`.

---

## 1. Estado do Git

### 1.1 Resumo

| Item | Clone principal (`main`) | Worktree (`claude/auditoria-…`) |
|---|---|---|
| HEAD | `0a94c2f71402…` — 2026-09-21 14:54 -0300 — "docs: PROJETO — FPS de 16 para 34 no Poco F4…" | mesmo commit |
| Árvore de trabalho | **limpa** (`git status --porcelain` vazio; só ignorados) | limpa (a saída da auditoria foi consolidada neste arquivo único, na raiz do clone principal) |
| Relação com `origin/main` | **ahead 93** — `main` tem 231 commits, `origin/main` tem 138 | idem (mesmo commit) |
| Último commit no GitHub | `0694794` de **2026-09-09** ("chore: originais do Meshy moram em arte/cenario/*/origem/") | — |
| Branches não mescladas em `main` | **nenhuma** (`git branch -a --no-merged main` vazio) | — |
| Tags | nenhuma | — |
| Stash | vazio | — |
| Remote | `origin https://github.com/Vvs2705/arkana.git` (sem token na URL) | — |
| Worktrees | 2: raiz (`main`) e `.claude/worktrees/auditoria-tecnica-completa-282af1` | — |

**O que os 93 commits não enviados contêm:** `git diff --shortstat origin/main main` = **738 arquivos, +78.881 linhas** — é a reescrita inteira em Unity (12/09 a 21/09) mais **134 arquivos LFS novos (~504 MB)**: os 20 FBX dos magos, os `.glb` de `Resources/`, retratos, texturas. Tudo isso existe **só neste disco** (mais o cache local em `.git/lfs`). Ver §6, achado P0.

### 1.2 Histórico

| Métrica | Valor |
|---|---|
| Primeiro commit | `14e334e` 2026-08-17 "Arkana v0.1 — protótipo 'Campo de Provas' (Fase 1 completa)" (era Roblox/Capacitor) |
| Total de commits em `main` | 231 (131 em ago/2026, 100 em set/2026) |
| Autores | Vinicius (225), Vvs2705 (6) — mesma pessoa, duas identidades git |
| Frequência em set/2026 | 01: 2 · 04: 3 · 09: 3 · 11: 4 · **12: 50** · 13: 11 · 16: 13 · 19: 5 · 21: 9 |
| Convenção de mensagem | Conventional-commits em pt-BR: `feat(unity)` 45, `feat` 34, `docs` 34, `chore(unity)` 21, `fix(unity)` 12, `fix` 11, `art` 4, `refactor` 2, `ci` 2, `chore` 2, `perf(unity)` 1, `test(unity)` 1, `wip(unity)` 1, `revert` 1 … **58 sem prefixo** (quase todos da fase Roblox, agosto) |
| Padrão recorrente | 6 commits "chore(unity): Main.unity regravada pelo build do APK (mesmo conteúdo, ids novos)" — o build de APK reescreve a cena e gera ruído de commit |

Últimos 10 commits: `0a94c2f` docs PROJETO · `c8275bd` chore URP global · `9749474` perf 16→34 FPS · `b9831a0` fix gelo · `789bc46` docs medição · `58a346b` chore Main.unity · `d90c23a` feat onda 18 · `ac327d6` chore Main.unity · `2d8510f` feat bancada de cortes · `5978bf5` docs.

### 1.3 Objetos, LFS e hooks

| Item | Valor |
|---|---|
| `git count-objects -vH` | 930 soltos (3,30 MiB); 4.129 em 3 packs (237,12 MiB); **garbage: 12 `.idx` sem `.pack`** (193 KiB) — 13 `.idx` órfãos em `.git/objects/pack/` (resto de `git gc`/`repack` interrompido; inofensivo, mas gera warning a cada comando) |
| `.git/` total | **1,8 GB** — `.git/lfs` 1,6 GB (cache LFS com versões antigas), `.git/objects` 243 MB |
| Git LFS | git-lfs 3.7.1; endpoint `https://github.com/Vvs2705/arkana.git/info/lfs` (auth basic); **392 arquivos, 1.055 MB**: 310 `.png`, 62 `.glb`, 20 `.fbx`; 0 ponteiros sem objeto local |
| Maiores LFS | 20 FBX dos magos 10–13 MB cada (`Resources/magos/*.fbx`, ~230 MB); `castelo.glb` 11 MB (3 cópias: `arte/cenario/_glb`, `mobile-godot/.../world/modelos`, `Resources/`) |
| Hooks customizados | `post-checkout`, `post-commit`, `post-merge`, `pre-push` — todos são os hooks **padrão do git-lfs** (`git lfs install`), nada próprio |
| CI | **nenhum**: sem `.github/`, `.gitlab-ci.yml`, `Jenkinsfile`, `.pre-commit-config.yaml` (os 2 commits `ci:` são de agosto, da fase Roblox) |

### 1.4 `.gitattributes` (resumo das regras LFS)

- `arte/**/*.{glb,fbx}`, `arte/personagens/**/_originais/*.png`, `arte/cenario/**/*.png` → LFS
- `mobile-unity/**/*.{fbx,glb,png,wav,ogg}` → LFS; `*.unity/*.prefab/*.asset/*.mat/*.meta` → `text eol=lf`
- `mobile-godot/godot/{characters,world,gameplay}/modelos/**/*.glb` e `characters/modelos/*.{png,ktx2}` → LFS
- **Sem regra para `.jpg`** — ver §6 (15 texturas JPG de 2–4 MB do Godot são blobs normais)
- Comentários no arquivo documentam a decisão do Diretor (26/08) e o custo de banda (cota grátis 1 GB/mês ≈ 2 clones limpos)

### 1.5 `.gitignore` (resumo)

Ignora: `mobile-unity/{Library,Temp,Obj,Build,Builds,Logs,UserSettings,.utmp}`, `*.csproj/*.sln/.vs`, `Assets/InitTestScene*`, `*.apk`, `*.aab`, `mobile-godot/godot/{.godot,build}`, `roblox/build`, `personagens/**/*.zip`, logs/caches, editores, SO, **segredos** (`.env`, `.env.*` exceto `.env.example`, `*.pem`, `*.key`, `*.keystore`, `*.jks`, `keystore.properties`), `mobile-godot/godot/characters/modelos/*/`, `arte/cenario/*/origem/`, `arte/cenario/ilha-fraturada/_originais-3d/*.glb`.

`git status --ignored` no clone principal confirma o que está fora do git: `.claude/worktrees/`, 5× `arte/cenario/*/origem/`, 37 `.glb` originais da ilha, `arte/tools/meshy/.env`, `mobile-unity/.utmp/`, `Assets/InitTestScene7691d2a3-….unity(.meta)`, `Builds/`, `Library/`, `Logs/`, `UserSettings/`.

---

## 2. Tamanho

### 2.1 Clone principal, 1º nível (`du -sh`)

| Pasta | Tamanho | Rastreado? |
|---|---|---|
| `mobile-unity/` | **16 GB** | parcialmente (Assets, Packages, ProjectSettings, 4 `.ps1`, 2 `.md`) |
| `.git/` | 1,8 GB | — (1,6 GB é cache LFS) |
| `arte/` | 1,6 GB | parcialmente (1,2 GB são originais Meshy ignorados) |
| `.claude/` | 1,1 GB | só `launch.json` e `scheduled_tasks.lock`; o resto é o worktree desta auditoria |
| `mobile-godot/` | 101 MB | sim (referência morta) |
| `design/` | 1,1 MB | sim |
| `roblox/` | 1,2 MB | sim |
| `CHANGELOG.md`, `CLAUDE.md`, `README.md`, `swarm.yaml` | 20 KB | sim |
| **Total aproximado** | **≈ 20,6 GB** no disco; ~1,3 GB para um clone limpo com LFS (243 MB objetos + 1.055 MB LFS) | |

### 2.2 `mobile-unity/` por subpasta

| Subpasta | Tamanho | Observação |
|---|---|---|
| `Library/` | **9,8 GB** | `Bee/` 7,2 GB (projeto Gradle/IL2CPP do Android), `PackageCache/` 1,3 GB, `Artifacts/` 824 MB, `PlayerDataCache/` 377 MB, `BurstCache/` 107 MB — tudo regenerável |
| `Builds/` | **5,4 GB** | `testes/` 5,2 GB (25 APKs), `arkana.apk` 228 MB, `build.log` 592 KB, `Arkana_BurstDebugInformation_DoNotShip/` 84 KB |
| `Assets/` | 481 MB | `_Arkana/Resources` 477 MB (FBX/GLB/PNG via LFS), `Scripts` 2,6 MB, `Tests` 1,3 MB, `Editor` 33 KB, `Settings` 18 KB, `Scenes` 13 KB |
| `Logs/` | 18 MB | logs de compilação (09–11/09), `diag-*.txt`, `carregamento-*.txt` de 21/09, `fotos/` |
| `.utmp/` | 2,3 MB | temporário do Unity (ignorado) |
| `ProjectSettings/` | 102 KB | 21 `.asset` + `ProjectVersion.txt` + `SceneTemplateSettings.json` |
| `Packages/` | 16 KB | `manifest.json` + `packages-lock.json` |
| `UserSettings/` | 5 KB | ignorado (correto) |

### 2.3 `arte/` e as maiores subpastas de `arte/cenario/`

`cenario/` 1,3 GB · `personagens/` 312 MB · `prompts/` 192 KB · `audio/` 172 KB · `tools/` 54 KB.
Em `cenario/`: `ilha-fraturada/` 515 MB · `03-luva-conjurador/` 188 MB · `01-castelo-voador/` 122 MB · `05-bau-celestial/` 116 MB · `04-manopla/` 109 MB · `02-luva-comum/` 104 MB · `_glb/` 45 MB · `06-ruinas/` 27 MB · `08-altar-sintonia/` 15 MB · `07-torre-arcana/` 11 MB. (A maior parte são os `origem/` e `_originais-3d/` ignorados.)

### 2.4 Contagens (arquivos **rastreados**, 1.554 no total)

| Tipo | Qtd | Tipo | Qtd |
|---|---|---|---|
| `.cs` | **189** (125 produção + 64 testes: 59 EditMode, 5 PlayMode) | `.md` | 134 |
| `.png` | 336 | `.glb` | 67 |
| `.gd` (Godot, referência) | 64 | `.jpg` | 47 |
| `.luau` (Roblox) | 40 | `.asset` | 24 |
| `.fbx` | 20 | `.shader` | 7 |
| `.py` | 6 | `.json` | 5 |
| `.ps1` | 4 | `.asmdef` | 4 |
| `.mat` | 4 | `.unity` | 1 |
| `.yaml` | 1 | `.prefab` / `.bat` | 0 |

Por pasta de 1º nível: `mobile-unity` 730 · `arte` 350 · `mobile-godot` 330 · `design` 93 · `roblox` 43 · `.claude` 2 · raiz 5.

`.cs` por pasta: `Tests/EditMode` 59 · `Scripts/Gameplay` 31 + `Gameplay/Habilidades` 26 · `Core` 17 · `UI` 16 · `World` 10 · `Menu` 10 · `Tests/PlayMode` 5 · `Characters` 4 · `Scripts/` (raiz) 4 · `Terrain` 3 · `Editor` 3 · `Audio` 1.

---

## 3. Mapa do projeto

Legenda: **[código]** C# do produto · **[gameplay]** · **[assets]** · **[android]** · **[build]** · **[scripts]** · **[config]** · **[testes]** · **[docs]** · **[infra]** · **[referência morta]** · **[ignorado]** fora do git.

```
ARKANA/
├── README.md, CLAUDE.md, CHANGELOG.md, swarm.yaml            [docs/config]
├── .gitattributes, .gitignore                                [config]
├── .claude/
│   ├── launch.json          "npm run dev" porta 5173 — RESTO DO CAPACITOR, não há package.json   [referência morta]
│   ├── scheduled_tasks.lock                                  [config]
│   └── worktrees/auditoria-tecnica-completa-282af1/          [ignorado] worktree desta auditoria (1,1 GB)
├── design/                  A DECISÃO (fonte da verdade)     [docs]
│   ├── 00-LEIA.md, PONTE.md (841 l), PROJETO.md (1.706 l — memória do projeto)
│   ├── gdd/        GDD.md (593 l), DANO, DIRECAO, ART, AUDIO, ITENS-EQUIPAVEIS, MANOPLAS-FUSAO, DESBLOQUEIO-ELENCO, ROADMAP_3D, PASSOS_GRAFICOS, COMO_JOGAR, CREDITS
│   ├── personagens/  00-LEIA + 20 fichas (01-pyra … 20-pip)
│   ├── cenario/      00-MODELO, 00-MODELOS-3D, 00-VALIDACAO, ILHA-FRATURADA-VALIDACAO, MAPA-GRANDE-PLANO
│   ├── infra/        00-LEIA, 01-MULTIJOGADOR, 02-SERVIDORES, 03-CONTAS-E-CUSTOS, 04-DADOS-E-MENORES, servidor-00…06   [infra]
│   ├── pipeline/     00-LEIA_PRIMEIRO, 01-DECISOES, 02-MESHY_CAPACIDADES, 03-ORCAMENTO, 04-PLANO_LOTES, ANDROID.md, MESHY.md, ROBLOX.md, FONTES_E_LINKS, INDEX
│   │   └── CENARIO/, MESHY/ (00-API_PLAYBOOK), PERSONAGENS/, TECH_ART/, TEMPLATES/ (ENVIRONMENT_PRODUCTION_CARD…)
│   └── referencias/  SPELLBREAK.md, ZONA-BATTLE-ROYALE.md
├── arte/                    A MATÉRIA-PRIMA                  [assets]
│   ├── 00-LEIA.md
│   ├── personagens/01-pyra … 20-pip/_originais/*.png   (160 PNG via LFS)
│   ├── cenario/  01-castelo-voador … 08-altar-sintonia, ilha-fraturada/, _glb/ (5 glb, ver §6), texturas/
│   │   └── */origem/, ilha-fraturada/_originais-3d/*.glb     [ignorado] originais Meshy (~1,2 GB)
│   ├── audio/vozes, prompts/
│   └── tools/  blender/{desneon,fundir_animacoes,otimizar}.py · meshy/{meshy.py,montar_glb.py,test_glb.py,.env.example}   [scripts]
│       └── meshy/.env                                        [ignorado] 56 bytes — NÃO aberto
├── mobile-unity/            O PRODUTO (Unity 6000.3.23f1)   [código/android]
│   ├── 00-LEIA.md, ARQUITETURA.md                            [docs]
│   ├── build_apk.ps1, portao.ps1, foto.ps1, importar_mago.ps1   [build/scripts]
│   ├── Assets/
│   │   ├── DefaultVolumeProfile.asset, UniversalRenderPipelineGlobalSettings.asset   [config]
│   │   ├── InitTestScene7691d2a3-….unity(.meta)              [ignorado] cena temporária do Test Framework
│   │   └── _Arkana/
│   │       ├── Editor/    Build.cs, MainSceneBuilder.cs, ImportacaoArkana.cs, Arkana.Editor.asmdef   [build]
│   │       ├── Scenes/    Main.unity (única cena no EditorBuildSettings)                              [config]
│   │       ├── Settings/  URP_Base.asset, URP_Base_Renderer.asset                                     [config]
│   │       ├── Resources/ 44 .glb de cenário, bau/castelo/3 luvas .glb, 7 .shader, 4 .mat, 2 png, Retratos/, magos/ (60 arq.: 20 fbx + 40 png)   [assets]
│   │       ├── Scripts/   Arkana.asmdef · Main.cs, MedidorDeFps.cs, PartidaPeloAdb.cs, BancadaDeCortes.cs   [código]
│   │       │   ├── Core/       Balance, Bus, Combat, Elemento, IEntidade, Kits(+GrupoA-D), Sintonia, Textos(+Grimorio/Ping/Selo), Velocidade, Vitalidade
│   │       │   ├── Gameplay/   Partida, Player, Bot, Pawn, Locomocao, Projetil, Arma, ArmaSlot, Loot, Zona, Agua, Queda, Derrubado, BauCelestial, KitRunner(+A-D), Grimorio, Efeitos, VisualDa*, CameraTerceiraPessoa, ChaoComObstaculos … + Habilidades/ (26)   [gameplay]
│   │       │   ├── World/      Ilha, Relevo, Castelo, Ruinas, Mata, Praia, Grama, Vegetacao, Sol, KitCenario
│   │       │   ├── Characters/ Mago, PoseMago, LuvaVisual, IdentidadeMago
│   │       │   ├── UI/         Hud, HudAviso, HudDupla, JoystickVirtual, GestoDeDisparo, Minimapa, MarcasDeAlvo, TelaDeCarregamento, SeloDoCampeao, AvisoGrimorio, FiltroDaltonismo …
│   │       │   ├── Menu/       Menu, Config, Elenco, SelecaoPersonagem, Logo, BotaoMenu, Estilo, Selo, TelaGrimorio, VitrineDoMenu
│   │       │   ├── Terrain/    TerrenoReativo, TerrenoReativoBehaviour, VisualDoTerreno
│   │       │   └── Audio/      Sfx
│   │       └── Tests/  EditMode/ (Arkana.Tests.asmdef, 58 arquivos) · PlayMode/ (Arkana.PlayTests.asmdef: BootTests, FotoTests, CarregamentoTests, AbateVisualTests, VooVisualTests)   [testes]
│   ├── Packages/  manifest.json, packages-lock.json          [config]
│   ├── ProjectSettings/  ProjectSettings.asset, ProjectVersion.txt, QualitySettings, GraphicsSettings, EditorBuildSettings, …   [config/android]
│   ├── Builds/                                               [ignorado][build] arkana.apk, build.log, testes/ (25 APKs), Arkana_BurstDebugInformation_DoNotShip/
│   ├── Library/ (9,8 GB), Logs/, UserSettings/, .utmp/       [ignorado] regeneráveis
├── mobile-godot/            Godot 4.4.1 — encerrado 09/09/2026   [referência morta]
│   ├── 00-LEIA.md ("É o que o Unity tem que alcançar")
│   └── godot/  project.godot, export_presets.cfg, ARQUITETURA.md, audio/ characters/ core/ gameplay/ juice/ menu/ terrain/ ui/ world/ tests/ export/ (64 .gd; .glb via LFS; 15 .jpg de textura como blob)
└── roblox/                  Campo de Provas (Rojo)           [referência morta]
    └── src/  default.project.json, src/{client 16, server 14, shared 6}, tools/ 6   (40 .luau)
```

---

## 4. APK / AAB existentes

### 4.1 Inventário

Nenhum `.aab` em lugar nenhum. Nenhum `.apk` fora de `mobile-unity/Builds/` (a busca excluiu `Library/`; dentro dela existe só o produto intermediário do Gradle, abaixo). Em `C:\Users\VINICIUS\Downloads\` há apenas 2 APKs de **outro projeto** (`limiar-greybox_2026-09-08_1507.apk` e `_1602.apk`, 32,9 MB cada) — nenhum APK do ARKANA fora do repositório.

| # | Caminho (relativo a `mobile-unity/Builds/`) | Tamanho | Data/hora |
|---|---|---|---|
| — | `arkana.apk` | 227,9 MB | 2026-09-21 14:47 |
| 1 | `testes/arkana-2026-09-12_0020.apk` | 175,7 MB | 09-12 00:20 |
| 2 | `testes/arkana-2026-09-12_0029.apk` | 175,7 MB | 09-12 00:29 |
| 3 | `testes/arkana-2026-09-12_0038.apk` | 175,7 MB | 09-12 00:29 |
| 4 | `testes/arkana-2026-09-12_0509.apk` | 244,0 MB | 09-12 05:09 |
| 5 | `testes/arkana-2026-09-12_0540.apk` | 266,0 MB | 09-12 05:39 |
| 6 | `testes/arkana-2026-09-12_0551.apk` | 266,0 MB | 09-12 05:51 |
| 7 | `testes/arkana-2026-09-12_1236.apk` | 178,5 MB | 09-12 12:36 |
| 8 | `testes/arkana-2026-09-12_1317.apk` | 179,1 MB | 09-12 13:17 |
| 9 | `testes/arkana-2026-09-12_1352.apk` | 179,1 MB | 09-12 13:52 |
| 10 | `testes/arkana-2026-09-12_1454.apk` | 179,1 MB | 09-12 14:54 |
| 11 | `testes/arkana-2026-09-12_1610.apk` | 181,2 MB | 09-12 16:10 |
| 12 | `testes/arkana-2026-09-12_1630.apk` | 181,3 MB | 09-12 16:29 |
| 13 | `testes/arkana-2026-09-12_1717.apk` | 196,0 MB | 09-12 17:17 |
| 14 | `testes/arkana-2026-09-12_1755.apk` | 196,1 MB | 09-12 17:55 |
| 15 | `testes/arkana-2026-09-12_2158.apk` | 196,4 MB | 09-12 21:58 |
| 16 | `testes/arkana-2026-09-13_0224.apk` | 214,1 MB | 09-13 02:24 |
| 17 | `testes/arkana-2026-09-13_0316.apk` | 220,5 MB | 09-13 03:16 |
| 18 | `testes/arkana-2026-09-16_1023.apk` | 221,1 MB | 09-16 10:23 |
| 19 | `testes/arkana-2026-09-16_1445.apk` | 228,2 MB | 09-16 14:45 |
| 20 | `testes/arkana-2026-09-19_1725.apk` | 228,3 MB | 09-19 17:25 |
| 21 | `testes/arkana-2026-09-21_1346.apk` | 228,3 MB | 09-21 13:45 |
| 22 | `testes/arkana-2026-09-21_1418.apk` | 228,4 MB | 09-21 14:18 |
| 23 | `testes/arkana-2026-09-21_1429.apk` | 228,4 MB | 09-21 14:29 |
| 24 | `testes/arkana-2026-09-21_1441.apk` | 227,9 MB | 09-21 14:41 |
| 25 | `testes/arkana-2026-09-21_1447.apk` | 227,9 MB | 09-21 14:47 |

Total em `Builds/testes/`: **5,2 GB** (26 APKs somando o `arkana.apk`: 5,4 GB). Tudo ignorado pelo git (`*.apk`).

**`arkana.apk` = `testes/arkana-2026-09-21_1447.apk` = `Library/Bee/Android/Prj/IL2CPP/Gradle/launcher/build/outputs/apk/release/launcher-release.apk`** — os três têm SHA-256 `c6b6b199424c898eadd3250cb32c814aceea20a305176c13d7dba90ed3957f23` (239.004.494 bytes). O `build_apk.ps1` gera em `Builds/arkana.apk` e copia com data para `testes/`.

### 4.2 O APK mais recente por dentro (`arkana.apk`, 2026-09-21 14:47)

| Campo | Valor | Fonte |
|---|---|---|
| Package | `br.com.vstack.arkana` | aapt2 badging |
| versionCode / versionName | **1 / 1.0** (nunca incrementado) | aapt2 |
| minSdk / targetSdk / compileSdk | **26 / 35 / 35** (Android 8.0 → 15) | aapt2 + `ProjectSettings.asset` |
| Label / activity | `Arkana` / `com.unity3d.player.UnityPlayerGameActivity` (alias `unityplayer.UnityActivity`) | aapt2 |
| ABIs (`lib/`) | **só `arm64-v8a`** (7 `.so`) — sem `armeabi-v7a`, sem `x86_64` | unzip -l |
| Backend | **IL2CPP** — `libil2cpp.so` 61,2 MB + `Managed/Metadata/global-metadata.dat` 7,6 MB; nenhum `libmono*.so` | unzip -l |
| Runtime | `libunity.so` 19,7 MB; **`libgame.so`** 0,1 MB + `libmain.so` → **entrada GameActivity** (`androidApplicationEntry: 2`); `libswappywrapper.so` (Frame Pacing); `lib_burst_generated.so` (Burst); `libc++_shared.so` | unzip -l |
| Assinatura | **v2 apenas** (v1/v3/v4 = false); 1 signatário; **`CN=Android Debug, O=Android, C=US`**, RSA 2048 → **certificado de DEBUG** (`~/.android/debug.keystore`, criado 2026-08-17; `androidUseCustomKeystore: 0`) | apksigner verify --print-certs |
| `keytool -printcert -jarfile` | "Não é um arquivo jar assinado" — esperado: não há assinatura v1/JAR, só v2 | keytool (OpenJDK do Unity) |
| Debuggable | **não** — `application-debuggable` ausente no badging; variante Gradle `release`; `extractNativeLibs=true` | aapt2 |
| Permissões | `INTERNET` e `br.com.vstack.arkana.DYNAMIC_RECEIVER_NOT_EXPORTED_PERMISSION` (padrão androidx) | aapt2 |
| Telas / densidades | small…xlarge; 160–640 + anydpi | aapt2 |
| Tamanho descomprimido | 476 MB em 593 entradas: `assets/` 386,6 MB (`assets/bin/Data` raiz 373,6 MB, `Managed` 7,9 MB, `Resources` 5,1 MB), `lib/` 82,8 MB, `classes.dex` 6,1 MB | unzip -l |
| Maior entrada | `assets/bin/Data/56b2e4048307a0c47b305cd0011bc1bf` 65,6 MB; depois 4 arquivos de ~8,2 MB e ~18 de 4,5–4,9 MB (os 20 magos em Resources) | unzip -l |
| Split/AAB | `androidSplitApplicationBinary: 0` — APK único, sem OBB | ProjectSettings |
| Build | Unity 6000.3.23f1 batchmode, Gradle 9.1.0 / AGP 9.0.0, `Arkana.EditorTools.Build.Android`; `build.log` termina "Exiting batchmode successfully… return code 0" | `Builds/build.log` |

**Consequência direta para o emulador:** o APK atual **não instala em AVD x86_64** (não tem `lib/x86_64`). Só serve emulador/imagem ARM64 (lento em host x86 sem virtualização ARM) ou um novo build com `AndroidTargetArchitectures` incluindo x86_64 (IL2CPP recompila tudo; `Library/Bee` de 7,2 GB cresce). Isso é decisão do agente de emulador, não desta auditoria.

---

## 5. Arquivos sensíveis / segredos

### 5.1 No disco (sem abrir)

| Caminho | Tamanho | Estado |
|---|---|---|
| `arte/tools/meshy/.env` | 56 bytes | **ignorado** pelo git (`.env`); confirmado `!!` no `git status --ignored` |
| `arte/tools/meshy/.env.example` | 183 bytes | versionado — modelo; única chave declarada: `MESHY_API_KEY=` (valor vazio/placeholder) |
| `C:\Users\VINICIUS\.android\debug.keystore` | 2.618 bytes (2026-08-17) | fora do repo; keystore de debug padrão do Android (senha pública `android`); é o que assina o APK |
| `*.keystore`, `*.jks`, `keystore.properties`, `*.pem`, `*.key`, `*.p12`, `credentials*`, `secrets*`, `token*` no projeto | — | **nenhum** encontrado (excluindo `Library/`, `Builds/`, `.git/`) |

### 5.2 Versionado

- `git ls-files | grep -iE "env|keystore|jks|pem|secret|token|credential"` → só `arte/tools/meshy/.env.example` (modelo) e `design/pipeline/TEMPLATES/ENVIRONMENT_PRODUCTION_CARD.md` (**falso positivo**: "ENVIRONMENT" = ambiente de cenário, é um template de ficha).
- Grep por padrões de chave (`AKIA…`, `sk-…`, `ghp_…`, `AIza…`, `-----BEGIN`, `password\s*=`, `apiKey`/`api_key`/`API_KEY`) em `*.cs *.ps1 *.py *.md *.json *.yaml *.gd *.luau *.bat`: **9 ocorrências, todas do nome da variável `MESHY_API_KEY`** com placeholder `msy_sua_chave_aqui` (`arte/tools/meshy/meshy.py` l.4, 42, 49, 54, 56; `design/pipeline/MESHY.md` l.25, 32, 45; `design/pipeline/MESHY/00-API_PLAYBOOK.md` l.11). Nenhum valor real. Nenhum `-----BEGIN`, nenhuma chave AWS/OpenAI/GitHub/Google.
- **Mecanismo da chave Meshy** (`meshy.py`, função `chave()`, l.40–58): lê `os.environ["MESHY_API_KEY"]`; se vazia, lê `tools/meshy/.env` ao lado do script (linha `MESHY_API_KEY=…`); se não achar, aborta com mensagem clara. Enviada como header `Authorization: Bearer`. Correto: a chave nunca está no código.
- `.claude/`: `launch.json` (só `name/runtimeExecutable/runtimeArgs/port`) e `scheduled_tasks.lock` — **sem** chaves de permissão ou tokens. Não existe `.claude/settings*.json` no repo.
- `mobile-godot/godot/export/SETUP.md` l.26 referencia um caminho absoluto de keystore de debug do Godot (`C:/Users/VINICIUS/AppData/Roaming/Godot/keystores/debug.keystore`) — não é segredo, é caminho; e é documentação morta.

**Veredito: não há segredo versionado.** `.gitignore` e `.gitattributes` estão corretos para isso.

---

## 6. Higiene / observações (severidade honesta)

| Sev. | Achado | Evidência | Efeito |
|---|---|---|---|
| **P0** | **93 commits (12 dias de trabalho: toda a reescrita Unity) só existem neste disco.** `origin/main` parou em `0694794` de 09/09. Junto vão 134 arquivos LFS novos (~504 MB: os 20 FBX dos magos, GLBs, retratos) — o cache em `.git/lfs` é local. Um HD que morre leva o produto. | `## main...origin/main [ahead 93]`; `git diff --shortstat origin/main main` = 738 arquivos, +78.881; `git log -1 origin/main` = 2026-09-09 | Perda total em falha de disco. Push vai custar ~504 MB de banda LFS (cota grátis do GitHub: 1 GB/mês; já documentado em `.gitattributes`) — pode estourar a cota no mesmo mês em que alguém clonar. |
| **P1** | **Sem keystore de release.** O APK sai assinado com `CN=Android Debug` (keystore padrão de `~/.android`, 17/08). `androidUseCustomKeystore: 0`. `versionCode` = 1 desde sempre. Para teste interno é normal; para qualquer distribuição (Play, teste fechado, até instalar por cima num aparelho que tenha outra build) vai travar. `design/pipeline/ANDROID.md` já lista isso como pendência da publicação. | apksigner: `Signer #1 certificate DN: C=US, O=Android, CN=Android Debug`; `ProjectSettings.asset` l.180, 289 | Não bloqueia hoje; bloqueia publicação. Sem backup do debug.keystore, trocar de máquina = reinstalar o app em todo aparelho de teste. |
| **P1** | **85,8 MB de binários versionados como blob comum, fora do LFS.** 5 `.glb` em `arte/cenario/_glb/` (castelo 10,8 MB, bau 9,6, luva-cajado 8,8, luva-manopla 8,3, luva-varinha 7,7 = 45 MB) entraram no commit `ee7d868` (27/08) **antes** da regra `arte/**/*.glb` existir; `check-attr` diz `filter: lfs` hoje, mas o blob no HEAD tem 11.109.628 bytes. Mais 15 `.jpg` de textura em `mobile-godot/godot/{world,gameplay}/modelos/` (2–4,4 MB cada, ~40 MB) — `.jpg` não tem regra LFS nenhuma. | `git ls-tree -r -l HEAD` ordenado; `git cat-file -s HEAD:arte/cenario/_glb/castelo.glb` = 11109628; `git lfs ls-files \| grep _glb` = 0 | Todo clone baixa 86 MB de história que não sai mais ("não tem volta barata", como o próprio `.gitattributes` avisa). Os mesmos `castelo.glb`/`bau.glb`/luvas já estão **triplicados** (arte/_glb, mobile-godot, Resources — os dois últimos no LFS). Não reescrever histórico (regra do CLAUDE.md); aceitar o custo ou, no máximo, parar de somar. |
| **P2** | **5,4 GB de APKs acumulados** em `Builds/testes` (25 builds de 12/09 a 21/09, 176–266 MB cada). Ignorados pelo git, sem valor histórico além do mais recente e talvez um por dia. | tabela §4.1; `du -sh` = 5,2 GB | Só espaço em disco (e o disco tem 7,9 GB de RAM segundo o `build.log` — máquina modesta; ver P2 seguinte). |
| **P2** | **`Library/` de 9,8 GB** (`Bee/` 7,2 GB é o projeto Gradle/IL2CPP Android com objetos compilados). Regenerável, mas regenerar custa um build IL2CPP completo (dezenas de minutos nesta máquina). | `du -sh mobile-unity/Library/*` | Não apagar por "higiene" sem motivo: é o cache que faz o build incremental de 21/09 sair em minutos. Se o agente do emulador decidir por x86_64, `Bee/` vai crescer (segundo alvo IL2CPP). |
| **P2** | **`mobile-godot/` (101 MB, 330 arquivos, 64 `.gd`) e `roblox/` (43 arquivos, 40 `.luau`) são referência morta declarada** (`mobile-godot/00-LEIA.md`: "Não roda mais nesta máquina"; Godot desinstalado 09/09). Dos LFS, `mobile-godot/godot/world/modelos/*.glb` e `characters/modelos/*.glb` duplicam o que já está em `Resources/`. | `swarm.yaml` regra 2; CHANGELOG 09/09 | Custo de clone (LFS duplicado) e de leitura. Decisão de manter é do Diretor (é "o que o Unity tem que alcançar"); só registrar. |
| **P2** | **`.git/lfs` com 1,6 GB** para 1.055 MB de LFS vivos: ~550 MB são versões antigas de PNG/GLB substituídos. `git lfs prune` limparia; é operação segura, mas fora do escopo desta auditoria (somente leitura). | `du -sh .git/*` | Só disco. |
| **P3** | **13 `.idx` órfãos** em `.git/objects/pack/` (garbage 193 KiB): todo comando de contagem imprime 12 warnings "no corresponding .pack". Sobra de `gc`/`repack` interrompido. | `git count-objects -vH` | Ruído. Remover os `.idx` sem par é seguro, mas fere "nunca `git gc --prune`" no espírito — deixar para o Diretor. |
| **P3** | **Referências mortas de configuração:** (a) `.claude/launch.json` manda `npm run dev` na porta 5173 (era o Capacitor, encerrado 20/08) — não há `package.json` na raiz; (b) `swarm.yaml` aponta `mobile-unity/Assets/Scripts/Core`, `Assets/Tests`, `Assets/Scenes` — os caminhos reais são `Assets/_Arkana/…`; (c) `design/pipeline/ANDROID.md` cita `Build.Android` em `Assets/Editor/Build.cs` — o método é `Arkana.EditorTools.Build.Android` em `Assets/_Arkana/Editor/Build.cs`; (d) `design/infra/servidor-06-ferramentas-locais.md` (verificado 20/08) descreve Godot/Rojo/Luau e diz que Android SDK e JDK estão ausentes — hoje existem dois SDKs (Unity e `%LOCALAPPDATA%\Android\Sdk`) e JDK 21 (Adoptium). | leitura dos arquivos; `ls package.json` falha | Quem seguir esses docs erra o caminho. Baixo custo de corrigir. |
| **P3** | **Duas identidades de autor** (Vinicius 225, Vvs2705 6) — `git config user.name` mudou em algum momento. | `git shortlog -sn` | Só cosmético no `blame`. |
| **P3** | **Commits "Main.unity regravada pelo build do APK" (6×):** o build reescreve a cena com ids novos e isso vira commit `chore` a cada APK. | `git log --oneline -30` | Ruído no histórico; um `git checkout -- Main.unity` no fim do `build_apk.ps1` (ou cena montada 100% por código, que já é o desenho: `MainSceneBuilder.cs`) resolveria. Vetável. |
| OK | `.meta`: **0 órfãos e 0 arquivos sem `.meta`** em `Assets/` (amostra completa). | `find` pareado | — |
| OK | `InitTestScene7691d2a3-….unity` existe no disco mas está **ignorado** e **não rastreado** (a regra `Assets/InitTestScene*` foi acrescentada depois do commit acidental de 12/09 e o arquivo foi retirado do índice). | `git status --ignored`; `git ls-files` sem match | — |
| OK | `packages-lock.json` versionado (correto para Unity — trava versões dos pacotes); `UserSettings/` ignorado; `*.csproj/*.sln` ignorados. | `.gitignore`, `git ls-files` | — |
| OK | **Sem caminho absoluto** em `*.ps1 *.py *.cs *.json *.yaml` (os `.ps1` usam `$PSScriptRoot`; só o caminho fixo do `Unity.exe` 6000.3.23f1 em `C:\Program Files\Unity\Hub\Editor\…`, que é o instalado). Único `.md` com caminho absoluto: `mobile-godot/godot/export/SETUP.md` (morto). | `git grep` | — |
| OK | Sem CI. Coerente com "uma pessoa, uma máquina, licença Personal que precisa do Hub aberto" (`portao.ps1`). Registrar apenas. | §1.3 | — |

---

## 7. Arquivos importantes

| Arquivo | Função | Por que é importante |
|---|---|---|
| `README.md` (raiz) | Estrutura do repo: `design/` decide, `arte/` fornece, `mobile-unity/` é o produto | Primeira leitura para qualquer agente |
| `CLAUDE.md` (raiz) | Regras permanentes: sem pastas novas, sem worktrees, só fast-forward em `main`, nunca reescrever histórico | Governa o que esta auditoria pode e não pode fazer |
| `CHANGELOG.md` | Marcos: Roblox (ago) → Capacitor (encerrado 20/08) → Godot (encerrado 09/09) → Unity | Explica por que há três bases no repo |
| `swarm.yaml` | Topologia de agentes e regras (design é fonte da verdade; LFS obrigatório; portão de testes) | Caminhos de escopo estão desatualizados (P3) |
| `.gitignore` | Exclui Library/Builds/Logs/APK/segredos/originais Meshy | Correto; é o que mantém 5,4 GB de APK e 9,8 GB de Library fora do git |
| `.gitattributes` | Regras LFS + `text eol=lf` para assets de texto do Unity; documenta custo de banda | Falta `.jpg`; regra `arte/**/*.glb` chegou depois dos `_glb` (P1) |
| `design/PROJETO.md` (1.706 linhas) | Memória do projeto, atualizado 21/09: estado, o que foi feito, "CONTINUAR DAQUI" | Documento único de memória por ordem do Diretor |
| `design/PONTE.md` (841 linhas) | O que o Roblox provou e ainda não está no GDD | Regra: nada migra sem passar pelo GDD |
| `design/gdd/GDD.md` (593 linhas) | GDD do battle royale de magos (classes, kits, zona, dano) | Fonte da verdade de gameplay |
| `design/pipeline/ANDROID.md` | Identidade do app (package, ARM64, API 26, Poco F4), como gerar APK, pendências de publicação | Base para o agente de emulador; tem 1 caminho desatualizado |
| `design/infra/00-LEIA.md` + `servidor-0*.md` | Multijogador, servidores, contas, custos, dados de menores, ferramentas locais | Planejamento pós-jogo pronto; `servidor-06` está defasado (20/08) |
| `design/pipeline/MESHY.md`, `MESHY/00-API_PLAYBOOK.md` | Como usar a API da Meshy (chave via `MESHY_API_KEY`) | Único ponto de segredo do projeto; bem tratado |
| `mobile-unity/00-LEIA.md` | Como rodar: `portao.ps1`, `foto.ps1`, `build_apk.ps1` | Manual operacional do produto |
| `mobile-unity/ARQUITETURA.md` | Contratos entre raias, namespaces, donos de arquivo, "sem pacote novo sem pedir" | Regras de código do Unity |
| `mobile-unity/build_apk.ps1` | Abre o Hub (licença), roda `Unity.exe -batchmode -executeMethod Arkana.EditorTools.Build.Android`, copia APK datado para `Builds/testes/` | É o único pipeline de build; sem CI |
| `mobile-unity/portao.ps1` | Compila + testes EditMode e PlayMode headless; "ARKANA: N testes, 0 falhas" | Portão de qualidade obrigatório (`swarm.yaml`) |
| `mobile-unity/foto.ps1` | Roda `FotoTests` com GPU e grava PNGs em `Logs/fotos/` | Como o Diretor julga (pelo visual) |
| `mobile-unity/importar_mago.ps1` | Zip da Meshy → `Resources/magos/<slug>.fbx + -cor.png + -normal.png` | Pipeline de personagem; alimenta 230 MB de LFS |
| `Assets/_Arkana/Editor/Build.cs` | `ApplySettings()` (URP, Linear, IL2CPP, ARM64, minSdk 26, targetSdk 35, landscape, `IncluirShadersDoCodigo`) e `Android()` | Onde mudar ABI (x86_64) se o emulador exigir; onde entraria keystore de release |
| `Assets/_Arkana/Editor/MainSceneBuilder.cs` | Monta `Main.unity` por código | A cena é derivada; explica os commits "regravada pelo build" |
| `Assets/_Arkana/Editor/ImportacaoArkana.cs` | AssetPostprocessor: liga material/textura dos FBX da Meshy | Convenção `<slug>-cor.png`/`-normal.png` |
| `Assets/_Arkana/Scenes/Main.unity` | Única cena no `EditorBuildSettings` | Tudo entra por ela |
| `Assets/_Arkana/Settings/URP_Base.asset`, `URP_Base_Renderer.asset` | Pipeline URP (SSAO desligado e sombras baixas no commit `9749474`) | Onde vive o desempenho de GPU |
| `ProjectSettings/ProjectSettings.asset` | `br.com.vstack.arkana`, IL2CPP (`scriptingBackend Android: 1`), `AndroidTargetArchitectures: 2` (ARM64), minSdk 26, targetSdk 35, `androidApplicationEntry: 2` (GameActivity), `androidUseCustomKeystore: 0`, `bundleVersion 1.0`, `AndroidBundleVersionCode 1` | Identidade e alvo do APK |
| `ProjectSettings/ProjectVersion.txt` | `6000.3.23f1 (09d2ecc7fb28)` | Única versão do Unity instalada; `build.log` confirma |
| `ProjectSettings/QualitySettings.asset` | 6 níveis (Very Low…Ultra), `m_CurrentQuality: 5`, todos com o mesmo `customRenderPipeline` (URP_Base) | O commit `9749474` diz que "Qualidade muda de verdade" via código, não por aqui |
| `ProjectSettings/GraphicsSettings.asset` | `m_CustomRenderPipeline` → URP_Base | Confirma URP |
| `Packages/manifest.json` | `com.unity.cloud.gltfast 6.20.0`, `inputsystem 1.14.0`, `render-pipelines.universal 17.3.0`, `test-framework 1.5.1`, `ugui 2.0.0` + módulos | Só 5 pacotes externos; glTFast carrega os `.glb` em runtime |
| `Packages/packages-lock.json` | Trava de versões | Versionado (correto) |
| `Scripts/Arkana.asmdef` | Assembly único do produto | Referenciado pelos 2 asmdefs de teste |
| `Scripts/Main.cs` | Bootstrap: monta arena, menu, HUD | Entrada do jogo |
| `Scripts/PartidaPeloAdb.cs`, `BancadaDeCortes.cs`, `MedidorDeFps.cs` | Partida disparada por `adb shell am start --ei`; 15 cortes de GPU de 10 s com FPS no logcat | Ferramentas de medição no aparelho — úteis também num emulador |
| `Scripts/Gameplay/Partida.cs`, `Player.cs`, `Bot.cs`, `Pawn.cs`, `Locomocao.cs`, `Zona.cs`, `Projetil.cs` | Loop da partida, jogador, bots, zona, tiro | Núcleo do gameplay |
| `Scripts/Core/Bus.cs`, `Elemento.cs`, `IEntidade.cs`, `Kits*.cs`, `Combat.cs`, `Balance.cs` | Contratos e regras puras (testáveis em EditMode) | Dono: coordenador (ARQUITETURA.md) |
| `Scripts/UI/Hud.cs`, `JoystickVirtual.cs`, `GestoDeDisparo.cs`, `Minimapa.cs` | HUD de toque | Interface do celular |
| `Scripts/World/Ilha.cs`, `Relevo.cs`, `Mata.cs`, `Ruinas.cs`, `Castelo.cs` | Geração da Ilha Fraturada por código a partir dos `.glb` | Onde entram os 44 GLB de `Resources/` |
| `Tests/EditMode/Arkana.Tests.asmdef` (58 arquivos) | Testes puros, `includePlatforms: Editor`, `UNITY_INCLUDE_TESTS` | Portão rápido |
| `Tests/PlayMode/Arkana.PlayTests.asmdef` (BootTests, FotoTests, CarregamentoTests, AbateVisualTests, VooVisualTests) | Testes com cena montada e fotos | Portão lento + fotos para o Diretor |
| `Builds/build.log` (592 KB) | Log do último build: Unity 6000.3.23f1, Gradle 9.1.0, AGP 9.0.0, return code 0, "Android IL2CPP/ARM64, minSdk 26, landscape" | Evidência do que gerou o APK |
| `mobile-godot/00-LEIA.md` | "Referência para a reescrita em Unity"; 12/12 autotestes verdes em 04/09; Godot desinstalado | Explica por que a pasta fica |
| `arte/00-LEIA.md` | Mapa de `arte/` (personagens, cenário, áudio, prompts, tools) | Entrada da matéria-prima |
| `arte/tools/meshy/meshy.py`, `montar_glb.py`, `test_glb.py` | Cliente da API Meshy; montagem de GLB; teste | Pipeline de geração 3D |
| `arte/tools/blender/otimizar.py`, `fundir_animacoes.py`, `desneon.py` | Decimação, fusão de clipes, remoção de neon | Pipeline de otimização para celular |
| `roblox/src/default.project.json` | Projeto Rojo "Arkana - Campo de Provas" | Referência morta (fase 1) |
| `.claude/launch.json` | `npm run dev` :5173 | Morto (Capacitor); P3 |

---

## 8. Comandos executados e ferramentas

### 8.1 Ferramentas

| Ferramenta | Versão / caminho |
|---|---|
| git | 2.54.0.windows.1 |
| git-lfs | 3.7.1 (GitHub; windows amd64; go 1.25.1) |
| unzip | UnZip 6.00 (Git Bash) |
| keytool | `C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\keytool.exe` (também Adoptium JDK 21.0.12) |
| aapt2 / apksigner | `…\AndroidPlayer\SDK\build-tools\36.0.0\` (Unity); também `%LOCALAPPDATA%\Android\Sdk\build-tools\{35.0.0,36.0.0}` |
| PowerShell | 5.1.19041.6456 (usado só para a versão) |
| du / find / sha256sum / awk | GNU coreutils 8.32 (Git Bash) |
| SDKs Android presentes (só listagem) | Unity: `platforms/android-{34,35,36}`, `platform-tools/adb.exe`, `cmake`, `cmdline-tools`, NDK; `%LOCALAPPDATA%\Android\Sdk`: `platforms/android-{35,36}`, `cmdline-tools/latest`, `build-tools`, `platform-tools`, `licenses` — **sem `emulator/`, sem `system-images/`, sem `~/.android/avd/`** (nenhum AVD existe hoje) |

### 8.2 Comandos (todos de leitura)

| Comando | Objetivo | Resultado |
|---|---|---|
| `git status --porcelain --branch` (clone e worktree) | Sujeira e relação com remoto | Limpo; `main...origin/main [ahead 93]` |
| `git log -1 --format="%H %ad %an %s" --date=iso` | HEAD | `0a94c2f…` 2026-09-21 14:54 Vinicius |
| `git log --oneline -30` | Últimos commits | Ver §1.2 |
| `git branch -a -vv` / `git worktree list` / `git tag -n1` / `git stash list` | Branches, worktrees, tags, stash | 2 locais + `origin/main`; 2 worktrees; 0 tags; 0 stash |
| `git remote -v` (URL mascarada por sed) | Remote | `https://github.com/Vvs2705/arkana.git`, sem token |
| `git lfs ls-files \| wc -l`, `git lfs ls-files -s`, `git lfs env \| grep Endpoint` | LFS | 392 arquivos / 1.055 MB; endpoint GitHub |
| `git count-objects -vH` | Objetos | 237 MiB em packs; 12 idx órfãos |
| `git rev-list --count main` / `origin/main` | Contagem | 231 / 138 |
| `git log --reverse … \| head -3` | Primeiro commit | 2026-08-17 "Arkana v0.1" |
| `git shortlog -sn HEAD` | Autores | Vinicius 225, Vvs2705 6 |
| `git log --since=2026-09-01 --format=%ad \| sort \| uniq -c` | Frequência | Pico 50 commits em 12/09 |
| `git branch -a --no-merged main` | Branches pendentes | nenhuma |
| `git ls-files -z \| xargs -0 du -k \| sort -rn \| head -25` | Maiores no disco (worktree) | 20 FBX de 10–13 MB (LFS) |
| `git ls-tree -r -l HEAD \| sort -k4 -n -r` + `git cat-file -s` + `git check-attr` | Blobs grandes fora do LFS | 20 arquivos / 85,8 MB (5 glb + 15 jpg) |
| `git status --ignored --porcelain` | O que está fora do git | Library, Builds, Logs, .env, originais Meshy… |
| `cat .gitattributes .gitignore`; `ls .git/hooks`; `ls .github …` | Regras, hooks, CI | Hooks = git-lfs; sem CI |
| `git log --format=%s \| grep -oE "^[a-z]+(\(…\))?:" \| sort \| uniq -c` | Convenção de commits | Conventional em pt-BR; 58 sem prefixo |
| `git diff --shortstat origin/main main`; `git diff --name-only … '*.png' '*.glb' '*.fbx'` + `git lfs ls-files -s` | O que não foi enviado | 738 arquivos, +78.881; 134 LFS ≈ 504 MB |
| `git config --get-regexp lfs` | Config LFS | filtros padrão; sem include/exclude |
| `du -sh --exclude=.git *`, `du -sh .git .git/* mobile-unity/* Library/* Builds/* arte/* arte/cenario/* Assets/_Arkana/*` | Tamanhos | §2 |
| `git ls-files \| grep -ciE "\.<ext>$"` (por extensão e por pasta) | Contagens | §2.4 |
| `find design arte mobile-unity/Assets/_Arkana … -maxdepth 2`; `ls Packages ProjectSettings Editor Tests Settings Scenes Resources` | Mapa | §3 |
| `ls -l Builds/*.apk Builds/testes/*.apk`; `find . -iname "*.apk" -o -iname "*.aab"` (prune Library/.git); `ls ~/Downloads/*.apk` | Inventário de APK | 26 no repo; 1 intermediário do Gradle; 2 de outro projeto em Downloads |
| `sha256sum arkana.apk testes/arkana-2026-09-21_1447.apk Library/…/launcher-release.apk` | Igualdade | Os 3 idênticos (`c6b6b199…`) |
| `unzip -l arkana.apk` (+ awk por pasta/lib/.so/META-INF/maiores) | Conteúdo | ARM64 only, IL2CPP, GameActivity; 476 MB descomprimido |
| `keytool -printcert -jarfile arkana.apk` | Certificado v1 | "Não é um arquivo jar assinado" (não há v1) |
| `apksigner verify --verbose --print-certs arkana.apk` (digests omitidos) | Esquema e certificado | v2 only; `CN=Android Debug`, RSA 2048 |
| `aapt2 dump badging arkana.apk`; `aapt2 dump xmltree --file AndroidManifest.xml` | Package, SDKs, permissões, debuggable | `br.com.vstack.arkana` 1/1.0, min 26, target 35, não debuggable, `extractNativeLibs=true` |
| `grep -nE "Build completed\|Gradle\|IL2CPP\|…" Builds/build.log`; `head/tail` | O que gerou o APK | Gradle 9.1.0/AGP 9.0.0; return code 0 |
| `find … -iname ".env*" -o -iname "*.keystore" … \| stat -c %s` (prune Library/Builds/.git) | Sensíveis no disco | `.env` 56 B (ignorado) e `.env.example` 183 B |
| `git ls-files \| grep -iE "env\|keystore\|…"`; `git grep -nIE "<padrões de chave>" -- *.cs *.ps1 …` (valores mascarados por sed) | Segredos versionados | Nenhum; só nome de variável + placeholder |
| `grep -nE "androidUseCustomKeystore\|AndroidMinSdkVersion\|…" ProjectSettings.asset`; `cat ProjectVersion.txt manifest.json`; `grep … QualitySettings.asset GraphicsSettings.asset EditorBuildSettings.asset` | Configuração Android/URP | §4.2 e §7 |
| `head -25 *.ps1`; `head -60 Build.cs`; `cat .claude/launch.json swarm.yaml`; `sed -n 40,58p meshy.py`; `grep -oE "^[A-Za-z_]+=" .env.example` | Scripts e mecanismo da chave | §5, §7 |
| `find Assets -name "*.meta"` pareado nos dois sentidos | `.meta` órfãos | 0 / 0 |
| `git grep -nE "C:\\\\\|C:/\|/c/Users" -- *.ps1 *.py *.cs *.json *.yaml *.bat` e `-- *.md` | Caminhos absolutos | 0 em código; 1 `.md` morto |
| `ls -l ~/.android` (só nomes/tamanhos); `ls …/AndroidPlayer/SDK`, `%LOCALAPPDATA%\Android\Sdk\*` | Keystore de debug e SDK/emulador | `debug.keystore` 2.618 B (17/08); sem emulator/system-images/AVD |

**Não consegui / não fiz:** ler o `AndroidManifest.xml` binário diretamente (não é necessário — `aapt2` resolveu); medir a banda LFS já consumida no GitHub (exige API/rede; fora do escopo); confirmar que os objetos LFS dos 93 commits **não** estão no servidor (inferido de `origin/main` parado em 09/09 e dos 134 arquivos novos no diff — `git lfs push --dry-run` tocaria a rede e não foi executado).
