# PADRÃO DE REFERÊNCIAS PARA MULTI-IMAGE

## Arquivos mínimos por personagem

`concept_master.png`  
`front.png`  
`side.png`  
`back.png`  
`three_quarter.png`  
`face_close.png`  
`signature_item.png`  
`marks.png`  
`palette.png`

## Regras das quatro vistas 3D
- mesma altura aparente;
- mesma roupa;
- mesmo cabelo;
- mesma manopla/arma;
- mesmos materiais;
- mesma paleta;
- fundo simples;
- luz uniforme;
- pose equivalente;
- sem capa cobrindo todo o corpo;
- sem partículas escondendo peças;
- câmera ortográfica ou longa distância focal sempre que possível.

## Ordem para Meshy 7
1. frente;
2. perfil;
3. costas;
4. 3/4.

A primeira imagem é tratada como vista principal na API atual.

## Marcas corporais
Entregar separadas:
- versão plana/preto e branco;
- versão colorida;
- posição no corpo;
- intensidade de emissão;
- versão sem glow.

Não “assar” glow forte no albedo. Glow deve ser controlável pelo shader.

## Peças assinatura
Gerar isoladamente quando:
- manopla;
- cajado;
- arco;
- fole;
- corvo;
- máscara;
- amuleto;
- cristal;
- equipamento mecânico.

Isso facilita modelagem manual e troca de LOD.
