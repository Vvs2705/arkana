# Export Android — setup da maquina (dono: raia MUNDO)

Config de MAQUINA, nao do repo. O repo carrega so' `export_presets.cfg` (na raiz
de `godot/`) e `export/build_apk.sh`. Nada disto vai para o git.

## O que a maquina precisa

Estado verificado em 21/08/2026: Godot, templates, Android SDK, JDK 21 e
keystore debug estao presentes. O APK debug foi exportado novamente em
`godot/build/arkana3d.apk`.

| Item | Caminho | Estado atual |
|---|---|---|
| Godot 4.4.1 | `%LOCALAPPDATA%/Programs/godot/Godot_v4.4.1-stable_win64.exe` | presente |
| Templates 4.4.1 | `%APPDATA%/Godot/export_templates/4.4.1.stable/` | presente |
| Android SDK | `%LOCALAPPDATA%/Android/Sdk` | presente |
| JDK 21 | `C:/Program Files/Eclipse Adoptium/jdk-21.0.12.8-hotspot` | presente |
| Keystore debug | `%APPDATA%/Godot/keystores/debug.keystore` | presente |

## editor_settings-4.4.tres (em `%APPDATA%/Godot/`)

O Godot headless le os caminhos do Android daqui. As chaves estao configuradas
para as dependencias presentes:

```
export/android/debug_keystore = "C:/Users/VINICIUS/AppData/Roaming/Godot/keystores/debug.keystore"
export/android/debug_keystore_pass = "android"
export/android/java_sdk_path = "C:\\Program Files\\Eclipse Adoptium\\jdk-21.0.12.8-hotspot"
export/android/android_sdk_path = "C:\\Users\\VINICIUS\\AppData\\Local\\Android\\Sdk"
```

Se o arquivo nao existir (maquina nova): abrir o editor uma vez (`Godot...exe -e
--quit`) para gera-lo, e colar as 4 linhas acima com os caminhos da maquina.

Keystore de DEBUG e' convencao publica do Android (alias `androiddebugkey`,
senhas `android`) — NAO e' segredo. Se sumir, o `build_apk.sh` gera um novo
sozinho via `keytool`. **Keystore de RELEASE: proibido gerar nesta raia.**

## Gerar o APK

```
bash godot/export/build_apk.sh
```

Sai em `godot/build/arkana3d.apk` (ignorado pelo git via `*.apk`). O script
falha cedo e com endereco se faltar Godot/template/SDK/JDK, e confere no final
que o APK contem `libgodot_android.so` + os assets do projeto.

## PENDENCIA de 1 linha no project.godot (dono: COORDENADOR)

O export Android do Godot 4.4 **falha silenciosamente** (erro de configuracao
SEM mensagem) se o projeto nao tiver, na secao `[rendering]`:

```
textures/vram_compression/import_etc2_astc=true
```

E' a linha que o wizard do Godot escreve em todo projeto Mobile; o
project.godot manuscrito ficou sem ela. O `build_apk.sh` detecta a falta e
falha cedo apontando exatamente isto. Pipeline PROVADO de ponta a ponta em
19/08 com a linha presente (APK de ~26MB gerado e verificado); project.godot
foi restaurado intocado apos a prova. Sem texturas importadas no projeto
(mundo procedural), a flag nao muda nada alem de destravar o export.

## Notas

- Preset usa o TEMPLATE PRE-COMPILADO (sem gradle): export em segundos, zero
  rede. O Godot 4.4.1 REJEITA min_sdk customizado sem gradle build (testado
  19/08 — erro de configuracao, nao warning). O template embute minSdk 21,
  superset de 24. Quando `use_gradle_build=true` ligar (plugins, fase G6),
  setar `gradle_build/min_sdk="24"` no preset e verificar a versao de Gradle
  exigida pelo Godot antes da primeira build.
- Landscape vem de `project.godot` (`display/window/handheld/orientation=4`,
  sensor_landscape) — o exporter escreve isso no manifest.
- Instalar no aparelho: `%LOCALAPPDATA%/Android/Sdk/platform-tools/adb.exe
  install -r godot/build/arkana3d.apk`.
