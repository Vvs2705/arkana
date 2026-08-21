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

Verificacao de 21/08/2026: **todas as dependencias estao presentes** (Godot
4.4.1, templates 4.4.1.stable, Android SDK, JDK 21 e keystore de debug). O
bloqueio anotado em 20/08 nao existe mais — o export roda de ponta a ponta. O
estado completo esta em `godot/export/SETUP.md`.

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
