using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA VEU (03) — Errante. GDD §3, ficha em design/personagens/03-veu.md; espelho de habilidades/veu.gd.
    ///   Passiva  Entrelinha      — 4s sem atacar/apanhar: desfoca (a distancia de 20m e' regra de quem OLHA)
    ///   Tatica   Atravessar      — 0,8s de ativacao + 1,5s no plano espectral (intangivel, invulneravel, rapida)
    ///   Suprema  Mare Espectral  — 5s de plano espectral para ela e os aliados a 6m
    /// OS LIMITADORES: qualquer dano quebra a passiva; o Atravessar deixa um ECO na entrada e ela sai 1s SEM
    /// CONJURAR (ataque incluso); na Mare NINGUEM conjura e um SINO toca no mundo real (KitState "sino_espectral").
    /// A intangibilidade e' do CORPO (IConjurador.AplicarEstado("intangivel")): a Mare leva aliados sem kit dela.
    /// </summary>
    public sealed class Veu : IHabilidade
    {
        public const string DESFOCADA = "desfocada";
        public const string ATIVACAO = "ativacao";
        public const string ESPECTRAL = "espectral";
        public const string MARE = "mare";
        public const string SINO = "sino_espectral";
        public const string INTANGIVEL = "intangivel";

        public bool Desfocada { get; private set; }

        public void Tick(KitRunner k, float dt)
        {
            // Entrelinha: quieta (sem atacar E sem apanhar) por `quietude` => desfocada. Borda: avisa 1x.
            bool quieta = Mathf.Min(k.DesdeAtaque, k.DesdeDano) >= k.Dados.Passiva["quietude"];
            if (quieta == Desfocada) return;
            Desfocada = quieta;
            k.AvisarEstado(DESFOCADA, quieta);
        }

        /// <summary>Atravessar: a tatica tambem AVISA (0,8s de ativacao) antes de valer.</summary>
        public void Tatica(KitRunner k) => k.LigarEstado(ATIVACAO, k.Dados.Tatica["ativacao"]);

        /// <summary>Mare: ela e os ALIADOS (EhPlayer, hoje o unico esquadrao) a `raio` m entram JUNTOS.</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            float dur = s["duracao"];
            k.LigarEstado(MARE, dur);
            k.BuffVelocidade(s["buff_vel"], dur);
            k.Silenciar(s["silencio"]);
            Espectral(k.Dono, dur);
            foreach (IEntidade a in k.AlvosPerto(k.Pos, s["raio"], k.Dono))
            {
                if (!a.EhPlayer) continue;
                Efeitos.Lentificar(a, s["buff_vel"], dur);
                Espectral(a, dur);
                IConjurador c = a as IConjurador;
                if (c != null) c.AplicarEstado("silencio", s["silencio"]);   // na Mare NINGUEM conjura
            }
            Bus.EmitKitState(SINO, true);   // o sino toca no mundo real: counter sonoro, para TODOS
        }

        public void EstadoAcabou(KitRunner k, string nome)
        {
            switch (nome)
            {
                case ATIVACAO: Entrar(k); break;
                case ESPECTRAL: k.Silenciar(k.Dados.Tatica["silencio_saida"]); break;   // o preco: 1s sem conjurar
                case MARE: Bus.EmitKitState(SINO, false); break;
            }
        }

        private void Entrar(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            k.Visual("eco", k.Pos, k.Pos, 0.35f, t["eco_dur"]);   // o ECO fica na ENTRADA, visivel
            k.LigarEstado(ESPECTRAL, t["duracao"]);
            k.BuffVelocidade(t["buff_vel"], t["duracao"]);
            Espectral(k.Dono, t["duracao"]);
        }

        /// <summary>Plano espectral = invulneravel (i-frames do Efeitos) + intangivel (o corpo resolve a colisao).</summary>
        private static void Espectral(IEntidade e, float dur)
        {
            Efeitos.EstadoAlvo s = Efeitos.De(e);
            s.IframesLeft = Mathf.Max(s.IframesLeft, dur);
            IConjurador c = e as IConjurador;
            if (c != null) c.AplicarEstado(INTANGIVEL, dur);
        }

        /// <summary>"Qualquer dano quebra a passiva": derruba no mesmo frame, sem esperar o tique.</summary>
        public void DanoRecebido(KitRunner k, float quanto)
        {
            if (!Desfocada) return;
            Desfocada = false;
            k.AvisarEstado(DESFOCADA, false);
        }

        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }
    }
}
