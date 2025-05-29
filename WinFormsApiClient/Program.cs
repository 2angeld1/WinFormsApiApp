using System;
using System.Windows.Forms;
using System.IO;
using WinFormsApiClient.VirtualPrinter;
using System.Diagnostics;
using System.Linq;
using WinFormsApiClient.VirtualWatcher;
using System.Management;

namespace WinFormsApiClient
{
    static class Program
    {
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // Configurar manejo de excepciones no controladas
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                if (e.ExceptionObject is Exception ex)
                    WatcherLogger.LogError("Excepción no controlada a nivel de dominio", ex);
            };
            Application.ThreadException += (s, e) =>
                WatcherLogger.LogError("Excepción no controlada en hilo de UI", e.Exception);

            // Procesar argumentos
            bool isSilentMode = args.Contains("/silent");
            bool isBackgroundMonitor = args.Contains("/backgroundmonitor");

            // Verificar que no haya ya una instancia del mismo tipo en ejecución
            if (isBackgroundMonitor && PreventMultipleBackgroundInstances())
            {
                // Ya hay una instancia, salir silenciosamente
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Configurar inicio automático con Windows
            EnsureBackgroundMonitorStartsWithWindows();

            // Procesar los argumentos
            if (isBackgroundMonitor)
            {
                // Modo monitor en segundo plano
                if (isSilentMode)
                {
                    // Modo silencioso con icono en el área de notificación
                    // Iniciar el monitor
                    WatcherLogger.LogActivity("Iniciando monitor en modo silencioso");
                    BackgroundMonitorService.Start();

                    // Crear un formulario invisible para mantener la aplicación en ejecución
                    Form invisibleForm = new Form
                    {
                        WindowState = FormWindowState.Minimized,
                        ShowInTaskbar = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Size = new System.Drawing.Size(1, 1),
                        Opacity = 0
                    };

                    // Asegurarse que no se muestre ni siquiera por un momento
                    invisibleForm.Load += (sender, e) => invisibleForm.Hide();

                    // Añadir ícono en la bandeja del sistema
                    using (NotifyIcon trayIcon = FormInteraction.ShowTrayIcon(
                        "ECM Central - Monitor de impresión"))
                    {
                        // Mantener la aplicación en ejecución
                        Application.Run(invisibleForm);
                    }
                }
                else
                {
                    // Modo normal (consola visible)
                    Console.WriteLine("INICIO DE MONITOR DE FONDO (modo sincronización)");
                    BackgroundMonitorService.StartAndNotify();

                    // Crear un formulario invisible para mantener la aplicación en ejecución
                    Form invisibleForm = new Form
                    {
                        WindowState = FormWindowState.Minimized,
                        ShowInTaskbar = false,
                        FormBorderStyle = FormBorderStyle.None,
                        Opacity = 0
                    };
                    invisibleForm.Load += (sender, e) => invisibleForm.Hide();

                    // Crear icono en bandeja del sistema
                    using (NotifyIcon trayIcon = FormInteraction.ShowTrayIcon(
                        "Monitor ECM Central - Modo sincronización"))
                    {
                        Application.Run(invisibleForm);
                    }
                }
            }
            else
            {
                // Modo normal de la aplicación
                Application.Run(new LoginForm());
            }
        }

        /// <summary>
        /// Previene que se ejecuten múltiples instancias del monitor de fondo
        /// </summary>
        private static bool PreventMultipleBackgroundInstances()
        {
            try
            {
                // Verificar si ya existe un marcador
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
                if (File.Exists(markerFilePath))
                {
                    try
                    {
                        // Verificar si el proceso existe
                        string pidContent = File.ReadAllText(markerFilePath);
                        if (int.TryParse(pidContent, out int pid))
                        {
                            try
                            {
                                Process proc = Process.GetProcessById(pid);
                                if (!proc.HasExited)
                                {
                                    // El proceso está en ejecución, salir
                                    return true;
                                }
                            }
                            catch
                            {
                                // El proceso no existe, continuar
                            }
                        }
                    }
                    catch
                    {
                        // Error al leer el archivo, ignorar
                    }
                }

                // También verificar procesos directamente
                Process currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.Id != currentProcess.Id) // No es este proceso
                        {
                            string cmdLine = GetProcessCommandLine(proc.Id);
                            if (cmdLine.Contains("/backgroundmonitor"))
                            {
                                // Ya hay un proceso con /backgroundmonitor
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        // Ignorar errores
                    }
                }

                return false;
            }
            catch
            {
                // En caso de error, asumir que no hay otra instancia
                return false;
            }
        }
        /// <summary>
        /// Obtiene la línea de comandos de un proceso
        /// </summary>
        private static string GetProcessCommandLine(int processId)
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
        /// Configura que el monitor se inicie automáticamente al iniciar Windows
        /// </summary>
        private static void EnsureBackgroundMonitorStartsWithWindows()
        {
            try
            {
                string logPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "autostart_setup.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Configurando inicio automático con Windows\r\n");

                // 1. Verificar si ya está configurado mediante registro
                if (!BackgroundMonitorService.IsInstalledForAutostart())
                {
                    File.AppendAllText(logPath, $"[{DateTime.Now}] No existe configuración de inicio automático, instalando...\r\n");

                    // Configurar inicio mediante el registro de Windows
                    bool registryResult = BackgroundMonitorService.InstallAutostart();
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Resultado configuración por registro: {registryResult}\r\n");

                    // Configurar inicio mediante tarea programada (más confiable)
                    bool taskResult = CreateStartupTask();
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Resultado configuración por tarea programada: {taskResult}\r\n");
                }
                else
                {
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Inicio automático ya está configurado\r\n");

                    // Verificar que la tarea programada también esté configurada
                    CreateStartupTask();
                }
            }
            catch (Exception ex)
            {
                // Registrar error pero no interrumpir la ejecución principal
                try
                {
                    string errorPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "autostart_error.log");
                    File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al configurar inicio automático: {ex.Message}\r\n{ex.StackTrace}\r\n");
                }
                catch { /* Si no podemos ni siquiera registrar el error, no hay mucho que hacer */ }
            }
        }

        /// <summary>
        /// Crea una tarea programada para iniciar el monitor al arrancar Windows
        /// </summary>
        private static bool CreateStartupTask()
        {
            try
            {
                string appPath = Application.ExecutablePath;
                string logPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "startup_task.log");

                // Script PowerShell para crear/actualizar la tarea programada
                string psScript = $@"
try {{
    $taskName = 'ECMCentralMonitor'
    $exe = '{appPath.Replace("\\", "\\\\")}'
    $action = New-ScheduledTaskAction -Execute $exe -Argument '/backgroundmonitor /silent'
    
    # Definir varios disparadores para mayor confiabilidad
    $triggers = @(
        # Al inicio de sesión (más común)
        (New-ScheduledTaskTrigger -AtLogon),
        # Al inicio del sistema (como respaldo)
        (New-ScheduledTaskTrigger -AtStartup)
    )
    
    # Configuración optimizada
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -Hidden
    
    # Usar el nivel más alto de privilegios disponible
    $principal = New-ScheduledTaskPrincipal -GroupId 'BUILTIN\Users' -RunLevel Highest
    
    # Si ya existe la tarea, eliminarla primero
    Unregister-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue -Confirm:$false
    
    # Crear la tarea
    $task = Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $triggers -Settings $settings -Principal $principal
    Write-Output ""Tarea programada creada: $($task.TaskName)""
    
    return $true
}} catch {{
    Write-Output ""Error: $($_.Exception.Message)""
    return $false
}}";

                // Ejecutar PowerShell con elevación (para garantizar permisos)
                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psScript);

                // Registrar el resultado
                File.AppendAllText(logPath, $"[{DateTime.Now}] Resultado de creación de tarea: {result}\r\n");

                return result.Contains("creada") || result.Contains("created");
            }
            catch (Exception ex)
            {
                try
                {
                    // Registrar error
                    string errorPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "task_error.log");
                    File.AppendAllText(errorPath, $"[{DateTime.Now}] Error al crear tarea: {ex.Message}\r\n{ex.StackTrace}\r\n");
                }
                catch { }

                return false;
            }
        }

        /// <summary>
        /// Procesa un nuevo archivo PDF detectado en la carpeta
        /// </summary>
        private static void ProcessNewPdfFile(string filePath, string logPath)
        {
            try
            {
                // Esperar a que el archivo esté completamente escrito
                System.Threading.Thread.Sleep(1000);

                if (File.Exists(filePath))
                {
                    // Registrar detección
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Archivo detectado: {filePath}\r\n");

                    // Verificar si la aplicación ya está en ejecución
                    Process currentProcess = Process.GetCurrentProcess();
                    Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

                    // Solo lanzar si no hay otra instancia ejecutándose (excepto esta)
                    if (processes.Length <= 1)
                    {
                        // Lanzar una nueva instancia de la aplicación con el archivo
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = Application.ExecutablePath,
                            Arguments = $"/processfile=\"{filePath}\"",
                            UseShellExecute = true
                        };

                        Process.Start(startInfo);
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Aplicación lanzada para procesar: {filePath}\r\n");
                    }
                    else
                    {
                        // La aplicación ya está en ejecución, enviar notificación
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Aplicación ya en ejecución. Archivo listo para procesamiento: {filePath}\r\n");

                        // Crear un archivo marcador para que la instancia en ejecución lo detecte
                        string markerFile = Path.Combine(
                            VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                            "pending_pdf.marker");

                        File.WriteAllText(markerFile, filePath);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar archivo PDF: {ex.Message}");
                File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR al procesar: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Método directo para procesar un archivo PDF específico
        /// </summary>
        private static void DirectProcessPdfFile(string filePath)
        {
            try
            {
                // Verificar que el archivo exista
                if (!File.Exists(filePath))
                {
                    MessageBox.Show(
                        $"No se encontró el archivo PDF:\n{filePath}",
                        "Error de procesamiento",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return;
                }

                // Registrar evento en log
                File.AppendAllText(
                    Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "direct_pdf_process.log"),
                    $"[{DateTime.Now}] Procesando directamente: {filePath}\r\n");

                Console.WriteLine($"Procesando directamente archivo PDF: {filePath}");

                // Abrir la aplicación principal
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Crear y mostrar la pantalla de login
                LoginForm form = new LoginForm();
                form.Show();

                // Agregar método para procesar el archivo después de la carga
                form.FormClosed += (sender, e) =>
                {
                    Environment.Exit(0);
                };

                // Configurar un timer para procesar el archivo después de un breve retraso
                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                timer.Interval = 3000; // 3 segundos
                timer.Tick += (sender, e) =>
                {
                    timer.Stop();
                    try
                    {
                        WinFormsApiClient.VirtualWatcher.DocumentProcessor.Instance.ProcessNewPrintJob(filePath);
                        Console.WriteLine("Documento procesado con éxito mediante método directo");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al procesar documento: {ex.Message}");
                    }
                };
                timer.Start();

                Application.Run(form);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en procesamiento directo: {ex.Message}");

                // Como último recurso, mostrar la pantalla tradicional
                Application.Run(new LoginForm());
            }
        }
        /// <summary>
        /// Instala un servicio a nivel de sistema para monitorear constantemente la cola de impresión
        /// </summary>
        private static void InstallSystemPrintMonitor()
        {
            try
            {
                // Obtener la ruta de la aplicación
                string appPath = System.Windows.Forms.Application.ExecutablePath;

                // Crear un servicio de Windows que ejecute nuestra aplicación con /backgroundmonitor
                string psCommand = $@"
$serviceName = 'ECMPrintMonitor'
$displayName = 'ECM Print Monitor Service'
$binPath = 'wininit.exe /RunCommand ""{appPath}"" /backgroundmonitor /silent'

# Verificar si el servicio ya existe
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue

if ($service -eq $null) {{
    # Crear el servicio
    New-Service -Name $serviceName -DisplayName $displayName -BinaryPathName $binPath -StartupType Automatic
    Start-Service -Name $serviceName
    Write-Output ""Servicio creado y iniciado""
}} else {{
    Write-Output ""El servicio ya existe""
}}

# Como alternativa, crear una tarea programada
$taskName = 'ECMPrintMonitorTask'
$trigger = New-ScheduledTaskTrigger -AtStartup
$action = New-ScheduledTaskAction -Execute '{appPath}' -Argument '/backgroundmonitor /silent'

$task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
if ($task -eq $null) {{
    Register-ScheduledTask -TaskName $taskName -Trigger $trigger -Action $action -RunLevel Highest -User 'SYSTEM'
    Write-Output ""Tarea programada creada""
}} else {{
    Write-Output ""La tarea programada ya existe""
}}
";

                // Ejecutar con PowerShell elevado
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -Command \"{psCommand}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Ejecutar como administrador
                    CreateNoWindow = false
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al instalar servicio de monitoreo: {ex.Message}");
            }
        }
        private static void ProcessCommandLineArguments(string[] args)
        {
            string argument = args[0].ToLower();
            // Agregar logs explícitos para diagnóstico
            Console.WriteLine($"Procesando argumento de línea de comandos: '{argument}'");

            // Guardar en un archivo para diagnóstico (opcional)
            File.AppendAllText(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "cmd_args.log"),
                $"[{DateTime.Now}] Procesando argumento: {argument}\r\n");

            if (argument.StartsWith("/processfile="))
            {
                // Extraer la ruta del archivo
                string filePath = argument.Substring(13).Trim('"');
                Console.WriteLine($"Procesando archivo PDF generado por Bullzip: {filePath}");

                // Método directo para procesar el archivo
                DirectProcessPdfFile(filePath);
                return;
            }
            else if (argument == "/login")
            {
                // NUEVO: Iniciar la aplicación normalmente y verificar si hay archivos pendientes
                LoginForm loginForm = new LoginForm();

                // Verificar si hay un archivo pendiente al cargar
                string markerPath = Path.Combine(
                    VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                    "last_bullzip_file.txt");

                if (File.Exists(markerPath))
                {
                    try
                    {
                        string pendingFile = File.ReadAllText(markerPath).Trim();

                        // Registrar para diagnóstico
                        Console.WriteLine($"Encontrado archivo pendiente: {pendingFile}");

                        // Variable para controlar ejecución única del handler
                        bool processed = false;

                        // Definir el handler con otro nombre para evitar conflicto con 'args'
                        EventHandler idleHandler = null;
                        idleHandler = (sender, eventArgs) =>
                        {
                            if (!processed)
                            {
                                processed = true;
                                Application.Idle -= idleHandler;

                                try
                                {
                                    if (File.Exists(pendingFile))
                                    {
                                        // Procesar el archivo
                                        WinFormsApiClient.VirtualWatcher.DocumentProcessor.Instance.ProcessNewPrintJob(pendingFile);
                                        Console.WriteLine($"Archivo procesado exitosamente: {pendingFile}");

                                        // Eliminar el marcador
                                        try { File.Delete(markerPath); } catch { }
                                    }
                                }
                                catch (Exception procEx)
                                {
                                    Console.WriteLine($"Error al procesar archivo pendiente: {procEx.Message}");
                                }
                            }
                        };

                        Application.Idle += idleHandler;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al leer archivo pendiente: {ex.Message}");
                    }
                }

                Application.Run(loginForm);
                return;
            }

            else if (argument == "/installprinter")
            {
                // Instalar impresora con permisos elevados (con diálogo)
                ECMVirtualPrinter.InstallPrinterAsync(false).Wait();
                return; // Salir después de instalar
            }
            else if (argument == "/installprintersilent")
            {
                // Instalar impresora silenciosamente (sin diálogos)
                ECMVirtualPrinter.InstallPrinterAsync(true).Wait();
                return; // Salir después de instalar
            }
            //else if (argument == "/installscanner")
            //{
            //    // Instalar escáner con permisos elevados
            //    ECMVirtualPrinter.InstallScannerDriverAsync().Wait();
            //    return; // Salir después de instalar
            //}
            else if (argument == "/backgroundmonitor")
            {
                // Verificar flags adicionales
                bool silentMode = args.Any(a => a.ToLower() == "/silent");
                bool lowImpactMode = args.Any(a => a.ToLower() == "/lowimpact");
                bool synchronizedMode = args.Any(a => a.ToLower() == "/waitforsync"); // Añadir esta línea

                Console.WriteLine("Iniciando modo de monitor en segundo plano" +
                    (silentMode ? " silenciosamente" : "") +
                    (lowImpactMode ? " (impacto reducido)" : "") +
                    (synchronizedMode ? " (modo sincronización)" : ""));

                // En modo de bajo impacto, limitamos la frecuencia de log
                if (lowImpactMode)
                {
                    WatcherLogger.SetLoggingInterval(TimeSpan.FromMinutes(30));
                }

                // Registrar en init_log.txt
                string initLogPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");
                File.AppendAllText(initLogPath,
                    $"[{DateTime.Now}] INICIO DE MONITOR DE FONDO" +
                    (synchronizedMode ? " (modo sincronización)" : "") +
                    (silentMode ? " (modo silencioso)" : "") + "\r\n");

                // Crear inmediatamente el archivo de marcador
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
                File.WriteAllText(markerFilePath, Process.GetCurrentProcess().Id.ToString());

                // Registrar eliminación del archivo al cerrar
                AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                    try { if (File.Exists(markerFilePath)) File.Delete(markerFilePath); } catch { }
                };

                // Registrar eliminación del archivo al cerrar
                AppDomain.CurrentDomain.ProcessExit += (s, e) => {
                    try { if (File.Exists(markerFilePath)) File.Delete(markerFilePath); } catch { }
                };

                WatcherLogger.LogActivity("Iniciando modo de monitor en segundo plano" +
                    (silentMode ? " silenciosamente" : "") +
                    (synchronizedMode ? " con sincronización" : ""));

                try
                {
                    // Configurar la carpeta de salida de la impresora
                    ConfigurePrinterOutputFolder();

                    // Iniciar el servicio de monitoreo - MODIFICADO PARA SINCRONIZACIÓN
                    if (synchronizedMode)
                    {
                        WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.StartAndNotify();
                    }
                    else
                    {
                        WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.Start();
                    }

                    // En modo silencioso, evitar mostrar ventanas
                    if (silentMode)
                    {
                        // Crear un formulario invisible para mantener la aplicación en ejecución
                        Form invisibleForm = new Form
                        {
                            WindowState = FormWindowState.Minimized,
                            ShowInTaskbar = false,
                            FormBorderStyle = FormBorderStyle.None,
                            Size = new System.Drawing.Size(1, 1),
                            Opacity = 0
                        };

                        // Asegurarse que no se muestre ni siquiera por un momento
                        invisibleForm.Load += (sender, e) => invisibleForm.Hide();

                        // Usar el método existente para mostrar el ícono en la bandeja del sistema
                        using (NotifyIcon trayIcon = FormInteraction.ShowTrayIcon(
                            "ECM Central - Monitor de impresión"))
                        {
                            // Mantener la aplicación en ejecución
                            Application.Run(invisibleForm);
                        }
                    }
                    else
                    {
                        // Modo normal con interfaz de usuario
                        // Crear un formulario invisible para mantener la aplicación en ejecución
                        using (Form invisibleForm = new Form())
                        {
                            invisibleForm.WindowState = FormWindowState.Minimized;
                            invisibleForm.ShowInTaskbar = false;
                            invisibleForm.FormBorderStyle = FormBorderStyle.None;
                            invisibleForm.Opacity = 0;

                            // Añadir ícono en la bandeja del sistema
                            using (NotifyIcon trayIcon = new NotifyIcon())
                            {
                                trayIcon.Icon = System.Drawing.SystemIcons.Application;
                                trayIcon.Text = "ECM Central - Monitor de impresión";
                                trayIcon.Visible = true;

                                // Menú contextual
                                trayIcon.ContextMenuStrip = new ContextMenuStrip();
                                trayIcon.ContextMenuStrip.Items.Add("Abrir aplicación", null, (s, e) => {
                                    Process.Start(new ProcessStartInfo
                                    {
                                        FileName = Application.ExecutablePath,
                                        UseShellExecute = true
                                    });
                                });
                                trayIcon.ContextMenuStrip.Items.Add("Comprobar estado", null, (s, e) => {
                                    WinFormsApiClient.VirtualWatcher.WatcherLogger.LogSystemDiagnostic();
                                    MessageBox.Show("Diagnóstico del sistema registrado en los logs", "ECM Central",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                                });
                                trayIcon.ContextMenuStrip.Items.Add("Salir", null, (s, e) => {
                                    WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.Stop();
                                    Application.Exit();
                                });

                                // Mantener la aplicación en ejecución
                                Application.Run(invisibleForm);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    WatcherLogger.LogError("Error al iniciar monitor de segundo plano", ex);
                    if (!silentMode)
                    {
                        MessageBox.Show($"Error al iniciar el monitor en segundo plano: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                return;
            }
            else if (argument.StartsWith("/silentprint="))
            {
                // Extraer la ruta del archivo
                string filePath = argument.Substring(13).Trim('"');
                Console.WriteLine($"Procesando archivo PDF en modo silencioso: {filePath}");

                // Registrar en el log para diagnóstico
                File.AppendAllText(
                    Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "silent_print.log"),
                    $"[{DateTime.Now}] Iniciando impresión silenciosa: {filePath}\r\n");

                try
                {
                    // Iniciar el monitor de background si no está en ejecución
                    if (!BackgroundMonitorService._isRunning)
                    {
                        // Inicializar sin mostrar interfaz visible
                        Console.WriteLine("Iniciando monitor en segundo plano para procesamiento silencioso");

                        // Configurar carpeta y automatización
                        ConfigurePrinterOutputFolder();

                        // Iniciar el servicio
                        BackgroundMonitorService.Start();

                        // Esperar un momento para que se inicie completamente
                        System.Threading.Thread.Sleep(1000);
                    }

                    // Procesar directamente el archivo PDF
                    if (File.Exists(filePath))
                    {
                        // Esperar a que el archivo esté completamente escrito
                        System.Threading.Thread.Sleep(800);

                        // Procesar el archivo directamente sin abrir UI
                        WinFormsApiClient.VirtualWatcher.DocumentProcessor.Instance.ProcessNewPrintJob(filePath);

                        // Salir silenciosamente una vez procesado
                        Environment.Exit(0);
                    }
                    else
                    {
                        File.AppendAllText(
                            Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "silent_print_error.log"),
                            $"[{DateTime.Now}] ERROR: Archivo no encontrado: {filePath}\r\n");
                        Environment.Exit(1);
                    }
                }
                catch (Exception ex)
                {
                    // Registrar error y salir
                    File.AppendAllText(
                        Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "silent_print_error.log"),
                        $"[{DateTime.Now}] Error en procesamiento silencioso: {ex.Message}\r\n{ex.StackTrace}\r\n");
                    Environment.Exit(1);
                }
                return;
            }
            else if (argument.StartsWith("/print:"))
            {
                // Extraer la ruta del archivo
                string filePath = argument.Substring(7);

                // Quitar comillas si existen
                if (filePath.StartsWith("\"") && filePath.EndsWith("\""))
                {
                    filePath = filePath.Substring(1, filePath.Length - 2);
                }

                Console.WriteLine($"Ruta de archivo extraída: {filePath}");

                if (File.Exists(filePath))
                {
                    Console.WriteLine($"Archivo encontrado, iniciando formulario de login con archivo pendiente: {filePath}");

                    // Guardar el archivo en una ubicación persistente si es necesario
                    string savedFilePath = filePath;

                    // Crear formulario de login con el archivo pendiente
                    LoginForm loginForm = new LoginForm(savedFilePath);
                    Application.Run(loginForm);
                    return;
                }
                else
                {
                    Console.WriteLine($"ERROR: Archivo no encontrado: {filePath}");
                    MessageBox.Show($"No se encontró el archivo: {filePath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    // El archivo no existe, iniciar normal
                    Application.Run(new LoginForm());
                    return;
                }
            }
            else if (argument == "/install-autostart")
            {
                // Instalar el servicio de monitoreo para que se inicie con Windows
                Console.WriteLine("Instalando servicio de monitoreo para inicio automático");

                // Verificar si ya está instalado
                bool alreadyInstalled = WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.IsInstalledForAutostart();

                if (alreadyInstalled)
                {
                    Console.WriteLine("El servicio ya está instalado, verificando impresora...");
                    // Solo verificar la impresora y reiniciar el servicio
                    if (!ECMVirtualPrinter.IsPrinterInstalled())
                    {
                        Console.WriteLine("Impresora no instalada, iniciando instalación...");
                        ECMVirtualPrinter.InstallPrinterAsync(true).Wait();
                    }

                    MessageBox.Show(
                        "El servicio de monitoreo ya está instalado y configurado para iniciarse con Windows.",
                        "Servicio activo",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Reiniciar el servicio
                    Process.Start(Application.ExecutablePath, "/backgroundmonitor");
                    return;
                }

                bool success = VirtualPrinter.PrinterInstaller.InstallBackgroundMonitor();

                if (success)
                {
                    MessageBox.Show(
                        "El servicio de monitoreo de impresión se ha instalado correctamente y se iniciará automáticamente con Windows.",
                        "Instalación completa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "No se pudo instalar el servicio de monitoreo. Por favor, inténtelo de nuevo con permisos de administrador.",
                        "Error de instalación",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            else if (argument == "/uninstall-autostart")
            {
                // Desinstalar el servicio de autoarranque
                Console.WriteLine("Desinstalando servicio de monitoreo del inicio automático");
                bool success = WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.UninstallAutostart();

                if (success)
                {
                    MessageBox.Show(
                        "El servicio de monitoreo se ha eliminado del inicio automático.",
                        "Desinstalación completa",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(
                        "No se pudo desinstalar el servicio de monitoreo. Por favor, inténtelo de nuevo con permisos de administrador.",
                        "Error de desinstalación",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            else if (argument == "/status")
            {
                // Mostrar estado actual del sistema de impresión
                Console.WriteLine("Verificando estado del sistema de impresión");

                // Realizar diagnóstico
                WinFormsApiClient.VirtualWatcher.WatcherLogger.LogSystemDiagnostic();

                // Verificar si la impresora está instalada
                bool printerInstalled = ECMVirtualPrinter.IsPrinterInstalled();

                // Verificar si el servicio está instalado para autoarranque
                bool serviceInstalled = WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.IsInstalledForAutostart();

                // Mostrar mensaje de estado
                MessageBox.Show(
                    $"Estado del sistema de impresión ECM Central:\n\n" +
                    $"- Impresora instalada: {(printerInstalled ? "Sí" : "No")}\n" +
                    $"- Servicio de monitoreo en inicio automático: {(serviceInstalled ? "Sí" : "No")}\n\n" +
                    "Se ha generado un diagnóstico completo en los archivos de log.",
                    "Estado del sistema",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                return; // Este return es crucial - si falta, seguirá ejecutando la aplicación
            }
            else
            {
                Console.WriteLine("Argumento no reconocido, iniciando aplicación normalmente");
                // Si no es un comando reconocido, iniciar normal
                Application.Run(new LoginForm());
            }
        }

        /// <summary>
        /// Inicia el monitor de impresora en segundo plano
        /// </summary>
        private static void StartPrinterWatcher()
        {
            try
            {
                // Configurar la carpeta de salida de la impresora
                ConfigurePrinterOutputFolder();

                // Iniciar el watcher en un hilo separado
                System.Threading.ThreadPool.QueueUserWorkItem(state =>
                {
                    try
                    {
                        PrinterWatcher.Instance.StartWatching();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al iniciar PrinterWatcher: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear el PrinterWatcher: {ex.Message}");
            }
        }

        /// <summary>
        /// Asegura que la carpeta de imágenes exista y contiene los archivos necesarios
        /// </summary>
        private static void EnsureImagesFolder()
        {
            try
            {
                // Crear la carpeta de imágenes si no existe
                string imagesFolder = Path.Combine(Application.StartupPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                    Console.WriteLine($"Carpeta de imágenes creada: {imagesFolder}");
                }

                CopyImageIfMissing(imagesFolder, "ecmicon.ico", true);
                CopyImageIfMissing(imagesFolder, "ecmlogofm.png", true);
                CopyImageIfMissing(imagesFolder, "illustration.png", false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar la carpeta de imágenes: {ex.Message}");
                // Continuamos con la ejecución aunque falle
            }
        }

        /// <summary>
        /// Copia un archivo de imagen si no existe en la carpeta de destino
        /// </summary>
        private static void CopyImageIfMissing(string folder, string filename, bool required)
        {
            try
            {
                string targetPath = Path.Combine(folder, filename);
                if (!File.Exists(targetPath))
                {
                    // Buscar el archivo en varias posibles ubicaciones
                    string[] possibleSourcePaths = {
                Path.Combine(Application.StartupPath, filename),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename),
                Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), filename),
                Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "images", filename)
            };

                    bool copied = false;
                    foreach (string sourcePath in possibleSourcePaths)
                    {
                        if (File.Exists(sourcePath))
                        {
                            try
                            {
                                // Intentar copiar el archivo usando un método que evita bloqueos
                                using (FileStream source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                using (FileStream destination = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None))
                                {
                                    byte[] buffer = new byte[4096];
                                    int bytesRead;
                                    while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        destination.Write(buffer, 0, bytesRead);
                                    }
                                }

                                Console.WriteLine($"Archivo {filename} copiado de {sourcePath} a {targetPath}");
                                copied = true;
                                break;
                            }
                            catch (IOException ex)
                            {
                                Console.WriteLine($"No se pudo copiar {filename}: {ex.Message}");
                            }
                        }
                    }

                    if (!copied && required)
                    {
                        Console.WriteLine($"ADVERTENCIA: No se pudo encontrar o copiar {filename}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al copiar {filename}: {ex.Message}");
            }
        }

        private static void ConfigurePrinterOutputFolder()
        {
            try
            {
                // Asegurar que la carpeta existe
                VirtualPrinter.VirtualPrinterCore.EnsureOutputFolderExists();

                string outputFolder = VirtualPrinter.VirtualPrinterCore.GetOutputFolderPath();
                Console.WriteLine($"Configurando impresora para usar carpeta: {outputFolder}");

                // Si estamos usando Microsoft Print to PDF, iniciar la automatización del diálogo
                if (VirtualPrinter.VirtualPrinterCore.DRIVER_NAME == "Microsoft Print To PDF")
                {
                    // Solo verificar que el directorio existe
                    if (!Directory.Exists(outputFolder))
                    {
                        Directory.CreateDirectory(outputFolder);
                        Console.WriteLine($"Carpeta de salida creada: {outputFolder}");
                    }

                    // Verificar si ya hay una instancia del script ejecutándose
                    string scriptName = Path.GetFileNameWithoutExtension(PDFDialogAutomation.AutoITCompiledPath);
                    Process[] processes = Process.GetProcessesByName(scriptName);

                    if (processes.Length > 0)
                    {
                        Console.WriteLine($"La automatización de diálogo ya está activa (PID: {processes[0].Id})");
                    }
                    else
                    {
                        // Intentar iniciar la automatización de diálogo
                        try
                        {
                            bool automated = PDFDialogAutomation.StartDialogAutomation();
                            Console.WriteLine($"Automatización de diálogo inicializada: {(automated ? "Exitosa" : "Fallida")}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error al iniciar automatización de diálogo: {ex.Message}");

                            // Intentar completar la operación sin bloquear la aplicación
                            System.Threading.ThreadPool.QueueUserWorkItem(state =>
                            {
                                try
                                {
                                    // Asegurar que exista la carpeta de destino
                                    VirtualPrinter.VirtualPrinterCore.EnsureOutputFolderExists();

                                    // Como último recurso, crear un archivo batch que lance la aplicación AutoIt3
                                    string batchFilePath = Path.Combine(Path.GetTempPath(), "ECMCentralAutoIT", "StartAutomation.bat");
                                    string scriptPath = Path.Combine(Path.GetTempPath(), "ECMCentralAutoIT", "PDFSaveDialogAutomation.au3");

                                    if (File.Exists(scriptPath))
                                    {
                                        string batchContent = @"@echo off
echo Iniciando automatizacion de dialogos PDF...
if exist ""C:\Program Files\AutoIt3\AutoIt3.exe"" (
    ""C:\Program Files\AutoIt3\AutoIt3.exe"" """ + scriptPath + @"""
) else if exist ""C:\Program Files (x86)\AutoIt3\AutoIt3.exe"" (
    ""C:\Program Files (x86)\AutoIt3\AutoIt3.exe"" """ + scriptPath + @"""
) else (
    echo AutoIt3.exe no encontrado
)";
                                        File.WriteAllText(batchFilePath, batchContent);

                                        // Ejecutar el batch silenciosamente
                                        ProcessStartInfo startInfo = new ProcessStartInfo
                                        {
                                            FileName = batchFilePath,
                                            CreateNoWindow = true,
                                            UseShellExecute = false
                                        };
                                        Process.Start(startInfo);
                                    }
                                }
                                catch (Exception batchEx)
                                {
                                    Console.WriteLine($"Error al crear script batch: {batchEx.Message}");
                                }
                            });
                        }
                    }
                }
                else
                {
                    // Configurar la impresora si no es Microsoft Print to PDF
                    if (VirtualPrinter.VirtualPrinterCore.IsPrinterInstalled())
                    {
                        string psCommand = $@"
                try {{
                    $printer = Get-Printer -Name '{VirtualPrinter.VirtualPrinterCore.PRINTER_NAME}' -ErrorAction SilentlyContinue
                    if ($printer) {{
                        $outputPath = '{outputFolder.Replace("\\", "\\\\")}'
                        Set-PrintConfiguration -PrinterName '{VirtualPrinter.VirtualPrinterCore.PRINTER_NAME}' -PrintTicketXML '<PrintTicket xmlns=""http://schemas.microsoft.com/windows/2003/08/printing/printticket""><ParameterInit name=""FileNameSettings""><StringParameter name=""DocumentNameExtension"">.pdf</StringParameter><StringParameter name=""Directory"">' + $outputPath + '</StringParameter></ParameterInit></PrintTicket>' -ErrorAction SilentlyContinue
                        Write-Output ""Impresora configurada correctamente para usar carpeta: $outputPath""
                    }} else {{
                        Write-Output ""La impresora no existe, no se puede configurar la carpeta de salida""
                    }}
                }} catch {{
                    Write-Output ""Error al configurar carpeta de salida: $($_.Exception.Message)""
                }}
            ";
                        VirtualPrinter.PowerShellHelper.RunPowerShellCommand(psCommand);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar carpeta de salida en impresora: {ex.Message}");
            }
        }

    }
}