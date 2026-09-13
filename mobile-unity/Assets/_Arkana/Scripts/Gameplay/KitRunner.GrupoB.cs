using System;
using System.Collections.Generic;

namespace Arkana.Gameplay
{
    /// <summary>O REGISTRO do Grupo B (12/09): Vitalis, Ilusionista, Vex e Aelion. Ficha em Kits.GrupoB.cs.</summary>
    public sealed partial class KitRunner
    {
        static partial void RegistrarGrupoB(Dictionary<string, Func<IHabilidade>> r)
        {
            r["07-vitalis"] = () => new Vitalis();
            r["08-ilusionista"] = () => new Ilusionista();
            r["09-vex"] = () => new Vex();
            r["11-aelion"] = () => new Aelion();
        }
    }
}
