# Evidência de QA — Fulgurito (2026-11-19)

Ata `docs/conselho/2026-11-19-fulgurito.md` · ADR `docs/architecture/adr-0001-fulgurito.md` · branch `feat/fulgurito`.

| Campo | Valor |
|---|---|
| Commit / PR | ver PR draft `feat/fulgurito` (evidência gerada sobre a árvore deste commit) |
| Build | Unity Editor 6000.3.23f1 batchmode (testes) · Android APK de teste via `build_apk.ps1` (ver §APK) |
| Dispositivo / perfil | PC Windows 10, RTX 3050 6 GB Laptop, D3D12, 7,7 GB RAM; QualitySettings "Medium" (nível da qualidade Média do Config), vSync 0, sem teto de FPS. **Poco F4: não medido.** |
| Comando de teste | `powershell -File mobile-unity\portao.ps1 -Rapido` (EditMode, `-batchmode -nographics -runTests -testResults`) |
| Resultado | **568 passaram / 0 falharam / 1 ignorado de 569** (XML `-testResults`); `TerrainFulguritoTests` 8/8 |
| FPS medido | ver §FPS — o número de PC em batchmode **não mede render** → aceite de FPS = NÃO AVALIADO |
| Screenshot | `2026-11-19-fulgurito-duna.png` (câmera do jogador + HUD), `2026-11-19-fulgurito-perto.png` (1200×540, reduzidas de 2400×1080) |
| Observado por | executor da validação F6.2 (papel `qa-lead`) — 23 vidros verde-pálidos em três feixes sobre a praia seca da IlhaMestre, crosta âmbar na base, HUD íntegra, nada sobreposto ao personagem |
| Originalidade | nome e regra vêm do GDD §16.4 ("invenção nossa"); visual próprio (lâminas de areia fundida). `ip-originality-reviewer` **não executado** |
| Segurança | Portão não se aplica (sem rede/auth/IAP/dados) |

## Linha base e prova vermelha → verde (veredito pelo XML, nunca pelo exit code)
| Rodada | Árvore | total | passed | failed | skipped | TerrainFulguritoTests |
|---|---|---|---|---|---|---|
| Linha base | `origin/main` limpo | 561 | 559 | 1 | 1 | — |
| VERMELHO | testes + `Fulgurito` com 8 defeitos plantados | 569 | 559 | 9 | 1 | 0/8 (todos falharam) |
| VERDE (1) | implementação correta | 569 | 567 | 1 | 1 | 8/8 |
| VERDE (final) | + areia perguntada sob demanda (IlhaMestre) | 569 | 568 | 0 | 1 | 8/8 |

Defeitos plantados (um por teste): D1 `BloqueiaVisao` sempre false · D2 sem checar areia · D3 qualquer elemento funde ·
D4 sem estilhaço · D5 `Tick` nunca expira · D6 sem teto `GlassMax` · D7 forte ignorado · D8 `Reset` não limpa.
A falha pré-existente da linha base (`CharactersMagoContornoTests.MagoExterno_VesteOToonComContorno_SemPerderTexturaNemTinta`:
"a textura da Meshy nao veio na troca de shader") passou na rodada final sem mudança nossa — **instável/dependente de import**,
não relacionada ao Fulgurito. O ignorado é `CharactersCorpoMagoTests` ("todo o elenco ja' tem os takes novos").
A prova vermelha foi feita antes do refactor "areia sob demanda"; o refactor mudou só **onde** a areia é consultada, não as regras
cobradas (rodada final verde com os mesmos 8 testes).

## FPS (`Foto_Fulgurito_DunaComFps`, `foto.ps1 "Foto_Fulgurito"`, com GPU)
```
qualidade=Medium gpu=NVIDIA GeForce RTX 3050 6GB Laptop GPU api=Direct3D12 ilha=IlhaMestre vidros-ativos=23 (teto 24)
ANTES (sem vidro):  quadros=5382 fps-medio=1345,3 p50=0,71ms p95=0,91ms
DEPOIS (com vidro): quadros=5520 fps-medio=1379,9 p50=0,70ms p95=0,89ms
```
Leitura honesta: ~1.350 fps num mapa de 4,8 km indica que, em `-batchmode`, a câmera do jogo **não renderiza a cada quadro**
(só quando a foto chama `Camera.Render`). O par antes/depois mede o **custo de CPU** da lógica + casca (Δ dentro do ruído), **não**
o custo de GPU dos 23 vidros. FPS de render = **NÃO AVALIADO** (sem janela/build Windows com câmera ativa; Poco F4 sem medida).
Orçamento declarado no ADR: ≤ 48 draw calls, ~100 tris por vidro, sem sombra.

## APK (`build_apk.ps1` → `Unity -batchmode -nographics -buildTarget Android -executeMethod Arkana.EditorTools.Build.Android -quit`)
| Build | Árvore | Resultado | Tempo | Tamanho |
|---|---|---|---|---|
| 1º | antes do refactor da areia (Fulgurito inerte na IlhaMestre — defeito nº 1) | exit 0 | 13 min 46 s (com reimport após a troca de branch) | 302,1 MB |
| **final** | árvore deste PR | **exit 0**, sem "BUILD SUJOU O REPO" | **3 min 47 s** | **302,1 MB** (316.743.013 B) |

APK final: `mobile-unity/Builds/testes/arkana-2026-11-19_0107.apk` (fora do git), SHA-256
`2FE66EACF385A3849A67B1264E2C9E4AD2D3D44B45F50DB873F88A36FF6A8ADA`. **Não instalado em aparelho** (sem emulador nesta
rodada por RAM; Poco F4 sem cabo) → abrir no aparelho = NÃO AVALIADO.

## Passos reproduzíveis
1. `git switch feat/fulgurito`
2. `powershell -File mobile-unity\portao.ps1 -Rapido` → `ARKANA (rapido: so EditMode): 568 passaram, 0 falharam, 1 ignorados`
3. `powershell -File mobile-unity\foto.ps1 "Foto_Fulgurito"` → `Logs/fotos/f62-fulgurito-*.png` + `fulgurito-fps.txt`
4. `powershell -File mobile-unity\build_apk.ps1`

## Defeitos encontrados
| # | Severidade | Descrição | Status |
|---|---|---|---|
| 1 | S2 | Na IlhaMestre o `Ilha.Relevo` é nulo: a 1ª versão da casca caía no plano sem areia e o Fulgurito **não existiria no jogo** (os testes EditMode estavam verdes). Achado pela foto. | Corrigido (areia da IlhaMestre por altura) |
| 2 | S3 (fora do escopo) | Pelo mesmo motivo o `TerrenoReativoBehaviour` roda na IlhaMestre no **plano de fallback de 180 m** centrado na origem: fogo/gelo/lama/muro só existem perto do centro de um mapa de 4,8 km. | Aberto — reportar ao dono |
| 3 | S4 | Leitura: as lâminas saem verde-água claras e pontudas — perto do "cristal azul-gelo" que o CD vetou na Rodada 1; crosta hexagonal chapada aparece flutuando/cortada em declive. | Aberto — revisão do CD |
| 4 | S4 | Vidro ainda não para projétil em voo (fora do escopo pela ata). | Aberto (próxima leva) |
