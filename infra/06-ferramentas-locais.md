# Ferramentas locais — o que a máquina tem e o que pode faltar

> **Princípio (mesmo com instalação autorizada pelo Diretor): instala-se
> QUANDO a fase pede, não antes.** Ferramenta parada é superfície de
> atualização, disco e distração — a mesma regra dos servidores.

## 1. O que a máquina JÁ tem (verificado em `godot/export/SETUP.md` e docs)

| Ferramenta | Onde | Serve a |
|---|---|---|
| **Godot 4.4.1** (editor + headless) | `%LOCALAPPDATA%/Programs/godot/` | todo o 3D; o export headless é o coração do pipeline |
| **Templates de export 4.4.1** | `%APPDATA%/Godot/export_templates/` | APK Android hoje; incluem Linux (x86_64 e arm64) para o servidor do G5 — nada novo a baixar |
| **JDK 21** (`jdk-21.0.12+8`) | `%LOCALAPPDATA%/Java/` | assinatura Android (keytool) e gradle build do AAB no G6 |
| **Android SDK** (platform-tools, build-tools 36) | `%LOCALAPPDATA%/Android/Sdk` | export, `adb install`, `apksigner` |
| Keystore de debug | `%APPDATA%/Godot/keystores/` | builds de teste (convenção pública, não é segredo) |
| **Rojo/Luau** (toolchain Roblox) | máquina | o Campo de Provas segue mantido para playtest de Sintonia — não remover |
| Node/npm + Capacitor/Gradle (do 2D) | repo `android/` | o 2D está descontinuado; fica como histórico. O cache do Gradle 8.14 não serve ao Godot (que pede 8.2 — `SETUP.md`) |
| Git + worktrees | — | o fluxo de raias inteiro |

**G0–G4 não precisam de NADA além disto.** A lista acima fecha o jogo contra
bots de ponta a ponta, incluindo o APK.

## 2. O que pode faltar adiante — cada item com a fase que o convoca

| Ferramenta | Fase que convoca | Para quê | Nota |
|---|---|---|---|
| **WSL2** (Ubuntu) | **G5** | rodar o export headless Linux do servidor NA máquina do Diretor antes de existir VPS — o passo de custo zero de `01-servidores-de-jogo.md` §5 | Componente do próprio Windows, R$ 0. **Preferir a Docker aqui**: mais leve, e o teste é "o binário Linux roda?", não "a imagem builda?" |
| Docker Desktop | G5, opcional | reproduzir o ambiente da VPS (systemd, rede) com fidelidade maior que WSL2 | Só se o WSL2 deixar dúvida. Gratuito para uso individual — verificar licença vigente |
| Cliente SSH/SCP | G5 | subir binário e operar a VPS | **Já vem no Windows 10** (OpenSSH nativo) — nada a instalar |
| **Blender** | **G3, condicional** | modelagem/rig manual de personagens e biomas | **Só se a rota procedural/low-poly autoral deixar de bastar** — o ROADMAP G3 põe procedural primeiro (custo zero) e geração por IA só com OK do Diretor. Não instalar por ansiedade: é aprendizado grande, e a decisão de rota é do Diretor |
| GitHub CLI (`gh`) ou só git remoto | quando CI entrar (`03` §5) | configurar Actions/Secrets sem sair do terminal | conveniência, não requisito |
| Ferramenta de captura de tela/vídeo do aparelho | G6 (ficha de loja) | capturas reais para a ficha | `adb shell screencap`/`screenrecord` **já resolvem** com o SDK atual — nada a instalar |

## 3. O que NÃO entra (e por quê registrar)

- **SDKs de analytics/anúncio/atribuição** — proibidos pela postura do projeto
  (GDD §9: "nenhuma coleta"; `docs/ANDROID.md` §3.2). Não é só política de
  produto: mantê-los fora é o que conserva o formulário de loja trivial.
- **Engines/launchers extras** (Unity Hub, Unreal) — o veículo é Godot 4.4,
  decidido no ROADMAP ("não discutir de novo").
- **Suítes de servidor** (kubectl, helm, terraform) — só existiriam para a
  Rota C de `01-servidores-de-jogo.md` §4, que está explicitamente marcada
  como overkill. Quando os gatilhos dela dispararem, instala-se em uma tarde.

## 4. Resumo em uma linha

Hoje: **zero instalações pendentes**. Próxima instalação prevista: **WSL2, no
dia em que o G5 abrir** — e nenhuma antes disso.
