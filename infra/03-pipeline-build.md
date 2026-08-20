# Pipeline de build — export, assinatura, versão, CI

> O que já existe funciona e é rápido. Este arquivo documenta o que se soma a
> ele por fase — não substitui nada que está de pé.

## 1. O que já existe (não mexer, usar)

- **`godot/export/build_apk.sh`** (dono: raia MUNDO) — APK de debug arm64 via
  Godot headless, com checagens que falham cedo e com endereço, e verificação
  do pacote no final. Pipeline provado ponta a ponta em 19/08 (~26 MB).
- **`godot/export/SETUP.md`** — o setup de máquina (Godot 4.4.1, templates,
  SDK, JDK 21, keystore de debug) e as pegadinhas já pagas: a flag
  `import_etc2_astc` sem a qual o export falha em silêncio, e o template
  pré-compilado que rejeita `min_sdk` customizado.
- **`docs/BUILD_ANDROID.md` §5** — o passo a passo de assinatura de release
  (keytool, keystore fora do repo, backup em dois lugares, Play App Signing).
  Foi escrito para o 2D/Capacitor, mas o **processo de chave é idêntico**:
  mesmo `keytool` do JDK 21, mesma regra "quem cria a chave é quem guarda a
  senha" — ato do Diretor, nunca de agente.

## 2. Export do servidor Linux (entra no G5)

Novo preset no `export_presets.cfg`: **"Linux Server"** com *Export Mode →
Dedicated Server* (detalhe do que isso faz em `01-servidores-de-jogo.md` §1).
Pontos objetivos:

- Cross-export Windows→Linux funciona com os templates 4.4.1 já instalados
  (x86_64; **arm64 também é oficial** no 4.4 — necessário se a VPS for a
  Oracle ARM).
- O `build_apk.sh` é o molde: um `build_server.sh` irmão com as mesmas
  checagens early-fail (`--export-release "Linux Server"
  build/arkana_server.x86_64`) é tudo que a fase pede. **Criar só quando o G5
  abrir** — script para preset que não existe é prateleira.
- O binário + `.pck` sobem para a VPS por `scp`; rodar com `--headless`.

## 3. Assinatura de release Android (entra no G6)

O processo humano já está documentado (`BUILD_ANDROID.md` §5). O que muda no
Godot em relação ao Gradle do 2D:

- **Nunca** preencher keystore/senha de release no `export_presets.cfg` — o
  arquivo é versionado. O Godot 4 lê as credenciais de **variáveis de
  ambiente** no export headless: `GODOT_ANDROID_KEYSTORE_RELEASE_PATH`,
  `GODOT_ANDROID_KEYSTORE_RELEASE_USER`,
  `GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD`. Mesmo espírito do
  `keystore.properties` fora do git — verificar na doc vigente do Godot os
  nomes exatos na versão usada no dia.
- A keystore em si: gerar pelo passo 1 do `BUILD_ANDROID.md` §5 (ato do
  Diretor 🧑), guardar fora do repo E fora desta máquina. Perder a chave de
  upload sem Play App Signing = nunca mais atualizar o app.
- **AAB** (formato exigido pela Play para apps novos): no Godot exige
  `use_gradle_build=true` no preset — e aí vale a nota do `SETUP.md`: setar
  `gradle_build/min_sdk="24"` e a primeira build baixa o Gradle 8.2 da rede.
  É mudança de G6, não de agora.

## 4. Versionamento de APK/AAB

A Play recusa reenvio com `version/code` repetido — o erro nº 1 de novato
(aviso já registrado no `BUILD_ANDROID.md`). Política proposta (1 linha de
disciplina, zero ferramenta):

- **`version/code`** (no preset Android): inteiro, **incrementa +1 a cada
  artefato que sai da máquina** para loja ou testador — nunca reusa.
- **`version/name`**: semver amarrado às fases do ROADMAP (`0.5.x` = G5,
  `1.0.0` = lançamento G6). É o que o jogador vê; o `code` é o que a loja
  compara.
- Registrar cada par (code, name, data, o que mudou) no CHANGELOG — hábito que
  o projeto já tem.

## 5. Quando um runner de CI (GitHub Actions) passa a valer

Hoje **não vale**: o export local leva segundos, uma pessoa builda, e a
verificação já está automatizada no script. CI agora seria manutenção sem
cliente.

Passa a valer quando **qualquer um** destes ficar verdadeiro:

1. **G5 em diante** — cliente e servidor precisam sair **do mesmo commit**
   sempre (dessincronizar protocolo cliente×servidor é o bug mais chato de
   caçar). CI que exporta os dois artefatos a cada tag elimina o "esqueci de
   rebuildar o servidor".
2. Mais de uma máquina/pessoa buildando.
3. Release para loja com cadência — build reproduzível fora da máquina do
   Diretor vira apólice de seguro.

Custo quando entrar: **R$ 0 na faixa gratuita** do GitHub Actions (minutos
mensais grátis em repo privado; repo público, ilimitado) — **verificar limite
vigente**. Receita conhecida: job Linux que baixa o Godot 4.4.1 headless + os
export templates (ou usa uma imagem docker comunitária de godot-ci) e roda os
mesmos scripts de `godot/export/`. Segredos (keystore base64 + senhas) em
GitHub Secrets, nunca no YAML. **Escrever o workflow só quando o gatilho 1
disparar.**
