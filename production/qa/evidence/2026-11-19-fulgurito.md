# Evidência de QA — Fulgurito (2026-11-19)

Ata `docs/conselho/2026-11-19-fulgurito.md` · ADR `docs/architecture/adr-0001-fulgurito.md` · branch `feat/fulgurito` · PR #11.

## Ciclo 2 (correção do Modo B ciclo 1 — `TIME DE JOGOS/time-jogos/docs/conselho/2026-11-19-arkana-pr11-modo-b.md`)

| Campo | Valor |
|---|---|
| Commit do código avaliado | **`ec007c1`** (fotos, FPS e suíte rodados sobre esta árvore; os commits seguintes só mexem em docs/evidência) |
| Build | Unity Editor 6000.3.23f1 — testes `-batchmode -nographics`; fotos `foto.ps1` (`-batchmode` **sem** `-nographics`: a GPU desenha) |
| Dispositivo / perfil | PC Windows 10, RTX 3050 6 GB Laptop, D3D12, 7,7 GB RAM; QualitySettings "Medium", vSync 0, sem teto de FPS (restaurados no `finally`). **Poco F4: não medido.** |
| EditMode local (XML `Logs/fulg-verde-final.xml`) | **573 passaram / 0 falharam / 1 ignorado de 574** (`CharactersCorpoMagoTests`: "todo o elenco já tem os takes novos"); Fulgurito **13/13** |
| CI (GitHub Actions, GameCI) | run `44577599879` **verde**: 533 · 528 passaram · 0 falharam · 5 ignorados (`VisualPecasMeshyTests`: .glb é ponteiro LFS no CI). Categoria `SoNoPC` (41 testes que leem LFS + 1 teto de 60 ms) fica no `portao.ps1` |
| Screenshots (1200×540, reduzidas de 2400×1080) | `2026-11-19-fulgurito-praia-inimigo.png` (câmera do jogador + HUD, **boneco 2 m atrás do vidro**), `2026-11-19-fulgurito-praia-tiro.png` (o mesmo quadro depois de 2 impactos de Fogo: o vão abre e o boneco aparece), `2026-11-19-fulgurito-perto.png` |
| Observado por | `qa-lead` (executor) — 23 vidros em três feixes na praia seca da IlhaMestre; boneco só com a cabeça aparecendo entre as pontas antes do tiro, de corpo inteiro depois; HUD íntegra |
| Originalidade | `ip-originality-reviewer` — ver §Originalidade |
| Segurança | Portão não se aplica (sem rede/auth/IAP/dados) |

### Prova vermelha → verde dos bloqueantes (veredito pelo XML, nunca pelo exit code)
| Rodada | Árvore | total | passed | failed | Quem falhou |
|---|---|---|---|---|---|
| VERMELHO-a (`Logs/fulg-vermelho-a.xml`, filtro `Fulgurito`) | testes novos + `Fulgurito` do ciclo 1 (sem a regra) + `AreiaDaMestre` com defeito plantado (sem água, sem piso, `<` no teto) | 13 | 9 | **4** | `RaioForteEmSequencia_NaoEstilhacaOProprioVidro` (Estilhacados = 1), `ChaoSecoNaFaixaDePraia_EhAreia`, `ChaoMolhado_NaoEhAreia`, `ForaDaFaixaDeAltura_NaoEhAreia` |
| VERMELHO-b (`Logs/fulg-vermelho-b.xml`) | regra nova com 2 defeitos plantados: D10 "nascendo ignora qualquer elemento", D11 "vidro velho nunca quebra" | 13 | 11 | **2** | `VidroNascendo_OutroElementoAindaEstilhaca`, `QualquerImpactoNoVidro_EstilhacaComUmTiro` |
| VERDE (`Logs/fulg-verde-final.xml`, suíte inteira) | implementação correta | 574 | 573 | **0** | — |

- **Bloqueante 1 (LP nº 1 / TD nº 1)** — vidro com vida < `Balance.Terrain.GlassNascer` (0,25 s, a mesma janela da subida na casca) **ignora Raio**; o disco forte segue fundindo o resto. 3 Raios fortes a 3 m no mesmo quadro → faixa contínua de 15 vidros, 0 estilhaçados (antes: 1 estilhaçado e buraco). Fogo/Água/Terra/Vento estilhaçam vidro nascendo (1 tiro destrói continua valendo).
- **Bloqueante 2 (LP nº 2 / TD nº 2)** — `FulguritoBehaviour.AreiaDaMestre(altura, superficieDaAgua)` pura + `TerrainFulguritoAreiaTests` (3 testes: faixa seca com bordas inclusas, molhado, fora da faixa/NaN).
- **Bloqueante 3 (LP nº 3 / TD nº 3)** — `Foto_Fulgurito_PraiaComFps` guarda nível de qualidade, `vSyncCount` e `targetFrameRate` e os restaura em `try/finally` (o `MedirRender` também devolve o `targetTexture` no `finally`).

### FPS (`Foto_Fulgurito_PraiaComFps`, `foto.ps1 "Foto_Fulgurito"`, com GPU) — `2026-11-19-fulgurito-fps.txt`
```
qualidade=Medium gpu=NVIDIA GeForce RTX 3050 6GB Laptop GPU api=Direct3D12 ilha=IlhaMestre vidros-ativos=23 (teto 24) tapa-a-linha=True estilhacados-pelo-tiro=2
ANTES (sem vidro):  fps-medio=1891,4 p50=0,52ms p95=0,63ms            <- CPU (a câmera não desenha por quadro em -batchmode)
DEPOIS (com vidro): fps-medio=2031,5 p50=0,48ms p95=0,60ms
RENDER FORCADO ANTES (Camera.Render 2400x1080 por quadro): fps-medio=155,9 p50=5,77ms p95=6,85ms
RENDER FORCADO DEPOIS:                                     fps-medio=167,0 p50=5,70ms p95=6,86ms
```
Leitura: com a câmera do jogador **desenhando a cena a cada quadro** (proxy de GPU de PC), p95 6,85 → 6,86 ms com 23 vidros (+0,1 %, dentro do
orçamento do ADR: nenhuma piora > 5 % no p95). **Não é o aparelho:** player Windows com janela não foi gerado e o Poco F4 segue sem medida
(sem cabo) → FPS no alvo mobile = **NÃO AVALIADO**.

### Originalidade
Ver §7 da ata (`ip-originality-reviewer`, ciclo 2).

## Ciclo 1 (histórico — commit `fcb95e3`)
| Rodada | Árvore | total | passed | failed | skipped |
|---|---|---|---|---|---|
| Linha base | `origin/main` limpo | 561 | 559 | 1 | 1 |
| VERMELHO | 8 testes + `Fulgurito` com D1–D8 plantados | 569 | 559 | 9 | 1 |
| VERDE (final) | implementação do ciclo 1 | 569 | 568 | 0 | 1 |

D1 `BloqueiaVisao` sempre false · D2 sem checar areia · D3 qualquer elemento funde · D4 sem estilhaço · D5 `Tick` nunca expira ·
D6 sem teto `GlassMax` · D7 forte ignorado · D8 `Reset` não limpa. APK do ciclo 1: `build_apk.ps1` exit 0, 302,1 MB, sem "BUILD SUJOU O REPO"
(SHA-256 `2FE66EACF385A3849A67B1264E2C9E4AD2D3D44B45F50DB873F88A36FF6A8ADA`); **APK não regerado no ciclo 2** (o build Android ficou fora do CI
por decisão de minutos/disco; a mudança do ciclo 2 é código C# + malha procedural, sem shader novo — `Particles/Unlit` já está em
`Build.ShadersDoCodigo`).

## Passos reproduzíveis
1. `git switch feat/fulgurito`
2. `powershell -File mobile-unity\portao.ps1 -Rapido` → EditMode inteiro (573/0/1)
3. `powershell -File mobile-unity\foto.ps1 "Foto_Fulgurito"` → `Logs/fotos/f62-fulgurito-{praia-inimigo,praia-tiro,perto}.png` + `fulgurito-fps.txt`

## Defeitos encontrados
| # | Severidade | Descrição | Status |
|---|---|---|---|
| 1 | S2 | Ciclo 1: Fulgurito inerte na IlhaMestre (`Ilha.Relevo` nulo). | Corrigido no ciclo 1; regra agora com EditMode (ciclo 2) |
| 2 | S3 (fora do escopo) | `TerrenoReativoBehaviour` na IlhaMestre roda no plano de fallback de 180 m centrado na origem. | Aberto — reportar ao dono |
| 3 | S4 | Ciclo 1: vidro lia como gelo azul e não tapava. | Corrigido no ciclo 2 (cor/forma/alfa) — julgamento do CD na §7 da ata |
| 4 | S3 | Suprema do Fizz estilhaçava o próprio vidro (vidro/buraco alternado). | Corrigido no ciclo 2 (`GlassNascer`) |
| 5 | S4 | `Pawn.Aterrar` num corpo **já pousado** devolve a `Queda.Pos` antiga (o boneco voltava ao spawn). Só afetou a foto; contornado no teste. | Aberto — fora do escopo, reportar |
| 6 | S4 | Vidro ainda não para projétil em voo (fora do escopo pela ata). | Aberto (próxima leva) |
| 7 | S4 | A crosta nova quase não aparece na foto (alfa baixo na borda + areia clara): sem borda dura, mas também sem leitura de "areia queimada". | Aberto — julgamento do CD |
