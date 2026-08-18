--!strict
-- ============================================================================
-- ARKANA (Roblox) — CÂMERA: PRESENÇA DO PERSONAGEM NA TELA
--
-- REPROVAÇÃO DO DIRETOR (18/08): "o boneco ficou muito pequeno, parece uma
-- miniatura". Causa: a câmera vivia entre 14 e 26 studs de distância — a essa
-- distância um personagem de ~5 studs ocupa quase nada da tela.
-- Referência aprovada: Pixel Gun 3D / Brawl Stars — o boneco TEM presença.
--
-- O QUE ESTE MÓDULO FAZ: ajusta PROPRIEDADES da câmera NATIVA do Roblox
-- (zoom, FOV, offset do humanoide). Não reescreve o sistema de câmera e NÃO
-- escreve em `Camera.CFrame` — quem escreve CFrame todo frame é o screen
-- shake da raia de VFX (BindToRenderStep em Camera+10). Brigar por CFrame
-- daria tranco duplo; aqui só mexemos nos knobs que o PlayerModule respeita.
-- Diretriz do projeto: "aproveitar os sistemas da plataforma"
-- (docs/ROBLOX.md §2).
--
-- Fiação: `CameraRig.init(player)` DEPOIS de `Controls.init` (Controls também
-- escreve zoom, no valor antigo — quem inicializa por último vence) e
-- `CameraRig.update(dt)` no laço do cliente.
-- ============================================================================

local CameraRig = {}

--- Faixa de zoom em studs (o mínimo aceito pelo motor é 0.5).
--- `start` é onde a partida COMEÇA; o jogador ainda pode aproximar/afastar
--- dentro da faixa. CALIBRAÇÃO — é o knob nº 1 do "tamanho do boneco".
local ZOOM = { min = 8, max = 16, start = 11 }

--- FOV menor = personagem maior na tela (o inverso do zoom-out). 70 (o valor
--- anterior) é grande-angular e achata o boneco; 65 aproxima sem enjoar.
local FIELD_OF_VIEW = 65

--- Deslocamento do ponto que a câmera olha, em studs, no espaço do PERSONAGEM
--- (Humanoid.CameraOffset): Y sobe a câmera (o mago desce na tela e libera a
--- linha de tiro), X joga o alvo para o ombro.
--- CUIDADO/CALIBRAÇÃO: o eixo X gira junto com o personagem — ao girar o corpo
--- o ombro troca de lado na tela. Se incomodar no playtest, zerar o X: o Y
--- sozinho já resolve "o mago cobre a mira" e é imune à rotação.
local SHOULDER = Vector3.new(1.2, 1.4, 0)

--- de quanto em quanto tempo conferimos se alguém mexeu nos nossos knobs
local RECHECK = 0.5

local player: Player? = nil
local stamped: Humanoid? = nil
local acc = 0

--- Min ANTES de Max: o motor não aceita Min > Max, e todo caminho daqui
--- ESTREITA a faixa vinda de fora (14/26), então baixar o mínimo primeiro é
--- sempre uma ordem válida.
local function applyRange(min: number, max: number)
	local p = player
	if not p then
		return
	end
	p.CameraMinZoomDistance = min
	p.CameraMaxZoomDistance = max
end

local function stampOffset(hum: Humanoid)
	stamped = hum
	hum.CameraOffset = SHOULDER
end

local function currentHumanoid(): Humanoid?
	local p = player
	local char = p and p.Character
	return if char then char:FindFirstChildOfClass("Humanoid") else nil
end

function CameraRig.init(p: Player)
	if player then
		return
	end
	player = p
	p.CameraMode = Enum.CameraMode.Classic

	-- Semear o zoom INICIAL: o Roblox não expõe "distância atual da câmera"
	-- para escrita, só a FAIXA — e clampeia a distância corrente dentro dela.
	-- Fechar a faixa no valor desejado e reabrir no frame seguinte é a forma
	-- suportada de dizer "comece a 11 studs".
	applyRange(ZOOM.start, ZOOM.start)
	task.defer(applyRange, ZOOM.min, ZOOM.max)

	local cam = workspace.CurrentCamera
	if cam then
		cam.FieldOfView = FIELD_OF_VIEW
	end

	p.CharacterAdded:Connect(function(char)
		local hum = char:WaitForChild("Humanoid", 10)
		if hum and hum:IsA("Humanoid") then
			stampOffset(hum)
		end
	end)
	local hum = currentHumanoid()
	if hum then
		stampOffset(hum)
	end
end

--- Rede de segurança barata: 2x por segundo confere se o FOV/zoom foram
--- sobrescritos (outro módulo, troca de Camera no respawn) e se o personagem
--- atual já recebeu o offset. Só ESCREVE quando o valor divergiu — assim não
--- disputa frame a frame com o screen shake nem com nada da raia de VFX.
function CameraRig.update(dt: number)
	local p = player
	if not p then
		return
	end
	acc += dt
	if acc < RECHECK then
		return
	end
	acc = 0

	local cam = workspace.CurrentCamera
	if cam and cam.FieldOfView ~= FIELD_OF_VIEW then
		cam.FieldOfView = FIELD_OF_VIEW
	end
	if p.CameraMinZoomDistance ~= ZOOM.min or p.CameraMaxZoomDistance ~= ZOOM.max then
		applyRange(ZOOM.min, ZOOM.max)
	end
	local hum = currentHumanoid()
	if hum and hum ~= stamped then
		stampOffset(hum)
	end
end

return CameraRig
