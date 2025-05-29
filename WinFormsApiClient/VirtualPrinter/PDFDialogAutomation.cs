using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using WinFormsApiClient.VirtualWatcher;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para automatizar el diálogo de guardado de PDF usando AutoIT
    /// </summary>
    public class PDFDialogAutomation
    {
        // Ruta para los archivos de AutoIT
        private static readonly string AutoITScriptFolder = Path.Combine(Path.GetTempPath(), "ECMCentralAutoIT");
        public static readonly string AutoITScriptPath = Path.Combine(AutoITScriptFolder, "PDFSaveDialogAutomation.au3");
        public static readonly string AutoITCompiledPath = Path.Combine(AutoITScriptFolder, "PDFSaveDialogAutomation.exe");

        // Proceso del script de AutoIT
        private static Process _autoITProcess = null;

        // Variable estática para evitar intentos repetidos en caso de error de seguridad
        private static DateTime _lastMonitorStartAttempt = DateTime.MinValue;
        private static bool _monitorStartupInProgress = false;

        /// <summary>
        /// Inicia el monitor en segundo plano y espera hasta que esté activo antes de continuar
        /// </summary>
        public static bool EnsureBackgroundMonitorBeforePrinting()
        {
            // Evitar múltiples llamadas simultáneas
            if (_monitorStartupInProgress)
            {
                Console.WriteLine("Verificación de monitor ya en curso, esperando...");
                return true;
            }

            // Evitar intentos demasiado frecuentes que pueden causar bucles
            TimeSpan timeSinceLastAttempt = DateTime.Now - _lastMonitorStartAttempt;
            if (timeSinceLastAttempt.TotalSeconds < 10) // No reintentar antes de 10 segundos
            {
                Console.WriteLine($"Último intento hace {timeSinceLastAttempt.TotalSeconds:F1} segundos, esperando...");
                return true;
            }

            _monitorStartupInProgress = true;
            _lastMonitorStartAttempt = DateTime.Now;

            try
            {
                Console.WriteLine("Verificando monitor de fondo antes de imprimir...");

                // Registrar en init_log.txt para diagnóstico
                string initLogPath = Path.Combine(VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");
                string fixedLogPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "print_process.log");

                try
                {
                    File.AppendAllText(initLogPath, $"[{DateTime.Now}] INICIO DE IMPRESIÓN - Verificando monitor de fondo...\r\n");
                    File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] INICIO DE IMPRESIÓN - Verificando monitor de fondo...\r\n");
                }
                catch (System.Security.SecurityException secEx)
                {
                    Console.WriteLine($"Advertencia: Sin permiso para escribir en logs: {secEx.Message}");
                    // Continuar a pesar del error de permisos en logs
                }

                // Verificar si el monitor ya está en ejecución
                if (BackgroundMonitorService._isRunning)
                {
                    string mensaje = "El monitor de fondo ya está activo, continuando con impresión.";
                    Console.WriteLine(mensaje);
                    try
                    {
                        File.AppendAllText(initLogPath, $"[{DateTime.Now}] {mensaje}\r\n");
                        File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] {mensaje}\r\n");
                    }
                    catch (Exception) { /* Ignorar errores de escritura */ }
                    return true;
                }

                // Verificar si realmente está en ejecución aunque la bandera diga lo contrario
                bool actuallyRunning = false;
                try
                {
                    actuallyRunning = BackgroundMonitorService.IsActuallyRunning();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al verificar estado real del monitor: {ex.Message}");
                }

                if (actuallyRunning)
                {
                    // El monitor está en ejecución, pero la bandera no está actualizada
                    Console.WriteLine("El monitor está en ejecución según verificación de procesos.");
                    BackgroundMonitorService._isRunning = true;
                    return true;
                }

                // No está activo, necesitamos iniciarlo directamente sin crear procesos adicionales
                try
                {
                    File.AppendAllText(initLogPath, $"[{DateTime.Now}] Monitor NO activo - Iniciando monitor de fondo...\r\n");
                    File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] Monitor NO activo - Iniciando monitor de fondo...\r\n");
                }
                catch (Exception) { /* Ignorar errores de escritura */ }

                // Iniciar el servicio directamente
                try
                {
                    BackgroundMonitorService.Start();
                    // Esperar brevemente a que se inicialice
                    System.Threading.Thread.Sleep(1000);

                    if (BackgroundMonitorService._isRunning)
                    {
                        string mensaje = "Monitor iniciado directamente con éxito.";
                        Console.WriteLine(mensaje);
                        try
                        {
                            File.AppendAllText(initLogPath, $"[{DateTime.Now}] ✓ {mensaje}\r\n");
                            File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] ✓ {mensaje}\r\n");
                        }
                        catch (Exception) { /* Ignorar errores de escritura */ }
                        return true;
                    }
                }
                catch (System.Security.SecurityException secEx)
                {
                    Console.WriteLine($"Error de seguridad al iniciar monitor: {secEx.Message}");
                    try
                    {
                        File.AppendAllText(initLogPath, $"[{DateTime.Now}] Error de seguridad: {secEx.Message}\r\n");

                        // Crear un archivo de diagnóstico específico para errores de seguridad
                        string securityErrorLog = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "security_errors.log");
                        File.AppendAllText(securityErrorLog, $"[{DateTime.Now}] Error de seguridad al iniciar monitor: {secEx.Message}\r\n" +
                                                           $"Stack trace: {secEx.StackTrace}\r\n" +
                                                           $"Identity: {System.Security.Principal.WindowsIdentity.GetCurrent().Name}\r\n" +
                                                           $"Token: {System.Security.Principal.WindowsIdentity.GetCurrent().Token}\r\n");
                    }
                    catch (Exception) { /* Ignorar errores de escritura */ }

                    // A pesar del error de seguridad, indicamos que el monitor sigue funcionando
                    // para evitar bucles infinitos de reintentos
                    return true;
                }
                catch (Exception directEx)
                {
                    Console.WriteLine($"Error al iniciar monitor directamente: {directEx.Message}");
                    try
                    {
                        File.AppendAllText(initLogPath, $"[{DateTime.Now}] Error al iniciar monitor directamente: {directEx.Message}\r\n");
                    }
                    catch (Exception) { /* Ignorar errores de escritura */ }
                }

                string mensaje2 = "No se pudo iniciar el monitor de fondo, continuando de todas formas";
                Console.WriteLine(mensaje2);
                try
                {
                    File.AppendAllText(initLogPath, $"[{DateTime.Now}] ⚠️ {mensaje2}\r\n");
                    File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] ⚠️ {mensaje2}\r\n");
                }
                catch (Exception) { /* Ignorar errores de escritura */ }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error crítico al iniciar monitor: {ex.Message}");
                return false;
            }
            finally
            {
                _monitorStartupInProgress = false;
            }
        }

        /// <summary>
        /// Inicia la automatización del diálogo de guardado de PDF
        /// </summary>
        public static bool StartDialogAutomation()
        {
            try
            {
                Console.WriteLine("Preparando automatización de diálogos PDF...");

                // Asegurar que la carpeta de salida existe
                VirtualPrinterCore.EnsureOutputFolderExists();

                // Verificar si Bullzip ya está instalado y configurarlo
                ConfigureBullzipIfExists();

                // Preparar componentes estándar
                CreateInstructionsFile();

                // Crear la carpeta para el script si no existe
                if (!Directory.Exists(AutoITScriptFolder))
                {
                    Directory.CreateDirectory(AutoITScriptFolder);
                    Console.WriteLine($"Carpeta para scripts de AutoIT creada: {AutoITScriptFolder}");
                }

                // Crear el script de AutoIT
                WriteAutoITScript();

                // También crear el archivo batch de respaldo
                CreateBackupBatchFile();

                // Iniciar el script AutoIT
                RunAutoITScript();

                Console.WriteLine("Sistema de captura PDF listo y esperando trabajos de impresión");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al preparar automatización de diálogo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configura Bullzip si ya está instalado
        /// </summary>
        private static void ConfigureBullzipIfExists()
        {
            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
            {
                if (printer.Contains("Bullzip") || printer.Contains("PDF Printer"))
                {
                    Console.WriteLine("Impresora Bullzip detectada. Configurando...");

                    try
                    {
                        string configFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Bullzip", "PDF Printer");

                        if (!Directory.Exists(configFolder))
                        {
                            Directory.CreateDirectory(configFolder);
                        }

                        // Crear archivo de configuración básico
                        string iniPath = Path.Combine(configFolder, "settings.ini");
                        string iniContent = $@"[PDF Printer]
Output={VirtualPrinterCore.FIXED_OUTPUT_PATH}
ShowSaveAS=never
ShowSettings=never
ShowProgress=no
ShowProgressFinished=no
ConfirmOverwrite=no
OpenViewer=no
FilenameTemplate=ECM_<date:yyyyMMdd>_<time:HHmmss>
RememberLastFolders=no
RememberLastName=no
RetainDialog=no
DefaultSaveSettings=yes";

                        File.WriteAllText(iniPath, iniContent);
                        Console.WriteLine("Configuración de Bullzip aplicada");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"No se pudo configurar Bullzip: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Intenta configurar Bullzip como prioridad al inicio del servicio
        /// </summary>
        public static bool InitBullzipAtStartup()
        {
            try
            {
                Console.WriteLine("Verificando instalación de Bullzip PDF Printer al inicio del servicio...");

                // Verificar si Bullzip ya está instalado
                bool bullzipExists = false;

                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    if (printer.Contains("Bullzip") || printer.Contains("PDF Printer") ||
                        printer.Equals(VirtualPrinterCore.PRINTER_NAME, StringComparison.OrdinalIgnoreCase))
                    {
                        bullzipExists = true;
                        Console.WriteLine($"Impresora encontrada: {printer}");
                        break;
                    }
                }

                if (bullzipExists)
                {
                    // Si ya existe, solo configurarla
                    Console.WriteLine("Bullzip ya está instalado, aplicando configuración...");

                    // Configurar carpeta de salida en la configuración
                    try
                    {
                        string configFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                            "Bullzip", "PDF Printer");

                        if (!Directory.Exists(configFolder))
                        {
                            Directory.CreateDirectory(configFolder);
                        }

                        string iniContent = $@"[PDF Printer]
Output={VirtualPrinterCore.FIXED_OUTPUT_PATH}
ShowSaveAS=never
ShowSettings=never
ShowProgress=no
ShowProgressFinished=no
ConfirmOverwrite=no
OpenViewer=no
RunOnSuccess=
RunOnError=
FilenameTemplate=ECM_<date:yyyyMMdd>_<time:HHmmss>
RememberLastFolders=no
RememberLastName=no
RetainDialog=no
DefaultSaveSettings=yes";

                        string iniPath = Path.Combine(configFolder, "settings.ini");
                        File.WriteAllText(iniPath, iniContent);

                        WatcherLogger.LogActivity("Configuración de Bullzip aplicada al inicio del servicio");
                        Console.WriteLine("Configuración aplicada a Bullzip existente");

                        // Crear archivo diagnóstico para confirmar
                        string diagFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "bullzip_initialized.txt");
                        File.WriteAllText(diagFile, $"Bullzip configurado durante inicio: {DateTime.Now}\r\nRuta de salida: {VirtualPrinterCore.FIXED_OUTPUT_PATH}");

                        return true;
                    }
                    catch (Exception configEx)
                    {
                        Console.WriteLine($"Error al configurar Bullzip existente: {configEx.Message}");
                        WatcherLogger.LogError("Error al configurar Bullzip existente", configEx);
                    }
                }
                else
                {
                    // Si no existe, instalarlo y configurarlo
                    Console.WriteLine("Bullzip no encontrado, iniciando instalación...");
                    WatcherLogger.LogActivity("Iniciando instalación de Bullzip al inicio del servicio");

                    // Usar el método de instalación forzada
                    bool success = ForceBullzipSetup();

                    if (success)
                    {
                        WatcherLogger.LogActivity("Bullzip instalado y configurado exitosamente");
                        Console.WriteLine("Bullzip instalado y configurado correctamente");

                        // Crear archivo para registrar la instalación exitosa
                        string installLogFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "bullzip_install_success.txt");
                        File.WriteAllText(installLogFile,
                            $"Instalación exitosa: {DateTime.Now}\r\n" +
                            $"Configurado para: {VirtualPrinterCore.FIXED_OUTPUT_PATH}");

                        return true;
                    }
                    else
                    {
                        Console.WriteLine("No se pudo instalar Bullzip");
                        WatcherLogger.LogActivity("Falló instalación de Bullzip");
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar Bullzip: {ex.Message}");
                WatcherLogger.LogError("Error al inicializar Bullzip", ex);
                return false;
            }
        }

        // Agregar esta variable estática en la clase PDFDialogAutomation
        private static bool _isConfiguring = false;

        /// <summary>
        /// Verifica que Bullzip esté funcionando correctamente y está configurado para imprimir
        /// </summary>
        public static bool VerifyBullzipForPrinting()
        {
            // Evitar llamadas recursivas que generan bucle
            if (_isConfiguring)
            {
                Console.WriteLine("Configuración de Bullzip ya en proceso, evitando bucle recursivo");
                return true;
            }

            try
            {
                _isConfiguring = true;

                // Verificar que la impresora existe y está disponible
                bool printerExists = false;
                string targetPrinterName = VirtualPrinterCore.PRINTER_NAME;

                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    if (printer.Equals(targetPrinterName, StringComparison.OrdinalIgnoreCase))
                    {
                        printerExists = true;
                        break;
                    }
                }

                if (!printerExists)
                {
                    Console.WriteLine($"La impresora {targetPrinterName} no se encuentra instalada. Reinstalando...");
                    return ForceBullzipSetup();
                }

                // Verificar y actualizar configuración si es necesario
                string configFile = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Bullzip", "PDF Printer", "settings.ini");

                if (!File.Exists(configFile) || (File.GetLastWriteTime(configFile) < DateTime.Now.AddDays(-1)))
                {
                    Console.WriteLine("Actualizando configuración de Bullzip...");

                    string configDir = Path.GetDirectoryName(configFile);
                    if (!Directory.Exists(configDir))
                    {
                        Directory.CreateDirectory(configDir);
                    }

                    string iniContent = $@"[PDF Printer]
Output={VirtualPrinterCore.FIXED_OUTPUT_PATH}
ShowSaveAS=never
ShowSettings=never
ShowProgress=no
ShowProgressFinished=no
ConfirmOverwrite=no
OpenViewer=no
FilenameTemplate=ECM_<date:yyyyMMdd>_<time:HHmmss>
RememberLastFolders=no
RememberLastName=no
RetainDialog=no
DefaultSaveSettings=yes";

                    File.WriteAllText(configFile, iniContent);
                    Console.WriteLine("Configuración de Bullzip actualizada");
                }

                // Establecer como impresora predeterminada
                string psCommand = $@"
try {{
    (New-Object -ComObject WScript.Network).SetDefaultPrinter('{targetPrinterName}')
    Write-Output ""Establecida como predeterminada: {targetPrinterName}""
    $true
}} catch {{
    Write-Output ""Error: $($_.Exception.Message)""
    $false
}}";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                return result.Contains("Establecida como predeterminada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar estado de Bullzip: {ex.Message}");
                return false;
            }
            finally
            {
                _isConfiguring = false; // Siempre liberar la bandera
            }
        }

        public static bool ForceBullzipSetup()
        {
            // Evitar recursividad si ya estamos configurando
            if (_isConfiguring)
            {
                Console.WriteLine("Configuración de Bullzip ya en proceso, evitando bucle recursivo");
                return true;
            }

            try
            {
                _isConfiguring = true;
                Console.WriteLine("Forzando configuración de Bullzip PDF Printer...");

                bool monitorStarted = false;

                // Verificar si el monitor está activo, de lo contrario iniciarlo
                if (!WinFormsApiClient.VirtualWatcher.BackgroundMonitorService._isRunning)
                {
                    string initLogPath = Path.Combine(VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");

                    // Verificar si ha ocurrido una excepción de seguridad reciente
                    bool recentSecurityError = false;
                    try
                    {
                        string securityMarker = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, ".security_error_marker");
                        if (File.Exists(securityMarker))
                        {
                            string content = File.ReadAllText(securityMarker);
                            if (DateTime.TryParse(content, out DateTime lastError) &&
                                (DateTime.Now - lastError).TotalMinutes < 5)
                            {
                                recentSecurityError = true;
                                Console.WriteLine("Error reciente de seguridad detectado, evitando reinicio de monitor");
                            }
                        }
                    }
                    catch { /* Ignorar errores de lectura */ }

                    if (!recentSecurityError)
                    {
                        string mensaje = "Monitor de fondo no activo. Iniciando proceso de sincronización...";
                        Console.WriteLine(mensaje);
                        try
                        {
                            SafeWriteToLog(initLogPath, $"[{DateTime.Now}] {mensaje}\r\n");
                            WatcherLogger.LogActivity("Iniciando sincronización de monitor desde configuración de Bullzip");

                            // Usar el método de sincronización de PDFDialogAutomation
                            monitorStarted = PDFDialogAutomation.EnsureBackgroundMonitorBeforePrinting();
                            SafeWriteToLog(initLogPath, $"[{DateTime.Now}] Resultado sincronización: {(monitorStarted ? "Éxito" : "Fallido")}\r\n");
                        }
                        catch (System.Security.SecurityException secEx)
                        {
                            Console.WriteLine($"Error de seguridad al sincronizar: {secEx.Message}");
                            try
                            {
                                string securityMarker = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, ".security_error_marker");
                                File.WriteAllText(securityMarker, DateTime.Now.ToString());
                            }
                            catch { /* Ignorar errores de escritura */ }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al sincronizar monitor: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Monitor ya está activo
                    monitorStarted = true;
                }

                // Intentar configurar Bullzip incluso si hubo problemas con el monitor
                bool bullzipConfigured = false;
                try
                {
                    bullzipConfigured = VirtualPrinterCore.ConfigureBullzipPrinter();
                }
                catch (Exception configEx)
                {
                    Console.WriteLine($"Error al configurar Bullzip: {configEx.Message}");
                }

                // Considerar éxito si el monitor está activo o Bullzip está configurado
                bool success = monitorStarted || bullzipConfigured;

                if (success)
                {
                    Console.WriteLine(bullzipConfigured ?
                        "Bullzip configurado correctamente como impresora principal" :
                        "Monitor iniciado correctamente, pero configuración de Bullzip falló");

                    // Intentar configurar la impresora predeterminada si Bullzip está configurado
                    if (bullzipConfigured)
                    {
                        try
                        {
                            string printername = VirtualPrinterCore.PRINTER_NAME;
                            bool printerExists = false;

                            foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                            {
                                if (printer.Equals(printername, StringComparison.OrdinalIgnoreCase))
                                {
                                    printerExists = true;
                                    break;
                                }
                            }

                            if (printerExists)
                            {
                                Console.WriteLine($"Configurando {printername} como impresora predeterminada");

                                using (System.Drawing.Printing.PrintDocument pd = new System.Drawing.Printing.PrintDocument())
                                {
                                    pd.PrinterSettings.PrinterName = printername;

                                    if (pd.PrinterSettings.IsValid)
                                    {
                                        Console.WriteLine("Confirmado: impresora es válida para .NET");

                                        string psCmd = $@"(New-Object -ComObject WScript.Network).SetDefaultPrinter('{printername}')";
                                        PowerShellHelper.RunPowerShellCommand(psCmd);

                                        string diagnosticFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "bullzip_status.txt");
                                        File.WriteAllText(diagnosticFile, $"Bullzip configurado: Sí\r\nFecha: {DateTime.Now}");
                                    }
                                }
                            }
                        }
                        catch (Exception printEx)
                        {
                            Console.WriteLine($"Aviso: Error al configurar impresora .NET: {printEx.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("No se pudo configurar Bullzip ni iniciar monitor");
                }

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al forzar configuración Bullzip: {ex.Message}");
                return false;
            }
            finally
            {
                _isConfiguring = false; // Siempre liberar la bandera
            }
        }

        /// <summary>
        /// Método auxiliar para escribir en archivos de log con manejo de excepciones de seguridad
        /// </summary>
        private static bool SafeWriteToLog(string filePath, string message)
        {
            try
            {
                File.AppendAllText(filePath, message);
                return true;
            }
            catch (System.Security.SecurityException)
            {
                // No hacer nada, es un error esperado en ciertos casos
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al escribir en log {filePath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Método a llamar cuando se detecta un trabajo de impresión para activar la automatización
        /// </summary>
        public static bool ActivateAutomationForPrintJob()
        {
            try
            {
                Console.WriteLine("¡Trabajo de impresión detectado! Activando automatización...");

                // Detener cualquier instancia previa
                StopDialogAutomation();

                // Intentar con Bullzip como primera prioridad
                bool success = ForceBullzipSetup();

                // Si Bullzip no funciona, usar AutoIT
                if (!success)
                {
                    Console.WriteLine("Bullzip no disponible. Intentando con AutoIT...");
                    success = RunAutoITScript();

                    // Si AutoIT tampoco funciona, usar el método directo
                    if (!success)
                    {
                        Console.WriteLine("AutoIT falló, usando vigilante de diálogo directo...");
                        success = StartDirectDialogWatcher();
                    }
                }

                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al activar automatización: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Crea un archivo de instrucciones para el usuario
        /// </summary>
        private static void CreateInstructionsFile()
        {
            try
            {
                string instructionsPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "INSTRUCCIONES_GUARDADO.txt");
                string instructions = $@"INSTRUCCIONES PARA USAR ECM CENTRAL PRINTER
=======================================

Si está viendo este archivo, significa que ha imprimido correctamente con la impresora ECM Central.

IMPORTANTE: Cuando imprima y aparezca el diálogo de Microsoft Print to PDF:

1. Guarde los archivos en esta carpeta: {VirtualPrinterCore.FIXED_OUTPUT_PATH}
2. Los archivos serán procesados automáticamente por el sistema

Si tiene problemas con la automatización, puede:
- Ejecutar el archivo MoverPDFs.bat que está en esta misma carpeta
- Contactar a soporte técnico

¡Gracias por usar ECM Central!
";
                File.WriteAllText(instructionsPath, instructions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear archivo de instrucciones: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea un archivo batch como método alternativo para la automatización
        /// </summary>
        public static void CreateBackupBatchFile()
        {
            try
            {
                string outputFolder = VirtualPrinterCore.FIXED_OUTPUT_PATH;
                string batchFilePath = Path.Combine(outputFolder, "MoverPDFs.bat");

                // Crear contenido del archivo batch para mover PDFs
                string batchContent = $@"@echo off
title Monitor de PDF para ECM Central
color 0A
echo ============================================
echo    MONITOR AUTOMATICO DE PDF - ECM CENTRAL
echo ============================================
echo.
echo Este programa vigilara sus carpetas y movera 
echo automaticamente los archivos PDF a la carpeta:
echo {outputFolder}
echo.
echo Por favor, mantenga esta ventana abierta mientras
echo este usando la aplicacion ECM Central.
echo.
echo Monitoreando...
echo (Presione Ctrl+C para finalizar)

:loop
:: Monitorear carpetas comunes
for %%F in (""%USERPROFILE%\Documents\*.pdf"" ""%USERPROFILE%\Desktop\*.pdf"" ""%USERPROFILE%\Downloads\*.pdf"") do (
    echo Encontrado archivo: %%F
    move ""%%F"" ""{outputFolder}\%%~nxF"" > nul 2>&1
    if not exist ""%%F"" (
        echo Archivo movido correctamente a {outputFolder}\%%~nxF
    )
)

:: Esperar 2 segundos y repetir
timeout /T 2 /NOBREAK > nul
goto loop
";

                // Guardar el archivo batch
                File.WriteAllText(batchFilePath, batchContent);
                Console.WriteLine($"Archivo batch de respaldo creado en: {batchFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear archivo batch de respaldo: {ex.Message}");
            }
        }

        /// <summary>
        /// Detiene la automatización del diálogo
        /// </summary>
        public static void StopDialogAutomation()
        {
            try
            {
                if (_autoITProcess != null && !_autoITProcess.HasExited)
                {
                    _autoITProcess.Kill();
                    _autoITProcess = null;
                    Console.WriteLine("Proceso de automatización de diálogo detenido");
                }

                // Buscar y detener cualquier proceso con el nombre del script
                string processName = Path.GetFileNameWithoutExtension(AutoITCompiledPath);
                Process[] processes = Process.GetProcessesByName(processName);

                foreach (var process in processes)
                {
                    try
                    {
                        process.Kill();
                        Console.WriteLine($"Proceso adicional de automatización detenido: {process.Id}");
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detener automatización de diálogo: {ex.Message}");
            }
        }

        // Script de AutoIT para automatización de diálogos
        private static void WriteAutoITScript()
        {
            // Asegurarse que la carpeta destino existe antes de crear el script
            Directory.CreateDirectory(VirtualPrinterCore.FIXED_OUTPUT_PATH);

            // Escribir el script de AutoIT (versión simplificada para mayor rendimiento)
            string scriptContent = $@";====================================================
; Script de automatización para diálogo de guardado PDF
; Generado por ECM Central
;====================================================

#include <File.au3>
#include <MsgBoxConstants.au3>
#include <WinAPIFiles.au3>
#include <Date.au3>
#include <AutoItConstants.au3>

; Configuración
Global $outputFolder = ""{VirtualPrinterCore.FIXED_OUTPUT_PATH.Replace("\\", "\\\\")}""

; Lógica principal
Global $logFile = $outputFolder & ""\autoit_log.txt""
FileWriteLine($logFile, ""[INICIO] "" & @YEAR & ""/"" & @MON & ""/"" & @MDAY & "" "" & @HOUR & "":"" & @MIN)

; Crear la carpeta de destino si no existe
If Not FileExists($outputFolder) Then
    DirCreate($outputFolder)
    FileWriteLine($logFile, ""Carpeta de destino creada: "" & $outputFolder)
EndIf

; Búsqueda continua de diálogos de guardado
While 1
    ; Buscar diálogos relacionados con PDF
    Local $hWnd = FindSaveDialog()
    
    ; Si se encontró un diálogo, procesarlo
    If $hWnd Then
        ProcessSaveDialog($hWnd)
    EndIf
    
    ; Pausa para no consumir recursos
    Sleep(100)
WEnd

; Función para encontrar diálogos de guardado
Func FindSaveDialog()
    Local $titles = [ _
        ""Guardar archivo PDF como"", ""Save PDF File As"", ""Guardar PDF como"", ""Save As"", _
        ""Guardar como"", ""Microsoft Print to PDF"", ""Guardar documento"", ""Print as PDF"" _
    ]
    
    For $i = 0 To UBound($titles) - 1
        $hWnd = WinGetHandle(""["" & $titles[$i] & ""]"")
        If @error = 0 Then Return $hWnd
    Next
    
    ; Búsqueda por clase de ventana
    $hWnd = WinGetHandle(""[CLASS:32770]"")
    If @error = 0 Then
        $title = WinGetTitle($hWnd)
        If StringInStr($title, ""PDF"") Or StringInStr($title, ""Guardar"") Or StringInStr($title, ""Save"") Then
            Return $hWnd
        EndIf
    EndIf
    
    Return 0
EndFunc

; Función para procesar el diálogo de guardado
Func ProcessSaveDialog($hWnd)
    ; Activar el diálogo
    WinActivate($hWnd)
    FileWriteLine($logFile, ""Diálogo de guardado encontrado: "" & WinGetTitle($hWnd))
    
    ; Generar nombre de archivo con timestamp
    Local $fileName = ""ECM_"" & @YEAR & @MON & @MDAY & ""_"" & @HOUR & @MIN & @SEC & "".pdf""
    Local $fullPath = $outputFolder & ""\"" & $fileName
    
    ; Establecer la ruta completa en el diálogo
    If ControlExists($hWnd, """", ""Edit1"") Then
        ControlSetText($hWnd, """", ""Edit1"", $fullPath)
    EndIf
    
    ; Presionar el botón Guardar
    If ControlExists($hWnd, """", ""Button1"") Then
        ControlClick($hWnd, """", ""Button1"")
    ElseIf ControlExists($hWnd, """", ""1"") Then
        ControlClick($hWnd, """", ""1"")
    Else
        ; Último recurso: Alt+G o Alt+S
        Send(""!g"") ; Alt+G para Guardar
        Sleep(200)
        Send(""!s"") ; Alt+S para Save
    EndIf
    
    ; Manejar posible diálogo de confirmación
    Sleep(500)
    $confirmHWnd = WinGetHandle(""[Confirm Save As]"")
    If @error = 0 Then
        ControlClick($confirmHWnd, """", ""Button1"")
        FileWriteLine($logFile, ""Diálogo de confirmación aceptado"")
    EndIf
    
    ; Notificar la creación del archivo
    FileWriteLine($logFile, ""Archivo guardado: "" & $fullPath)
EndFunc
";

            File.WriteAllText(AutoITScriptPath, scriptContent);
            Console.WriteLine($"Script de AutoIT actualizado: {AutoITScriptPath}");
        }

        private static bool RunAutoITScript()
        {
            try
            {
                // Asegurar que el directorio destino existe
                Directory.CreateDirectory(VirtualPrinterCore.FIXED_OUTPUT_PATH);

                // Detener cualquier script anterior
                StopDialogAutomation();

                // Verificar si ya hay una instancia en ejecución
                string processName = Path.GetFileNameWithoutExtension(AutoITCompiledPath);
                Process[] existingProcesses = Process.GetProcessesByName(processName);
                if (existingProcesses.Length > 0)
                {
                    Console.WriteLine("Ya existe una instancia de AutoIT en ejecución");
                    return true;
                }

                // Rutas posibles de AutoIt
                string[] possibleAutoItPaths = {
            @"C:\Program Files\AutoIt3\autoit3.exe",
            @"C:\Program Files (x86)\AutoIt3\autoit3.exe",
            // Añadir posible ubicación si AutoIt se descargó con la aplicación
            Path.Combine(Application.StartupPath, "tools", "autoit3.exe")
        };

                // Intentar ejecutar con AutoIt
                foreach (string autoItPath in possibleAutoItPaths)
                {
                    if (File.Exists(autoItPath))
                    {
                        try
                        {
                            ProcessStartInfo psi = new ProcessStartInfo
                            {
                                FileName = autoItPath,
                                Arguments = $"\"{AutoITScriptPath}\"",
                                UseShellExecute = true,
                                WindowStyle = ProcessWindowStyle.Hidden
                            };

                            _autoITProcess = Process.Start(psi);
                            Console.WriteLine($"Script AutoIT iniciado con: {autoItPath}");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error con AutoIt ({autoItPath}): {ex.Message}");
                        }
                    }
                }

                // Si no podemos encontrar AutoIT, usar un enfoque alternativo con PowerShell
                Console.WriteLine("AutoIT no encontrado, intentando con PowerShell como alternativa...");

                string psScript = $@"
        Add-Type -AssemblyName System.Windows.Forms
        $watcher = New-Object System.IO.FileSystemWatcher
        $watcher.Path = '{VirtualPrinterCore.FIXED_OUTPUT_PATH.Replace(@"\", @"\\")}'
        $watcher.Filter = '*.pdf'
        $watcher.EnableRaisingEvents = $true
        
        $action = {{ 
            $path = $Event.SourceEventArgs.FullPath 
            Write-Host ""PDF detectado: $path""
        }}
        
        Register-ObjectEvent -InputObject $watcher -EventName Created -Action $action
        
        while ($true) {{
            Start-Sleep -Seconds 1
            $dialogs = Get-Process | Where-Object {{ $_.MainWindowTitle -like '*PDF*' -or $_.MainWindowTitle -like '*Save*' -or $_.MainWindowTitle -like '*Guardar*' }}
            if ($dialogs) {{
                foreach ($dialog in $dialogs) {{
                    try {{
                        [System.Windows.Forms.SendKeys]::SendWait('{VirtualPrinterCore.FIXED_OUTPUT_PATH.Replace(@"\", @"\\")}' + '\ECM_' + (Get-Date -Format 'yyyyMMddHHmmss') + '.pdf')
                        Start-Sleep -Milliseconds 300
                        [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
                    }} catch {{ }}
                }}
            }}
        }}
        ";

                string scriptPath = Path.Combine(Path.GetTempPath(), "ecm_pdf_monitor.ps1");
                File.WriteAllText(scriptPath, psScript);

                ProcessStartInfo psPsi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -WindowStyle Hidden -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psPsi);
                Console.WriteLine("Alternativa PowerShell iniciada como respaldo");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al ejecutar script AutoIT: {ex.Message}");
                return false;
            }
        }


        public static bool StartDirectDialogWatcher()
        {
            try
            {
                Console.WriteLine("Iniciando vigilante de diálogo directo...");

                // Método simplificado que solo vigila por 30 segundos
                Task.Run(() =>
                {
                    try
                    {
                        WatchDialogProcessWithTimeout(TimeSpan.FromSeconds(30));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error en vigilante de diálogo: {ex.Message}");
                    }
                });

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar vigilante de diálogo: {ex.Message}");
                return false;
            }
        }

        private static void WatchDialogProcessWithTimeout(TimeSpan timeout)
        {
            DateTime endTime = DateTime.Now.Add(timeout);
            string[] searchTitles = { "Guardar", "Save", "PDF", "Print", "Imprimir", "Microsoft Print" };

            while (DateTime.Now < endTime)
            {
                try
                {
                    Process[] allProcesses = Process.GetProcesses();

                    foreach (Process proc in allProcesses)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(proc.MainWindowTitle))
                            {
                                string windowTitle = proc.MainWindowTitle.ToLower();

                                if (searchTitles.Any(title => windowTitle.Contains(title.ToLower())))
                                {
                                    Console.WriteLine($"Diálogo detectado: {proc.MainWindowTitle}");
                                    IntPtr hWnd = proc.MainWindowHandle;
                                    if (hWnd != IntPtr.Zero)
                                    {
                                        Win32API.SetForegroundWindow(hWnd);
                                        System.Threading.Thread.Sleep(300);

                                        // Enviar ruta predefinida
                                        string fileName = $"ECM_Doc_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                                        string fullPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, fileName);

                                        SendKeys.SendWait($"{fullPath}");
                                        System.Threading.Thread.Sleep(300);
                                        SendKeys.SendWait("{ENTER}");
                                        System.Threading.Thread.Sleep(500);
                                        SendKeys.SendWait("{ENTER}"); // Para confirmación

                                        return;
                                    }
                                }
                            }
                        }
                        catch { }
                    }

                    // Evitar uso excesivo de CPU
                    System.Threading.Thread.Sleep(300);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en vigilancia de diálogo: {ex.Message}");
                    System.Threading.Thread.Sleep(1000);
                }
            }

            Console.WriteLine("Vigilancia de diálogo finalizada");
        }
        /// <summary>
        /// Inicia un monitor de diálogos para todos los usuarios del sistema
        /// </summary>
        public static bool MonitorDialogsForAllUsers()
        {
            try
            {
                WatcherLogger.LogActivity("Iniciando monitor de diálogos para todos los usuarios");

                // Asegurar que la carpeta de salida existe
                if (!Directory.Exists(VirtualPrinterCore.FIXED_OUTPUT_PATH))
                {
                    Directory.CreateDirectory(VirtualPrinterCore.FIXED_OUTPUT_PATH);
                }

                // Iniciar la automatización de diálogos
                bool result = StartDialogAutomation();

                // Configurar un archivo batch para monitoreo continuo
                string monitorBatchPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "monitor_dialogs.bat");
                string batchContent = $@"@echo off
title Monitor de diálogos PDF - ECM Central
echo =============================================
echo   MONITOR AUTOMÁTICO DE DIÁLOGOS PDF
echo   ECM Central
echo =============================================
echo.
echo Monitoreando diálogos de guardado PDF...
echo Esta ventana puede minimizarse pero debe mantenerse abierta.
echo.

:loop
REM Verificar si hay diálogos abiertos
PowerShell -WindowStyle Hidden -Command ""
Add-Type -AssemblyName System.Windows.Forms
$titles = @('Guardar', 'Save', 'PDF', 'Print', 'Imprimir')
$found = $false

foreach ($process in Get-Process) {{
    if ($process.MainWindowTitle -ne '') {{
        foreach ($title in $titles) {{
            if ($process.MainWindowTitle -like '*' + $title + '*') {{
                $found = $true
                [System.Windows.Forms.SendKeys]::SendWait('{VirtualPrinterCore.FIXED_OUTPUT_PATH.Replace(@"\", @"\\")}' + '\documento_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.pdf')
                Start-Sleep -Milliseconds 300
                [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
                Start-Sleep -Milliseconds 500
                [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
                break
            }}
        }}
    }}
}}
""

REM Esperar antes de la siguiente verificación
timeout /t 1 /nobreak > nul
goto loop
";

                // Guardar el script
                File.WriteAllText(monitorBatchPath, batchContent);

                // Configurar permisos adecuados
                VirtualPrinterCore.EnsureScriptPermissions(monitorBatchPath);

                // Crear un archivo de instrucciones
                string instructionsPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "INSTRUCCIONES_MONITOR.txt");
                string instructions = $@"MONITOR DE DIÁLOGOS PDF
======================

Para activar la automatización de diálogos PDF:

1. Ejecute el archivo: {monitorBatchPath}
2. Deje esta aplicación ejecutándose minimizada

Este monitor detectará automáticamente los diálogos de guardado PDF y los
dirigirá a la carpeta: {VirtualPrinterCore.FIXED_OUTPUT_PATH}

Los archivos serán procesados automáticamente una vez guardados.
";

                File.WriteAllText(instructionsPath, instructions);

                WatcherLogger.LogActivity("Monitor de diálogos para todos los usuarios configurado correctamente");
                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al configurar monitor de diálogos para todos los usuarios", ex);
                return false;
            }
        }


        // Función para Windows API
        internal static class Win32API
        {
            [System.Runtime.InteropServices.DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);
        }
    }
}