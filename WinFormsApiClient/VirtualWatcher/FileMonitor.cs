using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms; // Añadir esta referencia

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase responsable del monitoreo de archivos en la carpeta de salida
    /// </summary>
    public class FileMonitor : IDisposable
    {
        // Timer para verificar periódicamente nuevos archivos
        private System.Timers.Timer _fileWatchTimer;

        // FileSystemWatcher para detectar cambios en tiempo real (carpeta principal)
        private FileSystemWatcher _fileSystemWatcher;

        // FileSystemWatcher para detectar cambios en tiempo real (carpeta documentos)
        private FileSystemWatcher _documentsWatcher;

        // Flag para indicar si el monitor está activo
        private bool _isMonitoring = false;
        private static readonly object _checkLock = new object();
        private static int _isChecking = 0;
        private static FileMonitor _instance;
        public static FileMonitor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FileMonitor();
                return _instance;
            }
        }

        private FileMonitor()
        {
            try
            {
                Console.WriteLine("Iniciando componente FileMonitor...");

                string outputFolder = WatcherCore.Instance.OutputFolder;
                string documentsFolder = WatcherCore.Instance.DocumentsFolder;

                // Inicializar timer para verificar nuevos archivos cada 2 segundos
                _fileWatchTimer = new System.Timers.Timer(2000);
                _fileWatchTimer.Elapsed += OnTimerElapsed;
                _fileWatchTimer.AutoReset = true;

                // Configurar FileSystemWatcher para carpeta principal
                _fileSystemWatcher = new FileSystemWatcher(outputFolder)
                {
                    Filter = "*.pdf",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                    EnableRaisingEvents = false // Se activará al llamar a StartMonitoring
                };

                _fileSystemWatcher.Created += FileSystemWatcher_Created;
                _fileSystemWatcher.Changed += FileSystemWatcher_Changed;

                // Configurar FileSystemWatcher para carpeta de documentos
                if (Directory.Exists(documentsFolder))
                {
                    _documentsWatcher = new FileSystemWatcher(documentsFolder)
                    {
                        Filter = "*.pdf",
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime,
                        EnableRaisingEvents = false // Se activará al llamar a StartMonitoring
                    };

                    _documentsWatcher.Created += FileSystemWatcher_Created;
                    _documentsWatcher.Changed += FileSystemWatcher_Changed;

                    Console.WriteLine($"Watcher para carpeta de documentos configurado: {documentsFolder}");
                }
                else
                {
                    Console.WriteLine($"La carpeta de documentos no existe: {documentsFolder}");
                }

                Console.WriteLine("Componente FileMonitor inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente FileMonitor: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Inicia el monitoreo de archivos PDF
        /// </summary>
        public void StartMonitoring()
        {
            if (!_isMonitoring)
            {
                WatcherLogger.LogActivity("Iniciando monitoreo de archivos");

                try
                {
                    // Iniciar el timer de archivos
                    _fileWatchTimer.Start();

                    // Activar FileSystemWatcher para carpeta principal
                    _fileSystemWatcher.EnableRaisingEvents = true;

                    // Activar FileSystemWatcher para carpeta de documentos
                    if (_documentsWatcher != null)
                    {
                        _documentsWatcher.EnableRaisingEvents = true;
                    }

                    _isMonitoring = true;
                    WatcherLogger.LogActivity("FileMonitor: Monitoreo de archivos iniciado");
                    Console.WriteLine("FileMonitor iniciado");
                }
                catch (Exception ex)
                {
                    WatcherLogger.LogError("Error al iniciar monitoreo de archivos", ex);
                }
            }
        }

        /// <summary>
        /// Detiene el monitoreo de archivos
        /// </summary>
        public void StopMonitoring()
        {
            if (_isMonitoring)
            {
                WatcherLogger.LogActivity("Deteniendo monitoreo de archivos");

                try
                {
                    _fileWatchTimer.Stop();
                    _fileSystemWatcher.EnableRaisingEvents = false;

                    if (_documentsWatcher != null)
                    {
                        _documentsWatcher.EnableRaisingEvents = false;
                    }

                    _isMonitoring = false;
                    WatcherLogger.LogActivity("FileMonitor: Monitoreo de archivos detenido");
                    Console.WriteLine("FileMonitor detenido");
                }
                catch (Exception ex)
                {
                    WatcherLogger.LogError("Error al detener monitoreo de archivos", ex);
                }
            }
        }

        public void CheckForNewPDFs()
        {
            // Si ya hay una verificación en curso, salir
            if (System.Threading.Interlocked.Exchange(ref _isChecking, 1) == 1)
                return;

            try
            {
                // Revisar la carpeta principal
                CheckFolderForPDFs(WatcherCore.Instance.OutputFolder);

                // Revisar la carpeta de documentos
                CheckFolderForPDFs(WatcherCore.Instance.DocumentsFolder);
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en CheckForNewPDFs", ex);
            }
            finally
            {
                // Marcar que hemos terminado
                _isChecking = 0;
            }
        }

        /// <summary>
        /// Revisa una carpeta específica en busca de PDFs nuevos
        /// </summary>
        private void CheckFolderForPDFs(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                WatcherLogger.LogActivity($"Revisando carpeta: {folderPath}");

                var existingFiles = WatcherCore.Instance.GetProcessedFilesList();
                WatcherLogger.LogActivity($"Archivos ya procesados: {existingFiles.Count}");

                var currentFiles = Directory.GetFiles(folderPath, "*.pdf");
                WatcherLogger.LogActivity($"Archivos PDF encontrados: {currentFiles.Length}");

                foreach (var file in currentFiles)
                {
                    WatcherLogger.LogActivity($"Analizando archivo: {Path.GetFileName(file)}");

                    if (!WatcherCore.Instance.IsFileProcessed(file))
                    {
                        WatcherLogger.LogActivity($"Archivo nuevo encontrado: {Path.GetFileName(file)}");

                        // Verificar si el archivo es accesible (no está bloqueado)
                        if (WatcherCore.Instance.IsFileReady(file))
                        {
                            WatcherLogger.LogActivity($"Archivo listo para procesar: {Path.GetFileName(file)}");

                            // Marcar como procesado
                            WatcherCore.Instance.MarkFileAsProcessed(file);

                            // Procesar el archivo asíncronamente
                            WatcherLogger.LogActivity($"Iniciando procesamiento de: {Path.GetFileName(file)}");
                            Task.Run(async () => await DocumentProcessor.Instance.ProcessNewPrintJob(file));
                        }
                        else
                        {
                            WatcherLogger.LogActivity($"Archivo no accesible todavía: {Path.GetFileName(file)}");
                        }
                    }
                    else
                    {
                        WatcherLogger.LogActivity($"Archivo ya procesado: {Path.GetFileName(file)}");
                    }
                }
            }
            else
            {
                WatcherLogger.LogActivity($"¡ADVERTENCIA! La carpeta de monitoreo no existe: {folderPath}");

                // Intentar crear la carpeta
                try
                {
                    Directory.CreateDirectory(folderPath);
                    WatcherLogger.LogActivity($"Carpeta de monitoreo creada: {folderPath}");
                }
                catch (Exception folderEx)
                {
                    WatcherLogger.LogActivity($"Error al crear carpeta de monitoreo: {folderEx.Message}");
                }
            }
        }

        /// <summary>
        /// Evento que se activa cuando FileSystemWatcher detecta un nuevo archivo
        /// </summary>
        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"FileSystemWatcher: Archivo creado: {e.FullPath}");
            // Crear una ruta de log apropiada
            string logPath = Path.Combine(
                VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                "watcher_events.log");
            ProcessNewPdfFile(e.FullPath, logPath);
        }

        /// <summary>
        /// Evento que se activa cuando FileSystemWatcher detecta un cambio en un archivo
        /// </summary>
        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"FileSystemWatcher: Archivo modificado: {e.FullPath}");
            // Crear una ruta de log apropiada
            string logPath = Path.Combine(
                VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                "watcher_events.log");
            ProcessNewPdfFile(e.FullPath, logPath);
        }
        // En FileMonitor.cs, modificar el método ProcessNewPdfFile:
        private static void ProcessNewPdfFile(string filePath, string logPath)
        {
            try
            {
                // Esperar a que el archivo esté completamente escrito
                System.Threading.Thread.Sleep(1000);

                if (File.Exists(filePath))
                {
                    // Registrar detección
                    File.AppendAllText(logPath, $"[{DateTime.Now}] Archivo detectado: {filePath}\r\n");

                    // Verificar si el archivo ya ha sido procesado
                    if (!WatcherCore.Instance.IsFileProcessed(filePath))
                    {
                        // Marcar el archivo como procesado para evitar procesamiento duplicado
                        WatcherCore.Instance.MarkFileAsProcessed(filePath);

                        // Procesar el documento directamente en lugar de lanzar un nuevo proceso
                        WatcherLogger.LogActivity("Procesando archivo directamente: " + filePath);

                        try
                        {
                            DocumentProcessor.Instance.ProcessNewPrintJob(filePath).Wait();
                            File.AppendAllText(logPath, $"[{DateTime.Now}] Archivo procesado directamente: {filePath}\r\n");
                        }
                        catch (Exception ex)
                        {
                            File.AppendAllText(logPath, $"[{DateTime.Now}] Error al procesar archivo: {ex.Message}\r\n");
                        }
                    }
                    else
                    {
                        File.AppendAllText(logPath, $"[{DateTime.Now}] Archivo ya procesado: {filePath}\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al procesar archivo PDF: {ex.Message}");
                File.AppendAllText(logPath, $"[{DateTime.Now}] ERROR al procesar: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Evento que se activa cuando el timer detecta nuevos archivos
        /// </summary>
        private async void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                // Buscar nuevos archivos en ambas carpetas
                CheckTimerForNewFiles(WatcherCore.Instance.OutputFolder);
                CheckTimerForNewFiles(WatcherCore.Instance.DocumentsFolder);
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en OnTimerElapsed", ex);
            }
        }

        /// <summary>
        /// Busca nuevos archivos en una carpeta específica utilizando el timer
        /// </summary>
        private async void CheckTimerForNewFiles(string folderPath)
        {
            if (!Directory.Exists(folderPath))
                return;

            var currentFiles = Directory.GetFiles(folderPath, "*.pdf");

            // Log periódico para verificar actividad
            if (DateTime.Now.Second % 30 == 0) // Log cada 30 segundos aproximadamente
            {
                Console.WriteLine($"FileMonitor activo, monitoreando carpeta {folderPath} con {currentFiles.Length} archivos");
            }

            var newFiles = new List<string>();

            foreach (string file in currentFiles)
            {
                if (!WatcherCore.Instance.IsFileProcessed(file))
                {
                    // Verifica si el archivo es reciente (menos de 60 segundos)
                    var fileInfo = new FileInfo(file);
                    TimeSpan fileAge = DateTime.Now - fileInfo.CreationTime;

                    if (fileAge.TotalSeconds < 60 && WatcherCore.Instance.IsFileReady(file))
                    {
                        Console.WriteLine($"¡Nuevo archivo PDF detectado!: {file}, Edad: {fileAge.TotalSeconds} segundos");
                        newFiles.Add(file);
                        WatcherCore.Instance.MarkFileAsProcessed(file); // Marcar como procesado
                    }
                }
            }

            // Si hay archivos nuevos
            if (newFiles.Count > 0)
            {
                // Detener el timer mientras procesamos el archivo
                _fileWatchTimer.Stop();

                // Ordenar por fecha de creación (más reciente primero)
                var latestFile = newFiles
                    .OrderByDescending(f => new FileInfo(f).CreationTime)
                    .FirstOrDefault();

                Console.WriteLine($"Procesando el archivo más reciente: {latestFile}");

                // Procesar el archivo
                await DocumentProcessor.Instance.ProcessNewPrintJob(latestFile);

                // Reanudar el timer
                _fileWatchTimer.Start();
            }
        }

        public void Dispose()
        {
            try
            {
                StopMonitoring();
                _fileWatchTimer?.Dispose();
                _fileSystemWatcher?.Dispose();
                _documentsWatcher?.Dispose();
                Console.WriteLine("FileMonitor dispuesto");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al disponer FileMonitor: {ex.Message}");
            }
        }
    }
}