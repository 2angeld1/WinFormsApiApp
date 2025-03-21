using System;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;

namespace WinFormsApiClient
{
    public partial class FormularioForm : MaterialForm
    {

        public FormularioForm()
        {
            InitializeComponent();
            // Asegúrate de configurar el tema de MaterialSkin aquí
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT; // Puedes cambiar a DARK si lo prefieres
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Blue500, Primary.Blue600, Primary.Blue300, Accent.LightBlue200, TextShade.WHITE);
        }

        // Lógica para seleccionar un archivo
        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                fileLabel.Text = fileDialog.FileName;
            }
        }

        // Lógica para el botón de enviar
        private void SubmitButton_Click(object sender, EventArgs e)
        {
            MessageBox.Show("Formulario enviado con éxito", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnScanToPortal_Click(object sender, EventArgs e)
        {
            Scanner.ScanAndUpload();
        }

        private void BtnPrintToPortal_Click(object sender, EventArgs e)
        {
            string filePath = "C:\\Users\\Public\\Documents\\documento.docx"; // Ruta de prueba
            PrinterHandler.PrintToCloud(filePath);
        }

    }
}