using System;
using System.IO;
using System.Threading.Tasks;
using WinFormsApiClient.VirtualWatcher;

namespace WinFormsApiClient
{
    /// <summary>
    /// Clase que actúa como fachada para el sistema de monitoreo de impresión
    /// </summary>
    public class PrinterWatcher : IDisposable
    {
        // Singleton
        private static PrinterWatcher _instance;
        public static PrinterWatcher Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PrinterWatcher();
                return _instance;
            }
        }

        private PrinterWatcher()
        {
            try
            {
                Console.WriteLine("Iniciando componente PrinterWatcher...");

                // Inicializar todos los componentes del sistema de monitoreo
                var core = WatcherCore.Instance;
                var logger = typeof(WatcherLogger);
                var fileMonitor = FileMonitor.Instance;
                var docProcessor = DocumentProcessor.Instance;
                var formInteraction = FormInteraction.Instance;
                var docUploader = DocumentUploader.Instance;

                Console.WriteLine("Componente PrinterWatcher inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente PrinterWatcher: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Inicia el monitoreo de nuevos archivos PDF
        /// </summary>
        public void StartWatching()
        {
            try
            {
                Console.WriteLine("Iniciando monitoreo de impresión...");

                // Verificar si la impresora está instalada
                Task.Run(async () => await VirtualPrinter.PrinterInstaller.InstallPrinterAsync()).Wait();

                // Solo verificar que el monitor de fondo esté activo
                if (!VirtualWatcher.BackgroundMonitorService.IsActuallyRunning())
                {
                    Console.WriteLine("Monitor de fondo no está activo. Use /backgroundmonitor para iniciarlo.");
                }

                Console.WriteLine("PrinterWatcher iniciado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al iniciar PrinterWatcher: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Detiene el monitoreo de archivos
        /// </summary>
        public void StopWatching()
        {
            try
            {
                Console.WriteLine("Deteniendo monitoreo de impresión...");

                // Detener el monitoreo de archivos
                FileMonitor.Instance.StopMonitoring();

                Console.WriteLine("PrinterWatcher detenido");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detener PrinterWatcher: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Realiza un diagnóstico del sistema de impresión
        /// </summary>
        public void DiagnosticCheck()
        {
            try
            {
                WatcherLogger.LogActivity("======= DIAGNÓSTICO DEL SISTEMA DE IMPRESIÓN =======");

                // 1. Verificar la carpeta de monitoreo
                string outputFolder = WatcherCore.Instance.OutputFolder;
                WatcherLogger.LogActivity($"Carpeta de monitoreo: {outputFolder}");

                if (Directory.Exists(outputFolder))
                {
                    var files = Directory.GetFiles(outputFolder, "*.pdf");
                    WatcherLogger.LogActivity($"Archivos PDF en carpeta: {files.Length}");

                    foreach (var file in files)
                    {
                        WatcherLogger.LogActivity($"  - {Path.GetFileName(file)} ({new FileInfo(file).Length / 1024}KB)");
                    }
                }
                else
                {
                    WatcherLogger.LogActivity("La carpeta de monitoreo NO EXISTE");
                }

                // 2. Verificar archivos procesados
                WatcherLogger.LogActivity($"Archivos procesados: {WatcherCore.Instance.ProcessedFilesCount}");

                // 3. Verificar estado de la impresora
                WatcherLogger.LogActivity($"Impresora instalada: {ECMVirtualPrinter.IsPrinterInstalled()}");

                // 4. Verificar permisos de escritura
                try
                {
                    string testFile = Path.Combine(outputFolder, "test_write.tmp");
                    File.WriteAllText(testFile, "Test");
                    File.Delete(testFile);
                    WatcherLogger.LogActivity("Prueba de escritura en carpeta de monitoreo: OK");
                }
                catch (Exception ex)
                {
                    WatcherLogger.LogActivity($"Error de escritura en carpeta de monitoreo: {ex.Message}");
                }

                WatcherLogger.LogActivity("Ejecutando verificación manual de nuevos PDFs...");
                FileMonitor.Instance.CheckForNewPDFs();

                WatcherLogger.LogActivity("======= FIN DEL DIAGNÓSTICO =======");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en DiagnosticCheck: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        public void Dispose()
        {
            try
            {
                StopWatching();

                // Disponer otros recursos si es necesario
                (FileMonitor.Instance as IDisposable)?.Dispose();

                Console.WriteLine("PrinterWatcher dispuesto correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al disponer PrinterWatcher: {ex.Message}");
            }
        }
    }
}