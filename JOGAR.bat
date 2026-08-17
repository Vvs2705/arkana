@echo off
chcp 65001 >nul
title ARKANA - Campo de Provas
cd /d "%~dp0"

echo.
echo   ###################################################
echo   #                                                 #
echo   #                A R K A N A                      #
echo   #          Magos Battle Royale - v0.1             #
echo   #                                                 #
echo   ###################################################
echo.

REM --- 1) Procura o Node: primeiro no PATH, depois em locais conhecidos ---
set "NODE_EXE="

where node >nul 2>nul
if %errorlevel%==0 (
    set "NODE_EXE=node"
    goto :node_ok
)

if exist "%ProgramFiles%\nodejs\node.exe" (
    set "NODE_EXE=%ProgramFiles%\nodejs\node.exe"
    goto :node_ok
)

REM Fallback: o Node que acompanha o driver do Playwright (usado no desenvolvimento)
set "PW_NODE=%LOCALAPPDATA%\Programs\Python\Python313\Lib\site-packages\playwright\driver\node.exe"
if exist "%PW_NODE%" (
    set "NODE_EXE=%PW_NODE%"
    goto :node_ok
)

echo   [ERRO] Node.js nao encontrado.
echo.
echo   Instale o Node.js LTS e rode este arquivo de novo:
echo     https://nodejs.org
echo.
pause
exit /b 1

:node_ok

REM --- 2) Primeira execucao: instala as dependencias ---
if not exist "node_modules\vite\bin\vite.js" (
    echo   Primeira execucao: instalando dependencias...
    echo   Isso leva alguns minutos, mas acontece so uma vez.
    echo.
    call npm install
    if not exist "node_modules\vite\bin\vite.js" (
        echo.
        echo   [ERRO] Falha ao instalar as dependencias.
        echo   Abra um terminal nesta pasta e rode: npm install
        echo.
        pause
        exit /b 1
    )
)

REM --- 3) Sobe o servidor e abre o navegador ---
echo   Iniciando o jogo...
echo.
echo   O jogo abre no navegador em: http://localhost:5173
echo   Para PARAR: feche esta janela preta ou aperte Ctrl+C.
echo.

start "" http://localhost:5173

"%NODE_EXE%" "node_modules\vite\bin\vite.js" --port 5173 --host 127.0.0.1

echo.
echo   Servidor encerrado.
pause
