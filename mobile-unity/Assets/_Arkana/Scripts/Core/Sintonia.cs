using System;
using System.Collections.Generic;
using UnityEngine;

namespace Arkana.Core
{
    /// <summary>Os 10 pares da matriz do GDD §9. Elemento igual nao funde, entao nao ha' 11o.</summary>
    public enum ComboSintonia
    {
        TornadoFlamejante, ChuvaDeMagma, ExplosaoDePlasma, CortinaDeVapor,
        Eletrocussao, Lamacal, TempestadeTorrencial, TempestadeDeAreia, CristaisCarregados, NuvemTempestuosa
    }

    /// <summary>O combo que SAIU: tudo o que o Efeito (17C) precisa para aplicar dano e terreno pelo Combat.</summary>
    public struct DisparoSintonia
    {
        public ComboSintonia Combo;
        /// <summary>Os dois conjuradores (A = o do 1o impacto). A e' o AUTOR do combo (Roblox: um autor por fusao).</summary>
        public IEntidade A, B;
        public Elemento ElA, ElB;
        /// <summary>Centro do combo: media dos dois impactos, no chao (tiro no corpo conta no pe' do alvo).</summary>
        public Vector3 Ponto;
        /// <summary>Quem o 2o impacto atingiu (ou o 1o); null = so' area.</summary>
        public IEntidade Alvo;
        /// <summary>(danoA + danoB) x Balance.Sintonia.Mult — "mais forte que a soma das partes", ao pe' da letra.</summary>
        public float Dano;
    }

    /// <summary>
    /// CONJURACAO COMBINADA ("Sintonia", GDD §9) — o pilar de inovacao. Regra provada no Roblox
    /// (roblox/src/src/server/Sintonia.luau), reescrita aqui; os numeros sao KNOB em Balance.Sintonia.
    /// FLUXO: todo projetil que IMPACTA (corpo ou chao) chama RegistrarImpacto -> casa com um impacto PENDENTE de um
    /// ALIADO (Combat.MesmoTime), de OUTRO conjurador, de ELEMENTO diferente, ha' no maximo JanelaS e a menos de
    /// RaioAlvoM no chao -> CANALIZA CanalizacaoS (Bus.SintoniaCanalizando, todo mundo ve: e' o telegrama que deixa
    /// o inimigo interromper) -> DISPARA (Efeito, depois Bus.SintoniaDisparou) ou FALHA se um dos dois cai ou morre.
    /// Este modulo decide QUANDO, ONDE e QUANTO; dano e terreno sao do Efeito, pelo ponto unico (Combat).
    /// A regra nao sabe quem e' humano: dupla de bots faz Sintonia igual.
    /// </summary>
    public static class Sintonia
    {
        /// <summary>
        /// IMPACTO SEM PAR fica PENDENTE por JanelaS. Cada impacto e' um candidato (uma rajada deixa varios); o par
        /// procura do MAIS RECENTE para o mais antigo e vale o primeiro que casa (Roblox). Os outros pendentes do
        /// mesmo conjurador ficam e expiram: ele entra em recarga e nao casa mais.
        /// </summary>
        private struct Pendente { public IEntidade Quem; public Elemento El; public Vector3 Pe; public IEntidade Alvo; public float Dano; public float T; }
        private sealed class Canal { public DisparoSintonia D; public float FimEm; }

        private static readonly List<Pendente> _pendentes = new List<Pendente>();
        private static readonly List<Canal> _canais = new List<Canal>();
        // Relogio proprio (so' Tick anda): pendente, canal e recarga sao "instantes" nele — nada a decrementar por frame.
        private static readonly Dictionary<IEntidade, float> _prontoEm = new Dictionary<IEntidade, float>();
        private static float _agora;

        /// <summary>Gancho do efeito (padrao Derrubado.Instalar -> Combat.InterceptarMorte): a 17C instala; Reset() limpa.</summary>
        public static Action<DisparoSintonia> Efeito;

        /// <summary>GDD §9: mesmo elemento nao funde -> null. Ordem dos parametros nao importa (ordena pelo enum).</summary>
        public static ComboSintonia? ComboDe(Elemento a, Elemento b)
        {
            switch (a < b ? (a, b) : (b, a))
            {
                case (Elemento.Fogo, Elemento.Vento): return ComboSintonia.TornadoFlamejante;
                case (Elemento.Fogo, Elemento.Terra): return ComboSintonia.ChuvaDeMagma;
                case (Elemento.Fogo, Elemento.Raio): return ComboSintonia.ExplosaoDePlasma;
                case (Elemento.Fogo, Elemento.Agua): return ComboSintonia.CortinaDeVapor;
                case (Elemento.Agua, Elemento.Raio): return ComboSintonia.Eletrocussao;
                case (Elemento.Agua, Elemento.Terra): return ComboSintonia.Lamacal;
                case (Elemento.Agua, Elemento.Vento): return ComboSintonia.TempestadeTorrencial;
                case (Elemento.Terra, Elemento.Vento): return ComboSintonia.TempestadeDeAreia;
                case (Elemento.Raio, Elemento.Terra): return ComboSintonia.CristaisCarregados;
                case (Elemento.Raio, Elemento.Vento): return ComboSintonia.NuvemTempestuosa;
            }
            return null;
        }

        /// <summary>
        /// Todo projetil que IMPACTA chama (Projetil.Impacto, corpo e chao). Quem esta' morto, derrubado, canalizando ou
        /// em recarga nem entra na janela. Tiro no corpo conta no PE' do alvo (o combo acontece no chao, onde o terreno
        /// reage); a distancia e' so' no plano — morro nao separa dois tiros no mesmo alvo.
        /// CANALIZANDO o mago anda e atira normalmente (Roblox): so' nao abre outro combo — o tiro dele nem vira pendente.
        /// </summary>
        public static void RegistrarImpacto(IEntidade atirador, Elemento el, Vector3 ponto, IEntidade alvoAtingido, float dano)
        {
            if (!Livre(atirador)) return;
            Vector3 pe = alvoAtingido != null ? alvoAtingido.Pos : ponto;
            for (int i = _pendentes.Count - 1; i >= 0; i--)
            {
                Pendente c = _pendentes[i];
                if (_agora - c.T > Balance.Sintonia.JanelaS) { _pendentes.RemoveAt(i); continue; }   // a varredura e' a faxina
                if (c.Quem == atirador || c.El == el) continue;
                if (!Combat.MesmoTime(c.Quem, atirador) || !Livre(c.Quem)) continue;
                if (NoPlano(c.Pe - pe) > Balance.Sintonia.RaioAlvoM) continue;
                _pendentes.RemoveAt(i);
                Canalizar(c, atirador, el, pe, alvoAtingido, dano);
                return;
            }
            _pendentes.Add(new Pendente { Quem = atirador, El = el, Pe = pe, Alvo = alvoAtingido, Dano = dano, T = _agora });
        }

        /// <summary>O relogio: canal que perdeu um conjurador FALHA; canal que completou CanalizacaoS DISPARA.</summary>
        public static void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            _agora += dt;
            for (int i = _canais.Count - 1; i >= 0; i--)
            {
                Canal ch = _canais[i];
                bool aDePe = DePe(ch.D.A), bDePe = DePe(ch.D.B);
                if (aDePe && bDePe && _agora < ch.FimEm) continue;
                _canais.RemoveAt(i);   // sai ANTES do Efeito: se ele reentrar (um combo que lanca projetil), este canal nao existe mais
                if (aDePe && bDePe)
                {
                    Efeito?.Invoke(ch.D);
                    Bus.EmitSintoniaDisparou(ch.D);
                    continue;
                }
                // ANTI-GRIEFING (PONTE A5): QUEM CONTINUA DE PE' NAO PAGA. A unica forma de cancelar e' cair ou morrer,
                // e ninguem provoca o proprio reembolso — so' o do parceiro. Quem caiu segue pagando.
                if (aDePe) _prontoEm.Remove(ch.D.A);
                if (bDePe) _prontoEm.Remove(ch.D.B);
                Bus.EmitSintoniaFalhou(ch.D.Combo, ch.D.A, ch.D.B, ch.D.Ponto);
            }
        }

        /// <summary>Fim/inicio de partida: sem isto a recarga de 24 s atravessava para a partida seguinte (cicatriz do Roblox).</summary>
        public static void Reset()
        {
            _pendentes.Clear();
            _canais.Clear();
            _prontoEm.Clear();
            _agora = 0f;
            Efeito = null;
        }

        public static bool Canalizando(IEntidade e)
        {
            for (int i = 0; i < _canais.Count; i++)
                if (_canais[i].D.A == e || _canais[i].D.B == e) return true;
            return false;
        }

        /// <summary>Segundos ate' poder combinar de novo; 0 = pronto.</summary>
        public static float CooldownRestante(IEntidade e)
        {
            float t;
            return e != null && _prontoEm.TryGetValue(e, out t) ? Mathf.Max(t - _agora, 0f) : 0f;
        }

        /// <summary>
        /// CUSTO COBRADO NO INICIO (GDD §9 + PONTE A5): os DOIS entram em recarga quando a canalizacao COMECA, nao
        /// quando o combo sai. E' isto que impede o spam e deixa a dupla seca depois de um combo errado.
        /// </summary>
        private static void Canalizar(Pendente a, IEntidade b, Elemento elB, Vector3 peB, IEntidade alvoB, float danoB)
        {
            var d = new DisparoSintonia
            {
                Combo = ComboDe(a.El, elB).Value,   // elementos diferentes: sempre ha' par
                A = a.Quem, B = b, ElA = a.El, ElB = elB,
                Ponto = (a.Pe + peB) * 0.5f,
                Alvo = alvoB ?? a.Alvo,
                Dano = (a.Dano + danoB) * Balance.Sintonia.Mult,
            };
            _prontoEm[a.Quem] = _agora + Balance.Sintonia.CooldownS;
            _prontoEm[b] = _agora + Balance.Sintonia.CooldownS;
            _canais.Add(new Canal { D = d, FimEm = _agora + Balance.Sintonia.CanalizacaoS });
            Bus.EmitSintoniaCanalizando(d.Combo, d.A, d.B, d.Ponto, Balance.Sintonia.CanalizacaoS);
        }

        // Derrubado e' estado de Gameplay: a reserva de esvaecimento deixa Vital.Viva true, entao "de pe'" pergunta aos dois.
        private static bool DePe(IEntidade e) =>
            e != null && e.Vital != null && e.Vital.Viva && !Arkana.Gameplay.Derrubado.Esta(e);

        // "Canalizando nao abre combo" sai de graca: o custo e' cobrado no inicio, entao quem canaliza ja' esta' em recarga.
        private static bool Livre(IEntidade e) => DePe(e) && CooldownRestante(e) <= 0f;

        private static float NoPlano(Vector3 v) => new Vector2(v.x, v.z).magnitude;
    }
}
