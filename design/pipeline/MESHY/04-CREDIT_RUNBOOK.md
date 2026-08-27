# RUNBOOK DE CRÉDITOS

## Antes do lote
- consultar saldo;
- estimar custo;
- definir teto;
- registrar responsável;
- definir número máximo de tentativas.

## Durante
Parar lote se:
- mais de 25% dos resultados falharem no mesmo defeito;
- direção de arte começou a variar;
- task cost divergiu do documento;
- parâmetros mudaram;
- modelo Meshy mudou.

## Depois
Registrar:
- saldo inicial;
- saldo final;
- assets aprovados;
- assets rejeitados;
- custo por aprovado.

## KPI
`taxa_aprovacao = assets_aprovados / gerações`

Meta inicial:
- props: >60%;
- personagem hero: qualidade > taxa de aprovação; não automatizar até o prompt/reference estar estável.
