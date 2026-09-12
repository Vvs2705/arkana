# mobile-unity/portao.ps1 — O PORTAO: compila e roda os testes EditMode, headless.
# Uso: powershell -File mobile-unity\portao.ps1
# Sucesso e' a linha "ARKANA: N testes, 0 falhas" no fim. Qualquer falha sai com codigo 1.
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
    # Write-Host de proposito: dentro de function, string solta vira valor de RETORNO e some da tela.
    Write-Host "  $Plataforma`: $([int]$Run.total) testes, $([int]$Run.failed) falhas (Unity exit $($P.ExitCode))"
    return @([int]$Run.total, [int]$Run.failed)
}

$Edit = Passada "EditMode" (Join-Path $Logs "portao.log") (Join-Path $Logs "portao-resultados.xml")
$Play = Passada "PlayMode" (Join-Path $Logs "portao-playmode.log") (Join-Path $Logs "portao-playmode-resultados.xml")
$Total = $Edit[-2] + $Play[-2]
$Falhas = $Edit[-1] + $Play[-1]
"ARKANA: $Total testes, $Falhas falhas"
if ($Falhas -gt 0 -or $Total -eq 0) { exit 1 }
exit 0
