--!strict
-- ============================================================================
-- RAIA D (ATMOSFERA) — o "entardecer arcano" do GDD §10, montado no CLIENTE.
--
-- POR QUE NO CLIENTE: efeitos de `Lighting` criados aqui são LOCAIS (não
-- replicam). É exatamente o que queremos: cada aparelho escolhe o próprio
-- nível de pós-processamento sem negociar com o servidor — e o servidor
-- continua o único dono do jogo (docs/ROBLOX.md §3).
--
-- A BASE já vem do `default.project.json` (ClockTime 17.5, Ambient azul-noite,
-- fog, ShadowMap). Este módulo só acrescenta o que precisa de decisão POR
-- APARELHO: Atmosphere, Sky e pós-processamento.
--
-- ZERO ASSET EXTERNO: nenhum `rbxassetid://` inventado. O `Sky` fica com as
-- texturas PADRÃO do engine (`rbxasset://`, embutidas no cliente) e a COR do
-- céu vem de `Atmosphere.Color`/`Decay` + do fog do projeto. O gradiente
-- chapado exato do GDD (#0B1026 no alto → #F0C75E no horizonte) exigiria um
-- skybox próprio, que só o Diretor pode subir — quando existir, é preencher as
-- 6 faces do Sky em `buildSky` e nada mais muda.
--
-- MODO NÉVOA (GDD §19.2): em celular/aparelho fraco o bloom e o sun rays saem
-- de cena e a névoa engrossa. 60 fps em celular é invariante, não meta.
-- ============================================================================

local Lighting = game:GetService("Lighting")
local UserInputService = game:GetService("UserInputService")

local Atmosphere = {}

-- ------------------------------------------------------------------ paleta
local NIGHT = Color3.fromHex("#0B1026") -- base azul-noite (GDD §10)
local GOLD = Color3.fromHex("#F0C75E") -- acento dourado (horizonte/marca)

-- ------------------------------------------------------------------ knobs
--- KNOB DE QUALIDADE — este é o "Modo Névoa" do GDD §19.2.
---   mode = "auto" (padrão): leve em aparelho só-toque OU se o fps cair;
---   mode = "rich": força o visual cheio — é o botão do Diretor para
---                  "mais gráfico" quando o playtest no aparelho-alvo segurar;
---   mode = "lite": força o visual leve (bom para gravar comparativo).
--- `fpsFloor`/`fpsWindow`: o vigia. Média abaixo do piso pela janela inteira →
--- rebaixa UMA VEZ e não volta: não oscilar liga/desliga vale mais que
--- recuperar o bloom.
Atmosphere.Quality = {
	mode = "auto",
	fpsFloor = 42,
	fpsWindow = 4.0,
	warmup = 3.0, -- s ignorados no começo: carregar mundo derruba fps e não conta
}
local Q = Atmosphere.Quality

--- aparelho só-toque = celular/tablet. Não é medida de força, é o palpite
--- barato e certo na maioria dos casos; o vigia de fps corrige o resto.
local touchOnly = UserInputService.TouchEnabled and not UserInputService.KeyboardEnabled
local demoted = false

--- Fonte ÚNICA da verdade de qualidade no cliente. O Vfx lê daqui (require
--- preguiçoso), para o Diretor ter um knob só, não dois.
function Atmosphere.isLite(): boolean
	if Q.mode == "lite" then
		return true
	elseif Q.mode == "rich" then
		return false
	end
	return touchOnly or demoted
end

-- ------------------------------------------------------------------ estado
local air: Atmosphere? = nil
local bloom: BloomEffect? = nil
local rays: SunRaysEffect? = nil
local grade: ColorCorrectionEffect? = nil

local pulseLeft = 0
local pulseTotal = 0.001
local pulseColor = GOLD

local fpsAcc, fpsFrames, warm = 0, 0, 0
local started = false

local GRADE = {
	-- brightness levemente positivo: junto com o Lighting mais claro do
	-- default.project.json, devolve a visibilidade que o Diretor cobrou
	brightness = 0.03,
	contrast = 0.14,
	saturation = 0.16,
	tint = Color3.fromRGB(255, 247, 235), -- leve calor de fim de tarde
}

local OWNED = { "ArkanaSky", "ArkanaAir", "ArkanaBloom", "ArkanaRays", "ArkanaGrade" }

-- ------------------------------------------------------------------ setup
--- Pega (ou cria) o filho de Lighting com este nome exato. Nome certo e classe
--- errada: some com ele — é instância local, não machuca ninguém.
local function ensure(class: string, name: string): Instance
	local found = Lighting:FindFirstChild(name)
	if found then
		if found.ClassName == class then
			return found
		end
		found:Destroy()
	end
	local inst = Instance.new(class)
	inst.Name = name
	inst.Parent = Lighting
	return inst
end

local function buildSky()
	-- dois Sky em Lighting = céu indefinido. Só o nosso fica (visão local).
	for _, child in ipairs(Lighting:GetChildren()) do
		if child:IsA("Sky") and child.Name ~= "ArkanaSky" then
			child:Destroy()
		end
	end
	local s = ensure("Sky", "ArkanaSky") :: Sky
	s.CelestialBodiesShown = true
	s.SunAngularSize = 14 -- sol presente; a hora (14.5) mantém o mundo iluminado
	s.MoonAngularSize = 9
	s.StarCount = 1800 -- já pintam no fim do dia arcano
end

local function buildAir()
	local a = ensure("Atmosphere", "ArkanaAir") :: Atmosphere
	a.Offset = 0.2
	a.Color = GOLD -- a névoa perto do horizonte puxa dourado…
	a.Decay = NIGHT -- …e decai para o azul-noite lá em cima
	a.Glare = 0.3
	-- FEEDBACK DO DIRETOR (18/08): "a área está escura, o sol não clareia quase
	-- nada". A bruma alta (1.6) somada ao sol baixo comia a visibilidade. O
	-- clima de entardecer agora vem da COR (Color/Decay/grading), não da falta
	-- de luz — a referência Pixel Gun é CLARA e saturada. Valor antigo: 1.6.
	a.Haze = 0.7
	air = a
end

local function buildPost()
	local b = ensure("BloomEffect", "ArkanaBloom") :: BloomEffect
	-- Threshold alto de propósito: só o que é REALMENTE brilhante floresce.
	-- "O que brilha é magia" (GDD §10) — parede iluminada não pode brilhar.
	b.Threshold = 1.75
	b.Intensity = 0.5
	b.Size = 20
	bloom = b

	local r = ensure("SunRaysEffect", "ArkanaRays") :: SunRaysEffect
	r.Intensity = 0.05 -- discreto: raio de sol é tempero, não prato
	r.Spread = 0.55
	rays = r

	local g = ensure("ColorCorrectionEffect", "ArkanaGrade") :: ColorCorrectionEffect
	g.Brightness = GRADE.brightness
	g.Contrast = GRADE.contrast
	g.Saturation = GRADE.saturation -- cores chapadas e saturadas (ref. Pixel Gun)
	g.TintColor = GRADE.tint
	grade = g
	-- SEM DepthOfField: é o efeito mais caro da lista em celular e o que menos
	-- comunica neste estilo blocky. Fica de fora de propósito.
end

--- Liga/desliga o que é caro. Roda no init e de novo se o vigia rebaixar.
local function applyQuality()
	local isLite = Atmosphere.isLite()
	if bloom then
		bloom.Enabled = not isLite
	end
	if rays then
		rays.Enabled = not isLite
	end
	if air then
		-- Modo Névoa literal: sem pós-processamento, a névoa engrossa e segura
		-- a leitura de distância (de quebra esconde o pop-in do terreno).
		air.Density = if isLite then 0.3 else 0.18
	end
	if grade then
		-- sem bloom, a cor precisa gritar um pouco mais para o jogo não apagar
		grade.Saturation = if isLite then GRADE.saturation + 0.06 else GRADE.saturation
	end
end

-- ------------------------------------------------------------------ API
function Atmosphere.init(_player: Player?)
	if started then
		return
	end
	started = true
	buildSky()
	buildAir()
	buildPost()
	applyQuality()
end

--- Soco de cor na tela inteira. Existe para O momento do jogo: a Sintonia
--- disparando (GDD §9). Quem chama é o Vfx — daqui não se escuta rede.
function Atmosphere.pulse(color: Color3?, strength: number?, duration: number?)
	pulseColor = color or GOLD
	pulseTotal = math.max(0.05, duration or 0.7)
	pulseLeft = math.max(pulseLeft, pulseTotal * math.clamp(strength or 1, 0, 1))
end

function Atmosphere.update(dt: number)
	if not started then
		return
	end

	-- vigia de fps: rebaixa uma vez e pronto (celular já nasce leve)
	if Q.mode == "auto" and not demoted and not touchOnly then
		warm += dt
		if warm >= Q.warmup then
			fpsAcc += dt
			fpsFrames += 1
			if fpsAcc >= Q.fpsWindow then
				if fpsFrames / fpsAcc < Q.fpsFloor then
					demoted = true
					applyQuality()
				end
				fpsAcc, fpsFrames = 0, 0
			end
		end
	end

	local g = grade
	if pulseLeft > 0 and g then
		pulseLeft = math.max(0, pulseLeft - dt)
		local k = (pulseLeft / pulseTotal) ^ 2 -- decai quadrático: o fim é suave
		g.Brightness = GRADE.brightness + 0.18 * k
		g.TintColor = GRADE.tint:Lerp(pulseColor, 0.45 * k)
		g.Contrast = GRADE.contrast + 0.1 * k
	end
end

--- Devolve o Lighting como estava. Sem isso, trocar de partida empilha efeito.
function Atmosphere.destroy()
	for _, name in ipairs(OWNED) do
		local inst = Lighting:FindFirstChild(name)
		if inst then
			inst:Destroy()
		end
	end
	air, bloom, rays, grade = nil, nil, nil, nil
	pulseLeft, fpsAcc, fpsFrames, warm = 0, 0, 0, 0
	started = false
end

return Atmosphere
