extends SceneTree
## tools/exportar_heightmap.gd — ALIMENTA O pc-unreal/, NAO E' FEATURE DO MOBILE.
##
## POR QUE ISTO EXISTE: a Ilha Fraturada mora dentro de `Island.height()`, e o
## relevo dela e' feito com o FastNoiseLite do Godot (semente 7, frequencia 0,02,
## simplex suave). Portar essa funcao para Python significaria reimplementar o
## ruido do Godot e torcer para bater — e ilha que "quase bate" e' ilha OUTRA.
##
## Entao quem exporta e' o proprio Godot: o mesmo `height()` que o APK usa,
## amostrado numa grade, gravado como RAW de 16 bits — que e' exatamente o que o
## Landscape do Unreal importa. Zero traducao, zero deriva.
##
## USO (headless, do diretorio mobile-godot/godot):
##   Godot_v4.4.1-stable_win64_console.exe --headless --path . \
##       --script res://tools/exportar_heightmap.gd
##
## SAIDA: `ilha-fraturada-2017.r16` + as cotas medidas, que sao o que decide a
## escala Z do Landscape la' no Unreal.

## 2017 = 32 x 63 + 1. O Landscape do Unreal so' aceita lado (n x 63 + 1); fora
## dessa lista ele reamostra e a costa vira serrilha.
const LADO := 2017

## O tamanho que a ilha vai ter NO UNREAL. O relevo nao muda — e' a mesma ilha
## amostrada mais larga, exatamente o que a const ESCALA do Island.gd ja' faz.
## Escala UNIFORME (XY e Z crescem juntos), entao as INCLINACOES sao identicas
## as da ilha que roda no APK; so' o mapa fica maior.
const ALVO_M := 2400.0


func _init() -> void:
	var ilha: Object = load("res://world/Island.gd").new()
	# `height()` so' depende de `_noise`, e _ready() nao roda em objeto solto —
	# entao as tres linhas que o _ready ajusta vao na mao. Se elas mudarem la',
	# mudam aqui: e' a UNICA duplicacao deste arquivo, e e' proposital (importar
	# a cena inteira arrastaria material, shader e malha para um export de dados).
	ilha._noise.seed = 7
	ilha._noise.frequency = 0.02
	ilha._noise.noise_type = FastNoiseLite.TYPE_SIMPLEX_SMOOTH

	var meia: float = ilha.SIZE * 0.5
	var passo: float = ilha.SIZE / float(LADO - 1)

	# Primeira passada: as cotas reais. Sem elas nao da' para escolher a escala Z
	# do Landscape, e chutar a escala e' achatar ou esticar a ilha inteira.
	var cotas := PackedFloat32Array()
	cotas.resize(LADO * LADO)
	var lo := INF
	var hi := -INF
	for j in LADO:
		var z: float = -meia + j * passo
		for i in LADO:
			var h: float = ilha.height(-meia + i * passo, z)
			cotas[j * LADO + i] = h
			lo = minf(lo, h)
			hi = maxf(hi, h)

	# Segunda passada: normaliza SIMETRICO em torno do zero. O Unreal le' RAW de
	# 16 bits little-endian, linha por linha, do canto superior esquerdo, e trata
	# o valor 32768 como a cota ZERO do ator.
	#
	# POR QUE SIMETRICO, e nao "estica de lo ate' hi": normalizando pela faixa o
	# nivel do mar (cota 0) cai em 11.200 de 65.535, ou seja **47,8 m ABAIXO do
	# zero do Unreal**. Medido em 28/08: com isso o plano do mar em Z=0 afogava a
	# ilha inteira, e todo limiar de cota do material (praia, terra) mentia.
	# Ancorando o meio da faixa no zero, o zero do Unreal E' o nivel do mar — sem
	# deslocar ator nenhum, o que importa porque **transform de Landscape nao
	# persiste** mexido por script no World Partition (os proxies do terreno estao
	# descarregados e a mudanca se perde no save).
	#
	# O preco e' faixa util: o lado mais curto (o fundo do mar, -6,2 m contra os
	# +30,1 m do pico) sobra sem uso. Numa altura de 16 bits isso e' irrelevante —
	# ainda restam ~10.900 passos por metro.
	var meia_faixa: float = maxf(maxf(absf(lo), absf(hi)), 0.001)
	var bytes := PackedByteArray()
	bytes.resize(LADO * LADO * 2)
	for k in cotas.size():
		var v := int(round(clampf(0.5 + 0.5 * cotas[k] / meia_faixa, 0.0, 1.0) * 65535.0))
		bytes[k * 2] = v & 0xFF
		bytes[k * 2 + 1] = (v >> 8) & 0xFF

	var destino := "res://../../pc-unreal/heightmap/ilha-fraturada-%d.r16" % LADO
	DirAccess.make_dir_recursive_absolute(
			ProjectSettings.globalize_path("res://../../pc-unreal/heightmap"))
	var f := FileAccess.open(destino, FileAccess.WRITE)
	if f == null:
		push_error("nao consegui gravar em %s" % destino)
		quit(1)
		return
	f.store_buffer(bytes)
	f.close()

	print("LADO=%d  SIZE=%.1f m  passo=%.3f m/vertice" % [LADO, ilha.SIZE, passo])
	print("COTA_MIN=%.4f m  COTA_MAX=%.4f m  MEIA_FAIXA=%.4f m" % [lo, hi, meia_faixa])
	# O numero que o Unreal precisa: a escala Z que faz 512 unidades de altura
	# valerem a faixa simetrica inteira, ja' na escala final do mapa.
	print("ESCALA_Z_UNREAL(mapa de %.0f m) = %.4f" % [ALVO_M,
			100.0 * (2.0 * meia_faixa * (ALVO_M / ilha.SIZE)) / 512.0])
	print("ESCALA_XY_UNREAL = %.4f cm/vertice" % (ALVO_M * 100.0 / float(LADO - 1)))
	print("ARQUIVO=%s  bytes=%d" % [ProjectSettings.globalize_path(destino), bytes.size()])
	quit()
