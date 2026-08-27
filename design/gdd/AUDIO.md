# AUDIO - direcao e implementacao atual

O audio do jogo 3D e produzido em `godot/audio/Sfx.gd`. A implementacao atual
sintetiza os efeitos em runtime e nao depende de arquivos de audio externos.

## Cobertura atual

- disparo e impacto dos cinco elementos;
- dano, esquiva e acoes de interface;
- contagem de inicio, vitoria e derrota;
- ambiente da partida;
- buses separados para SFX e ambiente.

O audio observa os eventos globais e nao decide gameplay. Sons futuros devem
preservar esse contrato e ser registrados em `docs/CREDITS.md` quando usarem
assets externos.

## Direcao futura

- musica hibrida orquestral-eletronica;
- camadas elementais sincronizadas durante o combate;
- mixagem por distancia para magias de outros jogadores;
- legendas ou feedback visual para informacao transmitida por som.
