using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O CONTRATO de um mago implementado (espelho dos 5 estaticos de KitRunner.gd + o gancho do terreno).
    /// Uma instancia POR KitRunner: o estado do kit (muralhas, fios, tear, acumuladores) mora nela, nunca em
    /// estatico — dois magos iguais na mesma partida nao dividem nada. Numeros SO' de Kits.De(slug).
    /// </summary>
    public interface IHabilidade
    {
        /// <summary>Passiva + estados, 1x por frame (o KitRunner ja' andou cooldown, carga e telegrafia).</summary>
        void Tick(KitRunner k, float dt);
        void Tatica(KitRunner k);
        /// <summary>SO' depois da telegrafia (GDD §4.3). Nunca no toque.</summary>
        void Suprema(KitRunner k);
        void DanoRecebido(KitRunner k, float quanto);
        void EstadoAcabou(KitRunner k, string nome);
        /// <summary>Bus.TerrainHit repassado: os limitadores elementais (agua apaga, vento empurra, golpe na ancora).</summary>
        void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte);
    }
}
