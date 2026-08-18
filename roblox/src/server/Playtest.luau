--!strict
-- ============================================================================
-- ARKANA (Roblox) — HARNESS DE PLAYTEST (raia 6.1: Analista de Dados)
-- Ferramenta do DIRETOR, não do jogo. Nada aqui roda sozinho: este módulo só
-- existe quando alguém o requer na Command Bar. O servidor nunca o carrega.
--
-- COMO USAR (Studio: aperte Play, abra a Command Bar e troque o contexto de
-- "Cliente" para "SERVIDOR" no seletor da barra — sem isso nada disto existe):
--
--   local P = require(game.ServerScriptService.Server.Playtest)
--
--   P.duo()          -- começa a partida AGORA, sem esperar o lobby, com os
--                       humanos que estiverem no servidor e ZERO bots. É o
--                       modo do teste de duplas: nenhum bot contamina o dado.
--   P.duo(3)         -- idem, mas com exatamente 3 bots (teste sozinho precisa
--                       de pelo menos 1 — com 0 bots e 1 humano a partida acaba
--                       na hora, porque só há um vivo).
--   P.report()       -- imprime o relatório da telemetria AGORA (pode chamar no
--                       meio da partida; a coleta continua).
--   P.variant()      -- mostra qual coorte de Sintonia está ativa (A ou B).
--   P.variant("B")   -- explica como trocar (é edição de arquivo + reinício —
--                       ver a nota de honestidade no fim deste cabeçalho).
--   P.trace(true)    -- imprime cada evento de telemetria conforme acontece.
--   P.reset()        -- DESCARTA o que foi coletado, abre janela nova e devolve
--                       os valores de Balance.match que o `duo()` mexeu.
--
-- POR QUE `duo()` MEXE EM BALANCE E NÃO CHAMA O MATCH: o Match não expõe
-- `startMatch` (é local) nem `forceEnd`. O caminho honesto sem editar arquivo de
-- outra raia é mexer nos MESMOS dados que o Match lê a cada passo do lobby.
-- ponytail: TETO CONHECIDO — isto é um empurrão no lobby, não um controle de
-- partida: não dá para ENCERRAR uma partida em andamento por aqui. O upgrade é
-- o Match expor `Match.forceStart()` / `Match.forceEnd()`; aí estas funções
-- viram uma linha cada.
--
-- NOTA DE HONESTIDADE (A/B da Sintonia): `Balance.sintonia` é RESOLVIDA no
-- require, a partir de `Balance.experiment.sintoniaVariant`, e a tabela de
-- variantes (`SINTONIA_VARIANTS`) é local do Balance — não é exportada. Trocar
-- de coorte em runtime exigiria repetir os números da variante B aqui dentro, o
-- que quebra a regra do projeto ("números viram dados; o código lê dados") e
-- deixaria dois lugares para rebalancear. Além disso, coorte trocada no meio da
-- sessão contamina as DUAS. Logo: variante se troca editando
-- `roblox/src/shared/Balance.luau` → `Balance.experiment.sintoniaVariant` e
-- rodando de novo. `P.variant()` só diz o que está valendo — e o relatório
-- carimba a variante no cabeçalho para o dado nunca ficar órfão da condição.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))

local Playtest = {}

--- valores originais do lobby, fotografados no require: `reset()` devolve.
local ORIGINAL = {
	lobbyWait = Balance.match.lobbyWait,
	minPlayersToStart = Balance.match.minPlayersToStart,
	fillWithBots = Balance.match.fillWithBots,
	maxPlayers = Balance.match.maxPlayers,
}

local function telemetry(): any
	local mod = script.Parent and script.Parent:FindFirstChild("Telemetry")
	local ok, m = pcall(function()
		return require(mod :: ModuleScript)
	end)
	return if ok then m else nil
end

--- Começa a partida no próximo passo do Match, sem esperar os 25 s de lobby.
--- `bots` = quantos bots entram (0 = só humanos, que é o teste de duplas).
function Playtest.duo(bots: number?): boolean
	local M = Balance.match
	local botCount = math.max(0, math.floor(tonumber(bots) or 0))
	local humans = #Players:GetPlayers()
	if humans == 0 then
		warn("[Playtest] ninguém no servidor — aperte Play antes")
		return false
	end
	if humans + botCount < 2 then
		warn("[Playtest] partida de 1 acaba sozinha: chame P.duo(1) ou entre com um segundo jogador")
	end

	M.lobbyWait = 0
	-- PISCADA: por um passo o Match precisa achar que a partida NÃO pode
	-- começar. É o único ramo em que ele reatribui `lobbyLeft = M.lobbyWait` —
	-- ou seja, o único jeito de zerar uma contagem regressiva já em curso.
	M.minPlayersToStart = math.huge
	M.fillWithBots = false
	RunService.Heartbeat:Wait()

	M.minPlayersToStart = 1
	M.fillWithBots = botCount > 0
	M.maxPlayers = humans + botCount
	RunService.Heartbeat:Wait()

	-- NÃO abrimos a janela de telemetria aqui: o próprio Match chama
	-- `Telemetry.matchStart(matchId)` dentro do `startMatch` que acabou de rodar
	-- neste mesmo Heartbeat. Chamar de novo zeraria a coleta que acabou de abrir
	-- (e trocaria o id da partida por um apelido).
	print(string.format("[Playtest] partida forçada — %d humano(s) + %d bot(s), variante Sintonia %s", humans, botCount, tostring(Balance.experiment.sintoniaVariant)))
	return true
end

--- Imprime o relatório da telemetria (a coleta segue aberta).
function Playtest.report(): boolean
	local tel = telemetry()
	if not tel or not tel.report then
		warn("[Playtest] telemetria indisponível")
		return false
	end
	local ok, text = pcall(tel.report)
	if not ok then
		warn("[Playtest] o relatório falhou: " .. tostring(text))
		return false
	end
	print(text)
	return true
end

--- Coorte de Sintonia ativa. Com argumento, ensina a trocar (ver NOTA DE
--- HONESTIDADE no cabeçalho: é edição de arquivo, não interruptor de runtime).
function Playtest.variant(want: string?): string
	local active = tostring(Balance.experiment.sintoniaVariant)
	local S = Balance.sintonia
	print(string.format("[Playtest] variante ATIVA: %s — canalização %.2fs, cooldown compartilhado %.1fs, dano x%.2f", active, S.channel, S.sharedCooldown, S.dmgMult))
	if typeof(want) == "string" and want ~= active then
		print("[Playtest] para rodar a coorte " .. want .. ":")
		print("   1) pare o Play;")
		print('   2) edite roblox/src/shared/Balance.luau → Balance.experiment.sintoniaVariant = "' .. want .. '";')
		print("   3) Play de novo e rode P.duo() — o relatório carimba a variante sozinho.")
		print("   (trocar em runtime repetiria os números da variante em outro arquivo e")
		print("    contaminaria as duas coortes na mesma sessão)")
	end
	return active
end

--- Traço ao vivo dos eventos (útil para ver se a instrumentação está disparando).
function Playtest.trace(on: boolean?): boolean
	local tel = telemetry()
	if not tel then
		return false
	end
	tel.VERBOSE = on ~= false
	print("[Playtest] traço de telemetria " .. (if tel.VERBOSE then "LIGADO" else "desligado"))
	return tel.VERBOSE
end

--- Descarta a coleta, abre janela nova e devolve o lobby ao que era.
function Playtest.reset(): boolean
	Balance.match.lobbyWait = ORIGINAL.lobbyWait
	Balance.match.minPlayersToStart = ORIGINAL.minPlayersToStart
	Balance.match.fillWithBots = ORIGINAL.fillWithBots
	Balance.match.maxPlayers = ORIGINAL.maxPlayers
	local tel = telemetry()
	if tel and tel.matchStart then
		pcall(tel.matchStart, "reset")
	end
	print("[Playtest] lobby restaurado e telemetria zerada (o que foi coletado FOI DESCARTADO)")
	return true
end

return Playtest
