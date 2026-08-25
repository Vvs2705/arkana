## NUMEROS DO JOGO — espelho do GDD (secoes 4, 5, 14, 19.3).
## Dono: COORDENADOR. Rebalancear = editar AQUI, nunca cacar constante em cena.
## Os valores compartilhados seguem o GDD e os resultados validos do Campo de
## Provas. Marcados como KNOB os que so' o playtest calibra.
extends Node

const PLAYER := {
	"hp": 100.0,
	"mana_max": 100.0,
	## KNOB (docs/DANO.md §7.2: 14 -> 16). A MANA, nao a cadencia, e' o teto de
	## verdade do DPS (economia unica, GDD §4.2). AUMENTAR = luta mais longa e
	## agressiva, DPS sustentado sobe e o TTK real cai. DIMINUIR = a luta vira
	## gerenciamento de recurso, o combate esfria e o TTK real sobe.
	"mana_regen": 16.0,      # por segundo
	"speed": 7.5,            # m/s — KNOB, recalibrar no aparelho
}

## OS 5 PERFIS DE ELEMENTO (retune docs/DANO.md §7.2). Alem dos 5 numeros de
## chassi, cada elemento carrega 4 fatores ADIMENSIONAIS que a arma NAO
## multiplica (o tier muda QUANTO, nunca O QUE):
##   esc/vida  : dano em ESCUDO x dano em VIDA (GDD §5). E' o que da' papel
##               diferente a cada elemento DENTRO do mesmo duelo.
##   empurrao  : multiplica COMBATE.knockback (o vento empurra, nao quebra).
##   estrutura : multiplica o dano em muro/torreta/totem/casulo/bigorna.
## KNOBs: <ELEM>.dmg AUMENTAR = aquele elemento vira a escolha obvia e o
## carrossel volta a nao importar; DIMINUIR = ele so' se justifica pelo efeito
## secundario (e' o desenho da agua e do vento). <ELEM>.esc AUMENTAR = ele vira
## o abre-escudo do jogo; DIMINUIR = fica inutil contra alvo escudado.
##
## FOGO — a REGUA. Linha neutra de proposito: e' a referencia mental do jogador.
const FIRE := {
	"dmg": 11.0,
	"mana_cost": 7.5,
	"fire_rate": 0.26,       # s entre disparos — conferir em balance.ts
	"projectile_speed": 26.0, # m/s — KNOB 3D
	"range": 34.0,           # m — KNOB 3D
	"esc": 1.00, "vida": 1.00, "empurrao": 1.0, "estrutura": 1.0,
}

## Elementos novos (R18) — linhagem GDD/Balance do Roblox (dano/mana ja'
## validados na regua dupla de TTK). Formas DISTINTAS por elemento e' lei
## (GDD §10: cor + FORMA, nunca so' cor).
## AGUA — o SETUP. Menor dano direto com escudo, mas e' quem MOLHA, e molhado
## e' o que faz o raio valer o dobro. Agua sozinha perde; agua ANTES do raio ganha.
const WATER := {
	"dmg": 9.3,
	"mana_cost": 6.9,
	"fire_rate": 0.28,
	"projectile_speed": 20.0,
	"range": 32.0,
	"esc": 1.00, "vida": 1.00, "empurrao": 1.0, "estrutura": 1.0,
}

## RAIO — o ABRE-ESCUDO. 1.25x em escudo e 1.00x em vida: derruba a barra azul
## e depois vira mediano. Projetil mais rapido do jogo.
const LIGHTNING := {
	"dmg": 12.3,
	"mana_cost": 8.8,
	"fire_rate": 0.32,
	"projectile_speed": 35.0,  # o mais rapido — quase-hitscan estilizado, NUNCA hitscan (GDD §4.1)
	"range": 38.0,
	"esc": 1.25, "vida": 1.00, "empurrao": 1.0, "estrutura": 1.6,
}

## TERRA — o FINALIZADOR e o ANTIESTRUTURA. 1.15x em vida, maior dano por tiro,
## projetil mais lento: dificil de acertar em quem se move, brutal em quem esta'
## preso, e e' ele que derruba cobertura (estrutura 2.0).
const EARTH := {
	"dmg": 13.6,
	"mana_cost": 9.3,
	"fire_rate": 0.35,
	"projectile_speed": 18.0,  # o mais lento e pesado
	"range": 28.0,
	"esc": 1.00, "vida": 1.15, "empurrao": 1.2, "estrutura": 2.0,
}

## VENTO — o CONTROLE. Menor dano e menor efeito em escudo do jogo, pagos com
## empurrao dobrado e a maior cadencia. Vento nao mata: vento tira do lugar.
const WIND := {
	"dmg": 7.5,
	"mana_cost": 6.1,
	"fire_rate": 0.23,         # o mais rapido de cadencia
	"projectile_speed": 27.0,
	"range": 30.0,
	"esc": 0.75, "vida": 1.00, "empurrao": 2.0, "estrutura": 0.6,
}

## Ordem canonica do carrossel — OS 5 COMPLETOS (G2 fechado na R19)
const ELEMENTS := ["fire", "water", "lightning", "earth", "wind"]

## ESCUDO DE MAGIA EVOLUTIVO (GDD §5 — numeros do GDD, sem alteracao).
## O escudo absorve ANTES da vida, 1:1, e o excedente do MESMO tiro TRANSBORDA
## para a vida (sem transbordo o ultimo tiro contra um escudo de 3 pontos
## desperdica 10 de dano e o TTK ganha um degrau aleatorio).
## NAO regenera sozinho: so' pergaminho, passiva da Tessa e Suprema do Brok.
const ESCUDO := {
	"niveis":     [50.0, 75.0, 100.0, 125.0],   # capacidade por nivel (1..4)
	"cores":      ["FFFFFF", "4C8CFF", "9B5CE6", "F0C75E"],  # branco/azul/roxo/dourado
	"evoluir":    [0.0, 150.0, 400.0, 900.0],   # dano causado ACUMULADO p/ entrar no nivel
	"transbordo": true,   # lei; desligar so' em playtest
	## KNOB: >0 desfaz o TTK alvo (recuar 10s apaga a luta inteira). 0.0 e' o
	## valor de projeto.
	"regen": 0.0,
}

## EFEITOS SECUNDARIOS POR ELEMENTO (docs/DANO.md §3.3/§3.4). O estado do alvo
## e' EXCLUSIVO por categoria — UM termico (queimando OU molhado, nunca os dois)
## e UM de movimento. A reacao sempre SUBSTITUI, nunca soma: e' o que mantem o
## modelo previsivel, e previsivel e' aprendivel.
const STATUS := {
	"burn_dps": 4.0, "burn_dur": 3.0,                  # queimadura do fogo direto
	"burn_fanned_dps": 7.0, "burn_fanned_bonus": 2.0,  # aticada pelo vento (GDD §16.4)
	"wet_dur": 5.0, "wet_slow": 0.90,                  # molhado: habilita a conducao
	"hypothermia_slow": 0.80,                          # molhado + agua de novo (GDD §16.4)
	"conduct_mult": 1.50, "conduct_stun": 0.40,        # raio em alvo MOLHADO
	"conduct_arc_m": 4.0, "conduct_arc_mult": 0.50,    # arco p/ outro molhado perto
	## TETO DO KERNEL (personagens/17-sylva.md). NUNCA SUBIR: acima de 0.8s o
	## jogador perde o controle e o jogo fica injusto.
	"stun_cap": 0.80,
}

## DANO AO LONGO DO TEMPO — as duas leis do estudo (docs/DANO.md §3.5).
const DOT := {
	## Teto de DoT SOMADO. AUMENTAR: o jogador cai de fontes que nao consegue
	## ler (queimadura + terreno + nevoa = 19 dps invisiveis). DIMINUIR: o
	## terreno e a queimadura viram enfeite.
	"teto_dps": 12.0,
	## KNOB: DoT vai direto na VIDA. O escudo protege contra MAGIA, nao contra
	## estar em chamas — e' o que da' dentes ao pilar §14. Desligar so' em playtest.
	"ignora_escudo": true,
	## s por aplicacao. MATA o dano de 0 a 60Hz (docs/DANO.md §2.3). AUMENTAR:
	## menos precisao no dano ambiental, mais folga de CPU. DIMINUIR: volta o
	## numero zero, o zumbido de som e o tween por frame.
	"tick": 0.25,
}

## FEEDBACK DE DANO. Cor + FORMA + SOM e' lei (GDD §10) — nunca so' cor.
## Consumido por Projectile (numero) e, quando a raia de UI migrar, por
## Hud/Sfx/MatchJuice.
const FEEDBACK := {
	"cor_escudo": "CFE6FF",  # branco-azulado: acerto em ESCUDO
	## AUMENTAR: os numeros somem numa soma so' e o jogador perde a cadencia do
	## acerto. DIMINUIR: escada ilegivel de numeros empilhados.
	"num_merge_s": 0.35,
	"num_life_s": 0.60,
	"num_scale_base": 0.80, "num_scale_gain": 0.50, "num_scale_max": 1.60,
	"hit_sound_min_s": 0.08,
	"dot_num_every_s": 0.50,
	"vignette_min_s": 0.12,
	"arc_dur_s": 1.40, "arc_deg": 60.0, "arc_max": 3,
}

## COMBATE — o que estava solto em constante de cena (Projectile.KNOCKBACK).
const COMBATE := {
	## m/s base do empurrao no acerto; multiplicado pelo campo `empurrao` do
	## elemento. AUMENTAR: todo acerto vira controle. DIMINUIR: o acerto perde peso.
	"knockback": 2.2,
	"ref_range_m": 12.0,    # alcance de REFERENCIA de toda medicao de TTK
}

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
	## KNOB (docs/DANO.md §7.2: 10 -> 8). AUMENTAR: 3s de poca eletrificada passa
	## de 24 para >30 de dano e a poca mata sozinha. DIMINUIR: ela deixa de
	## assustar e o combo agua+raio perde a razao de existir.
	"electrify_dps": 8.0,
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
	## R20 game feel: o dash NAO tem mais velocidade constante. A velocidade cai
	## em rampa de burst*media ate (2-burst)*media, entao a DISTANCIA nao muda
	## (a media e' a mesma) mas o ARRANQUE e' o dobro. 1.0 = o dash velho.
	"burst": 1.85,       # KNOB — >2.0 estoura (velocidade negativa no fim)
	## Ao sair do dash o corpo JA' esta' em velocidade de corrida na direcao do
	## dash (momentum preservado): 0 = para seco e reacelera, 1 = emenda.
	"exit_momentum": 1.0, # KNOB
}

## GAME FEEL (R20) — o PESO do personagem. O Diretor testou no aparelho e pediu
## "movimentacao e AGILIDADE": antes a velocidade era instantanea (liga/desliga
## como interruptor). Agora tudo passa por aceleracao. Todo numero aqui e' KNOB.
const MOVE := {
	## Aceleracao ao sair do lugar. Tempo ate a velocidade cheia = speed/accel.
	## 45 com speed 7.5 = 0.17s. MAIOR = mais responsivo e mais "arcade";
	## MENOR = mais peso, mas o dedo sente atraso (ruim em jogo de tiro).
	"accel": 45.0,        # m/s^2
	## Freio ao soltar o joystick. 60 = para em 0.13s. MENOR = derrapa (patina
	## no gelo); MAIOR = para seco e volta a parecer interruptor.
	"brake": 60.0,        # m/s^2
	## Inversao de sentido (ex.: correndo a direita e puxando a esquerda). Alto
	## de proposito: e' o que faz o strafe de tiroteio responder.
	"turn_accel": 95.0,   # m/s^2
	## Fator de aceleracao no AR. 0.35 = pouco controle no pulo/queda (peso).
	"air_control": 0.35,
	## Joystick: zona morta E curva de resposta. A deadzone TEM QUE BATER com a
	## do VirtualJoystick (ui/VirtualJoystick.gd usa 0.12) senao sobra um degrau
	## — logo que passa a zona morta o mago saia com 12% da velocidade.
	"stick_deadzone": 0.12,
	## Expoente da curva. 1.0 = linear. >1 = mais curso fino perto do centro
	## (da' pra ANDAR devagar, nao so' correr). >2 fica lerdo no meio.
	"stick_curve": 1.4,
	## Limiar de animacao com HISTERESE (m/s): entra em "run" acima de enter,
	## so' volta pra "idle" abaixo de exit. Sem os dois valores a animacao
	## PISCA quando a velocidade fica na fronteira.
	"run_anim_enter": 1.3,
	"run_anim_exit": 0.5,
	## Giro do corpo (rad/s do lerp). Mirando gira mais rapido: o corpo tem que
	## alcancar a camera. MAIOR = seco demais; MENOR = o tiro sai torto.
	"turn_rate_aim": 15.0,
	"turn_rate_free": 10.0,
	## Bonus de giro quando PARADO (pivo no lugar e' de graca na vida real).
	## Interpola de (1+bonus)*rate parado ate rate em velocidade cheia.
	"turn_pivot_bonus": 0.7,
	## Banking: o corpo se INCLINA pra dentro da curva. Barato e da' muita vida.
	"bank_gain": 0.035,   # rad por rad/s de giro
	"bank_max": 0.13,     # rad (~7.5 graus) — teto; acima disso parece moto
	"bank_rate": 9.0,     # 1/s de suavizacao; MENOR = a inclinacao "cola" atrasada
}

## ANIMACAO x VELOCIDADE (R20). A causa da PATINACAO: a anim de corrida roda em
## cadencia fixa enquanto o mago anda a 7.5 m/s. Agora o speed_scale sai da
## velocidade real. A conta e' generica (serve pro .glb da Meshy E pro
## procedural): velocidade_natural = run_stride_m / duracao_do_ciclo.
const ANIM := {
	## Metros que UM ciclo completo da anim "run" cobre no chao. E' o unico
	## numero que calibra a patinacao: pe escorregando PRA TRAS (o corpo anda
	## mais que a anim) = DIMINUA; perna girando demais = AUMENTE.
	## MEDIDO (nao estimado) em 21/08 no pyra.glb: o root motion do clipe
	## andava 7.91 m por ciclo. Esse deslocamento agora e' REMOVIDO na montagem
	## (tools/meshy/montar_glb.py) e o mesmo numero vira a passada aqui. Se
	## trocar a animacao de corrida, o script imprime a passada nova.
	"run_stride_m": 7.91,
	## Teto e piso do speed_scale. Sem teto, um buff de velocidade transforma a
	## corrida em desenho acelerado.
	"scale_min": 0.55,
	"scale_max": 1.9,
}

const TOUCH := {
	"aim_deadzone_dp": 14.0, # regra mobile compartilhada: dp, nunca px
	"tap_max_ms": 220,       # KNOB — calibrar SEMPRE junto com a deadzone
}

const MATCH := {
	"duration_s": 180.0,
	"bots": 6,
}
