using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Terrain
{
    /// <summary>
    /// A casca do terreno reativo: liga a classe pura a Ilha.Atual (grade do Lado real, biomas) e a Vegetacao
    /// (arvores viram combustivel e o carvao as consome). Sem ilha na cena, plano de 180 m — fiacao defensiva.
    /// Quem DESENHA as celulas (fogo, gelo, muro) le' `Terreno.Visiveis`; esta casca so' simula.
    /// </summary>
    public sealed class TerrenoReativoBehaviour : MonoBehaviour
    {
        [SerializeField] int seed = TerrenoReativo.WallSeed;
        /// <summary>Lado do plano de fallback (sem ilha), em metros.</summary>
        [SerializeField] float ladoSemIlha = 180f;

        /// <summary>A casca viva na cena (o Pawn le' FatorTerreno daqui; a Partida ticka o Terreno dela). Null sem cena.</summary>
        public static TerrenoReativoBehaviour Atual { get; private set; }

        public TerrenoReativo Terreno { get; private set; }
        /// <summary>Os pawns da arena (a cena escreve): quem pisa fogo/agua eletrificada paga DoT. So' vale FORA de partida —
        /// com Partida rodando, e' ela que ticka o terreno com a Arena (um dono de relogio, como o DoT).</summary>
        public IList<IEntidade> Alvos;

        void Start()
        {
            Ilha ilha = Ilha.Atual;
            if (ilha != null && ilha.Relevo != null)
            {
                Terreno = TerrenoReativo.Da(ilha.Relevo, seed);
                Vegetacao veg = ilha.Vegetacao;
                if (veg != null)
                {
                    for (int i = 0; i < veg.ContarArvores(); i++) Terreno.RegistrarArvore(i, veg.PosArvore(i));
                    Terreno.AoQueimarArvore = (i, q) => veg.MarcarQueimada(i, q);
                }
            }
            else Terreno = new TerrenoReativo(null, ladoSemIlha, seed);
            KitRunner.DpsDoTerreno = Terreno.DpsEm;   // a passiva da Pyra le' o fogo do chao daqui
            Bus.MatchStarted += Terreno.Reset;         // estado novo nao atravessa partida
            Atual = this;
        }

        void Update()
        {
            if (Terreno == null) return;
            // com partida rodando o relogio e' da Partida.Tick (senao o DoT do chao cobra em dobro)
            Partida m = Partida.Atual;
            if (m != null && m.Rodando) return;
            Terreno.Tick(Time.deltaTime, Alvos);
        }

        void OnDestroy()
        {
            if (Atual == this) Atual = null;
            if (Terreno == null) return;
            Bus.MatchStarted -= Terreno.Reset;
            Terreno.Desligar();
            KitRunner.DpsDoTerreno = null;
        }
    }
}
