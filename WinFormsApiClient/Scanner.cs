// Implementación de la clase Scanner
using System;
using System.IO;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;

namespace WinFormsApiClient
{
    public static class Scanner
    {
        /// <summary>
        /// Sube un archivo al portal usando la API de ECM Central
        /// </summary>
        public static async Task UploadToPortalAsync(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new FileNotFoundException("No se pudo encontrar el archivo para subir al portal.");
            }

            // Obtener el formulario principal activo
            FormularioForm mainForm = null;
            foreach (System.Windows.Forms.Form form in System.Windows.Forms.Application.OpenForms)
            {
                if (form is FormularioForm)
                {
                    mainForm = (FormularioForm)form;
                    break;
                }
            }

            if (mainForm != null)
            {
                // Establecer el archivo en el campo de entrada del formulario principal
                mainForm.EstablecerArchivoSeleccionado(filePath);

                // Notificar al usuario que el archivo ha sido seleccionado automáticamente
                System.Windows.Forms.MessageBox.Show(
                    $"El archivo '{Path.GetFileName(filePath)}' ha sido adjuntado automáticamente al formulario.",
                    "Archivo adjuntado",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Information);

                // Nota: Ahora el usuario puede completar los metadatos y enviar el formulario
                return;
            }

            // Si no se encontró el formulario, entonces subimos directamente a través de API
            // (aquí continuaría la implementación original de la subida a través de API)
            using (HttpClient client = new HttpClient())
            {
                // Configurar la autenticación y encabezados
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", AppSession.Current.AuthToken);

                // Leer el archivo como bytes para adjuntarlo
                byte[] fileBytes = File.ReadAllBytes(filePath);

                // Crear el contenido multipart para la solicitud
                using (var content = new MultipartFormDataContent())
                {
                    var fileContent = new ByteArrayContent(fileBytes);
                    fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                    // Agregar el archivo a la solicitud
                    content.Add(fileContent, "documents[]", Path.GetFileName(filePath));

                    // Aquí agregaríamos más metadatos si fuera necesario

                    // Enviar la solicitud a la API (URL de ejemplo)
                    HttpResponseMessage response = await client.PostAsync(
                        "https://ecm.ecmcentral.com/api/v2/documents", content);

                    // Verificar si la solicitud fue exitosa
                    if (!response.IsSuccessStatusCode)
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Error al subir el documento: {error}");
                    }
                }
            }
        }
    }
}