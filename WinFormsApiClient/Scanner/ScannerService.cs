using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;

namespace WinFormsApiClient.Scanner
{
    /// <summary>
    /// Servicio para interactuar con el escáner a nivel de aplicación
    /// </summary>
    public static class ScannerService
    {
        /// <summary>
        /// Inicia el proceso de escaneo de documento y subida al portal
        /// </summary>
        public static async Task<bool> ScanAndUploadAsync()
        {
            try
            {
                // Verificar licencia para escanear
                if (!await CheckLicenseForScanningAsync())
                {
                    MessageBox.Show(
                        "Su cuenta no tiene permisos para utilizar la función de escaneo.\n" +
                        "Por favor, contacte con el administrador para actualizar su licencia.",
                        "Licencia insuficiente",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return false;
                }

                // Usar el ScannerManager para gestionar el proceso
                return await ScannerManager.Instance.ScanAndUploadAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al iniciar el proceso de escaneo: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Verifica si el usuario tiene licencia para usar la funcionalidad de escaneo
        /// </summary>
        private static async Task<bool> CheckLicenseForScanningAsync()
        {
            try
            {
                // Obtener las características de licencia actuales
                var userLicense = WinFormsApiClient.ECMVirtualPrinter.UserLicense;

                // Verificar si la funcionalidad de escaneo está incluida
                bool hasScanLicense = userLicense.HasFlag(WinFormsApiClient.ECMVirtualPrinter.LicenseFeatures.Scan);

                if (hasScanLicense)
                    return true;

                // Si no tiene licencia, intentar verificar nuevamente con el servidor
                bool licenseVerified = await WinFormsApiClient.ECMVirtualPrinter.CheckLicenseAsync();

                if (!licenseVerified)
                    return false;

                // Verificar nuevamente después de actualizar desde el servidor
                userLicense = WinFormsApiClient.ECMVirtualPrinter.UserLicense;
                return userLicense.HasFlag(WinFormsApiClient.ECMVirtualPrinter.LicenseFeatures.Scan);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar licencia de escaneo: {ex.Message}");
                // Por defecto permitir si hay un error
                return true;
            }
        }

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

                return;
            }

            // Si no se encontró el formulario, entonces subimos directamente a través de API
            await UploadFileThroughApiAsync(filePath);
        }

        /// <summary>
        /// Sube un archivo directamente a través de la API
        /// </summary>
        private static async Task UploadFileThroughApiAsync(string filePath)
        {
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

                    // Enviar la solicitud a la API
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