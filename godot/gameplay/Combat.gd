## PONTO UNICO de dano (regra provada do projeto — ARQUITETURA.md).
## Ninguem toca hp de ninguem fora daqui; UI/telemetria OBSERVAM pelo Bus.
class_name Combat


static func deal(target: Node, amount: float, element := "fire") -> bool:
	if not is_instance_valid(target) or not ("hp" in target):
		return false
	if not (amount > 0.0):  # barra NaN, zero e negativo de uma vez so'
		return false
	if target.hp <= 0.0:
		return false  # ja' morto: nada de dano nem sinal duplicado
	if ("iframes_left" in target) and float(target.iframes_left) > 0.0:
		return false  # esquiva com i-frames (Balance.DODGE.iframes) — dano nao passa
	target.hp = maxf(float(target.hp) - amount, 0.0)
	Bus.damage_dealt.emit(target, int(round(amount)), element)
	if target.is_in_group("player"):
		Bus.health_changed.emit(float(target.hp), float(Balance.PLAYER.hp))
	if target.hp <= 0.0:
		Bus.entity_died.emit(target)
		if target.has_method("die"):
			target.die()
	return true
