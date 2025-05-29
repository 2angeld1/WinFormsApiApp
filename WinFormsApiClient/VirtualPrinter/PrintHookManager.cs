using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase que implementa un hook para interceptar trabajos de impresión antes de que lleguen a Bullzip
    /// </summary>
    public static class PrintHookManager
    {
        // API de Windows para interceptar trabajos de impresión
        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool AddPrintProcessor(string pServer, string pEnvironment, string pPathName, string pPrintProcessorName);

        // Flag global que indica si se ha instalado el hook
        private static bool _hookInstalled = false;

        // Carpeta para los logs de impresión
        private static string _printLogPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "print_hook_logs");

        /// <summary>
        /// Instala un hook a nivel global para interceptar trabajos de impresión
        /// </summary>
        public static bool InstallPrintHook()
        {
            try
            {
                // Asegurarse que la carpeta de logs existe
                string logFolder = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "print_hook_logs");
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                // Intentar escribir log con múltiples reintentos y manejo de concurrencia
                bool logWritten = false;
                int retryCount = 0;
                string logFile = Path.Combine(logFolder, "print_hook.log");

                while (!logWritten && retryCount < 3)
                {
                    try
                    {
                        using (FileStream fs = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            sw.WriteLine($"[{DateTime.Now}] Iniciando instalación de hook de impresión");
                            logWritten = true;
                        }
                    }
                    catch (IOException)
                    {
                        // Esperar un momento antes de reintentar
                        retryCount++;
                        Thread.Sleep(100 * retryCount);
                    }
                    catch (Exception logEx)
                    {
                        // Para otros errores, intentar con un nombre de archivo alternativo
                        try
                        {
                            logFile = Path.Combine(logFolder, $"print_hook_{Guid.NewGuid().ToString().Substring(0, 8)}.log");
                        }
                        catch
                        {
                            // Si todo falla, continuar sin logging
                            break;
                        }
                    }
                }

                // Crear un controlador a nivel de Windows que se active ANTES de que el trabajo llegue a Bullzip
                // Utilizamos la clave de registro de monitoreo de impresión
                bool registryConfigured = false;
                try
                {
                    using (RegistryKey key = Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\Print\Monitors", true))
                    {
                        if (key != null)
                        {
                            // Crear o abrir la subclave ECMPrintMonitor
                            RegistryKey monitorKey = key.OpenSubKey("ECMPrintMonitor", true);
                            if (monitorKey == null)
                                monitorKey = key.CreateSubKey("ECMPrintMonitor");

                            // Configurar los valores necesarios
                            monitorKey.SetValue("Driver", "ecmmon.dll");
                            monitorKey.SetValue("PreJob", 1, RegistryValueKind.DWord);

                            // Colocar la ruta de nuestra DLL
                            string dllPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ecmmon.dll");
                            registryConfigured = true;

                            // Escribir log si es posible
                            SafeWriteLog(logFile, $"[{DateTime.Now}] Clave de registro configurada correctamente");
                        }
                        else
                        {
                            // Escribir log si es posible
                            SafeWriteLog(logFile, $"[{DateTime.Now}] Error: No se pudo acceder a la clave de registro");
                        }
                    }
                }
                catch (Exception regEx)
                {
                    // Intentar escribir log
                    SafeWriteLog(logFile, $"[{DateTime.Now}] Error al configurar registro: {regEx.Message}");

                    // No interrumpir el flujo si falla el registro
                    registryConfigured = false;
                }

                // Si no existe la DLL o falló el registro, crear un archivo batch como alternativa
                string batchFile = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "ecm_print_hook.bat");
                bool batchCreated = false;

                try
                {
                    if (!registryConfigured || !File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ecmmon.dll")))
                    {
                        batchCreated = CreatePrintHookBatch();

                        if (batchCreated)
                        {
                            SafeWriteLog(logFile, $"[{DateTime.Now}] Script batch creado como alternativa");
                        }
                        else
                        {
                            SafeWriteLog(logFile, $"[{DateTime.Now}] No se pudo crear script batch alternativo");
                        }
                    }
                }
                catch (Exception batchEx)
                {
                    SafeWriteLog(logFile, $"[{DateTime.Now}] Error al crear batch: {batchEx.Message}");
                    batchCreated = false;
                }

                // Como método adicional, instalamos un watcher que verifique la cola de impresión
                bool watcherInstalled = false;
                try
                {
                    InstallPrintQueueWatcher();
                    watcherInstalled = true;
                    SafeWriteLog(logFile, $"[{DateTime.Now}] Watcher de cola instalado correctamente");
                }
                catch (Exception watcherEx)
                {
                    SafeWriteLog(logFile, $"[{DateTime.Now}] Error al instalar watcher: {watcherEx.Message}");
                    watcherInstalled = false;
                }

                // Considerar que el hook está instalado si al menos uno de los métodos tuvo éxito
                _hookInstalled = registryConfigured || batchCreated || watcherInstalled;

                // Log final
                if (_hookInstalled)
                {
                    SafeWriteLog(logFile, $"[{DateTime.Now}] Hook instalado exitosamente: Registro={registryConfigured}, Batch={batchCreated}, Watcher={watcherInstalled}");
                }
                else
                {
                    SafeWriteLog(logFile, $"[{DateTime.Now}] Advertencia: No se pudo instalar hook por ninguno de los métodos disponibles");
                }

                return _hookInstalled;
            }
            catch (Exception ex)
            {
                // Manejar el error general con múltiples opciones de log
                try
                {
                    // Intentar escribir en log principal
                    string errorLogPath = Path.Combine(_printLogPath, "print_hook_error.log");
                    SafeWriteLog(errorLogPath, $"[{DateTime.Now}] Error crítico al instalar hook: {ex.Message}\r\n{ex.StackTrace}");
                }
                catch
                {
                    // Si falla, intentar en una ubicación alternativa
                    try
                    {
                        string altLogPath = Path.Combine(
                            Path.GetTempPath(),
                            $"ecm_hook_error_{DateTime.Now:yyyyMMdd_HHmmss}.log");

                        File.WriteAllText(altLogPath,
                            $"[{DateTime.Now}] Error al instalar hook: {ex.Message}\r\n{ex.StackTrace}");
                    }
                    catch
                    {
                        // Si también falla, intentar con la consola
                        Console.WriteLine($"ERROR CRÍTICO: No se pudo instalar hook de impresión: {ex.Message}");
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Escribe en un archivo de log de manera segura, manejando excepciones y concurrencia
        /// </summary>
        private static void SafeWriteLog(string logPath, string message)
        {
            if (string.IsNullOrEmpty(logPath))
                return;

            for (int i = 0; i < 3; i++) // Hasta 3 intentos
            {
                try
                {
                    // Usar FileShare.ReadWrite para permitir acceso concurrente
                    using (FileStream fs = new FileStream(logPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                    using (StreamWriter writer = new StreamWriter(fs))
                    {
                        writer.WriteLine(message);
                    }
                    return; // Si tiene éxito, salir
                }
                catch (IOException)
                {
                    // Esperar brevemente y reintentar
                    Thread.Sleep(50 * (i + 1));
                }
                catch
                {
                    // Para otros errores, intentar una vez y abandonar
                    break;
                }
            }

            // Si todos los intentos fallan, intentar con una ruta alternativa
            try
            {
                string altPath = Path.Combine(
                    Path.GetDirectoryName(logPath) ?? Path.GetTempPath(),
                    $"alt_{Path.GetFileName(logPath)}_{DateTime.Now:HHmmss}.log");

                File.AppendAllText(altPath, message + Environment.NewLine);
            }
            catch
            {
                // Si todo falla, abandonar silenciosamente
            }
        }

        /// <summary>
        /// Crea un archivo batch que se ejecutará como sustituto de la DLL
        /// </summary>
        /// <returns>True si se creó el batch exitosamente, False en caso contrario</returns>
        private static bool CreatePrintHookBatch()
        {
            try
            {
                string batchPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "ecm_print_hook.bat");
                string scriptContent = $@"@echo off
REM Script para interceptar trabajos de impresión de Bullzip
echo [%date% %time%] Trabajo de impresión interceptado >> ""{Path.Combine(_printLogPath, "hook_batch.log")}""

REM Iniciar el monitor de segundo plano para que esté listo
start """" ""{Path.GetFullPath(System.Windows.Forms.Application.ExecutablePath)}"" /backgroundmonitor

REM Esperar a que el monitor esté activo
timeout /t 2 /nobreak > nul

exit
";
                File.WriteAllText(batchPath, scriptContent);

                // Configurar para que se ejecute en cada impresión de Bullzip
                ConfigureBullzipToRunBatch(batchPath);

                // Verificar que el archivo se creó correctamente
                return File.Exists(batchPath);
            }
            catch (Exception ex)
            {
                // Usar SafeWriteLog para consistencia con el resto del código
                SafeWriteLog(Path.Combine(_printLogPath, "batch_error.log"),
                    $"[{DateTime.Now}] Error al crear batch: {ex.Message}\r\n");
                return false;
            }
        }

        /// <summary>
        /// Configura Bullzip para ejecutar nuestro batch antes de procesar
        /// </summary>
        private static void ConfigureBullzipToRunBatch(string batchPath)
        {
            try
            {
                // Ubicación del archivo de configuración de Bullzip
                string bullzipConfigFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Bullzip", "PDF Printer");

                string iniPath = Path.Combine(bullzipConfigFolder, "settings.ini");

                // Si no existe la carpeta, crearla
                if (!Directory.Exists(bullzipConfigFolder))
                    Directory.CreateDirectory(bullzipConfigFolder);

                // Leer contenido actual si existe
                string currentContent = "";
                if (File.Exists(iniPath))
                    currentContent = File.ReadAllText(iniPath);

                // Modificar configuración para ejecutar nuestro batch antes de procesar
                string newContent;
                if (string.IsNullOrEmpty(currentContent) || !currentContent.Contains("[PDF Printer]"))
                {
                    // Crear archivo desde cero
                    newContent = $@"[PDF Printer]
Output={VirtualPrinterCore.FIXED_OUTPUT_PATH}
ShowSaveAS=never
ShowSettings=never
ShowProgress=no
RunOnPrinterEvent=1
RunPrinterEvent=""{batchPath}""
FilenameTemplate=ECM_<datetime:yyyyMMddHHmmss>
";
                }
                else
                {
                    // Modificar el existente
                    newContent = currentContent;
                    if (!newContent.Contains("RunOnPrinterEvent="))
                    {
                        // Insertar después de [PDF Printer]
                        int pos = newContent.IndexOf("[PDF Printer]");
                        if (pos >= 0)
                        {
                            pos = newContent.IndexOf('\n', pos) + 1;
                            newContent = newContent.Insert(pos, $"RunOnPrinterEvent=1\r\nRunPrinterEvent=\"{batchPath}\"\r\n");
                        }
                    }
                    else
                    {
                        // Reemplazar la línea existente
                        newContent = System.Text.RegularExpressions.Regex.Replace(
                            newContent,
                            @"RunOnPrinterEvent=.*\r?\n",
                            $"RunOnPrinterEvent=1\r\nRunPrinterEvent=\"{batchPath}\"\r\n");
                    }
                }

                // Guardar el archivo
                File.WriteAllText(iniPath, newContent);

                string logFile = Path.Combine(_printLogPath, "bullzip_config.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Configuración de Bullzip actualizada\r\n");
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(_printLogPath, "bullzip_config_error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Error al configurar Bullzip: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Instala un watcher que monitorice constantemente la cola de impresión
        /// </summary>
        private static void InstallPrintQueueWatcher()
        {
            try
            {
                // Iniciar un servicio que verifique la cola de impresión cada 1 segundo
                System.Windows.Forms.Timer printQueueTimer = new System.Windows.Forms.Timer();
                printQueueTimer.Interval = 1000;
                printQueueTimer.Tick += (sender, e) => CheckPrintQueue();
                printQueueTimer.Start();

                string logFile = Path.Combine(_printLogPath, "print_queue_watcher.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Watcher de cola de impresión iniciado\r\n");
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(_printLogPath, "watcher_error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Error al iniciar watcher: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Verifica la cola de impresión de Bullzip
        /// </summary>
        private static DateTime _lastJobTime = DateTime.MinValue;
        private static void CheckPrintQueue()
        {
            try
            {
                // Verificar si hay trabajos para Bullzip usando PowerShell
                string psCommand = @"
Get-Printer | Where-Object { $_.Name -like '*Bullzip*' -or $_.Name -like '*PDF Printer*' } | ForEach-Object {
    $printer = $_.Name
    Get-PrintJob -PrinterName $printer | Select-Object JobId, DocumentName, JobStatus, SubmittedTime | ConvertTo-Json
}";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);

                if (!string.IsNullOrEmpty(result) && (result.Contains("JobId") || result.Contains("SubmittedTime")))
                {
                    // Obtener la marca de tiempo del trabajo más reciente
                    DateTime newestJob = DateTime.MinValue;
                    try
                    {
                        // Parsear JSON para obtener el tiempo
                        if (Newtonsoft.Json.JsonConvert.DeserializeObject(result) is Newtonsoft.Json.Linq.JArray jobs)
                        {
                            foreach (var job in jobs)
                            {
                                if (job["SubmittedTime"] != null &&
                                    DateTime.TryParse(job["SubmittedTime"].ToString(), out DateTime jobTime))
                                {
                                    if (jobTime > newestJob)
                                        newestJob = jobTime;
                                }
                            }
                        }
                    }
                    catch { /* Ignorar errores de parseo */ }

                    // Si encontramos un trabajo nuevo o no pudimos analizar el tiempo
                    if (newestJob > _lastJobTime || newestJob == DateTime.MinValue)
                    {
                        // Hay un trabajo nuevo, iniciar el monitor de fondo
                        string logFile = Path.Combine(_printLogPath, "print_queue_events.log");
                        File.AppendAllText(logFile, $"[{DateTime.Now}] ¡Detectado trabajo de impresión! Iniciando monitor...\r\n");
                        File.AppendAllText(logFile, $"Datos trabajo: {result.Replace("\r\n", " ")}\r\n");

                        // Actualizar el tiempo del último trabajo procesado
                        if (newestJob > _lastJobTime)
                            _lastJobTime = newestJob;

                        // Iniciar el monitor de fondo
                        StartBackgroundMonitor();
                    }
                }
            }
            catch (Exception ex)
            {
                // Solo registrar el error sin detener el watcher
                string logFile = Path.Combine(_printLogPath, "queue_check_error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Error al verificar cola: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Inicia el monitor de fondo
        /// </summary>
        private static void StartBackgroundMonitor()
        {
            try
            {
                // Verificar si el monitor ya está activo
                if (!WinFormsApiClient.VirtualWatcher.BackgroundMonitorService._isRunning)
                {
                    string logFile = Path.Combine(_printLogPath, "monitor_start.log");
                    File.AppendAllText(logFile, $"[{DateTime.Now}] Iniciando monitor de fondo desde hook\r\n");

                    // Iniciar el monitor directamente
                    WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.Start();

                    // Si falla, intentar iniciarlo como proceso separado
                    if (!WinFormsApiClient.VirtualWatcher.BackgroundMonitorService._isRunning)
                    {
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = System.Windows.Forms.Application.ExecutablePath,
                            Arguments = "/backgroundmonitor",
                            UseShellExecute = true,
                            CreateNoWindow = true
                        };

                        Process.Start(startInfo);
                        File.AppendAllText(logFile, $"[{DateTime.Now}] Iniciando monitor como proceso separado\r\n");
                    }
                }
                else
                {
                    // Ya está activo, registrarlo
                    string logFile = Path.Combine(_printLogPath, "monitor_status.log");
                    File.AppendAllText(logFile, $"[{DateTime.Now}] Monitor de fondo ya está activo\r\n");
                }
            }
            catch (Exception ex)
            {
                string logFile = Path.Combine(_printLogPath, "monitor_start_error.log");
                File.AppendAllText(logFile, $"[{DateTime.Now}] Error al iniciar monitor: {ex.Message}\r\n");
            }
        }
    }
}