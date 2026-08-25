#!/usr/bin/env bash
# godot/tests/run_all.sh — o PORTAO do jogo 3D: roda os 12 autotestes headless.
#
# Uso (de qualquer diretorio):
#   bash godot/tests/run_all.sh
#   GODOT_BIN=/caminho/para/godot bash godot/tests/run_all.sh
#
# POR QUE ESTE ARQUIVO EXISTE: ate' 25/08/2026 os 12 testes so' existiam como 12
# linhas de comando copiadas a mao. Copiar comando a mao e' como se esquece um
# teste — e um teste esquecido nao acusa nada, ele so' deixa de existir em
# silencio. Aqui a lista e' UMA, e quem acrescentar um selftest novo o
# acrescenta neste vetor, senao o CI nunca vai executa-lo.
#
# REGRA: qualquer teste que sair com codigo diferente de zero ENCERRA o script.
# Verde parcial nao e' verde. PROVADO em 25/08/2026: com um defeito real no 8o
# teste, o runner parou no 8o, saiu com codigo 1 e NAO imprimiu a linha final.
#
# ARMADILHA PARA QUEM ESCREVER SELFTEST NOVO: chamar quit(1) dentro de
# _initialize() faz o Godot sair com codigo ZERO — ele encerra antes do laco
# principal e o codigo se perde. Um teste escrito assim passaria calado por
# este portao. Falhe pelo caminho normal (contar falhas e quit(1) de dentro do
# _process), que e' o que os 12 fazem hoje.
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$ROOT/godot"

# Padrao "godot" (Linux/CI e quem tem no PATH). No Windows desta maquina o
# binario vive fora do PATH: caimos no MESMO caminho que build_apk.sh usa, para
# nao existirem duas verdades sobre onde o Godot esta'.
GODOT_BIN="${GODOT_BIN:-godot}"
# ATENCAO ao teste abaixo: use -f (arquivo comum), NUNCA -x. O projeto tem uma
# PASTA chamada godot/, e diretorio passa no teste -x porque "executavel" para
# um diretorio significa atravessavel. Com -x, este fallback nunca disparava e o
# runner morria com "godot: command not found" mesmo com o binario instalado.
if ! command -v "$GODOT_BIN" >/dev/null 2>&1 && [[ ! -f "$GODOT_BIN" ]]; then
	FALLBACK="${LOCALAPPDATA:-}/Programs/godot/Godot_v4.4.1-stable_win64.exe"
	if [[ -f "$FALLBACK" ]]; then
		GODOT_BIN="$FALLBACK"
	else
		echo "ERRO: Godot nao encontrado. Defina GODOT_BIN=/caminho/para/godot" >&2
		exit 1
	fi
fi

SELFTESTS=(
	"res://gameplay/selftest.gd"
	"res://gameplay/selftest_kits.gd"
	"res://gameplay/selftest_zona.gd"
	"res://gameplay/selftest_derrubado.gd"
	"res://gameplay/queda/selftest.gd"
	"res://ui/selftest.gd"
	"res://menu/selftest.gd"
	"res://characters/selftest.gd"
	"res://world/selftest.gd"
	"res://terrain/selftest.gd"
	"res://audio/selftest.gd"
	"res://juice/selftest.gd"
)

echo "ARKANA — portao de autotestes"
echo "  Godot:   $("$GODOT_BIN" --version 2>/dev/null | tail -1)"
echo "  Projeto: $PROJECT"
echo

TOTAL=${#SELFTESTS[@]}
N=0
for t in "${SELFTESTS[@]}"; do
	N=$((N + 1))
	echo "── [$N/$TOTAL] $t"
	"$GODOT_BIN" --headless --path "$PROJECT" --script "$t"
	echo
done

echo "ARKANA: ${TOTAL}/${TOTAL} selftests executados com sucesso."
