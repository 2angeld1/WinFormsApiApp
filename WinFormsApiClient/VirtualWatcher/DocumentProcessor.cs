using System;
using System.IO;
using System.Threading.Tasks;

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase responsable del procesamiento de documentos PDF
    /// </summary>
    public class DocumentProcessor
    {
        private static DocumentProcessor _instance;
        public static DocumentProcessor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DocumentProcessor();
                return _instance;
            }
        }

        private DocumentProcessor()
        {
            try
            {
                Console.WriteLine("Iniciando componente DocumentProcessor...");
                Console.WriteLine("Componente DocumentProcessor inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente DocumentProcessor: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Procesa un nuevo trabajo de impresión
        /// </summary>
        public async Task<bool> ProcessNewPrintJob(string pdfPath)
        {
            WatcherLogger.LogActivity($"ProcessNewPrintJob iniciado con archivo: {pdfPath}");

            try
            {
                // Verificar que el archivo existe y es accesible
                if (!File.Exists(pdfPath) || !WatcherCore.Instance.IsFileReady(pdfPath))
                {
                    WatcherLogger.LogActivity($"El archivo {pdfPath} no existe o no es accesible");
                    return false;
                }

                // Registrar información detallada sobre el archivo
                try
                {
                    FileInfo fileInfo = new FileInfo(pdfPath);
                    WatcherLogger.LogActivity($"Detalles del archivo: Tamaño={fileInfo.Length / 1024}KB, Creado={fileInfo.CreationTime}, Modificado={fileInfo.LastWriteTime}");
                }
                catch (Exception fileInfoEx)
                {
                    WatcherLogger.LogActivity($"Error al obtener detalles del archivo: {fileInfoEx.Message}");
                }

                // También registrar en el log de ECMVirtualPrinter
                try
                {
                    string ecmLogFolder = ECMVirtualPrinter.GetLogFolderPath();
                    string ecmLogFile = Path.Combine(ecmLogFolder, $"ecm_print_{DateTime.Now:yyyyMMdd}.log");

                    File.AppendAllText(ecmLogFile,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] PDF detectado por PrinterWatcher: {pdfPath}\r\n");

                    WatcherLogger.LogActivity($"Información registrada en log de ECMVirtualPrinter: {ecmLogFile}");
                }
                catch (Exception logEx)
                {
                    WatcherLogger.LogActivity($"Error al escribir en log ECM: {logEx.Message}");
                }

                // Verificar si la aplicación está ejecutándose
                FormularioForm activeForm = null;
                bool appIsRunning = FormInteraction.Instance.TryGetActiveForm(out activeForm);

                if (!appIsRunning)
                {
                    WatcherLogger.LogActivity("Aplicación no está abierta o formulario principal no está activo");

                    // La aplicación no está abierta o el formulario principal no está activo
                    // Verificar si hay una sesión activa
                    if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                    {
                        WatcherLogger.LogActivity("No hay sesión activa, lanzando aplicación con archivo pendiente");

                        // Necesitamos iniciar la aplicación y mostrar login
                        ApplicationLauncher.LaunchApplicationWithFile(pdfPath);
                        return true;
                    }

                    WatcherLogger.LogActivity("Sesión activa encontrada, mostrando formulario de documento");

                    // Hay sesión pero el formulario principal no está abierto
                    // Abrimos formulario de documento
                    return await FormInteraction.Instance.ShowDocumentForm(pdfPath);
                }
                else
                {
                    WatcherLogger.LogActivity("Aplicación ya está abierta, estableciendo archivo en formulario existente");

                    // La aplicación ya está abierta - necesitamos invocar en el thread de UI
                    return FormInteraction.Instance.SetFileInActiveForm(activeForm, pdfPath);
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al procesar el trabajo de impresión", ex);
                return false;
            }
        }
    }
}