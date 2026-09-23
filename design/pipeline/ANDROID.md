# ANDROID — estado atual

O produto Android é o jogo em **Unity 6** (`mobile-unity/`). O pipeline Godot
foi encerrado em 09/09/2026 e o Capacitor antes dele, em 20/08.

## Identidade do aplicativo

| Campo | Valor |
|---|---|
| Engine | Unity 6000.3.23f1 (URP) |
| Package ID | `br.com.vstack.arkana` |
| Arquitetura | ARM64 |
| API mínima | Android 8.0 (API 26) |
| Aparelho de teste | Poco F4 (Snapdragon 870, Adreno 650, 120 Hz) |

## Gerar APK de desenvolvimento

O Unity está instalado com o módulo Android (SDK, NDK, JDK e Gradle próprios,
embutidos no editor). O build sai por linha de comando, sem abrir o editor:

```
powershell -File mobile-unity\build_apk.ps1
```

Por dentro: `Unity.exe -batchmode -nographics -buildTarget Android -executeMethod
Arkana.EditorTools.Build.Android` (método em `mobile-unity/Assets/_Arkana/Editor/Build.cs`,
log em `mobile-unity/Builds/build.log`). Exige o Unity Hub aberto (licença Personal;
o script abre). Produção é IL2CPP, **só ARM64**, release, assinada com a keystore
de debug. APKs são artefato local e ficam fora do git; o APK datado vai sempre
para `mobile-unity/Builds/testes/` do clone principal, venha de worktree ou não
(ordem do Diretor, 27/08).

## Emulador (desde 23/09/2026)

| Item | Valor |
|---|---|
| AVD | `arkana_api35` — Android 15 (API 35), `google_apis/x86_64` com tradução ARM64, 2400×1080 paisagem, 2 GB, GPU do host |
| Aceleração | WHPX (funciona nesta máquina; `mobile-unity/habilitar_whpx.bat` é contingência) |
| SDK | `%LOCALAPPDATA%\Android\Sdk` (emulator 37.1.11; platform-tools 37.0.1 é o adb da cadeia) |
| Variante de build | `Arkana.EditorTools.Build.AndroidEmulador` → `Builds/arkana-emulador.apk`: ARM64+x86_64, GLES3 fixo, Development; a produção continua ARM64-only |
| Esteira | `mobile-unity/emulador.ps1 [-Build]` → adb → `install -r` → partida automática (`arkana_auto`) → espera → screencap + logcat → PASS/FAIL em `Logs/emulador/<data>/` |

O emulador **não mede desempenho** (GPU do PC ≠ Adreno 650): serve para instalar,
abrir, jogar sem dedo, achar crash e ler log quando não há cabo. FPS e veredito
visual continuam no Poco F4. Uso em `mobile-unity/00-LEIA.md`.

## Estado de publicação

O APK serve para desenvolvimento e teste em aparelho. Publicação na Play Store
exige, no mínimo:

- keystore de release com backup externo;
- política de `versionCode` e `versionName`;
- AAB assinado e verificável;
- política de privacidade e canal de suporte;
- ficha da loja, capturas e classificação IARC;
- teste fechado conforme a regra vigente da Play Store.

Nenhuma conta, assinatura ou gasto deve ser criado antes da autorização do
Diretor.
