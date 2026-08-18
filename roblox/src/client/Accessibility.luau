--!strict
-- ============================================================================
-- RAIA D (UI) — ACESSIBILIDADE E CONFORTO: a LOJA DE PREFERÊNCIAS do cliente.
--
-- POR QUE EXISTE (auditoria de 18/08, gate de playtest com gente de fora): as
-- coisas desta lista, quando faltam, fazem o jogador culpar O JOGO por algo que
-- é da interface — tela que treme demais, flash que dói, HUD do lado errado do
-- polegar, cue crítico que só existia no som.
--
-- REGRA DESTE MÓDULO: ele NÃO desenha jogo. Ele guarda a escolha, aplica o que
-- é do cliente (escala da UI, contraste, espelho canhoto/destro, blur/DOF,
-- legendas) e EXPÕE o resto como número para quem desenha (Vfx: tremor, flash,
-- intensidade) ou toca (Audio: volume). O `Settings.luau` é só a tela que mexe
-- nestes valores.
--
-- UMA LOJA SÓ (R9): o volume era a exceção — o `Audio` guardava e persistia a
-- própria cópia, sem knob nenhum na tela. Agora a PREFERÊNCIA mora aqui (é uma
-- escolha de conforto como qualquer outra) e o `Audio` só a APLICA. Um dono
-- escreve, um dono persiste; ninguém sincroniza nada.
--
-- PLATAFORMA PRIMEIRO (docs/ROBLOX.md §2): o que o Roblox já resolve não é
-- recriado aqui —
--   · `GuiService.PreferredTextSize` (tamanho de texto preferido do usuário)
--     MULTIPLICA a nossa escala de UI;
--   · `UserGameSettings.ReducedMotion` vira o PADRÃO de tremor/flash (o menu
--     ainda pode subir; a escolha salva vence);
--   · `UserGameSettings.PreferredTransparency` afina a opacidade dos painéis;
--   · HÁPTICO/VIBRAÇÃO fica com o menu nativo do Roblox — não há toggle aqui de
--     propósito (recriar daria dois lugares para a mesma escolha).
--
-- PERSISTÊNCIA: atributos no LocalPlayer. Sobrevive a respawn e a partida
-- inteira, é 100% cliente e não custa remote. NÃO sobrevive a rejoin — isso
-- exige DataStore, que é do servidor (outra raia). Ver RELATÓRIO/gancho.
-- ============================================================================

local GuiService = game:GetService("GuiService")
local Lighting = game:GetService("Lighting")
local Players = game:GetService("Players")

local A11y = {}

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026")
local NIGHT_DEEP = Color3.fromHex("#05070F")
local GOLD_DIM = Color3.fromHex("#8A7336")
local TEXT = Color3.fromHex("#E8E6F0")

-- ------------------------------------------------------- recursos da plataforma
--- Tudo por pcall: cliente antigo (ou Studio velho) sem a propriedade não pode
--- derrubar o HUD inteiro por causa de um knob de conforto.
local function ugs(): any
	local ok, s = pcall(function()
		return UserSettings():GetService("UserGameSettings")
	end)
	return if ok then s else nil
end
local UGS = ugs()

local function native(name: string, default: any): any
	if not UGS then
		return default
	end
	local ok, v = pcall(function()
		return (UGS :: any)[name]
	end)
	return if ok and v ~= nil then v else default
end

--- `Enum.PreferredTextSize` do usuário → multiplicador. Nome desconhecido (enum
--- novo/velho) cai em 1: nunca quebra, só não bonifica.
local TEXT_BUMP: { [string]: number } = {
	Regular = 1,
	Medium = 1.08,
	Large = 1.18,
	Larger = 1.30,
	UseSystem = 1,
	System = 1,
}

local function nativeTextBump(): number
	local ok, v = pcall(function()
		return GuiService.PreferredTextSize
	end)
	if ok and typeof(v) == "EnumItem" then
		return TEXT_BUMP[v.Name] or 1
	end
	return 1
end

--- 0..1. O usuário pediu UI mais opaca no Roblox → nossos painéis obedecem.
local function nativeTransparency(): number
	return math.clamp(tonumber(native("PreferredTransparency", 1)) or 1, 0, 1)
end

local reducedMotion = native("ReducedMotion", false) == true

-- ------------------------------------------------------------------ prefs
--- `shake`/`flash`/`vfx` são FRAÇÕES (0 = desligado, 1 = cheio) porque é assim
--- que quem desenha usa: multiplicar. Booleano obrigaria o Vfx a inventar meio
--- termo, e "metade do tremor" é exatamente o pedido mais comum.
local DEFAULTS: { [string]: any } = {
	uiSize = 1, -- 0.9 · 1 · 1.15 · 1.3
	leftHanded = false,
	contrast = nativeTransparency() < 0.35,
	shake = if reducedMotion then 0 else 1,
	flash = if reducedMotion then 0 else 1,
	vfx = 1,
	blur = true, -- blur/DOF ligado
	captions = true,
	readyCue = true,
	--- VOLUME (0..1) é preferência de primeira classe, na MESMA loja das outras:
	--- mesmo atributo `Arkana_`, mesmos `listeners`, mesmo `set`. Quem TOCA é o
	--- `Audio`; ele RECEBE daqui e não guarda cópia persistida — uma loja só.
	--- Estes padrões espelham o mix de fábrica do `Audio` (VOL_DEFAULT) e o
	--- `selfTest` reprova se os dois números se separarem.
	volMaster = 1,
	volWorld = 0.9,
}

--- chave da loja → categoria do mixer. Só estas duas: `Ui`/`Music` ficam no mix
--- de fábrica (knob que ninguém mexe só ocupa tela).
local VOLUME_CAT: { [string]: string } = { volMaster = "Master", volWorld = "World" }

local ATTR = "Arkana_"
local prefs: { [string]: any } = {}
local loaded = false
local listeners: { (string) -> () } = {}

local function ensureLoaded()
	if loaded then
		return
	end
	loaded = true
	local p = Players.LocalPlayer
	for k, v in pairs(DEFAULTS) do
		local stored = if p then p:GetAttribute(ATTR .. k) else nil
		prefs[k] = if stored ~= nil and typeof(stored) == typeof(v) then stored else v
	end
end

-- ------------------------------------------------------------------ escala
local attached: { ScreenGui } = {}

local function scaleOf(): number
	ensureLoaded()
	return (tonumber(prefs.uiSize) or 1) * nativeTextBump()
end

--- Um `UIScale` por painel de topo. Escolhido de propósito em vez de reescrever
--- os layouts: `UIScale` cresce o painel a partir do AnchorPoint dele, então
--- painel de canto continua no canto e nada precisa saber da preferência.
--- Quem não quer escala (fundo de tela cheia) marca `A11yNoScale`.
local function stampScale(obj: Instance)
	if not obj:IsA("GuiObject") or obj:GetAttribute("A11yNoScale") == true then
		return
	end
	local s = obj:FindFirstChild("A11yScale")
	if s and not s:IsA("UIScale") then
		s:Destroy()
		s = nil
	end
	if not s then
		local n = Instance.new("UIScale")
		n.Name = "A11yScale"
		n.Parent = obj
		s = n
	end
	(s :: UIScale).Scale = scaleOf()
end

-- --------------------------------------------------------------- contraste
--- Pinta UM descendente. O valor ORIGINAL fica guardado num atributo na
--- primeira passada — é o que permite desligar o alto contraste sem recriar a
--- tela (e sem cada UI ter de lembrar a própria paleta).
local function paint(d: Instance, on: boolean, factor: number)
	if d:IsA("GuiObject") then
		local base = d:GetAttribute("A11yBg")
		if base == nil then
			base = d.BackgroundTransparency
			d:SetAttribute("A11yBg", base)
		end
		-- transparente de propósito (espaçador, caixa vazia) continua invisível
		if (base :: number) < 1 then
			d.BackgroundTransparency = (base :: number) * factor
		end
	end
	if d:IsA("TextLabel") or d:IsA("TextButton") then
		local base = d:GetAttribute("A11yStroke")
		if base == nil then
			base = d.TextStrokeTransparency
			d:SetAttribute("A11yStroke", base)
		end
		d.TextStrokeTransparency = if on then 0 else (base :: number)
	end
	if d:IsA("UIStroke") then
		local base = d:GetAttribute("A11yStroke")
		if base == nil then
			base = d.Transparency
			d:SetAttribute("A11yStroke", base)
		end
		d.Transparency = if on then 0 else (base :: number)
	end
end

local function repaint(gui: ScreenGui)
	ensureLoaded()
	local on = prefs.contrast == true
	local factor = if on then 0.15 else nativeTransparency()
	for _, d in ipairs(gui:GetDescendants()) do
		paint(d, on, factor)
	end
end

--- Registra uma ScreenGui desta raia: escala, contraste e o que entrar depois.
function A11y.attach(gui: ScreenGui)
	ensureLoaded()
	if table.find(attached, gui) then
		return
	end
	table.insert(attached, gui)
	for _, child in ipairs(gui:GetChildren()) do
		stampScale(child)
	end
	repaint(gui)
	gui.DescendantAdded:Connect(function(d)
		local on = prefs.contrast == true
		paint(d, on, if on then 0.15 else nativeTransparency())
		if d.Parent == gui then
			stampScale(d)
		end
	end)
end

-- ------------------------------------------------------- canhoto/destro
type Placed = { obj: GuiObject, anchor: Vector2, pos: UDim2 }
local placed: { Placed } = {}

local function applyPlace(p: Placed)
	if prefs.leftHanded == true then
		p.obj.AnchorPoint = Vector2.new(1 - p.anchor.X, p.anchor.Y)
		p.obj.Position = UDim2.new(1 - p.pos.X.Scale, -p.pos.X.Offset, p.pos.Y.Scale, p.pos.Y.Offset)
	else
		p.obj.AnchorPoint = p.anchor
		p.obj.Position = p.pos
	end
end

--- Posiciona `obj` no lugar CANÔNICO (layout de destro) e passa a espelhá-lo
--- quando o jogador escolhe HUD canhoto. Chamar de novo só atualiza o canônico
--- — é o que faz o relayout por viewport (SintoniaUi) conviver com o espelho.
function A11y.place(obj: GuiObject, anchor: Vector2, pos: UDim2)
	ensureLoaded()
	for _, p in ipairs(placed) do
		if p.obj == obj then
			p.anchor, p.pos = anchor, pos
			applyPlace(p)
			return
		end
	end
	local p: Placed = { obj = obj, anchor = anchor, pos = pos }
	table.insert(placed, p)
	applyPlace(p)
end

-- ------------------------------------------------------------------ blur/DOF
--- Só DESLIGA o que existe; ao religar devolve o estado que o efeito tinha.
--- Nada aqui cria efeito: se ninguém pôs blur/DOF na cena, isto é um no-op.
local function applyBlurTo(d: Instance)
	if not (d:IsA("BlurEffect") or d:IsA("DepthOfFieldEffect")) then
		return
	end
	local base = d:GetAttribute("A11yEnabled")
	if base == nil then
		base = d.Enabled
		d:SetAttribute("A11yEnabled", base)
	end
	d.Enabled = (prefs.blur == true) and (base :: boolean)
end

local function applyBlur()
	for _, d in ipairs(Lighting:GetChildren()) do
		applyBlurTo(d)
	end
end

-- ------------------------------------------------------------------ volume
--- Require preguiçoso e protegido (mesmo padrão do `Audio` com o `Atmosphere`):
--- se o módulo de som faltar ou explodir, a loja de preferências e a partida
--- continuam de pé. ÁUDIO NUNCA DECIDE JOGO — nem por omissão.
local audioMod: any = nil
local function audio(): any
	if audioMod == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Audio")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		audioMod = (ok and m) or false
	end
	return audioMod or nil
end

--- Empurra a preferência para o mixer. É o ÚNICO caminho do volume até o som:
--- o `Audio` não lê atributo nenhum, então não existe cópia para divergir.
local function applyVolume()
	local a = audio()
	if not a or not a.setVolume then
		return
	end
	for key, cat in pairs(VOLUME_CAT) do
		pcall(a.setVolume, cat, tonumber(prefs[key]) or DEFAULTS[key])
	end
end

-- ------------------------------------------------------------------ legendas
--- FAIXA DE LEGENDAS: um lugar só, sempre o mesmo, para os cues críticos —
--- "SINTONIA CANALIZANDO", "FORA DA ZONA". Quem joga sem som (ou no ônibus, que
--- é o caso normal em celular) lê aqui em vez de adivinhar.
--- Rodapé central: o polegar esquerdo mora no thumbstick e o direito nos botões
--- da raia C; o miolo de baixo é a única faixa larga que sobra.
local capGui: ScreenGui? = nil
local capBox: Frame
local capLabel: TextLabel
local capLeft = 0

local function ensureCaptions()
	if capGui then
		return
	end
	local p = Players.LocalPlayer
	local pg = if p then p:FindFirstChildOfClass("PlayerGui") else nil
	if not pg then
		return
	end
	local root = Instance.new("ScreenGui")
	root.Name = "ArkanaCaptions"
	root.ResetOnSpawn = false
	root.IgnoreGuiInset = false
	root.DisplayOrder = 8 -- acima do HUD/partida; abaixo do menu de opções (20)
	root.ZIndexBehavior = Enum.ZIndexBehavior.Sibling
	root.Parent = pg

	local box = Instance.new("Frame")
	box.Name = "Caption"
	box.AnchorPoint = Vector2.new(0.5, 1)
	box.BackgroundColor3 = NIGHT
	box.BackgroundTransparency = 0.25
	box.BorderSizePixel = 0
	box.Position = UDim2.new(0.5, 0, 1, -14)
	box.Size = UDim2.new(0.5, 0, 0, 30)
	box.Visible = false
	box.Parent = root
	local stroke = Instance.new("UIStroke")
	stroke.Color = GOLD_DIM
	stroke.Thickness = 1
	stroke.Transparency = 0.25
	stroke.Parent = box

	local label = Instance.new("TextLabel")
	label.BackgroundTransparency = 1
	label.Size = UDim2.new(1, -12, 1, -6)
	label.Position = UDim2.fromOffset(6, 3)
	label.Font = Enum.Font.GothamBold
	label.Text = ""
	label.TextColor3 = TEXT
	label.TextScaled = true
	label.TextStrokeColor3 = NIGHT_DEEP
	label.TextStrokeTransparency = 0.1
	label.Parent = box
	local cap = Instance.new("UITextSizeConstraint")
	cap.MaxTextSize = 20
	cap.Parent = label

	capGui, capBox, capLabel = root, box, label
	A11y.attach(root)
end

--- Legenda de UM cue crítico. Chamar só em TRANSIÇÃO (não por frame): a faixa é
--- para "aconteceu isto agora", não para status contínuo.
function A11y.caption(text: string, color: Color3?, hold: number?)
	ensureLoaded()
	if prefs.captions ~= true or text == "" then
		return
	end
	ensureCaptions()
	if not capGui then
		return
	end
	capLabel.Text = string.upper(text)
	capLabel.TextColor3 = color or TEXT
	capLabel.TextTransparency = 0
	capBox.Visible = true
	capLeft = hold or 2.4
end

-- ------------------------------------------------------------------ leitura
local function frac(key: string): number
	ensureLoaded()
	return math.clamp(tonumber(prefs[key]) or 1, 0, 1)
end

--- 0 = sem tremor · 1 = tremor cheio. CONSUMIDOR: Vfx (screen shake).
function A11y.shake(): number
	return frac("shake")
end

--- 0 = sem flash · 1 = flash cheio. Multiplica AMPLITUDE de pulso/piscada.
function A11y.flash(): number
	return frac("flash")
end

--- 0..1 de partícula/estilhaço/luz extra. CONSUMIDOR: Vfx (orçamento de efeito).
function A11y.vfx(): number
	return frac("vfx")
end

function A11y.uiScale(): number
	return scaleOf()
end

function A11y.leftHanded(): boolean
	ensureLoaded()
	return prefs.leftHanded == true
end

function A11y.highContrast(): boolean
	ensureLoaded()
	return prefs.contrast == true
end

--- Mostrar o indicador de "combo disponível" (Selo + pílula no HUD).
function A11y.readyCue(): boolean
	ensureLoaded()
	return prefs.readyCue == true
end

function A11y.get(key: string): any
	ensureLoaded()
	local v = prefs[key]
	return if v == nil then DEFAULTS[key] else v
end

function A11y.defaults(): { [string]: any }
	return DEFAULTS
end

-- ------------------------------------------------------------------ escrita
local function applyAll()
	for _, gui in ipairs(attached) do
		for _, child in ipairs(gui:GetChildren()) do
			stampScale(child)
		end
		repaint(gui)
	end
	for _, p in ipairs(placed) do
		if p.obj.Parent then
			applyPlace(p)
		end
	end
	applyBlur()
	applyVolume()
end

function A11y.set(key: string, value: any)
	ensureLoaded()
	if DEFAULTS[key] == nil or typeof(value) ~= typeof(DEFAULTS[key]) then
		return -- chave/tipo desconhecido: ignora em silêncio, nunca corrompe a loja
	end
	if VOLUME_CAT[key] then
		-- limita AQUI, não só no mixer: guardar 4 e tocar 1 é exatamente a
		-- divergência que esta raia veio matar
		value = math.clamp(value, 0, 1)
	end
	if prefs[key] == value then
		return
	end
	prefs[key] = value
	local p = Players.LocalPlayer
	if p then
		p:SetAttribute(ATTR .. key, value)
	end
	applyAll()
	if not prefs.captions then
		capLeft = 0
		if capGui then
			capBox.Visible = false
		end
	end
	for _, fn in ipairs(listeners) do
		task.spawn(fn, key)
	end
	-- MUDO NÃO PODE CUSTAR INFORMAÇÃO (GDD §10): "SINTONIA CANALIZANDO" e "FORA
	-- DA ZONA" existem no som E na faixa de legendas. Quem zera o volume ganha a
	-- faixa ligada na hora — LIGAR, não travar: desligar depois continua sendo
	-- escolha do jogador, e aí é escolha informada.
	if VOLUME_CAT[key] and value == 0 and prefs.captions ~= true then
		A11y.set("captions", true)
	end
end

--- Avisado a cada mudança. É como a `Settings` repinta os chips sem se
--- perguntar quem mexeu no valor.
function A11y.onChange(fn: (string) -> ())
	table.insert(listeners, fn)
end

-- ------------------------------------------------------------------ API
function A11y.init(_player: Player?)
	ensureLoaded()
	applyAll()
	-- o usuário pode mexer no menu do Roblox NO MEIO da partida: seguir o sinal
	-- nativo é mais barato (e mais correto) que ler a propriedade por frame
	pcall(function()
		GuiService:GetPropertyChangedSignal("PreferredTextSize"):Connect(applyAll)
	end)
	if UGS then
		pcall(function()
			(UGS :: any):GetPropertyChangedSignal("PreferredTransparency"):Connect(applyAll)
		end)
	end
	Lighting.ChildAdded:Connect(applyBlurTo)
end

function A11y.update(dt: number)
	if capLeft <= 0 or not capGui then
		return
	end
	capLeft -= dt
	if capLeft <= 0 then
		capBox.Visible = false
	elseif capLeft < 0.4 then
		capLabel.TextTransparency = 1 - capLeft / 0.4
	end
end

function A11y.destroy()
	table.clear(attached)
	table.clear(placed)
	table.clear(listeners)
	if capGui then
		capGui:Destroy()
		capGui = nil
	end
	capLeft = 0
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Accessibility).selfTest()
-- Cobre o espelho canhoto/destro: é a única conta daqui que não dá para ver a
-- olho, e errar nela joga o HUD para fora da tela em vez de para o outro lado.
-- ============================================================================
function A11y.selfTest()
	local probe = Instance.new("Frame")
	local p: Placed = { obj = probe, anchor = Vector2.new(0, 0), pos = UDim2.new(0, 12, 0, 12) }

	local was = prefs.leftHanded
	prefs.leftHanded = false
	applyPlace(p)
	assert(probe.AnchorPoint.X == 0 and probe.Position.X.Offset == 12, "destro saiu do canônico")

	prefs.leftHanded = true
	applyPlace(p)
	assert(probe.AnchorPoint.X == 1, "canhoto não trocou de âncora")
	assert(probe.Position.X.Scale == 1 and probe.Position.X.Offset == -12, "canhoto não espelhou o X")
	assert(probe.Position.Y.Offset == 12, "canhoto mexeu no Y (só o X espelha)")

	-- espelhar duas vezes tem de voltar ao original: é o que garante que trocar
	-- a opção repetidas vezes não acumula deslocamento
	prefs.leftHanded = false
	applyPlace(p)
	assert(probe.AnchorPoint.X == 0 and probe.Position.X.Offset == 12, "ida e volta não fechou")

	prefs.leftHanded = was
	probe:Destroy()

	-- ------------------------------------------------------------- volume
	-- UMA LOJA SÓ. As três coisas que quebram caladas aqui:
	local a = audio()
	assert(a and a.defaultVolume, "Audio não encontrado: o volume ficaria sem quem o aplique")

	-- 1) o padrão daqui TEM de ser o do mixer. Se alguém afinar o VOL_DEFAULT do
	-- `Audio` e esquecer desta tabela, o menu abre com "CHEIO" aceso num valor
	-- que não é o cheio — ou pior, com NENHUM chip aceso.
	for key, cat in pairs(VOLUME_CAT) do
		assert(a.defaultVolume(cat) == DEFAULTS[key], "padrão de volume divergiu do mixer: " .. key)
	end

	-- 2) a preferência CHEGA no mixer (o gancho é o motivo desta raia existir) e
	-- é limitada a 0..1 na loja, não só lá dentro.
	local wasVol, wasCap = A11y.get("volMaster"), A11y.get("captions")
	A11y.set("volMaster", 0.5)
	assert(a.getVolume("Master") == 0.5, "o mixer não recebeu a preferência de volume")
	A11y.set("volMaster", 4)
	assert(A11y.get("volMaster") == 1, "volume fora de 0..1 entrou na loja")
	assert(a.getVolume("Master") == 1, "loja e mixer divergiram")

	-- 3) MUDO NÃO CUSTA INFORMAÇÃO: zerar o volume liga as legendas dos avisos.
	A11y.set("captions", false)
	A11y.set("volMaster", 0)
	assert(A11y.get("captions") == true, "mudo deixou o jogador sem legendas nem som")

	A11y.set("volMaster", wasVol)
	A11y.set("captions", wasCap)

	print("[Accessibility] autoteste OK — espelho canhoto/destro · volume (loja única, mudo↔legendas)")
	return true
end

return A11y
