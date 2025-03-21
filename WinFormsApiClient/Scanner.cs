using System;
using System.IO;
using System.Windows.Forms;
using WIA;  // Requiere Microsoft Windows Image Acquisition Library v2.0

public class Scanner
{
    public static void ScanAndUpload()
    {
        try
        {
            WIA.CommonDialog dialog = new WIA.CommonDialog();
            ImageFile img = dialog.ShowAcquireImage(WiaDeviceType.ScannerDeviceType);

            if (img != null)
            {
                string outputPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "scan.jpg");
                img.SaveFile(outputPath);
                MessageBox.Show($"Documento escaneado guardado en: {outputPath}");

                // Llamar a la función para subir el archivo al portal
                UploadToPortal(outputPath);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al escanear: {ex.Message}");
        }
    }

    public static void UploadToPortal(string filePath)
    {
        // Aquí va la lógica para subir el archivo a la nube
        MessageBox.Show($"Subiendo {filePath} al portal...");
        // Implementa una petición HTTP para subir el archivo al servidor
    }
}
