# mobile-unity/portao.ps1 — O PORTAO: compila e roda os testes EditMode e PlayMode, headless.
# Uso: powershell -File mobile-unity\portao.ps1 [-Rapido]
#   (padrao) COMPLETO: EditMode + PlayMode.   -Rapido: so' EditMode (logica pura, ~1 min).
# A linha final separa PASSARAM / FALHARAM / IGNORADOS: teste ignorado NAO e' teste passado (04/10/2026: o portao
# antigo dizia "620 testes, 9 falhas" com 86 ignorados dentro e ZERO PlayMode passando). Sai com 1 se algo falhar ou
# se nada passar. As FOTOS (FotoTests) se ignoram sem GPU (-nographics): rodam pelo foto.ps1.
param([switch]$Rapido)
$ErrorActionPreference = "Stop"
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj = $PSScriptRoot

# LICENCA: a Personal e' resolvida online pelo cliente de licenca que o Unity HUB sobe. Com o Hub
# fechado o batchmode morre com "No valid Unity Editor license found" (exit 198) — medido em
# 09/09 e 11/09/2026. Entao: Hub fechado -> abre e espera o cliente subir.
if (-not (Get-Process "Unity Hub" -ErrorAction SilentlyContinue)) {
    # NUNCA por "unityhub://": sem caminho o Hub entende "instalar editor" e abre o aviso
    # "Nao e' possivel instalar / versao do Editor arquivado" (visto pelo Diretor em 11/09/2026).
    Start-Process "shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub"
    Start-Sleep -Seconds 25
}
$Logs = Join-Path $Proj "Logs"
New-Item -ItemType Directory -Force $Logs | Out-Null

# Uma passada = um Unity (a trava de instancia obriga a serie): EditMode primeiro (rapido, prova a logica pura),
# PlayMode depois (BootTests: monta a arena inteira em cena). Os totais somam na linha final.
function Passada([string]$Plataforma, [string]$Log, [string]$Xml) {
    if (Test-Path $Xml) { Remove-Item $Xml }
    $UnityArgs = @("-batchmode", "-nographics", "-projectPath", "`"$Proj`"",
                   "-runTests", "-testPlatform", $Plataforma,
                   "-testResults", "`"$Xml`"", "-logFile", "`"$Log`"")
    # WaitForExit, NUNCA -Wait: o -Wait espera a ARVORE de processos, e o Unity deixa o VBCSCompiler (servidor do
    # Roslyn) vivo ~10 min depois de compilar — o portao ficou parado ali em 12/09
    $P = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -PassThru -NoNewWindow
    $null = $P.Handle   # sem tocar no handle antes do fim, o ExitCode volta vazio (quirk do PowerShell)
    $P.WaitForExit()

    if (-not (Test-Path $Xml)) {
        Write-Host "PORTAO VERMELHO ($Plataforma): nao houve resultado de teste (compilou?). Unity exit $($P.ExitCode). Erros do log:"
        Select-String -Path $Log -Pattern "error CS|Exception|Error:" | Select-Object -First 30 | ForEach-Object { Write-Host $_.Line }
        exit 1
    }
    [xml]$R = Get-Content $Xml
    $Run = $R.'test-run'
    $R.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
        Write-Host "FALHOU ($Plataforma): $($_.fullname)"
        Write-Host ($_.failure.message.'#cdata-section')
    }
    # o motivo de cada ignorado, agrupado (classe + mensagem): um ignorado novo aparece aqui em vez de virar "passou"
    $R.SelectNodes("//test-case[@result='Skipped']") | Group-Object { "$($_.classname.Split('.')[-1]): $($_.reason.message.'#cdata-section')" } |
        ForEach-Object { Write-Host "  ignorado ($Plataforma) x$($_.Count) - $($_.Name)" }
    # Write-Host de proposito: dentro de function, string solta vira valor de RETORNO e some da tela.
    Write-Host "  $Plataforma`: $([int]$Run.passed) passaram, $([int]$Run.failed) falharam, $([int]$Run.skipped) ignorados de $([int]$Run.total) (Unity exit $($P.ExitCode))"
    return @([int]$Run.passed, [int]$Run.failed, [int]$Run.skipped)
}

$Res = @(Passada "EditMode" (Join-Path $Logs "portao.log") (Join-Path $Logs "portao-resultados.xml"))
if (-not $Rapido) {
    $Play = @(Passada "PlayMode" (Join-Path $Logs "portao-playmode.log") (Join-Path $Logs "portao-playmode-resultados.xml"))
    $Res = @(($Res[-3] + $Play[-3]), ($Res[-2] + $Play[-2]), ($Res[-1] + $Play[-1]))   # parenteses: no PowerShell a virgula pega antes do +
}
$Passaram, $Falhas, $Ignorados = $Res[-3], $Res[-2], $Res[-1]
"ARKANA$(if ($Rapido) { ' (rapido: so EditMode)' }): $Passaram passaram, $Falhas falharam, $Ignorados ignorados"
if ($Falhas -gt 0 -or $Passaram -eq 0) { exit 1 }
exit 0
