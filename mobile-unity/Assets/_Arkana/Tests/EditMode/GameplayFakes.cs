using System;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>Ilha de mentira: SO' o contrato que a raia GAMEPLAY le'. Plana no zero, seca, sem POIs por padrao.</summary>
    public sealed class FakeRelevo : IRelevo
    {
        public float Raio = 132f;
        public Func<float, float, float> AlturaFn = (x, z) => 0f;
        public Func<float, float, float> AguaFn = (x, z) => Relevo.Seco;
        /// <summary>Null = seco onde nao ha' agua e dentro do raio de terra.</summary>
        public Func<float, float, bool> PousarFn;
        public Poi[] PoisLista = new Poi[0];

        public static Poi PoiDe(string nome, Vector2 centro, float raioM, float raioTerra) => new Poi(nome, centro, raioM / raioTerra, raioTerra);

        public float RaioTerra => Raio;
        public float Altura(float x, float z) => AlturaFn(x, z);
        public float SuperficieDaAgua(float x, float z) => AguaFn(x, z);
        public bool PodePousar(float x, float z)
        {
            if (PousarFn != null) return PousarFn(x, z);
            return AguaFn(x, z) <= Relevo.Seco && new Vector2(x, z).magnitude <= Raio;
        }
        public Poi[] Pois => PoisLista;
    }

    /// <summary>Um pawn sem cena: nome, posicao, lado e a Vitalidade do Core (escudo nivel 1 cheio).</summary>
    public sealed class FakeEntidade : IEntidade
    {
        public string Nome { get; set; }
        public Vector3 Pos { get; set; }
        public bool EhPlayer { get; set; }
        public Vitalidade Vital { get; set; }

        public FakeEntidade(string nome, Vector3 pos, bool ehPlayer = false)
        {
            Nome = nome; Pos = pos; EhPlayer = ehPlayer;
            Vital = new Vitalidade(Balance.Player.Hp);
        }
    }
}
