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
	--- ENVIO PROTEGIDO: no boot, a pasta `Remotes` pode ainda não ter replicado
	--- e `Net.event` estoura (espera com tempo limite e devolve nil). Sem o
	--- pcall o erro subia até o laço do Main.client, que TIRA O MÓDULO DE CENA
	--- ao primeiro erro — e o relato do aparelho sumia calado, para sempre.
	--- `sent` só trava quando o envio ACONTECEU; se falhou, a janela é
	--- reamostrada e a próxima tenta de novo. Não é reenvio periódico: é a MESMA
	--- remessa, e depois que ela passa nunca mais sai nada daqui.
	local ok = pcall(function()
		Net.event(Net.Names.DeviceInfo):FireServer({
			tier = Device.tier(fps, touch, viewportY),
			touch = touch,
		})
	end)
	sent = ok
	if not ok then
		acc, frames = 0, 0
	end
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

	-- ------------------------------------------------- o relato SAI do cliente
	--- Metade do contrato mora aqui: o cliente TEM de emitir, UMA vez, com a
	--- forma exata { tier, touch }. Se este pedaço calar, `device_tier` volta a
	--- sair "unknown" no servidor e ninguém percebe até o dashboard ficar vazio.
	local got: any, hits = nil, 0
	local conn = Net.event(Net.Names.DeviceInfo).OnServerEvent:Connect(function(_player, payload)
		got, hits = payload, hits + 1
	end)
	--- alimenta a janela inteira (aquecimento + amostra) no ritmo pedido
	local function feed(fps: number)
		local dt = 1 / fps
		for _ = 1, math.ceil((WARMUP + SAMPLE) * fps) + 2 do
			Device.update(dt)
		end
	end

	--- 1) remote ainda não replicado: NÃO pode estourar o frame do cliente
	Device.init()
	local realEvent = Net.event
	Net.event = function()
		error("Remotes ainda não replicou")
	end
	local survived = pcall(feed, 60)
	Net.event = realEvent
	assert(survived, "envio tem de sobreviver ao remote ausente")
	assert(hits == 0, "nada pode ter saído enquanto o remote faltava")

	--- 2) remote de volta: o relato que falhou ainda sai (não foi dado como
	--- enviado enquanto não saiu)
	feed(60)
	assert(hits == 1, "o cliente TEM de reportar o aparelho quando o remote existe")
	assert(typeof(got) == "table", "payload de DeviceInfo tem de ser tabela")
	assert(got.tier == "low" or got.tier == "mid" or got.tier == "high", "tier fora do contrato: " .. tostring(got.tier))
	assert(typeof(got.touch) == "boolean", "touch tem de ser booleano")

	--- 3) e é UMA vez por sessão — não vira torneira de rede
	feed(60)
	assert(hits == 1, "o contrato é UMA vez por sessão; virou reenvio")

	conn:Disconnect()
	Device.init() -- não deixa a sessão de teste marcada como já reportada
	print("[Device] autoteste OK — classificação e relato do aparelho")
	return true
end

return Device
