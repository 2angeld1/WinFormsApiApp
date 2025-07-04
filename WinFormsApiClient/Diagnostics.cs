using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace WinFormsApiClient
{
    public static class Diagnostics
    {
        private static readonly object _logLock = new object();
        private static StringBuilder _logBuffer = new StringBuilder();
        private static Timer _flushTimer;
        private static string _logFilePath;
        private static bool _initialized = false;
        private static ConsoleOutputInterceptor _consoleInterceptor;

        // NUEVO: Ruta fija para el archivo de diagnóstico
        private static readonly string LOG_FOLDER = @"C:\Temp\ECM Central";
        private static readonly string LOG_FILE_NAME = "ecm_diagnostics.txt";

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // MODIFICADO: Usar ruta fija sin timestamp
                if (!Directory.Exists(LOG_FOLDER))
                {
                    Directory.CreateDirectory(LOG_FOLDER);
                }

                _logFilePath = Path.Combine(LOG_FOLDER, LOG_FILE_NAME);

                // MODIFICADO: Si el archivo existe, borrarlo para empezar limpio
                if (File.Exists(_logFilePath))
                {
                    try
                    {
                        File.Delete(_logFilePath);
                        Console.WriteLine("Archivo de diagnóstico anterior eliminado");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"No se pudo eliminar archivo anterior: {ex.Message}");
                        // Continuar usando el archivo existente
                    }
                }

                // Crear el archivo con encabezado de nueva sesión
                string sessionHeader = $"=== NUEVA SESIÓN DE DIAGNÓSTICO ===\r\n" +
                                     $"Fecha y hora: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\r\n" +
                                     $"Aplicación: ECM Central\r\n" +
                                     $"Versión: {System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version}\r\n" +
                                     $"Sistema: {Environment.OSVersion}\r\n" +
                                     $"Usuario: {Environment.UserName}\r\n" +
                                     $"Directorio de trabajo: {Environment.CurrentDirectory}\r\n" +
                                     $"========================================\r\n\r\n";

                File.WriteAllText(_logFilePath, sessionHeader);

                // Configurar timer para guardar el log periódicamente
                _flushTimer = new Timer(FlushLog, null, 2000, 2000);

                // Inicializar interceptor de consola
                _consoleInterceptor = new ConsoleOutputInterceptor();
                _consoleInterceptor.OnConsoleOutput += HandleConsoleOutput;

                LogInfo("Sistema de diagnóstico inicializado correctamente");
                LogInfo($"Archivo de log: {_logFilePath}");
                _initialized = true;
            }
            catch (Exception ex)
            {
                // Intentar crear un log de emergencia
                try
                {
                    string emergencyLog = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ecm_emergency_log.txt");
                    File.AppendAllText(emergencyLog,
                        $"[{DateTime.Now}] Error al inicializar diagnóstico: {ex.Message}\r\n");
                }
                catch { /* No podemos hacer nada si esto también falla */ }
            }
        }

        private static void HandleConsoleOutput(string message)
        {
            // Registrar salida de consola como información
            LogInfo($"[CONSOLE] {message}");
        }

        public static void LogInfo(string message)
        {
            Log("INFO", message);
        }

        public static void LogWarning(string message)
        {
            Log("ADVERTENCIA", message);
        }

        public static void LogError(string message, Exception ex = null)
        {
            if (ex != null)
            {
                Log("ERROR", $"{message} - {ex.GetType().Name}: {ex.Message}");
                Log("STACK", ex.StackTrace);
            }
            else
            {
                Log("ERROR", message);
            }
        }

        public static void LogOperationStart(string operation)
        {
            Log("INICIO", $"=== INICIO DE OPERACIÓN: {operation} ===");
        }

        public static void LogOperationEnd(string operation)
        {
            Log("FIN", $"=== FIN DE OPERACIÓN: {operation} ===");
        }

        public static void LogThreadInfo()
        {
            Log("HILO", $"ID: {Thread.CurrentThread.ManagedThreadId}, " +
                         $"IsBackground: {Thread.CurrentThread.IsBackground}, " +
                         $"Priority: {Thread.CurrentThread.Priority}, " +
                         $"IsThreadPoolThread: {Thread.CurrentThread.IsThreadPoolThread}");
        }

        public static void LogMemoryUsage()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Log("MEMORIA", $"Memoria física: {currentProcess.WorkingSet64 / (1024 * 1024)} MB, " +
                           $"Memoria privada: {currentProcess.PrivateMemorySize64 / (1024 * 1024)} MB, " +
                           $"Handles: {currentProcess.HandleCount}");
        }

        private static void Log(string type, string message)
        {
            try
            {
                lock (_logLock)
                {
                    string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{type}] {message}\r\n";
                    _logBuffer.Append(logEntry);

                    // Si el buffer es grande, forzar escritura
                    if (_logBuffer.Length > 4096)
                    {
                        FlushLog(null);
                    }
                }
            }
            catch { /* Ignorar errores de logging */ }
        }

        private static void FlushLog(object state)
        {
            try
            {
                string textToWrite;
                lock (_logLock)
                {
                    textToWrite = _logBuffer.ToString();
                    _logBuffer.Clear();
                }

                if (!string.IsNullOrEmpty(textToWrite) && !string.IsNullOrEmpty(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, textToWrite);
                }
            }
            catch { /* Ignorar errores de escritura */ }
        }

        public static void Shutdown()
        {
            try
            {
                LogInfo("Finalizando sesión de diagnóstico");
                LogInfo($"=== FIN DE SESIÓN: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
                
                FlushLog(null);
                _flushTimer?.Dispose();
                _consoleInterceptor?.Dispose();
                
                // Agregar separador para próxima sesión
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    File.AppendAllText(_logFilePath, "\r\n" + new string('=', 60) + "\r\n\r\n");
                }
            }
            catch { /* Ignorar errores al cerrar */ }
        }

        // NUEVO: Método para obtener la ruta del archivo de log
        public static string GetLogFilePath()
        {
            return _logFilePath ?? Path.Combine(LOG_FOLDER, LOG_FILE_NAME);
        }

        /// <summary>
        /// Clase para interceptar salidas de Console.WriteLine y redirigirlas al sistema de diagnóstico
        /// </summary>
        private class ConsoleOutputInterceptor : IDisposable
        {
            private readonly TextWriter _originalOutput;
            private readonly TextWriter _originalError;
            private bool _disposed = false;

            public event Action<string> OnConsoleOutput;

            public ConsoleOutputInterceptor()
            {
                // Guardar las salidas originales
                _originalOutput = Console.Out;
                _originalError = Console.Error;

                // Reemplazar con nuestros escritores personalizados
                Console.SetOut(new InterceptingTextWriter(_originalOutput, OnOutput));
                Console.SetError(new InterceptingTextWriter(_originalError, OnError));
            }

            private void OnOutput(string text)
            {
                OnConsoleOutput?.Invoke(text);
            }

            private void OnError(string text)
            {
                OnConsoleOutput?.Invoke($"[ERROR] {text}");
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    // Restaurar las salidas originales
                    Console.SetOut(_originalOutput);
                    Console.SetError(_originalError);
                    _disposed = true;
                }
            }

            private class InterceptingTextWriter : TextWriter
            {
                private readonly TextWriter _original;
                private readonly Action<string> _callback;

                public InterceptingTextWriter(TextWriter original, Action<string> callback)
                {
                    _original = original;
                    _callback = callback;
                }

                public override Encoding Encoding => _original.Encoding;

                public override void Write(char value)
                {
                    _original.Write(value);
                }

                public override void Write(string value)
                {
                    if (value != null)
                    {
                        _callback(value);
                    }
                    _original.Write(value);
                }

                public override void WriteLine(string value)
                {
                    if (value != null)
                    {
                        _callback(value);
                    }
                    _original.WriteLine(value);
                }
            }
        }
    }
}