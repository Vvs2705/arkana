using System;

namespace Arkana.Core
{
    /// <summary>
    /// NUMEROS DO JOGO — espelho de mobile-godot/godot/core/Balance.gd (GDD §4, §5, §14, §19.3).
    /// Dono: COORDENADOR. Rebalancear = editar AQUI, nunca cacar constante em cena.
    /// Os marcados KNOB so' o playtest calibra; o comentario diz o que AUMENTAR/DIMINUIR faz.
    /// </summary>
    public static class Balance
    {
        public static class Player
        {
            public const float Hp = 100f;
            public const float ManaMax = 100f;
            /// <summary>KNOB (DANO.md §7.2: 14 -> 16). A MANA, nao a cadencia, e' o teto real do DPS.</summary>
            public const float ManaRegen = 16f;
            /// <summary>m/s — KNOB, recalibrar no aparelho.</summary>
            public const float Speed = 7.5f;
            /// <summary>Salto arcano: 5.2 m/s da' ~1,4 m — passa cerca, NAO vira plataforma. KNOB.</summary>
            public const float JumpV = 5.2f;
        }

        /// <summary>
        /// Perfil de um elemento: 5 numeros de chassi + 4 fatores ADIMENSIONAIS que a arma NAO multiplica
        /// (o tier muda QUANTO, nunca O QUE). Esc/Vida = papel dentro do duelo; Empurrao x Combate.Knockback;
        /// Estrutura x dano em muro/torreta/totem.
        /// </summary>
        public sealed class PerfilElemento
        {
            public float Dmg, ManaCost, FireRate, ProjectileSpeed, Range, Esc, Vida, Empurrao, Estrutura;
        }

        /// <summary>FOGO — a REGUA. Linha neutra de proposito.</summary>
        public static readonly PerfilElemento Fogo = new PerfilElemento
        {
            Dmg = 11f, ManaCost = 7.5f, FireRate = 0.26f, ProjectileSpeed = 26f, Range = 34f,
            Esc = 1.00f, Vida = 1.00f, Empurrao = 1.0f, Estrutura = 1.0f
        };
        /// <summary>AGUA — o SETUP. Agua sozinha perde; agua ANTES do raio ganha.</summary>
        public static readonly PerfilElemento Agua = new PerfilElemento
        {
            Dmg = 9.3f, ManaCost = 6.9f, FireRate = 0.28f, ProjectileSpeed = 20f, Range = 32f,
            Esc = 1.00f, Vida = 1.00f, Empurrao = 1.0f, Estrutura = 1.0f
        };
        /// <summary>RAIO — o ABRE-ESCUDO (1.25x em escudo). Projetil mais rapido; quase-hitscan, NUNCA hitscan.</summary>
        public static readonly PerfilElemento Raio = new PerfilElemento
        {
            Dmg = 12.3f, ManaCost = 8.8f, FireRate = 0.32f, ProjectileSpeed = 35f, Range = 38f,
            Esc = 1.25f, Vida = 1.00f, Empurrao = 1.0f, Estrutura = 1.6f
        };
        /// <summary>TERRA — o FINALIZADOR (1.15x em vida) e o ANTIESTRUTURA (2.0). O mais lento e pesado.</summary>
        public static readonly PerfilElemento Terra = new PerfilElemento
        {
            Dmg = 13.6f, ManaCost = 9.3f, FireRate = 0.35f, ProjectileSpeed = 18f, Range = 28f,
            Esc = 1.00f, Vida = 1.15f, Empurrao = 1.2f, Estrutura = 2.0f
        };
        /// <summary>VENTO — o CONTROLE. Vento nao mata: vento tira do lugar (empurrao 2x, escudo 0.75x).</summary>
        public static readonly PerfilElemento Vento = new PerfilElemento
        {
            Dmg = 7.5f, ManaCost = 6.1f, FireRate = 0.23f, ProjectileSpeed = 27f, Range = 30f,
            Esc = 0.75f, Vida = 1.00f, Empurrao = 2.0f, Estrutura = 0.6f
        };

        public static PerfilElemento Perfil(Elemento e)
        {
            switch (e)
            {
                case Elemento.Fogo: return Fogo;
                case Elemento.Agua: return Agua;
                case Elemento.Raio: return Raio;
                case Elemento.Terra: return Terra;
                case Elemento.Vento: return Vento;
            }
            throw new ArgumentOutOfRangeException(nameof(e));
        }

        /// <summary>
        /// ESCUDO DE MAGIA EVOLUTIVO (GDD §5). Absorve ANTES da vida e o excedente do MESMO tiro transborda
        /// (sem transbordo o ultimo tiro contra 3 pontos de escudo desperdica 10 e o TTK ganha degrau aleatorio).
        /// NAO regenera sozinho: so' pergaminho, passiva da Tessa e Suprema do Brok.
        /// </summary>
        public static class Escudo
        {
            /// <summary>Capacidade por nivel (1..4).</summary>
            public static readonly float[] Niveis = { 50f, 75f, 100f, 125f };
            /// <summary>Dano causado ACUMULADO para entrar no nivel.</summary>
            public static readonly float[] Evoluir = { 0f, 150f, 400f, 900f };
            /// <summary>Branco / azul / roxo / dourado (hex sem #).</summary>
            public static readonly string[] Cores = { "FFFFFF", "4C8CFF", "9B5CE6", "F0C75E" };
            /// <summary>Lei; desligar so' em playtest.</summary>
            public const bool Transbordo = true;
            /// <summary>KNOB: >0 desfaz o TTK alvo. 0 e' o valor de projeto.</summary>
            public const float Regen = 0f;
        }

        /// <summary>
        /// EFEITOS SECUNDARIOS (DANO.md §3.3/§3.4). Estado EXCLUSIVO por categoria (um termico, um de movimento);
        /// a reacao SUBSTITUI, nunca soma — previsivel e' aprendivel.
        /// </summary>
        public static class Status
        {
            public const float BurnDps = 4f, BurnDur = 3f;
            /// <summary>Queimadura aticada pelo vento (GDD §16.4).</summary>
            public const float BurnFannedDps = 7f, BurnFannedBonus = 2f;
            /// <summary>Molhado habilita a conducao.</summary>
            public const float WetDur = 5f, WetSlow = 0.90f;
            /// <summary>Molhado + agua de novo.</summary>
            public const float HypothermiaSlow = 0.80f;
            /// <summary>Raio em alvo MOLHADO.</summary>
            public const float ConductMult = 1.50f, ConductStun = 0.40f;
            /// <summary>Arco para outro molhado perto.</summary>
            public const float ConductArcM = 4f, ConductArcMult = 0.50f;
            /// <summary>TETO DO KERNEL. NUNCA SUBIR: acima de 0.8s o jogador perde o controle.</summary>
            public const float StunCap = 0.80f;
        }

        /// <summary>DANO AO LONGO DO TEMPO — as duas leis (DANO.md §3.5).</summary>
        public static class Dot
        {
            /// <summary>Teto SOMADO. AUMENTAR: o jogador cai de fontes que nao le. DIMINUIR: terreno vira enfeite.</summary>
            public const float TetoDps = 12f;
            /// <summary>KNOB: DoT vai direto na VIDA — o escudo protege contra MAGIA, nao contra estar em chamas.</summary>
            public const bool IgnoraEscudo = true;
            /// <summary>s por aplicacao. MATA o dano de 0 a 60Hz. DIMINUIR = volta o numero zero e o tween por frame.</summary>
            public const float Tick = 0.25f;
        }

        /// <summary>FEEDBACK DE DANO. Cor + FORMA + SOM e' lei (GDD §10).</summary>
        public static class Feedback
        {
            /// <summary>Branco-azulado: acerto em ESCUDO.</summary>
            public const string CorEscudo = "CFE6FF";
            /// <summary>AUMENTAR: os numeros somem numa soma so'. DIMINUIR: escada ilegivel.</summary>
            public const float NumMergeS = 0.35f;
            public const float NumLifeS = 0.60f;
            public const float NumScaleBase = 0.80f, NumScaleGain = 0.50f, NumScaleMax = 1.60f;
            public const float HitSoundMinS = 0.08f;
            public const float DotNumEveryS = 0.50f;
            public const float VignetteMinS = 0.12f;
            public const float ArcDurS = 1.40f, ArcDeg = 60f;
            public const int ArcMax = 3;
        }

        public static class Combate
        {
            /// <summary>m/s base do empurrao; x PerfilElemento.Empurrao. AUMENTAR: todo acerto vira controle.</summary>
            public const float Knockback = 2.2f;
            /// <summary>Alcance de REFERENCIA de toda medicao de TTK.</summary>
            public const float RefRangeM = 12f;
        }

        /// <summary>
        /// TERRENO REATIVO (GDD §14). O fogo propaga por ORCAMENTO (uma rolagem por aresta), NUNCA por chance
        /// por tique — chance por tique carbonizou 380/380 celulas em 100% das rodadas no projeto-mae.
        /// </summary>
        public static class Terrain
        {
            public const float CellSize = 3f;
            /// <summary>Celulas que UMA ignicao pode espalhar (a lei).</summary>
            public const int FuelBudget = 16;
            /// <summary>1 rolagem POR ARESTA (nunca por tique).</summary>
            public const float EdgeChance = 0.5f;
            public const float BurnDuration = 8f;
            public const float BurnDps = 6f;
            public const float FreezeDuration = 10f;
            public const float ElectrifyDuration = 3f;
            /// <summary>KNOB (10 -> 8). AUMENTAR: a poca mata sozinha. DIMINUIR: agua+raio perde a razao.</summary>
            public const float ElectrifyDps = 8f;
            public const float MudDuration = 6f;
            /// <summary>Fator no produto de velocidade (piso: nunca 0).</summary>
            public const float MudSlow = 0.55f;
            public const float WallHp = 60f;
            public const float WallDuration = 12f;
            public const float WallHeight = 3.5f;
        }

        public static class Dodge
        {
            public const float Distance = 5f;      // m — KNOB
            public const float Duration = 0.18f;
            public const float Cooldown = 2.6f;    // Roblox pos-calibracao R5
            public const float Iframes = 0.12f;    // s de invulnerabilidade no inicio — KNOB
            /// <summary>Rampa de burst*media ate (2-burst)*media: mesma DISTANCIA, arranque dobrado. >2.0 estoura.</summary>
            public const float Burst = 1.85f;
            /// <summary>0 = para seco e reacelera, 1 = emenda na corrida.</summary>
            public const float ExitMomentum = 1f;
        }

        /// <summary>FLUTUAR — segurar o SALTO no ar segura a queda pagando a MESMA mana do disparo. Nao e' voo.</summary>
        public static class Flutuar
        {
            /// <summary>Com regen 16/s, 22 e' deficit real: quem flutua fica sem tiro. DIMINUIR = combate sai do chao.</summary>
            public const float ManaPorS = 22f;
            /// <summary>Teto por flutuacao: atravessa um vao, nao vira posicao de sniper.</summary>
            public const float DurMax = 2f;
            /// <summary>m/s de descida (~1/8 da queda livre): magia segurando o corpo, nao elevador parado.</summary>
            public const float DescV = 1.2f;
        }

        /// <summary>GAME FEEL (R20) — o PESO do personagem. Tudo passa por aceleracao; todo numero e' KNOB.</summary>
        public static class Move
        {
            public const float Accel = 45f;        // m/s^2 — 0.17s ate a velocidade cheia
            public const float Brake = 60f;        // m/s^2 — para em 0.13s; MENOR patina
            public const float TurnAccel = 95f;    // inversao de sentido; alto p/ o strafe responder
            public const float AirControl = 0.35f;
            /// <summary>TEM QUE BATER com a do joystick virtual, senao sobra degrau.</summary>
            public const float StickDeadzone = 0.12f;
            public const float StickCurve = 1.4f;  // >1 = curso fino perto do centro
            /// <summary>HISTERESE: entra em run acima de Enter, so' volta a idle abaixo de Exit (senao pisca).</summary>
            public const float RunAnimEnter = 1.3f, RunAnimExit = 0.5f;
            public const float TurnRateAim = 15f, TurnRateFree = 10f;
            public const float TurnPivotBonus = 0.7f;
            /// <summary>Banking: inclina pra dentro da curva. Max ~7.5 graus; acima parece moto.</summary>
            public const float BankGain = 0.035f, BankMax = 0.13f, BankRate = 9f;
            /// <summary>KNOB (onda 15B, padrao do genero; o Diretor pode vetar com 1): RECUAR MIRANDO anda a esta fracao.
            /// DIMINUIR = recuar vira castigo e o pe' do Walk_Backward escorrega menos; 1 = recua na velocidade cheia.</summary>
            public const float BackpedalMult = 0.7f;
            /// <summary>Graus entre a MIRA e o MOVIMENTO a partir dos quais o corpo RECUA (pernas para tras). O BackpedalMult
            /// entra em rampa de RecuoGraus-RecuoRampa (1) a RecuoGraus+RecuoRampa (cheio): sem degrau no strafe diagonal.</summary>
            public const float RecuoGraus = 105f, RecuoRampa = 15f;
            /// <summary>KNOB (04/10, toque de celular): s depois de sair da BORDA em que o salto ainda vale (coyote). So' saindo
            /// andando — nunca depois de um pulo (sem pulo duplo). AUMENTAR = pulo no ar; DIMINUIR = toque atrasado perdido.</summary>
            public const float CoyoteS = 0.10f;
            /// <summary>KNOB (04/10): s que um toque de SALTO no ar fica guardado e sai no quadro em que o pe' toca o chao (jump
            /// buffer). Uma vaga so', sem fila. AUMENTAR = pula sozinho ao pousar; DIMINUIR = toque cedo perdido.</summary>
            public const float PuloGuardadoS = 0.12f;
            /// <summary>Graus de rampa que o corpo sobe (CharacterController.slopeLimit) e acompanha DESCENDO sem descolar.</summary>
            public const float InclinacaoMax = 50f;
        }

        /// <summary>ANIMACAO x VELOCIDADE (R20): speed_scale sai da velocidade real — fim da patinacao.</summary>
        public static class Anim
        {
            /// <summary>MEDIDO no pyra.glb (21/08): root motion de 7.91 m por ciclo. Pe' escorrega pra tras = DIMINUA.</summary>
            public const float RunStrideM = 7.91f;
            public const float ScaleMin = 0.55f, ScaleMax = 1.9f;
            /// <summary>MEDIDO no 01-pyra.fbx (16/09): o Walk_Backward anda 0,90 m em 0,875 s de clipe (o importador tira esse
            /// deslocamento; a lei da patinacao do recuo sai daqui). Pe' escorrega para a frente = DIMINUA.</summary>
            public const float BackStrideM = 0.90f;
            /// <summary>KNOB: teto da cadencia do andar para tras. O clipe e' um ANDAR (1 m/s): recuando a 5,25 m/s ele satura
            /// aqui e o pe' escorrega. AUMENTAR = passo de formiga; DIMINUIR = mais patinacao.</summary>
            public const float BackScaleMax = 2.6f;
            /// <summary>Graus que o tronco desfaz do giro das pernas (Spine02/Spine01/Spine, um terco cada). Acima disso o peito
            /// sai da mira. AUMENTAR = coruja; DIMINUIR = o tiro sai de lado.</summary>
            public const float TorcaoMax = 75f;
            /// <summary>Histerese do recuo: entra em Move.RecuoGraus, so' volta a correr de frente abaixo de RecuoGraus - isto.</summary>
            public const float RecuoBanda = 15f;
            /// <summary>1/s — com que pressa as pernas acham o rumo novo (exponencial). MAIOR = pivo seco; MENOR = deslize.</summary>
            public const float PernasRate = 12f;
            /// <summary>KNOB: a aterrissagem do Regular_Jump toca nesta velocidade (0,78 s de clipe -> ~0,5 s). Parado so'.</summary>
            public const float PousoVel = 1.5f;
            /// <summary>MEDIDO no Running do 01-pyra.fbx (quadro 12 de 16): os dois pes fora do chao, joelho da frente erguido,
            /// pe' de tras alto. E' o quadro que o mago SEM o Regular_Jump segura no ar (o pulo de reserva).</summary>
            public const float PuloReservaT = 0.46f;

            // ---- MECANIM / MIXAMO (04/10/2026, design/pipeline/MIXAMO.md) ----
            /// <summary>KNOB: o Mecanim (biblioteca do Mixamo) vale para os 20 magos; false = so' o Validation Set (Fizz,
            /// Corvomante, Basalto). Sem o controller ou com o Avatar invalido, o mago cai no legado sozinho.</summary>
            public const bool MecanimEmTodos = true;
            /// <summary>KNOB: teto da cadencia da passada acima do clipe mais rapido. A corrida do jogo (7,5 m/s) passa do
            /// sprint do Mixamo (4,7 m/s num humano de 1,8 m): ate' aqui as pernas aceleram, dai' para cima o pe' escorrega.
            /// AUMENTAR = pernas frenéticas no mago pequeno; DIMINUIR = mais patinacao.</summary>
            public const float CadenciaMax = 2.2f;
            /// <summary>KNOB: a magia de 1 mao (Standing 1H Magic Attack 01, 2,3 s) comeca nesta fracao do clipe (a mao ja'
            /// indo a frente: o projetil sai no toque, nao no fim de uma preparacao) e toca nesta velocidade.</summary>
            public const float CastInicioN = 0.22f, CastVel = 1.6f;
            /// <summary>s que o gesto do tronco segura antes de soltar a camada (magia, tatica, suprema, golpe).</summary>
            public const float CastSeguraS = 0.55f, KitSeguraS = 0.9f, GolpeSeguraS = 0.35f;
        }

        public static class Touch
        {
            /// <summary>dp, nunca px.</summary>
            public const float AimDeadzoneDp = 14f;
            /// <summary>KNOB — calibrar SEMPRE junto com a deadzone.</summary>
            public const int TapMaxMs = 220;
        }

        /// <summary>A duracao NAO e' o fim da partida — e' a rede de seguranca. O fim de verdade e' ultimo em pe'.</summary>
        public static class Match
        {
            /// <summary>480 s = 70 abertura + 10 formacao + 5 fases (396 s) + folga da queda. Baixar reabre fim por cronometro.</summary>
            public const float DurationS = 480f;
            /// <summary>Ilha do Documento Mestre: 180 abertura + 10 + 5 fases do Zona.FASES_GRANDE (1.320 s) + folga.</summary>
            public const float DurationGrandeS = 1800f;
            /// <summary>12 e' o teto estrutural (14 nascimentos) e o passo que se paga sem medir FPS no celular.</summary>
            public const int Bots = 12;
            /// <summary>Modo em TIME (25/09/2026, ref. PUBG Mobile): TRIO — player + 2 parceiros bots + 5 trios de bots = 18 corpos.
            /// A meta e' 40-60 no mapa de 4,8 km; subir TimesInimigos so' depois de medir a percepcao dos bots (O(n2)) no aparelho.</summary>
            public const int TamanhoDoTime = 3;
            public const int TimesInimigos = 5;
            /// <summary>Nome antigo (era 6 duplas): os times inimigos, qualquer tamanho.</summary>
            public const int DuplasInimigas = TimesInimigos;
        }

        /// <summary>
        /// SINTONIA (GDD §9; regra em Core.Sintonia). Numeros = VARIANTE A do Roblox (Balance.luau, sintoniaVariant) e
        /// NAO VALIDADOS (PONTE A19 e §4 item 1): o proprio Roblox abriu um A/B (a B e' 0,75 s / 18 s / x2,0) porque
        /// ninguem sabe qual funciona. Entram como KNOB; quem julga e' o Diretor, no aparelho.
        /// DISTANCIA: studs -> metros a ~0,5 m/stud. Medido nos pares que existem nos dois jogos: velocidade 0,34
        /// (22 studs/s -> 7,5 m/s), alcance ~0,38 (90 studs -> 28-38 m), celula de terreno 0,75 (4 -> 3 m); 0,5 e' o meio.
        /// </summary>
        public static class Sintonia
        {
            /// <summary>s para o 2o aliado impactar. AUMENTAR = combo por acaso; DIMINUIR = so' dupla treinada combina.</summary>
            public const float JanelaS = 1.5f;
            /// <summary>s de canalizacao visivel ANTES da fusao (a janela de interromper). DIMINUIR = combo sem resposta.</summary>
            public const float CanalizacaoS = 1.0f;
            /// <summary>s de recarga dos DOIS, cobrada no INICIO. DIMINUIR = spam de combo; AUMENTAR = pilar que ninguem ve.</summary>
            public const float CooldownS = 24f;
            /// <summary>Dano do combo = (danoA + danoB) x Mult. Tem de ficar > 1 ("mais forte que a soma", GDD §9).</summary>
            public const float Mult = 2.35f;
            /// <summary>m entre os dois impactos (14 studs). AUMENTAR = dois alvos diferentes fundem; DIMINUIR = so' tiro colado.</summary>
            public const float RaioAlvoM = 7f;

            /// <summary>
            /// m de area de cada combo (PONTE A15; Elements.luau em studs x 0,5). E' o par (raio, duracao) que separa burst
            /// pontual de negacao de area — rebalanceia-se o combo por aqui, sem mexer no dano.
            /// </summary>
            public static float Raio(ComboSintonia c)
            {
                switch (c)
                {
                    case ComboSintonia.TornadoFlamejante: return 8f;      // 16 studs
                    case ComboSintonia.ChuvaDeMagma: return 9f;           // 18
                    case ComboSintonia.ExplosaoDePlasma: return 6f;       // 12 (a PONTE A15 diz 10; vale o codigo, Elements.luau)
                    case ComboSintonia.CortinaDeVapor: return 10f;        // 20
                    case ComboSintonia.Eletrocussao: return 11f;          // 22
                    case ComboSintonia.Lamacal: return 10f;               // 20
                    case ComboSintonia.TempestadeTorrencial: return 12f;  // 24
                    case ComboSintonia.TempestadeDeAreia: return 11f;     // 22
                    case ComboSintonia.CristaisCarregados: return 8f;     // 16 (o terreno usa metade: minas, nao redoma)
                    case ComboSintonia.NuvemTempestuosa: return 5f;       // 10
                }
                throw new ArgumentOutOfRangeException(nameof(c));
            }

            /// <summary>s do que PERSISTE (tornado, poca, lama, cristais, nuvem). Plasma 0,6 = o clarao do burst.</summary>
            public static float Duracao(ComboSintonia c)
            {
                switch (c)
                {
                    case ComboSintonia.TornadoFlamejante: return 6f;
                    case ComboSintonia.ChuvaDeMagma: return 8f;
                    case ComboSintonia.ExplosaoDePlasma: return 0.6f;
                    case ComboSintonia.CortinaDeVapor: return 9f;
                    case ComboSintonia.Eletrocussao: return 2f;
                    case ComboSintonia.Lamacal: return 12f;
                    case ComboSintonia.TempestadeTorrencial: return 6f;
                    case ComboSintonia.TempestadeDeAreia: return 8f;
                    case ComboSintonia.CristaisCarregados: return 15f;
                    case ComboSintonia.NuvemTempestuosa: return 6f;       // "persegue o alvo marcado por 6 s" (GDD §9)
                }
                throw new ArgumentOutOfRangeException(nameof(c));
            }
        }
    }
}
