using UnityEngine;
using Arkana.Characters;
using Arkana.World;

namespace Arkana.Menu
{
    /// <summary>
    /// O FUNDO VIVO do menu: o mago escolhido em guarda no topo do PICO (neve, torres escuras, o por do sol atras) e a
    /// camera do menu orbitando devagar em volta dele. Mora na camera do menu; o Main liga/desliga junto com ela.
    /// O mago e' so' o Visual (Mago, nunca Pawn) e SOME no OnDisable: o treino nasce justamente no pico.
    /// Trocou o mago no Elenco = mago novo no pico. Sem ilha, nao faz nada (fiacao defensiva, sem log).
    /// </summary>
    public sealed class VitrineDoMenu : MonoBehaviour
    {
        // KNOB: o enquadramento inteiro, por foto (a referencia e' a 03-mago-de-perto: camera a ~4-5 m, um pouco acima)
        public const float Raio = 4.8f, Altura = 1.7f, Velocidade = 0.06f;   // m do mago, m acima do pe', rad/s (~105 s por volta)
        /// <summary>O olhar mira ~1 m acima do pe' e 1,3 m a ESQUERDA da tela: o mago cai no terco direito (a UI e' centrada).</summary>
        public const float OlharY = 1f, OlharLado = 1.3f;
        /// <summary>A escolha mora no PlayerPrefs (no Android, JNI): relida 2x por segundo, nao todo quadro.</summary>
        const float ReleituraS = 0.5f;

        Mago _mago;
        string _slug;
        float _ang = 0.6f;   // comeca no angulo da foto 03 (camera a +x,+z do mago)
        float _relogio;

        void LateUpdate()
        {
            Ilha ilha = Ilha.Atual;
            if (ilha == null || ilha.Relevo == null) return;
            _relogio -= Time.unscaledDeltaTime;
            if (_mago == null || _relogio <= 0f)
            {
                _relogio = ReleituraS;
                string slug = SelecaoPersonagem.MagoEscolhido;
                if (_mago == null || slug != _slug)
                {
                    Soltar();
                    _slug = slug;
                    _mago = Mago.Criar(null, slug);   // ja' nasce no idle de combate
                }
            }
            Vector2 pk = ilha.Relevo.Pico;
            var pe = new Vector3(pk.x, Ilha.AlturaDoChao(pk.x, pk.y), pk.y);
            _ang += Velocidade * Time.unscaledDeltaTime;   // sem escala: a pausa da HUD nao congela o menu
            var volta = new Vector3(Mathf.Sin(_ang), 0f, Mathf.Cos(_ang));   // mago -> camera, no plano
            Vector3 cam = pe + volta * Raio;
            cam.y = Mathf.Max(pe.y + Altura, Ilha.AlturaDoChao(cam.x, cam.z) + 1f);   // a crista do pico nao engole a camera
            Vector3 esquerda = Vector3.Cross(-volta, Vector3.up);   // a esquerda de quem olha o mago
            transform.position = cam;
            transform.LookAt(pe + Vector3.up * OlharY + esquerda * OlharLado);
            _mago.transform.SetPositionAndRotation(pe, Quaternion.LookRotation(volta));   // de frente para a camera (+Z do modelo)
        }

        void OnDisable() { Soltar(); }

        void OnDestroy() { Soltar(); }

        /// <summary>SetActive(false) ANTES do Destroy: o Destroy so' vale no fim do quadro, e a partida monta o jogador no
        /// pico NESTE quadro — o mago da vitrine nao pode aparecer (nem na primeira foto) da partida.</summary>
        void Soltar()
        {
            if (_mago != null) { _mago.gameObject.SetActive(false); Destroy(_mago.gameObject); }
            _mago = null;
            _slug = null;
        }
    }
}
