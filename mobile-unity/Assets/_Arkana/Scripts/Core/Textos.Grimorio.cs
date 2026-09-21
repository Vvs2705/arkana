using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>Textos do GRIMORIO DE DESCOBERTAS (onda 18C, GDD §18.3). Mesmo estilo do Textos.cs: acento na tela.</summary>
    public static partial class Textos
    {
        public const string GrimorioTitulo = "GRIMÓRIO DE DESCOBERTAS";
        public const string GrimorioBotao = "GRIMÓRIO";
        /// <summary>O aviso em partida, na linha de cima; a de baixo e' o nome da pagina.</summary>
        public const string GrimorioAviso = "PÁGINA DO GRIMÓRIO";
        public const string GrimorioContagem = "{0}/{1}";
        public const string GrimorioPaginas = "PÁGINAS";
        /// <summary>Pagina apagada: so' o enigma. O nome e a frase de verdade NUNCA aparecem antes da descoberta.</summary>
        public const string GrimorioTrancada = "???";
        public const string GrimorioLadoTerreno = "O TERRENO";
        public const string GrimorioLadoDupla = "A DUPLA E O DESFECHO";
        public const string GrimorioRodape = "Cada página é algo que você descobriu jogando. Nenhuma dá poder — descobrir é o prêmio.";

        /// <summary>
        /// id -> { nome, frase }. Os ids sao os do Gameplay.Grimorio (estaveis: o save guarda a POSICAO de cada um). So' o
        /// que o jogo FAZ hoje no celular — pagina que mente ensina errado (as duas trocas do Roblox estao no Grimorio.cs).
        /// </summary>
        public static readonly Dictionary<string, string[]> GrimorioPagina = new Dictionary<string, string[]>
        {
            { "lago_congelado", new[] { "Lago Congelado", "Água sobre a água vira gelo — e o gelo não deixa o raio passar." } },
            { "conducao", new[] { "Condução", "Raio na água corre o lago inteiro — e quem estiver dentro sente." } },
            { "fogo_apagado", new[] { "Fogo Apagado", "Água sobre as chamas devolve o chão: apagar não vira carvão." } },
            { "muro_de_pedra", new[] { "Muro de Pedra", "Terra ergue cobertura onde não havia nenhuma. Segura tiro — até cair." } },
            { "lamacal", new[] { "Lamaçal", "Água em chão de terra: quem entrar vai sair devagar." } },
            { "vento_no_fogo", new[] { "Vento no Fogo", "O vento não apaga: espalha. A chama corre para onde você sopra." } },
            { "sintonia", new[] { "Sintonia Completa", "Dois elementos, um feitiço só — mais forte que a soma das partes." } },
            { "pacto_quebrado", new[] { "Pacto Quebrado", "A canalização inimiga é um aviso. Você leu e cortou." } },
            { "retorno", new[] { "Retorno Contestável", "Você ficou ao lado do parceiro caído e o trouxe de volta." } },
            { "o_mapa_e_arma", new[] { "O Mapa é Arma", "Venceu depois de dobrar o terreno três vezes na mesma partida." } },
            { "no_vermelho", new[] { "No Vermelho", "A tempestade arcana mordeu fundo e você continuou de pé." } },
            { "escudo_coroado", new[] { "Escudo Coroado", "Derrubou alguém com o escudo já evoluído ao nível 3." } },
        };
    }
}
