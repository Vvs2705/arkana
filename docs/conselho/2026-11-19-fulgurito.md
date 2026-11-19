# Ata do Conselho — Fulgurito (raio funde a areia em vidro) (2026-11-19)

> Arquivo: `docs/conselho/2026-11-19-fulgurito.md` · Aberta por: `producer`
> Estado: DESPACHADA (revisão pós-código pendente: PR draft, não mergear sem o dono)

> **Como esta ata foi feita (validação F6.2 do Time de Jogos).** O ARKANA ainda **não** tem o time instalado
> (`install_time.py` não foi rodado — decisão separada), então os subagentes `creative-director`, `technical-director`
> e `producer` não existem como `subagent_type` neste repo. As posições das Rodadas 1 e 2 abaixo foram escritas pelo
> executor **lendo e seguindo estritamente** cada definição em
> `TIME DE JOGOS/time-jogos/.claude/agents/<diretor>.md` (§5.1 Modo A: Riscos · Abordagem · Veto · Critério de aceite;
> réplica `concorda / discorda / ajuste mínimo`; nunca 3ª rodada). Não são saídas de três modelos independentes — o
> desacoplamento de opinião que o Conselho promete **não foi testado** aqui. Registrado como defeito na validação.

## 0. Entrada (copiada do pedido, sem reescrever)
- **Pedido original:** "Council Modo A: ata docs/conselho/2026-11-19-<slug>.md for ONE small mechanic, e.g. a spell with a terrain effect (original name/visual) [...] Implement minimal: pure C# logic class + thin MonoBehaviour; EditMode NUnit tests [...]"
- **Jogo / repositório:** ARKANA (`Vvs2705/arkana`, `mobile-unity/`, Unity 6000.3.23f1) · **Branch alvo:** `feat/fulgurito` (de `origin/main` `c37f778`)

## 1. Enquadramento (producer, máx. 10 linhas)
- **Objetivo:** implementar o **Fulgurito** do GDD §16.4 ("Raio em área de areia → vitrifica: cria cobertura de vidro frágil (1 tiro destrói, mas bloqueia visão)") como efeito de terreno, lógica pura + casca.
- **Restrições:** Android/Poco F4 (meta 45 fps estáveis, GDD §19.2 pede 60), orçamento de partida já no limite (§0: descida ≤ 2,3 M tris, ~2.000 batches); 1 Unity por vez, 7,7 GB RAM; lógica pura + `MonoBehaviour` casca (`ARQUITETURA.md`); fogo/terreno por regra previsível (§14); custo zero; monetização: n.a.; originalidade: nome e visual próprios (o GDD já marca o Fulgurito como "invenção nossa").
- **Critério de pronto (testável):** `TerrainFulguritoTests` (EditMode) vermelho com defeitos plantados e verde com a implementação; suíte EditMode inteira sem regressão (XML do `-testResults`); APK de teste gerado por `build_apk.ps1` sem sujar o repo; foto do vidro na duna + FPS antes/depois no PC (perfil declarado) em `production/qa/evidence/`.
- **Fora de escopo:** projétil em voo parar no vidro (`BloqueiaTiro` no `Projetil`), bot/IA usar o vidro, som próprio, linha no Grimório, Sintonia com vidro, partículas de estilhaço, medida no Poco F4.

## 2. Rodada 1 — posições independentes
### creative-director (seguindo `creative-director.md` §5.1)
- **Riscos de visão:** (1) virar "mais um muro" — o muro de Terra já é cobertura; se o vidro não tiver identidade própria o terreno fica genérico. (2) Leitura em 6": vidro transparente demais some na areia clara das dunas; opaco demais vira "caixote". (3) Clone: cristal azul-gelo lembra kits de outros jogos; o nosso é **areia fundida** (verde-pálido com crosta âmbar), ciência real (§16.4).
- **Abordagem recomendada:** menor versão que serve aos pilares nº 2 (terreno reativo) e "conhecimento é poder" (§16.4): só Raio, só areia, frágil (qualquer impacto quebra), temporário. *Experience target:* "o jogador dispara Raio na duna, vê um feixe de lâminas de vidro brotar do chão em ¼ s e se esconde atrás; o primeiro tiro inimigo estilhaça e ele fica exposto".
- **Veto:** vidro que bloqueia **para sempre** ou que aguenta vários tiros (vira muro e apaga a diferença); visual azul-gelo/cristal genérico; qualquer venda (pele de vidro paga com vantagem).
- **Critério de aceite criativo:** foto do build (não descrição) do vidro sobre a areia, com HUD, câmera do jogador; silhueta legível contra a duna.

### technical-director (seguindo `technical-director.md` §5.1)
- **Riscos técnicos:** (1) mexer no `TerrenoReativo` (546 linhas, fogo por orçamento provado) arrisca regressão no pilar — preferir classe **nova** que ouve o mesmo `Bus.TerrainHit`. (2) Custo de render: cada vidro = 2 draw calls (lâminas alfa + crosta); sem teto, 100 raios na duna = 200 batches a mais numa cena que já tem ~2.000 → **teto duro `GlassMax` = 24** (≤ 48 batches, ~0,5 K tris). (3) Shader: só `MaterialVfx` (Unlit da lista `Build.ShadersDoCodigo`) — `Shader.Find` fora dela é null no aparelho. (4) Alocação por quadro no `Update`.
- **Abordagem recomendada:** `Arkana.Terrain.Fulgurito` puro (grade da mesma `CellSize`, `Reagir/Tick/Reset/Desligar`, `Visiveis` para a cena), números em `Balance.Terrain` (`GlassDuration`, `GlassMax`, `GlassHeight`), casca `FulguritoBehaviour` com pool criado 1x, nascendo no passo "terreno" do `Main` ao lado do `TerrenoReativoBehaviour`.
- **Veto:** lógica no `MonoBehaviour`; editar o `TerrenoReativo`; pacote novo; shader por nome fora da lista; teste que nunca ficou vermelho; veredito pelo exit code.
- **Critério de aceite técnico:** XML do `-testResults` com `failed=0` e os testes novos vistos vermelhos (log citado); FPS antes/depois na mesma cena (PC como proxy declarado, Poco = NÃO AVALIADO); `build_apk.ps1` sem "BUILD SUJOU O REPO".

### producer (seguindo `producer.md` §5.1)
- **Riscos de escopo/prazo:** Unity serializado (portão → vermelho → verde → build → foto ≈ 5 aberturas do Unity); o PR #10 (`perf/arvores-instanciadas`, draft) toca o mundo — não pode haver colisão de arquivo. PlayMode completo não cabe na leva (nem é exigido pelo pedido).
- **Sequência recomendada:** ata → ADR → baseline do portão (EditMode) → código com defeitos plantados (vermelho) → código certo (verde) → APK → foto+FPS → evidência → PR draft.
- **Veto:** bloquear projétil em voo na mesma leva (mexe em `Projetil`, outro sistema); som/Grimório/IA juntos ("3 sistemas numa leva").
- **Critério de aceite de escopo:** o PR entrega só o §1; arquivos novos + 3 toques pequenos (`Balance.cs`, `Main.cs`, `FotoTests.cs`); nenhum arquivo do PR #10.

## 3. Rodada 2 — réplicas (última rodada)
### creative-director → TD / PR
- TD: **concorda** com teto 24 e classe nova; **ajuste mínimo:** teto cheio deve **recusar** novo vidro (o jogador aprende o limite), nunca apagar o mais velho em silêncio.
- PR: **concorda** em deixar projétil/som/Grimório fora; exige que a foto mostre o vidro na **duna** (bioma certo).

### technical-director → CD / PR
- CD: **concorda** — "recusar quando cheio" é também determinístico (melhor para a rede futura). Cor verde-pálido/âmbar sem custo extra (Unlit chapado).
- PR: **concorda**; **ajuste mínimo:** `Main.cs` ganha uma linha só, no passo "terreno" já existente (sem passo novo de carregamento).

### producer → CD / TD
- **Concorda** com ambos; incorpora "recusar quando cheio" e "uma linha no Main" ao despacho. Sem 3ª rodada.

## 4. Decisão
- **Consenso?** sim (ajustes da Rodada 2 incorporados).
- **Desempate por domínio aplicado:** não foi necessário. (Registro: o visual é do CD, o teto/arquitetura do TD, a sequência do producer.)
- **Escalar ao usuário?** não — não há empate de visão (o Fulgurito já está no GDD §16.4).
- **ADR:** `docs/architecture/adr-0001-fulgurito.md` (Status `Proposed` — ver nota no ADR).

## 5. Despacho imediato
| # | Raia | Agente dono | Entrega | Arquivos (raia exclusiva) | Depende de | Unity? |
|---|---|---|---|---|---|---|
| 1 | Lógica pura + números | `gameplay-programmer` (via `lead-programmer`) | `Fulgurito.cs`, `Balance.Terrain.Glass*` | `Scripts/Terrain/Fulgurito.cs`, `Scripts/Core/Balance.cs` (3 linhas) | — | n |
| 2 | Testes EditMode + prova vermelha | `qa-lead` → `qa-tester` | `TerrainFulguritoTests.cs`, XML vermelho e verde | `Tests/EditMode/TerrainFulguritoTests.cs` | 1 | s |
| 3 | Casca + visual + fiação | `unity-specialist` | `FulguritoBehaviour.cs`, 1 linha no `Main.cs` | `Scripts/Terrain/FulguritoBehaviour.cs`, `Scripts/Main.cs` | 1 | n |
| 4 | APK de teste | `build-ci-engineer` | APK via `build_apk.ps1` | `Builds/` (fora do git) | 2, 3 | s |
| 5 | Foto + FPS antes/depois | `mobile-performance-specialist` + `qa-lead` | `Foto_Fulgurito_Duna`, evidência | `Tests/PlayMode/FotoTests.cs` (1 teste no fim), `production/qa/evidence/` | 3, 4 | s |

Regra: máx. 3 subagentes por leva, 1 Unity por vez. **Nesta validação as raias foram executadas em série pelo próprio executor** (o time não está instalado no ARKANA), seguindo o contrato de raia.

## 6. Escalada ao usuário (só visão do jogo)
> Não houve.

## 7. Revisão pós-código (PR draft)
| Diretor | Evidência avaliada | Veredito | Ciclo (1–3) |
|---|---|---|---|
| technical-director | código, testes, perf | NÃO AVALIADO (não executado — Modo B fora do escopo desta validação; PR fica draft para o dono) | — |
| creative-director | screenshot vs brief e ADR | NÃO AVALIADO (não executado) | — |
| producer | escopo, critério de pronto | NÃO AVALIADO (não executado) | — |
