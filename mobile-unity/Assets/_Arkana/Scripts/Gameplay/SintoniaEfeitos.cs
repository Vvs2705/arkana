using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O QUE CADA UM DOS 10 COMBOS FAZ NO MUNDO (GDD §9 + §14; PONTE A4/A15). QUANDO fundir e' da Core.Sintonia; aqui so'
    /// o efeito, instalado no gancho `Sintonia.Efeito` (o molde do Derrubado.Instalar). Tres passos, NESTA ORDEM:
    /// (1) RECEITA DE TERRENO pelo Bus.TerrainHit — o terreno pega TODO MUNDO (A4). A ordem e as ausencias sao as do
    ///     Roblox (Sintonia.luau `EFFECTS`): Eletrocussao nao molha (congelaria a poca e mataria a conducao), o Tornado
    ///     nao venta (apagaria a chama recem-nascida), o Vapor so' molha (vapor e' fogo APAGADO, nao fogo somado).
    /// (2) GOLPE: `d.Dano` com fonte A e status SO' em inimigo do time de A (A4: magia mirada nao pega parceiro).
    ///     Atordoar so' pelo Efeitos.Atordoar — o teto de 0,8 s mora la' e daqui nao se fura.
    /// (3) O QUE FICA, numa lista com Tick. Regra: o que ANDA ou MIRA (tornado, nuvem, minas) e' corpo do combo e so'
    ///     pega inimigo; o que vira CHAO (magma, lama, vapor, areia) pega todo mundo, como o §14 — a dupla tambem se
    ///     queima pelo mapa. COUNTER elemental (GDD §9): o elemento certo caindo dentro da zona (tiro, kit ou outro
    ///     combo — todos publicam TerrainHit) a desfaz ("Torrencial apaga Magma"); a mina quebra com qualquer golpe.
    /// Os numeros de area e duracao sao Balance.Sintonia.Raio/Duracao (PONTE A15, NAO VALIDADOS). Os KNOBS locais abaixo
    /// tambem sao hipotese: moram aqui ate' a Balance.Sintonia (dono 17B) recebe-los — o Diretor julga no aparelho.
    /// </summary>
    public static class SintoniaEfeitos
    {
        // ------------------------------------------------------------------ KNOBS locais (mover para Balance.Sintonia)
        /// <summary>m/s: o tornado anda DEVAGAR — correr dele e' o contra-jogo (o player corre ~6 m/s).</summary>
        public const float TornadoVel = 2.5f;
        /// <summary>m de puxao por pulso (4 Hz) em quem esta' no raio: ~2 m/s de sucao, menos que correr.</summary>
        public const float PuxaoM = 0.3f;
        /// <summary>Fracao do raio que e' o FUNIL: ali dentro o inimigo pega fogo (Efeitos.Acender).</summary>
        public const float NucleoFracao = 0.35f;
        /// <summary>m/s da nuvem: mais rapida que correr — "persegue" (GDD §9). A esquiva desvia a descarga.</summary>
        public const float NuvemVel = 7f;
        /// <summary>s entre descargas e m (horizontal) em que a nuvem alcanca o alvo; fracao de d.Dano por descarga.</summary>
        public const float NuvemPulsoS = 1f, NuvemAlcanceM = 2.5f, NuvemFracao = 0.1f;
        /// <summary>O tornado e a nuvem sem alvo procuram o inimigo mais perto neste multiplo do raio.</summary>
        public const float BuscaFracao = 2f;
        /// <summary>Minas por combo, no anel desta fracao do raio; m de gatilho (pisar/golpe) e de estouro.</summary>
        public const int Cristais = 5;
        public const float CristalAnel = 0.55f, CristalToqueM = 1.5f, CristalEstouroM = 3f;
        /// <summary>s de atordoamento (Roblox: janela 0,6–0,8 da auditoria). Pedir mais e' inocuo: o Efeitos grampeia.</summary>
        public const float StunEletro = 0.7f, StunCristal = 0.6f;
        /// <summary>Vapor: "passo travado" na nuvem escaldante (Roblox: 2 s a 0,55).</summary>
        public const float VaporLento = 0.55f, VaporLentoS = 2f;
        /// <summary>m de faixa vertical das areas de chao (quem esta' no castelo, 400 m acima, nao cai no combo).</summary>
        public const float FaixaM = 4f;

        /// <summary>O que um combo deixou no mundo. Publico para a casca desenhar (VisualDaSintonia le' `Ativos`).</summary>
        public sealed class Persistente
        {
            public ComboSintonia Combo;
            public Vector3 Pos;
            public float Raio, Duracao, Restante;
            public IEntidade A, Alvo;
            /// <summary>Nuvem: s desde a ultima descarga (a casca acende o raio logo depois dela; 0 = a do estouro).</summary>
            public float DesdeADescarga;
            /// <summary>Cristal: QUEBROU (a casca estilhaca) ou so' expirou (apaga).</summary>
            public bool Quebrou;
            internal Vector3 Rumo;
            internal float Dano, Acc;
        }

        private static readonly List<Persistente> _ativos = new List<Persistente>();
        public static IReadOnlyList<Persistente> Ativos => _ativos;

        /// <summary>Casca do empurrao: `v` = direcao x METROS pelo canal de knockback do corpo. ponytail: o teste troca (fake nao
        /// e' Pawn), como o Gromm.EmpurrarCorpo.</summary>
        public static Action<IEntidade, Vector3> Empurrar = (e, v) => ApoioGrupoD.Empurrar(e, v, v.magnitude);

        /// <summary>Liga o gancho da Core.Sintonia e o ouvido dos counters. Idempotente.</summary>
        public static void Instalar()
        {
            Sintonia.Efeito = Disparar;
            Bus.TerrainHit -= AoTerreno;
            Bus.TerrainHit += AoTerreno;
        }

        /// <summary>Apaga tudo o que ficou e solta o ouvido do Bus (inicio e fim de partida).</summary>
        public static void Reset()
        {
            _ativos.Clear();
            Bus.TerrainHit -= AoTerreno;
        }

        /// <summary>A FUSAO: receita de terreno, golpe no inimigo, e o que fica.</summary>
        public static void Disparar(DisparoSintonia d)
        {
            ComboSintonia c = d.Combo;
            float raio = Balance.Sintonia.Raio(c);
            Receita(c, d.Ponto, raio);   // ANTES de nascer a mina: a propria receita nao a quebra
            IList<IEntidade> arena = Arena();
            if (arena != null)
                for (int i = 0; i < arena.Count; i++)
                {
                    IEntidade e = arena[i];
                    if (!Inimigo(d.A, e) || !Dentro(d.Ponto, raio, e.Pos)) continue;
                    if (Efeitos.De(e).IframesLeft > 0f) continue;   // a esquiva e' imunidade TOTAL (a mesma lei do projetil)
                    // ponytail: o dano sai no elemento de A (o 1o impacto) — o escudo morde pelo perfil dele. Vetavel.
                    Combat.AplicarDano(e, d.Dano, d.ElA, d.A);
                    Golpe(c, e, d.Ponto);
                }
            Nascer(d, raio);
        }

        /// <summary>Relogio do que ficou (a Partida chama 1x por frame, junto do Combat.TickDot).</summary>
        public static void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            IList<IEntidade> arena = Arena();
            for (int i = _ativos.Count - 1; i >= 0; i--)
            {
                if (i >= _ativos.Count) continue;   // um ouvinte (fim de partida -> Reset) limpou a lista no meio
                Persistente p = _ativos[i];
                if (p.Restante > 0f)
                {
                    p.Restante -= dt;
                    Viver(p, dt, arena);
                }
                if (!(p.Restante > 0f) && i < _ativos.Count && _ativos[i] == p) _ativos.RemoveAt(i);
            }
        }

        /// <summary>Os dois elementos do combo (a casca pinta com eles). A ordem e' a do nome; o par e' comutativo.</summary>
        public static void Par(ComboSintonia c, out Elemento a, out Elemento b)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante: a = Elemento.Fogo; b = Elemento.Vento; return;
                case ComboSintonia.ChuvaDeMagma: a = Elemento.Fogo; b = Elemento.Terra; return;
                case ComboSintonia.ExplosaoDePlasma: a = Elemento.Fogo; b = Elemento.Raio; return;
                case ComboSintonia.CortinaDeVapor: a = Elemento.Fogo; b = Elemento.Agua; return;
                case ComboSintonia.Eletrocussao: a = Elemento.Agua; b = Elemento.Raio; return;
                case ComboSintonia.Lamacal: a = Elemento.Agua; b = Elemento.Terra; return;
                case ComboSintonia.TempestadeTorrencial: a = Elemento.Agua; b = Elemento.Vento; return;
                case ComboSintonia.TempestadeDeAreia: a = Elemento.Terra; b = Elemento.Vento; return;
                case ComboSintonia.CristaisCarregados: a = Elemento.Terra; b = Elemento.Raio; return;
                default: a = Elemento.Vento; b = Elemento.Raio; return;
            }
        }

        /// <summary>O elemento que DESFAZ a zona (GDD §9/§14). Null = nao e' zona (instantaneo) ou e' mina (qualquer golpe).</summary>
        public static Elemento? Counter(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante:
                case ComboSintonia.ChuvaDeMagma:
                case ComboSintonia.TempestadeDeAreia: return Elemento.Agua;   // agua apaga a chama e assenta a poeira
                case ComboSintonia.CortinaDeVapor: return Elemento.Vento;     // §14: vento dissipa nevoa/gas
                case ComboSintonia.Lamacal: return Elemento.Fogo;             // §14: fogo seca a lama
                case ComboSintonia.NuvemTempestuosa: return Elemento.Terra;   // §14: terra isola/aterra o raio
            }
            return null;
        }

        // ------------------------------------------------------------------ (1) receita de terreno

        /// <summary>A metade MUNDO, pelo mesmo Bus.TerrainHit de todo impacto (terreno, estruturas de kit, counters).</summary>
        private static void Receita(ComboSintonia c, Vector3 centro, float raio)
        {
            switch (c)
            {
                case ComboSintonia.TornadoFlamejante:
                case ComboSintonia.ChuvaDeMagma:
                    Bus.EmitTerrainHit(Elemento.Fogo, centro, true);
                    break;
                case ComboSintonia.ExplosaoDePlasma:
                    Espalhar(Elemento.Raio, centro, raio);
                    Bus.EmitTerrainHit(Elemento.Fogo, centro, true);
                    break;
                case ComboSintonia.CortinaDeVapor:
                case ComboSintonia.Lamacal:
                case ComboSintonia.TempestadeTorrencial:
                    Espalhar(Elemento.Agua, centro, raio);
                    break;
                case ComboSintonia.Eletrocussao:
                case ComboSintonia.NuvemTempestuosa:
                    Espalhar(Elemento.Raio, centro, raio);
                    break;
                case ComboSintonia.TempestadeDeAreia:
                    Espalhar(Elemento.Vento, centro, raio);
                    break;
                case ComboSintonia.CristaisCarregados:
                    // cacho de muro no MEIO raio (o GDD pede MINAS, nao redoma) + a carga eletrica
                    Bus.EmitTerrainHit(Elemento.Terra, centro, true);
                    Espalhar(Elemento.Raio, centro, raio * 0.5f);
                    break;
            }
        }

        /// <summary>
        /// O elemento na AREA: centro + aneis de amostras a cada 2 celulas (o respingo de 1,5 celula de cada uma cobre o disco).
        /// O FOGO nao passa por aqui: cada impacto de fogo abre uma FRENTE com orcamento proprio, e N amostras seriam N
        /// orcamentos — a carbonizacao que o §14 proibe. ponytail: o fogo do combo e' UMA ignicao forte (3x3) no centro;
        /// o disco inteiro aceso pede um Reagir com raio no TerrenoReativo (arquivo sem dono) — subir se o Diretor achar pouco.
        /// </summary>
        private static void Espalhar(Elemento el, Vector3 centro, float r)
        {
            Bus.EmitTerrainHit(el, centro, false);
            float passo = Balance.Terrain.CellSize * 2f;
            for (float anel = passo; anel <= r + 0.01f; anel += passo)
            {
                int n = Mathf.CeilToInt(2f * Mathf.PI * anel / passo);
                for (int k = 0; k < n; k++)
                {
                    float a = k * 2f * Mathf.PI / n;
                    Bus.EmitTerrainHit(el, centro + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * anel, false);
                }
            }
        }

        // ------------------------------------------------------------------ (2) golpe

        /// <summary>Status e empurrao do estouro (so' inimigo). Lamacal NAO lentifica aqui: a lama e' a zona (nao dobrar).</summary>
        private static void Golpe(ComboSintonia c, IEntidade e, Vector3 ponto)
        {
            if (c == ComboSintonia.Eletrocussao) Efeitos.Atordoar(e, StunEletro);
            else if (c == ComboSintonia.CortinaDeVapor) Efeitos.Lentificar(e, VaporLento, VaporLentoS);
            float m = EmpurraoM(c);
            Vector3 fora = Plano(e.Pos - ponto);
            if (m > 0f && fora.sqrMagnitude > 1e-4f) Empurrar(e, fora.normalized * m);
        }

        /// <summary>m do empurrao do estouro: o do Roblox (studs/s) / 14 — o knockback daqui decai a 9 m/s2. KNOB.</summary>
        private static float EmpurraoM(ComboSintonia c)
        {
            switch (c)
            {
                case ComboSintonia.TempestadeTorrencial: return 3.9f;   // 55: o combo que EMPURRA
                case ComboSintonia.ExplosaoDePlasma: return 2.9f;       // 40
                case ComboSintonia.TempestadeDeAreia: return 2.1f;      // 30
                case ComboSintonia.NuvemTempestuosa: return 1.8f;       // 25
            }
            return 0f;   // o tornado SUGA (no Tick), nao empurra
        }

        // ------------------------------------------------------------------ (3) o que fica

        private static void Nascer(DisparoSintonia d, float raio)
        {
            ComboSintonia c = d.Combo;
            float dur = Balance.Sintonia.Duracao(c);
            if (!(dur > 0f)) return;
            switch (c)
            {
                case ComboSintonia.ExplosaoDePlasma:
                case ComboSintonia.Eletrocussao:
                case ComboSintonia.TempestadeTorrencial:
                    return;   // golpes: a duracao deles e' so' do visual (o eletrico que fica e' o do TERRENO)
                case ComboSintonia.CristaisCarregados:
                    for (int k = 0; k < Cristais; k++)
                    {
                        float a = (k * 360f / Cristais + 18f) * Mathf.Deg2Rad;
                        Vector3 p = d.Ponto + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (raio * CristalAnel);
                        p.y = Ilha.AlturaDoChao(p.x, p.z);
                        Novo(d, p, CristalToqueM, dur);
                    }
                    return;
            }
            Persistente n = Novo(d, d.Ponto, raio, dur);
            if (d.A != null) n.Rumo = Plano(d.Ponto - d.A.Pos);
            n.Rumo = n.Rumo.sqrMagnitude > 1e-4f ? n.Rumo.normalized : Vector3.forward;
        }

        private static Persistente Novo(DisparoSintonia d, Vector3 pos, float raio, float dur)
        {
            var p = new Persistente
            {
                Combo = d.Combo, Pos = pos, Raio = raio, Duracao = dur, Restante = dur, A = d.A,
                Alvo = Inimigo(d.A, d.Alvo) ? d.Alvo : null, Dano = d.Dano,
            };
            _ativos.Add(p);
            return p;
        }

        private static void Viver(Persistente p, float dt, IList<IEntidade> arena)
        {
            if (p.Combo == ComboSintonia.TornadoFlamejante) Andar(p, TornadoVel, dt, arena);
            else if (p.Combo == ComboSintonia.NuvemTempestuosa)
            {
                Andar(p, NuvemVel, dt, arena);
                p.DesdeADescarga += dt;
                if (p.DesdeADescarga >= NuvemPulsoS) Descarregar(p);
            }
            // pulso no balde do DoT (4 Hz): nunca dano por frame
            p.Acc += dt;
            if (p.Acc < Balance.Dot.Tick || arena == null) return;
            float janela = p.Acc;
            p.Acc = 0f;
            for (int i = 0; i < arena.Count && p.Restante > 0f; i++)
            {
                IEntidade e = arena[i];
                if (!Vivo(e) || !Dentro(p.Pos, p.Raio, e.Pos)) continue;
                switch (p.Combo)
                {
                    case ComboSintonia.TornadoFlamejante: if (Inimigo(p.A, e)) Sugar(p, e); break;
                    case ComboSintonia.ChuvaDeMagma: Combat.AplicarDot(e, Balance.Terrain.BurnDps, janela, "burn", null); break;   // lava = chao em chamas
                    case ComboSintonia.CortinaDeVapor: Combat.AplicarDot(e, Balance.Status.BurnDps, janela, "burn", null); break;  // escalda leve
                    case ComboSintonia.TempestadeDeAreia: Combat.AplicarDot(e, Balance.Status.BurnDps, janela, "earth", null); break;
                    case ComboSintonia.Lamacal: Atolar(e); break;
                    case ComboSintonia.CristaisCarregados: if (Inimigo(p.A, e)) Quebrar(p, arena); break;   // PISOU
                }
            }
            // ponytail: "cegar" (vapor, areia) e' so' o VISUAL (a cupula vista de dentro); o bot nao cega — a visao dele e'
            // da PercepcaoBot (17D). Subir: a percepcao pergunta se o bot esta' Dentro de vapor/areia e corta a VISAO.
        }

        /// <summary>Tornado e nuvem: rumo ao alvo vivo (o do disparo, ou o inimigo mais perto); o tornado sem ninguem segue o
        /// rumo do tiro. Andam no chao (a nuvem na altura do alvo; a casca a poe no ceu).</summary>
        private static void Andar(Persistente p, float vel, float dt, IList<IEntidade> arena)
        {
            if (!Vivo(p.Alvo)) p.Alvo = MaisPerto(arena, p.A, p.Pos, p.Raio * BuscaFracao);
            Vector3 passo;
            if (p.Alvo != null)
            {
                Vector3 falta = Plano(p.Alvo.Pos - p.Pos);
                passo = falta.magnitude <= vel * dt ? falta : falta.normalized * (vel * dt);
            }
            else if (p.Combo == ComboSintonia.TornadoFlamejante) passo = p.Rumo * (vel * dt);
            else return;
            p.Pos += passo;
            p.Pos.y = p.Alvo != null && p.Combo == ComboSintonia.NuvemTempestuosa ? p.Alvo.Pos.y : Ilha.AlturaDoChao(p.Pos.x, p.Pos.z);
        }

        /// <summary>Puxao para o funil (pelo canal de knockback) e, no funil, fogo no corpo.</summary>
        private static void Sugar(Persistente p, IEntidade e)
        {
            Vector3 d = Plano(p.Pos - e.Pos);
            float m = d.magnitude;
            if (m > 0.05f) Empurrar(e, d * (Mathf.Min(PuxaoM, m) / m));
            if (m <= p.Raio * NucleoFracao) Efeitos.Acender(e, Balance.Status.BurnDps, Balance.Status.BurnDur);
        }

        /// <summary>Lama: lentidao de STATUS so' onde o TERRENO nao virou lama (a celula de lama ja' cobra pelo fator de
        /// terreno — dobrar deixaria a poca duas vezes pior, a regra do Roblox). Nao rouba uma lentidao mais forte.</summary>
        private static void Atolar(IEntidade e)
        {
            TerrenoReativo t = Terreno();
            if (t != null && t.FatorTerreno(e.Pos) < 1f) return;
            Efeitos.EstadoAlvo s = Efeitos.De(e);
            if (s.StatusMult < Balance.Terrain.MudSlow) return;
            // segura 2 pulsos: quem sai da lama volta a correr em meio segundo
            Efeitos.Lentificar(e, Balance.Terrain.MudSlow, Mathf.Max(s.SlowLeft, Balance.Dot.Tick * 2f));
        }

        /// <summary>A nuvem cai no alvo que ela alcanca (a esquiva desvia). A descarga e' Raio no chao: conduz na agua.</summary>
        private static void Descarregar(Persistente p)
        {
            IEntidade a = p.Alvo;
            if (!Vivo(a) || Plano(a.Pos - p.Pos).sqrMagnitude > NuvemAlcanceM * NuvemAlcanceM || Efeitos.De(a).IframesLeft > 0f) return;
            p.DesdeADescarga = 0f;
            Combat.AplicarDano(a, p.Dano * NuvemFracao, Elemento.Raio, p.A);
            Bus.EmitTerrainHit(Elemento.Raio, a.Pos, false);
        }

        /// <summary>A MINA estoura: atordoa os inimigos perto (o teto e' do Efeitos). Uma vez so'.</summary>
        private static void Quebrar(Persistente p, IList<IEntidade> arena)
        {
            if (p.Quebrou) return;
            p.Quebrou = true;
            p.Restante = 0f;
            if (arena == null) return;
            for (int i = 0; i < arena.Count; i++)
            {
                IEntidade e = arena[i];
                if (Inimigo(p.A, e) && Dentro(p.Pos, CristalEstouroM, e.Pos)) Efeitos.Atordoar(e, StunCristal);
            }
        }

        /// <summary>COUNTER: todo impacto publica TerrainHit. O elemento certo desfaz a zona; qualquer golpe quebra a mina.</summary>
        private static void AoTerreno(Elemento el, Vector3 pos, bool forte)
        {
            for (int i = 0; i < _ativos.Count; i++)
            {
                Persistente p = _ativos[i];
                if (!(p.Restante > 0f) || !Dentro(p.Pos, p.Raio, pos)) continue;
                if (p.Combo == ComboSintonia.CristaisCarregados) Quebrar(p, Arena());
                else if (Counter(p.Combo) == el) p.Restante = 0f;
            }
        }

        // ------------------------------------------------------------------ utilidades

        private static IList<IEntidade> Arena() => Partida.Atual != null ? Partida.Atual.Arena : null;

        private static TerrenoReativo Terreno()
        {
            TerrenoReativo t = Partida.Atual != null ? Partida.Atual.Terreno : null;
            if (t == null && TerrenoReativoBehaviour.Atual != null) t = TerrenoReativoBehaviour.Atual.Terreno;
            return t;
        }

        private static bool Vivo(IEntidade e) => e != null && e.Vital != null && e.Vital.Viva;

        /// <summary>Vivo e de OUTRO time (Combat.MesmoTime: o ponto unico de "e' aliado?").</summary>
        private static bool Inimigo(IEntidade a, IEntidade e) => Vivo(e) && !Combat.MesmoTime(a, e);

        private static IEntidade MaisPerto(IList<IEntidade> arena, IEntidade a, Vector3 pos, float r)
        {
            if (arena == null) return null;
            IEntidade melhor = null;
            float melhorD = float.PositiveInfinity;
            for (int i = 0; i < arena.Count; i++)
            {
                IEntidade e = arena[i];
                if (!Inimigo(a, e) || !Dentro(pos, r, e.Pos)) continue;
                float dd = Plano(e.Pos - pos).sqrMagnitude;
                if (dd < melhorD) { melhorD = dd; melhor = e; }
            }
            return melhor;
        }

        /// <summary>Area de CHAO: distancia horizontal (o pe' na encosta conta) dentro da faixa de altura.</summary>
        private static bool Dentro(Vector3 centro, float raio, Vector3 pos)
        {
            float dx = pos.x - centro.x, dz = pos.z - centro.z, dy = pos.y - centro.y;
            return dx * dx + dz * dz <= raio * raio && dy * dy <= FaixaM * FaixaM;
        }

        private static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
