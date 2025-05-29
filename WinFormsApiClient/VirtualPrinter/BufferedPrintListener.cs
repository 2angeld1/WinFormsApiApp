using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Collections.Concurrent;
using WinFormsApiClient.VirtualWatcher;
using System.Drawing.Printing;
using Microsoft.Win32;
using System.Linq;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase que intercepta y almacena en buffer las solicitudes de impresión
    /// antes de que lleguen a los controladores de impresora como Bullzip
    /// </summary>
    public class BufferedPrintListener : IDisposable
    {
        #region API de Windows y Estructuras para interceptar impresiones

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetPrinterData(IntPtr hPrinter, string pValueName, out uint pType, IntPtr pData, uint nSize, out uint pcbNeeded);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(
            IntPtr hFile,
            [Out] byte[] lpBuffer,
            uint nNumberOfBytesToRead,
            out uint lpNumberOfBytesRead,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Constantes para la API de Windows
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_FLAG_OVERLAPPED = 0x40000000;

        #endregion

        // Cola para trabajos de impresión detectados
        private static readonly ConcurrentQueue<PrintJobInfo> _printQueue = new ConcurrentQueue<PrintJobInfo>();

        // Thread para el monitoreo en segundo plano
        private static Thread _monitorThread;
        private static volatile bool _shouldStop = false;

        // Ruta para logs de diagnóstico
        private static readonly string _logPath = Path.Combine(
            VirtualPrinterCore.FIXED_OUTPUT_PATH, "print_listener_logs");

        // Tiempo entre verificaciones de la cola de impresión
        private const int POLL_INTERVAL_MS = 300;

        // Singleton
        private static BufferedPrintListener _instance;
        public static BufferedPrintListener Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BufferedPrintListener();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Constructor privado (singleton)
        /// </summary>
        private BufferedPrintListener()
        {
            try
            {
                // Crear carpeta para logs
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);

                string startupLog = Path.Combine(_logPath, "listener_startup.log");
                File.AppendAllText(startupLog, $"[{DateTime.Now}] Iniciando BufferedPrintListener\r\n");

                // Instalar filtros de impresión para detectar trabajos
                InstallPrintFilters();

                // Iniciar el thread de monitoreo
                _monitorThread = new Thread(MonitorPrintQueue)
                {
                    IsBackground = true,
                    Name = "PrintQueueMonitor",
                    Priority = ThreadPriority.AboveNormal // Prioridad alta para rápida detección
                };

                _monitorThread.Start();
                File.AppendAllText(startupLog, $"[{DateTime.Now}] Thread de monitoreo iniciado\r\n");

                // También monitorear el spooler para detectar cambios
                InstallSpoolerMonitor();

                // Registrar manejador para eventos de sistema
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                SystemEvents.SessionEnding += SystemEvents_SessionEnding;

                // Registrar en Windows para notificaciones sobre impresión
                InstallGlobalPrintJobListener();
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "listener_startup_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al iniciar BufferedPrintListener: {ex.Message}\r\n{ex.StackTrace}\r\n");

                // Intentar iniciar de todas formas partes críticas
                try
                {
                    if (_monitorThread == null || !_monitorThread.IsAlive)
                    {
                        _monitorThread = new Thread(MonitorPrintQueue)
                        {
                            IsBackground = true,
                            Name = "EmergencyPrintMonitor"
                        };
                        _monitorThread.Start();
                    }
                }
                catch { /* Ignorar errores en modo de emergencia */ }
            }
        }

        /// <summary>
        /// Instala filtros de impresión para detectar trabajos
        /// </summary>
        private void InstallPrintFilters()
        {
            try
            {
                // Registrar evento para monitorear impresoras
                var printerArray = PrinterSettings.InstalledPrinters.Cast<string>().ToArray(); 
                // Esto inicia el subsistema de impresión de .NET
                // Configurar un timer para verificar periódicamente
                System.Windows.Forms.Timer checkTimer = new System.Windows.Forms.Timer
                {
                    Interval = 1000, // 1 segundo
                    Enabled = true
                };

                checkTimer.Tick += (sender, e) => CheckPrinterStatus();
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "filter_install_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al instalar filtros: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Verifica el estado de las impresoras para detectar actividad
        /// </summary>
        private void CheckPrinterStatus()
        {
            try
            {
                // Verificar específicamente impresoras relacionadas con Bullzip
                foreach (string printerName in PrinterSettings.InstalledPrinters)
                {
                    if (printerName.Contains("Bullzip") ||
                        printerName.Contains("PDF") ||
                        printerName.Equals(VirtualPrinterCore.PRINTER_NAME, StringComparison.OrdinalIgnoreCase))
                    {
                        // Intentar abrir la impresora para ver si tiene trabajos
                        IntPtr hPrinter = IntPtr.Zero;
                        try
                        {
                            if (OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                            {
                                uint pType = 0;
                                uint pcbNeeded = 0;

                                // Verificar si hay datos de impresión
                                GetPrinterData(hPrinter, "Status", out pType, IntPtr.Zero, 0, out pcbNeeded);

                                if (pcbNeeded > 0)
                                {
                                    // Hay actividad - registrar y notificar
                                    string activityLog = Path.Combine(_logPath, "printer_activity.log");
                                    File.AppendAllText(activityLog, $"[{DateTime.Now}] Actividad detectada en impresora: {printerName}\r\n");

                                    // Iniciar el monitor si no está activo
                                    if (!BackgroundMonitorService._isRunning)
                                    {
                                        OnPrintJobDetected(new PrintJobInfo { PrinterName = printerName, Timestamp = DateTime.Now });
                                    }
                                }
                            }
                        }
                        finally
                        {
                            if (hPrinter != IntPtr.Zero)
                            {
                                ClosePrinter(hPrinter);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // No registrar cada error para evitar llenar el log
                if (DateTime.Now.Second % 15 == 0) // Solo registrar cada 15 segundos
                {
                    string errorLog = Path.Combine(_logPath, "printer_check_error.log");
                    File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al verificar impresoras: {ex.Message}\r\n");
                }
            }
        }

        /// <summary>
        /// Instala un monitor del spooler de impresión
        /// </summary>
        private void InstallSpoolerMonitor()
        {
            try
            {
                // Iniciar un timer que verifique cambios en los archivos del spooler
                string spoolerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "spool", "PRINTERS");

                if (Directory.Exists(spoolerPath))
                {
                    // Crear un watcher para esta carpeta
                    FileSystemWatcher spoolWatcher = new FileSystemWatcher
                    {
                        Path = spoolerPath,
                        NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime,
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true
                    };

                    // Eventos para detectar nuevos archivos de spool
                    spoolWatcher.Created += (s, e) => OnSpoolFileActivity(e.FullPath, "creado");
                    spoolWatcher.Changed += (s, e) => OnSpoolFileActivity(e.FullPath, "modificado");

                    string watcherLog = Path.Combine(_logPath, "spool_watcher.log");
                    File.AppendAllText(watcherLog, $"[{DateTime.Now}] Monitor de spooler instalado en: {spoolerPath}\r\n");
                }
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "spool_monitor_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al instalar monitor de spooler: {ex.Message}\r\n");

                // Intentar método alternativo con polling
                StartSpoolerPolling();
            }
        }

        /// <summary>
        /// Método alternativo: polling periódico del spooler
        /// </summary>
        private void StartSpoolerPolling()
        {
            try
            {
                // Crear un timer para verificar periódicamente
                System.Windows.Forms.Timer pollTimer = new System.Windows.Forms.Timer
                {
                    Interval = 2000, // 2 segundos
                    Enabled = true
                };

                pollTimer.Tick += (sender, e) =>
                {
                    try
                    {
                        // Usar PowerShell para verificar trabajos pendientes
                        string psCommand = @"
                        Get-Printer | ForEach-Object {
                            $printer = $_.Name
                            Get-PrintJob -PrinterName $printer -ErrorAction SilentlyContinue | 
                            Select-Object @{Name='Printer';Expression={$printer}}, JobId, DocumentName
                        } | ConvertTo-Json";

                        string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);

                        if (!string.IsNullOrEmpty(result) && result.Contains("JobId"))
                        {
                            string activityLog = Path.Combine(_logPath, "print_polling.log");
                            File.AppendAllText(activityLog, $"[{DateTime.Now}] Trabajos detectados por polling\r\n");

                            // Asegurar que el monitor esté activo
                            OnPrintJobDetected(new PrintJobInfo { PrinterName = "SystemPolling", Timestamp = DateTime.Now });
                        }
                    }
                    catch { /* Ignorar errores de polling */ }
                };
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "polling_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al iniciar polling: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Maneja la actividad de archivos en el spooler
        /// </summary>
        private void OnSpoolFileActivity(string filePath, string action)
        {
            try
            {
                string activityLog = Path.Combine(_logPath, "spool_activity.log");

                // Limitar el registro a archivos que podrían estar relacionados con impresión
                if (filePath.EndsWith(".SHD") || filePath.EndsWith(".SPL") ||
                    filePath.Contains("00") || !File.Exists(activityLog))
                {
                    File.AppendAllText(activityLog, $"[{DateTime.Now}] Archivo de spool {action}: {Path.GetFileName(filePath)}\r\n");

                    // Notificar sobre el trabajo detectado
                    OnPrintJobDetected(new PrintJobInfo
                    {
                        PrinterName = "SpoolerActivity",
                        DocumentName = Path.GetFileName(filePath),
                        Timestamp = DateTime.Now
                    });
                }
            }
            catch { /* Ignorar errores al manejar eventos de spooler */ }
        }

        /// <summary>
        /// Configura un listener global para todos los trabajos de impresión
        /// </summary>
        private void InstallGlobalPrintJobListener()
        {
            try
            {
                // Instalar un event handler para cada impresora instalada
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    try
                    {
                        PrinterSettings settings = new PrinterSettings { PrinterName = printer };
                        if (settings.IsValid)
                        {
                            // Crear un print document para esta impresora
                            PrintDocument pd = new PrintDocument
                            {
                                PrinterSettings = settings
                            };

                            // Evento que se disparará justo antes de imprimir
                            pd.BeginPrint += (s, e) =>
                            {
                                try
                                {
                                    // Este evento se dispara justo antes de imprimir
                                    PrintDocument doc = s as PrintDocument;
                                    if (doc != null)
                                    {
                                        string jobPrinter = doc.PrinterSettings.PrinterName;
                                        string jobName = doc.DocumentName;

                                        string printLog = Path.Combine(_logPath, "print_events.log");
                                        File.AppendAllText(printLog, $"[{DateTime.Now}] Evento BeginPrint detectado: {jobPrinter} - {jobName}\r\n");

                                        // Iniciar el monitor justo antes de imprimir
                                        OnPrintJobDetected(new PrintJobInfo
                                        {
                                            PrinterName = jobPrinter,
                                            DocumentName = jobName,
                                            Timestamp = DateTime.Now
                                        });
                                    }
                                }
                                catch { /* Ignorar errores en evento */ }
                            };
                        }
                    }
                    catch { /* Ignorar impresoras que no se pueden monitorear */ }
                }

                // Registrar evento de impresión global (si está disponible)
                try
                {
                    string globalLog = Path.Combine(_logPath, "global_hook.log");
                    File.AppendAllText(globalLog, $"[{DateTime.Now}] Intentando instalar hook global\r\n");

                    // Esto es un método avanzado - podría requerir privilegios elevados
                    bool hookInstalled = RegisterGlobalPrintHook();
                    File.AppendAllText(globalLog, $"[{DateTime.Now}] Hook global instalado: {hookInstalled}\r\n");
                }
                catch (Exception hookEx)
                {
                    string errorLog = Path.Combine(_logPath, "hook_error.log");
                    File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al instalar hook global: {hookEx.Message}\r\n");
                }
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "listener_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al instalar listener global: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Registra un hook global para detectar eventos de impresión
        /// </summary>
        private bool RegisterGlobalPrintHook()
        {
            try
            {
                // Este método utiliza técnicas avanzadas de Windows para interceptar actividad de impresión
                // Por seguridad, implementamos un método más simple que no requiere hooks a nivel de sistema

                // Como alternativa, registramos un archivo que el spooler verificará
                string hookMarkerPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, ".print_hook");
                File.WriteAllText(hookMarkerPath, DateTime.Now.ToString());

                // También crear un archivo .bat que realizará el monitoreo
                string batchPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "print_monitor.bat");
                string batchContent = $@"@echo off
REM Monitor de impresión para ECM Central
title ECM Print Monitor
echo Iniciando monitor de impresión...

:MonitorLoop
REM Iniciar la aplicación en modo monitor si hay actividad de impresión
powershell -Command ""& {{
    $printers = Get-Printer | ForEach-Object {{ $_.Name }}
    
    foreach ($printer in $printers) {{
        $jobs = Get-PrintJob -PrinterName $printer -ErrorAction SilentlyContinue
        if ($jobs -ne $null -and $jobs.Count -gt 0) {{
            $logFile = '{Path.Combine(_logPath, "batch_monitor.log").Replace(@"\", @"\\")}'
            Add-Content -Path $logFile -Value ""$([DateTime]::Now): Detectado trabajo para $printer""
            
            Start-Process -FilePath '{Application.ExecutablePath.Replace(@"\", @"\\")}' -ArgumentList '/backgroundmonitor' -WindowStyle Hidden
            exit
        }}
    }}
}}""

REM Esperar 2 segundos antes de verificar de nuevo
timeout /t 2 /nobreak > nul
goto MonitorLoop
";

                File.WriteAllText(batchPath, batchContent);

                // Iniciar el batch en segundo plano
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    WindowStyle = ProcessWindowStyle.Minimized,
                    CreateNoWindow = false, // Mejor mantenerlo visible para diagnóstico
                    UseShellExecute = true
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "hook_register_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al registrar hook global: {ex.Message}\r\n");
                return false;
            }
        }

        /// <summary>
        /// Maneja un evento de trabajo de impresión detectado
        /// </summary>
        private void OnPrintJobDetected(PrintJobInfo jobInfo)
        {
            try
            {
                // Registrar el evento
                string jobLog = Path.Combine(_logPath, "print_jobs.log");
                File.AppendAllText(jobLog, $"[{DateTime.Now}] Trabajo detectado - Impresora: {jobInfo.PrinterName}, Documento: {jobInfo.DocumentName ?? "N/A"}\r\n");

                // Añadir a la cola de trabajos
                _printQueue.Enqueue(jobInfo);

                // Intentar iniciar el monitor inmediatamente sin esperar al thread
                StartMonitorForPrinting();
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "job_handler_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al manejar trabajo: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Inicia el monitor de fondo para la impresión
        /// </summary>
        private void StartMonitorForPrinting()
        {
            try
            {
                // Verificar si el monitor ya está activo
                if (BackgroundMonitorService._isRunning)
                {
                    string statusLog = Path.Combine(_logPath, "monitor_status.log");
                    File.AppendAllText(statusLog, $"[{DateTime.Now}] Monitor ya está activo, no es necesario iniciarlo\r\n");
                    return;
                }

                string startLog = Path.Combine(_logPath, "monitor_start.log");
                File.AppendAllText(startLog, $"[{DateTime.Now}] Iniciando monitor para impresión...\r\n");

                // Intentar método directo primero
                BackgroundMonitorService.Start();

                // Verificar si se inició correctamente
                if (BackgroundMonitorService._isRunning)
                {
                    File.AppendAllText(startLog, $"[{DateTime.Now}] ✓ Monitor iniciado correctamente (método directo)\r\n");
                    return;
                }

                // Si falló, intentar iniciar como proceso separado
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "/backgroundmonitor /waitforsync",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process.Start(psi);
                File.AppendAllText(startLog, $"[{DateTime.Now}] Monitor iniciado como proceso separado\r\n");

                // Esperar un momento para asegurarnos de que el monitor tenga tiempo de iniciar
                Thread.Sleep(2000);
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "monitor_start_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al iniciar monitor: {ex.Message}\r\n");

                // Como último recurso, intentar otro método
                try
                {
                    // Usar ApplicationLauncher como fallback
                    ApplicationLauncher.StartBackgroundMonitorSilently();
                }
                catch { /* Ignorar errores en el último recurso */ }
            }
        }

        /// <summary>
        /// Thread que monitorea y procesa la cola de trabajos
        /// </summary>
        private void MonitorPrintQueue()
        {
            try
            {
                string threadLog = Path.Combine(_logPath, "queue_thread.log");
                File.AppendAllText(threadLog, $"[{DateTime.Now}] Thread de monitoreo iniciado\r\n");

                while (!_shouldStop)
                {
                    try
                    {
                        // Procesar cualquier trabajo en la cola
                        if (_printQueue.TryDequeue(out PrintJobInfo jobInfo))
                        {
                            File.AppendAllText(threadLog, $"[{DateTime.Now}] Procesando trabajo de cola: {jobInfo.PrinterName}\r\n");

                            // Iniciar el monitor para este trabajo
                            StartMonitorForPrinting();
                        }

                        // También verificar por procesos relacionados con impresión
                        CheckPrintRelatedProcesses();

                        // Pausa para no consumir demasiada CPU
                        Thread.Sleep(POLL_INTERVAL_MS);
                    }
                    catch (Exception ex)
                    {
                        // Registrar error pero seguir monitoreando
                        if (DateTime.Now.Second % 30 == 0) // Limitar a un registro cada 30 segundos
                        {
                            string errorLog = Path.Combine(_logPath, "queue_thread_error.log");
                            File.AppendAllText(errorLog, $"[{DateTime.Now}] Error en bucle de monitoreo: {ex.Message}\r\n");
                        }

                        // Dormir un poco más para dar tiempo a que se resuelva el problema
                        Thread.Sleep(2000);
                    }
                }
            }
            catch (Exception ex)
            {
                string fatalLog = Path.Combine(_logPath, "thread_fatal_error.log");
                File.AppendAllText(fatalLog, $"[{DateTime.Now}] Error fatal en thread de monitoreo: {ex.Message}\r\n{ex.StackTrace}\r\n");

                // Intentar reiniciar el thread
                try
                {
                    Thread.Sleep(5000); // Esperar 5 segundos

                    if (!_shouldStop)
                    {
                        _monitorThread = new Thread(MonitorPrintQueue)
                        {
                            IsBackground = true,
                            Name = "PrintQueueMonitor_Restarted"
                        };
                        _monitorThread.Start();
                        File.AppendAllText(fatalLog, $"[{DateTime.Now}] Thread reiniciado después de error fatal\r\n");
                    }
                }
                catch { /* Si falla el reinicio, no podemos hacer nada más */ }
            }
        }

        /// <summary>
        /// Verifica procesos del sistema relacionados con impresión
        /// </summary>
        private void CheckPrintRelatedProcesses()
        {
            try
            {
                // Lista de nombres de procesos relacionados con impresión
                string[] printProcesses = new[]
                {
                    "spoolsv", "printui", "splwow64", "bullzip", "pdfmaker",
                    "printfilterpipelinesvc", "PDFCreator", "PDFPrinter", "printBRM"
                };

                // Verificar cada proceso
                Process[] processes = Process.GetProcesses();

                foreach (Process proc in processes)
                {
                    try
                    {
                        // Verificar si el nombre del proceso está en la lista
                        if (printProcesses.Any(p => proc.ProcessName.IndexOf(p, StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            // Verificar también CPU y memoria para ver si está activo
                            if (proc.PriorityClass == ProcessPriorityClass.Normal ||
                                proc.PriorityClass == ProcessPriorityClass.AboveNormal ||
                                proc.PriorityClass == ProcessPriorityClass.High)
                            {
                                // Registrar solo si es nuevo o cambió
                                string procLog = Path.Combine(_logPath, "print_processes.log");
                                if (!File.Exists(procLog) || new FileInfo(procLog).Length < 10000)
                                {
                                    File.AppendAllText(procLog, $"[{DateTime.Now}] Proceso activo: {proc.ProcessName} (PID: {proc.Id})\r\n");
                                }

                                // Iniciar monitor si no está activo
                                if (!BackgroundMonitorService._isRunning)
                                {
                                    StartMonitorForPrinting();
                                }

                                // No seguir verificando una vez que encontramos un proceso activo
                                return;
                            }
                        }
                    }
                    catch { /* Ignorar errores al acceder a información de procesos */ }
                }
            }
            catch (Exception ex)
            {
                if (DateTime.Now.Second % 30 == 0) // Limitar logging
                {
                    string errorLog = Path.Combine(_logPath, "process_check_error.log");
                    File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al verificar procesos: {ex.Message}\r\n");
                }
            }
        }

        /// <summary>
        /// Maneja el evento de cambio de modo de energía
        /// </summary>
        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            try
            {
                if (e.Mode == Microsoft.Win32.PowerModes.Resume)
                {
                    string resumeLog = Path.Combine(_logPath, "system_events.log");
                    File.AppendAllText(resumeLog, $"[{DateTime.Now}] Sistema reanudado de suspensión\r\n");

                    // Reiniciar componentes después de reanudar
                    if (_monitorThread == null || !_monitorThread.IsAlive)
                    {
                        _shouldStop = false;
                        _monitorThread = new Thread(MonitorPrintQueue)
                        {
                            IsBackground = true,
                            Name = "PrintQueueMonitor_Resume"
                        };
                        _monitorThread.Start();
                        File.AppendAllText(resumeLog, $"[{DateTime.Now}] Thread de monitoreo reiniciado\r\n");
                    }

                    // También reinstalar filtros
                    InstallPrintFilters();
                }
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "power_event_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al manejar cambio de energía: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Maneja el evento de finalización de sesión
        /// </summary>
        private void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            try
            {
                string sessionLog = Path.Combine(_logPath, "system_events.log");
                File.AppendAllText(sessionLog, $"[{DateTime.Now}] Sesión finalizando: {e.Reason}\r\n");

                // Detener el monitoreo limpiamente
                _shouldStop = true;
            }
            catch { /* Ignorar errores al finalizar */ }
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Detener el thread de monitoreo
                _shouldStop = true;

                if (_monitorThread != null && _monitorThread.IsAlive)
                {
                    _monitorThread.Join(1000); // Esperar hasta 1 segundo
                }

                // Desregistrar eventos
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                SystemEvents.SessionEnding -= SystemEvents_SessionEnding;

                string disposeLog = Path.Combine(_logPath, "listener_dispose.log");
                File.AppendAllText(disposeLog, $"[{DateTime.Now}] BufferedPrintListener liberado correctamente\r\n");
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "dispose_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al liberar recursos: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Estructura para información de trabajos de impresión detectados
        /// </summary>
        private class PrintJobInfo
        {
            public string PrinterName { get; set; }
            public string DocumentName { get; set; }
            public DateTime Timestamp { get; set; }
        }
    }
}