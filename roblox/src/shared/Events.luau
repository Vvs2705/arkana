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
	TutorialStep = "tutorial_step",

	--- SESSÃO — o par que responde a pergunta V5 ("o combate é gostoso o
	--- bastante para o jogador VOLTAR?", docs/ROBLOX.md §1). Era a única das
	--- cinco perguntas sem uma linha de instrumentação: `SessionEnd` estava
	--- declarado neste schema desde a R5 e NUNCA foi emitido.
	--- Ninguém mede "volta amanhã" numa sessão — mede-se o que o alpha ENTREGA:
	--- quanto tempo ficou, quantas provas jogou, e EM QUE PONTO desistiu. Um
	--- jogador que sai no lobby, um que sai no meio da primeira prova e um que
	--- fica para a terceira são três respostas diferentes para a mesma pergunta.
	SessionStart = "session_start",
	SessionEnd = "session_end",

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

--- MODO DE ENTRADA do disparo — a régua do requisito de celular (docs/ROBLOX.md
--- §4: "mira e disparo em UM gesto", que reprovou no teste do APK 2D). Viaja
--- como campo do `spell_cast` já existente, e não como remote novo: é PURA
--- telemetria, mentir aqui não dá vantagem nenhuma, e um remote a mais é
--- superfície de ataque a mais num lugar público.
---   drag = arrastou do botão e soltou (o gesto único)
---   tap  = toque curto (disparo rápido na direção da câmera)
---   key  = teclado/mouse (PC)
Events.INPUT_KINDS = { drag = true, tap = true, key = true }

--- Só o que está na lista entra no dado. Entrada desconhecida vira `nil` (e não
--- "unknown"): categoria inventada por cliente hostil não pode criar coluna no
--- relatório do Diretor.
function Events.sanitizeInput(raw: any): string?
	return if typeof(raw) == "string" and Events.INPUT_KINDS[raw] then raw else nil
end

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
