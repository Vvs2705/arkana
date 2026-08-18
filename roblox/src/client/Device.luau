--!strict
-- ============================================================================
-- RAIA D (UI) — RELATO DO APARELHO (telemetria pura).
--
-- POR QUE EXISTE: sem este dado, "60 fps no aparelho alvo" (GDD §19.2) é
-- SENSAÇÃO, não número. O playtest com gente de fora vai gerar reclamação de
-- travamento e ninguém vai saber em que classe de aparelho ela aconteceu.
--
-- É SÓ TELEMETRIA — nada de jogo depende disto, então mentir aqui não dá
-- vantagem nenhuma (é por isso que pode vir do cliente sem ferir a regra de
-- ouro do docs/ROBLOX.md §3). O servidor recebe e ANOTA; se ignorar, o jogo
-- continua idêntico.
--
-- COMO ESTIMA: mede os primeiros segundos de jogo já rodando (o cliente é o
-- único que sabe o próprio fps) e cruza com toque × resolução. Nada de lista de
-- modelos — lista de aparelho envelhece; fps medido, não.
-- Envia UMA vez, em `Net.Names.DeviceInfo`: { tier, touch }.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local UserInputService = game:GetService("UserInputService")
local Workspace = game:GetService("Workspace")

local Net = require(ReplicatedStorage:WaitForChild("Shared"):WaitForChild("Net"))

local Device = {}

--- Janela de medição. Começa DEPOIS do aquecimento: carregar mundo derruba o
--- fps de qualquer aparelho e classificaria todo mundo como "low".
--- CALIBRAÇÃO: são estes dois números se o relato vier sistematicamente baixo.
local WARMUP = 4.0
local SAMPLE = 3.0

--- Pisos de fps. Espelham o `Atmosphere.Quality.fpsFloor` (42) de propósito:
--- quem o vigia de qualidade rebaixaria é exatamente quem queremos ver no
--- relatório como "low".
local FPS_LOW = 42
local FPS_HIGH = 55
--- altura de viewport a partir da qual o aparelho é grande o bastante para
--- "high" (celular em paisagem raramente passa disto)
local TALL = 800

--- Classe do aparelho a partir do que o CLIENTE sabe. Pura de propósito: é a
--- única regra deste arquivo e o autoteste no fim cobre ela.
function Device.tier(fps: number, touch: boolean, viewportY: number): string
	if fps < FPS_LOW then
		return "low"
	end
	if touch then
		-- celular que segura 55+ é bom celular, mas não é desktop: "high" fica
		-- reservado para quem tem folga de verdade
		return if fps >= FPS_HIGH then "mid" else "low"
	end
	if fps >= FPS_HIGH and viewportY >= TALL then
		return "high"
	end
	return "mid"
end

local warm = 0
local acc, frames = 0, 0
local sent = false

function Device.init(_player: Player?)
	warm, acc, frames, sent = 0, 0, 0, false
end

function Device.update(dt: number)
	if sent then
		return
	end
	if warm < WARMUP then
		warm += dt
		return
	end
	acc += dt
	frames += 1
	if acc < SAMPLE then
		return
	end
	sent = true

	local render = frames / acc
	--- `GetRealPhysicsFPS` é o relógio de física (teto 60). Um aparelho que já
	--- está apanhando derruba os dois; ficar com o MENOR evita chamar de "high"
	--- quem só teve uma janela de render boa.
	local ok, phys = pcall(function()
		return Workspace:GetRealPhysicsFPS()
	end)
	local fps = if ok and typeof(phys) == "number" and phys > 0 then math.min(render, phys) else render

	local touch = UserInputService.TouchEnabled and not UserInputService.KeyboardEnabled
	local cam = Workspace.CurrentCamera
	local viewportY = if cam then cam.ViewportSize.Y else 640

	-- contrato de Net.Names.DeviceInfo: exatamente { tier, touch }
	Net.event(Net.Names.DeviceInfo):FireServer({
		tier = Device.tier(fps, touch, viewportY),
		touch = touch,
	})
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (cliente):
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Device).selfTest()
-- ============================================================================
function Device.selfTest()
	assert(Device.tier(20, true, 640) == "low", "celular travando tem de ser low")
	assert(Device.tier(20, false, 1080) == "low", "desktop travando tem de ser low")
	assert(Device.tier(58, true, 640) == "mid", "celular liso é mid, nunca high")
	assert(Device.tier(45, true, 640) == "low", "celular no limiar é low")
	assert(Device.tier(60, false, 1080) == "high", "desktop folgado tem de ser high")
	assert(Device.tier(60, false, 600) == "mid", "tela pequena sem toque não é high")
	assert(Device.tier(48, false, 1080) == "mid", "fps intermediário é mid")
	print("[Device] autoteste OK — classificação de aparelho")
	return true
end

return Device
