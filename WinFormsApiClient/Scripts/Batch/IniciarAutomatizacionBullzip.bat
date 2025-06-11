@echo off
title Automatización Bullzip PDF - ECM Central
color 0A

:: Configuración de rutas - USAR LA CARPETA OFICIAL
set OUTPUT_FOLDER=C:\Temp\ECM Central
set LOG_FOLDER=%OUTPUT_FOLDER%\logs
set AUTOIT_SCRIPT=%~dp0..\AutoIt\BullzipSaveDialog.au3

echo [%date% %time%] Iniciando sistema de automatización Bullzip > "%OUTPUT_FOLDER%\bullzip_batch_log.txt"

:: Crear carpetas necesarias
if not exist "%OUTPUT_FOLDER%" mkdir "%OUTPUT_FOLDER%"
if not exist "%LOG_FOLDER%" mkdir "%LOG_FOLDER%"

echo [%date% %time%] Verificando configuración de Bullzip... >> "%OUTPUT_FOLDER%\bullzip_batch_log.txt"

:: Configurar directamente Bullzip para guardar sin interacción
set BULLZIP_CONFIG=%APPDATA%\Bullzip\PDF Printer\settings.ini
set BULLZIP_CONFIG_FOLDER=%APPDATA%\Bullzip\PDF Printer

if not exist "%BULLZIP_CONFIG_FOLDER%" mkdir "%BULLZIP_CONFIG_FOLDER%"

echo [PDF Printer] > "%BULLZIP_CONFIG%"
echo Output=%OUTPUT_FOLDER% >> "%BULLZIP_CONFIG%"
echo ShowSaveAS=never >> "%BULLZIP_CONFIG%"
echo ShowSettings=never >> "%BULLZIP_CONFIG%"
echo ShowProgress=no >> "%BULLZIP_CONFIG%"
echo ShowProgressFinished=no >> "%BULLZIP_CONFIG%"
echo ConfirmOverwrite=no >> "%BULLZIP_CONFIG%"
echo OpenViewer=no >> "%BULLZIP_CONFIG%"
echo FilenameTemplate=ECM_^^<date:yyyyMMdd^^>_^^<time:HHmmss^^> >> "%BULLZIP_CONFIG%"
echo RememberLastFolders=no >> "%BULLZIP_CONFIG%"
echo DefaultSaveSettings=yes >> "%BULLZIP_CONFIG%"

echo [%date% %time%] Configuración de Bullzip actualizada >> "%OUTPUT_FOLDER%\bullzip_batch_log.txt"

:: Verificar si AutoIt está instalado
echo Verificando instalación de AutoIt...
if exist "C:\Program Files\AutoIt3\AutoIt3.exe" (
    set AUTOIT_PATH="C:\Program Files\AutoIt3\AutoIt3.exe"
    goto :run_script
) else if exist "C:\Program Files (x86)\AutoIt3\AutoIt3.exe" (
    set AUTOIT_PATH="C:\Program Files (x86)\AutoIt3\AutoIt3.exe"
    goto :run_script
) else (
    echo AutoIt no está instalado. Iniciando monitor básico...
    goto :monitor_mode
)

:run_script
echo Ejecutando script de automatización...
echo [%date% %time%] Iniciando AutoIt con script: %AUTOIT_SCRIPT% >> "%OUTPUT_FOLDER%\bullzip_batch_log.txt"
start "" %AUTOIT_PATH% "%AUTOIT_SCRIPT%"

:monitor_mode
:: Crear un script de monitoreo independiente para mover archivos PDF
echo @echo off > "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo title Monitor de PDFs para ECM Central >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo :loop >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo echo [%%date%% %%time%%] Buscando archivos PDF... >> "%LOG_FOLDER%\pdf_monitor.log" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo. >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo :: Monitorear carpetas comunes >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo for %%%%F in ( >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "%%USERPROFILE%%\Desktop\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "%%USERPROFILE%%\Downloads\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "%%USERPROFILE%%\Documents\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "%%USERPROFILE%%\Documentos\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "%%TEMP%%\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "C:\Temp\ECM Central\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     "C:\downloads\*.pdf" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo ) do ( >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     if exist "%%%%F" ( >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo         echo [%%date%% %%time%%] Encontrado: %%%%F >> "%LOG_FOLDER%\pdf_monitor.log" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo         move "%%%%F" "%OUTPUT_FOLDER%\%%%%~nxF" ^>nul >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo         if not exist "%%%%F" ( >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo             echo [%%date%% %%time%%] Movido correctamente a %OUTPUT_FOLDER% >> "%LOG_FOLDER%\pdf_monitor.log" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo             echo "%OUTPUT_FOLDER%\%%%%~nxF" ^> "%OUTPUT_FOLDER%\last_bullzip_file.txt" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo             echo "%OUTPUT_FOLDER%\%%%%~nxF" ^> "%OUTPUT_FOLDER%\pending_pdf.marker" >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo         ) >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo     ) >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo ) >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo. >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo timeout /t 1 /nobreak ^>nul >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo goto loop >> "%OUTPUT_FOLDER%\MonitorPDFs.bat"

echo Iniciando monitor de archivos PDF...
start "" "%OUTPUT_FOLDER%\MonitorPDFs.bat"
echo [%date% %time%] Monitor de archivos iniciado >> "%OUTPUT_FOLDER%\bullzip_batch_log.txt"
echo.
echo Automatización de Bullzip configurada correctamente.
echo Los archivos PDF se guardarán automáticamente en: %OUTPUT_FOLDER%
echo.
echo Para asegurar que los PDF se procesen correctamente, también iniciaremos MoverPDFs.bat

:: Iniciar también MoverPDFs.bat del proyecto como respaldo final
set MOVER_SCRIPT=%~dp0MoverPDFs.bat
if exist "%MOVER_SCRIPT%" (
    echo Iniciando script MoverPDFs.bat...
    start /min "" "%MOVER_SCRIPT%"
    echo [%date% %time%] MoverPDFs.bat iniciado >> "%OUTPUT_FOLDER%\bullzip_batch_log.txt"
)

exit