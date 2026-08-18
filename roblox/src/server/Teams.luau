--!strict
-- ============================================================================
-- TIMES / DUOS — P0 da auditoria externa de 18/08.
-- Sem times, a Sintonia podia disparar entre INIMIGOS: queimava o cooldown dos
-- dois e, pior, CONTAMINAVA a pergunta nº 1 do projeto ("dois jogadores
-- coordenam de propósito?") — não dava para separar cooperação de acidente
-- adversarial. Com times, fusão entre inimigos deixa de existir por construção.
--
-- GDD §9: "dois magos DO SQUAD". Este módulo é a única fonte de "quem é aliado".
--
-- ESCOPO DELIBERADO: uma tabela, sem eventos, sem persistência, sem placar de
-- time. O Match monta na largada e limpa no lobby; todo o resto só consulta.
-- ============================================================================

local Teams = {}

--- Um membro de time é um JOGADOR ou o Humanoid de um BOT (o Match preenche a
--- partida com bots — GDD §19.6 — e eles também precisam de lado). A própria
--- Instance é o identificador estável: não há id paralelo para dessincronizar.
export type Member = Player | Humanoid

--- duos é o padrão do jogo. O Match lê daqui; trocar o formato é trocar isto.
Teams.DEFAULT_SIZE = 2

--- CHAVES FRACAS: bot morto tem o Humanoid destruído no meio da partida, e a
--- entrada some sozinha em vez de segurar a Instance até o próximo `build`.
local function freshTable(): { [Instance]: number }
	return setmetatable({}, { __mode = "k" }) :: any
end

local teamOf: { [Instance]: number } = freshTable()

--- Monta os times da partida. A ORDEM da lista decide as duplas — o Match passa
--- os HUMANOS PRIMEIRO, de propósito: é dupla de humanos que a pergunta V1 mede;
--- bot só entra de parceiro quando sobra humano ímpar. Devolve o nº de times.
function Teams.build(roster: { Member }, size: number?): number
	Teams.clear()
	local n = math.max(1, math.floor(size or Teams.DEFAULT_SIZE))
	local teams = 0
	for i, member in ipairs(roster) do
		local id = math.floor((i - 1) / n) + 1
		teamOf[member :: any] = id
		teams = math.max(teams, id)
	end
	return teams
end

--- Id do time (nil = fora da partida / sem time — lobby, espectador, etc).
function Teams.teamOf(member: Member?): number?
	return if member then teamOf[member :: any] else nil
end

--- São aliados? Ninguém é aliado de si mesmo, e SEM TIME NÃO HÁ ALIADO: no
--- lobby (nenhum `build` ainda) a Sintonia simplesmente não funde. É o lado
--- seguro do erro — o caro é fundir com inimigo.
function Teams.areAllies(a: Member?, b: Member?): boolean
	if not a or not b or a == b then
		return false
	end
	local ta = teamOf[a :: any]
	return ta ~= nil and ta == teamOf[b :: any]
end

--- Companheiro(s) de time. Varredura linear porque o roster tem no máximo 20
--- membros (GDD §19.2) — um índice invertido aqui seria estado a dessincronizar.
function Teams.matesOf(member: Member): { Member }
	local out: { Member } = {}
	local id = teamOf[member :: any]
	if not id then
		return out
	end
	for other, t in pairs(teamOf) do
		if t == id and other ~= member then
			table.insert(out, other :: any)
		end
	end
	return out
end

function Teams.clear()
	--- tabela NOVA em vez de limpar a antiga: já nasce com o metatable de chaves
	--- fracas e não depende de `table.clear` conviver com metatable.
	teamOf = freshTable()
end

return Teams
