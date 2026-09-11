using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O que a raia GAMEPLAY pergunta ao MUNDO — e so' isso. As classes puras recebem isto por parametro
    /// (nunca a cena) para o teste EditMode fingir uma ilha. `Arkana.World.Relevo` ja' tem estes cinco
    /// membros com estes nomes (RaioTerra/Pois sao campos la': o coordenador liga por `: IRelevo` ou adaptador).
    /// </summary>
    public interface IRelevo
    {
        /// <summary>Raio de TERRA FIRME em metros — a unica medida de que zona, loot e bau precisam.</summary>
        float RaioTerra { get; }
        /// <summary>A verdade do chao em (x, z).</summary>
        float Altura(float x, float z);
        /// <summary>Cota da lamina d'agua em (x, z), ou `Relevo.Seco` (-1e9) onde nao ha' agua.</summary>
        float SuperficieDaAgua(float x, float z);
        /// <summary>Chao seco e acima da praia: onde loot, bau e circulo final podem pousar.</summary>
        bool PodePousar(float x, float z);
        /// <summary>Pontos de interesse com centro E raio (fracao do raio de terra).</summary>
        Poi[] Pois { get; }
    }
}
