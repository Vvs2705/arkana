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

## Elementos novos (R18) — linhagem GDD/Balance do Roblox (dano/mana ja'
## validados na regua dupla de TTK). Formas DISTINTAS por elemento e' lei
## (GDD §10: cor + FORMA, nunca so' cor).
const WATER := {
	"dmg": 11.0,
	"mana_cost": 8.0,
	"fire_rate": 0.30,
	"projectile_speed": 20.0,
	"range": 38.0,
}

const LIGHTNING := {
	"dmg": 15.0,
	"mana_cost": 10.0,
	"fire_rate": 0.34,
	"projectile_speed": 34.0,  # o mais rapido — quase-hitscan estilizado, NUNCA hitscan (GDD §4.1)
	"range": 44.0,
}

const EARTH := {
	"dmg": 16.0,
	"mana_cost": 11.0,
	"fire_rate": 0.36,
	"projectile_speed": 18.0,  # o mais lento e pesado
	"range": 34.0,
}

const WIND := {
	"dmg": 9.0,
	"mana_cost": 7.0,
	"fire_rate": 0.24,         # o mais rapido de cadencia
	"projectile_speed": 26.0,
	"range": 36.0,
}

## Ordem canonica do carrossel — OS 5 COMPLETOS (G2 fechado na R19)
const ELEMENTS := ["fire", "water", "lightning", "earth", "wind"]

## TERRENO REATIVO (GDD §14 + NOTA DE MEDIÇÃO). A regra que custou caro
## aprender: o fogo propaga por ORÇAMENTO (uma rolagem por aresta), NUNCA por
## chance por tique — chance por tique carboniza o mapa inteiro (medido:
## 380/380 celulas em 100% das rodadas no projeto-mae). Para regular o
## incendio, mexa no ORCAMENTO.
const TERRAIN := {
	"cell_size": 3.0,          # m — a grade logica sobre a ilha
	"fuel_budget": 16,         # celulas que UMA ignicao pode espalhar (a lei)
	"edge_chance": 0.5,        # 1 rolagem POR ARESTA (nunca por tique)
	"burn_duration": 8.0,      # s ate virar carvao (GDD §14: cobertura some)
	"burn_dps": 6.0,
	"freeze_duration": 10.0,   # s de lago congelado (vira rota — GDD §14)
	"electrify_duration": 3.0,
	"electrify_dps": 10.0,
	"mud_duration": 6.0,
	"mud_slow": 0.55,          # fator no produto de velocidade (piso: nunca 0)
	"wall_hp": 60.0,
	"wall_duration": 12.0,
	"wall_height": 3.5,        # m
}

const DODGE := {
	"distance": 5.0,     # m — KNOB
	"duration": 0.18,    # s
	"cooldown": 2.6,     # do Roblox pos-calibracao R5 (2.6/0.18)
	"iframes": 0.12,     # s de invulnerabilidade no inicio — KNOB
}

const TOUCH := {
	"aim_deadzone_dp": 14.0, # a MESMA regra do 2D/Roblox: dp, nunca px
	"tap_max_ms": 220,       # KNOB — calibrar SEMPRE junto com a deadzone
}

const MATCH := {
	"duration_s": 180.0,
	"bots": 6,
}
