using System;
using System.Diagnostics;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para ejecutar comandos de PowerShell
    /// </summary>
    public class PowerShellHelper
    {
        private static bool _initialized = false;

        static PowerShellHelper()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente PowerShellHelper...");
                    // Verificar que PowerShell esté disponible
                    string version = RunPowerShellCommandWithOutput("$PSVersionTable.PSVersion.ToString()");
                    Console.WriteLine($"PowerShell encontrado, versión: {version}");

                    _initialized = true;
                    Console.WriteLine("Componente PowerShellHelper inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente PowerShellHelper: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Ejecuta un comando PowerShell y devuelve la salida como texto
        /// </summary>
        public static string RunPowerShellCommandWithOutput(string command)
        {
            using (Process process = new Process())
            {
                try
                {
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-Command \"{command}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;

                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    return output.Trim();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al ejecutar PowerShell: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Ejecuta un comando de PowerShell con privilegios elevados
        /// </summary>
        public static void RunPowerShellCommand(string command)
        {
            using (Process process = new Process())
            {
                try
                {
                    process.StartInfo.FileName = "powershell.exe";
                    process.StartInfo.Arguments = $"-Command \"{command}\"";
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.Verb = "runas"; // Solicita privilegios de administrador

                    // Crear objeto para capturar la salida y errores
                    var outputBuilder = new System.Text.StringBuilder();
                    var errorBuilder = new System.Text.StringBuilder();

                    // Configurar manejadores de eventos para capturar la salida
                    process.OutputDataReceived += (sender, args) => {
                        if (!string.IsNullOrEmpty(args.Data))
                            outputBuilder.AppendLine(args.Data);
                    };

                    process.ErrorDataReceived += (sender, args) => {
                        if (!string.IsNullOrEmpty(args.Data))
                            errorBuilder.AppendLine(args.Data);
                    };

                    // Iniciar el proceso y habilitar la captura de salida
                    process.Start();
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();

                    // Establecer un tiempo máximo de espera (3 minutos)
                    bool processExited = process.WaitForExit(180000);

                    if (!processExited)
                    {
                        try { process.Kill(); } catch { }
                        throw new TimeoutException("El comando de PowerShell tardó demasiado en ejecutarse.");
                    }

                    if (process.ExitCode != 0)
                    {
                        string error = errorBuilder.ToString();
                        if (string.IsNullOrEmpty(error))
                            error = "Error desconocido al ejecutar el comando PowerShell.";

                        // Verificar si el error está relacionado con permisos
                        if (error.Contains("privilegios") || error.Contains("permisos") ||
                            error.Contains("acceso denegado") || error.Contains("denied"))
                        {
                            throw new UnauthorizedAccessException("No tiene suficientes permisos. Ejecute como administrador.");
                        }

                        // En el caso de la configuración de impresora, algunos errores son esperados
                        if (command.Contains("Set-PrintConfiguration") || command.Contains("Set-Printer"))
                        {
                            Console.WriteLine($"Advertencia en configuración de impresora: {error}");
                        }
                        else
                        {
                            throw new Exception($"Error en PowerShell: {error}");
                        }
                    }

                    // Verificar la salida para depuración
                    string output = outputBuilder.ToString();
                    if (!string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine($"Salida de PowerShell: {output}");
                    }
                }
                catch (Exception ex)
                {
                    // Si ya es una excepción personalizada, relanzarla
                    if (ex is TimeoutException || ex is UnauthorizedAccessException)
                        throw;

                    // Para errores de proceso o al iniciar PowerShell
                    throw new Exception($"Error al ejecutar PowerShell: {ex.Message}", ex);
                }
            }
        }
    }
}