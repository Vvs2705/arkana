# Ferramentas locais - estado verificado

Verificado em 20/08/2026 durante a consolidacao do repositorio.

## Presentes

| Ferramenta | Estado | Uso |
|---|---|---|
| Godot 4.4.1 | instalado em `%LOCALAPPDATA%/Programs/godot/` | editor, import e selftests |
| Rojo | instalado | build do Campo de Provas |
| Luau | instalado | runner e verificacao do Roblox |
| Git | instalado | versionamento e sincronizacao |

## Ausentes na verificacao atual

| Dependencia | Caminho esperado | Impacto |
|---|---|---|
| Templates de export Godot 4.4.1 | `%APPDATA%/Godot/export_templates/4.4.1.stable/` | bloqueia novo APK/AAB |
| Android SDK | `%LOCALAPPDATA%/Android/Sdk` | bloqueia export e `adb` |
| JDK 21 | `%LOCALAPPDATA%/Java/jdk-21.0.12+8` | bloqueia assinatura e Gradle |
| Keystore de debug | `%APPDATA%/Godot/keystores/debug.keystore` | o script pode gerar depois que o JDK voltar |

O arquivo `godot/build/arkana3d.apk` existe e foi gerado antes dessas
dependencias deixarem de estar disponiveis. Para reproduzir o APK, restaurar os
tres primeiros itens e seguir `godot/export/SETUP.md`.

## Futuro

- Blender: opcional em G3, se a producao procedural nao bastar.
- WSL2 ou Docker: somente em G5 para testar o servidor Linux.
- CI: somente em G5, quando cliente e servidor precisarem sair do mesmo commit.

Nao instalar engines extras, SDKs de anuncios ou ferramentas de servidor sem
uma fase ativa que justifique o custo e a manutencao.
