#!/usr/bin/env bash
# ============================================================================
# ARKANA — roda os testes do jogo Roblox FORA do Studio (raia 3.6: QA).
#
#   ./roblox/tools/run.sh              # boot + todos os selfTest + partida
#   ./roblox/tools/run.sh 900          # partida com teto de 900s simulados
#
# POR QUE UM BUNDLE: o CLI do luau dá um ambiente global SEPARADO e SOMENTE
# LEITURA para cada arquivo requerido, e não tem `io` para ler fonte. Então o
# jeito de injetar `game`/`script`/`Instance` nos módulos do projeto é juntar
# tudo num arquivo só e carregar cada módulo com loadstring+setfenv — que é,
# por acaso, exatamente o que o Roblox faz com ModuleScript.
# ============================================================================
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROBLOX="$(dirname "$HERE")"
SRC="$ROBLOX/src"
OUT="$HERE/.build"
BUNDLE="$OUT/bundle.luau"

LUAU="${LUAU:-$LOCALAPPDATA/Programs/luau/luau.exe}"
if [ ! -x "$LUAU" ]; then
  LUAU="$(command -v luau || true)"
fi
if [ -z "$LUAU" ] || [ ! -e "$LUAU" ]; then
  echo "luau não encontrado. Instale ou aponte LUAU=/caminho/luau.exe" >&2
  exit 127
fi

mkdir -p "$OUT"

# Nível de colchete longo: o código do projeto usa ']]' (índice de tabela), mas
# nunca ']===]'. Se um dia usar, este teste avisa em vez de gerar bundle quebrado.
if grep -rq ']===]' "$SRC" "$HERE"/*.luau; then
  echo "fonte contém ']===]' — suba o nível do colchete longo no run.sh" >&2
  exit 1
fi

emit() {  # emit <chave> <arquivo>
  printf 'S["%s"] = [===[\n' "$1" >> "$BUNDLE"
  cat "$2" >> "$BUNDLE"
  printf '\n]===]\n' >> "$BUNDLE"
}

: > "$BUNDLE"
printf -- '-- GERADO POR roblox/tools/run.sh — NÃO EDITAR\nlocal S = {}\n' >> "$BUNDLE"
printf 'S.__maxSeconds = "%s"\n' "${1:-600}" >> "$BUNDLE"

for area in shared server client; do
  for f in "$SRC/$area"/*.luau; do
    [ -e "$f" ] || continue
    emit "$area/$(basename "$f")" "$f"
  done
done
emit "tools/harness.luau" "$HERE/harness.luau"
emit "tools/run.luau" "$HERE/run.luau"

cat >> "$BUNDLE" <<'LUA'
local H = assert(loadstring(S["tools/harness.luau"], "@roblox/tools/harness.luau"))(S)
assert(loadstring(S["tools/run.luau"], "@roblox/tools/run.luau"))(H, S)
LUA

exec "$LUAU" "$BUNDLE"
