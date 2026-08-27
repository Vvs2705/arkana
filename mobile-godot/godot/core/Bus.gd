## Barramento de sinais do jogo — o "Net/Events" desta frente.
## Dono: COORDENADOR. Raia nova precisa de sinal novo? Pede, nao cria local.
## REGRA HERDADA DO PROJETO: telemetria/UI OBSERVAM por aqui e nunca decidem
## jogo. Quem aplica dano e' um lugar so' (gameplay/Combat.gd).
extends Node

## O SINAL DE DANO COMPLETO (docs/DANO.md §C1). O antigo `damage_dealt` nao carregava
## QUEM causou nem SE bateu em escudo — sem isso ficam impossiveis, todos de uma
## vez: indicador direcional, hitmarker correto (a HUD usa hoje a heuristica
## "alvo nao e' o player, logo fui eu"), credito de dano para a evolucao do
## escudo, kill feed com autor e som por tipo de acerto.
##   amount    : FLOAT — dano EFETIVO aplicado (escudo + vida). Nunca 0.
##   source    : quem causou (null = terreno/DoT/ambiente, nao tem direcao)
##   on_shield : o escudo do alvo absorveu ao menos parte deste acerto
## O antigo `damage_dealt` foi REMOVIDO em 25/08/2026: ui/Hud.gd e audio/Sfx.gd
## ja' tinham migrado, e o unico gancho restante era um ramo `else` que nunca
## rodava. Sinal sem ouvinte de producao e' contrato morto.
signal damage_applied(target: Node, amount: float, element: String, source: Node, on_shield: bool)
## ESCUDO EVOLUTIVO (GDD §5). A HUD OBSERVA; quem muda escudo e' o Combat.
signal shield_changed(entity: Node, shield: float, shield_max: float, level: int)
signal shield_broken(entity: Node)
signal entity_died(entity: Node)
## DERRUBADO E REERGUER (GDD §3.7/§3.8, §4.5, §18.6 — gameplay/Derrubado.gd).
## Vida zero nao e' mais morte: quem tem esquadrao CAI, esvaece e pode ser
## reerguido. `entity_died` continua sendo a morte DE VERDADE e sai uma vez so'
## — o derrubado nao a emite. Vocabulario 10+: DERRUBADO e ESVAECER, nunca
## mutilacao, nunca sangue.
## entity_derrubada: caiu agora. `causador` = quem derrubou (null = ambiente).
## E' o gancho do Truque de Fuga do Ilusionista (cacos de luz + reflexo caido).
signal entity_derrubada(entity: Node, causador: Node)
## entity_reerguida: levantou. `por` = quem canalizou (a Lumen da Vitalis
## entra aqui como no' proxy, nao a Vitalis).
signal entity_reerguida(entity: Node, por: Node)
## derrubado_progresso: os DOIS numeros da HUD, a 4 Hz e so' quando o player
## esta' envolvido (caido ou resgatando). Ambos 0..1:
##   esvaecimento : 1.0 acabou de cair, 0.0 apagou (x ESVAECER_S = segundos)
##   reerguer     : progresso da canalizacao do resgate
signal derrubado_progresso(entity: Node, esvaecimento: float, reerguer: float)
signal match_started
signal match_over(victory: bool)
signal mana_changed(current: float, max: float)
signal health_changed(current: float, max: float)
signal element_changed(element: String)
signal game_start_requested  # o menu pede a partida; quem troca de cena e' o Main
## A COSTURA DO TERRENO REATIVO (R19): o gameplay EMITE o impacto; o terreno
## REAGE. E' o mesmo contrato que segurou 8 fases no projeto-mae — quem muda o
## mundo nunca aplica dano, e quem aplica dano nunca muda o mundo.
signal terrain_hit(element: String, pos: Vector3, strong: bool)
signal terrain_changed(kind: String, pos: Vector3)  # p/ audio/UI observarem
signal player_killed_bot(bot_name: String)          # p/ kill feed e audio
signal dodge_performed
## O som do disparo toca no INSTANTE do cast (o impacto ja' tem o proprio som
## via terrain_hit/damage_applied). So' o PLAYER emite por ora — 6 bots na
## cadencia deles viraria cacofonia sem atenuacao por distancia; quando o Sfx
## ganhar posicionamento 3D, os bots entram.
signal spell_cast(element: String)
## TODO disparo do jogo, com POSICAO (26/08). Existe porque conjurar DENUNCIA:
## a percepcao dos bots ouve por aqui ("barulho denuncia, sons de pegadas,
## respiracao" — ordem do Diretor). Emitido por Projectile.launch, o ponto
## unico por onde todo tiro ja' passa — nenhum atirador precisou mudar.
signal disparo(pawn: Node, pos: Vector3)

## LOOT DE ARMAS ARCANAS (GDD §16.2 — R21). A HUD OBSERVA, nunca decide: quem
## equipa e' o ArmaSlot do pawn e quem troca o ataque e' o proprio spec da arma.
## loot_prompt: o player entrou/saiu do raio de um loot ("PEGAR: Cajado").
signal loot_prompt(nome: String, raridade: String, perto: bool)
## weapon_equipped: alguem trocou de arma. elementos traz 1 (varinha/cajado) ou
## os 2 FIXOS da manopla.
## `pawn` vem PRIMEIRO de proposito: quem consome le' o dono antes de qualquer
## outra coisa. Ate' 25/08/2026 este sinal nao dizia de quem era, e a HUD
## adivinhava comparando o arma_id com o slot do jogador — um BOT equipando a
## MESMA arma mudava o icone do jogador. Sinal ambiguo obriga cada consumidor a
## adivinhar, e cada um adivinha diferente.
signal weapon_equipped(pawn: Node, arma_id: String, nome: String, raridade: String, elementos: PackedStringArray)

## HABILIDADES DOS MAGOS (GDD §3 e §4 — raia GAMEPLAY/HABILIDADES). A HUD e o
## audio OBSERVAM; quem decide habilidade e' um lugar so' (gameplay/KitRunner).
## SO' O PLAYER emite (6 bots com kit viraria enxurrada) — mesma regra do
## dodge_performed.
## kit_bound: que mago entrou em campo e se ele TEM kit implementado (17 dos 20
## ainda nao tem: a HUD desenha o botao apagado).
signal kit_bound(slug: String, implementado: bool)
## kit_cooldown: emitido na BORDA (no instante do uso e no instante em que fica
## pronto), nunca por frame. tipo = "tatica" | "suprema". A HUD interpola entre
## as duas bordas, ou le KitRunner.frac_tatica()/frac_suprema() quando quiser
## o valor exato.
signal kit_cooldown(tipo: String, restante: float, total: float)
## kit_telegraph: A LEI DO §4.3 no ar — "se mata rapido, avisa antes". Sai no
## toque da suprema, com os segundos de aviso ANTES do efeito: o audio toca o
## som alto e a UI mostra o aviso nesta janela.
## TODO conjurador emite, nao so' o player: o GDD §4.3 diz que toda suprema e'
## telegrafada com som alto ANTES do impacto — se a suprema do inimigo fosse
## muda, a contra-jogada nao existiria. `pos` deixa o audio atenuar por
## distancia (raia AUDIO ja' faz isso).
signal kit_telegraph(slug: String, tipo: String, duracao: float, pos: Vector3)
## kit_state: liga/desliga de um estado nomeado do kit ("braco_livre",
## "desfocada", "silencio", "sino_espectral", "escudo_quebrado", "revelado",
## "fio_zumbido", "braco_molhado", "braco_frio"). Nome novo NAO precisa de
## sinal novo — a HUD ignora o que nao souber desenhar.
signal kit_state(nome: String, ligado: bool)

## BAU CELESTIAL (GDD §16.2 — evento de mundo, a UNICA porta da manopla).
## Quem emite: gameplay/BauCelestial.gd. A HUD OBSERVA e desenha; nunca decide.
## ⚠️ NAO e' a suprema da Vitalis: o kit dela virou "Jardim da Aurora" em 20/08.
## bau_anunciado: o bau comecou a cair. `pos` = ponto EXATO de pouso (marcador
## na bussola/minimapa), `segundos` = quanto falta ate' pousar (contagem).
signal bau_anunciado(pos: Vector3, segundos: float)
## bau_pousou: da' pra abrir a partir de agora. O marcador vira "aberto ja'".
signal bau_pousou(pos: Vector3)
## bau_canalizando: barra de progresso 0..1 do PLAYER abrindo. 0.0 = cancelou
## (saiu do raio). So' sai quando o player esta' canalizando — bot e' ruido.
## `pawn` primeiro, mesma razao de weapon_equipped: sem ele o sinal so' podia
## ser do player por convencao, e convencao nao e' contrato.
signal bau_canalizando(pawn: Node, progresso: float)
## bau_aberto: acabou. `por_player` diz se a manopla foi para o jogador ou para
## um bot (kill feed: "a manopla caiu em outras maos"); `elementos` traz os 2
## FIXOS dela, para a HUD mostrar o par que o carrossel nao pode mais trocar.
signal bau_aberto(por_player: bool, elementos: PackedStringArray)

## ESTADOS ELEMENTAIS NO ALVO (GDD §10 — o counter e' COR + FORMA + **SOM**; o
## som e' um terco da leitura, entao estado que so' tem cor esta' pela metade).
## PEDIDO DA RAIA AUDIO (R21), SEM EMISSOR AINDA: quem deveria emitir e'
## gameplay/Pawn.gd na BORDA de acender()/molhar()/atordoar()/lentificar() —
## na borda, nunca por frame, e so' quando o estado MUDA (a queimadura refresca
## sem re-emitir). `nome` usa o vocabulario que Pawn.estado() ja' devolve:
## "burn" | "wet" | "frost" | "stun". audio/Sfx.gd ja' OBSERVA e tem um timbre
## para cada um; nome desconhecido e' silencio, nunca timbre errado.
signal status_aplicado(alvo: Node, nome: String)

## ZONA / TEMPESTADE ARCANA (gameplay/Zona.gd — o circulo que fecha).
## ⚠️ Mecanica PROJETADA na raia de gameplay, ainda nao escrita no GDD: ele so'
## a PRESSUPOE ("zona fechando" no Degrau 3, "revela a proxima zona segura" no
## Vidente, "ate' a zona entrar na fase final" no resgate). Numeros e fases em
## Zona.gd, aguardando o carimbo do Diretor.
## Quem emite: gameplay/Zona.gd. A HUD OBSERVA e desenha; nunca decide.
## Para o valor CONTINUO (seta da bussola, distancia ate' a borda) a HUD LE'
## direto `zona.centro` / `zona.raio` / `zona.dentro(pos)` — nao existe sinal
## por frame, de proposito.
## zona_avisou: a zona esta' PARADA e o PROXIMO circulo ja' e' publico. `centro`
## e `raio` sao os do circulo NOVO, `segundos` e' quanto falta ate' a parede
## comecar a andar (contagem regressiva "A tempestade avanca em X").
## ---------------------------------------------------------------- A QUEDA
## O inicio de partida de battle royale: o castelo voador cruza o mapa, o
## jogador salta, cai, plana e pousa. Ordem do Diretor (26/08): durante a queda
## o mago NAO tem poder nenhum alem do que nasce com ele — nada de magia no ar.
##
## fase: "no_castelo" | "caindo" | "planando" | "pousou"
signal queda_fase(fase: String)
## Altura acima do solo, em metros, durante a queda. Para a HUD desenhar o
## altimetro. Quem manda e' a raia da queda; UI e audio so' observam.
signal queda_altura(metros: float, velocidade: float)
## Rota do castelo pelo mapa, publicada uma vez no comeco da partida, para a
## HUD/minimapa poder desenhar a linha por onde da' para saltar.
signal castelo_rota(inicio: Vector3, fim: Vector3, duracao: float)

## zona_abertura: A TEMPESTADE AINDA NAO EXISTE e vai existir em `segundos`.
## E' o cronometro de 1:10 que roda com o MAPA INTEIRO ABERTO, depois do pouso
## (ordem do Diretor, 27/08). A HUD conta para tras; ninguem toma dano aqui.
signal zona_abertura(segundos: float)
## zona_formando: a parede esta' APARECENDO em torno do mapa, vindo de fora e
## parando na borda. Nao tira chao de ninguem — e' o anuncio de que a fase de
## exploracao acabou.
signal zona_formando(raio: float, duracao: float)
signal zona_avisou(fase: int, centro: Vector3, raio: float, segundos: float)
## zona_fechando: a parede COMECOU a andar, e leva `duracao` segundos ate' o
## `raio` novo. O anel do proximo circulo apaga aqui.
signal zona_fechando(fase: int, centro: Vector3, raio: float, duracao: float)
## zona_dano: o PLAYER levou o tique da tempestade (1x por segundo, NUNCA por
## frame — docs/DANO.md §2.3). `dano` = efetivo aplicado, `dps` = o da fase,
## para a HUD mostrar o quanto vai doer se ele nao correr.
signal zona_dano(dano: float, dps: float)
## zona_estado: BORDA (o player atravessou a parede), nao estado por frame.
## `dentro` false = acende a vinheta e o aviso "VOLTE PARA A ZONA".
signal zona_estado(dentro: bool)
