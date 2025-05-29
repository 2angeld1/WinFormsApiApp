using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para manejar la subida de documentos al portal
    /// </summary>
    public class DocumentUploader
    {
        private static bool _initialized = false;

        static DocumentUploader()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente DocumentUploader...");
                    _initialized = true;
                    Console.WriteLine("Componente DocumentUploader inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente DocumentUploader: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Sube un archivo al portal
        /// </summary>
        public static async Task UploadToPortalAsync(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    // Aquí implementarías la lógica real para subir el archivo al portal
                    // Usando el token de autenticación desde AppSession
                    string token = WinFormsApiClient.AppSession.Current.AuthToken;

                    // Simulación de subida (reemplaza esto con la implementación real)
                    await Task.Delay(1000); // Simular procesamiento
                    MessageBox.Show($"Archivo {Path.GetFileName(filePath)} subido exitosamente al portal.",
                        "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("El archivo especificado ya no existe o no se puede acceder a él.",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al subir el archivo al portal: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}