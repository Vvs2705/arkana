using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO NOCTUS (19) — ataque / perseguicao. Ficha em design/personagens/19-noctus.md; tempos no DIRECAO.md §4 e §6.
    ///   Passiva  Sede de Eter     — 15% do dano causado (em inimigo: o DanoCausado ja' e' anti-farm) volta como ETER; abate
    ///                               devolve 25 de vida
    ///   Tatica   Mordida do Vazio — investida de 6 m que VIAJA: o 1o inimigo tocado perde 20 de eter (para ele) e fica MARCADO
    ///                               (o proximo disparo dele custa +50%)
    ///   Suprema  Forma de Nevoa   — 4 s intangivel (projetil atravessa), lentidao em quem ele atravessa
    /// OS LIMITADORES SAO PARTE DO KIT: drena ETER, nunca vida; a investida tem 6 m SECOS — errou, 1,5 s sem conjurar; na nevoa
    /// NAO conjura, a luz do dia o freia e o rastro mostra o caminho; ao recondensar fica FAMINTO 3 s (a passiva nao rende).
    /// Contra quem nao guarda eter (Basalto, boneco vazio) a mordida rende o que houver — ate' zero.
    /// §4.2 INTACTO: o Noctus e' o unico kit que ESCREVE mana — so' para GANHAR (passiva, mordida) e drenar a do ALVO. A propria
    /// nunca paga nada: tatica e suprema continuam cooldown e carga (o teste prova).
    /// </summary>
    public sealed class Noctus : IHabilidade
    {
        public const string NEVOA = "nevoa";
        public const string FAMINTO = "faminto";

        private readonly HashSet<IEntidade> _nevoaTocou = new HashSet<IEntidade>();
        private KitRunner _k;
        private Action<IEntidade, float, Elemento, IEntidade, bool> _aoDanar;
        private float _danoAntes = -1f;
        private ApoioGrupoD.Investida _mordida;
        private float _mordidaAcc;
        private IConjurador _marcado;
        private EfeitoVisual _marcaVisual;
        private float _marcaRestante, _eterDoMarcado;
        private float _nevoaAcc, _rastroAcc;

        public ApoioGrupoD.Investida MordidaEmCurso => _mordida;
        /// <summary>Quem carrega a marca agora (null = ninguem).</summary>
        public IConjurador Marcado => _marcado;

        public void Tick(KitRunner k, float dt)
        {
            Ouvir(k);
            Passiva(k);
            if (_mordida != null) Morder(k, dt);
            if (_marcado != null) Marca(k, dt);
            if (k.EstadoAtivo(NEVOA)) Nevoa(k, dt);
        }

        // ------------------------------------------------------------------ passiva

        /// <summary>O canal Apex, espelhado: o DELTA do dano causado (inimigo) vira eter. O 1o tique so' fotografa o que havia.</summary>
        private void Passiva(KitRunner k)
        {
            float causado = k.Dono.Vital.DanoCausado;
            if (_danoAntes < 0f) _danoAntes = causado;
            float delta = causado - _danoAntes;
            _danoAntes = causado;
            if (delta > 0f && !k.EstadoAtivo(FAMINTO)) GanharEter(k, delta * k.Dados.Passiva["conversao"]);
        }

        /// <summary>ABATE: o motor nao conta quem morreu — o Bus conta (DamageApplied sai com a vida JA' descontada). O ouvinte
        /// e' do KIT e o Desligar do motor nao o conhece: ele se solta sozinho quando o corpo some da cena (Bus.Reset tambem).</summary>
        private void Ouvir(KitRunner k)
        {
            if (_aoDanar != null) return;
            _k = k;
            _aoDanar = AoDanar;
            Bus.DamageApplied += _aoDanar;
        }

        private void AoDanar(IEntidade alvo, float quanto, Elemento el, IEntidade fonte, bool escudo)
        {
            if (_k == null) return;
            if (ApoioGrupoD.MortoNaCena(_k.Dono)) { Bus.DamageApplied -= _aoDanar; _k = null; return; }
            if (fonte != _k.Dono || alvo == null || alvo.Vital == null || alvo.Vital.Viva) return;
            if (ApoioGrupoD.MesmoLado(fonte, alvo) || !_k.Dono.Vital.Viva) return;
            _k.DevolverDano(_k.Dados.Passiva["abate_vida"]);
            _k.Visual("noctus_dreno", alvo.Pos, alvo.Pos, 0.25f, 0.9f, alvo);   // o banquete: o fio de eter corre do caido para ele
        }

        /// <summary>A UNICA escrita de mana do grupo D: GANHAR, grampeado no teto. A HUD fica sabendo (a regen do Pawn so' avisa
        /// quando esta' abaixo do teto).</summary>
        private static void GanharEter(KitRunner k, float q)
        {
            if (!(q > 0f)) return;
            IConjurador c = k.Dono;
            c.Mana = Mathf.Min(c.Mana + q, Balance.Player.ManaMax);
            if (c.EhPlayer) Bus.EmitManaChanged(c.Mana, Balance.Player.ManaMax);
        }

        // ------------------------------------------------------------------ tatica

        /// <summary>Mordida do Vazio: a rota sai na mira e o corpo e' empurrado por ela (a mesma conta).</summary>
        public void Tatica(KitRunner k)
        {
            float alcance = k.Dados.Tatica["alcance"];
            Vector3 d = k.Mira();
            _mordida = new ApoioGrupoD.Investida(k, "noctus_investida", k.Pos, d, alcance, 0.8f);
            _mordidaAcc = 0f;
            ApoioGrupoD.Impulso(k.Dono, d, alcance);
        }

        private void Morder(KitRunner k, float dt)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            _mordida.Andar(dt);
            _mordidaAcc += dt;
            if (_mordidaAcc < 0.05f && !_mordida.Acabou) return;
            _mordidaAcc = 0f;
            IEntidade alvo = _mordida.Tocou(k, t["raio_toque"]);
            if (alvo != null)
            {
                _mordida = null;
                Drenar(k, alvo);
                return;
            }
            if (!_mordida.Acabou) return;
            _mordida = null;
            k.Silenciar(t["erro_dur"]);   // 6 m secos: errou, 1,5 s vulneravel (sem conjurar — a janela de punicao)
        }

        private void Drenar(KitRunner k, IEntidade alvo)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            k.Visual("noctus_dreno", alvo.Pos, alvo.Pos, 0.2f, 0.8f, alvo);
            IConjurador c = alvo as IConjurador;
            if (c == null) return;   // quem nao guarda eter nao rende nada
            float q = Mathf.Min(t["dreno"], Mathf.Max(c.Mana, 0f));
            c.Mana -= q;
            GanharEter(k, q);
            if (_marcaVisual != null) _marcaVisual.Restante = 0f;
            _marcado = c;
            _marcaRestante = t["marca_dur"];
            _eterDoMarcado = c.Mana;
            _marcaVisual = k.Visual("noctus_marca", alvo.Pos, alvo.Pos, 0.45f, t["marca_dur"], alvo);
        }

        /// <summary>A MARCA: a mana do marcado CAIU de um tique para o outro (disparou) — cobra mais metade do que caiu, uma vez,
        /// e a marca sai. ponytail: "o proximo ACERTO dele em Noctus" vira "o proximo DISPARO dele" — o motor nao diz ao kit
        /// quem acertou quem; pagar no disparo pede um fator de custo por atirador no Pawn.Atirar.</summary>
        private void Marca(KitRunner k, float dt)
        {
            _marcaRestante -= dt;
            if (_marcaRestante <= 0f || _marcado.Vital == null || !_marcado.Vital.Viva) { SoltarMarca(); return; }
            float m = _marcado.Mana;
            if (m < _eterDoMarcado - 1f)
            {
                _marcado.Mana = Mathf.Max(m - (_eterDoMarcado - m) * k.Dados.Tatica["marca_extra"], 0f);
                SoltarMarca();
                return;
            }
            _eterDoMarcado = m;   // a regen sobe a regua
        }

        private void SoltarMarca()
        {
            _marcado = null;
            if (_marcaVisual != null) _marcaVisual.Restante = 0f;
            _marcaVisual = null;
        }

        // ------------------------------------------------------------------ suprema

        /// <summary>Forma de Nevoa: intangivel pelo corpo (i-frames: o Acerto da Partida pula) e sem conjurar.
        /// ponytail: a ilha e' dia inteira hoje — a nevoa anda a 0,85 sempre; sombra de mata/ruina nao e' consultada.</summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            float dur = s["duracao"];
            k.LigarEstado(NEVOA, dur);
            k.Silenciar(dur);
            k.BuffVelocidade(s["vel_dia"], dur);
            Efeitos.EstadoAlvo e = Efeitos.De(k.Dono);
            e.IframesLeft = Mathf.Max(e.IframesLeft, dur);
            k.Dono.AplicarEstado("intangivel", dur);
            k.Visual("noctus_nevoa", k.Pos, k.Pos, 1f, dur, k.Dono);
            _nevoaTocou.Clear();
            _nevoaAcc = 0f;
            _rastroAcc = 0f;
        }

        /// <summary>Na nevoa: o RASTRO fica no chao (o limitador: mostra o caminho) e quem ele atravessa fica lento, uma vez.</summary>
        private void Nevoa(KitRunner k, float dt)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            _rastroAcc += dt;
            if (_rastroAcc >= s["rastro_passo"]) { _rastroAcc = 0f; k.Visual("noctus_rastro", k.Pos, k.Pos, 0.6f, s["rastro_dur"]); }
            _nevoaAcc += dt;
            if (_nevoaAcc < 0.1f) return;
            _nevoaAcc = 0f;
            foreach (IEntidade e in k.AlvosPerto(k.Pos, s["raio_toque"], k.Dono))
                if (ApoioGrupoD.Inimigo(k.Dono, e) && _nevoaTocou.Add(e)) Efeitos.Lentificar(e, s["lentidao"], s["lentidao_dur"]);
        }

        /// <summary>Recondensa de joelhos, FAMINTO: 3 s sem a passiva render.</summary>
        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome != NEVOA) return;
            k.LigarEstado(FAMINTO, k.Dados.Suprema["faminto_dur"]);
            k.Visual("noctus_rastro", k.Pos, k.Pos, 1.2f, 1f);
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }
    }
}
