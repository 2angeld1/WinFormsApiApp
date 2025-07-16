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
        public static void OpenDownloadPage()
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://download.pdfforge.org/download/pdfcreator/PDFCreator-stable",
                UseShellExecute = true
            });
        }
    }        
}
