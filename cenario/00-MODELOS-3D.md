# CENÁRIO — os modelos 3D do Meshy (26/08/2026, fim de tarde)

**Todos gerados no SITE, na conta do Diretor** (política DIRECAO §10.2 — os
processos visíveis na galeria dele), Multi-View com as vistas ortogonais dos
concepts aprovados. **Revisados NO VIEWER antes de baixar** — regra nova do
Diretor após ele mesmo pegar dois defeitos que eu ia deixar passar.

## O placar

| Peça | Nome Meshy | Tris | Revisão 3D | Arquivo em `origem/` |
|---|---|---:|---|---|
| Castelo | Aetherstone Citadel | 1,90 M | ✅ | `castelo-site-hipoly.glb` (+ backup API 30k) |
| Luva Comum | Emberhand Gauntlet | 1,99 M | ✅ runa/brasas/remendos; palma lisa é FIEL (runa de tinta só acende conjurando) | `luva-comum-site-hipoly.glb` |
| Luva Conjurador | Celestial Sovereign G. | 2,00 M | ✅ **v2** — v1 REPROVADA pelo Diretor (gema extra no anelar + palma lisa); v2 corrigiu a geometria (4 cristais exatos) | `luva-conjurador-v2-site-hipoly.glb` (v1 mantida p/ comparação) |
| Manopla | Gemini Gauntlet | 1,99 M | ✅ 2 gemas fogo+vento, selo no antebraço, polegar de ouro | `manopla-site-hipoly.glb` |
| Baú | Gemforged Treasure Ch. | 1,98 M | ✅ Selo+gemas na frente, dobradiças, runas | `bau-site-hipoly.glb` |

**Créditos do dia: 210** (7 gerações × 30) · saldo **2.984**.

## Ressalva registrada — a palma da Conjurador

Duas gerações seguidas ignoraram o **bordado dourado da palma** (a IA de
textura "limpa" a palma). Terceira loteria seria desperdício: **o bordado
entra como DECAL no albedo na passada do Blender**, determinístico e de graça,
junto da decimação que já é obrigatória. A palma quase não aparece em jogo
(terceira pessoa), então não bloqueia integração.

## A esteira do Blender (próxima fase — Blender já instalado)

Para CADA peça: decimar do hi-poly (~2 M) ao alvo de jogo — castelo ~30 k,
luvas ~3–6 k, baú ~8 k — com bake de normal do hi-poly; conjurador ganha o
decal da palma; exportar GLB de jogo para `godot/world/modelos/` (este SIM em
git/LFS); colisão simples à mão (nunca trimesh de IA); FPS no aparelho.
O `castelo-api-30k.glb` é candidato a base de retopo do castelo.
