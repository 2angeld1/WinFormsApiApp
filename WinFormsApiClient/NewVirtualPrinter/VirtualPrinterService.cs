using System;
using System.Threading.Tasks;

namespace WinFormsApiClient.NewVirtualPrinter
{
    /// <summary>
    /// Servicio principal para la nueva implementación de impresora virtual con PDFCreator
    /// </summary>
    public static class VirtualPrinterService
    {
        private static bool _initialized = false;
        
        /// <summary>
        /// Inicializa el servicio de impresora virtual con PDFCreator
        /// </summary>
        public static async Task<bool> InitializeAsync()
        {
            if (_initialized)
                return true;

            try
            {
                Console.WriteLine("Inicializando servicio de impresora virtual con PDFCreator...");

                // 1. Asegurar que PDFCreator esté instalado
                bool pdfCreatorReady = await PDFCreatorInstaller.EnsurePDFCreatorInstalledAsync();
                if (!pdfCreatorReady)
                {
                    Console.WriteLine("Error: No se pudo instalar PDFCreator");
                    return false;
                }

                // 2. Configurar PDFCreator
                bool configured = PDFCreatorManager.ConfigurePDFCreator();
                if (!configured)
                {
                    Console.WriteLine("Error: No se pudo configurar PDFCreator");
                    return false;
                }

                _initialized = true;
                Console.WriteLine("Servicio de impresora virtual con PDFCreator inicializado correctamente");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inicializando servicio: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Procesa un comando de impresión desde la línea de comandos
        /// </summary>
        public static void ProcessPrintCommand(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("Error: Ruta de archivo vacía");
                    return;
                }

                Console.WriteLine($"Procesando comando de impresión con PDFCreator: {filePath}");
                PrintJobHandler.ProcessPrintJob(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando comando de impresión: {ex.Message}");
            }
        }

        /// <summary>
        /// Ejecuta diagnóstico usando PDFCreator
        /// </summary>
        public static void RunDiagnostics()
        {
            try
            {
                Console.WriteLine("Ejecutando diagnóstico del sistema PDFCreator...");
                
                Console.WriteLine($"¿PDFCreator instalado? {PDFCreatorManager.IsPDFCreatorInstalled()}");
                PDFCreatorManager.DiagnosePDFCreator();
                
                Console.WriteLine("Diagnóstico completado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en diagnóstico: {ex.Message}");
            }
        }
    }
}