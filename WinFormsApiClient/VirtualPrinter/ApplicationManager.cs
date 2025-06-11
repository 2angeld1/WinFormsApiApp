using System;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WinFormsApiClient.VirtualWatcher;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para gestionar la ejecución de la aplicación
    /// </summary>
    public class ApplicationManager
    {
        private static bool _initialized = false;

        static ApplicationManager()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente ApplicationManager...");
                    _initialized = true;
                    Console.WriteLine("Componente ApplicationManager inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente ApplicationManager: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Verifica si la aplicación está en ejecución e intenta iniciarla si no lo está
        /// </summary>
        /// <param name="timeoutSeconds">Tiempo máximo de espera en segundos (predeterminado: 30 segundos)</param>
        /// <returns>True si la aplicación está o se inició correctamente, False en caso contrario</returns>
        public static bool EnsureApplicationIsRunning(int timeoutSeconds = 30)
        {
            bool isRunning = false;
            string errorLog = string.Empty;

            try
            {
                // Obtener la carpeta de logs en el directorio de la aplicación
                string logFolder = VirtualPrinterCore.GetLogFolderPath();
                string logFile = Path.Combine(logFolder, $"printer_error_{DateTime.Now:yyyyMMdd}.log");

                // Verificar si alguna instancia de nuestra aplicación está ejecutándose
                Process currentProcess = Process.GetCurrentProcess();
                Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);

                // Si hay más de una instancia (la actual), la aplicación ya está en ejecución
                if (processes.Length > 1)
                {
                    Console.WriteLine("La aplicación ya está en ejecución");
                    File.AppendAllText(logFile, $"[{DateTime.Now}] La aplicación ya está en ejecución\r\n");
                    isRunning = true;
                }
                else
                {
                    Console.WriteLine("La aplicación no está en ejecución, intentando iniciarla");
                    File.AppendAllText(logFile, $"[{DateTime.Now}] La aplicación no está en ejecución, intentando iniciarla\r\n");

                    try
                    {
                        // Obtener la ruta del ejecutable actual
                        string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;

                        // Iniciar la aplicación sin argumentos
                        ProcessStartInfo startInfo = new ProcessStartInfo
                        {
                            FileName = exePath,
                            UseShellExecute = true,
                            WindowStyle = ProcessWindowStyle.Normal
                        };

                        Process appProcess = Process.Start(startInfo);

                        if (appProcess != null)
                        {
                            // Iniciar contador para timeout
                            DateTime startTime = DateTime.Now;
                            bool timeoutReached = false;

                            // Esperar a que la aplicación se inicie (máximo timeoutSeconds segundos)
                            while (!isRunning && !timeoutReached)
                            {
                                // Comprobar si ha pasado el tiempo de timeout
                                if ((DateTime.Now - startTime).TotalSeconds > timeoutSeconds)
                                {
                                    timeoutReached = true;
                                    errorLog = $"Timeout alcanzado después de {timeoutSeconds} segundos esperando que la aplicación iniciara.";
                                    File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR: {errorLog}\r\n");
                                    Console.WriteLine(errorLog);
                                    break;
                                }

                                // Esperar un poco antes de comprobar de nuevo
                                Thread.Sleep(500);

                                // Volver a comprobar si la aplicación está en ejecución
                                processes = Process.GetProcessesByName(currentProcess.ProcessName);
                                if (processes.Length > 1)
                                {
                                    isRunning = true;
                                    File.AppendAllText(logFile, $"[{DateTime.Now}] Aplicación iniciada correctamente\r\n");
                                    Console.WriteLine("Aplicación iniciada correctamente");
                                }
                            }
                        }
                        else
                        {
                            errorLog = "No se pudo iniciar el proceso de la aplicación";
                            File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR: {errorLog}\r\n");
                            Console.WriteLine(errorLog);
                        }
                    }
                    catch (Exception ex)
                    {
                        errorLog = $"Error al iniciar la aplicación: {ex.Message}";
                        File.AppendAllText(logFile, $"[{DateTime.Now}] ERROR: {errorLog}\r\n" +
                                                  $"[{DateTime.Now}] Stack trace: {ex.StackTrace}\r\n");
                        Console.WriteLine(errorLog);
                    }
                }
            }
            catch (Exception ex)
            {
                errorLog = $"Error en EnsureApplicationIsRunning: {ex.Message}";
                Console.WriteLine(errorLog);
                // Intentar guardar el error en un lugar accesible incluso si falla la creación del log principal
                try
                {
                    File.WriteAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_error.log"),
                        $"[{DateTime.Now}] {errorLog}\r\n{ex.StackTrace}");
                }
                catch { /* Ignore */ }
            }

            return isRunning;
        }

        /// <summary>
        /// Lanza la aplicación principal
        /// </summary>
        public static bool LaunchApplication()
        {
            Console.WriteLine("LaunchApplication iniciado");
            try
            {
                // Obtener la ruta actual de la aplicación
                string appPath = System.Reflection.Assembly.GetEntryAssembly().Location;

                // Lanzar la aplicación
                Console.WriteLine($"Ejecutando proceso: {appPath}");
                Process.Start(appPath);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al lanzar la aplicación: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }

        /// <summary>
        /// Lanza la aplicación ECM Central con un archivo específico
        /// </summary>
        public static void LaunchApplicationWithFile(string filePath)
        {
            try
            {
                // Verificar si ya está abierto algún formulario relevante
                foreach (Form form in Application.OpenForms)
                {
                    if (form.GetType().Name == "FormularioForm" || form.GetType().Name == "LoginForm")
                    {
                        // La aplicación ya está abierta, marcar el archivo para procesamiento
                        string markerFile = Path.Combine(
                            VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                            "pending_file.marker");

                        File.WriteAllText(markerFile, filePath);

                        // Activar la ventana existente
                        try
                        {
                            form.Invoke(new Action(() => {
                                form.WindowState = FormWindowState.Normal;
                                form.Activate();

                                // Notificar al usuario que hay un nuevo archivo para procesar
                                MessageBox.Show(
                                    $"Se ha detectado un nuevo archivo para procesar:\n{Path.GetFileName(filePath)}",
                                    "Nuevo archivo PDF",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);

                                // Si es el formulario principal, llamar al método que procesa archivos si existe
                                if (form.GetType().Name == "FormularioForm")
                                {
                                    var method = form.GetType().GetMethod("ProcessPendingFile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                                    method?.Invoke(form, new object[] { filePath });
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            WatcherLogger.LogError("Error al activar ventana existente", ex);
                        }

                        WatcherLogger.LogActivity($"Aplicación ya abierta, marcando archivo para procesamiento: {filePath}");
                        return;
                    }
                }

                // Buscar procesos existentes que podrían ser la aplicación principal
                Process currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                foreach (var proc in processes)
                {
                    try
                    {
                        if (proc.Id != currentProcess.Id) // No contar este proceso
                        {
                            string cmdLine = GetCommandLine(proc.Id);
                            // Si hay un proceso que no sea un monitor de fondo, es probablemente la UI principal
                            if (!cmdLine.Contains("/backgroundmonitor") && !string.IsNullOrEmpty(cmdLine))
                            {
                                // Encontramos una instancia de la app principal, enviar el archivo a procesar
                                Console.WriteLine("Se encontró una instancia de la aplicación principal activa");

                                // Crear un archivo marcador para que la aplicación lo detecte
                                string pendingFile = Path.Combine(
                                    VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                                    "pending_file.marker");

                                File.WriteAllText(pendingFile, filePath);

                                // Intentar activar la ventana
                                try
                                {
                                    proc.EnableRaisingEvents = true;
                                    proc.Refresh();
                                    SetForegroundWindow(proc.MainWindowHandle);
                                }
                                catch (Exception ex)
                                {
                                    WatcherLogger.LogError("Error al activar ventana de proceso existente", ex);
                                }

                                return;
                            }
                        }
                    }
                    catch { /* Ignorar errores */ }
                }

                // La aplicación no está abierta, lanzarla con el argumento adecuado
                WatcherLogger.LogActivity($"Lanzando aplicación con archivo: {filePath}");

                // Evitar múltiples lanzamientos simultáneos
                string lockFile = Path.Combine(
                    VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                    ".app_launch.lock");

                if (File.Exists(lockFile))
                {
                    try
                    {
                        DateTime lockTime = File.GetLastWriteTime(lockFile);
                        if ((DateTime.Now - lockTime).TotalSeconds < 10)
                        {
                            // Lanzamiento muy reciente, marcar el archivo y salir
                            File.WriteAllText(
                                Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "pending_file.marker"),
                                filePath);

                            WatcherLogger.LogActivity($"Lanzamiento reciente detectado, archivo marcado: {filePath}");
                            return;
                        }
                    }
                    catch { /* Ignorar errores leyendo el archivo */ }
                }

                // Crear/actualizar archivo de bloqueo
                try { File.WriteAllText(lockFile, DateTime.Now.ToString()); } catch { }

                // Lanzar la aplicación
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = $"/print:\"{filePath}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                WatcherLogger.LogActivity($"Aplicación lanzada para procesar archivo: {filePath}");
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al lanzar aplicación con archivo", ex);
            }
        }

        // Método auxiliar para obtener la línea de comandos de un proceso
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

        // Importación de API de Win32 para traer una ventana al frente
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}