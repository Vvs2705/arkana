#!/usr/bin/env bash
# godot/export/build_apk.sh — dono: raia MUNDO (R16, G0).
# Gera build/arkana3d.apk (debug, arm64) via Godot headless.
# Uso:  bash godot/export/build_apk.sh          (de qualquer diretorio)
# Pre-requisitos documentados em godot/export/SETUP.md.
set -euo pipefail

GODOT="${GODOT:-$LOCALAPPDATA/Programs/godot/Godot_v4.4.1-stable_win64.exe}"
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJ="$(dirname "$HERE")"                       # godot/
OUT="$PROJ/build/arkana3d.apk"
TPL="$APPDATA/Godot/export_templates/4.4.1.stable"
JDK="$LOCALAPPDATA/Java/jdk-21.0.12+8"

fail() { echo "ERRO: $*" >&2; exit 1; }

# --- checagens (falhar cedo, com endereco) ---
[[ -f "$GODOT" ]] || fail "Godot 4.4.1 nao encontrado em $GODOT (defina GODOT=caminho.exe)"
[[ -f "$TPL/android_debug.apk" ]] || fail "template Android ausente: $TPL/android_debug.apk"
[[ -d "$LOCALAPPDATA/Android/Sdk/platform-tools" ]] || fail "Android SDK ausente em $LOCALAPPDATA/Android/Sdk"
[[ -d "$JDK" ]] || fail "JDK 21 ausente em $JDK"
[[ -f "$PROJ/export_presets.cfg" ]] || fail "export_presets.cfg ausente em $PROJ"

# O export Android do Godot 4.4 exige esta flag em project.godot e falha
# SILENCIOSAMENTE sem ela (validacao SEM mensagem — conferido no source 4.4.1,
# has_valid_project_configuration -> should_import_etc2_astc). project.godot e'
# do COORDENADOR (ARQUITETURA.md): esta raia PEDE em vez de editar. E' a linha
# que o proprio wizard do Godot escreve em projetos Mobile.
if ! grep -q "import_etc2_astc=true" "$PROJ/project.godot"; then
	fail "project.godot precisa (dono: COORDENADOR) da linha abaixo na secao [rendering]:
	textures/vram_compression/import_etc2_astc=true"
fi

# keystore de DEBUG (convencao publica androiddebugkey/android — NAO e' segredo).
# Godot usa o de editor_settings ($APPDATA/Godot/keystores); ~/.android e' o fallback.
if [[ ! -f "$APPDATA/Godot/keystores/debug.keystore" && ! -f "$HOME/.android/debug.keystore" ]]; then
	echo "keystore de debug ausente — gerando o padrao em ~/.android/debug.keystore"
	mkdir -p "$HOME/.android"
	"$JDK/bin/keytool.exe" -genkeypair -v -keystore "$HOME/.android/debug.keystore" \
		-storepass android -keypass android -alias androiddebugkey \
		-keyalg RSA -keysize 2048 -validity 10000 \
		-dname "CN=Android Debug,O=Android,C=US"
	mkdir -p "$APPDATA/Godot/keystores"
	cp "$HOME/.android/debug.keystore" "$APPDATA/Godot/keystores/debug.keystore"
fi

mkdir -p "$PROJ/build"

echo "== 1/3 import headless =="
"$GODOT" --headless --path "$PROJ" --import >/dev/null 2>&1 || true
# ponytail: || true — Main.tscn (raia gameplay) ausente nao pode travar o APK;
# erros REAIS de export aparecem no passo 2, que e' estrito.

echo "== 2/3 export debug =="
"$GODOT" --headless --path "$PROJ" --export-debug "Android" "$OUT"

echo "== 3/3 verificacao =="
[[ -f "$OUT" ]] || fail "APK nao foi gerado em $OUT"
SIZE=$(du -h "$OUT" | cut -f1)
# Windows: logo apos o export, o primeiro unzip pode ler o zip ainda em flush
# e listar VAZIO (visto na R17: grep falhava com o APK integro). Uma pausa e
# uma retentativa separam "arquivo quebrado" de "arquivo ainda fechando".
if ! unzip -l "$OUT" | grep -q "libgodot_android.so"; then
	sleep 2
	unzip -l "$OUT" | grep -q "libgodot_android.so" || fail "APK sem libgodot_android.so"
fi
unzip -l "$OUT" | grep -q "assets/" || fail "APK sem assets/ (pck do projeto)"
echo "OK: $OUT ($SIZE)"
unzip -l "$OUT" | grep -E "lib/|\.pck|assets/.*\.(pck|so)" | head -8 || true
