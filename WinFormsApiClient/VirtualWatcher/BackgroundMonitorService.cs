using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase que maneja el servicio de monitoreo en segundo plano
    /// </summary>
    public static class BackgroundMonitorService
    {
        public static bool _isInstalled = false;
        public static bool _isRunning = false;
        private static System.Threading.Timer _heartbeatTimer;
        private static readonly string _startupKey = "ECMCentralPrinterMonitor";

        /// <summary>
        /// Inicia el servicio de monitoreo en segundo plano
        /// </summary>
        public static bool Start()
        {
            try
            {
                // Ubicación única para registro de diagnóstico
                string diagPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "monitor_start_diag.log");
                File.AppendAllText(diagPath, $"[{DateTime.Now}] Inicio de método Start() en BackgroundMonitorService\r\n");

                // PASO 1: Verificar si ya hay una instancia activa mediante varios métodos
                // ---------------------------------------------------------------------

                // 1.1 - Verificar flag local
                if (_isRunning)
                {
                    File.AppendAllText(diagPath, $"[{DateTime.Now}] Flag local _isRunning = true, saliendo\r\n");
                    return true; // Ya estamos ejecutando en este mismo proceso
                }

                // 1.2 - Verificar con método robusto de comprobación
                if (IsActuallyRunning())
                {
                    File.AppendAllText(diagPath, $"[{DateTime.Now}] IsActuallyRunning() detectó instancia activa, saliendo\r\n");
                    _isRunning = true; // Actualizar flag local para coherencia
                    return true;
                }

                // 1.3 - Verificación directa por procesos con argumento específico
                int runningMonitorPid = -1;
                var processes = Process.GetProcessesByName("WinFormsApiClient");

                foreach (var proc in processes)
                {
                    try
                    {
                        // No contar el proceso actual
                        if (proc.Id != Process.GetCurrentProcess().Id)
                        {
                            string cmdLine = GetCommandLine(proc.Id);
                            if (cmdLine.Contains("/backgroundmonitor"))
                            {
                                runningMonitorPid = proc.Id;
                                File.AppendAllText(diagPath, $"[{DateTime.Now}] Encontrado proceso activo con PID {runningMonitorPid}, saliendo\r\n");
                                _isRunning = true; // Actualizar flag por coherencia
                                return true;
                            }
                        }
                    }
                    catch { /* Ignorar errores individuales */ }
                }

                // PASO 2: Limpiar estado anterior y crear marcadores nuevos
                // ---------------------------------------------------------------------

                File.AppendAllText(diagPath, $"[{DateTime.Now}] No se detectaron instancias activas, procediendo con inicialización\r\n");

                // 2.1 - Limpiar marcadores obsoletos
                CleanupOldMarkers();

                // 2.2 - Crear marcador de ejecución actualizado (solo PID, sin información adicional)
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
                File.WriteAllText(markerFilePath, Process.GetCurrentProcess().Id.ToString());
                File.AppendAllText(diagPath, $"[{DateTime.Now}] Creado marcador en {markerFilePath} con PID {Process.GetCurrentProcess().Id}\r\n");

                // 2.3 - Configurar eliminación automática del marcador al salir
                AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                    try
                    {
                        if (File.Exists(markerFilePath))
                            File.Delete(markerFilePath);
                    }
                    catch { /* Ignorar errores al eliminar */ }
                };

                // PASO 3: Inicializar componentes del sistema
                // ---------------------------------------------------------------------

                WatcherLogger.LogActivity("Iniciando servicio de monitoreo en segundo plano");

                // 3.1 - Verificar estado de la impresora
                if (!ECMVirtualPrinter.IsPrinterInstalled())
                {
                    WatcherLogger.LogActivity("Impresora no instalada, intentando instalar...");
                    File.AppendAllText(diagPath, $"[{DateTime.Now}] Impresora no instalada, iniciando instalación\r\n");
                    Task.Run(async () => await PrinterInstaller.InstallPrinterAsync(true)).Wait();
                }

                // 3.2 - Configuración de carpetas y automatización
                EnsureOutputAndAutomation(); // Esta función NO debe crear MoverPDFs.bat automáticamente

                // PASO 4: Iniciar servicios y monitores
                // ---------------------------------------------------------------------

                // 4.1 - Iniciar solo FileMonitor (es el componente esencial)
                WatcherLogger.LogActivity("Iniciando monitor de archivos PDF");
                FileMonitor.Instance.StartMonitoring();

                // 4.2 - Verificación inicial única de PDFs existentes
                WatcherLogger.LogActivity("Ejecutando verificación inicial de archivos existentes");
                FileMonitor.Instance.CheckForNewPDFs();

                // 4.3 - Configurar heartbeat reducido (cada 60 segundos en vez de 30)
                _heartbeatTimer = new System.Threading.Timer(SendHeartbeat, null, 0, 60000);

                // PASO 5: Finalizar inicialización y marcar como activo
                // ---------------------------------------------------------------------

                _isRunning = true;
                WatcherLogger.LogActivity("Servicio de monitoreo iniciado correctamente");

                // 5.1 - Crear marcador de servicio activo (registro adicional)
                try
                {
                    string readyMarkerPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "monitor_active.marker");
                    File.WriteAllText(readyMarkerPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    File.AppendAllText(diagPath, $"[{DateTime.Now}] Inicialización completada con éxito\r\n");
                }
                catch (Exception ex)
                {
                    File.AppendAllText(diagPath, $"[{DateTime.Now}] Error al crear marcador de servicio activo: {ex.Message}\r\n");
                }

                // Registrar diagnóstico completo del sistema
                WatcherLogger.LogSystemDiagnostic();

                // DESPUÉS DE VERIFICAR QUE NO HAY INSTANCIAS ACTIVAS
                // AGREGAR ESTA LLAMADA:
                
                // CONFIGURACIÓN COMPLETA AL INICIAR EL MONITOR
                Console.WriteLine("Ejecutando configuración automática del sistema...");
                ForceSystemAutoConfiguration();
                
                return true;
            }
            catch (Exception ex)
            {
                // Registrar error en archivo de diagnóstico independiente
                try
                {
                    string errorPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "monitor_error.log");
                    File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al iniciar monitor: {ex.Message}\r\n{ex.StackTrace}\r\n");
                }
                catch { /* Si falla el registro, no podemos hacer más */ }

                WatcherLogger.LogError("Error crítico al iniciar servicio de monitoreo", ex);
                return false;
            }
        }
        private static void CheckForNewPdfFiles()
        {
            try
            {
                string outputFolder = VirtualPrinterCore.GetOutputFolderPath();
                if (!Directory.Exists(outputFolder))
                    return;

                // Obtener archivos recientes (últimos 30 segundos)
                var recentFiles = Directory.GetFiles(outputFolder, "*.pdf")
                    .Select(f => new FileInfo(f))
                    .Where(f => (DateTime.Now - f.CreationTime).TotalSeconds < 30)
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                if (recentFiles.Count > 0)
                {
                    string newestFile = recentFiles[0].FullName;
                    Console.WriteLine($"Nuevo archivo PDF detectado: {newestFile}");

                    // Crear marcador para que la aplicación principal lo procese
                    VirtualPrinterCore.CreatePendingFileMarker(newestFile);

                    // Si la aplicación principal está en ejecución, notificarle
                    Process[] processes = Process.GetProcessesByName(
                        Path.GetFileNameWithoutExtension(Application.ExecutablePath));

                    if (processes.Length > 1) // Más de una instancia (esta misma cuenta como una)
                    {
                        // La aplicación principal ya está en ejecución
                        Console.WriteLine("Aplicación principal en ejecución, marcador creado para procesamiento");
                    }
                    else
                    {
                        // Iniciar la aplicación principal
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = Application.ExecutablePath,
                            Arguments = $"/print:\"{newestFile}\"",
                            UseShellExecute = true
                        };
                        Process.Start(startInfo);
                        Console.WriteLine("Aplicación principal iniciada para procesar archivo");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar nuevos PDFs: {ex.Message}");
            }
        }
        /// <summary>
        /// Limpia marcadores antiguos que podrían causar falsa detección de monitores activos
        /// </summary>
        private static void CleanupOldMarkers()
        {
            try
            {
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
                if (File.Exists(markerFilePath))
                {
                    string content = File.ReadAllText(markerFilePath);
                    if (int.TryParse(content, out int pid))
                    {
                        try
                        {
                            // Verificar si el proceso existe
                            Process.GetProcessById(pid);
                            // No hacer nada - proceso existe
                        }
                        catch
                        {
                            // Proceso no existe, eliminar marcador
                            File.Delete(markerFilePath);
                        }
                    }
                    else
                    {
                        // Contenido no válido, eliminar marcador
                        File.Delete(markerFilePath);
                    }
                }
            }
            catch { /* Ignorar errores */ }
        }
        /// <summary>
        /// Inicia el servicio de monitoreo y notifica cuando está listo (para sincronización con Bullzip)
        /// </summary>
        public static void StartAndNotify()
        {
            try
            {
                // Crear archivo en la carpeta de logs
                string initLogPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");
                File.AppendAllText(initLogPath, $"[{DateTime.Now}] Iniciando servicio de monitoreo en modo sincronizado\r\n");

                // Crear archivo adicional en la carpeta fija
                string fixedLogPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "monitor_start.log");
                File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] Iniciando servicio de monitoreo en modo sincronizado\r\n");

                // Iniciar el servicio como siempre
                Start();

                // Si se inició correctamente, crear un archivo marcador para notificar que está listo
                if (_isRunning)
                {
                    string mensaje = "Monitor iniciado correctamente y listo para usar";
                    File.AppendAllText(initLogPath, $"[{DateTime.Now}] ✓ {mensaje}\r\n");
                    File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] ✓ {mensaje}\r\n");

                    string markerPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "monitor_ready.marker");
                    File.WriteAllText(markerPath, $"Monitor iniciado en: {DateTime.Now}");

                    WatcherLogger.LogActivity("Monitor listo - archivo de sincronización creado");
                }
                else
                {
                    string mensaje = "No se pudo iniciar el monitor de fondo";
                    File.AppendAllText(initLogPath, $"[{DateTime.Now}] ❌ ERROR: {mensaje}\r\n");
                    File.AppendAllText(fixedLogPath, $"[{DateTime.Now}] ❌ ERROR: {mensaje}\r\n");
                    WatcherLogger.LogError("El monitor no se inició correctamente", new Exception("Fallo de inicio"));
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al iniciar servicio con notificación", ex);

                try
                {
                    string initLogPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");
                    File.AppendAllText(initLogPath, $"[{DateTime.Now}] ❌ ERROR al iniciar servicio: {ex.Message}\r\n");
                }
                catch { }
            }
        }
        public static bool IsActuallyRunning()
        {
            try
            {
                // Comprobar el flag interno
                if (_isRunning)
                    return true;

                // Verificar archivo de marcador
                string markerFile = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
                if (File.Exists(markerFile))
                {
                    try
                    {
                        string pidContent = File.ReadAllText(markerFile);
                        if (int.TryParse(pidContent, out int pid))
                        {
                            try
                            {
                                Process process = Process.GetProcessById(pid);
                                if (process != null && !process.HasExited)
                                {
                                    // Verificar línea de comando
                                    string cmdLine = GetCommandLine(pid);
                                    if (cmdLine.Contains("backgroundmonitor"))
                                        return true;
                                }
                            }
                            catch { /* El proceso no existe */ }
                        }

                        // Si llegamos aquí, el marcador está obsoleto
                        try { File.Delete(markerFile); } catch { }
                    }
                    catch { /* Ignorar error leyendo marcador */ }
                }

                // Última comprobación: buscar procesos directamente
                var processes = Process.GetProcessesByName("WinFormsApiClient");
                foreach (var proc in processes)
                {
                    try
                    {
                        // No contar este proceso
                        if (proc.Id != Process.GetCurrentProcess().Id)
                        {
                            string cmdLine = GetCommandLine(proc.Id);
                            if (cmdLine.Contains("backgroundmonitor"))
                                return true;
                        }
                    }
                    catch { /* Ignorar errores */ }
                }

                return false;
            }
            catch
            {
                return _isRunning; // Fallback
            }
        }


        /// <summary>
        /// Obtiene la línea de comando usada para iniciar un proceso
        /// </summary>
        private static string GetCommandLine(int processId)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    $"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {processId}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj["CommandLine"]?.ToString() ?? string.Empty;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
        /// <summary>
        /// Verifica y asegura que la carpeta de salida exista y la automatización esté activa
        /// </summary>
        private static void EnsureOutputAndAutomation()
        {
            try
            {
                WatcherLogger.LogActivity("Verificando estado de carpeta de salida y automatización...");

                // NUEVO: Inicializar Bullzip como primera prioridad al inicio del servicio
                bool bullzipInitSuccess = PDFDialogAutomation.InitBullzipAtStartup();
                WatcherLogger.LogActivity($"Inicialización de Bullzip al inicio: {(bullzipInitSuccess ? "Exitosa" : "Fallida")}");

                // Si Bullzip fue inicializado correctamente, ajustar configuración adicional
                if (bullzipInitSuccess)
                {
                    WatcherLogger.LogActivity("Bullzip configurado como impresora principal");

                    // Verificar que Bullzip esté listo para imprimir
                    PDFDialogAutomation.VerifyBullzipForPrinting();
                }

                // Configurar la impresora para guardar directamente en la carpeta de destino
                VirtualPrinter.VirtualPrinterCore.SetupPrinterConfiguration();

                // Asegurar que exista la carpeta de salida
                string outputFolder = VirtualPrinter.VirtualPrinterCore.GetOutputFolderPath();

                if (!Directory.Exists(outputFolder))
                {
                    WatcherLogger.LogActivity($"Creando carpeta de salida: {outputFolder}");
                    Directory.CreateDirectory(outputFolder);
                }
                else
                {
                    WatcherLogger.LogActivity($"Carpeta de salida existe: {outputFolder}");
                }

                // Verificar proceso de automatización
                if (VirtualPrinter.VirtualPrinterCore.DRIVER_NAME == "Microsoft Print To PDF")
                {
                    WatcherLogger.LogActivity("Verificando estado de automatización para Microsoft Print to PDF");

                    try
                    {
                        // Verificar si ya hay un proceso de automatización ejecutándose
                        string processName = Path.GetFileNameWithoutExtension(PDFDialogAutomation.AutoITCompiledPath);
                        Process[] processes = Process.GetProcessesByName(processName);

                        if (processes.Length > 0)
                        {
                            WatcherLogger.LogActivity($"Proceso de automatización ya en ejecución (PID: {processes[0].Id})");
                        }
                        else
                        {
                            // Intentar varios métodos para iniciar la automatización
                            // 1. Método principal - usar StartDialogAutomation
                            WatcherLogger.LogActivity("No se detectó proceso de automatización, iniciando uno nuevo");
                            bool success = PDFDialogAutomation.StartDialogAutomation();
                            WatcherLogger.LogActivity($"Resultado de inicio de automatización: {(success ? "Éxito" : "Fallido")}");

                            // 2. Si falla, crear y ejecutar un batch como respaldo
                            if (!success)
                            {
                                WatcherLogger.LogActivity("Intentando método alternativo - creando batch file");
                                PDFDialogAutomation.CreateBackupBatchFile();

                                // 3. Crear un archivo de instrucciones para usuarios
                                string instructionsFile = Path.Combine(outputFolder, "INSTRUCCIONES_PDF.txt");
                                string instructions =
        $@"INSTRUCCIONES PARA GUARDAR ARCHIVOS PDF EN ECM CENTRAL

Al imprimir con la impresora 'ECM Central':

1. Cuando aparezca el diálogo de guardar, guarde los archivos en:
   {outputFolder}

2. Los archivos deben tener extensión .pdf

Si tiene problemas, contacte al soporte técnico.
";
                                File.WriteAllText(instructionsFile, instructions);
                                WatcherLogger.LogActivity($"Archivo de instrucciones creado en: {instructionsFile}");
                            }
                        }
                    }
                    catch (Exception exProc)
                    {
                        WatcherLogger.LogError("Error al verificar proceso de automatización", exProc);
                    }
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en verificación de carpeta y automatización", ex);
            }
        }

        /// <summary>
        /// Detiene el servicio de monitoreo
        /// </summary>
        public static void Stop()
        {
            try
            {
                WatcherLogger.LogActivity("Deteniendo servicio de monitoreo");

                FileMonitor.Instance.StopMonitoring();

                // Detener el timer de heartbeat
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;

                _isRunning = false;
                WatcherLogger.LogActivity("Servicio de monitoreo detenido");
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al detener servicio de monitoreo", ex);
            }
        }

        /// <summary>
        /// Instala el servicio para que se inicie automáticamente con Windows
        /// </summary>
        public static bool InstallAutostart()
{
    try
    {
        using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
        {
            if (key != null)
            {
                // Primero eliminar cualquier entrada existente para evitar duplicados
                try
                {
                    key.DeleteValue(_startupKey, false);
                }
                catch { /* Ignorar si no existe */ }

                // Agregar UNA SOLA entrada con argumentos específicos
                string startupCommand = $"\"{Application.ExecutablePath}\" /backgroundmonitor /silent";
                key.SetValue(_startupKey, startupCommand);
                
                // Verificar que se guardó correctamente
                string savedValue = key.GetValue(_startupKey)?.ToString();
                if (savedValue == startupCommand)
                {
                    Console.WriteLine("Autostart instalado correctamente");
                    return true;
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error instalando autostart: {ex.Message}");
    }
    return false;
}

        /// <summary>
        /// Desinstala el servicio del autoarranque
        /// </summary>
        public static bool UninstallAutostart()
        {
            try
            {
                WatcherLogger.LogActivity("Desinstalando servicio de autoarranque");

                // Limpiar archivos temporales que pueden causar errores de script
                try
                {
                    string tempPath = Path.GetTempPath();
                    foreach (string file in Directory.GetFiles(tempPath, "invisible_launcher_*.vbs"))
                    {
                        try
                        {
                            File.Delete(file);
                            WatcherLogger.LogActivity($"Eliminado archivo temporal: {file}");
                        }
                        catch (Exception ex)
                        {
                            WatcherLogger.LogError($"No se pudo eliminar archivo temporal: {file}", ex);
                        }
                    }
                }
                catch (Exception cleanEx)
                {
                    WatcherLogger.LogError("Error al limpiar archivos temporales", cleanEx);
                    // Continuar con la desinstalación aunque falle la limpieza
                }

                // Eliminar la entrada del registro si existe
                bool registryRemoved = false;
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
                {
                    if (key != null)
                    {
                        if (key.GetValue(_startupKey) != null)
                        {
                            key.DeleteValue(_startupKey);
                            registryRemoved = true;
                            WatcherLogger.LogActivity("Entrada de registro eliminada correctamente");
                        }
                    }
                }

                // Eliminar el acceso directo de la carpeta de inicio si existe
                bool shortcutRemoved = false;
                try
                {
                    string startupFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                        "ECM Monitor.lnk");

                    if (File.Exists(startupFolder))
                    {
                        File.Delete(startupFolder);
                        shortcutRemoved = true;
                        WatcherLogger.LogActivity("Acceso directo eliminado correctamente");
                    }
                }
                catch (Exception shortcutEx)
                {
                    WatcherLogger.LogError("Error al eliminar acceso directo", shortcutEx);
                }

                // Eliminar tarea programada
                try
                {
                    string psCommand = @"
try {
    $taskName = 'ECMCentralMonitor'
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false -ErrorAction SilentlyContinue
    Write-Output 'Tarea programada eliminada correctamente'
    return $true
} catch {
    Write-Output ""Error al eliminar tarea programada: $($_.Exception.Message)""
    return $false
}";

                    string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                    bool taskRemoved = result.Contains("correctamente");

                    if (taskRemoved)
                        WatcherLogger.LogActivity("Tarea programada eliminada correctamente");
                }
                catch (Exception taskEx)
                {
                    WatcherLogger.LogError("Error al eliminar tarea programada", taskEx);
                }

                _isInstalled = false;
                WatcherLogger.LogActivity("Servicio desinstalado correctamente del autoarranque");

                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al desinstalar servicio de autoarranque", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifica si el servicio está instalado para autoarranque
        /// </summary>
        public static bool IsInstalledForAutostart()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false))
                {
                    if (key != null)
                    {
                        return key.GetValue(_startupKey) != null;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Envía un heartbeat al archivo de log para demostrar que el servicio sigue activo
        /// </summary>
        private static void SendHeartbeat(object state)
        {
            try
            {
                WatcherLogger.LogActivity("Monitor de impresión activo - Heartbeat");

                // Verificar si hay nuevos trabajos de impresión
                CheckPrintQueue();
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en heartbeat", ex);
            }
        }

        /// <summary>
        /// Verifica si hay nuevos trabajos de impresión
        /// </summary>
        private static void CheckPrintQueue()
        {
            try
            {
                // NUEVO: Verificar que Bullzip esté configurado correctamente antes de cada trabajo
                PDFDialogAutomation.VerifyBullzipForPrinting();

                // Usar PowerShell para verificar la cola de impresión
                string psCommand = $@"
    Get-PrintJob -PrinterName '{ECMVirtualPrinter.PRINTER_NAME}' -ErrorAction SilentlyContinue | 
    Select-Object JobId, DocumentName, JobStatus | 
    ConvertTo-Json";

                using (Process process = new Process())
                {
                    process.StartInfo = new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-Command \"{psCommand}\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (!string.IsNullOrWhiteSpace(output) && output.Contains("DocumentName"))
                    {
                        WatcherLogger.LogActivity("Trabajos de impresión detectados:");
                        WatcherLogger.LogActivity(output);

                        // Revisar estado de las carpetas de salida
                        WatcherLogger.LogActivity($"Verificando carpeta de salida principal: {VirtualPrinter.VirtualPrinterCore.GetOutputFolderPath()}");
                        if (!Directory.Exists(VirtualPrinter.VirtualPrinterCore.GetOutputFolderPath()))
                        {
                            WatcherLogger.LogActivity("¡La carpeta de salida no existe! Creándola...");
                            Directory.CreateDirectory(VirtualPrinter.VirtualPrinterCore.GetOutputFolderPath());
                        }

                        // Forzar una verificación de archivos nuevos
                        WatcherLogger.LogActivity("Forzando verificación de archivos nuevos...");
                        FileMonitor.Instance.CheckForNewPDFs();

                        // Si detecta trabajo de impresión pendiente por más de 2 minutos, intentar limpiar cola
                        if (output.Contains("8210") || output.Contains("8208"))
                        {
                            WatcherLogger.LogActivity("Detectado trabajo de impresión pendiente. Verificando directorios...");
                            string documentsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ECM Central");
                            WatcherLogger.LogActivity($"Verificando directorio alternativo: {documentsPath}");

                            if (Directory.Exists(documentsPath))
                            {
                                var pdfFiles = Directory.GetFiles(documentsPath, "*.pdf");
                                WatcherLogger.LogActivity($"Encontrados {pdfFiles.Length} archivos PDF en directorio alternativo");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al verificar cola de impresión", ex);
            }
        }
        /// <summary>
        /// Inicia el servicio de monitoreo en modo silencioso con icono en la barra de tareas
        /// </summary>
        public static void StartSilently()
        {
            try
            {
                WatcherLogger.LogActivity("Iniciando servicio de monitoreo en modo silencioso con icono");

                if (_isRunning)
                {
                    WatcherLogger.LogActivity("El servicio ya está en ejecución");
                    return;
                }

                // Verificar si la impresora está instalada
                if (!ECMVirtualPrinter.IsPrinterInstalled())
                {
                    WatcherLogger.LogActivity("La impresora ECM Central no está instalada. Intentando instalar...");
                    Task.Run(async () => await PrinterInstaller.InstallPrinterAsync(true)).Wait();
                }
                else
                {
                    WatcherLogger.LogActivity("La impresora ECM Central ya está instalada, continuando ejecución");
                }

                // Verificar carpeta de salida y procesos de automatización
                EnsureOutputAndAutomation();

                // Iniciar los monitores
                WatcherLogger.LogActivity("Iniciando monitores de impresión");
                FileMonitor.Instance.StartMonitoring();

                // Realizar primera verificación manual de PDFs existentes
                WatcherLogger.LogActivity("Ejecutando verificación inicial de PDFs existentes");
                FileMonitor.Instance.CheckForNewPDFs();

                // Configurar timer de heartbeat para mantener el servicio activo
                _heartbeatTimer = new System.Threading.Timer(SendHeartbeat, null, 0, 30000); // cada 30 segundos

                _isRunning = true;
                WatcherLogger.LogActivity("Servicio de monitoreo iniciado correctamente en modo silencioso");
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al iniciar servicio de monitoreo en modo silencioso", ex);
            }
        }

        // NUEVO MÉTODO EN BackgroundMonitorService
        private static void ForceSystemAutoConfiguration()
        {
            try
            {
                // Solo ejecutar si no se ha configurado recientemente
                string statusFile = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "system_configured.marker");
                
                if (File.Exists(statusFile))
                {
                    DateTime lastConfig = File.GetLastWriteTime(statusFile);
                    if ((DateTime.Now - lastConfig).TotalHours < 24)
                    {
                        Console.WriteLine("Sistema configurado recientemente, omitiendo...");
                        return;
                    }
                }
                
                Console.WriteLine("Ejecutando configuración automática...");
                
                // 1. Verificar e instalar Bullzip
                bool bullzipInit = VirtualPrinter.PDFDialogAutomation.InitBullzipAtStartup();
                
                // 2. Configurar Bullzip si se instaló o ya existe
                if (bullzipInit)
                {
                    VirtualPrinter.VirtualPrinterCore.ConfigureBullzipPrinter();
                    VirtualPrinter.PDFDialogAutomation.CreateBackupBatchFile();
                }
                
                // 3. Asegurar carpetas y permisos
                VirtualPrinter.VirtualPrinterCore.EnsureOutputFolderExists();
                
                // Marcar como configurado
                File.WriteAllText(statusFile, $"Configuración automática ejecutada: {DateTime.Now}");
                
                Console.WriteLine("Configuración automática completada");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en configuración automática: {ex.Message}");
            }
        }
    }
}