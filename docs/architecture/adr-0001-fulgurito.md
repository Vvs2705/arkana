# ADR-0001: Fulgurito — raio funde a areia em vidro frágil

## Status

Proposed

> Quem pode mover para `Accepted`: o dono (usuário), ou o `technical-director` com a confirmação explícita dele
> (regra do template `templates/ADR.md`). O Conselho decidiu por consenso (ata abaixo), mas o dono ainda não
> confirmou — por isso fica `Proposed` até a revisão do PR draft. (O `technical-director.md` diz "Status → Accepted"
> por conta própria; o template diz o contrário — contradição registrada na validação F6.2.)

## Date

2026-11-19

## Last Verified

2026-11-19

## Decision Makers

Conselho (creative-director, technical-director, producer) — ata `docs/conselho/2026-11-19-fulgurito.md`.

## Summary

O GDD §16.4 prevê o Fulgurito (Raio na areia vira cobertura de vidro frágil) e nada no jogo o implementa. Decidido:
classe pura nova `Arkana.Terrain.Fulgurito` ouvindo `Bus.TerrainHit`, com casca `FulguritoBehaviour` e teto duro de 24 vidros,
sem tocar no `TerrenoReativo`.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.3.23f1 (URP) |
| **Domain** | Core (gameplay de terreno) + Rendering (malha procedural, Unlit) |
| **Layer** | Feature |
| **Knowledge Risk** | LOW — só APIs já usadas no projeto (`Mesh.SetVertices`, `MaterialVfx.Solido`, `Bus`) |
| **References Consulted** | `mobile-unity/ARQUITETURA.md`, `Scripts/Terrain/TerrenoReativo.cs`, `Scripts/Gameplay/Habilidades/VisualDosKits.cs` (`MaterialVfx`), `Editor/Build.cs` (`ShadersDoCodigo`) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Shader Unlit transparente presente no APK (lista `ShadersDoCodigo`); FPS no Poco F4 |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | Projétil parar no vidro (`BloqueiaTiro`), Grimório "descobriu o Fulgurito", som de estilhaço |
| **Blocks** | None |
| **Ordering Note** | Não colide com o PR #10 (`perf/arvores-instanciadas`): nenhum arquivo em comum |

## Context

### Problem Statement
O terreno reativo (pilar nº 2) cobre fogo, gelo, raio na água, lama, muro e vento. A areia (biomas `Dunas` e `Praia`) não
reage a nada: o Raio lá não faz efeito. O GDD §16.4 já decidiu a regra; faltava o código.

### Current State
`TerrenoReativo` classifica a areia como `TipoCelula.Chao`; `AplicarRaio` só age em água conectada.

### Constraints
- Lógica pura + casca (`ARQUITETURA.md`); números em `Balance`.
- Cena de partida já perto do limite (§0 do GDD: ~2.000 batches na descida).
- Só shaders da lista `Build.ShadersDoCodigo` por `Shader.Find`.

### Requirements
- Só Raio cria; só em areia; forte = disco 3×3 (mesmo `RaioForte` do muro).
- Qualquer impacto numa célula de vidro estilhaça (1 tiro destrói) e não refunde no mesmo impacto.
- Expira em `GlassDuration` (8 s); teto `GlassMax` (24) — cheio, **recusa** (nunca troca o mais velho).
- Tapa a visão (`BloqueiaVisao`).

## Decision

Classe nova `Fulgurito` (grade com a mesma `Balance.Terrain.CellSize`), ligada ao `Bus.TerrainHit` no construtor,
`Desligar()` no `OnDestroy`, `Reset()` no `Bus.MatchStarted`. Casca `FulguritoBehaviour` nasce no passo "terreno" do `Main`
(uma linha), decide o que é areia pela ilha da cena — `Bioma.Dunas/Praia` na ilha procedural; na **IlhaMestre** (a ilha do
jogo hoje, `Main.UsarIlhaMestre = true`, onde `Ilha.Relevo` é nulo) chão seco entre `Relevo.PraiaY` (1,4 m) e 3,75 m, a faixa
onde a camada "areia" do splat pesa mais que metade — e pergunta **só na célula atingida** (pré-classificar 1.600×1.600
células custaria segundos de carregamento). Monta um pool de 24 objetos (lâminas alfa verde-pálido + crosta âmbar), sem sombra, zero alocação por quadro.

### Key Interfaces
```csharp
public sealed class Fulgurito {
  Fulgurito(float lado, Func<float,float,bool> ehAreia);
  void Reagir(Elemento el, Vector3 pos, bool forte); void Tick(float dt); void Reset(); void Desligar();
  int Ativos; bool TemVidro(Vector3); bool BloqueiaVisao(Vector3); IReadOnlyList<VidroVisivel> Visiveis;
}
Balance.Terrain.GlassDuration = 8f; GlassMax = 24; GlassHeight = 2.4f;
```

### Implementation Guidelines
- Must never editar `TerrenoReativo` para isto; must ouvir o mesmo `Bus.TerrainHit`.
- Must never alocar no `Update` da casca; must usar `MaterialVfx` (Unlit da lista de shaders do build).
- Teste novo must ser visto vermelho com o defeito antes de contar.

## Alternatives Considered

### Alternative 1: novo `TipoCelula.Areia` + `EstadoCelula.Vidro` dentro do `TerrenoReativo`
- **Pros**: uma grade só; `BloqueiaTiro` já existe lá.
- **Cons**: mexe no sistema do pilar (546 linhas, fogo por orçamento medido), no `VisualDoTerreno` (758 linhas) e na classificação de biomas — regressão provável.
- **Rejection Reason**: risco alto para uma mecânica pequena; pode migrar depois se o vidro provar valor.

### Alternative 2: vidro como `GameObject` com colisor físico
- **Pros**: bloqueia tiro e corpo de graça.
- **Cons**: lógica na física (não testável em EditMode), emparedar jogador.
- **Rejection Reason**: viola a regra lógica-pura; o tiro fica para leva própria.

## Consequences

### Positive
- Areia passa a reagir; Raio ganha uso fora da água; 8 testes novos.
### Negative
- Duas grades lógicas sobre a ilha (memória: `bool[N*N]`, ~N² bytes — desprezível).
- Nesta leva o vidro ainda **não para projétil em voo** (só tapa a visão e quebra com impacto).
### Neutral
- Muro de Terra e vidro podem coexistir na mesma célula (sistemas independentes).

## Risks
- Shader alfa ausente no APK → `MaterialVfx.Solido` devolve fallback Unlit; mitigação: já usado por outros VFX no build.
- Leitura na duna clara → foto de evidência obrigatória (CD).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| GDD.md §16.4 | "Fulgurito: Raio em área de areia → vitrifica: cobertura de vidro frágil (1 tiro destrói, mas bloqueia visão)" | `Fulgurito.Reagir` + `BloqueiaVisao` + estilhaço em qualquer impacto |
| GDD.md §14 | terreno previsível e aprendível | regras determinísticas (sem sorteio), teto que recusa |
| GDD.md §19.2 | 60 fps intermediário | teto 24 vidros (≤ 48 draw calls), sem sombra |

## Performance Implications
- **CPU**: `Tick` O(vidros vivos ≤ 24); `Update` itera 24 transforms.
- **Memória**: grade `bool` (~N² B) + 2 malhas minúsculas (~40 vértices).
- **Render**: até 48 draw calls, ~100 triângulos por vidro.
- **Network**: n.a.

## Migration Plan
Aditivo. **Rollback**: remover a linha do `Main.cs` (o vidro deixa de nascer) ou reverter o commit.

## Validation Criteria
`TerrainFulguritoTests` 8/8 verde e vistos vermelhos; suíte EditMode sem regressão (XML); APK gerado; foto + FPS antes/depois
em `production/qa/evidence/2026-11-19-fulgurito.md`; Poco F4 = pendente.

## Related
- Ata: `docs/conselho/2026-11-19-fulgurito.md`
- Código: `mobile-unity/Assets/_Arkana/Scripts/Terrain/Fulgurito.cs`, `FulguritoBehaviour.cs`; testes `Tests/EditMode/TerrainFulguritoTests.cs`
