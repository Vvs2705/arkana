# Servidores de jogo — Godot dedicated server (G5)

> Fase que ativa: **G5** (`docs/ROADMAP_3D.md`) — "2 telefones na mesma
> partida". Antes disso, nada aqui se contrata.

## 1. Como funciona o servidor dedicado do Godot

É o **mesmo projeto** de `godot/`, exportado para **Linux** com o modo
**Dedicated Server** e rodado com `--headless`:

- No editor: preset de export "Linux", *Export Mode → Dedicated Server*. Isso
  marca o build com a feature `dedicated_server` e remove/esvazia recursos
  visuais e de áudio (texturas, meshes viram placeholder) — o binário não
  renderiza nada, só simula.
- No código: `OS.has_feature("dedicated_server")` separa caminho de servidor e
  de cliente **no mesmo script**. Uma base de código, dois artefatos — é
  exatamente o que o ROADMAP G5 pede.
- Rede: **high-level multiplayer** do Godot — `ENetMultiplayerPeer` (UDP),
  RPCs, `MultiplayerSpawner`/`MultiplayerSynchronizer`. O servidor cria
  `create_server(porta)`; cada cliente, `create_client(ip, porta)`.
- **Servidor autoritativo** — princípio que o projeto já provou no Roblox e
  que o `docs/ANDROID.md` §2 lista como conhecimento que atravessa a ponte:
  cliente envia intenção (input), servidor simula e responde estado. Detalhe
  de anti-cheat em `02-backend-servicos.md`.
- Modelo operacional mais simples que existe: **1 processo = 1 partida = 1
  porta UDP**. N partidas = N processos (um `systemd` template tipo
  `arkana@5000.service`, `arkana@5001.service`… resolve). Orquestração dentro
  do processo (várias partidas num processo só) é otimização para depois, se
  um dia a conta de RAM apertar.

O export Linux a partir do Windows funciona normalmente — os templates 4.4.1
já instalados incluem Linux. Detalhe do preset em `03-pipeline-build.md`.

## 2. Quanto cabe por vCPU — ordens de grandeza honestas

Ninguém pode prometer número sem medir **este** jogo. O que dá para sustentar
como ordem de grandeza, para uma partida do escopo do GDD §19.2 (**20 slots**,
física simples, terreno reativo por células, tick de simulação 20–30 Hz):

| Recurso | Ordem de grandeza | Ressalva |
|---|---|---|
| CPU | **1 partida de 20 slots por vCPU é o chute conservador**; 2–4 partidas/vCPU é plausível com tick 20 Hz e física contida | Medir no G5 é parte da entrega da fase. Bots contam como CPU (IA roda no servidor), não como rede |
| RAM | ~100–300 MB por processo headless (sem texturas) | Depende do quanto o mundo procedural aloca; medir |
| Banda | ~10–30 kbps por jogador em cada sentido com sincronização decente; uma partida cheia de 20 humanos ≈ poucos Mbps | O primeiro teste G5 são 2 telefones + bots — banda irrelevante |
| Disco | binário + pck: dezenas de MB; logs | Qualquer VPS serve |

Tradução prática: **uma VPS de 2 vCPU / 2–4 GB roda com folga as partidas de
teste do G5** (2 humanos + bots, poucas salas simultâneas). O dia em que isso
não bastar é um dia BOM — significa jogadores.

## 3. Latência: São Paulo não é preferência, é requisito

Arkana é jogo de tiro/esquiva com TTK curto. Latência é jogabilidade:

| De onde o jogador BR conecta | Ping típico | Veredito |
|---|---|---|
| Servidor em **São Paulo** | ~5–60 ms (dentro do país) | o alvo |
| Servidor na costa leste dos EUA | ~110–150 ms | ruim para tiro |
| Servidor na Europa | ~180–230 ms | inviável |

Consequência dura: **as VPS mais baratas do mundo ficam de fora**. Hetzner
(Alemanha/Finlândia/EUA/Singapura) e Contabo (EUA/EU/Ásia) **não têm região no
Brasil** hoje — verificar vigente, mas enquanto não tiverem, o preço imbatível
delas não compra o ping. DigitalOcean idem (região mais próxima: Nova York).

## 4. As rotas de hospedagem, comparadas para um dev solo BR

Preços de 20/08/2026, **verificar preço vigente** em todos — e câmbio do dia
(estimativas em R$ usam ~R$ 5,50/US$, verificar).

### Rota A — VPS simples em São Paulo ← **a recomendada para G5**

| Provedor | Região SP? | Spec de entrada útil | Preço hoje |
|---|---|---|---|
| **Oracle Cloud (Always Free)** | **Sim** (região São Paulo) | até 4 vCPU ARM + 24 GB RAM, no free tier | **R$ 0** — verificar vigente; exige cartão no cadastro, capacidade ARM em SP às vezes esgota, e conta free pode ser recuperada por inatividade. Godot 4.4 tem template **linux arm64** oficial, então ARM serve |
| **Vultr** | **Sim** | 1 vCPU / 1–2 GB (Cloud Compute) | ~US$ 6–12/mês — verificar vigente |
| **Hostinger VPS** | **Sim** (datacenter SP) | 1–2 vCPU / 4 GB | ~R$ 25–40/mês em promoção, renovação mais cara — verificar vigente |
| **Magalu Cloud** (nuvem BR) | **Sim** | VM pequena | faixa de dezenas de R$/mês — verificar vigente (produto novo, preço instável) |
| **GCP** (`southamerica-east1`) | **Sim** | e2-small (2 vCPU compartilhada / 2 GB) | ~US$ 15–20/mês + egress — verificar vigente |
| **AWS** (`sa-east-1`) | **Sim** | t3.small (2 vCPU / 2 GB) | ~US$ 25/mês + egress ~US$ 0,15/GB — verificar vigente. SP na AWS é das regiões mais caras do mundo; egress de jogo em AWS morde |
| Hetzner / Contabo / DigitalOcean | **Não** | (as mais baratas do mundo em specs) | €4–7/mês — irrelevante sem região BR (§3) |

**Leitura:** Vultr SP na faixa de US$ 6–12/mês é o piso comercial confiável;
Oracle free tier é o experimento de custo zero que vale tentar primeiro
(se a capacidade ARM em SP estiver disponível). Hyperscaler (AWS/GCP/Azure)
só se o Diretor já tiver crédito ou familiaridade — para 1 servidor de jogo,
paga-se o dobro pelo mesmo.

### Rota B — bare metal

Máquina física dedicada (ex.: **Latitude.sh**, empresa BR com bare metal em
SP, a partir de ~US$ 100+/mês — verificar vigente). Faz sentido quando dezenas
de partidas simultâneas constantes tornam o custo por vCPU da VPS pior que o
da máquina inteira. **Não é agora.** Ordem de grandeza do ponto de virada:
quando a conta mensal de VPS passar de ~R$ 500–1.000 constantes, cotar bare
metal.

### Rota C — orquestrador de game server (Agones/Kubernetes, ou gerenciados)

Agones (k8s) e serviços gerenciados de fleet resolvem: subir/derrubar
servidores por demanda, alocar partida em frota, autoscaling multirregião.
**Overkill agora em todas as dimensões**: exige operar um cluster k8s (custo
fixo maior que o servidor de jogo em si) e resolve um problema — escala — que
o projeto não tem.

**Quando deixaria de ser overkill** (qualquer um destes):
- partidas simultâneas constantes na casa das **dezenas para centenas**, com
  pico e vale fortes (aí pagar máquina 24h dói e autoscaling paga o custo);
- mais de uma região servida ao mesmo tempo;
- mais de uma pessoa operando infra.

Até lá, `systemd` + 1 VPS é a engenharia certa, não a versão pobre.

## 5. Recomendação clara para o PRIMEIRO teste multiplayer (G5)

**1 VPS barata em São Paulo — Vultr ~US$ 6–12/mês (verificar preço vigente),
ou Oracle free tier ARM se houver capacidade — rodando N processos do export
headless, um por partida de teste.**

Sequência concreta da fase, em ordem:

1. **Custo zero primeiro:** cliente e servidor na MESMA rede local (o export
   headless rodando no próprio PC do Diretor via WSL2 — ver
   `06-ferramentas-locais.md` — e os 2 telefones no Wi-Fi de casa). Prova o
   netcode sem contratar nada.
2. **Depois a VPS SP** (ato do Diretor): sobe o mesmo binário, abre a porta
   UDP no firewall, e os telefones conectam por IP público via 4G/Wi-Fi.
   É isso que prova latência real.
3. Sem domínio, sem load balancer, sem TLS nesta fase — IP e porta bastam
   para teste. Domínio entra em `04-distribuicao.md` quando houver público.

O que fica para decisão futura do Diretor:
- **Qual provedor contratar** (esta pasta compara; contratar é ato dele).
- **Free tier Oracle como base de produção** — bom para teste; para produção,
  decidir se a fragilidade (conta recuperável, capacidade) é aceitável.
- **Quando sair de 1 VPS** — o gatilho está na Rota B/C acima.
