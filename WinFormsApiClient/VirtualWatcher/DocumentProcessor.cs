using System;
using System.IO;
using System.Threading;
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
        public async Task<bool> ProcessNewPrintJob(string pdfPath)
        {
            WatcherLogger.LogActivity($"ProcessNewPrintJob iniciado con archivo: {pdfPath}");

            try
            {
                // MEJORA: Verificación más robusta del archivo
                if (!File.Exists(pdfPath))
                {
                    WatcherLogger.LogActivity($"El archivo {pdfPath} no existe");
                    return false;
                }

                // MEJORA: Usar FileInfo una sola vez para evitar múltiples accesos
                FileInfo fileInfo = new FileInfo(pdfPath);

                // Verificar si el archivo es accesible con un enfoque más seguro
                if (!await IsFileAccessibleAsync(pdfPath))
                {
                    WatcherLogger.LogActivity($"El archivo {pdfPath} no es accesible en este momento");
                    // Programar reintento posterior
                    CreatePendingMarker(pdfPath);
                    return false;
                }

                // MEJORA: Comprobar tamaño del archivo
                if (fileInfo.Length > 50 * 1024 * 1024)
                {
                    WatcherLogger.LogActivity($"¡ADVERTENCIA! Archivo grande detectado: {fileInfo.Length / (1024 * 1024)}MB");
                }

                // MEJORA: Manejar la forma en que interactuamos con el formulario
                return await ProcessFileWithUIInteraction(pdfPath);
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al procesar el trabajo de impresión", ex);
                CreatePendingMarker(pdfPath);
                return false;
            }
        }

        // NUEVO: Método para verificar acceso a archivo de forma asíncrona
        private async Task<bool> IsFileAccessibleAsync(string filePath)
        {
            try
            {
                // Usar CancellationToken para limitar el tiempo de espera
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
                {
                    return await Task.Run(() => {
                        try
                        {
                            using (FileStream fs = new FileStream(
                                filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            {
                                // Leer algunos bytes para verificar acceso
                                byte[] buffer = new byte[1024];
                                return fs.Read(buffer, 0, buffer.Length) > 0;
                            }
                        }
                        catch
                        {
                            return false;
                        }
                    }, cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout alcanzado
                WatcherLogger.LogActivity("Timeout al verificar acceso al archivo");
                return false;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al verificar acceso al archivo", ex);
                return false;
            }
        }

        // NUEVO: Método para procesar archivo con mejor interacción con UI
        private async Task<bool> ProcessFileWithUIInteraction(string pdfPath)
        {
            // Verificar si la aplicación está ejecutándose
            FormularioForm activeForm = null;
            bool appIsRunning = FormInteraction.Instance.TryGetActiveForm(out activeForm);

            if (!appIsRunning)
            {
                WatcherLogger.LogActivity("Aplicación no está abierta o formulario principal no está activo");

                // MEJORA: Crear copia de seguridad del archivo antes de lanzar la aplicación
                string backupFile = CreateSecureBackup(pdfPath);

                // Si no hay sesión, lanzar aplicación desde cero
                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    WatcherLogger.LogActivity("No hay sesión activa, lanzando aplicación con archivo pendiente");

                    // Llamar al método y luego devolver un valor booleano directamente
                    ApplicationLauncher.LaunchApplicationWithFile(backupFile ?? pdfPath);

                    // En un método async, devolvemos el valor base (bool), no Task.FromResult
                    return true;
                }

                // Si hay sesión pero formulario no está abierto, mostrar formulario
                return await FormInteraction.Instance.ShowDocumentForm(backupFile ?? pdfPath);
            }
            else
            {
                WatcherLogger.LogActivity("Aplicación ya está abierta, estableciendo archivo con tiempo de espera");

                // MEJORA: Copiar el archivo antes de enviarlo al formulario
                string tempCopy = CreateSecureBackup(pdfPath);
                string fileToUse = tempCopy ?? pdfPath;

                // MEJORA: Usar Task.Run con mejor manejo de timeout
                try
                {
                    using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                    {
                        var setFileTask = Task.Run(() =>
                            FormInteraction.Instance.SetFileInActiveFormWithTimeout(
                                activeForm, fileToUse, cts.Token));

                        return await setFileTask;
                    }
                }
                catch (TaskCanceledException)
                {
                    // Timeout - crear marcador para reintento posterior
                    WatcherLogger.LogActivity("Timeout al establecer archivo en formulario, creando marcador para reintento");
                    CreatePendingMarker(pdfPath);
                    return false;
                }
            }
        }

        // NUEVO: Método para crear copia de seguridad del archivo
        private string CreateSecureBackup(string originalPath)
        {
            try
            {
                string tempDir = Path.Combine(Path.GetTempPath(), "ECM_Backups");
                if (!Directory.Exists(tempDir))
                    Directory.CreateDirectory(tempDir);

                string tempFile = Path.Combine(
                    tempDir,
                    $"{Path.GetFileNameWithoutExtension(originalPath)}_{Guid.NewGuid().ToString("N")}{Path.GetExtension(originalPath)}");

                // Usar filestream para evitar bloqueos
                using (FileStream source = new FileStream(originalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (FileStream dest = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    source.CopyTo(dest);
                }

                WatcherLogger.LogActivity($"Copia de seguridad creada: {tempFile}");
                return tempFile;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al crear copia de seguridad", ex);
                return null; // Devolver null para indicar que se debe usar el archivo original
            }
        }

        private void CreatePendingMarker(string filePath)
        {
            try
            {
                // Guardar en múltiples ubicaciones para mayor fiabilidad
                string[] markerPaths = {
            Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt"),
            Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "pending_pdf.marker"),
            Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "last_bullzip_file.txt")
        };

                foreach (string path in markerPaths)
                {
                    try
                    {
                        File.WriteAllText(path, filePath);
                    }
                    catch { /* Ignorar errores individuales */ }
                }
            }
            catch { /* Ignorar errores */ }
        }
    }
}