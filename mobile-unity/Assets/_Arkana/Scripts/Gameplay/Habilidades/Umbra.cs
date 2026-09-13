using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DA UMBRA (12) — Ataque/Perseguicao. Ficha em design/personagens/12-umbra.md; tempos em DIRECAO.md §4 e §6.
    ///   Passiva  Passo de Veludo   — corre SEM SOM de passos (o corpo: "sem_passos"); abate devolve 30% da esquiva
    ///   Tatica   Veu Umbrio        — 2,5 s de penumbra (o corpo: "penumbra" — o bot nao a ve' PARADA; andando e' um vulto);
    ///                                o 1o golpe saindo do veu da' +50% (lamina violeta no alvo)
    ///   Suprema  Danca das Sombras — 6 s: cada esquiva TELEPORTA 8 m e deixa uma sombra-isca que explode FRACO onde ela estava
    /// OS LIMITADORES: conjurar QUEBRA o veu (ataque ou suprema); a isca so' doi colada (2,5 m, assedio) e cada salto
    /// SUSSURRA (um Disparo: os bots a 30 m ouvem); no fim as sombras restantes implodem e ela fica 1 s cega de LUZ
    /// (sem conjurar). O bonus e o abate leem Bus.DamageApplied com fonte = ela: o +50% e' dano NOVO pelo ponto unico
    /// (Combat), nunca multiplicador escondido no projetil.
    /// </summary>
    public sealed class Umbra : IHabilidade
    {
        public const string VEU = "umbra_veu";
        public const string DANCA = "umbra_danca";
        /// <summary>m do teleporte de cada esquiva na Danca (ficha 12: "teleporta curto (8m)").
        /// ponytail: o numero mora aqui porque o Kits.GrupoC nao e' desta onda — mudar para umbra.Suprema["teleporte"].</summary>
        public const float TELEPORTE_M = 8f;
        /// <summary>s do "sem_passos" renovado a cada tique (morta, o corpo volta a fazer barulho sozinho).</summary>
        const float PASSO_S = 0.5f;

        /// <summary>
        /// Casca: devolve `fracao` do cooldown CHEIO da esquiva ao corpo. ponytail: a esquiva mora na Locomocao do Pawn e o
        /// IConjurador nao tem o verbo — o padrao fala com o Pawn, o teste troca. Motor: IConjurador.RecarregarEsquiva.
        /// </summary>
        public static Action<IConjurador, float> RecarregarEsquiva = PadraoRecarregar;

        private readonly List<Sombra> _sombras = new List<Sombra>();
        private KitRunner _k;
        private bool _ouvindo, _armado, _avisando;
        private float _veu, _veuT, _graca;
        private EfeitoVisual _auraVeu;

        public bool NoVeu => _veu > 0f;
        /// <summary>O proximo golpe dela leva o bonus (dentro do veu ou na janela curta logo depois).</summary>
        public bool BonusArmado => _armado;
        public int Abates { get; private set; }
        public IReadOnlyList<Sombra> Sombras => _sombras;

        public void Tick(KitRunner k, float dt)
        {
            Ouvir(k);
            k.Dono.AplicarEstado(Pawn.SEM_PASSOS, PASSO_S);   // Passo de Veludo: os passos dela nao entregam
            if (_veu > 0f)
            {
                _veu -= dt;
                _veuT += dt;
                // CONJURAR QUEBRA: atacou depois do veu comecar (o relogio de ataque zerou) ou a suprema esta' avisando
                if (_veu <= 0f || k.DesdeAtaque < _veuT - 0.0001f || k.Telegrafia > 0f) SairDoVeu(k);
            }
            else if (_graca > 0f)
            {
                _graca -= dt;
                if (_graca <= 0f) _armado = false;
            }
            // ANTECIPA (DIRECAO §6): a luz ao redor dela e' sugada para dentro, durante todo o aviso
            if (k.Telegrafia > 0f)
            {
                if (!_avisando) k.Visual("umbra_suga", k.Pos, k.Pos, k.Dados.Suprema["raio"], k.Telegrafia, k.Dono);
                _avisando = true;
            }
            else _avisando = false;
            if (k.EstadoAtivo(DANCA) && k.DashIniciou) Saltou(k);
            for (int i = _sombras.Count - 1; i >= 0; i--)
                if (!_sombras[i].Tick(k, dt)) _sombras.RemoveAt(i);
        }

        /// <summary>Veu Umbrio: penumbra NO CORPO (a visao do bot nao a pega parada) + bonus armado. A HUD so' ouve o estado.</summary>
        public void Tatica(KitRunner k)
        {
            Ouvir(k);
            Dictionary<string, float> t = k.Dados.Tatica;
            _veu = t["duracao"];
            _veuT = 0f;
            _graca = 0f;
            _armado = true;
            if (_auraVeu != null) _auraVeu.Restante = 0f;
            _auraVeu = k.Visual(VEU, k.Pos, k.Pos, 1f, _veu, k.Dono);
            k.Dono.AplicarEstado(Pawn.PENUMBRA, _veu);
            k.AvisarEstado(VEU, true);
        }

        /// <summary>Danca das Sombras: so' depois do aviso (o KitRunner chama). A aura e' a mesma fumaca do veu.</summary>
        public void Suprema(KitRunner k)
        {
            float dur = k.Dados.Suprema["duracao"];
            k.LigarEstado(DANCA, dur);
            k.Visual(VEU, k.Pos, k.Pos, 1.3f, dur, k.Dono);
        }

        /// <summary>Fim da danca: as iscas restantes IMPLODEM nela (sem dano) e a luz a cega 1 s — o oposto dela.</summary>
        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome != DANCA) return;
            for (int i = 0; i < _sombras.Count; i++) _sombras[i].Implodir();
            _sombras.Clear();
            float cega = k.Dados.Suprema["cega_dur"];
            k.Silenciar(cega);
            k.Visual("umbra_luz", k.Pos, k.Pos, 1.6f, cega, k.Dono);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        // ------------------------------------------------------------------ o veu

        private void SairDoVeu(KitRunner k)
        {
            _veu = 0f;
            if (_armado) _graca = k.Dados.Tatica["janela_bonus"];   // o tiro que saiu no fim do veu ainda voa
            if (_auraVeu != null) { _auraVeu.Restante = 0f; _auraVeu = null; }
            k.Dono.AplicarEstado(Pawn.PENUMBRA, 0f);   // o corpo volta a ser visto
            k.AvisarEstado(VEU, false);
        }

        /// <summary>Cada esquiva na danca TELEPORTA na direcao dela (o corpo corta o dash e pousa seguro): a isca fica ONDE ELA
        /// ESTAVA, a tinta risca a saida e a chegada, e o sussurro vaza de onde ela sumiu.</summary>
        private void Saltou(KitRunner k)
        {
            Vector3 de = k.Pos, dir = k.Dono.DirecaoDaEsquiva;
            _sombras.Add(new Sombra(k, de));
            k.Visual("umbra_tinta", de, de, 0.5f, 0.5f);
            Bus.EmitDisparo(k.Dono, de);   // o SUSSURRO do salto: counter sonoro para os bots
            if (dir.sqrMagnitude > 1e-6f && k.Dono.Teleportar(de + dir.normalized * TELEPORTE_M))
                k.Visual("umbra_tinta", k.Pos, k.Pos, 0.5f, 0.5f);   // o corte de tinta tambem na CHEGADA
        }

        // ------------------------------------------------------------------ o bonus e o abate

        /// <summary>
        /// Assina o Bus UMA vez (o KitRunner nao avisa a morte do kit): o ouvido se solta sozinho no primeiro dano depois
        /// que o corpo dela for destruido. ponytail: um ouvido morto por partida com Umbra ate' o proximo dano de alguem.
        /// </summary>
        private void Ouvir(KitRunner k)
        {
            if (_ouvindo) return;
            _k = k;
            _ouvindo = true;
            Bus.DamageApplied += AoDano;
        }

        private void AoDano(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool escudo)
        {
            KitRunner k = _k;
            UnityEngine.Object corpo = k.Dono as UnityEngine.Object;
            if (!ReferenceEquals(corpo, null) && corpo == null) { Bus.DamageApplied -= AoDano; _ouvindo = false; return; }
            if (!ReferenceEquals(fonte, k.Dono) || alvo == null || ReferenceEquals(alvo, fonte)) return;
            if (!_armado) { if (Caiu(alvo)) Abater(k); return; }
            _armado = false;   // desarma ANTES: o golpe extra volta por este mesmo ouvido (e conta o abate, se matar)
            _graca = 0f;
            if (_veu > 0f) SairDoVeu(k);
            if (Caiu(alvo)) { Abater(k); return; }   // o golpe base ja' matou: o bonus nao tem em quem
            Combat.AplicarDano(alvo, dano * k.Dados.Tatica["bonus_dano"], el, fonte);
            k.Visual("umbra_corte", alvo.Pos, alvo.Pos, 0.9f, 0.45f, alvo);   // a lamina violeta risca o alvo
        }

        private static bool Caiu(IEntidade e) => e.Vital != null && !e.Vital.Viva;

        private void Abater(KitRunner k)
        {
            Abates++;
            RecarregarEsquiva?.Invoke(k.Dono, k.Dados.Passiva["esquiva_por_abate"]);
        }

        private static void PadraoRecarregar(IConjurador c, float fracao)
        {
            Pawn p = c as Pawn;
            if (p == null || p.Loc == null) return;
            p.Loc.DodgeCd = Mathf.Max(p.Loc.DodgeCd - Balance.Dodge.Cooldown * fracao, 0f);
        }

        // ------------------------------------------------------------------ SOMBRA-ISCA
        /// <summary>A isca parada onde ela estava: espera um pouco e explode FRACO (so' quem esta' colado).</summary>
        public sealed class Sombra
        {
            public readonly Vector3 Pos;
            public float Espera { get; private set; }
            public readonly EfeitoVisual Visual;

            public Sombra(KitRunner k, Vector3 pos)
            {
                Dictionary<string, float> s = k.Dados.Suprema;
                Pos = pos;
                Espera = s["sombra_espera"];
                Visual = k.Visual("umbra_sombra", pos, pos, s["sombra_raio"], Espera);
            }

            /// <summary>Devolve false quando a isca acabou (explodiu).</summary>
            public bool Tick(KitRunner k, float dt)
            {
                Espera -= dt;
                if (Espera > 0f) return true;
                Dictionary<string, float> s = k.Dados.Suprema;
                // Vento: sombra nao e' elemento; o Vento e' o que menos morde escudo (0,75) — assedio, nao nuke
                foreach (IEntidade alvo in k.AlvosPerto(Pos, s["sombra_raio"], k.Dono))
                    Combat.AplicarDano(alvo, s["sombra_dano"], Elemento.Vento, k.Dono);
                k.Visual("umbra_estouro", Pos, Pos, s["sombra_raio"], 0.5f);
                return false;
            }

            public void Implodir() { Espera = 0f; Visual.Restante = 0f; }
        }
    }
}
