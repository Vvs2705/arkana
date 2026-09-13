using System;
using System.Collections.Generic;

namespace Arkana.Gameplay
{
    /// <summary>O REGISTRO do Grupo C (12/09): Umbra, Brok, Gromm e Maris. Numeros em Kits.GrupoC.cs.</summary>
    public sealed partial class KitRunner
    {
        static partial void RegistrarGrupoC(Dictionary<string, Func<IHabilidade>> r)
        {
            r["12-umbra"] = () => new Umbra();
            r["13-brok"] = () => new Brok();
            r["14-gromm"] = () => new Gromm();
            r["15-maris"] = () => new Maris();
        }
    }
}
