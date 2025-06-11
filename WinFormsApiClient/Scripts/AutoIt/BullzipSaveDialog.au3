;=========================================================
; Script de automatización para diálogos de Bullzip PDF Printer
; Actualizado: Junio 2025 - Versión Robusta
;=========================================================

#include <AutoItConstants.au3>
#include <WinAPI.au3>
#include <File.au3>
#include <Date.au3>
#include <Array.au3>
#include <MsgBoxConstants.au3>

; Configuración de carpetas - DESTINO PRINCIPAL: La carpeta ECM Central
Global $outputFolder = "C:\Temp\ECM Central"
Global $logFile = $outputFolder & "\bullzip_autoit_log.txt"
Global $alternativeFolders[5] = ["C:\downloads", _
                              @DesktopDir, _
                              @MyDocumentsDir, _
                              @TempDir, _
                              "C:\Temp"]

; Iniciar log
If Not FileExists($outputFolder) Then
    DirCreate($outputFolder)
EndIf

FileWriteLine($logFile, _NowCalc() & " - Script de automatización de Bullzip iniciado (VERSIÓN MEJORADA)")

; Verificar instalación de Bullzip y configuración
CheckBullzipConfig()

; Carpetas a monitorear
MonitorFolders()

; Notificar inicio exitoso
TraySetIcon(@SystemDir & "\shell32.dll", 46)
TraySetToolTip("ECM Central - Automatización Bullzip activa")
TrayTip("ECM Central - Bullzip", "Automatización de guardado activa", 5, $TIP_ICONASTERISK)

; Bucle principal para monitorear diálogos de guardado
While 1
    ; Buscar diálogo de Bullzip
    Local $hWnd = FindBullzipDialog()
    
    If $hWnd <> 0 Then
        AutoSavePDF($hWnd)
    EndIf
    
    ; Pequeña pausa para no consumir recursos
    Sleep(200)
WEnd

; Verificar y configurar Bullzip
Func CheckBullzipConfig()
    ; Configurar Bullzip para usar el directorio correcto
    Local $configFolder = @AppDataDir & "\Bullzip\PDF Printer"
    Local $iniFile = $configFolder & "\settings.ini"
    
    ; Crear carpeta si no existe
    If Not FileExists($configFolder) Then
        DirCreate($configFolder)
        FileWriteLine($logFile, _NowCalc() & " - Carpeta de configuración Bullzip creada: " & $configFolder)
    EndIf
    
    ; Leer configuración actual
    Local $currentOutput = IniRead($iniFile, "PDF Printer", "Output", "")
    Local $currentShowSaveAs = IniRead($iniFile, "PDF Printer", "ShowSaveAs", "")
    
    FileWriteLine($logFile, _NowCalc() & " - Configuración actual de Bullzip:")
    FileWriteLine($logFile, _NowCalc() & " - Output: " & $currentOutput)
    FileWriteLine($logFile, _NowCalc() & " - ShowSaveAs: " & $currentShowSaveAs)
    
    ; Aplicar configuración para nunca mostrar diálogo
    IniWrite($iniFile, "PDF Printer", "Output", $outputFolder)
    IniWrite($iniFile, "PDF Printer", "ShowSaveAs", "never")  ; IMPORTANTE: "never", no "nunca"
    IniWrite($iniFile, "PDF Printer", "ShowSettings", "never")
    IniWrite($iniFile, "PDF Printer", "ShowProgress", "no")
    IniWrite($iniFile, "PDF Printer", "ShowProgressFinished", "no")
    IniWrite($iniFile, "PDF Printer", "ConfirmOverwrite", "no")
    IniWrite($iniFile, "PDF Printer", "OpenViewer", "no")
    IniWrite($iniFile, "PDF Printer", "FilenameTemplate", "ECM_<date:yyyyMMdd>_<time:HHmmss>")
    
    FileWriteLine($logFile, _NowCalc() & " - Configuración de Bullzip actualizada correctamente")
EndFunc

; Función para monitorear carpetas populares
Func MonitorFolders()
    ; Asegurar que existan las carpetas alternativas 
    For $i = 0 To UBound($alternativeFolders) - 1
        If Not FileExists($alternativeFolders[$i]) And $i > 0 Then ; No crear las carpetas del sistema, solo la nuestra
            DirCreate($alternativeFolders[$i])
            FileWriteLine($logFile, _NowCalc() & " - Carpeta de monitoreo creada: " & $alternativeFolders[$i])
        EndIf
    Next
    
    ; Iniciar un script de monitoreo separado
    Local $monitorScript = $outputFolder & "\PDFMonitor.bat"
    Local $content = @CRLF & _
        "@echo off" & @CRLF & _
        "echo Monitoreando carpetas para archivos PDF..." & @CRLF & _
        ":loop" & @CRLF & _
        "REM Buscar archivos PDF en carpetas comunes" & @CRLF & _
        "for %%F in (" & @CRLF & _
        "    ""%USERPROFILE%\Desktop\*.pdf""" & @CRLF & _
        "    ""%USERPROFILE%\Downloads\*.pdf""" & @CRLF & _
        "    ""%USERPROFILE%\Documentos\*.pdf""" & @CRLF & _
        "    ""%USERPROFILE%\Documents\*.pdf""" & @CRLF & _
        "    ""%TEMP%\*.pdf""" & @CRLF & _
        "    ""C:\downloads\*.pdf""" & @CRLF & _
        "    ""C:\Temp\*.pdf""" & @CRLF & _
        ") do (" & @CRLF & _
        "    if exist ""%%F"" (" & @CRLF & _
        "        move ""%%F"" """ & $outputFolder & "\%%~nxF"" >nul 2>&1" & @CRLF & _
        "        if not exist ""%%F"" (" & @CRLF & _
        "            echo [%date% %time%] Movido: %%F a " & $outputFolder & "\%%~nxF" & @CRLF & _
        "            echo " & $outputFolder & "\%%~nxF > """ & $outputFolder & "\pending_pdf.marker""" & @CRLF & _
        "        )" & @CRLF & _
        "    )" & @CRLF & _
        ")" & @CRLF & _
        "timeout /t 1 /nobreak >nul" & @CRLF & _
        "goto loop"
    
    FileWrite($monitorScript, $content)
    FileWriteLine($logFile, _NowCalc() & " - Script de monitoreo creado: " & $monitorScript)
    
    ; Ejecutar el script de monitoreo en segundo plano
    Run(@ComSpec & " /c start /min " & $monitorScript, "", @SW_HIDE)
EndFunc

; Función para encontrar diálogos de Bullzip
Func FindBullzipDialog()
    ; Buscar ventanas por título exacto y por contenido
    Local $titles = [ _
        "Bullzip PDF Printer", _
        "PDF Printer", _
        "Crear Archivo", _
        "Save PDF File", _
        "Guardar PDF", _
        "Guardar como", _
        "Save As", _
        "Microsoft Print to PDF", _
        "Save" _
    ]
    
    ; Intentar dos métodos de búsqueda
    ; 1. Búsqueda directa por título
    For $i = 0 To UBound($titles) - 1
        Local $hWnd = WinGetHandle($titles[$i])
        If @error = 0 Then
            FileWriteLine($logFile, _NowCalc() & " - Diálogo encontrado por título exacto: " & $titles[$i])
            Return $hWnd
        EndIf
    Next
    
    ; 2. Búsqueda con comodín
    For $i = 0 To UBound($titles) - 1
        Local $hWnd = WinGetHandle("*" & $titles[$i] & "*")
        If @error = 0 Then
            Local $title = WinGetTitle($hWnd)
            FileWriteLine($logFile, _NowCalc() & " - Diálogo encontrado por título parcial: " & $title)
            Return $hWnd
        EndIf
    Next
    
    ; 3. Buscar cualquier diálogo (#32770) con ciertos términos en el título
    Local $hWnds = WinList()
    For $i = 1 To $hWnds[0][0]
        If $hWnds[$i][0] <> "" Then
            Local $class = WinGetClass($hWnds[$i][1])
            Local $title = $hWnds[$i][0]
            
            If $class = "#32770" And _
               (StringInStr($title, "PDF") Or _
                StringInStr($title, "Guardar") Or _
                StringInStr($title, "Save") Or _
                StringInStr($title, "Bullzip") Or _
                StringInStr($title, "Microsoft Print")) Then
                FileWriteLine($logFile, _NowCalc() & " - Diálogo encontrado por clase y contenido: " & $title)
                Return $hWnds[$i][1]
            EndIf
        EndIf
    Next
    
    Return 0
EndFunc

; Función para guardar automáticamente el PDF
Func AutoSavePDF($hWnd)
    ; Logear el evento
    Local $title = WinGetTitle($hWnd)
    FileWriteLine($logFile, _NowCalc() & " - Procesando diálogo: " & $title)
    
    ; Captura de pantalla para diagnóstico (opcional)
    Local $screenshotPath = $outputFolder & "\dialog_screenshot_" & @YEAR & @MON & @MDAY & "_" & @HOUR & @MIN & @SEC & ".jpg"
    
    ; Activar la ventana y esperar para asegurar que esté en primer plano
    WinActivate($hWnd)
    WinWaitActive($hWnd, "", 3)
    
    ; Generar nombre único para el archivo
    Local $timestamp = @YEAR & @MON & @MDAY & "_" & @HOUR & @MIN & @SEC
    Local $filename = "ECM_" & $timestamp & ".pdf"
    Local $fullPath = $outputFolder & "\" & $filename
    
    FileWriteLine($logFile, _NowCalc() & " - Nombre de archivo generado: " & $fullPath)
    
    ; REGISTRO DETALLADO: Listar todos los controles encontrados en el diálogo
    Local $allControls = WinGetClassList($hWnd)
    FileWriteLine($logFile, _NowCalc() & " - Lista de clases en el diálogo: " & @CRLF & $allControls)
    
    ; Método mejorado para establecer el nombre del archivo
    ; Intento 1: Control Edit estándar
    If ControlSetText($hWnd, "", "Edit1", $fullPath) Then
        FileWriteLine($logFile, _NowCalc() & " - Texto establecido en Edit1")
        Sleep(500)
    ; Intento 2: Control Edit alternativo
    ElseIf ControlSetText($hWnd, "", "[CLASS:Edit; INSTANCE:1]", $fullPath) Then
        FileWriteLine($logFile, _NowCalc() & " - Texto establecido en Edit por clase")
        Sleep(500)
    ; Intento 3: Usar envío de teclas
    Else
        FileWriteLine($logFile, _NowCalc() & " - Usando envío de teclas para ruta")
        Send("^a") ; Ctrl+A para seleccionar todo
        Sleep(300)
        Send($fullPath) ; Enviar ruta completa
        Sleep(500)
    EndIf
    
    ; Método para hacer clic en guardar - abordaje múltiple
    Local $clicked = False
    
    ; 1. Intentar primero hacer clic directamente en los botones de "Guardar" o "Save"
    Local $buttons = ["Guardar", "Save", "OK", "&Save", "&Guardar"]
    For $btnText In $buttons
        If WinExists("[CLASS:Button; TITLE:" & $btnText & "]") Then
            ControlClick($hWnd, "", "[CLASS:Button; TITLE:" & $btnText & "]")
            FileWriteLine($logFile, _NowCalc() & " - Clic en botón: " & $btnText)
            $clicked = True
            ExitLoop
        EndIf
    Next
    
    ; 2. Si no se pudo encontrar por texto, intentar por ID
    If Not $clicked Then
        For $i = 1 To 5
            If ControlClick($hWnd, "", "Button" & $i) Then
                FileWriteLine($logFile, _NowCalc() & " - Clic en Button" & $i)
                $clicked = True
                ExitLoop
            EndIf
        Next
    EndIf
    
    ; 3. Si aún no funciona, enviar combinaciones de teclas
    If Not $clicked Then
        FileWriteLine($logFile, _NowCalc() & " - Usando combinaciones de teclas")
        
        Send("{TAB}") ; Moverse al siguiente control
        Sleep(300)
        Send("{ENTER}") ; Intentar Enter
        Sleep(500)
        
        Send("{ALTDOWN}s{ALTUP}") ; Alt+S (Save)
        Sleep(300)
        
        Send("{ALTDOWN}g{ALTUP}") ; Alt+G (Guardar)
        Sleep(300)
        
        Send("{ENTER}") ; Enter como último recurso
    EndIf
    
    ; Esperar para manejar diálogos de confirmación
    Sleep(1000)
    Local $confirmWnd = WinGetHandle("[REGEXPTITLE:(?i).*(confirm|replace|reemplazar|sobrescribir).*]")
    If @error = 0 Then
        FileWriteLine($logFile, _NowCalc() & " - Detectado diálogo de confirmación: " & WinGetTitle($confirmWnd))
        
        ; Intentar hacer clic en "Sí" o "Yes"
        If ControlClick($confirmWnd, "", "Button1") Then
            FileWriteLine($logFile, _NowCalc() & " - Clic en Button1 (Sí/Yes)")
        ElseIf ControlClick($confirmWnd, "", "6") Then
            FileWriteLine($logFile, _NowCalc() & " - Clic en control 6 (alternativo)")
        Else
            Send("{LEFT}{ENTER}") ; Típicamente para seleccionar "Sí" y presionar Enter
            FileWriteLine($logFile, _NowCalc() & " - Enviado LEFT+ENTER para confirmar")
        EndIf
        
        Sleep(500)
    EndIf
    
    ; Verificar si el archivo se guardó - esperar hasta 5 segundos
    Local $saved = False
    For $i = 1 To 10
        Sleep(500)
        If FileExists($fullPath) Then
            $saved = True
            FileWriteLine($logFile, _NowCalc() & " - ✓ ÉXITO: Archivo guardado correctamente en " & $fullPath)
            
            ; Crear el archivo de marcador
            FileWrite($outputFolder & "\last_bullzip_file.txt", $fullPath)
            FileWrite($outputFolder & "\pending_pdf.marker", $fullPath)
            ExitLoop
        EndIf
    Next
    
    ; Si no se encontró, buscar en todas las carpetas posibles
    If Not $saved Then
        FileWriteLine($logFile, _NowCalc() & " - Archivo no encontrado en ruta principal, buscando alternativas...")
        $saved = SearchPDFInAllLocations($filename)
    EndIf
    
    ; Notificación en caso de éxito o error
    If $saved Then
        TrayTip("ECM Central", "PDF guardado correctamente", 3)
    Else
        FileWriteLine($logFile, _NowCalc() & " - ⚠ ERROR: No se pudo confirmar que el PDF se guardó correctamente")
        TrayTip("ECM Central", "No se pudo guardar el PDF automáticamente", 3, $TIP_ICONEXCLAMATION)
    EndIf
    
    Return $saved
EndFunc

; Función para buscar PDFs en todas las ubicaciones posibles
Func SearchPDFInAllLocations($targetFilename)
    ; Buscar el archivo específico primero
    Local $allLocations = ["C:\downloads", _
                          $outputFolder, _
                          @DesktopDir, _
                          @MyDocumentsDir, _
                          @TempDir, _
                          "C:\Temp", _
                          @UserProfileDir & "\Downloads"]
    
    ; Buscar primero por nombre exacto
    FileWriteLine($logFile, _NowCalc() & " - Buscando archivo: " & $targetFilename)
    For $location In $allLocations
        If FileExists($location) Then
            Local $fullPath = $location & "\" & $targetFilename
            If FileExists($fullPath) Then
                FileWriteLine($logFile, _NowCalc() & " - Archivo encontrado en: " & $fullPath)
                
                ; Intentar mover el archivo a la ubicación correcta si no está ya ahí
                If $location <> $outputFolder Then
                    Local $destPath = $outputFolder & "\" & $targetFilename
                    If FileMove($fullPath, $destPath, 1) Then
                        FileWriteLine($logFile, _NowCalc() & " - Archivo movido a ubicación correcta: " & $destPath)
                        FileWrite($outputFolder & "\last_bullzip_file.txt", $destPath)
                        FileWrite($outputFolder & "\pending_pdf.marker", $destPath)
                        Return True
                    Else
                        ; Si no se puede mover, usar la ubicación actual
                        FileWrite($outputFolder & "\last_bullzip_file.txt", $fullPath)
                        FileWrite($outputFolder & "\pending_pdf.marker", $fullPath)
                        Return True
                    EndIf
                Else
                    FileWrite($outputFolder & "\last_bullzip_file.txt", $fullPath)
                    FileWrite($outputFolder & "\pending_pdf.marker", $fullPath)
                    Return True
                EndIf
            EndIf
        EndIf
    Next
    
    ; Si no lo encontramos por nombre exacto, buscar cualquier PDF reciente
    FileWriteLine($logFile, _NowCalc() & " - Buscando cualquier PDF reciente...")
    
    Local $newestPDF = ""
    Local $newestTime = 0
    
    For $location In $allLocations
        If FileExists($location) Then
            Local $search = FileFindFirstFile($location & "\*.pdf")
            If $search <> -1 Then
                While 1
                    Local $file = FileFindNextFile($search)
                    If @error Then ExitLoop
                    
                    Local $filePath = $location & "\" & $file
                    Local $fileTime = FileGetTime($filePath, 0, 1) ; Obtener tiempo de creación
                    
                    ; Si el archivo es más reciente que el último encontrado
                    If $fileTime > $newestTime Then
                        $newestPDF = $filePath
                        $newestTime = $fileTime
                    EndIf
                WEnd
                FileClose($search)
            EndIf
        EndIf
    Next
    
    ; Si encontramos un archivo reciente (menos de 2 minutos)
    If $newestPDF <> "" And (TimerDiff($newestTime) < 120000 Or FileGetTime($newestPDF, 0, 1) > @YEAR & @MON & @MDAY & @HOUR & @MIN - 2) Then
        FileWriteLine($logFile, _NowCalc() & " - PDF reciente encontrado: " & $newestPDF)
        
        ; Usar este archivo
        FileWrite($outputFolder & "\last_bullzip_file.txt", $newestPDF)
        FileWrite($outputFolder & "\pending_pdf.marker", $newestPDF)
        Return True
    EndIf
    
    Return False
EndFunc