using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O SLOT DE ARMA DO PAWN — estado puro, composicao por fora. Guarda a luva equipada, o elemento
    /// que MORA nela (DIRECAO.md §1) e o par fixo da manopla.
    /// A LEI DAS LUVAS: todos caem de MAOS NUAS; sem luva NAO ha' ataque basico. Taticas e supremas sao natas.
    /// </summary>
    public sealed class ArmaSlot
    {
        /// <summary>Segundos de animacao do gesto de pegar e o instante em que a mao alcanca o chao (numeros do MUNDO).</summary>
        public const float GESTO_S = 0.65f;
        public const float GESTO_CONTATO_S = 0.24f;

        public readonly IEntidade Dono;
        /// <summary>"" = maos nuas.</summary>
        public string ArmaId { get; private set; } = "";
        /// <summary>So' a manopla usa (2 fixos).</summary>
        public Elemento[] Par { get; private set; } = new Elemento[0];
        /// <summary>O elemento DA LUVA (viaja com ela no swap). Null = luva legada sem elemento.</summary>
        public Elemento? ElementoDaLuva { get; private set; }
        /// <summary>Pega sozinho quando o loot e' de tier ESTRITAMENTE melhor (bot). Sidegrade passa pelo botao.</summary>
        public bool AutoUpgrade = true;

        private int _alt;

        public ArmaSlot(IEntidade dono) { Dono = dono; }

        public bool Armado => ArmaId != "";
        /// <summary>Sem luva nao ha' ataque basico. Habilidades NAO passam por aqui.</summary>
        public bool PodeAtacar => Armado;
        /// <summary>Maos nuas = tier -1: QUALQUER luva do chao e' upgrade.</summary>
        public int Tier => Armado ? Arma.Tier(ArmaId) : -1;
        public ArmaDef Dados => Arma.Dados(ArmaId);
        public ArmaSpec Spec(Elemento el) => Arma.Spec(el, ArmaId);

        /// <summary>
        /// Luva de elemento travado esconde o carrossel; maos nuas tambem (nao existe elemento na tela).
        /// So' a luva legada sem elemento deixa o carrossel escolher.
        /// </summary>
        public bool CarrosselVisivel => Armado && Dados.Elementos <= 1 && ElementoDaLuva == null;

        /// <summary>Elementos que a arma porta. Manopla: o par; luva: o dela (ou o escolhido, se legada).</summary>
        public Elemento[] Elementos(Elemento escolhido)
        {
            if (Dados.Elementos > 1 && Par.Length >= 2) return Par;
            return new[] { ElementoDaLuva ?? escolhido };
        }

        /// <summary>O elemento que o PROXIMO disparo vai usar, SEM girar a manopla: o tiro cobra mana e cadencia dele e so' gira
        /// depois de pagar (antes a manopla cobrava o elemento do carrossel e atirava o do par — achado de 04/10).</summary>
        public Elemento ProximoDisparo(Elemento escolhido)
        {
            Elemento[] els = Elementos(escolhido);
            return els.Length >= 2 ? els[(_alt + 1) % els.Length] : ElementoDaLuva ?? escolhido;
        }

        /// <summary>Elemento DESTE disparo. Manopla alterna os 2 fixos tiro a tiro; a luva manda o dela.</summary>
        public Elemento ElementoDoDisparo(Elemento escolhido)
        {
            Elemento[] els = Elementos(escolhido);
            if (els.Length >= 2)
            {
                _alt = (_alt + 1) % els.Length;
                return els[_alt];
            }
            return ElementoDaLuva ?? escolhido;
        }

        /// <summary>Troca a arma ativa. Emite WeaponEquipped com o pawn PRIMEIRO. Devolve false para id invalido.</summary>
        public bool Equipar(string id, Elemento[] par = null, Elemento? elemento = null)
        {
            if (!Arma.Existe(id)) return false;
            ArmaId = id;
            Par = par ?? new Elemento[0];
            ElementoDaLuva = elemento;
            _alt = 0;
            ArmaDef d = Dados;
            Bus.EmitWeaponEquipped(Dono, id, d.Nome, d.Raridade, Elementos(Elemento.Fogo));
            return true;
        }

        /// <summary>
        /// PEGAR vira TROCAR: equipa a nova e devolve a anterior (para o loot deixa-la no chao).
        /// Null = estava de maos nuas (o loot e' CONSUMIDO: nada de "arma vazia" fantasma no chao)
        /// ou a arma ja' era a mesma (nada acontece, sem sinal duplicado — `trocou` = false).
        /// </summary>
        public LootItem Trocar(string id, Elemento[] par, Elemento? elemento, out bool trocou)
        {
            trocou = false;
            if (id == ArmaId || !Arma.Existe(id)) return null;
            LootItem velha = Armado ? new LootItem(ArmaId, Par, ElementoDaLuva, UnityEngine.Vector3.zero) : null;
            trocou = Equipar(id, par, elemento);
            return velha;
        }

        /// <summary>Volta as maos nuas (teste / restart).</summary>
        public void Desarmar()
        {
            ArmaId = "";
            Par = new Elemento[0];
            ElementoDaLuva = null;
            _alt = 0;
        }
    }
}
