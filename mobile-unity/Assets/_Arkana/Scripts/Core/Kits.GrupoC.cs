using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// NUMEROS DO GRUPO C (12/09): Umbra, Brok, Gromm e Maris. Fichas em design/personagens/12..15 (texto) e os tempos de
    /// DIRECAO.md §4 (tabela dos 20). As mesmas leis do Kits.cs: nada de "mana"/"custo" (o teste reprova), telegrafia
    /// entre 1 e 4 s, tatica entre 5 e 10 s. Os LIMITADORES moram no MESMO bloco do poder que pagam.
    /// </summary>
    public static partial class Kits
    {
        static partial void GrupoC(Dictionary<string, KitDef> m)
        {
            KitDef umbra = m["12-umbra"];
            umbra.Implementado = true;
            umbra.TaticaCd = 8f;          // o veu arma um golpe de +50%: agride, mas nao e' dano puro
            umbra.SupremaCarga = 45f;
            umbra.Telegrafia = 1.5f;      // a luz ao redor dela e' SUGADA para dentro
            // Passo de Veludo: abate devolve 30% da esquiva (fracao do cooldown cheio da esquiva).
            umbra.Passiva = new Dictionary<string, float>
            {
                { "esquiva_por_abate", 0.3f },
            };
            // Veu Umbrio: 2,5 s de penumbra; o 1o golpe saindo dele da' +50%. O PRECO: conjurar QUEBRA o veu; a janela
            // depois do veu e' curta (o tiro que saiu no fim ainda voa).
            umbra.Tatica = new Dictionary<string, float>
            {
                { "duracao", 2.5f }, { "bonus_dano", 0.5f }, { "janela_bonus", 1.5f },
            };
            // Danca das Sombras: 6 s, cada esquiva deixa uma sombra-isca que explode FRACO onde ela estava. O PRECO: a isca
            // so' doi colada (2,5 m — assedio, nao nuke), o sussurro denuncia (Disparo), e no fim 1 s cega de LUZ (sem conjurar).
            umbra.Suprema = new Dictionary<string, float>
            {
                { "duracao", 6f }, { "raio", 2f }, { "sombra_espera", 0.6f }, { "sombra_raio", 2.5f }, { "sombra_dano", 15f },
                { "cega_dur", 1f },
            };

            KitDef brok = m["13-brok"];
            brok.Implementado = true;
            brok.TaticaCd = 7f;           // protecao: meio-piso da regua
            brok.SupremaCarga = 40f;      // utilidade de grupo enche rapido
            brok.Telegrafia = 2f;         // ergue a bigorna acima da cabeca, clang a cada passo
            // Tempera Eterna: 15% menos dano de TERRENO (fogo/eletrico no chao).
            brok.Passiva = new Dictionary<string, float>
            {
                { "reducao_terreno", 0.15f },
            };
            // Runa-Escudo: muralha curva de 4 m a 2,5 m a frente, bloqueia tiro inimigo por 6 s. O PRECO: so' a FRENTE
            // (flanco e por cima passam), vida propria (tiro gasta x Estrutura do elemento).
            brok.Tatica = new Dictionary<string, float>
            {
                { "comprimento", 4f }, { "raio_arco", 2.5f }, { "altura", 2.2f }, { "espessura", 0.5f },
                { "duracao", 6f }, { "vida", 120f },
            };
            // Forja Viva: bigorna 12 s; quem esta' no raio regenera ESCUDO (nunca vida) + runa pessoal de 25. O PRECO: a
            // bigorna tem 150 de vida, e' BARULHENTA (clang = Disparo que os bots ouvem) e no fim as runas do martelo apagam
            // 3 s (sem tatica).
            brok.Suprema = new Dictionary<string, float>
            {
                { "duracao", 12f }, { "raio", 6f }, { "escudo_regen", 5f }, { "runa", 25f },
                { "vida", 150f }, { "raio_corpo", 0.9f }, { "clang", 1.5f }, { "runas_apagadas", 3f },
            };

            KitDef gromm = m["14-gromm"];
            gromm.Implementado = true;
            gromm.TaticaCd = 7f;          // cura: utilidade
            gromm.SupremaCarga = 45f;
            gromm.Telegrafia = 2.5f;      // ajoelha, tambores, o chao treme numa linha
            // Sangue da Estepe: curar um ALIADO devolve 30% do curado ao Gromm.
            gromm.Passiva = new Dictionary<string, float>
            {
                { "cura_propria", 0.3f },
            };
            // Totem das Chuvas: 6 hp/s por 8 s no raio; a chuva APAGA fogo (§14). O PRECO: o totem tem 80 de vida (o alvo
            // obvio), a chuva nao pergunta o time (apaga o fogo tatico aliado, e agua no chao faz lama para todos).
            gromm.Tatica = new Dictionary<string, float>
            {
                { "cura", 6f }, { "duracao", 8f }, { "raio", 4.5f }, { "distancia", 1.5f },
                { "vida", 80f }, { "raio_corpo", 0.7f }, { "apaga_cada", 2f },
            };
            // Espirito do Trovao: bisao de 30 m em linha, empurra inimigos, derruba muro de pedra, rastro que acelera. O PRECO:
            // avisa 2,5 s antes, dano irrelevante (desloca, nao mata), e no fim o Gromm levanta com o peso dos anos (2 s lento).
            gromm.Suprema = new Dictionary<string, float>
            {
                { "comprimento", 30f }, { "velocidade", 16f }, { "largura", 2f }, { "empurrao", 11f }, { "dano", 5f },
                { "muro_golpes", 3f }, { "rastro_dur", 5f }, { "rastro_largura", 1.5f }, { "rastro_vel", 1.3f },
                { "cansado_dur", 2f }, { "cansado_vel", 0.7f },
            };

            KitDef maris = m["15-maris"];
            maris.Implementado = true;
            maris.TaticaCd = 8f;          // controle: meio da regua
            maris.SupremaCarga = 45f;
            maris.Telegrafia = 2f;        // a agua da area RECUA primeiro
            // Mare Viva: sobre agua (lago, poca/lama, a propria Mare Cheia) regenera 3 hp/s.
            maris.Passiva = new Dictionary<string, float>
            {
                { "regen", 3f },
            };
            // Onda Prisao: esfera LENTA (9 m/s, visivel, esquivavel) que ergue coluna d'agua: 1,2 s suspenso (ele ainda
            // conjura) e molhado. O PRECO: a esquiva fura (i-frames) e a suspensao nao silencia.
            maris.Tatica = new Dictionary<string, float>
            {
                { "velocidade", 9f }, { "alcance", 24f }, { "altura", 1.2f }, { "raio_toque", 1f }, { "suspensao", 1.2f },
            };
            // Mare Cheia: 9 m de agua por 10 s: aliados deslizam (+20%), inimigos afundam (-20%), TODOS molhados. O PRECO:
            // a area CONDUZ raio (qualquer raio dentro eletrifica tudo — ela inclusive, §14) e no fim a roupa encharcada
            // deixa 2 s lenta.
            maris.Suprema = new Dictionary<string, float>
            {
                { "raio", 9f }, { "duracao", 10f }, { "buff_vel", 1.2f }, { "lentidao", 0.8f },
                { "encharcada_dur", 2f }, { "encharcada_vel", 0.8f },
            };
        }
    }
}
