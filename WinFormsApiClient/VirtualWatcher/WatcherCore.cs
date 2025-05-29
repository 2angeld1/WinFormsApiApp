using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase base que contiene la configuración y funcionalidad central del sistema de monitoreo
    /// </summary>
    public class WatcherCore
    {
        // Nombre de la impresora a monitorear
        public const string PRINTER_NAME = "PDFCreator";
        // Carpeta donde se guardarán los PDFs generados
        private string _outputFolder;

        // Nueva carpeta adicional para monitorear (carpeta de Documentos)
        private string _documentsFolder;

        // HashSet para mantener registro de los archivos ya procesados
        private HashSet<string> _processedFiles = new HashSet<string>();

        // Flag para inicialización
        public bool _isInitialized = false;

        private static WatcherCore _instance;
        public static WatcherCore Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new WatcherCore();
                return _instance;
            }
        }

        private WatcherCore()
        {
            try
            {
                Console.WriteLine("Iniciando componente WatcherCore...");

                // Configurar la carpeta de salida fija
                _outputFolder = VirtualPrinter.VirtualPrinterCore.GetOutputFolderPath();
                Console.WriteLine($"WatcherCore configurado para monitorear carpeta: {_outputFolder}");

                // También monitorear la carpeta de Documentos (donde el usuario suele guardar)
                _documentsFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    VirtualPrinter.VirtualPrinterCore.OUTPUT_FOLDER);
                Console.WriteLine($"WatcherCore configurado para monitorear carpeta adicional: {_documentsFolder}");

                // Asegurar que ambas carpetas existan
                EnsureFoldersExist();

                // Registrar archivos actuales para no procesarlos nuevamente
                RegisterExistingFiles();

                _isInitialized = true;
                Console.WriteLine("Componente WatcherCore inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente WatcherCore: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Asegura que las carpetas de monitoreo existan
        /// </summary>
        private void EnsureFoldersExist()
        {
            try
            {
                // Crear la carpeta principal si no existe
                if (!Directory.Exists(_outputFolder))
                {
                    Directory.CreateDirectory(_outputFolder);
                    Console.WriteLine($"Carpeta de monitoreo principal creada: {_outputFolder}");
                }
                else
                {
                    // Listar archivos existentes para depuración
                    var existingFiles = Directory.GetFiles(_outputFolder, "*.pdf");
                    Console.WriteLine($"Carpeta principal existente con {existingFiles.Length} archivos PDF");
                }

                // Crear la carpeta de documentos si no existe
                if (!Directory.Exists(_documentsFolder))
                {
                    Directory.CreateDirectory(_documentsFolder);
                    Console.WriteLine($"Carpeta de monitoreo secundaria creada: {_documentsFolder}");
                }
                else
                {
                    // Listar archivos existentes para depuración
                    var existingFiles = Directory.GetFiles(_documentsFolder, "*.pdf");
                    Console.WriteLine($"Carpeta secundaria existente con {existingFiles.Length} archivos PDF");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear carpetas de monitoreo: {ex.Message}");
            }
        }

        /// <summary>
        /// Registra archivos existentes para evitar reprocesamiento
        /// </summary>
        private void RegisterExistingFiles()
        {
            try
            {
                // Registrar archivos de la carpeta principal
                if (Directory.Exists(_outputFolder))
                {
                    foreach (string file in Directory.GetFiles(_outputFolder, "*.pdf"))
                    {
                        _processedFiles.Add(file);
                    }
                }

                // Registrar archivos de la carpeta secundaria
                if (Directory.Exists(_documentsFolder))
                {
                    foreach (string file in Directory.GetFiles(_documentsFolder, "*.pdf"))
                    {
                        _processedFiles.Add(file);
                    }
                }

                Console.WriteLine($"Total de archivos existentes registrados: {_processedFiles.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar archivos existentes: {ex.Message}");
            }
        }

        /// <summary>
        /// Obtiene la carpeta de salida donde se guardan los archivos PDF
        /// </summary>
        public string OutputFolder => _outputFolder;

        /// <summary>
        /// Obtiene la carpeta secundaria (Documentos) donde se guardan los archivos PDF
        /// </summary>
        public string DocumentsFolder => _documentsFolder;

        /// <summary>
        /// Verifica si un archivo ya ha sido procesado
        /// </summary>
        public bool IsFileProcessed(string filePath)
        {
            return _processedFiles.Contains(filePath);
        }

        /// <summary>
        /// Marca un archivo como procesado
        /// </summary>
        public void MarkFileAsProcessed(string filePath)
        {
            if (!_processedFiles.Contains(filePath))
            {
                _processedFiles.Add(filePath);
            }
        }

        /// <summary>
        /// Obtiene la cantidad de archivos procesados
        /// </summary>
        public int ProcessedFilesCount => _processedFiles.Count;

        /// <summary>
        /// Obtiene una copia de la lista de archivos procesados
        /// </summary>
        public List<string> GetProcessedFilesList()
        {
            return new List<string>(_processedFiles);
        }

        /// <summary>
        /// Verifica si un archivo está listo para ser procesado (no está bloqueado)
        /// </summary>
        public bool IsFileReady(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}