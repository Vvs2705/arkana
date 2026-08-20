## NUMEROS DO JOGO — espelho do GDD (secoes 4, 5, 14, 19.3).
## Dono: COORDENADOR. Rebalancear = editar AQUI, nunca cacar constante em cena.
## Linhagem: portado de src/core/balance.ts (2D, sancionado pelo GDD) — a raia
## de gameplay confere os valores la' antes de usar. Marcados como KNOB os que
## so' o playtest calibra.
extends Node

const PLAYER := {
	"hp": 100.0,
	"mana_max": 100.0,
	"mana_regen": 14.0,      # por segundo — conferir em src/core/balance.ts
	"speed": 7.5,            # m/s — KNOB (2D usa px; 3D recalibra no aparelho)
	"jump": 4.5,             # KNOB
}

const FIRE := {
	"dmg": 13.0,
	"mana_cost": 9.0,
	"fire_rate": 0.27,       # s entre disparos — conferir em balance.ts
	"projectile_speed": 24.0, # m/s — KNOB 3D
	"range": 40.0,           # m — KNOB 3D
}

const TOUCH := {
	"aim_deadzone_dp": 14.0, # a MESMA regra do 2D/Roblox: dp, nunca px
	"tap_max_ms": 220,       # KNOB — calibrar SEMPRE junto com a deadzone
}

const MATCH := {
	"duration_s": 180.0,
	"bots": 6,
}
