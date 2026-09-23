@echo off
REM mobile-unity\habilitar_whpx.bat — habilita o Windows Hypervisor Platform (WHPX), a UNICA aceleracao
REM possivel para o Android Emulator nesta maquina (o hipervisor da Microsoft ja esta ativo por causa do
REM WSL2/Docker + VBS, o que impede HAXM e AEHD). Auditoria de 23/09/2026: HypervisorPlatform = desabilitado.
REM
REM O QUE FAZ:  liga o recurso opcional "HypervisorPlatform" (Ativar ou desativar recursos do Windows).
REM O QUE NAO FAZ: nao mexe em WSL2, Docker, VBS/Isolamento de nucleo, Hyper-V completo, nem em nenhuma
REM                configuracao de seguranca. E' reversivel: Disable-WindowsOptionalFeature -FeatureName HypervisorPlatform.
REM EXIGE: executar COMO ADMINISTRADOR (botao direito > Executar como administrador) e REINICIAR o Windows.
REM Depois do reboot, confira com:  %LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -accel-check
REM (esperado: "WHPX ... is installed and usable").
net session >nul 2>&1
if %errorlevel% neq 0 (
  echo Precisa rodar como ADMINISTRADOR. Botao direito no arquivo ^> Executar como administrador.
  pause
  exit /b 1
)
powershell -NoProfile -ExecutionPolicy Bypass -Command "Get-CimInstance Win32_OptionalFeature | Where-Object Name -eq 'HypervisorPlatform' | Select-Object Name, InstallState | Format-Table -AutoSize"
echo Habilitando HypervisorPlatform (WHPX)...
powershell -NoProfile -ExecutionPolicy Bypass -Command "Enable-WindowsOptionalFeature -Online -FeatureName HypervisorPlatform -NoRestart | Select-Object RestartNeeded | Format-List"
echo.
echo Pronto. REINICIE o Windows para o WHPX entrar em vigor. Nada mais foi alterado.
pause
