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

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // Crear un archivo de log con timestamp
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                _logFilePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    $"ecm_diagnostics_{timestamp}.txt");

                // Crear el archivo
                File.WriteAllText(_logFilePath, $"=== INICIO DE DIAGNÓSTICO {timestamp} ===\r\n");

                // Configurar timer para guardar el log periódicamente
                _flushTimer = new Timer(FlushLog, null, 2000, 2000);

                // Inicializar interceptor de consola
                _consoleInterceptor = new ConsoleOutputInterceptor();
                _consoleInterceptor.OnConsoleOutput += HandleConsoleOutput;

                LogInfo("Sistema de diagnóstico inicializado correctamente");
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

                if (!string.IsNullOrEmpty(textToWrite))
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
                FlushLog(null);
                _flushTimer?.Dispose();
                _consoleInterceptor?.Dispose();
                LogInfo("Sistema de diagnóstico finalizado");
            }
            catch { /* Ignorar errores al cerrar */ }
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