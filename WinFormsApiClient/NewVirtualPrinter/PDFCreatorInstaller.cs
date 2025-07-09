using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;

namespace WinFormsApiClient.NewVirtualPrinter
{
    public static class PDFCreatorInstaller
    {
        private const string PDFCREATOR_DOWNLOAD_URL = "https://download.pdfforge.org/download/pdfcreator/PDFCreator-5_1_2-Setup.exe";
        private const string INSTALLER_NAME = "PDFCreator-Setup.exe";

        /// <summary>
        /// Descarga e instala PDFCreator
        /// </summary>
        public static async Task<bool> DownloadAndInstallAsync()
        {
            try
            {
                Console.WriteLine("Descargando PDFCreator...");

                string tempFolder = Path.Combine(Path.GetTempPath(), "ECM_PDFCreator");
                if (!Directory.Exists(tempFolder))
                    Directory.CreateDirectory(tempFolder);

                string installerPath = Path.Combine(tempFolder, INSTALLER_NAME);

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(15);
                    var response = await client.GetAsync(PDFCREATOR_DOWNLOAD_URL);
                    response.EnsureSuccessStatusCode();

                    var fileBytes = await response.Content.ReadAsByteArrayAsync();
                    File.WriteAllBytes(installerPath, fileBytes);
                }

                Console.WriteLine($"PDFCreator descargado: {installerPath}");

                // Ejecutar instalador
                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = "/VERYSILENT /NORESTART", // Instalación muy silenciosa
                    UseShellExecute = true,
                    Verb = "runas"
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        await Task.Run(() => process.WaitForExit(600000)); // 10 minutos

                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("PDFCreator instalado correctamente");
                            try { File.Delete(installerPath); } catch { }
                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error instalando PDFCreator: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Asegura que PDFCreator esté instalado
        /// </summary>
        public static async Task<bool> EnsurePDFCreatorInstalledAsync()
        {
            if (PDFCreatorManager.IsPDFCreatorInstalled())
            {
                Console.WriteLine("PDFCreator ya está instalado");
                return true;
            }

            var result = MessageBox.Show(
                "ECM Central necesita instalar PDFCreator para funcionar correctamente.\n\n" +
                "¿Desea descargar e instalar PDFCreator ahora?\n\n" +
                "Nota: Se requieren permisos de administrador.",
                "Instalar PDFCreator",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result != DialogResult.Yes)
                return false;

            using (var progressForm = new Form())
            {
                progressForm.Text = "Instalando PDFCreator";
                progressForm.Size = new Size(400, 120);
                progressForm.StartPosition = FormStartPosition.CenterScreen;
                progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                progressForm.MaximizeBox = false;
                progressForm.MinimizeBox = false;

                var label = new Label
                {
                    Text = "Descargando e instalando PDFCreator...",
                    AutoSize = true,
                    Location = new Point(20, 20)
                };
                progressForm.Controls.Add(label);

                var progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    Location = new Point(20, 50),
                    Size = new Size(350, 23)
                };
                progressForm.Controls.Add(progressBar);

                progressForm.Show();
                Application.DoEvents();

                try
                {
                    bool success = await DownloadAndInstallAsync();
                    progressForm.Close();

                    if (success)
                    {
                        MessageBox.Show("PDFCreator instalado correctamente.",
                            "Instalación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("Error durante la instalación de PDFCreator.",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    progressForm.Close();
                    MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }
        }
    }
}