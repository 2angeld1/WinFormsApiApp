using System;
using System.Diagnostics;
using System.IO;
using System.Printing;
using System.Windows.Forms;

public class PrinterHandler
{
    public static void PrintToCloud(string filePath)
    {
        try
        {
            string printerName = "Microsoft Print to PDF";  // Impresora virtual
            string outputPdf = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "printout.pdf");

            // Configurar el proceso para imprimir
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = filePath,
                Verb = "print",
                CreateNoWindow = true,
                Arguments = $"\"{printerName}\"" // 🔹 Aquí usamos la variable
            };

            Process process = Process.Start(psi);
            process.WaitForExit();

            MessageBox.Show($"Documento impreso en {outputPdf}");

            // Subir el archivo al portal
            UploadToPortal(outputPdf);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al imprimir: {ex.Message}");
        }
    }

    public static void UploadToPortal(string filePath)
    {
        // Aquí implementas la lógica para subir el archivo al portal
        MessageBox.Show($"Subiendo {filePath} al portal...");
    }
}
