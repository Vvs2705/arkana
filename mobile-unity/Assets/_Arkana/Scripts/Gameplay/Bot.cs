using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A DUPLA, pura (contrato 17G; GDD §9 "jogador solo/random": o solo e' pareado com um PARCEIRO BOT). Quem e' aliado
    /// (Combat.MesmoTime: um lugar so'), o alvo que vale, o aliado caido a socorrer, a faixa de 4–8 m de acompanhar e a luva
    /// de elemento DIFERENTE do parceiro (mesmo elemento nao funde: sem ela nao ha' Sintonia). A decisao e a casca so' leem.
    /// </summary>
    public static class Dupla
    {
        /// <summary>m: a faixa em que o parceiro acompanha fora de combate. Passou de MAX, volta; para de voltar em PARA (o
        /// meio da faixa: histerese, senao ele anda e para a cada passo na borda). KNOB por aparelho (game feel).</summary>
        public const float ACOMPANHA_MIN = 4f, ACOMPANHA_MAX = 8f, ACOMPANHA_PARA = (ACOMPANHA_MIN + ACOMPANHA_MAX) * 0.5f;
        /// <summary>m do caido em que o socorrista para: dentro do raio do Derrubado (ele canaliza por proximidade), com folga.</summary>
        public const float SOCORRO_M = Derrubado.RAIO_M * 0.6f;
        /// <summary>m que a luva de elemento diferente pode estar MAIS LONGE que a mais perto e ainda ganhar (o parceiro nao
        /// atravessa o mapa por ela). KNOB.</summary>
        public const float LUVA_DESVIO_M = 20f;
        /// <summary>m de lado entre quem salta junto (a dupla pousa colada, nao empilhada). KNOB.</summary>
        public const float AFASTAMENTO_M = 3f;
        /// <summary>O AZUL-ALIADO: marca do parceiro, ponto no minimapa, anel do aliado caido. Frio contra o vermelho do inimigo.</summary>
        public static readonly Color CorAliado = new Color(0.32f, 0.64f, 1f);

        /// <summary>A cor do DERRUBADO no mundo (VisualDoAbate): o AZUL-ALIADO para quem e' do time do jogador — menos ele mesmo
        /// (o proprio caido le' o perigo no vermelho, e o painel da HUD e' dele) —, `inimigo` para o resto.</summary>
        public static Color CorDoCaido(IEntidade jogador, IEntidade caido, Color inimigo) => Aliado(jogador, caido) ? CorAliado : inimigo;

        /// <summary>Do mesmo time e nao e' ele mesmo.</summary>
        public static bool Aliado(IEntidade a, IEntidade b) => a != null && b != null && a != b && Combat.MesmoTime(a, b);

        /// <summary>O que um bot pode mirar: vivo, outro, e de OUTRO time. Aliado nunca e' alvo.</summary>
        public static bool AlvoValido(IEntidade eu, IEntidade alvo) =>
            alvo != null && alvo != eu && alvo.Vital != null && alvo.Vital.Viva && !Combat.MesmoTime(eu, alvo);

        /// <summary>O aliado DERRUBADO mais perto (null = ninguem caido). O socorro manda mais que a presa.</summary>
        public static IEntidade AliadoCaido(IEntidade eu, IList<IEntidade> arena)
        {
            if (eu == null || arena == null) return null;
            IEntidade melhor = null;
            float d2 = float.PositiveInfinity;
            for (int i = 0; i < arena.Count; i++)
            {
                IEntidade e = arena[i];
                if (!Aliado(eu, e) || e.Vital == null || !e.Vital.Viva || !Derrubado.Esta(e)) continue;
                float dd = (e.Pos - eu.Pos).sqrMagnitude;
                if (dd < d2) { d2 = dd; melhor = e; }
            }
            return melhor;
        }

        /// <summary>Histerese do acompanhar: comeca a voltar alem de MAX, para de voltar ao chegar em PARA.</summary>
        public static bool Voltar(float dist, bool voltando) => dist > (voltando ? ACOMPANHA_PARA : ACOMPANHA_MAX);

        /// <summary>Os elementos que a luva de `e` porta (null = maos nuas ou sem corpo). Aloca: so' quem esta' desarmado pede.</summary>
        public static Elemento[] ElementosDe(IEntidade e)
        {
            var p = e as Pawn;
            return p != null && p.Slot != null && p.Slot.Armado ? p.Slot.Elementos(p.Elemento) : null;
        }

        /// <summary>A luva que o parceiro caca: a mais perto de elemento que o outro NAO tem, se ela nao estiver mais que
        /// LUVA_DESVIO_M alem da mais perto de todas; senao a mais perto (arma de qualquer cor ainda vale mais que maos nuas).</summary>
        public static LootItem LuvaPreferida(Vector3 pos, IList<LootItem> itens, Elemento[] evitar)
        {
            LootItem perto = DecisaoBot.LuvaMaisPerto(pos, itens);
            if (perto == null || evitar == null || evitar.Length == 0) return perto;
            LootItem outro = null;
            float d2 = float.PositiveInfinity;
            for (int i = 0; i < itens.Count; i++)
            {
                if (Compartilha(itens[i], evitar)) continue;
                float dd = (itens[i].Pos - pos).sqrMagnitude;
                if (dd < d2) { d2 = dd; outro = itens[i]; }
            }
            if (outro == null) return perto;
            return Mathf.Sqrt(d2) <= Vector3.Distance(pos, perto.Pos) + LUVA_DESVIO_M ? outro : perto;
        }

        static bool Compartilha(LootItem l, Elemento[] els)
        {
            for (int i = 0; i < els.Length; i++)
            {
                if (l.ElementoDaLuva.HasValue && l.ElementoDaLuva.Value == els[i]) return true;
                for (int j = 0; j < l.Par.Length; j++) if (l.Par[j] == els[i]) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// OS SENTIDOS DO BOT, puros (Bot.gd, ordem do Diretor 26/08). Nasce CEGO. Quatro canais, cada um com
    /// contra-jogada legivel: VISTO (&lt;12 m), OUVIDO (&lt;18 m so' quem se MOVE — ficar parado esconde),
    /// DISPARO (&lt;30 m, Bus.Disparo — conjurar denuncia), REVIDE (tomar dano ensina quem bateu, sem limite).
    /// Presa e' todo mago de OUTRO time (Combat.MesmoTime); no solo cada um e' o seu time, entao segue FFA.
    /// FOCO DA DUPLA: quem o `Parceiro` acerta vira o meu alvo (o mesmo alvo e' o que faz a Sintonia acontecer).
    /// Memoria curta: alvo morto ou alem de MEMORIA e' esquecido.
    /// So' procura alvo NOVO quando esta' sem nenhum (senao vira pinball entre passantes).
    /// OCULTO (invisivel, penumbra parada — o corpo decide): a visao so' o pega colado (VISAO_OCULTO, o brilho de perto) e os
    /// passos nao o localizam; o alvo que some e' largado e o bot vai ate' onde o viu. DISPARO e REVIDE continuam valendo.
    /// </summary>
    public sealed class PercepcaoBot
    {
        public const float VISAO = 12f, AUDICAO_PASSOS = 18f, AUDICAO_DISPARO = 30f, MEMORIA = 30f;
        /// <summary>m em que o oculto ainda se ve' (o brilho do invisivel, o vulto da penumbra colado) — KNOB.</summary>
        public const float VISAO_OCULTO = 2.5f;
        /// <summary>s entre varreduras — 12 bots a 2 Hz custa nada.</summary>
        public const float PERCEPCAO_S = 0.5f;
        /// <summary>m/s acima do qual os passos entregam.</summary>
        public const float PASSOS_V = 1f;

        public IEntidade Alvo { get; private set; }
        /// <summary>Para onde ir atras de um som (a decisao consome e zera).</summary>
        public Vector3? Pista { get; private set; }
        /// <summary>O aliado cujo ACERTO vira o meu alvo (o jogador, para o parceiro dele; o outro da dupla inimiga). Null = sozinho.</summary>
        public IEntidade Parceiro;

        private readonly IEntidade _eu;
        private float _acc;
        private bool _ligado;

        public PercepcaoBot(IEntidade eu) { _eu = eu; }

        /// <summary>Liga os ouvidos no Bus (e desliga com Desligar; Bus.Reset tambem os cala).</summary>
        public void Ligar()
        {
            if (_ligado) return;
            _ligado = true;
            Bus.Disparo += OuvirDisparo;
            Bus.DamageApplied += Revidar;
        }

        public void Desligar()
        {
            _ligado = false;
            Bus.Disparo -= OuvirDisparo;
            Bus.DamageApplied -= Revidar;
        }

        public void Esquecer() { Alvo = null; }
        public Vector3? ConsumirPista() { Vector3? p = Pista; Pista = null; return p; }

        public bool AlvoVivo => Alvo != null && Alvo.Vital != null && Alvo.Vital.Viva;

        /// <summary>Relogio: varre a cada PERCEPCAO_S. `velocidadeDe` = m/s horizontal de cada mago (passos); `ocultoDe` = a
        /// visao nao o pega (null = ninguem oculto).</summary>
        public void Tick(float dt, IList<IEntidade> magos, Func<IEntidade, float> velocidadeDe, Func<IEntidade, bool> ocultoDe = null)
        {
            _acc += dt;
            if (_acc < PERCEPCAO_S) return;
            _acc = 0f;
            Varrer(magos, velocidadeDe, ocultoDe);
        }

        public void Varrer(IList<IEntidade> magos, Func<IEntidade, float> velocidadeDe, Func<IEntidade, bool> ocultoDe = null)
        {
            if (Alvo != null && (!AlvoVivo || Dist(Alvo.Pos) > MEMORIA || !Dupla.AlvoValido(_eu, Alvo))) Alvo = null;
            // sumiu da vista: larga o alvo e vai ate' onde o viu por ultimo (disparo e revide o devolvem)
            if (Alvo != null && ocultoDe != null && ocultoDe(Alvo) && Dist(Alvo.Pos) >= VISAO_OCULTO) { Pista = Alvo.Pos; Alvo = null; }
            if (Alvo != null || magos == null) return;
            IEntidade melhor = null;
            float melhorD = float.PositiveInfinity;
            for (int i = 0; i < magos.Count; i++)
            {
                IEntidade p = magos[i];
                if (!Dupla.AlvoValido(_eu, p)) continue;   // morto, eu mesmo ou ALIADO: nunca e' presa
                float d = Dist(p.Pos);
                if (d >= melhorD) continue;
                bool oculto = ocultoDe != null && ocultoDe(p);
                bool visto = d < (oculto ? VISAO_OCULTO : VISAO);
                bool ouvido = !oculto && d < AUDICAO_PASSOS && velocidadeDe != null && velocidadeDe(p) > PASSOS_V;
                if (visto || ouvido) { melhor = p; melhorD = d; }
            }
            if (melhor != null) Alvo = melhor;
        }

        /// <summary>Conjurou perto = entregou a posicao. So' LARGA o alvo atual se o disparo veio de MAIS PERTO.</summary>
        public void OuvirDisparo(IEntidade quem, Vector3 pos)
        {
            if (quem == null || quem == _eu || !Vivo || Combat.MesmoTime(_eu, quem)) return;   // o disparo do aliado nao denuncia ninguem
            float d = Dist(pos);
            if (d >= AUDICAO_DISPARO) return;
            if (AlvoVivo && d >= Dist(Alvo.Pos)) return;
            Alvo = quem;
            Pista = pos;
        }

        /// <summary>Tomou dano = aprendeu quem bateu, mesmo fora de toda audicao. O PARCEIRO acertou alguem = o foco passa
        /// para esse alguem (o ultimo que ele acertou).</summary>
        public void Revidar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool escudo)
        {
            if (fonte != null && fonte == Parceiro && alvo != _eu) { Focar(alvo); return; }
            if (alvo != _eu || fonte == null || fonte == _eu || !Vivo || Combat.MesmoTime(_eu, fonte)) return;
            Alvo = fonte;
            Pista = fonte.Pos;
        }

        /// <summary>O alvo do parceiro vira o meu (se for presa: aliado e morto nao entram).</summary>
        public void Focar(IEntidade alvo)
        {
            if (!Vivo || !Dupla.AlvoValido(_eu, alvo)) return;
            Alvo = alvo;
            Pista = alvo.Pos;
        }

        /// <summary>O que o parceiro tem SOB A MIRA (antes do primeiro acerto): vale so' para quem esta' sem alvo.</summary>
        public void MiraDoParceiro(IEntidade sobAMira)
        {
            if (!AlvoVivo) Focar(sobAMira);
        }

        private bool Vivo => _eu != null && _eu.Vital != null && _eu.Vital.Viva;
        private float Dist(Vector3 p) => Vector3.Distance(_eu.Pos, p);
    }

    /// <summary>
    /// A DECISAO DO BOT, pura e determinista por seed: desarmado CACA a luva mais proxima (a Lei das Luvas vale
    /// para ele); armado persegue, encara e atira com erro; foge da zona; loot no caminho e' do auto-upgrade
    /// (Loot.TentarAutoUpgrade, na casca); salta do castelo num instante sorteado da rota.
    /// EM DUPLA (Escolta): a tempestade manda > o ALIADO CAIDO (vai ate' ele e fica perto: o Derrubado canaliza por
    /// proximidade) > a presa > ACOMPANHAR o parceiro a 4–8 m (so' quem Segue: o parceiro do jogador e o 2o de cada dupla
    /// inimiga) > vagar; desarmado caca a luva de elemento diferente do parceiro.
    /// </summary>
    public sealed class DecisaoBot
    {
        /// <summary>O que a dupla pede da decisao. O default e' o bot sozinho (o FFA de hoje, byte a byte).</summary>
        public struct Escolta
        {
            /// <summary>Acompanha o parceiro fora de combate (a 4–8 m).</summary>
            public bool Segue;
            public Vector3 Parceiro;
            /// <summary>Onde esta' o aliado derrubado a socorrer (null = ninguem caido).</summary>
            public Vector3? Socorrer;
            /// <summary>Elementos da luva do parceiro: desarmado, prefere a luva de OUTRO elemento (null = qualquer).</summary>
            public Elemento[] Evitar;
        }

        public const float CHASE_DIST = 20f, ATTACK_DIST = 12f;
        /// <summary>rad — KNOB: erro de mira do bot.</summary>
        public const float AIM_SPREAD = 0.12f;
        /// <summary>KNOB: cadencia do bot vs player.</summary>
        public const float FIRE_RATE_MULT = 4f;
        public const float DODGE_CHANCE = 0.3f, DODGE_ROLL_S = 1.4f;
        /// <summary>Fracao da rota do castelo em que o bot pode saltar (nem na ponta de entrada, nem em fila no fim).</summary>
        public const float SALTO_MIN = 0.18f, SALTO_MAX = 0.86f;
        public const float CHEGOU_M = 1.5f;

        public struct Saida
        {
            public Vector3 Dir;          // intencao de movimento (0..1)
            public Vector3 Encarar;      // zero = encara o movimento
            public bool Atirar;
            public Vector3 DirTiro;
            public bool Esquivar;
            public Vector3 DirEsquiva;
        }

        /// <summary>Instante (fracao da rota) em que salta do castelo.</summary>
        public readonly float SaltoEm;
        public Vector3 Destino { get; private set; }

        private readonly System.Random _rng;
        private float _repick, _dodgeRoll;
        private bool _voltando;

        public DecisaoBot(int seed)
        {
            _rng = new System.Random(seed);
            SaltoEm = Faixa(SALTO_MIN, SALTO_MAX);
        }

        /// <summary>Destino imposto de fora (zona, bau, som): sobrepoe o vagar e segura o repique.</summary>
        public void IrPara(Vector3 pos) { Destino = pos; _repick = 4f; }

        /// <summary>A luva mais proxima no chao. Sem teto: um bot desarmado atravessa o mapa por uma arma.</summary>
        public static LootItem LuvaMaisPerto(Vector3 pos, IList<LootItem> itens)
        {
            LootItem melhor = null;
            float d2 = float.PositiveInfinity;
            if (itens == null) return null;
            for (int i = 0; i < itens.Count; i++)
            {
                float dd = (itens[i].Pos - pos).sqrMagnitude;
                if (dd < d2) { d2 = dd; melhor = itens[i]; }
            }
            return melhor;
        }

        public Saida Decidir(float dt, Vector3 pos, bool armado, IEntidade alvo, Vector3? pista, IList<LootItem> loot, Zona zona, Vector3? bau, bool podeAtirar,
            Escolta escolta = default(Escolta))
        {
            _repick -= dt;
            _dodgeRoll -= dt;
            var s = new Saida();
            if (pista.HasValue) IrPara(pista.Value);
            bool vivo = alvo != null && alvo.Vital != null && alvo.Vital.Viva;
            float dist = vivo ? Vector3.Distance(pos, alvo.Pos) : float.PositiveInfinity;
            bool foraDaZona = zona != null && zona.DevePuxar(pos);
            if (foraDaZona) IrPara(zona.Centro);   // a tempestade manda mais que a presa
            else if (bau.HasValue && Vector3.Distance(pos, bau.Value) < BauCelestial.ATRAIR_BOT_M) IrPara(bau.Value);   // o farol atrai
            // SOCORRO: o aliado caido manda mais que a presa (a tempestade ainda manda mais que ele)
            bool socorro = !foraDaZona && escolta.Socorrer.HasValue;
            if (socorro) IrPara(escolta.Socorrer.Value);

            if (armado && vivo && dist < ATTACK_DIST)
            {
                s.Encarar = Plano(alvo.Pos - pos);
                if (podeAtirar)
                {
                    s.Atirar = true;
                    Vector3 de = pos + Vector3.up * Pawn.ALTURA_MAO, para = alvo.Pos + Vector3.up * 1.2f;
                    s.DirTiro = GirarY((para - de).normalized, Faixa(-AIM_SPREAD, AIM_SPREAD));
                }
                if (foraDaZona || socorro) s.Dir = Rumo(pos, Destino, socorro ? Dupla.SOCORRO_M : 0f);   // atira andando para o caido
            }
            else if (armado && vivo && dist < CHASE_DIST && !foraDaZona && !socorro)
            {
                s.Dir = Plano(alvo.Pos - pos);
            }
            else if (socorro)
            {
                s.Dir = Rumo(pos, Destino, Dupla.SOCORRO_M);   // chega e FICA: o Derrubado canaliza enquanto ele estiver perto
            }
            else if (armado && escolta.Segue && !foraDaZona)
            {
                Vector3 d = escolta.Parceiro - pos; d.y = 0f;
                _voltando = Dupla.Voltar(d.magnitude, _voltando);
                s.Dir = _voltando ? Plano(d) : Vector3.zero;   // dentro da faixa, fica: quem manda o passo e' o parceiro
            }
            else
            {
                if (_repick <= 0f || Vector3.Distance(pos, Destino) < CHEGOU_M)
                {
                    _repick = Faixa(2f, 5f);
                    Destino = pos + new Vector3(Faixa(-12f, 12f), 0f, Faixa(-12f, 12f));
                    if (!armado)
                    {
                        LootItem l = Dupla.LuvaPreferida(pos, loot, escolta.Evitar);   // DESARMADO, a prioridade e' ACHAR LUVA
                        if (l != null) { Destino = l.Pos; _repick = 6f; }
                    }
                }
                s.Dir = Plano(Destino - pos);
            }
            if (armado && vivo && dist < CHASE_DIST && _dodgeRoll <= 0f)
            {
                _dodgeRoll = DODGE_ROLL_S;
                if (_rng.NextDouble() < DODGE_CHANCE)
                {
                    s.Esquivar = true;
                    s.DirEsquiva = Vector3.Cross(Plano(alvo.Pos - pos), Vector3.up) * (_rng.NextDouble() < 0.5 ? 1f : -1f);
                }
            }
            return s;
        }

        private float Faixa(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        /// <summary>Gira em torno de Y por `rad` (trig pura: a decisao nao encosta em Quaternion nativo).</summary>
        private static Vector3 GirarY(Vector3 v, float rad)
        {
            float c = Mathf.Cos(rad), s = Mathf.Sin(rad);
            return new Vector3(v.x * c + v.z * s, v.y, -v.x * s + v.z * c);
        }

        private static Vector3 Plano(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude < 0.0001f ? Vector3.zero : v.normalized;
        }

        /// <summary>Rumo no plano ate' `destino`; a menos de `para` m, fica (zero).</summary>
        public static Vector3 Rumo(Vector3 pos, Vector3 destino, float para)
        {
            Vector3 d = destino - pos; d.y = 0f;
            return d.sqrMagnitude <= para * para ? Vector3.zero : Plano(d);
        }
    }

    /// <summary>Casca: percepcao + decisao sobre um Pawn. Nome = Kits.De(slug).Nome; tint so' em slug sem ficha.
    /// Em DUPLA (`Parear`): quem Segue salta do castelo JUNTO do parceiro e o persegue no ar (pousa ao lado, afastado
    /// AFASTAMENTO_M); no chao a Escolta vai para a decisao.</summary>
    public sealed class Bot : MonoBehaviour
    {
        public static readonly Color[] TINTS =
        {
            new Color(0.9f, 0.3f, 0.3f), new Color(0.3f, 0.7f, 0.9f), new Color(0.4f, 0.85f, 0.4f),
            new Color(0.95f, 0.8f, 0.3f), new Color(0.75f, 0.4f, 0.9f), new Color(0.95f, 0.55f, 0.25f),
        };

        public Pawn Pawn { get; private set; }
        public PercepcaoBot Percepcao { get; private set; }
        public DecisaoBot Decisao { get; private set; }
        /// <summary>O outro da dupla (null = sozinho, o solo de hoje). Para o parceiro do jogador, o Pawn do jogador.</summary>
        public IEntidade Parceiro { get; private set; }
        /// <summary>Acompanha o parceiro (salta junto, persegue no ar, fica a 4–8 m). O lider da dupla inimiga nao segue.</summary>
        public bool Segue { get; private set; }
        private Castelo _castelo;
        private float _lado = 1f;

        public static Bot Criar(Transform pai, string slug, int seed)
        {
            Pawn pawn = Pawn.Criar(pai, slug, false);
            pawn.CadenciaMult = DecisaoBot.FIRE_RATE_MULT;
            var b = pawn.gameObject.AddComponent<Bot>();
            b.Pawn = pawn;
            b.Percepcao = new PercepcaoBot(pawn);
            b.Decisao = new DecisaoBot(seed);
            b.Percepcao.Ligar();
            Kits.KitDef kit = Kits.De(slug);
            if (!Kits.Magos.ContainsKey(kit.Slug)) pawn.SetTint(TINTS[((seed % TINTS.Length) + TINTS.Length) % TINTS.Length]);
            return b;
        }

        /// <summary>Forma a dupla deste lado: `parceiro` e' de quem ele adota o alvo e a quem socorre; `segue` = acompanha.
        /// `lado` (+1/-1) = de que lado do parceiro pousa.</summary>
        public void Parear(IEntidade parceiro, bool segue, float lado = 1f)
        {
            Parceiro = parceiro;
            Segue = segue;
            _lado = lado;
            Percepcao.Parceiro = parceiro;
        }

        public void Embarcar(Castelo castelo) { _castelo = castelo; Pawn.Embarcar(castelo); }

        void OnEnable() { if (Percepcao != null) Percepcao.Ligar(); }
        void OnDisable() { if (Percepcao != null) Percepcao.Desligar(); }

        void Update()
        {
            if (Pawn == null || !Pawn.Viva) return;
            Pawn guia = Segue ? Parceiro as Pawn : null;
            if (Pawn.Queda.NoAr)
            {
                // salta pelo relogio da rota (nao tem dedo); sem castelo, cai de onde esta'. Quem SEGUE salta quando o parceiro salta.
                if (Pawn.Queda.Fase == Queda.NO_CASTELO)
                {
                    bool hora = guia != null ? guia.Queda.Fase != Queda.NO_CASTELO : _castelo != null && _castelo.Progresso >= Decisao.SaltoEm;
                    if (_castelo == null || hora) Pawn.Saltar();
                }
                else if (guia != null) NoArJunto(guia);
                return;
            }
            Partida m = Partida.Atual;
            IList<IEntidade> arena = m != null ? m.Arena : null;
            Percepcao.Tick(Time.deltaTime, arena, VelocidadeDe, OcultoDe);
            IList<LootItem> loot = m != null && m.Loot != null ? m.Loot.Itens : null;
            if (m != null && m.Loot != null) m.Loot.TentarAutoUpgrade(Pawn.Slot);   // loot no caminho: so' tier maior
            Vector3? bau = m != null && m.Bau != null && m.Bau.PodeAbrir ? m.Bau.Pos : (Vector3?)null;
            DecisaoBot.Saida s = Decisao.Decidir(Time.deltaTime, Pawn.Pos, Pawn.Slot.Armado, Percepcao.Alvo, Percepcao.ConsumirPista(),
                loot, m != null ? m.Zona : null, bau, Pawn.PodeAgir, Escolta(arena));
            Pawn.YawCam = 0f;
            Pawn.Stick = new Vector2(s.Dir.x, s.Dir.z);
            Pawn.EncararDir = s.Encarar;
            if (s.Esquivar) Pawn.Dodge(s.DirEsquiva);
            if (s.Atirar) Pawn.Atirar(s.DirTiro);
        }

        /// <summary>O que a dupla pede neste quadro. Sozinho: o default (a decisao de hoje).</summary>
        private DecisaoBot.Escolta Escolta(IList<IEntidade> arena)
        {
            var e = new DecisaoBot.Escolta();
            if (Parceiro == null) return e;
            bool deP = Parceiro.Vital != null && Parceiro.Vital.Viva && !Derrubado.Esta(Parceiro);
            e.Segue = Segue && deP;   // o parceiro caido e' socorro, nao escolta
            e.Parceiro = Parceiro.Pos;
            IEntidade caido = Dupla.AliadoCaido(Pawn, arena);
            if (caido != null) e.Socorrer = caido.Pos;
            if (!Pawn.Slot.Armado) e.Evitar = Dupla.ElementosDe(Parceiro);   // so' desarmado caca luva
            return e;
        }

        /// <summary>No ar atras de quem segue: mira o ponto AO LADO dele e abre/fecha o planeio junto (quem cai sem planar
        /// chega antes e pousa longe).</summary>
        private void NoArJunto(Pawn guia)
        {
            Vector3 alvo = guia.Pos + new Vector3(_lado * Dupla.AFASTAMENTO_M, 0f, 0f);
            Vector3 d = DecisaoBot.Rumo(Pawn.Pos, alvo, 1f);
            Pawn.YawCam = 0f;
            Pawn.Stick = new Vector2(d.x, d.z);
            if (guia.Queda.Planando != Pawn.Queda.Planando) Pawn.Planar();
        }

        /// <summary>O que os passos entregam: nada de quem anda sem som (Passo de Veludo).</summary>
        private static float VelocidadeDe(IEntidade e)
        {
            var p = e as Pawn;
            return p != null && !p.SemPassos ? p.VelocidadeHorizontal : 0f;
        }

        private static bool OcultoDe(IEntidade e)
        {
            var p = e as Pawn;
            return p != null && p.Oculto;
        }
    }
}
