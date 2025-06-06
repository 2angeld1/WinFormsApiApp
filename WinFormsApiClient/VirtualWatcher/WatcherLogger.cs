using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.ServiceProcess;
namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase para manejar el registro de logs del sistema de monitoreo
    /// </summary>
    public static class WatcherLogger
    {
        public static bool _initialized = false;
        private static readonly object _lockObject = new object();
        private static string _initLogFile;
        private static string _dailyLogFile;
        private static FileSystemWatcher _logWatcher;
        private static TimeSpan _loggingInterval = TimeSpan.FromSeconds(1); // Por defecto, sin restricción
        static WatcherLogger()
        {
            try
            {
                Console.WriteLine("Iniciando componente WatcherLogger...");

                // Configurar archivos de log
                string logFolder = ECMVirtualPrinter.GetLogFolderPath();
                _initLogFile = Path.Combine(logFolder, "init_log.txt");
                _dailyLogFile = Path.Combine(logFolder, $"print_watcher_{DateTime.Now:yyyyMMdd}.log");

                // Asegurar que la carpeta existe
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                    WriteToLog(_initLogFile, $"Carpeta de logs creada: {logFolder}");
                }

                // Escribir mensaje de inicialización
                WriteToLog(_initLogFile, "===== INICIO DEL SISTEMA DE MONITOREO DE IMPRESIÓN =====");
                WriteToLog(_initLogFile, $"Fecha y hora: {DateTime.Now}");
                WriteToLog(_initLogFile, $"Modo: Servicio en segundo plano (sin interfaz gráfica)");
                WriteToLog(_initLogFile, $"Estado de impresora: {(ECMVirtualPrinter.IsPrinterInstalled() ? "Instalada" : "No instalada")}");

                // Monitorear la carpeta de logs para detectar cambios
                ConfigureLogWatcher(logFolder);

                _initialized = true;
                Console.WriteLine("Componente WatcherLogger inicializado correctamente");
                WriteToLog(_initLogFile, "Componente WatcherLogger inicializado correctamente");
                WriteToLog(_initLogFile, "Esperando trabajos de impresión (Ctrl+P)...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente WatcherLogger: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                // Intentar escribir en el escritorio como último recurso
                try
                {
                    string desktopLog = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "watcher_error_log.txt");

                    File.AppendAllText(desktopLog,
                        $"[{DateTime.Now}] Error al inicializar WatcherLogger: {ex.Message}\r\n" +
                        $"Stack trace: {ex.StackTrace}\r\n");
                }
                catch { /* No hay más que podamos hacer */ }
            }
        }
        /// <summary>
        /// Establece el intervalo de tiempo entre registros para reducir la cantidad de logs
        /// cuando se está en modo de bajo impacto
        /// </summary>
        /// <param name="interval">Intervalo de tiempo mínimo entre registros consecutivos</param>
        public static void SetLoggingInterval(TimeSpan interval)
        {
            try
            {
                _loggingInterval = interval;

                // Registrar el cambio de configuración
                LogActivity($"Intervalo de logging establecido a: {interval.TotalMinutes} minutos");
            }
            catch (Exception ex)
            {
                // Registrar error pero no interrumpir la ejecución
                Console.WriteLine($"Error al cambiar intervalo de logging: {ex.Message}");
            }
        }
        /// <summary>
        /// Configura un monitor para detectar cambios en la carpeta de logs
        /// </summary>
        private static void ConfigureLogWatcher(string logFolder)
        {
            try
            {
                _logWatcher = new FileSystemWatcher(logFolder)
                {
                    Filter = "*.log",
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                _logWatcher.Created += (sender, e) =>
                {
                    WriteToLog(_initLogFile, $"Nuevo archivo de log creado: {Path.GetFileName(e.FullPath)}");
                };

                WriteToLog(_initLogFile, "Monitor de carpeta de logs configurado");
            }
            catch (Exception ex)
            {
                WriteToLog(_initLogFile, $"Error al configurar monitor de logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra un mensaje en el log de actividad del watcher y en init_log.txt
        /// </summary>
        private static DateTime _lastLogTime = DateTime.MinValue;
        public static void LogActivity(string message)
        {
            try
            {
                // Verificar si ha pasado suficiente tiempo desde el último log
                if (DateTime.Now - _lastLogTime < _loggingInterval)
                {
                    return; // No registrar si no ha pasado el intervalo mínimo
                }

                _lastLogTime = DateTime.Now;
                // Registrar en ambos archivos
                WriteToLog(_initLogFile, message);
                WriteToLog(_dailyLogFile, message);

                Console.WriteLine($"[PRINTER WATCHER] {message}");
            }
            catch (Exception ex)
            {
                // Si falla, intentar escribir en el escritorio
                try
                {
                    string desktopLog = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "printer_watcher_log.txt");

                    File.AppendAllText(desktopLog,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n" +
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Error al escribir log: {ex.Message}\r\n");

                    Console.WriteLine($"Error al escribir en log principal: {ex.Message}");
                    Console.WriteLine($"Log alternativo creado en: {desktopLog}");
                }
                catch (Exception desktopEx)
                {
                    // No podemos hacer nada más
                    Console.WriteLine($"ERROR CRÍTICO: No se pudo escribir en ningún log: {desktopEx.Message}");
                }
            }
        }

        /// <summary>
        /// Registra un error en el log de actividad del watcher
        /// </summary>
        public static void LogError(string message, Exception ex = null)
        {
            LogActivity($"ERROR: {message}");

            if (ex != null)
            {
                LogActivity($"Excepción: {ex.GetType().Name}");
                LogActivity($"Mensaje: {ex.Message}");
                LogActivity($"Stack trace: {ex.StackTrace}");

                // Registrar excepción interna si existe
                if (ex.InnerException != null)
                {
                    LogActivity($"Excepción interna: {ex.InnerException.GetType().Name}");
                    LogActivity($"Mensaje interno: {ex.InnerException.Message}");
                }
            }
        }

        /// <summary>
        /// Registra específicamente eventos de impresión
        /// </summary>
        public static void LogPrintEvent(string message, bool isSuccess = true)
        {
            string formattedMessage = isSuccess
                ? $"[PRINT EVENT] {message}"
                : $"[PRINT ERROR] {message}";

            LogActivity(formattedMessage);

            // Obtener información de la llamada actual para depuración
            try
            {
                StackTrace trace = new StackTrace(1, true);
                StackFrame frame = trace.GetFrame(0);
                if (frame != null)
                {
                    string methodName = frame.GetMethod()?.Name ?? "Unknown";
                    int lineNumber = frame.GetFileLineNumber();
                    LogActivity($"Llamado desde: {methodName} (línea {lineNumber})");
                }
            }
            catch { /* Ignorar errores en la captura del stack trace */ }
        }

        /// <summary>
        /// Registra información del trabajo de impresión detectado
        /// </summary>
        public static void LogPrintJob(string printerName, string documentName, string status)
        {
            string message = $"Trabajo de impresión: [{printerName}] - \"{documentName}\" - Estado: {status}";
            LogActivity(message);

            // Registrar en init_log.txt con formato especial
            WriteToLog(_initLogFile, new string('-', 20));
            WriteToLog(_initLogFile, $"NUEVO TRABAJO DETECTADO: {documentName}");
            WriteToLog(_initLogFile, $"Impresora: {printerName}");
            WriteToLog(_initLogFile, $"Estado: {status}");
            WriteToLog(_initLogFile, $"Hora: {DateTime.Now:HH:mm:ss.fff}");
            WriteToLog(_initLogFile, new string('-', 20));
        }

        /// <summary>
        /// Escribe un mensaje en un archivo de log específico con bloqueo para evitar problemas de concurrencia
        /// </summary>
        private static void WriteToLog(string logFilePath, string message)
        {
            if (string.IsNullOrEmpty(logFilePath))
                return;

            // Usar lock para evitar problemas de concurrencia al escribir en el archivo
            lock (_lockObject)
            {
                try
                {
                    // Asegurar que el directorio existe
                    string directory = Path.GetDirectoryName(logFilePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    // Intentar escribir con varios reintentos si hay error de acceso
                    int maxRetries = 3;
                    int currentRetry = 0;
                    bool success = false;

                    while (!success && currentRetry < maxRetries)
                    {
                        try
                        {
                            using (FileStream fs = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                            using (StreamWriter writer = new StreamWriter(fs))
                            {
                                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
                            }
                            success = true;
                        }
                        catch (IOException)
                        {
                            // Esperar un poco y reintentar
                            currentRetry++;
                            Thread.Sleep(100 * currentRetry);
                        }
                        catch (Exception)
                        {
                            // Otro tipo de error, no reintentar
                            break;
                        }
                    }

                    // Si aún falla después de los reintentos, usar un nombre de archivo alternativo
                    if (!success)
                    {
                        string alternativeLogPath = logFilePath + $".{Guid.NewGuid().ToString().Substring(0, 8)}.log";
                        try
                        {
                            File.AppendAllText(alternativeLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n");
                        }
                        catch { /* Ignorar si también falla */ }
                    }
                }
                catch
                {
                    // Silenciar errores aquí para evitar recursión infinita
                }
            }
        }

        /// <summary>
        /// Registra un diagnóstico completo del sistema de impresión con manejo de errores mejorado
        /// </summary>
        public static void LogSystemDiagnostic()
        {
            try
            {
                WriteToLog(_initLogFile, new string('=', 20) + " DIAGNÓSTICO DEL SISTEMA " + new string('=', 20));

                // 1. Estado de la impresora
                bool printerInstalled = ECMVirtualPrinter.IsPrinterInstalled();
                WriteToLog(_initLogFile, $"Impresora ECM instalada: {printerInstalled}");

                // 2. Estado del servicio de impresión - Sin usar PowerShell
                // 2. Estado del servicio de impresión - Sin usar PowerShell
                try
                {
#if !NET20 && !NET30 && !NET35
                    // Usar ServiceController si está disponible
                    try
                    {
                        using (var service = new ServiceController("Spooler"))
                        {
                            string status = "Desconocido";
                            try { status = service.Status.ToString(); } catch { }

                            WriteToLog(_initLogFile, "Estado del servicio Spooler:");
                            WriteToLog(_initLogFile, $"Nombre: Spooler, Estado: {status}");
                        }
                    }
                    catch
                    {
                        // Alternativa usando WMI
                        WriteToLog(_initLogFile, "Estado del servicio Spooler (WMI):");
                        WriteToLog(_initLogFile, "Estado no disponible (se requiere ServiceProcess)");
                    }
#else
    // Para versiones de .NET que no tengan ServiceController
    WriteToLog(_initLogFile, "Estado del servicio Spooler:");
    WriteToLog(_initLogFile, "Estado no disponible (versión de .NET incompatible)");
#endif
                }
                catch (Exception ex)
                {
                    WriteToLog(_initLogFile, $"No se pudo obtener información del servicio Spooler: {ex.Message}");
                }

                // 3. Trabajos de impresión - Simplificado para evitar PowerShell
                try
                {
                    WriteToLog(_initLogFile, "Trabajos de impresión actuales:");
                    WriteToLog(_initLogFile, "Información no disponible (requiere elevación de privilegios)");
                }
                catch (Exception ex)
                {
                    WriteToLog(_initLogFile, $"No se pudo obtener información de trabajos: {ex.Message}");
                }

                // 4. Información del sistema
                WriteToLog(_initLogFile, $"Sistema operativo: {Environment.OSVersion}");
                WriteToLog(_initLogFile, $"64 bits: {Environment.Is64BitOperatingSystem}");
                WriteToLog(_initLogFile, $"Memoria disponible: {GetAvailableMemory()} MB");

                WriteToLog(_initLogFile, new string('=', 60));
            }
            catch (Exception ex)
            {
                WriteToLog(_initLogFile, $"Error al generar diagnóstico: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la memoria disponible en MB utilizando métodos alternativos compatibles con la mayoría de sistemas
        /// </summary>
        private static long GetAvailableMemory()
        {
            try
            {
                // Enfoque completamente basado en .NET sin PowerShell
                // para evitar problemas de Win32Exception
                long memoryMB = -1;

                try
                {
                    // Usar .NET directamente para obtener información de memoria
                    using (var pc = new PerformanceCounter("Memory", "Available MBytes", true))
                    {
                        float memoryFloat = pc.NextValue();
                        memoryMB = (long)memoryFloat; // Conversión explícita de float a long
                        return memoryMB;
                    }
                }
                catch
                {
                    // Si falla PerformanceCounter, intentar con WMI pero evitando PowerShell
                    try
                    {
                        var wmiQuery = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                        foreach (var item in wmiQuery.Get())
                        {
                            var freePhysicalMemory = Convert.ToInt64(item["FreePhysicalMemory"]);
                            memoryMB = freePhysicalMemory / 1024; // KB a MB
                            break;
                        }

                        if (memoryMB > 0)
                            return memoryMB;
                    }
                    catch
                    {
                        // Si también falla, usar estimación
                    }
                }

                // Como último recurso usar una estimación
                return Environment.Is64BitOperatingSystem ? 2048 : 1024;
            }
            catch (Exception)
            {
                return -1;
            }
        }
    }
}