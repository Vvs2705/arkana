using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O CORPO EM MOVIMENTO, puro (porte de Pawn.gd: move_velocity/accel_step/dash_speed_at/apply_gravity/pular).
    /// Recebe a INTENCAO ja' moldada (o joystick virtual aplica deadzone e curva: JoystickLogica e' a fonte unica;
    /// aqui NAO se reaplica, senao sobra degrau) e devolve o DESLOCAMENTO do frame — quem move o corpo e' a casca.
    /// A velocidade maxima chega de fora, SEMPRE por Velocidade.Produto (ver Speed) — ninguem escreve m/s.
    /// A mana da flutuacao e' cobrada AQUI (por ref): a regra "sem mana a magia larga o corpo" e' testavel sem cena.
    /// </summary>
    public sealed class Locomocao
    {
        /// <summary>m/s^2 de decaimento do empurrao — KNOB local (pedir entrada em Balance se sobreviver ao playtest).</summary>
        public const float KNOCK_DECAY = 9f;
        /// <summary>Descida (m/s) a partir da qual o corpo assume a pose de QUEDA (parado sem chao NAO e' cair).</summary>
        public const float QUEDA_ANIM_V = 1.5f;
        public const float GRAVIDADE = 9.81f;
        /// <summary>Vy no chao: o CharacterController precisa de um empurrao para baixo para manter isGrounded.</summary>
        public const float COLA_CHAO = -1f;
        /// <summary>m/s MEDIOS do dash de kit (mordida, zigue-zague) — KNOB local: 6 m em 0,4 s, metade da esquiva (27,8 m/s),
        /// que ainda se ve' VIAJAR (GDD §4.1). AUMENTAR = investida que nasce no alvo; DIMINUIR = volta a parecer corrida.</summary>
        public const float VEL_IMPULSO = 15f;
        /// <summary>s descendo rapido (Vy &lt; -QUEDA_ANIM_V) sem chao para a pose virar QUEDA — degrau nao e' queda. Num PULO,
        /// o ar alem do voo previsto (caiu do barranco). KNOB local.</summary>
        public const float QUEDA_LONGA_S = 0.35f;
        /// <summary>s da aterrissagem PARADA depois de um pulo ou queda longa (o Mago toca o pouso do take nesse tempo:
        /// Balance.Anim.PousoVel). Correndo, a passada entra direto.</summary>
        public const float POUSO_S = 0.5f;

        /// <summary>Velocidade horizontal INTENCIONAL (sem knockback).</summary>
        public Vector3 VelH;
        public float Vy;
        public Vector3 Knockback;
        /// <summary>A velocidade REAL aplicada no ultimo Tick (o que a animacao e o banking leem).</summary>
        public Vector3 Vel;
        public float DodgeLeft, DodgeCd, IframesLeft;
        /// <summary>s que faltam do dash de kit (Impulso). > 0 = o corpo segue a rota, o stick nao soma.</summary>
        public float ImpulsoLeft;
        public float FlutuaS;
        public bool Flutuando;
        /// <summary>Histerese idle/run (RunAnimEnter/Exit).</summary>
        public bool RunAnim;
        /// <summary>Inclinacao do visual (rad) pra dentro da curva.</summary>
        public float Bank;
        /// <summary>Ultima velocidade maxima recebida (frac de velocidade para giro e banking).</summary>
        public float VelMax = Balance.Player.Speed;
        /// <summary>No ar por um PULO: a pose e' o salto, nao a queda (AtualizarAr zera no chao).</summary>
        public bool Pulando;
        /// <summary>s sem chao; s descendo rapido; s de voo previstos na decolagem (2*Vy/g); s que faltam da aterrissagem.</summary>
        public float NoArS, DescendoS, VooS, PousoS;
        /// <summary>s que um SALTO tocado no ar ainda espera o chao (jump buffer, Balance.Move.PuloGuardadoS). A casca pula ao pousar.</summary>
        public float PuloGuardadoS;
        /// <summary>O atordoamento do ultimo Tick: o corpo nao obedece — nem anda, nem esquiva, nem pula (o tiro ja' barrava).</summary>
        public bool Atordoado { get; private set; }

        /// <summary>A cola no chao acompanha descida de ate' InclinacaoMax (a -1 m/s fixa so' segurava ~7,6 graus correndo).</summary>
        static readonly float TanInclinacao = Mathf.Tan(Balance.Move.InclinacaoMax * Mathf.Deg2Rad);

        private Vector3 _dodgeDir, _impDir;
        /// <summary>s desde o ultimo quadro com chao (coyote). Nasce "nunca pisou": corpo novo no ar nao ganha pulo.</summary>
        private float _semChaoS = float.PositiveInfinity;
        private float _impM, _impDur;
        private float _prevYaw;
        private bool _yawInit;

        public float VelocidadeHorizontal => new Vector2(Vel.x, Vel.z).magnitude;
        public bool Esquivando => DodgeLeft > 0f;
        /// <summary>Para onde a ultima esquiva saiu (XZ normalizada; zero antes da primeira).</summary>
        public Vector3 DirEsquiva => _dodgeDir;
        public bool DodgePronto => DodgeCd <= 0f;
        /// <summary>1 = acabou de usar, 0 = pronta. A UI so' LE.</summary>
        public float DodgeCdFrac => DodgeCd / Balance.Dodge.Cooldown;
        /// <summary>Flutuar e' UMA vez por salto: o chao devolve o saldo.</summary>
        public float FlutuaRestante => Mathf.Max(Balance.Flutuar.DurMax - FlutuaS, 0f);
        /// <summary>0..1 de quanto o corpo esta' rapido em relacao ao maximo (pivo e banking).</summary>
        public float Frac => Mathf.Clamp01(new Vector2(VelH.x, VelH.z).magnitude / Mathf.Max(VelMax, 0.01f));

        /// <summary>O produto unico. Quem chama junta terreno (agua), status (lentidao) e postura (derrubado).</summary>
        public static float Speed(float terreno, float status, float postura) =>
            Velocidade.Produto(Balance.Player.Speed, terreno, status, postura);

        /// <summary>Stick (x direita, y frente; magnitude 0..1 ja' moldada) -> direcao no mundo pelo yaw da camera (rad).</summary>
        public static Vector3 DirDoStick(Vector2 stick, float yawCam)
        {
            // um NaN do toque envenenava a VelH para sempre (AccelStep persegue o alvo NaN)
            if (float.IsNaN(stick.x) || float.IsNaN(stick.y) || float.IsNaN(yawCam)) return Vector3.zero;
            var frente = new Vector3(Mathf.Sin(yawCam), 0f, Mathf.Cos(yawCam));
            var direita = new Vector3(Mathf.Cos(yawCam), 0f, -Mathf.Sin(yawCam));
            Vector3 d = direita * stick.x + frente * stick.y;
            return d.sqrMagnitude > 1f ? d.normalized : d;
        }

        /// <summary>Um passo de aceleracao horizontal: a velocidade PERSEGUE o alvo. Soltou = freia; inverteu = strafe seco; no ar = menos.</summary>
        public static Vector3 AccelStep(Vector3 cur, Vector3 target, float dt, bool noChao)
        {
            float rate = Balance.Move.Accel;
            if (target.sqrMagnitude < 0.0001f) rate = Balance.Move.Brake;
            else if (Vector3.Dot(cur, target) < 0f) rate = Balance.Move.TurnAccel;
            if (!noChao) rate *= Balance.Move.AirControl;
            return Vector3.MoveTowards(cur, target, rate * dt);
        }

        /// <summary>Rampa linear de burst*media ate' (2-burst)*media: a MEDIA continua distance/duration, so' o arranque muda.</summary>
        public static float DashSpeedAt(float p) => Rampa(Balance.Dodge.Distance, Balance.Dodge.Duration, p);

        /// <summary>A rampa de TODO dash (esquiva e kit): velocidade no progresso `p` (0..1) de `metros` em `dur` s.</summary>
        public static float Rampa(float metros, float dur, float p) =>
            metros / dur * Mathf.Lerp(Balance.Dodge.Burst, 2f - Balance.Dodge.Burst, Mathf.Clamp01(p));

        /// <summary>Metros andados `t` s depois do arranque de um dash de `metros` em `dur` s — a integral da Rampa. E' a conta
        /// que a ROTA de um kit (ApoioGrupoD.Investida) usa: a logica e o corpo andam juntos.</summary>
        public static float PercorridoNoDash(float metros, float dur, float t)
        {
            if (!(dur > 0f)) return metros;
            t = Mathf.Clamp(t, 0f, dur);
            float b = Balance.Dodge.Burst;
            return metros / dur * (b * t + (1f - b) * t * t / dur);
        }

        /// <summary>Quanto dura um dash de kit de `metros` (a VEL_IMPULSO).</summary>
        public static float DuracaoDoImpulso(float metros) => Mathf.Max(metros, 0f) / VEL_IMPULSO;

        /// <summary>Dash em `dir` (XZ) com i-frames. Quem chama resolve o "parado = para onde olha". O ultimo verbo manda:
        /// esquivar sai do dash de kit.</summary>
        public bool Dodge(Vector3 dir)
        {
            if (!DodgePronto || Atordoado) return false;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return false;
            _dodgeDir = dir.normalized;
            DodgeLeft = Balance.Dodge.Duration;
            DodgeCd = Balance.Dodge.Cooldown;
            IframesLeft = Balance.Dodge.Iframes;
            ImpulsoLeft = 0f;
            return true;
        }

        /// <summary>
        /// DASH DE KIT: `metros` em `dur` s na direcao (XZ), na MESMA rampa da esquiva, e para — sem a cauda do empurrao (o
        /// Knockback decai a 9 m/s^2: 6 m levavam 1,15 s deslizando). Sem i-frames e sem cooldown (o kit paga o dele). O
        /// stick nao soma por cima: o corpo anda a rota que o kit telegrafou. O ultimo verbo manda: corta a esquiva.
        /// </summary>
        public bool Impulso(Vector3 dir, float metros, float dur)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-6f || !(metros > 0f) || !(dur > 0f)) return false;
            _impDir = dir.normalized;
            _impM = metros;
            _impDur = dur;
            ImpulsoLeft = dur;
            DodgeLeft = 0f;
            return true;
        }

        /// <summary>O corpo mudou de lugar num quadro (teleporte): dash, empurrao e velocidade vertical nao atravessam com
        /// ele — nem o pulo (sem aterrissagem no destino). A corrida (VelH) e os i-frames ficam.</summary>
        public void Parar()
        {
            DodgeLeft = 0f; ImpulsoLeft = 0f;
            Knockback = Vector3.zero;
            Vy = 0f;
            Pulando = false; NoArS = 0f; DescendoS = 0f;
        }

        /// <summary>SALTO ARCANO: do chao, ou ate' CoyoteS depois de sair da borda ANDANDO (nunca depois de um pulo: sem pulo
        /// duplo); nunca nadando nem atordoado. No ar, o toque fica GUARDADO PuloGuardadoS e a casca pula ao pousar.
        /// `fatorAltura` = quantas vezes mais ALTO (h = v^2/2g, logo a velocidade sobe pela raiz): a mola do Fizz e' 1,5.</summary>
        public bool Pular(bool noChao, bool nadando, float fatorAltura = 1f)
        {
            if (nadando || Atordoado) return false;
            bool coyote = !noChao && !Pulando && Vy <= 0f && _semChaoS <= Balance.Move.CoyoteS;
            if (!noChao && !coyote) { PuloGuardadoS = Balance.Move.PuloGuardadoS; return false; }
            Vy = Balance.Player.JumpV * Mathf.Sqrt(Mathf.Max(fatorAltura, 0f));
            Pulando = true;
            VooS = TempoDeVoo(Vy);
            NoArS = 0f; DescendoS = 0f; PousoS = 0f; PuloGuardadoS = 0f;
            return true;
        }

        /// <summary>s de ar de um pulo que sai do chao com `vy` e volta a mesma altura.</summary>
        public static float TempoDeVoo(float vy) => vy > 0f ? 2f * vy / GRAVIDADE : 0f;

        /// <summary>
        /// Os relogios do ar, DEPOIS do Move, com o chao deste quadro. Tocar o chao depois de um pulo (ou de uma queda longa)
        /// arma a aterrissagem. O quadro da decolagem nao conta: o isGrounded ainda pode ser o de antes.
        /// </summary>
        public void AtualizarAr(float dt, bool noChao, bool nadando)
        {
            if (dt <= 0f) return;
            PousoS = Mathf.Max(PousoS - dt, 0f);
            _semChaoS = noChao || nadando ? 0f : _semChaoS + dt;
            if (!noChao && !nadando)
            {
                NoArS += dt;
                DescendoS = Vy < -QUEDA_ANIM_V ? DescendoS + dt : 0f;
                return;
            }
            if (Pulando && NoArS <= 0f && Vy > 0f && !nadando) return;
            if (!nadando && (Pulando || QuedaLonga)) PousoS = POUSO_S;
            Pulando = false; NoArS = 0f; DescendoS = 0f;
        }

        /// <summary>Queda de verdade: no pulo, o ar passou do voo previsto; sem pulo, descendo rapido ha' QUEDA_LONGA_S.</summary>
        public bool QuedaLonga => Pulando ? NoArS > VooS + QUEDA_LONGA_S : DescendoS > QUEDA_LONGA_S;

        /// <summary>A pose do corpo sem chao: planar, queda longa, o salto — ou null (degrau: segue a passada).</summary>
        public string AnimNoAr()
        {
            if (Flutuando) return "planar";
            if (QuedaLonga) return "cair";
            return Pulando ? "pular" : null;
        }

        /// <summary>
        /// RECUAR MIRANDO e' mais lento (Balance.Move.BackpedalMult): `graus` = da mira ate' a intencao de movimento. Rampa de
        /// RecuoGraus-RecuoRampa (1) a RecuoGraus+RecuoRampa (BackpedalMult) — sem degrau quando o stick passa pela diagonal.
        /// Entra no produto de velocidade como fator de postura.
        /// </summary>
        public static float FatorDeRecuo(float graus)
        {
            if (float.IsNaN(graus)) return 1f;
            float a = Mathf.Abs(Mathf.DeltaAngle(0f, graus));
            float u = Mathf.InverseLerp(Balance.Move.RecuoGraus - Balance.Move.RecuoRampa, Balance.Move.RecuoGraus + Balance.Move.RecuoRampa, a);
            return Mathf.Lerp(1f, Balance.Move.BackpedalMult, u);
        }

        public void Empurrar(Vector3 v) { Knockback += v; }

        /// <summary>
        /// Um passo. `dir` = intencao no mundo (0..1). Devolve o DESLOCAMENTO do frame (a casca faz Move).
        /// `mana` e' cobrada pela flutuacao (Balance.Flutuar.ManaPorS) enquanto `querFlutuar` e ha' saldo.
        /// </summary>
        public Vector3 Tick(float dt, Vector3 dir, bool noChao, bool nadando, float velMax, bool atordoado, bool querFlutuar, ref float mana)
        {
            if (dt <= 0f) return Vector3.zero;
            VelMax = velMax;
            DodgeCd = Mathf.Max(DodgeCd - dt, 0f);
            IframesLeft = Mathf.Max(IframesLeft - dt, 0f);
            PuloGuardadoS = Mathf.Max(PuloGuardadoS - dt, 0f);
            Knockback = Vector3.MoveTowards(Knockback, Vector3.zero, KNOCK_DECAY * dt);
            Atordoado = atordoado;
            if (atordoado) dir = Vector3.zero;   // o corpo nao obedece (teto Status.StunCap)
            dir.y = 0f;

            Vector3 h;
            if (DodgeLeft > 0f)
            {
                float dur = Balance.Dodge.Duration;
                // amostra no MEIO do passo: rampa linear -> a soma dos passos da' a distancia exata (sem isso fica ~5% curto).
                // O ultimo passo so' anda o que falta (como o Impulso): um quadro travado de 0,33 s nao corta a esquiva pela metade.
                float passo = Mathf.Min(dt, DodgeLeft);
                float p = Mathf.Clamp01((dur - DodgeLeft + passo * 0.5f) / dur);
                DodgeLeft -= passo;
                h = _dodgeDir * (DashSpeedAt(p) * passo / dt);
                // momentum: ao acabar o dash o corpo JA' esta' correndo pra la'
                VelH = _dodgeDir * velMax * Balance.Dodge.ExitMomentum;
            }
            else if (ImpulsoLeft > 0f)
            {
                // o ultimo passo so' anda o que falta: a soma da' `metros` exato (a rota do kit mede a mesma integral)
                float passo = Mathf.Min(dt, ImpulsoLeft);
                float p = (_impDur - ImpulsoLeft + passo * 0.5f) / _impDur;
                ImpulsoLeft -= passo;
                h = _impDir * (Rampa(_impM, _impDur, p) * passo / dt);
                VelH = _impDir * velMax * Balance.Dodge.ExitMomentum;
            }
            else
            {
                VelH = AccelStep(VelH, dir * velMax, dt, noChao);
                h = VelH + Knockback;
            }

            float vyPasso;   // a velocidade vertical MEDIA do passo: com a gravidade, a media e' exata (o pulo nao depende do FPS)
            if (nadando)
            {
                // a lamina segura o corpo: gravidade zero (quem persegue a superficie e' Agua.Flutuar)
                Vy = 0f; FlutuaS = 0f; Flutuando = false;
                vyPasso = 0f;
            }
            else if (noChao && Vy <= 0f)
            {
                FlutuaS = 0f; Flutuando = false;   // o chao devolve a flutuacao inteira
                // a cola acompanha a ladeira na velocidade de agora: a -1 m/s fixa descolava em descida acima de ~7,6 graus
                // correndo (pulo negado, freio x0,35, flutuar ligando). Saindo de uma borda, cai seco — o coyote perdoa o pulo.
                // So' o PASSO desce mais (Vy guardada segue -1): saindo da borda a queda comeca normal, e a esquiva (51 m/s)
                // nao vira mergulho — o teto e' a corrida.
                Vy = COLA_CHAO;
                vyPasso = Mathf.Min(COLA_CHAO, -Mathf.Min(new Vector2(h.x, h.z).magnitude, velMax) * TanInclinacao);
            }
            else
            {
                // so' na DESCIDA: segurando o SALTO desde o toque, o 2o quadro do pulo ja' flutuava e o corpo subia 8-16 cm
                // em vez de ~1,4 m (o botao fica "segurando" enquanto o dedo esta' na tela)
                bool quer = querFlutuar && !noChao && Vy <= 0f && FlutuaRestante > 0f && mana > 0f;
                if (quer)
                {
                    // FLUTUAR: descida lenta e constante, nunca sobe (voo livre desequilibra). Paga a mesma mana do tiro.
                    FlutuaS += dt;
                    mana = Mathf.Max(mana - Balance.Flutuar.ManaPorS * dt, 0f);
                    Vy = -Balance.Flutuar.DescV;
                    Flutuando = mana > 0f;   // mana no fim = a magia larga o corpo, na hora
                    vyPasso = Vy;
                }
                else
                {
                    Flutuando = false;
                    float vy0 = Vy;
                    Vy -= GRAVIDADE * dt;
                    vyPasso = (vy0 + Vy) * 0.5f;   // Euler puro dava topo 1,335 m a 60 FPS e 1,29 m a 30 FPS
                }
            }
            Vel = new Vector3(h.x, vyPasso, h.z);
            return Vel * dt;
        }

        /// <summary>
        /// O que o corpo ANDOU de verdade (a casca chama depois do CharacterController.Move): contra a parede a intencao segue
        /// cheia, mas a passada (AnimNoChao, cadencia, pernas, mira dos bots) le' a velocidade REAL — sem correr parado.
        /// So' o plano: a vertical segue a do Tick (a rede de seguranca do chao teleporta e nao e' velocidade).
        /// </summary>
        public void Real(Vector3 deslocamento, float dt)
        {
            if (!(dt > 0f) || float.IsNaN(deslocamento.x) || float.IsNaN(deslocamento.z)) return;
            Vel = new Vector3(deslocamento.x / dt, Vel.y, deslocamento.z / dt);
        }

        /// <summary>Banking: inclina pra dentro da curva, proporcional ao giro deste frame e a quanto esta' rapido.</summary>
        public void AtualizarBank(float yawRad, float dt)
        {
            if (dt <= 0f) return;
            if (!_yawInit) { _prevYaw = yawRad; _yawInit = true; }   // sem isso o 1o frame acusa um giro gigante
            float yawRate = Mathf.DeltaAngle(_prevYaw * Mathf.Rad2Deg, yawRad * Mathf.Rad2Deg) * Mathf.Deg2Rad / dt;
            _prevYaw = yawRad;
            float alvo = Mathf.Clamp(yawRate * Balance.Move.BankGain, -Balance.Move.BankMax, Balance.Move.BankMax) * Frac;
            Bank = Mathf.Lerp(Bank, alvo, Mathf.Min(Balance.Move.BankRate * dt, 1f));
        }

        /// <summary>"run"/"idle" pela velocidade REAL com histerese (um limiar so' pisca na fronteira).</summary>
        public string AnimNoChao()
        {
            float v = VelocidadeHorizontal;
            if (RunAnim) { if (v < Balance.Move.RunAnimExit) RunAnim = false; }
            else if (v > Balance.Move.RunAnimEnter) RunAnim = true;
            return RunAnim ? "run" : "idle";
        }
    }
}
