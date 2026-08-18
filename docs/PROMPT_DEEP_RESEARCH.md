# Prompt para ChatGPT Deep Research (copiar e colar)

> **Como usar:** anexe o `.rar` com o projeto inteiro e cole o texto abaixo.
> O dossiê está embutido no prompt de propósito — se o modelo não conseguir
> abrir o arquivo compactado, ele ainda tem contexto suficiente para produzir
> o relatório. Se abrir, o prompt manda ler o código de verdade.
>
> Arquivos mais importantes dentro do pacote: `docs/GDD.md` (fonte da verdade
> do produto), `docs/ROBLOX.md`, `docs/PROJETO_PRISMA.md`, `docs/EQUIPE.md`,
> `docs/HISTORICO.md`, `docs/COMO_JOGAR.md`, `CHANGELOG.md`, e o código em
> `roblox/src/` (versão atual) e `src/` (protótipo 2D anterior).

---

You are a senior game-industry analyst and technical director. Your client is the solo creative director of an in-development mage battle royale called **Arkana**, built with AI coding agents rather than a human studio. Produce a deeply researched, decision-ready strategy report. Prioritise verifiable evidence over opinion, and be blunt about what is not worth building.

## 0. Attached material — read it first

The complete project is attached as a compressed archive (`.rar`). Open it and read before researching. Priority order:

1. `docs/GDD.md` — the game design document; it is the product's source of truth.
2. `docs/ROBLOX.md` — the plan and validation questions for the current build.
3. `docs/PROJETO_PRISMA.md` — the visual/technical art strategy and the engine decision.
4. `CHANGELOG.md` — everything shipped so far, in order.
5. `docs/HISTORICO.md`, `docs/EQUIPE.md`, `docs/COMO_JOGAR.md` — technical history and known traps, the studio-role blueprint, and the current controls.
6. Source code: `roblox/src/` (current Roblox build, Luau) and `src/` (earlier 2D prototype, TypeScript). Read at least the shared contracts (`roblox/src/shared/`) and the server modules `Terrain.luau`, `Combat.luau`, `Sintonia.luau`, `Match.luau` — the real numbers live in `roblox/src/shared/Balance.luau`.

Where the documents and the code disagree, **the code is the truth about what exists** and the GDD is the truth about what is intended. Point out any contradiction you find — that is itself a valuable finding.

If you cannot open the archive, say so explicitly at the top of your report and proceed using the dossier below.

## 1. Project dossier (summary — verify against the attached files)

**Concept.** Battle royale of mages. Positioning: proven BR skeleton plus two proprietary mechanical inventions. Tagline: "Fall from the flying castle, master the five elements, be the last mage standing."

**The two pillars (the actual differentiators):**
1. **Sintonia (Combined Casting).** Two mages casting *different* elements at the same target within a 1.5s window fuse into a stronger Combined Spell. Ten elemental pairs (Fire+Wind = Flaming Tornado, Water+Lightning = Electrocution, and so on). Limiters: roughly 1s of interruptible channelling, a long shared cooldown charged *at the start* so an interrupted combo does not refund, and both casters drained of mana. It is the mechanical reason to play in squads.
2. **Reactive Terrain.** The map is a resource, not scenery. The architecture follows the three formal rules of Breath of the Wild's chemistry engine. Fire spreads through forest and permanently destroys cover; water freezes the lake into a walkable bridge; lightning conducts through all connected water; earth raises destructible walls; water plus dirt creates slowing mud.

**Other design commitments.** Five elements (Fire, Water, Earth, Wind, Lightning). Everything is a travel-time projectile — no hitscan. Two separate economies: basic attacks cost mana, tactical abilities cost cooldown. An Evolving Magic Shield with four tiers, upgraded by damage dealt. Five Apex-style classes and a launch cast of ten mages. A Brazilian-folklore school of mages (Curupira, Saci, Boitatá, Iara, Cuca) planned as a signature differentiator. A public anti-pay-to-win oath: cosmetics only, never power. Target of 20 players per match rather than 60, mobile-first, 60fps on mid-range Android.

**Two products, one design document.**
- **Now — a validation build on Roblox.** Third-person, blocky art (visual reference: Pixel Gun 3D). Purpose: validate fun with real players at near-zero infrastructure cost using the platform's ready-made multiplayer. Explicitly accepted: nothing technical migrates out of Roblox; only the design document and the learning transfer.
- **Later — a premium product.** First-person, Android, targeting the visual bar of Spell Arena and commercial mobile shooters. It starts only after validation closes.

**Technical state of the Roblox build (working; never played by outside players).** Rojo plus Luau, git-versioned. Server-authoritative: the client sends intent only, while damage, mana, cooldown, collision and terrain state are decided server-side. Implemented: a 60x60 cell arena with lake, forest, tall grass and rocks; the full reactive-terrain state machine; travel-time projectiles; the evolving shield; a dodge with i-frames; all ten Sintonia combos with their terrain reactions; the battle-royale loop (lobby, a zone shrinking in five phases, elimination, champion card); code-built blocky bots with a wander/chase/attack/flee-from-fire state machine; one-gesture aim-and-fire touch controls (drag from the spell button, release to fire); jump, sprint and crouch; a third-person camera; a HUD with colour-plus-shape accessibility; and procedural atmosphere and VFX behind an automatic low-end quality gate. Zero third-party assets — no marketplace or Toolbox models. Roughly 2,600 world parts against a 4,000 budget.

**Prior prototype.** A complete 2D top-down build in Phaser 3 and TypeScript validated the terrain chemistry and the combat feel before the Roblox port.

## 2. Hard constraints (any recommendation that violates these is useless)

- **Team:** one creative director plus AI coding agents. No human netcode engineer, no art or audio staff, no funding. Recommendations must be executable at that scale or explicitly labelled "requires hiring".
- **Budget:** effectively zero. Free and open tooling, plus platform-native systems.
- **Mobile-first:** 60fps on mid-range Android, and still playable on weak devices.
- **Monetisation:** cosmetics only, permanently. Any pay-for-power suggestion is out of scope.
- **Assets:** no marketplace or Toolbox models, for quality and security reasons.
- **Delivery cadence:** everything must arrive as testable increments. No multi-month rewrite before something is playable.

## 3. What to research (go deep, cite sources, note dates)

**A. Market and competitive landscape.** The current size and trajectory of mobile and Roblox battle royale, and where magic-combat BR sits within it. A rigorous teardown of the closest comparables: **Spellbreak** (why it shut down — separate the mechanical question from retention, monetisation and marketing, using the developers' own post-mortems), **Spell Arena**, **Pixel Gun 3D**, **Brawl Stars**, **Free Fire**, **Fortnite**, **Apex Legends**, and the top Roblox shooters and BR experiences currently ranking. For each: the mechanic they own, their retention loop, and the single lesson transferable to Arkana. Then identify the actual gap a two-pillar magic BR could occupy — or state plainly if there is none.

**B. Battle royale mechanics, from the literature and from shipped games.** What makes match pacing work at 20 players versus 60. Zone and circle design mathematics: timing, damage curves, centre selection, and the third-party problem. Loot distribution and rarity curves. Time-to-kill norms per platform, and what TTK does to skill expression and casual retention. Respawn and revive systems and their retention effects. Ping systems and squad communication without voice. Anti-snowball measures. Bot policy in early matches. Match-length targets for mobile sessions. Give numbers wherever numbers exist, with sources.

**C. Ability and character design.** Frameworks for designing a roster where every ability has visible counterplay. How Apex, Overwatch and Valorant telegraph power, and how they stop information-gathering abilities from becoming oppressive. Then stress-test **Sintonia** specifically: find any shipped game with genuine cooperative spell-combining, what broke it, and which limiters kept it fair. Recommend the safest version to test first and exactly what to measure to know whether it works. Also evaluate the **evolving shield** and the **mana-plus-cooldown two-economy model** against shipped equivalents.

**D. Reactive terrain and simulation in production.** How shipped games run environmental simulation (fire spread, freezing, conduction) at scale in multiplayer without breaking netcode or the frame budget: what state is replicated, what is client-predicted, and what is faked visually. Cover cellular-automata fire-spread models, percolation thresholds, and the cost of authoritative world state on mobile. Name concrete techniques and the trade-off each one buys.

**E. Graphics that punch above the team's weight.** What actually makes a stylised low-poly or blocky mobile game read as premium rather than cheap: silhouette and shape language, flat-colour and gradient discipline, lighting and post-processing that survive a mobile frame budget, VFX layering, the animation minimum that matters most per character, and readability rules for competitive play. Include Roblox-specific rendering capabilities and limits as of now. Then run the same analysis for the future first-person premium product, including which engine (Godot 4, Unity, Unreal) a solo-plus-AI team can realistically ship a networked mobile shooter in, with evidence from comparable shipped titles.

**F. Roblox as a business and a discovery platform.** How the discovery algorithm actually rewards games in practice, and which metrics drive it. Realistic revenue economics after the platform cut. What genuinely moves the needle for a new experience: thumbnails and icons, first-session design, update cadence, social features, UGC, and creator and influencer distribution in Brazil specifically. Documented case studies of games that grew from nothing, and the common causes of failure. Also: what a Roblox hit does and does not prove about the viability of a standalone product.

**G. Retention and live operations at small scale.** Day-1, day-7 and day-30 benchmarks for the genre. Onboarding patterns that work in the first three minutes. Progression that respects an anti-P2W stance. Battle-pass structure. A season cadence a one-person operation can actually sustain. What to instrument and measure from day one.

**H. Localisation and the Brazilian market.** The size and behaviour of the Brazilian mobile BR audience. Whether the Brazilian-folklore mage school is a genuine differentiator or a niche trap, with evidence either way. Cultural, legal and naming pitfalls of using folklore characters commercially.

## 4. Required output

Write the report in **Brazilian Portuguese** (the client's language), keeping source citations in their original language. Structure it as:

1. **Executive summary** — the five decisions the client should make now, each with its reasoning in two sentences.
2. **Verdict on the two pillars** — is Sintonia genuinely defensible? Is reactive terrain worth its technical cost? Evidence for and against, plus the cheapest experiment that would settle each question.
3. **Prioritised backlog** — a table ranked by impact divided by effort, marking each item as doable by AI agents, needs a human specialist, or not worth doing. Include rough effort in person-days and the specific validation question each item answers.
4. **Explicit cut list** — features in the existing design document that the research suggests dropping or deferring, with the reason each is a trap. Be ruthless; this section is as valuable as the backlog.
5. **Concrete design specifications** for the top recommendations: numbers, timings, counterplay, and how success will be measured. Not vague advice.
6. **Graphics roadmap** — an ordered sequence of changes for the Roblox build and then for the premium product, each with its expected perceived-quality gain relative to cost.
7. **Risk register** — the failure modes most likely to kill this project, ranked by probability times damage, each with an early-warning indicator.
8. **Sources** — annotated, flagging which are primary (developer post-mortems, GDC talks, platform documentation, analytics reports) and which are secondary or speculative.

## 5. Rules of engagement

- Separate **evidence** from **inference** explicitly. When you infer, say so and give a confidence level.
- Prefer primary sources, and name the date of every key figure; the industry moves fast and stale numbers mislead.
- Where evidence is thin or contested, say so instead of smoothing it over.
- Do not propose anything requiring a team, a budget or a technology the client does not have — unless you mark it clearly as a future-conditional bet and state what would have to become true first.
- Assume the client would rather hear "cut this" than "add this". The goal is the shortest credible path to a game that real players choose to keep playing.
