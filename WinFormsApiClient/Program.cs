using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices; // Añadir esta línea
using System.Text; // Para Encoding
using System.Threading;
using System.Threading.Tasks; // Agregar esta línea para Task
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;
using WinFormsApiClient.VirtualWatcher;
namespace WinFormsApiClient
{
    static class Program
    {
        static Mutex monitorMutex;

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.ThreadException += (sender, e) => {
                try
                {
                    Console.WriteLine($"Excepción no controlada en hilo de UI: {e.Exception.Message}");
                    Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");

                    // Registrar en archivo de log
                    File.AppendAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_error_log.txt"),
                        $"[{DateTime.Now}] Excepción en UI: {e.Exception.Message}\r\n{e.Exception.StackTrace}\r\n\r\n");

                    MessageBox.Show(
                        $"Se ha producido un error en la aplicación:\n{e.Exception.Message}\n\nLa aplicación intentará continuar.",
                        "Error en la aplicación",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                catch
                {
                    // Si falla el manejo de excepciones, no podemos hacer mucho más
                }
            };

            // Configurar manejo global para excepciones en otros hilos
            AppDomain.CurrentDomain.UnhandledException += (sender, e) => {
                try
                {
                    var ex = e.ExceptionObject as Exception;
                    Console.WriteLine($"Excepción no controlada en AppDomain: {ex?.Message}");

                    File.AppendAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_error_log.txt"),
                        $"[{DateTime.Now}] Excepción en AppDomain: {ex?.Message}\r\n{ex?.StackTrace}\r\n\r\n");
                }
                catch
                {
                    // Si falla el manejo de excepciones, no podemos hacer mucho más
                }
            };
            // Configurar manejo de excepciones no controladas
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                if (e.ExceptionObject is Exception ex)
                    WatcherLogger.LogError("Excepción no controlada a nivel de dominio", ex);
            };
            Application.ThreadException += (s, e) =>
                WatcherLogger.LogError("Excepción no controlada en hilo de UI", e.Exception);
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    // Si detectamos que se está intentando ejecutar un batch, hacerlo silenciosamente
                    if (arg.EndsWith(".bat", StringComparison.OrdinalIgnoreCase) && File.Exists(arg))
                    {
                        // Ejecutar silenciosamente en lugar de mostrar ventana
                        //VirtualPrinter.VirtualPrinterCore.RunBatchSilently(arg);
                        return; // Salir sin mostrar ventana
                    }
                }

                ProcessCommandLineArguments(args);
                return;
            }
            // Procesar argumentos
            bool isSilentMode = args.Contains("/silent");
            bool isBackgroundMonitor = args.Contains("/backgroundmonitor");

            if (isBackgroundMonitor)
            {
                bool createdNew = false;
                monitorMutex = new Mutex(true, "Global\\ECMBackgroundMonitorMutex", out createdNew);
                if (!createdNew)
                {
                    Console.WriteLine("Ya existe una instancia del monitor ejecutándose.");
                    return;
                }

                // Asegurar que se crea la carpeta de salida y los archivos necesarios
                try
                {
                    // Crear la carpeta de salida si no existe
                    if (!Directory.Exists(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH))
                    {
                        Directory.CreateDirectory(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH);
                        Console.WriteLine($"Carpeta creada: {VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH}");
                    }

                    // Forzar la creación del archivo MoverPDFs.bat
                    VirtualPrinter.PDFDialogAutomation.CreateBackupBatchFile();
                    Console.WriteLine("Archivo MoverPDFs.bat verificado/creado");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al inicializar archivos: {ex.Message}");
                }

                // Crear icono en la bandeja del sistema
                NotifyIcon trayIcon = new NotifyIcon();
                
                try
                {
                    // Intentar cargar el icono de la aplicación
                    trayIcon.Icon = AppIcon.DefaultIcon ?? SystemIcons.Application;
                    trayIcon.Text = "ECM Central - Monitor de impresión activo";
                    trayIcon.Visible = true;
                    Console.WriteLine("Icono de bandeja del sistema configurado correctamente");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al configurar icono: {ex.Message}");
                    trayIcon.Icon = SystemIcons.Application; // Fallback
                    trayIcon.Text = "ECM Central - Monitor activo";
                    trayIcon.Visible = true;
                }

                // Configurar menú contextual del icono
                ContextMenuStrip contextMenu = new ContextMenuStrip();
                
                // Opción para abrir la aplicación principal
                ToolStripMenuItem openApp = new ToolStripMenuItem("Abrir ECM Central");
                openApp.Click += (s, e) => {
                    try
                    {
                        Process.Start(Application.ExecutablePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al abrir aplicación: {ex.Message}", 
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };
                contextMenu.Items.Add(openApp);

                // Separador
                contextMenu.Items.Add(new ToolStripSeparator());

                // Opción para detener el monitor
                ToolStripMenuItem stopMonitor = new ToolStripMenuItem("Detener monitor");
                stopMonitor.Click += (s, e) => {
                    if (MessageBox.Show("¿Está seguro de que desea detener el monitor de impresión?", 
                        "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        BackgroundMonitorService.Stop();
                        Application.Exit();
                    }
                };
                contextMenu.Items.Add(stopMonitor);

                // Asignar el menú al icono
                trayIcon.ContextMenuStrip = contextMenu;

                // Configurar doble clic para abrir la aplicación
                trayIcon.DoubleClick += (s, e) => {
                    try
                    {
                        Process.Start(Application.ExecutablePath);
                    }
                    catch { }
                };

                if (isSilentMode)
                {
                    WatcherLogger.LogActivity("Iniciando monitor en modo silencioso");
                    BackgroundMonitorService.StartSilently();
                }
                else
                {
                    BackgroundMonitorService.Start();
                }

                Application.Run(); // Mantener vivo el proceso con icono en bandeja
                
                // Cleanup al salir
                trayIcon.Visible = false;
                trayIcon.Dispose();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Configurar mecanismo de recuperación para evitar congelaciones
            ConfigureApplicationRecovery();
            
            // Modo normal de la aplicación
            Application.Run(new LoginForm());
        }

        private static void ConfigureApplicationRecovery()
        {
            try
            {
                // Configurar recuperación de aplicación
                ApplicationRecoveryManager.RegisterForApplicationRecovery(new ApplicationRecoveryManager.RecoveryCallback((state) =>
                {
                    try
                    {
                        // Generar un nombre de archivo único para cada proceso
                        string uniqueFileName = $"ecm_recovery_{Process.GetCurrentProcess().Id}_{DateTime.Now:yyyyMMdd_HHmmss}.log";
                        string recoveryLogPath = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                            uniqueFileName);

                        // Crear un mensaje de diagnóstico
                        string recoveryMessage = $"[{DateTime.Now}] Recuperación iniciada desde estado de aplicación no respondiente\r\n";

                        // Usar FileShare.ReadWrite para permitir acceso simultáneo si es necesario
                        using (StreamWriter writer = new StreamWriter(recoveryLogPath, true, Encoding.UTF8))
                        {
                            writer.WriteLine(recoveryMessage);
                            writer.WriteLine($"[{DateTime.Now}] Proceso ID: {Process.GetCurrentProcess().Id}");
                            writer.WriteLine($"[{DateTime.Now}] Archivos abiertos en procesamiento:");

                            // Añadir información sobre archivos en procesamiento
                            try
                            {
                                string pendingFilePath = Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt");
                                if (File.Exists(pendingFilePath))
                                {
                                    string pendingFile = File.ReadAllText(pendingFilePath);
                                    writer.WriteLine($"[{DateTime.Now}] Archivo pendiente: {pendingFile}");
                                }
                            }
                            catch (Exception ex)
                            {
                                writer.WriteLine($"[{DateTime.Now}] Error al leer archivo pendiente: {ex.Message}");
                            }

                            writer.Flush();
                        }

                        // Registrar en consola también
                        Console.WriteLine(recoveryMessage);
                        Console.WriteLine($"Log de recuperación creado en: {recoveryLogPath}");

                        // Indicar a Windows que la recuperación fue exitosa
                        ApplicationRecoveryManager.ApplicationRecoveryFinished(true);
                        return 0;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error en recuperación: {ex.Message}");
                        ApplicationRecoveryManager.ApplicationRecoveryFinished(false);
                        return -1;
                    }
                }), IntPtr.Zero, 30000, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar recuperación: {ex.Message}");
            }
        }
        /// <summary>
        /// Proporciona acceso a la API de recuperación de aplicaciones de Windows
        /// </summary>
        internal static class ApplicationRecoveryManager
        {
            // Delegado para la función de callback de recuperación
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate int RecoveryCallback(IntPtr pvParameter);

            // Función P/Invoke para registrar la aplicación para recuperación
            [DllImport("kernel32.dll")]
            public static extern int RegisterApplicationRecoveryCallback(
                RecoveryCallback pRecoveryCallback,
                IntPtr pvParameter,
                int dwPingInterval,
                int dwFlags);

            // Función P/Invoke para notificar que la recuperación ha finalizado
            [DllImport("kernel32.dll")]
            public static extern int ApplicationRecoveryFinished(
                bool bSuccess);

            // Función P/Invoke para cancelar la recuperación
            [DllImport("kernel32.dll")]
            public static extern int ApplicationRecoveryInProgress(
                out bool pbCancelled);

            /// <summary>
            /// Envuelve la función nativa para registrar la recuperación de la aplicación
            /// </summary>
            public static void RegisterForApplicationRecovery(RecoveryCallback callback, IntPtr parameter, int pingInterval, int flags)
            {
                int result = RegisterApplicationRecoveryCallback(callback, parameter, pingInterval, flags);
                if (result != 0)
                {
                    throw new InvalidOperationException($"Error al registrar para recuperación: código {result}");
                }
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
        public static string GetProcessCommandLine(int processId)
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
        private static void EnsureBackgroundMonitorStartsWithWindows()
        {
            try
            {
                string logPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "autostart_setup.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Configurando inicio automático con Windows\r\n");

                // IMPORTANTE: USAR SOLO UN MÉTODO DE AUTOARRANQUE
                // Configurar inicio mediante tarea programada (más confiable) y no usar registro
                bool taskResult = CreateStartupTask();
                File.AppendAllText(logPath, $"[{DateTime.Now}] Resultado configuración por tarea programada: {taskResult}\r\n");

                // No usar BackgroundMonitorService.InstallAutostart() - para evitar duplicación
            }
            catch (Exception ex)
            {
                // Registrar error
            }
        }

        // Modificar el método CreateStartupTask para usar opciones de ejecución invisibles
        private static bool CreateStartupTask()
        {
            try
            {
                string appPath = Application.ExecutablePath;
                string logPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "startup_task.log");

                // SOLUCIÓN: Verificar si ya existe la tarea programada
                string checkExistingScript = @"
try {
    $taskName = 'ECMCentralMonitor'
    $task = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($task -ne $null) {
        # Verificar la acción y los parámetros para determinar si es necesario actualizar
        $action = $task.Actions[0]
        $arguments = $action.Arguments
        
        # Verificar si los argumentos ya incluyen /silent
        if ($arguments -match '/backgroundmonitor /silent') {
            Write-Output 'La tarea programada ya existe con los parámetros correctos'
            return $true
        }
        else {
            # Devuelve que la tarea existe pero debe actualizarse
            Write-Output 'La tarea programada existe pero necesita actualización'
            return $true
        }
    }
    else {
        # La tarea no existe
        Write-Output 'La tarea programada no existe'
        return $false
    }
}
catch {
    Write-Output ('Error al verificar tarea programada: ' + $_.Exception.Message)
    return $false
}";

                string existingTaskResult = PowerShellHelper.RunPowerShellCommandWithOutput(checkExistingScript);
                File.AppendAllText(logPath, $"[{DateTime.Now}] Verificación de tarea existente: {existingTaskResult}\r\n");

                // Si la tarea ya está configurada correctamente, no hacer nada
                if (existingTaskResult.Contains("La tarea programada ya existe con los parámetros correctos"))
                {
                    return true;
                }

                // Script PowerShell para crear/actualizar la tarea programada
                string psScript = $@"
try {{
    $taskName = 'ECMCentralMonitor'
    $exe = '{appPath.Replace("\\", "\\\\")}'
    $action = New-ScheduledTaskAction -Execute $exe -Argument '/backgroundmonitor /silent'
    
    # Definir UN SOLO disparador para evitar duplicidad
    # Al inicio de sesión (más común y fiable)
    $trigger = New-ScheduledTaskTrigger -AtLogon
    
    # Configuración optimizada - añadir opción Hidden
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -Hidden

    # IMPORTANTE: Usar RunLevel Normal para evitar prompts UAC
    $principal = New-ScheduledTaskPrincipal -GroupId 'BUILTIN\Users' -RunLevel Normal
    
    # Si ya existe la tarea, eliminarla primero
    Unregister-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue -Confirm:$false
    
    # Crear la tarea
    $task = Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger -Settings $settings -Principal $principal
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

                // Verificar si la ruta es relativa (solo nombre de archivo)
                if (!Path.IsPathRooted(filePath))
                {
                    // Convertir a ruta absoluta usando la carpeta de salida
                    filePath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, filePath);
                }

                // Guardar para uso posterior en caso de login
                string tempFile = Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt");
                File.WriteAllText(tempFile, filePath);
                Console.WriteLine($"Archivo pendiente guardado en: {tempFile}");

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
            // else if (argument == "/backgroundmonitor")
            // {
            //     // Verificar flags adicionales
            //     bool silentMode = args.Any(a => a.ToLower() == "/silent");
            //     bool lowImpactMode = args.Any(a => a.ToLower() == "/lowimpact");
            //     bool synchronizedMode = args.Any(a => a.ToLower() == "/waitforsync"); // Añadir esta línea

            //     Console.WriteLine("Iniciando modo de monitor en segundo plano" +
            //         (silentMode ? " silenciosamente" : "") +
            //         (lowImpactMode ? " (impacto reducido)" : "") +
            //         (synchronizedMode ? " (modo sincronización)" : ""));

            //     // En modo de bajo impacto, limitamos la frecuencia de log
            //     if (lowImpactMode)
            //     {
            //         WatcherLogger.SetLoggingInterval(TimeSpan.FromMinutes(30));
            //     }

            //     // Registrar en init_log.txt
            //     string initLogPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.GetLogFolderPath(), "init_log.txt");
            //     File.AppendAllText(initLogPath,
            //         $"[{DateTime.Now}] INICIO DE MONITOR DE FONDO" +
            //         (synchronizedMode ? " (modo sincronización)" : "") +
            //         (silentMode ? " (modo silencioso)" : "") + "\r\n");

            //     // Crear inmediatamente el archivo de marcador
            //     string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
            //     File.WriteAllText(markerFilePath, Process.GetCurrentProcess().Id.ToString());

            //     // Registrar eliminación del archivo al cerrar
            //     AppDomain.CurrentDomain.ProcessExit += (s, e) => {
            //         try { if (File.Exists(markerFilePath)) File.Delete(markerFilePath); } catch { }
            //     };

            //     WatcherLogger.LogActivity("Iniciando modo de monitor en segundo plano" +
            //         (silentMode ? " silenciosamente" : "") +
            //         (synchronizedMode ? " con sincronización" : ""));

            //     try
            //     {
            //         // Configurar la carpeta de salida de la impresora
            //         ConfigurePrinterOutputFolder();

            //         // Iniciar el servicio de monitoreo - MODIFICADO PARA SINCRONIZACIÓN
            //         if (synchronizedMode)
            //         {
            //             WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.StartAndNotify();
            //         }
            //         else
            //         {
            //             WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.Start();
            //         }

            //         // En modo silencioso, evitar mostrar ventanas
            //         if (silentMode)
            //         {
            //             // Crear un formulario invisible para mantener la aplicación en ejecución
            //             Form invisibleForm = new Form
            //             {
            //                 WindowState = FormWindowState.Minimized,
            //                 ShowInTaskbar = false,
            //                 FormBorderStyle = FormBorderStyle.None,
            //                 Size = new System.Drawing.Size(1, 1),
            //                 Opacity = 0
            //             };

            //             // Asegurarse que no se muestre ni siquiera por un momento
            //             invisibleForm.Load += (sender, e) => invisibleForm.Hide();

            //             // Usar el método existente para mostrar el ícono en la bandeja del sistema
            //             using (NotifyIcon trayIcon = FormInteraction.ShowTrayIcon(
            //                 "ECM Central - Monitor de impresión"))
            //             {
            //                 // Mantener la aplicación en ejecución
            //                 Application.Run(invisibleForm);
            //             }
            //         }
            //         else
            //         {
            //             // Modo normal con interfaz de usuario
            //             // Crear un formulario invisible para mantener la aplicación en ejecución
            //             using (Form invisibleForm = new Form())
            //             {
            //                 invisibleForm.WindowState = FormWindowState.Minimized;
            //                 invisibleForm.ShowInTaskbar = false;
            //                 invisibleForm.FormBorderStyle = FormBorderStyle.None;
            //                 invisibleForm.Opacity = 0;

            //                 // Añadir ícono en la bandeja del sistema
            //                 using (NotifyIcon trayIcon = new NotifyIcon())
            //                 {
            //                     trayIcon.Icon = System.Drawing.SystemIcons.Application;
            //                     trayIcon.Text = "ECM Central - Monitor de impresión";
            //                     trayIcon.Visible = true;

            //                     // Menú contextual
            //                     trayIcon.ContextMenuStrip = new ContextMenuStrip();
            //                     trayIcon.ContextMenuStrip.Items.Add("Abrir aplicación", null, (s, e) => {
            //                         Process.Start(new ProcessStartInfo
            //                         {
            //                             FileName = Application.ExecutablePath,
            //                             UseShellExecute = true
            //                         });
            //                     });
            //                     trayIcon.ContextMenuStrip.Items.Add("Comprobar estado", null, (s, e) => {
            //                         WinFormsApiClient.VirtualWatcher.WatcherLogger.LogSystemDiagnostic();
            //                         MessageBox.Show("Diagnóstico del sistema registrado en los logs", "ECM Central",
            //                             MessageBoxButtons.OK, MessageBoxIcon.Information);
            //                     });
            //                     trayIcon.ContextMenuStrip.Items.Add("Salir", null, (s, e) => {
            //                         WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.Stop();
            //                         Application.Exit();
            //                     });

            //                     // Mantener la aplicación en ejecución
            //                     Application.Run(invisibleForm);
            //                 }
            //             }
            //         }
            //     }
            //     catch (Exception ex)
            //     {
            //         WatcherLogger.LogError("Error al iniciar monitor de segundo plano", ex);
            //         if (!silentMode)
            //         {
            //             MessageBox.Show($"Error al iniciar el monitor en segundo plano: {ex.Message}",
            //                 "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            //         }
            //     }
            //     return;
            // }
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
                Console.WriteLine("Instalando autostart y configurando sistema completo...");

                // 1. Instalar autostart normal
                bool success = BackgroundMonitorService.InstallAutostart();
                bool alreadyInstalled = BackgroundMonitorService.IsInstalledForAutostart();

                if (alreadyInstalled)
                {
                    Console.WriteLine("Autostart ya instalado, verificando configuración completa...");

                    // AQUÍ ES DONDE FALTA - FORZAR LA CONFIGURACIÓN COMPLETA
                    ForceCompleteSystemConfiguration();

                    if (!ECMVirtualPrinter.IsPrinterInstalled())
                    {
                        Console.WriteLine("Impresora no detectada, iniciando instalación...");
                        Task.Run(async () => await PrinterInstaller.InstallPrinterAsync(true)).Wait();
                    }
                }

                if (success)
                {
                    Console.WriteLine("Autostart instalado/verificado correctamente");
                    MessageBox.Show("El monitor se iniciará automáticamente con Windows.", "Autostart configurado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    Console.WriteLine("Error instalando autostart");
                    MessageBox.Show("Error al configurar el autostart", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }
            else if (argument == "/diagnose-bullzip")
            {
                Console.WriteLine("=== DIAGNÓSTICO DE BULLZIP ===");

                // Verificar si Bullzip está instalado
                bool bullzipExists = false;
                foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    if (printer.Contains("Bullzip") || printer.Contains("PDF Printer"))
                    {
                        Console.WriteLine($"Bullzip encontrado: {printer}");
                        bullzipExists = true;
                    }
                }

                if (!bullzipExists)
                {
                    Console.WriteLine("PROBLEMA: Bullzip NO está instalado");
                    Console.WriteLine("Ejecutando instalación...");
                    bool result = VirtualPrinter.PDFDialogAutomation.InitBullzipAtStartup();
                    Console.WriteLine($"Resultado instalación: {result}");
                }

                // Verificar archivos necesarios
                string batchPath = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "MoverPDFs.bat");
                Console.WriteLine($"MoverPDFs.bat existe: {File.Exists(batchPath)}");

                if (!File.Exists(batchPath))
                {
                    Console.WriteLine("Creando MoverPDFs.bat...");
                    VirtualPrinter.PDFDialogAutomation.CreateBackupBatchFile();
                    Console.WriteLine($"Creado: {File.Exists(batchPath)}");
                }

                // Verificar configuración
                string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bullzip", "PDF Printer", "settings.ini");
                Console.WriteLine($"Configuración Bullzip existe: {File.Exists(configPath)}");

                Console.WriteLine("=== FIN DIAGNÓSTICO ===");
                return;
            }
            else if (argument == "/force-bullzip-setup")
            {
                ForceCompleteBullzipSetup();
                return;
            }
        } // Cierre del método ProcessCommandLineArguments
        private static void ForceCompleteBullzipSetup()
{
    try
    {
        Console.WriteLine("=== FORZANDO CONFIGURACIÓN COMPLETA DE BULLZIP ===");
        
        // 1. Verificar si Bullzip está instalado
        bool bullzipExists = false;
        foreach (string printer in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
        {
            if (printer.Contains("Bullzip") || printer.Contains("PDF Printer"))
            {
                bullzipExists = true;
                Console.WriteLine($"Bullzip encontrado: {printer}");
                break;
            }
        }
        
        // 2. Si no existe, intentar instalarlo
        if (!bullzipExists)
        {
            Console.WriteLine("Bullzip no encontrado, ejecutando instalación...");
            
            // Método 1: Usar ForceBullzipSetup
            bool setupResult = VirtualPrinter.PDFDialogAutomation.ForceBullzipSetup();
            Console.WriteLine($"Resultado ForceBullzipSetup: {setupResult}");
            
            // Método 2: Usar InitBullzipAtStartup
            bool initResult = VirtualPrinter.PDFDialogAutomation.InitBullzipAtStartup();
            Console.WriteLine($"Resultado InitBullzipAtStartup: {initResult}");
        }
        
        // 3. Configurar Bullzip existente
        bool configResult = VirtualPrinter.VirtualPrinterCore.ConfigureBullzipPrinter();
        Console.WriteLine($"Resultado configuración: {configResult}");
        
        // 4. Crear archivos batch
        VirtualPrinter.PDFDialogAutomation.CreateBackupBatchFile();
        Console.WriteLine("MoverPDFs.bat creado");
        
        // 5. Configurar monitor para Bullzip
        bool monitorResult = VirtualPrinter.VirtualPrinterCore.EnsureMonitorForBullzip();
        Console.WriteLine($"Resultado monitor Bullzip: {monitorResult}");
        
        Console.WriteLine("=== CONFIGURACIÓN COMPLETA FINALIZADA ===");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en configuración completa: {ex.Message}");
    }
}
        // AGREGAR EL MÉTODO DENTRO DE LA CLASE Program
        private static void ForceCompleteSystemConfiguration()
        {
            try
            {
                Console.WriteLine("=== INICIANDO CONFIGURACIÓN COMPLETA DEL SISTEMA ===");
                
                // 1. Forzar inicialización de Bullzip (método existente)
                Console.WriteLine("1. Verificando e instalando Bullzip...");
                bool bullzipResult = VirtualPrinter.PDFDialogAutomation.InitBullzipAtStartup();
                Console.WriteLine($"   Resultado Bullzip: {bullzipResult}");
                
                // 2. Configurar Bullzip (método existente)
                Console.WriteLine("2. Configurando Bullzip...");
                bool configResult = VirtualPrinter.VirtualPrinterCore.ConfigureBullzipPrinter();
                Console.WriteLine($"   Resultado configuración: {configResult}");
                
                // 3. Crear archivo batch (método existente)
                Console.WriteLine("3. Creando MoverPDFs.bat...");
                VirtualPrinter.PDFDialogAutomation.CreateBackupBatchFile();
                Console.WriteLine("   MoverPDFs.bat creado");
                
                // 4. Asegurar carpeta de salida
                Console.WriteLine("4. Verificando carpeta de salida...");
                VirtualPrinter.VirtualPrinterCore.EnsureOutputFolderExists();
                Console.WriteLine("   Carpeta verificada");
                
                // 5. Forzar configuración del monitor para Bullzip
                Console.WriteLine("5. Configurando monitor para Bullzip...");
                bool monitorResult = VirtualPrinter.VirtualPrinterCore.EnsureMonitorForBullzip();
                Console.WriteLine($"   Resultado monitor: {monitorResult}");
                
                Console.WriteLine("=== CONFIGURACIÓN COMPLETA FINALIZADA ===");
                
                // Crear archivo de estado para verificar que se ejecutó
                string statusFile = Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "system_configured.marker");
                File.WriteAllText(statusFile, $"Sistema configurado completamente el {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en configuración completa: {ex.Message}");
            }
        }

        // AGREGAR EL MÉTODO ConfigurePrinterOutputFolder QUE FALTA
        private static void ConfigurePrinterOutputFolder()
        {
            try
            {
                // Asegurar que existe la carpeta de salida
                VirtualPrinter.VirtualPrinterCore.EnsureOutputFolderExists();
                
                // Crear el archivo batch si no existe
                VirtualPrinter.PDFDialogAutomation.CreateBackupBatchFile();
                
                Console.WriteLine("Carpeta de salida de impresora configurada correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar carpeta de salida: {ex.Message}");
            }
        }
    } // Cierre de la clase Program
} // Cierre del namespace