# ANDROID - estado atual

O produto Android atual e o jogo 3D em Godot 4.4. O pipeline antigo baseado em
Capacitor foi encerrado e removido.

## Identidade do aplicativo

| Campo | Valor |
|---|---|
| Engine | Godot 4.4.1 |
| Projeto | `godot/project.godot` |
| Preset | `godot/export_presets.cfg` |
| Package ID | `br.com.vstack.arkana3d` |
| Arquitetura de debug | ARM64 |
| Saida local | `godot/build/arkana3d.apk` |

## Gerar APK de debug

O ultimo APK local existe em `godot/build/arkana3d.apk`. Na verificacao de
20/08/2026, os templates de export 4.4.1, o Android SDK e o JDK 21 nao estavam
mais nos caminhos configurados; portanto, um novo export exige restaurar essas
dependencias. O estado completo esta em `godot/export/SETUP.md`.

```bash
bash godot/export/build_apk.sh
```

Com o ambiente restaurado, o script importa o projeto, exporta o APK e verifica
se o pacote contem o runtime Android e os assets do jogo. APKs e AABs sao
artefatos locais e ficam fora do Git.

## Estado de publicacao

O APK atual serve para desenvolvimento e teste em aparelho. Publicacao na Play
Store continua fora do escopo de G3 e exige, no minimo:

- keystore de release com backup externo;
- politica de `versionCode` e `versionName`;
- AAB assinado e verificavel;
- politica de privacidade e canal de suporte;
- ficha da loja, capturas e classificacao IARC;
- teste fechado conforme a regra vigente da Play Store.

Esses itens entram em G6. Nenhuma conta, assinatura ou gasto deve ser criado
antes da autorizacao do Diretor.
