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
                // Verificar si hay una instancia de la aplicación principal ya en ejecución
                // excluyendo la instancia actual y los procesos del monitor
                bool mainAppRunning = false;
                Form activeForm = null;

                // Primero comprobar si estamos en la aplicación principal comprobando los formularios abiertos
                foreach (Form form in Application.OpenForms)
                {
                    if (form.GetType().Name == "FormularioForm" || form.GetType().Name == "LoginForm")
                    {
                        mainAppRunning = true;
                        activeForm = form;
                        break;
                    }
                }

                // Si estamos en la aplicación principal o tenemos un archivo para imprimir
                if (mainAppRunning || !string.IsNullOrEmpty(filePath))
                {
                    //// Iniciar el monitor en segundo plano obligatoriamente antes de imprimir
                    //bool monitorStarted = await StartBackgroundMonitorAsync();

                    //if (!monitorStarted)
                    //{
                    //    MessageBox.Show(
                    //        "No se pudo iniciar el monitor en segundo plano necesario para la impresión.\n" +
                    //        "Por favor, inténtelo nuevamente o ejecute la aplicación con el parámetro /backgroundmonitor.",
                    //        "Error de inicialización",
                    //        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    //    return;
                    //}

                    // Esperar un momento para que el monitor se inicialice completamente
                    await Task.Delay(1000);

                    // Si estamos en la aplicación principal, continuar con el proceso normal
                    if (mainAppRunning)
                    {
                        await PrintProcessor.PrintDocumentAsync(filePath);
                        return;
                    }

                    // Si no estamos en la aplicación principal, verificar si hay otra instancia activa
                    Process currentProcess = Process.GetCurrentProcess();
                    var processes = Process.GetProcessesByName(currentProcess.ProcessName);

                    foreach (var proc in processes)
                    {
                        try
                        {
                            if (proc.Id != currentProcess.Id) // No contar este proceso
                            {
                                string cmdLine = Program.GetProcessCommandLine(proc.Id);
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

                                    // Asegurarse que la aplicación está activa/visible
                                    VirtualWatcher.ApplicationLauncher.LaunchApplicationWithFile(filePath);
                                    return;
                                }
                            }
                        }
                        catch { /* Ignorar errores */ }
                    }

                    // Si no hay una instancia activa, iniciar una nueva con el archivo
                    await PrintProcessor.PrintDocumentAsync(filePath);
                }
                else
                {
                    // Continuar con el proceso normal de impresión
                    await PrintProcessor.PrintDocumentAsync(filePath);
                }
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
        public static async Task<bool> StartBackgroundMonitorAsync()
        {
            try
            {
                Console.WriteLine("Verificando si el monitor en segundo plano está activo...");

                // Verificación reforzada usando el método IsActuallyRunning
                if (BackgroundMonitorService.IsActuallyRunning())
                {
                    Console.WriteLine("El monitor ya está activo (verificación reforzada)");
                    return true;
                }

                // El monitor no está activo, iniciar usando el método StartBackgroundMonitorSilently
                // que ya tiene toda la lógica de verificación e inicio
                return VirtualWatcher.ApplicationLauncher.StartBackgroundMonitorSilently();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar monitor en segundo plano: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Escanea un documento y lo sube al portal
        /// </summary>
        public static Task<bool> ScanAndUploadAsync() =>
            WinFormsApiClient.Scanner.ScannerService.ScanAndUploadAsync();

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