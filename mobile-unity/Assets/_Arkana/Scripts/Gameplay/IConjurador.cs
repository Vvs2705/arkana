using System;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O MINIMO que o motor de kits (KitRunner) pede ao pawn que o carrega. Quem implementa: Gameplay.Pawn
    /// (raia PAWN); em teste, um fake. O kit NUNCA escreve velocidade, vida ou escudo por aqui: velocidade e'
    /// Efeitos.Lentificar (produto unico), dano e' Combat, escudo e' Vitalidade — o pawn so' entrega mira,
    /// chao, mana (que o kit LE' e nunca gasta — GDD §4.2) e os VERBOS que so' o corpo resolve (estado, teleporte, dash,
    /// pulo): o corpo cuida de CharacterController, agua, queda e chao; o kit so' pede.
    /// </summary>
    public interface IConjurador : IEntidade
    {
        /// <summary>A mana e' do ATAQUE. Tatica e suprema nao a tocam — o teste prova.</summary>
        float Mana { get; set; }
        /// <summary>Direcao de mira (camera no player, corpo no bot). Pode nao estar normalizada.</summary>
        Vector3 DirecaoDaMira { get; }
        /// <summary>Falso na queda: a carga da suprema NAO anda no ar (ordem do Diretor, 26/08).</summary>
        bool NoChao { get; }
        /// <summary>
        /// Estado que so' o corpo resolve: "intangivel" (i-frames), "silencio" (sem conjurar, aliado na Mare), "invisivel"
        /// (a visao do bot nao o pega; de perto, o brilho), "penumbra" (idem, so' PARADO: andando e' um vulto), "sem_passos"
        /// (os passos nao entregam). Renova para o maior prazo, nunca encurta; dur 0 APAGA (o kit revela quem escondeu).
        /// </summary>
        void AplicarEstado(string nome, float dur);
        /// <summary>Entidades VIVAS a ate' `raio` m do ponto (o pawn inclusive). Null = ninguem por perto.</summary>
        Func<Vector3, float, IEntidade[]> AlvosNoRaio { get; }
        /// <summary>O corpo SURGE em `destino` num quadro, com POUSO SEGURO: chao seco e livre (nunca o mar, dentro do morro
        /// ou em cima de copa, pilar ou muro), recuando pela linha de onde estava. Dash e empurrao nao atravessam junto.
        /// False = no ar (castelo, queda) ou morto: nao sai do lugar.</summary>
        bool Teleportar(Vector3 destino);
        /// <summary>DASH de kit: `metros` em `dur` s na direcao (XZ), na rampa da esquiva, e para — sem o deslize do empurrao.
        /// Sem i-frames (esquiva e' outra coisa). False = direcao, distancia ou tempo nulos.</summary>
        bool Impulso(Vector3 dir, float metros, float dur);
        /// <summary>Quantas vezes mais ALTO o proximo pulo sobe (1 = normal). A mola do Fizz escreve; o corpo aplica no pulo.</summary>
        float FatorDePulo { get; set; }
        /// <summary>Quantos pulos o corpo ja' deu: o kit ve' a DECOLAGEM pela borda do contador (a faisca da mola).</summary>
        int Pulos { get; }
        /// <summary>Para onde a ultima esquiva saiu (XZ normalizada). A Danca da Umbra teleporta por ela.</summary>
        Vector3 DirecaoDaEsquiva { get; }
    }
}
