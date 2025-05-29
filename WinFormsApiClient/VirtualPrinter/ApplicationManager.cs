using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows.Forms;

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
        /// Lanza la aplicación principal con un archivo específico
        /// </summary>
        public static bool LaunchApplicationWithFile(string filePath)
        {
            Console.WriteLine($"LaunchApplicationWithFile iniciado con archivo: {filePath}");
            try
            {
                // Obtener la ruta actual de la aplicación
                string appPath = System.Reflection.Assembly.GetEntryAssembly().Location;

                // Verificar que el archivo existe
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"ERROR: No se encontró el archivo: {filePath}");
                    return false;
                }

                // Asegurarse de que la ruta no tenga espacios sin comillas
                string safeFilePath = filePath;
                if (filePath.Contains(" ") && !filePath.StartsWith("\""))
                {
                    safeFilePath = $"\"{filePath}\"";
                }

                // Registrar el archivo en un archivo temporal para garantizar persistencia
                string tempFile = Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt");
                File.WriteAllText(tempFile, filePath);

                // Lanzar la aplicación con el archivo como argumento
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = $"/print:{safeFilePath}",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                Console.WriteLine($"Ejecutando proceso: {appPath} con argumentos: {startInfo.Arguments}");
                Process.Start(startInfo);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al lanzar la aplicación: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return false;
            }
        }
    }
}