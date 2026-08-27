--!strict
-- ============================================================================
-- ARKANA (Roblox) — OS 5 ELEMENTOS + MATRIZ DE SINTONIA
-- Fonte: GDD §9 (elementos e os 10 combos) e §10 (paleta + acessibilidade).
--
-- REGRA DE ACESSIBILIDADE INEGOCIÁVEL (GDD §10): cada elemento tem
-- COR + FORMA distintas — nunca só cor (daltonismo). A `shape` abaixo é a
-- assinatura geométrica do projétil, montada com primitivas do Roblox
-- (estética blocky ref. Pixel Gun — zero assets externos, zero Toolbox).
-- Dono: COORDENADOR.
-- ============================================================================

export type Id = "Fire" | "Water" | "Earth" | "Wind" | "Lightning"

export type Shape = "Wedge" | "Ball" | "Block" | "Ring" | "Bolt"

export type Element = {
	id: Id,
	pt: string,
	en: string,
	color: Color3,
	--- cor da luz emitida (mais clara que a cor base — o que brilha é magia)
	glow: Color3,
	shape: Shape,
	--- ordem no carrossel de troca (GDD §19.3)
	order: number,
}

local Elements = {}

Elements.List = {
	{
		id = "Fire" :: Id,
		pt = "Fogo",
		en = "Fire",
		color = Color3.fromHex("#FF5A2A"),
		glow = Color3.fromHex("#FFB07A"),
		--- lágrima/cone → cunha apontada (regra de silhueta do GDD §10)
		shape = "Wedge" :: Shape,
		order = 1,
	},
	{
		id = "Water" :: Id,
		pt = "Água",
		en = "Water",
		color = Color3.fromHex("#2AA7FF"),
		glow = Color3.fromHex("#A8DDFF"),
		shape = "Ball" :: Shape,
		order = 2,
	},
	{
		id = "Earth" :: Id,
		pt = "Terra",
		en = "Earth",
		color = Color3.fromHex("#A8763E"),
		glow = Color3.fromHex("#D8AE76"),
		--- cristal/lasca facetada → cubo girado
		shape = "Block" :: Shape,
		order = 3,
	},
	{
		id = "Wind" :: Id,
		pt = "Vento",
		en = "Wind",
		color = Color3.fromHex("#8FE8C9"),
		glow = Color3.fromHex("#D6FFF1"),
		--- anel/hélice quase invisível
		shape = "Ring" :: Shape,
		order = 4,
	},
	{
		id = "Lightning" :: Id,
		pt = "Raio",
		en = "Lightning",
		color = Color3.fromHex("#F5D90A"),
		glow = Color3.fromHex("#FFF7A8"),
		--- zigue-zague segmentado
		shape = "Bolt" :: Shape,
		order = 5,
	},
}

--- índice por id (Elements.ById.Fire.color, etc.)
Elements.ById = {} :: { [Id]: Element }
for _, e in ipairs(Elements.List) do
	Elements.ById[e.id] = e
end

--- ordem canônica dos ids (carrossel, tecla Q, botões de toque)
Elements.Order = { "Fire", "Water", "Earth", "Wind", "Lightning" } :: { Id }

function Elements.isValid(id: any): boolean
	return typeof(id) == "string" and Elements.ById[id :: Id] ~= nil
end

-- ============================================================================
-- MATRIZ DE SINTONIA — os 10 pares (GDD §9). A chave é a dupla ordenada
-- alfabeticamente ("Fire|Wind"), para o par ser comutativo.
-- ============================================================================

export type Combo = {
	key: string,
	pt: string,
	en: string,
	--- efeito resumido (o servidor implementa; a UI mostra o nome)
	effect: string,
	color: Color3,
	--- raio de área do efeito combinado, em studs
	radius: number,
	duration: number,
}

local function comboKey(a: Id, b: Id): string
	if a < b then
		return a .. "|" .. b
	end
	return b .. "|" .. a
end
Elements.comboKey = comboKey

Elements.Combos = {
	[comboKey("Fire", "Wind")] = {
		key = comboKey("Fire", "Wind"),
		pt = "Tornado Flamejante",
		en = "Flaming Tornado",
		effect = "tornado que anda e suga inimigos próximos",
		color = Color3.fromHex("#FF8A3D"),
		radius = 16,
		duration = 6,
	},
	[comboKey("Fire", "Earth")] = {
		key = comboKey("Fire", "Earth"),
		pt = "Chuva de Magma",
		en = "Magma Rain",
		effect = "área de lava persistente que nega terreno",
		color = Color3.fromHex("#FF4500"),
		radius = 18,
		duration = 8,
	},
	[comboKey("Fire", "Lightning")] = {
		key = comboKey("Fire", "Lightning"),
		pt = "Explosão de Plasma",
		en = "Plasma Burst",
		effect = "burst de dano alto em ponto único",
		color = Color3.fromHex("#FFD98A"),
		radius = 12,
		duration = 0.6,
	},
	[comboKey("Fire", "Water")] = {
		key = comboKey("Fire", "Water"),
		pt = "Cortina de Vapor",
		en = "Steam Curtain",
		effect = "névoa escaldante: cega e causa dano leve",
		color = Color3.fromHex("#E8F4FF"),
		radius = 20,
		duration = 9,
	},
	[comboKey("Water", "Lightning")] = {
		key = comboKey("Water", "Lightning"),
		pt = "Eletrocussão",
		en = "Electrocution",
		effect = "a água conduz: atordoa todos na poça",
		color = Color3.fromHex("#9BE7FF"),
		radius = 22,
		duration = 2,
	},
	[comboKey("Water", "Earth")] = {
		key = comboKey("Water", "Earth"),
		pt = "Lamaçal",
		en = "Quagmire",
		effect = "lentidão severa em área grande",
		color = Color3.fromHex("#6B5433"),
		radius = 20,
		duration = 12,
	},
	[comboKey("Water", "Wind")] = {
		key = comboKey("Water", "Wind"),
		pt = "Tempestade Torrencial",
		en = "Torrential Storm",
		effect = "apaga fogo, revela pegadas, empurra",
		color = Color3.fromHex("#7FD4FF"),
		radius = 24,
		duration = 6,
	},
	[comboKey("Earth", "Wind")] = {
		key = comboKey("Earth", "Wind"),
		pt = "Tempestade de Areia",
		en = "Sandstorm",
		effect = "cegueira em área + dano contínuo leve",
		color = Color3.fromHex("#C9A96A"),
		radius = 22,
		duration = 8,
	},
	[comboKey("Earth", "Lightning")] = {
		key = comboKey("Earth", "Lightning"),
		pt = "Cristais Carregados",
		en = "Charged Crystals",
		effect = "minas de cristal que atordoam ao quebrar",
		color = Color3.fromHex("#D9C46A"),
		radius = 16,
		duration = 15,
	},
	[comboKey("Wind", "Lightning")] = {
		key = comboKey("Wind", "Lightning"),
		pt = "Nuvem Tempestuosa",
		en = "Storm Cloud",
		effect = "nuvem que persegue o alvo marcado por 6s",
		color = Color3.fromHex("#CFE8F5"),
		radius = 10,
		duration = 6,
	},
} :: { [string]: Combo }

function Elements.getCombo(a: Id, b: Id): Combo?
	if a == b then
		return nil -- mesmo elemento não funde (GDD §9: são 10 PARES distintos)
	end
	return Elements.Combos[comboKey(a, b)]
end

return Elements
