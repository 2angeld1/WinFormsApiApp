using System;
using System.IO;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para manejar el registro de logs
    /// </summary>
    public static class LogHelper
    {
        private static bool _initialized = false;

        static LogHelper()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente LogHelper...");
                    _initialized = true;
                    Console.WriteLine("Componente LogHelper inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente LogHelper: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Registra un mensaje en el archivo de log con mejor manejo de errores
        /// </summary>
        public static void LogMessage(string logFile, string message)
        {
            try
            {
                // Verificar si la carpeta existe, y crearla si no
                string logFolder = Path.GetDirectoryName(logFile);
                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                    Console.WriteLine($"Carpeta de logs creada durante registro: {logFolder}");
                }

                // Registrar el mensaje
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR AL REGISTRAR LOG: {ex.Message}");

                // Intento de último recurso - escribir en el escritorio
                try
                {
                    string desktopLog = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ecm_error_log.txt");

                    File.AppendAllText(desktopLog,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Error al escribir en log original: {ex.Message}\r\n" +
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Mensaje original: {message}\r\n");
                }
                catch
                {
                    // No podemos hacer nada más si también falla
                }
            }
        }
    }
}