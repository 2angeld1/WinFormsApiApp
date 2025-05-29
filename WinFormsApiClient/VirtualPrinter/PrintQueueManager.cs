using System;
using System.IO;
using WinFormsApiClient.VirtualPrinter;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para gestionar la cola de impresión
    /// </summary>
    public class PrintQueueManager
    {
        private static bool _initialized = false;

        static PrintQueueManager()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente PrintQueueManager...");
                    _initialized = true;
                    Console.WriteLine("Componente PrintQueueManager inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente PrintQueueManager: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Limpia la cola de impresión de la impresora virtual
        /// </summary>
        public static bool ClearPrintQueue()
        {
            Console.WriteLine("Limpiando cola de impresión...");
            string logFile = Path.Combine(VirtualPrinterCore.GetLogFolderPath(), "print_queue_errors.log");

            // Usar directamente PowerShell sin intentar WMI primero
            try
            {
                LogHelper.LogMessage(logFile, "Limpiando cola de impresión con PowerShell...");
                string psCommand = $@"
            try {{
                $jobs = Get-PrintJob -PrinterName '{VirtualPrinterCore.PRINTER_NAME}' -ErrorAction SilentlyContinue
                if ($jobs) {{
                    $jobCount = ($jobs | Measure-Object).Count
                    Write-Output ""Encontrados $jobCount trabajos de impresión""
                    $jobs | ForEach-Object {{ 
                        Write-Output ""Eliminando trabajo: $($_.DocumentName)""
                        $_ | Remove-PrintJob -ErrorAction SilentlyContinue 
                    }}
                    Write-Output ""Cola de impresión limpiada exitosamente""
                }} else {{
                    Write-Output ""No hay trabajos de impresión en cola""
                }}
                return $true
            }} catch {{
                Write-Output ""ERROR: $($_.Exception.Message)""
                return $false
            }}
        ";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                LogHelper.LogMessage(logFile, $"Resultado de limpieza con PowerShell: {result}");

                return result.Contains("exitosamente") || result.Contains("No hay trabajos");
            }
            catch (Exception ex)
            {
                LogHelper.LogMessage(logFile, $"Error al limpiar cola de impresión con PowerShell: {ex.Message}");
                LogHelper.LogMessage(logFile, $"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}