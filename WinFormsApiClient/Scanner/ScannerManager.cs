using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using WIA;

namespace WinFormsApiClient.Scanner
{
    /// <summary>
    /// Clase responsable de la administración del escáner
    /// </summary>
    public class ScannerManager
    {
        private static ScannerManager _instance;
        private readonly string _tempScanFolder;
        private bool _initialized = false;

        public static ScannerManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new ScannerManager();
                return _instance;
            }
        }

        public ScannerManager()
        {
            try
            {
                Console.WriteLine("Iniciando componente ScannerManager...");

                // Crear carpeta temporal para almacenar escaneos
                _tempScanFolder = Path.Combine(
                    Path.GetTempPath(),
                    "ECM_Central_Scans");

                if (!Directory.Exists(_tempScanFolder))
                    Directory.CreateDirectory(_tempScanFolder);

                _initialized = true;
                Console.WriteLine("Componente ScannerManager inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente ScannerManager: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Escanea un documento y devuelve la ruta al archivo temporal creado
        /// </summary>
        public async Task<string> ScanDocumentAsync()
        {
            return await Task.Run(() => ScanDocument());
        }

        /// <summary>
        /// Implementación sincrónica del escaneo de documentos
        /// </summary>
        private string ScanDocument()
        {
            try
            {
                // Mostrar el diálogo de selección de escáner
                var device = SelectScannerDevice();
                if (device == null)
                    return null;

                // Configurar parámetros de escaneo
                var item = device.Items[1]; // Normalmente el primer ítem es el escáner plano

                // Configurar propiedades de escaneo
                SetScannerProperties(ref item);

                // Realizar el escaneo
                var imageFile = (ImageFile)item.Transfer();

                // Guardar la imagen escaneada
                string filename = Path.Combine(_tempScanFolder, $"scan_{DateTime.Now:yyyyMMddHHmmss}.pdf");
                SaveScannedImageToPdf(imageFile, filename);

                return filename;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al escanear documento: {ex.Message}",
                    "Error de escaneo", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Muestra diálogo para seleccionar un escáner
        /// </summary>
        private Device SelectScannerDevice()
        {
            try
            {
                // Crear el administrador de dispositivos WIA
                var deviceManager = new DeviceManager();

                // Lista para almacenar los dispositivos disponibles
                var availableDevices = new System.Collections.Generic.List<DeviceInfo>();

                // Enumerar todos los dispositivos disponibles
                foreach (DeviceInfo deviceInfo in deviceManager.DeviceInfos)
                {
                    if (deviceInfo.Type == WiaDeviceType.ScannerDeviceType)
                    {
                        availableDevices.Add(deviceInfo);
                    }
                }

                // Si no hay escáneres disponibles
                if (availableDevices.Count == 0)
                {
                    MessageBox.Show("No se encontraron escáneres conectados al sistema.",
                        "Sin escáneres", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return null;
                }

                // Si solo hay un escáner, usarlo directamente
                if (availableDevices.Count == 1)
                {
                    return availableDevices[0].Connect();
                }

                // Mostrar diálogo para seleccionar entre múltiples escáneres
                using (var deviceSelectionForm = new Form())
                {
                    deviceSelectionForm.Text = "Seleccionar escáner";
                    deviceSelectionForm.Size = new Size(400, 200);
                    deviceSelectionForm.StartPosition = FormStartPosition.CenterScreen;
                    deviceSelectionForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    deviceSelectionForm.MaximizeBox = false;
                    deviceSelectionForm.MinimizeBox = false;

                    var label = new Label
                    {
                        Text = "Seleccione un escáner:",
                        Location = new Point(10, 10),
                        AutoSize = true
                    };

                    var comboBox = new ComboBox
                    {
                        Location = new Point(10, 40),
                        Width = 360,
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };

                    foreach (var device in availableDevices)
                    {
                        comboBox.Items.Add(device.Properties["Name"].get_Value());
                    }
                    comboBox.SelectedIndex = 0;

                    var okButton = new Button
                    {
                        Text = "Aceptar",
                        Location = new Point(200, 120),
                        DialogResult = DialogResult.OK
                    };

                    var cancelButton = new Button
                    {
                        Text = "Cancelar",
                        Location = new Point(290, 120),
                        DialogResult = DialogResult.Cancel
                    };

                    deviceSelectionForm.Controls.AddRange(new Control[] { label, comboBox, okButton, cancelButton });
                    deviceSelectionForm.AcceptButton = okButton;
                    deviceSelectionForm.CancelButton = cancelButton;

                    if (deviceSelectionForm.ShowDialog() == DialogResult.OK)
                    {
                        return availableDevices[comboBox.SelectedIndex].Connect();
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al buscar escáneres: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
        }

        /// <summary>
        /// Configura las propiedades del escáner
        /// </summary>
        private void SetScannerProperties(ref Item item)
        {
            try
            {
                // Configurar propiedades comunes de escaneo
                SetWiaProperty(item.Properties, "Horizontal Resolution", 300);
                SetWiaProperty(item.Properties, "Vertical Resolution", 300);
                SetWiaProperty(item.Properties, "Current Intent", 2); // Color
                SetWiaProperty(item.Properties, "Scan Color Mode", 2); // Color
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar propiedades del escáner: {ex.Message}");
                // Continuar con valores predeterminados
            }
        }

        /// <summary>
        /// Establece una propiedad WIA específica
        /// </summary>
        private void SetWiaProperty(IProperties properties, object propName, object propValue)
        {
            try
            {
                Property prop = properties.get_Item(propName);
                if (prop != null)
                {
                    prop.set_Value(propValue);
                }
            }
            catch
            {
                // Ignorar errores - algunas propiedades pueden no existir en todos los escáneres
            }
        }

        /// <summary>
        /// Guarda la imagen escaneada como PDF
        /// </summary>
        private void SaveScannedImageToPdf(ImageFile imageFile, string filename)
        {
            // Crear un archivo temporal para la imagen
            string tempImageFile = Path.Combine(
                _tempScanFolder,
                $"temp_scan_{Guid.NewGuid().ToString("N")}.jpg");

            try
            {
                // Guardar la imagen en formato JPG
                imageFile.SaveFile(tempImageFile);

                // Cargar la imagen guardada
                using (System.Drawing.Image img = System.Drawing.Image.FromFile(tempImageFile))
                {
                    // Convertir a PDF usando iTextSharp (alternativa: usar otra librería PDF)
                    ConvertImageToPdf(img, filename);
                }
            }
            finally
            {
                // Limpiar el archivo temporal
                if (File.Exists(tempImageFile))
                {
                    try { File.Delete(tempImageFile); } catch { }
                }
            }
        }

        /// <summary>
        /// Convierte una imagen a PDF
        /// </summary>
        private void ConvertImageToPdf(System.Drawing.Image image, string outputPath)
        {
            // Nota: Esta es una implementación simplificada usando System.Drawing
            // En una implementación real, usaríamos una librería como iTextSharp

            // Como alternativa simple, guardamos en formato JPEG con extensión PDF
            // En una implementación real, esto debería cambiarse por una verdadera conversión a PDF
            image.Save(outputPath, ImageFormat.Jpeg);

            // Mostrar advertencia de que esto es una implementación simplificada
            Console.WriteLine("Nota: Se requiere integrar una librería PDF real para la conversión adecuada");
        }

        /// <summary>
        /// Abre un diálogo para escanear y subir un documento
        /// </summary>
        public async Task<bool> ScanAndUploadAsync()
        {
            try
            {
                // Escanear el documento
                string scannedFilePath = await ScanDocumentAsync();

                if (string.IsNullOrEmpty(scannedFilePath) || !File.Exists(scannedFilePath))
                {
                    // El escaneo fue cancelado o falló
                    return false;
                }

                // Mostrar diálogo de confirmación
                DialogResult result = MessageBox.Show(
                    "El documento se ha escaneado correctamente.\n¿Desea subirlo al portal?",
                    "Escaneo completo",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Subir el documento al portal
                    await UploadScannedDocumentAsync(scannedFilePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al escanear y subir documento: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Sube un documento escaneado al portal
        /// </summary>
        private async Task UploadScannedDocumentAsync(string filePath)
        {
            try
            {
                // Obtener el formulario principal activo
                FormularioForm mainForm = null;
                foreach (Form form in Application.OpenForms)
                {
                    if (form is FormularioForm)
                    {
                        mainForm = (FormularioForm)form;
                        break;
                    }
                }

                if (mainForm != null)
                {
                    // Establecer el archivo en el formulario principal
                    mainForm.EstablecerArchivoSeleccionado(filePath);

                    MessageBox.Show(
                        $"El documento escaneado '{Path.GetFileName(filePath)}' ha sido cargado en el formulario.\n" +
                        "Complete los metadatos necesarios y presione 'Enviar' para subirlo.",
                        "Documento escaneado",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    // Si no hay formulario activo, usar la clase Scanner para subir directamente
                    await ScannerService.UploadToPortalAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al subir el documento escaneado: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}