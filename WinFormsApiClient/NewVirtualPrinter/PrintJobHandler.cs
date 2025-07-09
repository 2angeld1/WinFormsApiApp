using System;
using System.IO;
using System.Windows.Forms;
namespace WinFormsApiClient.NewVirtualPrinter
{
    /// <summary>
    /// Clase para manejar trabajos de impresión desde Bullzip
    /// </summary>
    public static class PrintJobHandler
    {
        /// <summary>
        /// Procesa un archivo PDF generado por Bullzip
        /// </summary>
        public static void ProcessPrintJob(string pdfFilePath)
        {
            try
            {
                Console.WriteLine($"Procesando trabajo de impresión: {pdfFilePath}");

                // Verificar que el archivo existe
                if (!File.Exists(pdfFilePath))
                {
                    Console.WriteLine("Error: El archivo PDF no existe");
                    return;
                }

                // Log del trabajo
                string logPath = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "print_jobs.log");
                File.AppendAllText(logPath, $"[{DateTime.Now}] Nuevo trabajo: {Path.GetFileName(pdfFilePath)}\r\n");

                // Abrir la aplicación con el archivo
                OpenApplicationWithFile(pdfFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error procesando trabajo de impresión: {ex.Message}");
                
                // Log del error
                string errorLog = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "print_errors.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error procesando {pdfFilePath}: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Abre la aplicación principal con el archivo PDF
        /// </summary>
        private static void OpenApplicationWithFile(string pdfFilePath)
        {
            try
            {
                // Verificar si la aplicación ya está abierta
                foreach (Form form in Application.OpenForms)
                {
                    if (form is FormularioForm formularioForm)
                    {
                        // La aplicación ya está abierta, establecer el archivo
                        form.Invoke(new Action(() =>
                        {
                            formularioForm.EstablecerArchivoSeleccionado(pdfFilePath);
                            form.WindowState = FormWindowState.Normal;
                            form.BringToFront();
                        }));
                        
                        Console.WriteLine("Archivo establecido en formulario existente");
                        return;
                    }
                }

                // La aplicación no está abierta, verificar sesión
                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    // No hay sesión, abrir LoginForm con archivo pendiente
                    Application.Run(new LoginForm(pdfFilePath));
                }
                else
                {
                    // Hay sesión, abrir FormularioForm directamente
                    var formularioForm = new FormularioForm();
                    formularioForm.Load += (s, e) =>
                    {
                        formularioForm.EstablecerArchivoSeleccionado(pdfFilePath);
                    };
                    Application.Run(formularioForm);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error abriendo aplicación: {ex.Message}");
                
                // Mostrar mensaje al usuario
                MessageBox.Show(
                    $"Se ha generado un PDF pero ocurrió un error al abrir la aplicación:\n{ex.Message}\n\nArchivo: {pdfFilePath}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}