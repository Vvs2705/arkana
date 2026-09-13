using System;
using System.Collections.Generic;

namespace Arkana.Gameplay
{
    /// <summary>O REGISTRO do Grupo A: slug -> fabrica. A ficha (Implementado = true) mora em Kits.GrupoA.cs — as duas andam juntas.</summary>
    public sealed partial class KitRunner
    {
        static partial void RegistrarGrupoA(Dictionary<string, Func<IHabilidade>> r)
        {
            r["02-ceifadora"] = () => new Ceifadora();
            r["04-corvus"] = () => new Corvus();
            r["05-corvomante"] = () => new Corvomante();
            r["06-olho-de-eter"] = () => new OlhoDeEter();
        }
    }
}
