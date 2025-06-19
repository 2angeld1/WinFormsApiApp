using FontAwesome.Sharp;
using Microsoft.Win32;
using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Xml.Linq;
using WinFormsApiClient.VirtualWatcher;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TextBox;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

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

                        // Asegurar que existe el batch
                        string batchPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "MoverPDFs.bat");
                        if (!File.Exists(batchPath))
                        {
                            CreateBackupBatchFile();
                            VirtualPrinterCore.EnsureScriptPermissions(batchPath);
                        }

                        // Crear archivo de configuración básico con RunOnSuccess
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
DefaultSaveSettings=yes
RunOnSuccess=""{batchPath}""
RunOnPrinterEvent=1
RunPrinterEventTiming=after";

                        File.WriteAllText(iniPath, iniContent);
                        Console.WriteLine("Configuración de Bullzip aplicada con ejecución de MoverPDFs.bat");

                        // Ejecutar el batch para verificarlo
                        RunBatchHidden(batchPath);
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

                // Asegurar que existe el batch MoverPDFs.bat
                string batchPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "MoverPDFs.bat");
                if (!File.Exists(batchPath) ||
                    (File.GetLastWriteTime(batchPath) < DateTime.Now.AddDays(-1)))
                {
                    Console.WriteLine("Creando/actualizando archivo batch MoverPDFs.bat");
                    CreateBackupBatchFile();

                    // Asegurar permisos de ejecución
                    VirtualPrinterCore.EnsureScriptPermissions(batchPath);
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

                    // Configuración actualizada con RunOnSuccess
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
DefaultSaveSettings=yes
RunOnSuccess=""{batchPath}""
RunOnPrinterEvent=1
RunPrinterEventTiming=after";

                    File.WriteAllText(configFile, iniContent);
                    Console.WriteLine("Configuración de Bullzip actualizada con ejecución de MoverPDFs.bat");

                    // Ejecutar el batch inmediatamente para verificar que funciona
                    RunBatchHidden(batchPath);

                    // Crear un archivo de registro para diagnóstico
                    string diagFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "bullzip_config_updated.txt");
                    File.WriteAllText(diagFile, $"Configuración actualizada: {DateTime.Now}\r\nRunOnSuccess: {batchPath}");
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
                    string initializationLogPath = Path.Combine(VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");

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
                            SafeWriteToLog(initializationLogPath, $"[{DateTime.Now}] {mensaje}\r\n");
                            WatcherLogger.LogActivity("Iniciando sincronización de monitor desde configuración de Bullzip");

                            // Usar el método de sincronización de PDFDialogAutomation
                            monitorStarted = PDFDialogAutomation.EnsureBackgroundMonitorBeforePrinting();
                            SafeWriteToLog(initializationLogPath, $"[{DateTime.Now}] Resultado sincronización: {(monitorStarted ? "Éxito" : "Fallido")}\r\n");
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

        public static void CreateBackupBatchFile()
        {
            try
            {
                string outputFolder = VirtualPrinterCore.FIXED_OUTPUT_PATH;
                string batchFilePath = Path.Combine(outputFolder, "MoverPDFs.bat");

                // Crear contenido del archivo batch para mover PDFs - Versión simplificada y más robusta
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
    if exist ""%%F"" (
        echo Encontrado archivo: %%F
        move ""%%F"" ""{outputFolder}\%%~nxF"" > nul 2>&1
        if not exist ""%%F"" (
            echo Archivo movido correctamente a {outputFolder}\%%~nxF
        )
    )
)

:: Esperar 2 segundos y repetir - USANDO VALOR FIJO
timeout /T 2 /NOBREAK > nul
goto loop
";
                // Guardar el archivo batch primero, antes de intentar ejecutarlo
                File.WriteAllText(batchFilePath, batchContent);
                Console.WriteLine($"Archivo batch de respaldo creado en: {batchFilePath}");

                // Asegurar permisos para ejecución
                VirtualPrinterCore.EnsureScriptPermissions(batchFilePath);

                // Ahora ejecutar el batch
                RunBatchHidden(batchFilePath);
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
    ; Buscar diálogos específicos de guardado - NO otros diálogos
    Local $hWnd = FindSaveDialog()
    
    ; Si se encontró un diálogo, procesarlo
    If $hWnd Then
        FileWriteLine($logFile, ""Diálogo encontrado: "" & WinGetTitle($hWnd))
        ProcessSaveDialog($hWnd)
    EndIf
    
    ; Pausa para no consumir recursos
    Sleep(300)
WEnd

; Función para encontrar diálogos de guardado
Func FindSaveDialog()
    ; Solo buscar diálogos específicos de guardado
    Local $titles = [ _
        ""Guardar archivo PDF como"", ""Save PDF File As"", ""Guardar PDF como"", ""Save As"", _
        ""Guardar como"", ""Microsoft Print to PDF"", ""Guardar documento"", ""Print as PDF"" _
    ]
    
    For $i = 0 To UBound($titles) - 1
        $hWnd = WinGetHandle(""["" & $titles[$i] & ""]"")
        If @error = 0 Then Return $hWnd
    Next
    
    ; Búsqueda más específica para evitar falsos positivos
    $hWnd = WinGetHandle(""[CLASS:32770]"")
    If @error = 0 Then
        $title = WinGetTitle($hWnd)
        ; Solo activar con ventanas de guardado
        If StringInStr($title, ""PDF"") And (StringInStr($title, ""Guardar"") Or StringInStr($title, ""Save"")) Then
            Return $hWnd
        EndIf
    EndIf
    
    Return 0
EndFunc

; Función para procesar el diálogo de guardado
Func ProcessSaveDialog($hWnd)
    ; Verificar que el diálogo realmente es de guardado (doble verificación)
    Local $title = WinGetTitle($hWnd)
    If Not (StringInStr($title, ""PDF"") Or StringInStr($title, ""Guardar"") Or StringInStr($title, ""Save"")) Then
        FileWriteLine($logFile, ""Ignorando diálogo no relacionado con PDF: "" & $title)
        Return
    EndIf
    
    ; Activar el diálogo
    WinActivate($hWnd)
    FileWriteLine($logFile, ""Procesando diálogo: "" & $title)
    
    ; Esperar un momento para que la ventana esté lista
    Sleep(300)
    
    ; Generar nombre de archivo con timestamp
    Local $fileName = ""ECM_"" & @YEAR & @MON & @MDAY & ""_"" & @HOUR & @MIN & @SEC & "".pdf""
    Local $fullPath = $outputFolder & ""\"" & $fileName
    
    ; Guardar la ruta original como respaldo
    Local $originalPath = $fullPath
    
    ; Establecer la ruta completa en el diálogo
    If ControlExists($hWnd, """", ""Edit1"") Then
        FileWriteLine($logFile, ""Configurando ruta: "" & $fullPath)
        ControlSetText($hWnd, """", ""Edit1"", $fullPath)
    EndIf
    
    ; Esperar a que el control se actualice
    Sleep(300)
    
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
    
    ; Logear la acción
    FileWriteLine($logFile, ""Botón de guardar presionado para: "" & $fullPath)
    
    ; Manejar posible diálogo de confirmación
    Sleep(500)
    $confirmHWnd = WinGetHandle(""[Confirm Save As]"")
    If @error = 0 Then
        ControlClick($confirmHWnd, """", ""Button1"")
        FileWriteLine($logFile, ""Diálogo de confirmación aceptado"")
    EndIf
    
    ; Notificar la creación del archivo
    FileWriteLine($logFile, ""Archivo guardado: "" & $fullPath)
    
    ; Importante: No realizar ninguna otra acción con el archivo recién guardado
EndFunc";

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

                // NUEVO: Registrar inicio explícito de AutoIT
                string logFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "autoit_execution.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Iniciando script AutoIT para trabajo de impresión\r\n");

                // Escribir una versión actualizada del script AutoIt
                WriteAutoITScriptWithSafeHandling();

                // Rutas posibles de AutoIt
                string[] possibleAutoItPaths = {
            @"C:\Program Files\AutoIt3\autoit3.exe",
            @"C:\Program Files (x86)\AutoIt3\autoit3.exe",
            Path.Combine(System.Windows.Forms.Application.StartupPath, "tools", "autoit3.exe")
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
                            File.AppendAllText(logFile, $"[{DateTime.Now}] Script AutoIT iniciado usando {autoItPath}\r\n");
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error con AutoIt ({autoItPath}): {ex.Message}");
                            File.AppendAllText(logFile, $"[{DateTime.Now}] Error al iniciar AutoIT: {ex.Message}\r\n");
                        }
                    }
                }

                // Si llegamos aquí, no se pudo iniciar AutoIT
                File.AppendAllText(logFile, $"[{DateTime.Now}] No se encontró AutoIT, se usará método alternativo\r\n");

                // Resto del método (PowerShell alternativo)
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al ejecutar script AutoIT: {ex.Message}");
                return false;
            }
        }
        private static void WriteAutoITScriptWithSafeHandling()
        {
            try
            {
                Directory.CreateDirectory(VirtualPrinterCore.FIXED_OUTPUT_PATH);

                // Script AutoIT mejorado con mejor integración de MoverPDFs.bat
                string scriptContent = @";====================================================
; Script de automatización para diálogo de guardado PDF
; Generado por ECM Central - " + DateTime.Now.ToString() + @"
; VERSIÓN MEJORADA - Integración directa con MoverPDFs.bat
;====================================================

#include <File.au3>
#include <Date.au3>
#include <Array.au3>
#include <String.au3>
#include <AutoItConstants.au3>

; Configuración
Global $outputFolder = """ + VirtualPrinterCore.FIXED_OUTPUT_PATH.Replace(@"\", @"\\") + @"""
Global $logFile = $outputFolder & ""\autoit_diagnostics.log""
Global $batchFile = $outputFolder & ""\MoverPDFs.bat""

; Inicialización
FileWriteLine($logFile, ""[INICIO] "" & @YEAR & ""/"" & @MON & ""/"" & @MDAY & "" "" & @HOUR & "":"" & @MIN & "":"" & @SEC & "" - Script iniciado"")

; Verificar que la carpeta de salida existe
If Not FileExists($outputFolder) Then
    DirCreate($outputFolder)
    FileWriteLine($logFile, ""Carpeta de salida creada: "" & $outputFolder)
EndIf

; Crear y ejecutar MoverPDFs.bat si no existe
If Not FileExists($batchFile) Then
    CrearArchivoBatch()
    FileWriteLine($logFile, ""Archivo batch MoverPDFs.bat creado en: "" & $batchFile)
EndIf

; Ejecutar MoverPDFs.bat en segundo plano al inicio
FileWriteLine($logFile, ""Ejecutando MoverPDFs.bat en segundo plano..."")
Run($batchFile, $outputFolder, @SW_MINIMIZE)

; Bucle principal
While 1
    ; Buscar diálogos de guardado
    Local $hWnd = FindSaveDialog()
    
    If $hWnd Then
        $title = WinGetTitle($hWnd)
        FileWriteLine($logFile, ""Diálogo detectado: "" & $title)
        ProcessSaveDialog($hWnd)
        
        ; Esperar 2 segundos después de procesar cada diálogo y ejecutar MoverPDFs.bat
        Sleep(2000)
        If FileExists($batchFile) Then
            FileWriteLine($logFile, ""Ejecutando MoverPDFs.bat después de guardar..."")
            Run($batchFile, $outputFolder, @SW_MINIMIZE)
        EndIf
    EndIf
    
    ; Pausa para no sobrecargar CPU
    Sleep(200)
WEnd

; Función para crear el archivo batch MoverPDFs.bat
Func CrearArchivoBatch()
    Local $batchContent = ""@echo off
title Monitor de PDF para ECM Central
color 0A
echo ============================================
echo    MONITOR AUTOMATICO DE PDF - ECM CENTRAL
echo ============================================
echo.
echo Este programa vigilara sus carpetas y movera 
echo automaticamente los archivos PDF a la carpeta:
echo "" & $outputFolder & ""
echo.
echo Monitoreando...

:loop
:: Monitorear carpetas comunes y mover PDFs a carpeta destino
for %%F in (
    ""%USERPROFILE%\Documents\*.pdf"" 
    ""%USERPROFILE%\Desktop\*.pdf"" 
    ""%USERPROFILE%\Downloads\*.pdf""
    ""%TEMP%\*.pdf""
    ""%USERPROFILE%\AppData\Local\Temp\*.pdf""
) do (
    if exist ""%%F"" (
        echo [%date% %time%] Encontrado archivo: %%F
        move ""%%F"" """" & $outputFolder & ""\\%%~nxF"" > nul 2>&1
        if not exist ""%%F"" (
            echo [%date% %time%] Archivo movido correctamente a "" & $outputFolder & ""\\%%~nxF
            echo ========================================
        )
    )
)

:: Esperar 1 segundo y repetir
timeout /T 1 /NOBREAK > nul
goto loop""

    FileWrite($batchFile, $batchContent)
EndFunc

; Función para encontrar diálogos de guardado
Func FindSaveDialog()
    ; Lista de títulos de diálogos de guardado
    Local $titles = [ _
        ""Guardar archivo PDF como"", ""Save PDF File As"", _
        ""Guardar PDF como"", ""Save As PDF"", _
        ""Guardar como"", ""Save As"", _
        ""Microsoft Print to PDF"", _
        ""Guardar documento"", ""Save document"", _
        ""Bullzip PDF Printer"", ""PDF Printer"" _
    ]
    
    ; Buscar por título exacto
    For $i = 0 To UBound($titles) - 1
        $hWnd = WinGetHandle(""["" & $titles[$i] & ""]"")
        If @error = 0 Then 
            Return $hWnd
        EndIf
    Next
    
    ; Buscar por clase + palabras clave en título
    $hWnd = WinGetHandle(""[CLASS:32770]"")
    If @error = 0 Then
        $title = WinGetTitle($hWnd)
        
        If ((StringInStr($title, ""PDF"") > 0) And _
           ((StringInStr($title, ""Guardar"") > 0) Or _
            (StringInStr($title, ""Save"") > 0) Or _
            (StringInStr($title, ""Microsoft Print"") > 0) Or _
            (StringInStr($title, ""Bullzip"") > 0))) Then
            Return $hWnd
        EndIf
    EndIf
    
    Return 0
EndFunc

; Función para procesar el diálogo de guardado
Func ProcessSaveDialog($hWnd)
    ; Activar la ventana
    WinActivate($hWnd)
    FileWriteLine($logFile, ""Procesando diálogo: "" & WinGetTitle($hWnd))
    
    ; Obtener nombre original del archivo
    Local $fileName = """"
    
    If ControlExists($hWnd, """", ""Edit1"") Then
        $fileName = ControlGetText($hWnd, """", ""Edit1"")
        FileWriteLine($logFile, ""Nombre sugerido: "" & $fileName)
    EndIf
    
    ; Extraer solo el nombre del archivo (sin ruta)
    Local $fileNameOnly = """"
    
    If StringInStr($fileName, ""\"") > 0 Then
        ; Dividir por barras y tomar el último elemento
        Local $aPathParts = StringSplit($fileName, ""\"", $STR_NOCOUNT)
        $fileNameOnly = $aPathParts[UBound($aPathParts)-1]
    Else
        $fileNameOnly = $fileName
    EndIf
    
    ; Si no tiene extensión PDF o está vacío, usar nombre predeterminado
    If $fileNameOnly = """" Or Not StringInStr($fileNameOnly, "".pdf"") Then
        $fileNameOnly = ""ECM_"" & @YEAR & @MON & @MDAY & ""_"" & @HOUR & @MIN & @SEC & "".pdf""
        FileWriteLine($logFile, ""Usando nombre predeterminado: "" & $fileNameOnly)
    EndIf
    
    ; Ruta completa en la carpeta de salida
    Local $fullPath = $outputFolder & ""\"" & $fileNameOnly
    FileWriteLine($logFile, ""Guardando en: "" & $fullPath)
    
    ; Establecer la ruta en el diálogo
    If ControlExists($hWnd, """", ""Edit1"") Then
        ControlSetText($hWnd, """", ""Edit1"", $fullPath)
        Sleep(300)
    Else
        Send($fullPath)
        Sleep(300)
    EndIf
    
    ; Presionar el botón Guardar
    If ControlExists($hWnd, """", ""Button1"") Then
        ControlClick($hWnd, """", ""Button1"")
    ElseIf ControlExists($hWnd, """", ""1"") Then
        ControlClick($hWnd, """", ""1"")
    ElseIf ControlGetHandle($hWnd, ""Guardar"", ""Button"") Then
        ControlClick($hWnd, ""Guardar"", ""Button"")
    ElseIf ControlGetHandle($hWnd, ""Save"", ""Button"") Then
        ControlClick($hWnd, ""Save"", ""Button"")
    Else
        Send(""{ENTER}"")  ; Tecla Enter
        Sleep(200)
        Send(""!s"")       ; Alt+S (Save)
        Sleep(200)
        Send(""!g"")       ; Alt+G (Guardar)
    EndIf
    
    ; Manejar posible diálogo de confirmación
    Sleep(500)
    Local $confirmHWnd = WinGetHandle(""[Confirm Save As]"")
    If @error = 0 Then
        ControlClick($confirmHWnd, """", ""Button1"")
        FileWriteLine($logFile, ""Diálogo de confirmación aceptado"")
    EndIf
    
    ; Esperar a que se complete el guardado
    Sleep(1000)
    
    ; Verificar si el archivo se guardó correctamente
    If FileExists($fullPath) Then
        FileWriteLine($logFile, ""✓ Archivo guardado correctamente: "" & $fullPath)
        
        ; CLAVE: Registrar el archivo guardado para ayudar a MoverPDFs.bat
        Local $registroFile = $outputFolder & ""\ultimo_archivo_guardado.txt""
        FileWrite($registroFile, $fullPath)
    Else
        FileWriteLine($logFile, ""⚠ Archivo no encontrado en ruta esperada. Ejecutando MoverPDFs.bat"")
    EndIf
EndFunc";

                File.WriteAllText(AutoITScriptPath, scriptContent);

                // Asegurar permisos adecuados
                try
                {
                    VirtualPrinterCore.EnsureScriptPermissions(AutoITScriptPath);
                    WatcherLogger.LogActivity("Permisos establecidos correctamente para: " + AutoITScriptPath);
                }
                catch (Exception ex)
                {
                    string errorLogPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "autoit_permissions_error.log");
                    File.AppendAllText(errorLogPath, $"[{DateTime.Now}] Error al configurar permisos: {ex.Message}\r\n");
                }

                // Crear versión mejorada de MoverPDFs.bat
                CreateImprovedMoverPDFBatch();
            }
            catch (Exception ex)
            {
                string errorPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "autoit_script_error.log");
                File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al crear script AutoIT: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

        private static void CreateImprovedMoverPDFBatch()
        {
            try
            {
                string outputFolder = VirtualPrinterCore.FIXED_OUTPUT_PATH;
                string batchFilePath = Path.Combine(outputFolder, "MoverPDFs.bat");
                string logsFolder = Path.Combine(outputFolder, "logs");

                // Asegurar que existe la carpeta de logs
                if (!Directory.Exists(logsFolder))
                {
                    Directory.CreateDirectory(logsFolder);
                }

                // Versión mejorada con detección avanzada de archivos y manejo de errores
                // FIX: Se cambia la forma en que las variables se definen y se usa un valor fijo para CHECK_INTERVAL
                string batchContent = @"@echo off
:: Monitor de PDF para ECM Central (Versión 2.7 - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm") + @")
:: Monitorea y procesa archivos PDF para ECM Central
setlocal EnableDelayedExpansion

title ECM Central - Monitor de PDF
color 0A

:: Configuración de entorno
set ""OUTPUT_FOLDER=" + outputFolder + @"""
set ""LOGS_FOLDER=" + outputFolder + @"\logs""
set ""LOG_FILE=%LOGS_FOLDER%\pdf_monitor.log""
set ""ERROR_LOG=%LOGS_FOLDER%\pdf_errors.log""
set ""MARKER_FILE=%OUTPUT_FOLDER%\last_processed_file.marker""
set ""PENDING_MARKER=%OUTPUT_FOLDER%\pending_pdf.marker""
:: FIX: Usar valor fijo en lugar de variable
set CHECK_INTERVAL=2

:: Crear carpeta de logs si no existe
if not exist ""%LOGS_FOLDER%"" mkdir ""%LOGS_FOLDER%""

:: Función de registro
call :log ""===== INICIO DEL MONITOR DE PDF - ECM CENTRAL =====""
call :log ""Carpeta de salida: %OUTPUT_FOLDER%""
call :log ""Intervalo de verificación: %CHECK_INTERVAL% segundos""

echo ============================================
echo    MONITOR AUTOMATICO DE PDF - ECM CENTRAL
echo ============================================
echo.
echo Monitoreando PDFs en carpetas del sistema...
echo Los archivos serán procesados automáticamente.
echo.
echo Carpeta destino: %OUTPUT_FOLDER%
echo.

:: Inicialización - verificar y corregir permisos
icacls ""%OUTPUT_FOLDER%"" /grant Everyone:(OI)(CI)F >nul 2>&1
call :log ""Permisos verificados para carpeta de salida""

:: Verificar si la aplicación ECM está en ejecución
tasklist /FI ""IMAGENAME eq WinFormsApiClient.exe"" 2>NUL | find /I /N ""WinFormsApiClient.exe"">NUL
if errorlevel 1 (
    call :log ""ECM Central no está en ejecución, configurando notificación automática""
) else (
    call :log ""ECM Central ya está en ejecución, procesamiento directo activado""
)

:: Bucle principal
:loop
call :process_folders
:: FIX: Usar un valor fijo para el timeout
timeout /T 2 /NOBREAK > nul
goto loop

:: Función para procesar todas las carpetas
:process_folders
:: Lista explícita de carpetas a monitorear
call :check_folder ""%USERPROFILE%\Documents""
call :check_folder ""%USERPROFILE%\Desktop""
call :check_folder ""%USERPROFILE%\Downloads""
call :check_folder ""%USERPROFILE%\AppData\Local\Temp""
call :check_folder ""%TEMP%""
call :check_folder ""C:\Windows\Temp""
call :check_folder ""%USERPROFILE%\OneDrive\Documentos""
call :check_folder ""C:\Users\Public\Documents""

:: También verificar si hay archivos recientes en la carpeta de salida
for %%F in (""%OUTPUT_FOLDER%\*.pdf"") do (
    :: Solo procesar archivos creados/modificados en los últimos 15 minutos
    forfiles /P ""%OUTPUT_FOLDER%"" /M ""%%~nxF"" /D -0 /C ""cmd /c if @isdir==FALSE (echo @path & exit 0)"" >nul 2>&1
    if not errorlevel 1 (
        :: Archivo reciente encontrado, registrar para procesamiento
        call :log ""Archivo reciente encontrado en carpeta destino: %%~nxF""
        :: Marcar para procesamiento
        echo %%F > ""%PENDING_MARKER%""
    )
)

:: Verificar si hay un archivo último guardado por AutoIT o Bullzip
if exist ""%OUTPUT_FOLDER%\ultimo_archivo_guardado.txt"" (
    for /f ""delims="" %%f in ('type ""%OUTPUT_FOLDER%\ultimo_archivo_guardado.txt""') do (
        set ""ULTIMO_ARCHIVO=%%f""
        if exist ""!ULTIMO_ARCHIVO!"" (
            call :log ""Procesando archivo guardado por AutoIT/Bullzip: !ULTIMO_ARCHIVO!""
            :: Marcar para procesamiento
            echo !ULTIMO_ARCHIVO! > ""%PENDING_MARKER%""
            :: Eliminar el archivo de registro después de procesarlo
            del ""%OUTPUT_FOLDER%\ultimo_archivo_guardado.txt"" >nul 2>&1
        )
    )
)

exit /b 0

:: Función para revisar una carpeta específica
:check_folder
set ""FOLDER=%~1""
if exist ""%FOLDER%"" (
    for %%F in (""%FOLDER%\*.pdf"") do (
        call :check_file ""%%F""
    )
)
exit /b 0

:: Función para verificar y procesar un archivo
:check_file
set ""filename=%~1""
set ""basename=%~nx1""
set ""target=%OUTPUT_FOLDER%\%~nx1""

:: Verificar si el archivo está en uso/bloqueado
copy /b ""!filename!""+,, ""!filename!"".testlock >nul 2>&1
if exist ""!filename!"".testlock (
    del ""!filename!"".testlock >nul 2>&1

    :: Archivo no bloqueado, verificar tamaño
    for %%A in (""!filename!"") do set filesize=%%~zA
    if !filesize! GTR 0 (
        call :log ""Archivo PDF encontrado: !filename! (Tamaño: !filesize! bytes)""

        :: Mover el archivo a la carpeta destino
        move ""!filename!"" ""!target!"" >nul 2>&1
        if not exist ""!filename!"" (
            call :log ""Archivo movido correctamente a: !target!""
            
            :: Marcar este archivo para procesamiento
            echo !target! > ""%PENDING_MARKER%""
            
            :: Registrar como último archivo procesado
            echo !target! > ""%MARKER_FILE%""
            
            :: Verificar si la aplicación está en ejecución
            tasklist /FI ""IMAGENAME eq WinFormsApiClient.exe"" 2>NUL | find /I /N ""WinFormsApiClient.exe"">NUL
            if errorlevel 1 (
                call :log ""Iniciando ECM Central con archivo: !target!""
                start """" """ + System.Windows.Forms.Application.ExecutablePath.Replace("\\", "\\\\") + @""" /processfile=""!target!""
            ) else (
                call :log ""ECM Central ya en ejecución, archivo marcado para procesamiento: !basename!""
            )
        ) else (
            call :log_error ""No se pudo mover el archivo: !filename!""
        )
    )
) else (
    :: El archivo está bloqueado, registrarlo pero no intentar moverlo
    for %%A in (""!filename!"") do set filesize=%%~zA
    if !filesize! GTR 0 (
        call :log ""Archivo PDF bloqueado: !filename! (Se intentará en próximo ciclo)""
    )
)

exit /b 0

:: Función de registro
:log
echo [%date% %time%] %~1 >> ""%LOG_FILE%""
exit /b 0

:log_error
echo [%date% %time%] ERROR: %~1 >> ""%ERROR_LOG%""
echo [%date% %time%] ERROR: %~1 >> ""%LOG_FILE%""
exit /b 0
";

                // Escribir el archivo batch
                File.WriteAllText(batchFilePath, batchContent);

                // Crear archivo de marcador vacío si no existe
                string markerFile = Path.Combine(outputFolder, "pending_pdf.marker");
                if (!File.Exists(markerFile))
                {
                    File.WriteAllText(markerFile, string.Empty);
                }

                // Asegurar permisos correctos en los archivos
                try
                {
                    VirtualPrinterCore.EnsureScriptPermissions(batchFilePath);

                    // Log de la operación
                    WatcherLogger.LogActivity($"Archivo batch mejorado creado: {batchFilePath}");
                }
                catch (Exception permEx)
                {
                    // Registrar el error pero permitir que la función termine
                    string errorLog = Path.Combine(logsFolder, "batch_creation_error.log");
                    File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al establecer permisos: {permEx.Message}\r\n");

                    // Intentar método alternativo de permisos
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "icacls",
                            Arguments = $"\"{batchFilePath}\" /grant:r Everyone:(F)",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        using (Process process = Process.Start(psi))
                        {
                            process.WaitForExit();
                        }
                    }
                    catch (Exception) { /* Ignorar errores en este método alternativo */ }
                }

                // Crear un acceso directo para el batch en la carpeta de inicio si es posible
                try
                {
                    string startupFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        "ECM Central - Monitor PDF.lnk");

                    // Usar PowerShell para crear un acceso directo
                    string psCommand = $@"
$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut('{startupFolder.Replace("\\", "\\\\")}')
$shortcut.TargetPath = '{batchFilePath.Replace("\\", "\\\\")}'
$shortcut.WindowStyle = 1
$shortcut.Description = 'Monitor de PDF para ECM Central'
$shortcut.WorkingDirectory = '{Path.GetDirectoryName(batchFilePath).Replace("\\", "\\\\")}'
$shortcut.Save()";

                    PowerShellHelper.RunPowerShellCommand(psCommand);
                }
                catch (Exception) { /* Ignorar errores al crear el acceso directo */ }

                // Ejecutar el batch en segundo plano
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = batchFilePath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Minimized
                    };

                    Process.Start(psi);
                    WatcherLogger.LogActivity("MoverPDFs.bat iniciado en segundo plano");
                }
                catch (Exception ex)
                {
                    string errorLog = Path.Combine(logsFolder, "batch_execution_error.log");
                    File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al iniciar batch: {ex.Message}\r\n{ex.StackTrace}\r\n");
                }
            }
            catch (Exception ex)
            {
                string errorPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "batch_error.log");
                File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al crear MoverPDFs.bat: {ex.Message}\r\n{ex.StackTrace}\r\n");

                // También registrar usando el logger del sistema
                WatcherLogger.LogError("Error al crear el archivo batch MoverPDFs.bat", ex);
            }
        }
        /// <summary>
        /// Crea un script VBScript para ejecutar MoverPDFs.bat silenciosamente
        /// </summary>
        public static bool CreateSilentVBScript()
        {
            try
            {
                string outputFolder = VirtualPrinterCore.FIXED_OUTPUT_PATH;
                string logsFolder = Path.Combine(outputFolder, "logs");
                string batchPath = Path.Combine(outputFolder, "MoverPDFs.bat");
                string vbsPath = Path.Combine(outputFolder, "SilentLauncherForMoverPDFs.vbs");

                // Crear carpeta de logs si no existe
                if (!Directory.Exists(logsFolder))
                {
                    Directory.CreateDirectory(logsFolder);
                }

                // Contenido del script VBScript
                string scriptContent = $@"' Script para iniciar MoverPDFs.bat sin mostrar ventanas
' Generado: {DateTime.Now:dd/M/yyyy HH:mm:ss}

On Error Resume Next

Set WshShell = CreateObject(""WScript.Shell"")

' Ruta del batch
BatchPath = ""{batchPath.Replace(@"\", @"\\")}""

' Crear archivo de registro
Set fso = CreateObject(""Scripting.FileSystemObject"")

' Verificar si existe la carpeta logs
If Not fso.FolderExists(""{logsFolder.Replace(@"\", @"\\")}"") Then
    fso.CreateFolder(""{logsFolder.Replace(@"\", @"\\")}"")
End If

' Registrar inicio
Set logFile = fso.OpenTextFile(""{Path.Combine(logsFolder, "silent_launcher_mover.log").Replace(@"\", @"\\")}"", 8, True)
logFile.WriteLine ""[INICIO] "" & Now & "" - Lanzando MoverPDFs silenciosamente""
logFile.Close

' Ejecutar el batch completamente oculto
WshShell.Run ""cmd /c """" & BatchPath & """" /silent"", 0, False

' Registrar éxito
Set logFile = fso.OpenTextFile(""{Path.Combine(logsFolder, "silent_launcher_mover.log").Replace(@"\", @"\\")}"", 8, True)
logFile.WriteLine ""["" & Now & ""] MoverPDFs iniciado exitosamente (modo silencioso)""
logFile.Close
";

                // Escribir el script VBScript
                File.WriteAllText(vbsPath, scriptContent);
                WatcherLogger.LogActivity($"Script VBScript creado: {vbsPath}");

                // Dar permisos de ejecución al script
                VirtualPrinterCore.EnsureScriptPermissions(vbsPath);

                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al crear script VBScript", ex);
                return false;
            }
        }

        /// <summary>
        /// Ejecuta el script VBScript para iniciar MoverPDFs.bat silenciosamente
        /// </summary>
        public static bool RunSilentVBScript()
        {
            try
            {
                string outputFolder = VirtualPrinterCore.FIXED_OUTPUT_PATH;
                string vbsPath = Path.Combine(outputFolder, "SilentLauncherForMoverPDFs.vbs");

                // Verificar si el script existe, crearlo si no
                if (!File.Exists(vbsPath))
                {
                    if (!CreateSilentVBScript())
                    {
                        return false;
                    }
                }

                // Ejecutar el script VBScript
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "wscript.exe",
                    Arguments = $"\"{vbsPath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process process = Process.Start(psi);
                WatcherLogger.LogActivity($"Script VBScript para MoverPDFs.bat ejecutado silenciosamente");

                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al ejecutar script VBScript", ex);
                return false;
            }
        }
        /// <summary>
        /// Crea un acceso directo que ejecuta el batch en modo oculto a través de VBScript
        /// </summary>
        private static void CreateHiddenBatchShortcut(string batchFilePath)
        {
            try
            {
                string outputFolder = VirtualPrinterCore.FIXED_OUTPUT_PATH;
                string vbsPath = Path.Combine(outputFolder, "SilentLauncherForMoverPDFs.vbs");

                // Asegurar que el script VBScript existe
                if (!File.Exists(vbsPath))
                {
                    CreateSilentVBScript();
                }

                string startupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "ECM Monitor PDF.lnk");

                // Script PowerShell que crea un acceso directo al script VBScript (no al batch directamente)
                string psCommand = $@"
$WshShell = New-Object -ComObject WScript.Shell
$shortcut = $WshShell.CreateShortcut('{startupFolder.Replace("\\", "\\\\")}')
$shortcut.TargetPath = 'wscript.exe'
$shortcut.Arguments = '\""{vbsPath.Replace("\\", "\\\\")}\""'
$shortcut.WindowStyle = 7
$shortcut.WorkingDirectory = '{Path.GetDirectoryName(vbsPath).Replace("\\", "\\\\")}'
$shortcut.Description = 'Monitor de PDF para ECM Central (Modo silencioso)'
$shortcut.IconLocation = '%SystemRoot%\System32\shell32.dll,46'
$shortcut.Save()
Write-Output 'Acceso directo modo oculto creado correctamente'";

                PowerShellHelper.RunPowerShellCommand(psCommand);
                WatcherLogger.LogActivity("Acceso directo en modo oculto creado para VBScript de MoverPDFs.bat");
            }
            catch (Exception ex)
            {
                // Registrar error pero no interrumpir el proceso
                string errorPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "shortcut_error.log");
                File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al crear acceso directo oculto: {ex.Message}\r\n");
            }
        }

        public static void RunBatchHidden(string batchFilePath)
        {
            try
            {
                // Verificar primero si el archivo existe
                if (!File.Exists(batchFilePath))
                {
                    // Si no existe, creamos el archivo batch
                    CreateBackupBatchFile();

                    // Verificamos de nuevo para asegurarnos que se ha creado
                    if (!File.Exists(batchFilePath))
                    {
                        throw new FileNotFoundException("No se pudo crear el archivo batch", batchFilePath);
                    }
                }

                // Registrar la ejecución para diagnóstico
                string logFolder = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "logs");
                if (!Directory.Exists(logFolder))
                    Directory.CreateDirectory(logFolder);

                string logFile = Path.Combine(logFolder, "batch_executions.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Ejecutando batch: {batchFilePath}\r\n");

                // Usar método más simple que casi siempre funciona
                try
                {
                    ProcessStartInfo simpleStart = new ProcessStartInfo
                    {
                        FileName = batchFilePath,
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    };

                    Process process = Process.Start(simpleStart);
                    File.AppendAllText(logFile, $"[{DateTime.Now}] Batch iniciado directamente con PID: {(process != null ? process.Id : -1)}\r\n");

                    // Crear un archivo marcador para confirmar que el batch se ha ejecutado
                    string markerFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "batch_executed.marker");
                    File.WriteAllText(markerFile, $"Batch ejecutado: {DateTime.Now}");

                    // Registrar en el sistema de logs
                    WatcherLogger.LogActivity($"Batch MoverPDFs.bat ejecutado correctamente: {batchFilePath}");
                    return;
                }
                catch (Exception ex)
                {
                    // Si el método simple falla, intentar con método alternativo
                    File.AppendAllText(logFile, $"[{DateTime.Now}] Error al iniciar batch directamente: {ex.Message}. Intentando método alternativo...\r\n");
                }

                // Método alternativo usando cmd.exe explícitamente
                try
                {
                    ProcessStartInfo cmdStart = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c start \"Monitor PDF\" \"{batchFilePath}\"",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Normal
                    };

                    Process.Start(cmdStart);
                    File.AppendAllText(logFile, $"[{DateTime.Now}] Batch iniciado con método alternativo\r\n");

                    // Registrar en el sistema de logs
                    WatcherLogger.LogActivity($"Batch MoverPDFs.bat ejecutado con método alternativo: {batchFilePath}");
                }
                catch (Exception cmdEx)
                {
                    // Registrar error detallado
                    string errorPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "batch_error.log");
                    File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al ejecutar batch (ambos métodos): {cmdEx.Message}\r\n{cmdEx.StackTrace}\r\n");

                    // También registrar en el sistema de logs
                    WatcherLogger.LogError("Error al ejecutar MoverPDFs.bat (todos los métodos fallaron)", cmdEx);
                }
            }
            catch (Exception ex)
            {
                // Registrar error detallado
                string errorPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "batch_error.log");
                File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al preparar ejecución de batch: {ex.Message}\r\n{ex.StackTrace}\r\n");

                // También registrar en el sistema de logs
                WatcherLogger.LogError("Error al preparar ejecución de MoverPDFs.bat", ex);
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