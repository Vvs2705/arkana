## O GRAMPO DO METAL PARA O MOBILE — a lição do mago preto, agora com um dono.
##
## Os modelos do Meshy chegam com metallic = 1.00, e metal é quase todo
## REFLEXO: sem reflection probe (não há nenhuma — não cabe no orçamento), o
## renderer mobile devolve BREU. Foi medido em 26/08 no vídeo do Diretor: a
## Pyra atravessava a ilha como uma silhueta preta.
##
## O primeiro conserto nasceu dentro de characters/Mage.gd (_domar_pbr). Com o
## cenário inteiro vindo do mesmo pipeline (castelo, luvas, baú), a regra
## ganhou um arquivo próprio: quem instancia modelo do Meshy chama Pbr.domar.
## Uma fonte só — no dia em que existir probe e o grampo mudar, muda AQUI.
class_name Pbr

const METAL_MAX := 0.2
const ASPEREZA_MIN := 0.45


static func domar(raiz: Node) -> void:
	var pilha: Array = [raiz]
	while not pilha.is_empty():
		var n: Node = pilha.pop_back()
		for c in n.get_children():
			pilha.append(c)
		if not (n is MeshInstance3D):
			continue
		var mi := n as MeshInstance3D
		if mi.mesh == null:
			continue
		for s in mi.mesh.get_surface_count():
			var m := mi.mesh.surface_get_material(s)
			if m is StandardMaterial3D:
				var sm := m as StandardMaterial3D
				sm.metallic = minf(sm.metallic, METAL_MAX)
				sm.roughness = maxf(sm.roughness, ASPEREZA_MIN)
