using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase responsable de la subida de documentos al servidor
    /// </summary>
    public class DocumentUploader
    {
        private static DocumentUploader _instance;
        public static DocumentUploader Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new DocumentUploader();
                return _instance;
            }
        }

        private DocumentUploader()
        {
            try
            {
                Console.WriteLine("Iniciando componente DocumentUploader...");
                Console.WriteLine("Componente DocumentUploader inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente DocumentUploader: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sube un documento al servidor
        /// </summary>
        public async Task<bool> UploadDocumentAsync(string filePath, string cabinetId, string categoryId,
            string subcategoryId, string title, string description)
        {
            WatcherLogger.LogActivity($"UploadDocumentAsync iniciado con archivo: {filePath}");

            try
            {
                // Verificar token de autenticación
                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    throw new InvalidOperationException("No hay una sesión activa. Por favor, inicie sesión.");
                }

                using (var client = new HttpClient())
                {
                    // Configurar cliente HTTP con token de autenticación
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", AppSession.Current.AuthToken);

                    // Crear contenido multipart
                    using (var content = new MultipartFormDataContent())
                    {
                        // Añadir el archivo
                        var fileBytes = File.ReadAllBytes(filePath);
                        var fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                        content.Add(fileContent, "file", Path.GetFileName(filePath));

                        // Añadir los datos del formulario
                        content.Add(new StringContent(cabinetId), "cabinet_id");
                        content.Add(new StringContent(categoryId), "category_id");

                        if (!string.IsNullOrEmpty(subcategoryId))
                        {
                            content.Add(new StringContent(subcategoryId), "subcategory_id");
                        }

                        if (!string.IsNullOrEmpty(title))
                        {
                            content.Add(new StringContent(title), "title");
                        }

                        if (!string.IsNullOrEmpty(description))
                        {
                            content.Add(new StringContent(description), "description");
                        }

                        // Enviar la solicitud a la API
                        WatcherLogger.LogActivity("Enviando solicitud HTTP a la API...");
                        var response = await client.PostAsync("https://ecm.ecmcentral.com/api/v2/documents/upload", content);

                        // Leer la respuesta
                        var responseContent = await response.Content.ReadAsStringAsync();
                        WatcherLogger.LogActivity($"Respuesta HTTP: {(int)response.StatusCode} {response.StatusCode}");
                        WatcherLogger.LogActivity($"Contenido: {responseContent}");

                        // Verificar si la solicitud fue exitosa
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new Exception($"Error del servidor ({response.StatusCode}): {responseContent}");
                        }

                        WatcherLogger.LogActivity("Documento subido exitosamente");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al subir documento", ex);
                return false;
            }
        }
    }
}