using System;
using System.Threading.Tasks;
using WinFormsApiClient.NewVirtualPrinter;
//using WinFormsApiClient.Scanner; // Agregar esta línea

namespace WinFormsApiClient
{
    /// <summary>
    /// Clase principal de la impresora virtual ECM Central - Nueva implementación
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
                    Console.WriteLine("Iniciando componente ECMVirtualPrinter (Nueva implementación)...");
                    _initialized = true;
                    Console.WriteLine("Componente ECMVirtualPrinter inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente ECMVirtualPrinter: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicializa el sistema de impresora virtual
        /// </summary>
        public static async Task<bool> InitializeAsync()
        {
            return await VirtualPrinterService.InitializeAsync();
        }

        /// <summary>
        /// Verifica si Bullzip está instalado
        /// </summary>
        public static bool IsPrinterInstalled()
        {
            return PDFCreatorManager.IsPDFCreatorInstalled();
        }

        /// <summary>
        /// Procesa un trabajo de impresión
        /// </summary>
        public static void ProcessPrintJob(string filePath)
        {
            VirtualPrinterService.ProcessPrintCommand(filePath);
        }

        /// <summary>
        /// Ejecuta diagnósticos del sistema
        /// </summary>
        public static void RunDiagnostics()
        {
            VirtualPrinterService.RunDiagnostics();
        }

        /// <summary>
        /// Obtiene la carpeta de salida
        /// </summary>
        public static string GetOutputFolder()
        {
            return PDFCreatorManager.OUTPUT_FOLDER;
        }
        ///// <summary>
        ///// Inicia el proceso de escaneo y subida - NUEVO
        ///// </summary>
        //public static async Task<bool> ScanAndUploadAsync()
        //{
        //    return await ScannerService.ScanAndUploadAsync();
        //}
    }
}