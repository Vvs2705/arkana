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

O Unity está instalado com o módulo Android (SDK, NDK e JDK próprios). O build
sai por linha de comando, sem abrir o editor:

```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -nographics -quit \
  -projectPath mobile-unity -executeMethod Build.Android -logFile mobile-unity/Logs/build.log
```

O método `Build.Android` é escrito no passo 0 (`Assets/Editor/Build.cs`). APKs
são artefato local e ficam fora do git; o APK datado vai sempre para a mesma
pasta do clone principal, venha de worktree ou não (ordem do Diretor, 27/08).

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
