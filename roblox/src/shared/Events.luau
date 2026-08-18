--!strict
-- ============================================================================
-- ARKANA — SCHEMA DE TELEMETRIA (contrato compartilhado)
-- Origem: auditoria externa de 18/08/2026. O relatório é explícito:
-- "sem `is_bot`, todos os primeiros dashboards serão enganadores" e
-- "o maior buraco do projeto não é documental: não existe comportamento de
-- jogador externo para analisar".
--
-- REGRA DE OURO: todo evento carrega o CONTEXTO PADRÃO (abaixo). Quem emite não
-- precisa preencher tudo — o Telemetry enriquece com o que já sabe (time, bot,
-- fase da zona, vivos). Quem emite preenche só o que é específico do evento.
--
-- Nada aqui decide jogo. Telemetria é observação; se ela sumir, o jogo continua.
-- Dono: COORDENADOR.
-- ============================================================================

local Events = {}

--- Nomes canônicos. Use SEMPRE a constante, nunca a string solta — errar o nome
--- de um evento é o tipo de bug que só aparece quando o dashboard fica vazio.
Events.Name = {
	-- partida
	MatchStart = "match_start",
	MatchEnd = "match_end",
	SessionEnd = "session_end",
	TutorialStep = "tutorial_step",

	-- combate
	SpellCast = "spell_cast",
	SpellHit = "spell_hit",
	TacticCast = "tactic_cast",
	DodgeStart = "dodge_start",
	Damage = "damage",
	Knock = "knock",
	Elimination = "elimination",

	-- Sintonia (o funil que responde a pergunta V1 do projeto)
	SintoniaPing = "sintonia_ping",
	SintoniaAccept = "sintonia_accept",
	--- houve situação em que um combo ERA possível (dois aliados, elementos
	--- diferentes, alvo em comum ao alcance) — o denominador do funil
	SintoniaOpportunity = "sintonia_opportunity",
	SintoniaChannelStart = "sintonia_channel_start",
	SintoniaInterrupted = "sintonia_interrupted",
	SintoniaFired = "sintonia_fired",

	-- terreno
	TerrainStateChange = "terrain_state_change",
	--- mudança de terreno que teve consequência tática verificável (abate,
	--- fuga, travessia criada, cobertura destruída)
	TerrainTacticalOutcome = "terrain_tactical_outcome",

	-- social / revive
	ReviveStart = "revive_start",
	ReviveComplete = "revive_complete",

	--- GRIMÓRIO (GDD §18.3): o jogador descobriu uma interação pela primeira
	--- vez. É progressão por EXPERIMENTAÇÃO, não por poder — e é o que mede se
	--- a química do jogo está sendo descoberta sozinha ou só quando mandam.
	Discovery = "discovery",
}

--- Campos que TODO evento de combate deve carregar (o Telemetry preenche).
--- Documentado aqui para o schema ser legível sem abrir o coletor.
Events.CONTEXT_FIELDS = {
	"match_id",
	"user_id_hash",
	"is_bot", -- sem isto, humano e bot se misturam e o dado mente
	"team_id",
	"device_tier",
	"timestamp",
	"element",
	"position_bucket",
	"shield_tier",
	"alive_count",
	"zone_phase",
}

--- Granularidade do bucket de posição: telemetria não precisa de coordenada
--- exata, precisa de "onde no mapa" — e agregar reduz volume e ruído.
Events.POSITION_BUCKET_STUDS = 20

function Events.positionBucket(pos: Vector3): string
	local b = Events.POSITION_BUCKET_STUDS
	return string.format("%d,%d", math.floor(pos.X / b), math.floor(pos.Z / b))
end

--- Hash estável e curto do jogador. Não é anonimização forte — é para o log não
--- carregar id cru por toda parte; o servidor sempre sabe quem é.
function Events.userHash(userId: number): string
	return string.format("u%08x", bit32.band(userId * 2654435761, 0xFFFFFFFF))
end

return Events
