@echo off
setlocal
set "APPDIR=%~dp0Aplicacion"
set "APP=%APPDIR%\KasaServiceTracker.exe"

if not exist "%APP%" (
  echo No se encontro la aplicacion compilada en:
  echo %APP%
  echo Vuelve a extraer el ZIP completo y ejecuta este archivo otra vez.
  pause
  exit /b 1
)

powershell.exe -NoProfile -ExecutionPolicy Bypass -Command "$w=New-Object -ComObject WScript.Shell; $s=$w.CreateShortcut([Environment]::GetFolderPath('Desktop')+'\KASA Service Tracker.lnk'); $s.TargetPath='%APP%'; $s.WorkingDirectory='%APPDIR%'; $s.Description='KASA Service Tracker'; $s.Save()"

if errorlevel 1 (
  echo No fue posible crear el acceso directo.
  pause
  exit /b 1
)

echo Acceso directo creado correctamente en el Escritorio.
start "" "%APP%"
endlocal
