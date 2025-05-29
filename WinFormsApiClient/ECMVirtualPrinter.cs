using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;
using WinFormsApiClient.VirtualWatcher;

namespace WinFormsApiClient
{
    /// <summary>
    /// Clase principal de la impresora virtual ECM Central
    /// Actúa como fachada para acceder a las funcionalidades de la impresora virtual
    /// </summary>
    public class ECMVirtualPrinter
    {
        private static bool _initialized = false;

        static ECMVirtualPrinter()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente ECMVirtualPrinter...");

                    // Inicializar componentes clave si no se han inicializado ya
                    var core = VirtualPrinterCore.GetLogFolderPath(); // Forzar inicialización

                    _initialized = true;
                    Console.WriteLine("Componente ECMVirtualPrinter inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente ECMVirtualPrinter: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Obtiene el nombre de la impresora virtual
        /// </summary>
        public static string PRINTER_NAME => VirtualPrinterCore.PRINTER_NAME;

        /// <summary>
        /// Obtiene la carpeta de salida para los archivos impresos
        /// </summary>
        public static string OUTPUT_FOLDER => VirtualPrinterCore.OUTPUT_FOLDER;

        /// <summary>
        /// Obtiene la carpeta de logs
        /// </summary>
        public static string LOG_FOLDER => VirtualPrinterCore.LOG_FOLDER;

        /// <summary>
        /// Verifica si el usuario tiene permisos de administrador
        /// </summary>
        public static bool IsAdministrator() => VirtualPrinterCore.IsAdministrator();

        /// <summary>
        /// Verifica si la impresora ECM Central está instalada
        /// </summary>
        public static bool IsPrinterInstalled() => VirtualPrinterCore.IsPrinterInstalled();

        /// <summary>
        /// Obtiene la ruta completa de la carpeta de logs
        /// </summary>
        public static string GetLogFolderPath() => VirtualPrinterCore.GetLogFolderPath();

        /// <summary>
        /// Limpia la cola de impresión de la impresora virtual
        /// </summary>
        public static bool ClearPrintQueue() => PrintQueueManager.ClearPrintQueue();

        /// <summary>
        /// Instala automáticamente la impresora si el usuario tiene permisos
        /// </summary>
        public static Task<bool> InstallPrinterAsync(bool silent = false) =>
            PrinterInstaller.InstallPrinterAsync(silent);

        /// <summary>
        /// Configura y repara la impresora virtual si hay problemas
        /// </summary>
        public static Task<bool> RepairPrinterAsync() =>
            PrinterInstaller.RepairPrinterAsync();

        /// <summary>
        /// Verifica si el usuario tiene licencia para usar la funcionalidad
        /// </summary>
        public static Task<bool> CheckLicenseAsync() =>
            PrintProcessor.CheckLicenseAsync();

        /// <summary>
        /// Imprime un documento a través de la impresora virtual ECM Central
        /// asegurando que el monitor en segundo plano esté activo primero
        /// </summary>
        public static async Task PrintDocumentAsync(string filePath = null)
        {
            try
            {
                // Iniciar el monitor en segundo plano obligatoriamente antes de imprimir
                bool monitorStarted = await StartBackgroundMonitorAsync();

                if (!monitorStarted)
                {
                    MessageBox.Show(
                        "No se pudo iniciar el monitor en segundo plano necesario para la impresión.\n" +
                        "Por favor, inténtelo nuevamente o ejecute la aplicación con el parámetro /backgroundmonitor.",
                        "Error de inicialización",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Esperar un momento para que el monitor se inicialice completamente
                await Task.Delay(1000);

                // Continuar con el proceso normal de impresión
                await PrintProcessor.PrintDocumentAsync(filePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al preparar el sistema para imprimir: {ex.Message}",
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Inicia el monitor en segundo plano y espera a que esté activo
        /// </summary>
        private static async Task<bool> StartBackgroundMonitorAsync()
        {
            try
            {
                Console.WriteLine("Verificando si el monitor en segundo plano está activo...");

                // Verificar si ya está en ejecución
                if (VirtualWatcher.BackgroundMonitorService._isRunning)
                {
                    Console.WriteLine("El monitor ya está en ejecución según BackgroundMonitorService");
                    return true;
                }

                // Verificar archivo de marcador
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");

                if (File.Exists(markerFilePath))
                {
                    try
                    {
                        string pidContent = File.ReadAllText(markerFilePath);
                        if (int.TryParse(pidContent, out int pid))
                        {
                            try
                            {
                                Process process = Process.GetProcessById(pid);
                                if (!process.HasExited)
                                {
                                    Console.WriteLine($"Monitor encontrado con PID: {pid}");
                                    return true;
                                }
                            }
                            catch
                            {
                                // El proceso ya no existe
                                Console.WriteLine("Archivo de marcador existe pero el proceso no está activo");
                                try { File.Delete(markerFilePath); } catch { }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al leer archivo de marcador: {ex.Message}");
                    }
                }

                // El monitor no está activo, iniciarlo
                Console.WriteLine("Iniciando monitor en segundo plano...");

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "/backgroundmonitor",
                    UseShellExecute = true,
                    CreateNoWindow = false
                };

                Process proc = Process.Start(startInfo);

                // Esperar a que el monitor esté activo (verificar archivo de marcador)
                int maxAttempts = 10;
                for (int i = 0; i < maxAttempts; i++)
                {
                    await Task.Delay(500);

                    if (File.Exists(markerFilePath))
                    {
                        Console.WriteLine("Monitor iniciado correctamente");
                        return true;
                    }

                    Console.WriteLine($"Esperando a que el monitor se inicie... {i + 1}/{maxAttempts}");
                }

                // Si llegamos aquí, el monitor no se inició correctamente
                Console.WriteLine("No se pudo confirmar que el monitor se haya iniciado correctamente");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar monitor en segundo plano: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sube un archivo al portal
        /// </summary>
        public static Task UploadToPortalAsync(string filePath) =>
            WinFormsApiClient.VirtualPrinter.DocumentUploader.UploadToPortalAsync(filePath);


        /// <summary>
        /// Activa o desactiva el bypass de verificación de licencia
        /// </summary>
        public static void SetLicenseBypass(bool bypass) =>
            PrintProcessor.SetLicenseBypass(bypass);

        /// <summary>
        /// Obtiene o establece la licencia del usuario actual
        /// </summary>
        public static PrintProcessor.LicenseFeatures UserLicense =>
            PrintProcessor.UserLicense;

        /// <summary>
        /// Funcionalidades disponibles según licencia
        /// </summary>
        [Flags]
        public enum LicenseFeatures
        {
            None = 0,
            Print = 1,
            Upload = 2,
            Scan = 4,
            All = Print | Upload | Scan
        }
    }
}