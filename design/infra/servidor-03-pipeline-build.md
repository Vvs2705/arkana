# Pipeline de build - export, assinatura, versao e CI

## Estado atual

- `godot/export/build_apk.sh` gera o APK de debug ARM64, falha cedo quando o
  ambiente esta incompleto e verifica o pacote final.
- `godot/export/SETUP.md` documenta Godot 4.4.1, templates, Android SDK, JDK 21
  e keystore de debug.
- `godot/export_presets.cfg` contem o preset Android versionado.
- APKs e AABs ficam fora do Git.

## G5 - servidor Linux

Quando o multiplayer dedicado abrir, adicionar um preset `Linux Server` e um
script `godot/export/build_server.sh` com as mesmas checagens do export Android.
Cliente e servidor devem ser produzidos pelo mesmo commit.

## G6 - assinatura Android

A assinatura de release e ato do Diretor:

1. Gerar uma keystore de upload com o `keytool` do JDK 21.
2. Guardar a keystore e as senhas fora do repositorio e com backup externo.
3. Passar credenciais ao export por variaveis de ambiente ou configuracao local,
   nunca por `export_presets.cfg` versionado.
4. Ativar Gradle build para gerar AAB conforme os requisitos vigentes do Godot
   e da Play Store.
5. Verificar a assinatura antes de distribuir.

Os nomes exatos das variaveis e os requisitos da loja devem ser conferidos na
documentacao vigente no momento de G6.

## Versionamento

- `version/code`: inteiro crescente; nunca reutilizar em artefato distribuido.
- `version/name`: semver alinhado aos marcos do roadmap.
- Registrar code, name, data e commit no CHANGELOG de cada build externa.

## CI

GitHub Actions passa a valer em G5, quando cliente e servidor precisam sair do
mesmo commit, ou quando mais de uma maquina produzir builds. Ate la, o export
local verificado e mais simples. Segredos de assinatura pertencem ao cofre de
secrets do provedor e nunca ao YAML.
