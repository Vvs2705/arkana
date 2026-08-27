#!/usr/bin/env bash
# A ESTEIRA COMPLETA de um mago, da malha ja' gerada ate' o .glb que o jogo veste.
#
# Uso:  bash tools/meshy/vestir.sh 03-veu [04-corvus ...]
#
# O QUE ELA FAZ, e por que cada passo existe:
#   1. riggar (API, 5 creditos) — o esqueleto. A API so' devolve walking/running.
#   2. FUNDIR os clipes do brok.glb (gratis) — o rig da Meshy entrega o MESMO
#      esqueleto de 24 ossos para todo mundo (conferido em 27/08), entao as acoes
#      do Brok retargetam por nome de osso. Sem este passo o modelo NAO ENTRA NO
#      JOGO: Mage.REQUIRED_ANIMS exige idle/run/cast e sem os tres o Mage rejeita
#      o modelo externo e cai no mago procedural — o personagem sumiria calado.
#   3. instalar como <nome>.glb, que e' o que Mage.model_path_for() procura
#      (ele corta o "NN-" do slug).
set -euo pipefail
RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# DE ONDE VEM a malha gerada. Existe porque o atelie de arte
# (personagens/*/arte/_originais/) NAO vai para o git — 307 MB, e' materia-prima
# — entao `meshy.py` so' roda no clone principal, e e' la' que a malha nasce.
# Quem funde e instala pode ser um worktree. Um env var resolve os dois lados
# sem duplicar arquivo entre repositorios.
ORIGEM="${ORIGEM:-$RAIZ/godot/characters/modelos}"
BLENDER="${BLENDER:-/c/Program Files/Blender Foundation/Blender 5.2/blender.exe}"
DOADOR="$RAIZ/godot/characters/modelos/brok.glb"
TMP="${TMPDIR:-/tmp}/arkana-vestir"
mkdir -p "$TMP"

for slug in "$@"; do
	nome="${slug#*-}"
	dir="$ORIGEM/$slug"
	echo "== $slug -> $nome.glb"
	if [[ ! -f "$dir/$slug.glb" ]]; then
		echo "   PULADO: $dir/$slug.glb nao existe (rode 'meshy.py gerar $slug')" >&2
		continue
	fi
	if [[ ! -f "$dir/${slug}_rigged.glb" ]]; then
		echo "   PULADO: falta ${slug}_rigged.glb (rode 'meshy.py riggar $slug' no clone principal)" >&2
		continue
	fi
	cp "$dir/${slug}_rigged.glb" "$TMP/rig.glb"
	cp "$DOADOR" "$TMP/doador.glb"
	"$BLENDER" --background --python "$RAIZ/tools/blender/fundir_animacoes.py" -- \
		"$TMP/rig.glb" "$TMP/doador.glb" "$TMP/final.glb" manter 2>&1 | grep -E "FUNDIDO|Error" || true
	cp "$TMP/final.glb" "$RAIZ/godot/characters/modelos/$nome.glb"
	echo "   OK: godot/characters/modelos/$nome.glb"
done
