using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// Projetil elemental: TEMPO DE VIAGEM (GDD §4.1 — nada de hitscan). Cor + FORMA por elemento (GDD §10).
    /// Numeros SO' de Balance x arma. Classe PURA de voo; o acerto e' delegado a um `Func` injetado (a casca
    /// resolve a fisica). `Lancar` e' o ponto UNICO por onde TODO tiro passa: emite Disparo (percepcao dos bots).
    /// </summary>
    public sealed class Projetil
    {
        public enum Forma { Esfera, Lamina, Dardo, Pedra, Espiral }

        /// <summary>m/s² de queda da agua ("arco leve").</summary>
        public const float WATER_ARC = 1.6f;
        /// <summary>Hitbox igual para todos — a FORMA e' leitura visual.</summary>
        public const float RAIO_HITBOX = 0.25f;

        public readonly IEntidade Atirador;
        public readonly Elemento ElementoDoTiro;
        public Vector3 Pos { get; private set; }
        public readonly Vector3 Dir;
        public readonly float Velocidade;
        public readonly float Dano;
        public float AlcanceRestante { get; private set; }
        public bool Vivo { get; private set; } = true;
        private float _queda;

        private Projetil(IEntidade atirador, Vector3 origem, Vector3 dir, Elemento el, ArmaSpec spec)
        {
            Atirador = atirador; Pos = origem; Dir = dir.normalized; ElementoDoTiro = el;
            Velocidade = spec.ProjectileSpeed; AlcanceRestante = spec.Range; Dano = spec.Dmg;
        }

        public static Forma FormaDe(Elemento el)
        {
            switch (el)
            {
                case Elemento.Agua: return Forma.Lamina;
                case Elemento.Raio: return Forma.Dardo;
                case Elemento.Terra: return Forma.Pedra;
                case Elemento.Vento: return Forma.Espiral;
                default: return Forma.Esfera;
            }
        }

        /// <summary>Cor canonica por elemento (paleta GDD §10).</summary>
        public static Color Tint(Elemento el)
        {
            switch (el)
            {
                case Elemento.Agua: return new Color32(0x2A, 0xA7, 0xFF, 255);
                case Elemento.Raio: return new Color32(0xF5, 0xD9, 0x0A, 255);
                case Elemento.Terra: return new Color32(0xA8, 0x76, 0x3E, 255);
                case Elemento.Vento: return new Color32(0x8F, 0xE8, 0xC9, 255);
                default: return new Color32(0xFF, 0x5A, 0x2A, 255);
            }
        }

        public Forma FormaDoTiro => FormaDe(ElementoDoTiro);

        /// <summary>
        /// O PONTO UNICO DO TIRO. O spec sai do slot do atirador (sem slot = perfil cru = luva comum).
        /// Emite Disparo para TODO mundo (conjurar denuncia) e SpellCast so' para o player.
        /// </summary>
        public static Projetil Lancar(IEntidade atirador, Vector3 origem, Vector3 direcao, Elemento el, ArmaSlot slot = null)
        {
            ArmaSpec spec = slot != null ? slot.Spec(el) : Arma.Spec(el, Arma.VARINHA);
            var p = new Projetil(atirador, origem, direcao, el, spec);
            Bus.EmitDisparo(atirador, origem);
            if (atirador != null && atirador.EhPlayer) Bus.EmitSpellCast(el);
            return p;
        }

        /// <summary>m: o maior pedaco de voo entre dois testes de acerto. A hitbox do corpo tem ~1,2 m de largura: pedacos de
        /// ate' 0,5 m nao pulam por cima dela. Antes era 1 teste por quadro — Raio + Cajado a 30 FPS andava 1,46 m e errava
        /// ate' no centro do alvo (achado de 04/10).</summary>
        public const float SUBPASSO_M = 0.5f;

        /// <summary>
        /// Um passo de voo, em pedacos de SUBPASSO_M. `acerto(pos)` devolve quem esta' na hitbox (ou null); `vizinhos` alimenta
        /// o arco de conducao; `livreM` = metros ate' o primeiro SOLIDO do mundo neste quadro (a casca mede: Partida.Obstaculo) —
        /// quem estiver antes dele leva o tiro, depois dele nao: a rocha e' cobertura, como a mira ja' tratava. Devolve true se
        /// o projetil ainda voa.
        /// </summary>
        public bool Tick(float dt, Func<Vector3, IEntidade> acerto = null, IList<IEntidade> vizinhos = null, float livreM = float.PositiveInfinity)
        {
            if (!Vivo || dt <= 0f) return false;
            float passo = Velocidade * dt;
            int n = Mathf.Max(1, Mathf.CeilToInt(passo / SUBPASSO_M));
            float sdt = dt / n, sub = passo / n, andou = 0f;
            for (int k = 0; k < n; k++)
            {
                if (andou + sub > livreM)
                {
                    Pos += Dir * Mathf.Max(livreM - andou, 0f);   // o tiro para NO solido (a cor do impacto, o fogo na parede)
                    Impacto(null, vizinhos);
                    return false;
                }
                Pos += Dir * sub;
                andou += sub;
                AlcanceRestante -= sub;
                if (ElementoDoTiro == Elemento.Agua)
                {
                    _queda += WATER_ARC * sdt;
                    Pos += new Vector3(0f, -_queda * sdt, 0f);
                }
                if (acerto != null)
                {
                    IEntidade alvo = acerto(Pos);
                    // PONTE A4: o tiro direto ATRAVESSA o aliado (fogo amigo desligado); o terreno segue pegando todo mundo
                    if (alvo != null && !Combat.MesmoTime(alvo, Atirador)) { Impacto(alvo, vizinhos); return false; }
                }
            }
            if (AlcanceRestante <= 0f) Vivo = false;
            return Vivo;
        }

        /// <summary>De onde o tiro NASCE: `saida` m a frente da mao, ou no solido colado nela (`livreM` = metros livres da mao
        /// na direcao do tiro). Sem isto, encostado na parede, o tiro nascia do outro lado dela.</summary>
        public static Vector3 Saida(Vector3 mao, Vector3 dir, float saida, float livreM, out bool colado)
        {
            colado = livreM < saida;
            return mao + dir * (colado ? Mathf.Max(livreM, 0f) : saida);
        }

        /// <summary>
        /// O ELEMENTO DEIXA DE SER COSMETICO: a reacao no alvo se resolve ANTES do dano (conducao multiplica este
        /// tiro). Dano SO' pelo Combat. TerrainHit SEMPRE: o terreno decide se reage; este codigo nunca muda o mundo.
        /// E' o impacto UNICO (corpo; chao, muro e peca de kit chegam com alvo null): a Sintonia ouve daqui, por ultimo.
        /// </summary>
        public void Impacto(IEntidade alvo, IList<IEntidade> vizinhos = null)
        {
            if (!Vivo) return;
            Vivo = false;
            if (alvo != null && !Combat.MesmoTime(alvo, Atirador))
            {
                float mult = Efeitos.Aplicar(alvo, ElementoDoTiro, Dano, Atirador, vizinhos);
                float efetivo = Combat.AplicarDano(alvo, Dano * mult, ElementoDoTiro, Atirador);
                if (efetivo > 0f && alvo.Vital != null && !alvo.Vital.Viva && !alvo.EhPlayer
                        && Atirador != null && Atirador.EhPlayer)
                    Bus.EmitPlayerKilledBot(alvo.Nome);
            }
            Bus.EmitTerrainHit(ElementoDoTiro, Pos, false);
            Sintonia.RegistrarImpacto(Atirador, ElementoDoTiro, Pos, alvo, Dano);
        }

        /// <summary>EMPURRAO = base x fator do elemento. Dobrado so' no chao (nada de juggle no ar).</summary>
        public float Empurrao(bool alvoNoChao)
        {
            float f = Balance.Perfil(ElementoDoTiro).Empurrao;
            if (f > 1f && !alvoNoChao) f = 1f;
            return Balance.Combate.Knockback * f;
        }
    }
}
