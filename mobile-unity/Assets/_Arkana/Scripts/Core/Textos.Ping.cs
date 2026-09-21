namespace Arkana.Core
{
    /// <summary>TEXTOS do PING DE SINTONIA (GDD §18.7, onda 18D): a conversa da dupla na faixa da Sintonia.</summary>
    public static partial class Textos
    {
        // ---------------------------------------------------------------- ping de sintonia (GDD §18.7)
        /// <summary>O pedido: o jogador tocou no anel da Sintonia pronto.</summary>
        public const string PingCombo = "COMBO?";
        /// <summary>O parceiro topou; embaixo, o combo que VAI sair (PingVai).</summary>
        public const string PingAceito = "PARCEIRO: ACEITO";
        public const string PingVai = "→ {0}";
        /// <summary>O parceiro so' tem o mesmo elemento (ou esta' sem luva): recusa sem fingir.</summary>
        public const string PingSemCombo = "SEM COMBO";
        /// <summary>Tocou no anel sem inimigo na mira nem acertado: o que fazer para o pedido sair.</summary>
        public const string PingSemAlvo = "MIRE NUM INIMIGO";
    }
}
