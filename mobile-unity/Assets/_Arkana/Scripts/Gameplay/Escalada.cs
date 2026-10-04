using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// SUBIR EM OBSTACULO (04/10/2026, pedido do Diretor; prompt mestre §10 "auto-mantle contextual"): obstaculo BAIXO
    /// (Balance.Move.EscaladaMin..EscaladaVault) o mago pula por cima apoiando a mao — correndo de encontro a ele ou tocando
    /// o SALTO; BEIRADA (ate' EscaladaMax) so' com o SALTO, de frente, e ele escala. So' se o corpo CABE em cima (sem teto).
    /// O caminho SOBE na frente da face e so' depois AVANCA por cima do topo: nunca cruza a parede. Puro: a casca (Pawn)
    /// sonda o mundo (Sondar) e move o corpo pelo Ponto desta classe.
    /// </summary>
    public sealed class Escalada
    {
        public enum Tipo { Nada, Vault, Subir }

        public Tipo Atual { get; private set; } = Tipo.Nada;
        public bool Ativa => Atual != Tipo.Nada;
        /// <summary>A direcao em que o corpo segue ao terminar (o embalo do vault).</summary>
        public Vector3 Frente { get; private set; }

        Vector3 _de, _alto, _fim;
        float _t, _dur;

        /// <summary>O que fazer diante de um obstaculo de `altura` m (do pe' ao topo), com o corpo cabendo la' em cima ou nao.</summary>
        public static Tipo Decidir(float altura, bool cabe, bool pediuPulo, bool correndo)
        {
            if (!cabe || float.IsNaN(altura) || altura < Balance.Move.EscaladaMin || altura > Balance.Move.EscaladaMax) return Tipo.Nada;
            if (altura <= Balance.Move.EscaladaVault) return pediuPulo || correndo ? Tipo.Vault : Tipo.Nada;
            return pediuPulo ? Tipo.Subir : Tipo.Nada;
        }

        /// <summary>Comeca no pe' `de`, sobe ate' a altura do `topo` e termina em `fim` (em cima, alem da borda).</summary>
        public void Iniciar(Tipo tipo, Vector3 de, Vector3 topo, Vector3 fim)
        {
            if (tipo == Tipo.Nada) return;
            Atual = tipo;
            _de = de;
            _alto = new Vector3(de.x, topo.y + 0.02f, de.z);   // em pe' na frente da face, a 2 cm acima do topo
            _fim = new Vector3(fim.x, topo.y + 0.02f, fim.z);
            Vector3 f = fim - de; f.y = 0f;
            Frente = f.sqrMagnitude > 1e-6f ? f.normalized : Vector3.forward;
            _t = 0f;
            _dur = tipo == Tipo.Vault ? Balance.Move.EscaladaVaultS : Balance.Move.EscaladaSubirS;
        }

        /// <summary>Avanca o relogio e devolve onde o pe' esta'. Sobe na 1a parte (SubidaFrac), avanca por cima na 2a.</summary>
        public Vector3 Tick(float dt)
        {
            if (!Ativa) return _fim;
            _t += Mathf.Max(dt, 0f);
            Vector3 p = Ponto(_t / _dur);
            if (_t >= _dur) Atual = Tipo.Nada;
            return p;
        }

        /// <summary>O caminho em `u` (0..1): subida com saida suave ate' o alto, depois o avanco por cima do topo.</summary>
        public Vector3 Ponto(float u)
        {
            u = Mathf.Clamp01(float.IsNaN(u) ? 1f : u);
            const float sobe = SubidaFrac;
            if (u <= sobe)
            {
                float k = u / sobe;
                return Vector3.Lerp(_de, _alto, 1f - (1f - k) * (1f - k));
            }
            return Vector3.Lerp(_alto, _fim, (u - sobe) / (1f - sobe));
        }

        /// <summary>Fracao do tempo gasta subindo (o resto e' o avanco por cima da borda).</summary>
        public const float SubidaFrac = 0.55f;

        public void Cancelar() { Atual = Tipo.Nada; }

        // ------------------------------------------------------------------ a sonda (fisica; a casca chama)

        static readonly RaycastHit[] _hits = new RaycastHit[16];
        static readonly Collider[] _perto = new Collider[16];

        /// <summary>
        /// O obstaculo a frente do pe' `pe` na direcao `frente`: a face (na altura do joelho, ate' `raio` + EscaladaAlcance),
        /// o topo logo depois dela (raio de cima para baixo) e se uma capsula do corpo (`raio`, `alturaCorpo`) cabe em cima
        /// E no caminho de subida (sem teto). Corpos de mago (CharacterController) nao contam. False = nada escalavel.
        /// </summary>
        public static bool Sondar(Vector3 pe, Vector3 frente, float raio, float alturaCorpo, out float altura, out Vector3 topo, out Vector3 fim)
        {
            altura = float.NaN; topo = pe; fim = pe;
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-6f) return false;
            frente.Normalize();
            if (!Solido(pe + Vector3.up * Balance.Move.EscaladaMin * 0.6f, frente, raio + Balance.Move.EscaladaAlcance, out RaycastHit face)) return false;
            if (Mathf.Abs(face.normal.y) > 0.5f) return false;   // rampa ou telhado, nao face
            Vector3 alem = face.point + frente * (raio * 0.6f + 0.05f);
            float teto = Balance.Move.EscaladaMax + 0.3f;
            alem.y = pe.y + teto;
            if (!Solido(alem, Vector3.down, teto, out RaycastHit cima)) return false;
            altura = cima.point.y - pe.y;
            topo = cima.point;
            fim = cima.point + frente * (raio + 0.15f);
            float r = raio * 0.9f;
            // cabe em cima (sem teto) e o caminho de SUBIDA, na frente da face, tambem esta' livre ate' a altura do topo + corpo
            bool cabe = !Bloqueado(fim + Vector3.up * (r + 0.05f), fim + Vector3.up * (alturaCorpo - r), r)
                && !Bloqueado(pe + Vector3.up * (alturaCorpo - r), pe + Vector3.up * (altura + alturaCorpo - r), r);
            return cabe;
        }

        static bool Solido(Vector3 de, Vector3 dir, float max, out RaycastHit melhor)
        {
            melhor = default;
            int n = Physics.RaycastNonAlloc(de, dir, _hits, max, ~0, QueryTriggerInteraction.Ignore);
            float d = float.PositiveInfinity;
            for (int i = 0; i < n; i++)
                if (!(_hits[i].collider is CharacterController) && _hits[i].distance < d) { d = _hits[i].distance; melhor = _hits[i]; }
            return d < float.PositiveInfinity;
        }

        static bool Bloqueado(Vector3 a, Vector3 b, float r)
        {
            int n = Physics.OverlapCapsuleNonAlloc(a, b, r, _perto, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (!(_perto[i] is CharacterController)) return true;
            return false;
        }
    }
}
