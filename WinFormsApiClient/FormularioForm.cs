using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApiClient
{
    public partial class FormularioForm : MaterialForm
    {
        private bool _cabinetesLoaded = false;
        private Label statusLabel;
        private string selectedFilePath;
        private Label fileSizeLabel;

        // Clase auxiliar para los items de ComboBox
        private class ComboboxItem
        {
            public string Text { get; set; }
            public string Value { get; set; }
            public object Tag { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        // Clase auxiliar para gestionar los iconos de archivos
        private class FileIcons
        {
            public static Image GetFileIcon(string extension)
            {
                if (string.IsNullOrEmpty(extension))
                    return null;

                extension = extension.ToLower();

                if (extension == ".pdf")
                    return CreateIconPlaceholder("PDF", Color.Red);
                else if (extension == ".jpg" || extension == ".jpeg")
                    return CreateIconPlaceholder("JPG", Color.Blue);
                else if (extension == ".png")
                    return CreateIconPlaceholder("PNG", Color.Green);
                else if (extension == ".webp")
                    return CreateIconPlaceholder("WEBP", Color.Purple);
                else
                    return CreateIconPlaceholder("?", Color.Gray);
            }

            private static Image CreateIconPlaceholder(string text, Color color)
            {
                Bitmap bmp = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(color);
                    g.DrawRectangle(Pens.White, 0, 0, 31, 31);

                    using (Font font = new Font("Arial", 8, FontStyle.Bold))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(text, font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
                    }
                }
                return bmp;
            }
        }

        public FormularioForm()
        {
            InitializeComponent();

            this.Icon = AppIcon.DefaultIcon;
            ThemeManager.ApplyLightTheme(this);

            statusLabel = new Label();
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, this.ClientSize.Height - 30);
            statusLabel.Text = "";
            statusLabel.ForeColor = Color.Gray;
            this.Controls.Add(statusLabel);

            this.Load += FormularioForm_Load;
        }

        private async void FormularioForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Verificar sesión
                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    MessageBox.Show("No hay sesión activa", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.Close();
                    return;
                }

                // Cargar gabinetes en segundo plano
                Task.Run(() =>
                {
                    try
                    {
                        var cabinetsDict = PrepararDatosGabinetes();
                        this.BeginInvoke(new Action(() =>
                        {
                            ActualizarUIConGabinetes(cabinetsDict);
                            _cabinetesLoaded = true;
                        }));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error cargando gabinetes: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en FormularioForm_Load: {ex.Message}");
            }
        }

        // Método para preparar los datos de gabinetes en segundo plano
        private Dictionary<string, Dictionary<string, object>> PrepararDatosGabinetes()
        {
            try
            {
                if (AppSession.Current.Cabinets == null || AppSession.Current.Cabinets.Count == 0)
                {
                    return null;
                }

                var cabinetsDict = new Dictionary<string, Dictionary<string, object>>();

                foreach (var cabinet in AppSession.Current.Cabinets)
                {
                    try
                    {
                        string cabinetId = cabinet.Key;
                        var jCabinet = JObject.FromObject(cabinet.Value);
                        string cabinetName = jCabinet["cabinet_name"]?.ToString() ?? "Gabinete sin nombre";

                        cabinetsDict[cabinetId] = jCabinet.ToObject<Dictionary<string, object>>();
                        cabinetsDict[cabinetId]["display_name"] = cabinetName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error procesando gabinete: {ex.Message}");
                    }
                }

                return cabinetsDict;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al preparar datos de gabinetes: {ex.Message}");
                return null;
            }
        }

        private void ActualizarUIConGabinetes(Dictionary<string, Dictionary<string, object>> cabinetsDict)
        {
            try
            {
                // Limpiar dropdowns
                dropdown1.Items.Clear();
                dropdown2.Items.Clear();
                dropdown3.Items.Clear();

                // Añadir primer elemento de selección
                dropdown1.Items.Add("Seleccione un archivador");
                dropdown1.SelectedIndex = 0;

                if (cabinetsDict != null && cabinetsDict.Count > 0)
                {
                    foreach (var cabinet in cabinetsDict)
                    {
                        string cabinetId = cabinet.Key;
                        string cabinetName = cabinet.Value["display_name"]?.ToString() ?? "Gabinete sin nombre";

                        var cabinetItem = new ComboboxItem
                        {
                            Text = cabinetName,
                            Value = cabinetId,
                            Tag = cabinet.Value
                        };

                        dropdown1.Items.Add(cabinetItem);
                    }

                    dropdown1.Tag = cabinetsDict;
                }

                _cabinetesLoaded = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al actualizar UI con gabinetes: {ex.Message}");
                MessageBox.Show($"Error al cargar los gabinetes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Dropdown1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Limpiar el dropdown de categorías y subcategorías
            dropdown2.Items.Clear();
            dropdown3.Items.Clear();

            dropdown2.Items.Add("Seleccione una categoría");
            dropdown3.Items.Add("Seleccione una subcategoría (opcional)");

            dropdown3.Enabled = false;

            if (dropdown1.SelectedIndex > 0)
            {
                dropdown2.Enabled = true;

                try
                {
                    var selectedCabinetItem = (ComboboxItem)dropdown1.SelectedItem;
                    string cabinetId = selectedCabinetItem.Value;

                    var cabinetsDict = (Dictionary<string, Dictionary<string, object>>)dropdown1.Tag;

                    if (cabinetsDict.ContainsKey(cabinetId))
                    {
                        var cabinet = cabinetsDict[cabinetId];

                        if (cabinet.ContainsKey("categories"))
                        {
                            var categoriesObj = JObject.FromObject(cabinet["categories"]);

                            foreach (var category in categoriesObj.Properties())
                            {
                                string categoryId = category.Name;
                                string categoryName = category.Value["category_name"]?.ToString() ?? "Categoría sin nombre";

                                var categoryItem = new ComboboxItem
                                {
                                    Text = categoryName,
                                    Value = categoryId
                                };

                                dropdown2.Items.Add(categoryItem);

                                if (category.Value["subcategories"] != null)
                                {
                                    categoryItem.Tag = category.Value["subcategories"].ToObject<Dictionary<string, object>>();
                                }
                            }
                        }
                    }

                    if (dropdown2.Items.Count > 0)
                    {
                        dropdown2.SelectedIndex = 0;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar categorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                dropdown2.Enabled = false;
            }
        }

        private void Dropdown2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Limpiar el dropdown de subcategorías
            dropdown3.Items.Clear();
            dropdown3.Items.Add("Seleccione una subcategoría (opcional)");
            dropdown3.SelectedIndex = 0;

            if (dropdown2.SelectedIndex > 0)
            {
                dropdown3.Enabled = true;

                try
                {
                    var selectedCategoryItem = (ComboboxItem)dropdown2.SelectedItem;
                    var subcategoriesDict = selectedCategoryItem.Tag as Dictionary<string, object>;

                    if (subcategoriesDict != null && subcategoriesDict.Count > 0)
                    {
                        foreach (var subcategory in subcategoriesDict)
                        {
                            string subcategoryId = subcategory.Key;
                            var subcategoryObj = JObject.FromObject(subcategory.Value);
                            string subcategoryName = subcategoryObj["subcategory_name"].ToString();

                            var subcategoryItem = new ComboboxItem
                            {
                                Text = subcategoryName,
                                Value = subcategoryId
                            };

                            dropdown3.Items.Add(subcategoryItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar subcategorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                dropdown3.Enabled = false;
            }
        }

        private void RemoveFileButton_Click(object sender, EventArgs e)
        {
            fileLabel.Text = "Ningún archivo seleccionado";
            fileNameLabel.Text = "";
            filePanelContainer.Visible = false;
        }

        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            try
            {
                fileDialog.Filter = "Archivos permitidos (*.pdf;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp|" +
                                    "Documentos PDF (*.pdf)|*.pdf|" +
                                    "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";
                fileDialog.Title = "Seleccionar documento o imagen";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string extension = Path.GetExtension(fileDialog.FileName).ToLower();
                    if (extension == ".pdf" || extension == ".jpg" || extension == ".jpeg" ||
                        extension == ".png" || extension == ".webp")
                    {
                        fileLabel.Text = fileDialog.FileName;
                        fileNameLabel.Text = Path.GetFileName(fileDialog.FileName);

                        fileTypeIcon.Image = FileIcons.GetFileIcon(extension);
                        filePanelContainer.Visible = true;
                    }
                    else
                    {
                        MessageBox.Show("Por favor seleccione un archivo PDF o imagen (JPG, PNG, WEBP).",
                            "Tipo de archivo no soportado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al seleccionar archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Limpiar todos los campos del formulario
                titulo.Text = string.Empty;
                desc.Text = string.Empty;

                // Resetear los comboboxes 
                dropdown1.SelectedIndex = 0;
                dropdown2.Items.Clear();
                dropdown3.Items.Clear();
                dropdown2.Items.Add("Seleccione una categoría");
                dropdown3.Items.Add("Seleccione una subcategoría (opcional)");
                dropdown2.SelectedIndex = 0;
                dropdown3.SelectedIndex = 0;
                dropdown2.Enabled = false;
                dropdown3.Enabled = false;

                // Limpiar el panel de archivo
                fileLabel.Text = "Ningún archivo seleccionado";
                fileNameLabel.Text = "";
                filePanelContainer.Visible = false;

                MessageBox.Show("Todos los campos han sido limpiados", "Limpieza completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al reiniciar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void SubmitButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Validar selecciones
                if (dropdown1.SelectedIndex <= 0 || dropdown2.SelectedIndex <= 0)
                {
                    MessageBox.Show("Por favor seleccione un archivador y una categoría", "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Verificar si se seleccionó un archivo
                if (string.IsNullOrEmpty(fileLabel.Text) || !File.Exists(fileLabel.Text))
                {
                    MessageBox.Show("Por favor seleccione un archivo para subir", "Archivo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar tipo de archivo
                string extension = Path.GetExtension(fileLabel.Text).ToLower();
                if (extension != ".pdf" && extension != ".jpg" && extension != ".jpeg" &&
                    extension != ".png" && extension != ".webp")
                {
                    MessageBox.Show("El archivo seleccionado no es un formato permitido. Por favor seleccione un archivo PDF o imagen (JPG, PNG, WEBP).",
                        "Tipo de archivo no soportado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener IDs seleccionados
                string cabinetId = ((ComboboxItem)dropdown1.SelectedItem).Value;
                string categoryId = ((ComboboxItem)dropdown2.SelectedItem).Value;
                string subcategoryId = null;
                string documentTitle = titulo.Text;
                string documentDesc = desc.Text;
                string filePath = fileLabel.Text;

                if (dropdown3.SelectedIndex > 0)
                {
                    subcategoryId = ((ComboboxItem)dropdown3.SelectedItem).Value;
                }

                // Mostrar formulario de carga durante el proceso
                using (Form loadingForm = new Form())
                {
                    loadingForm.Text = "Subiendo documento";
                    loadingForm.Size = new Size(300, 100);
                    loadingForm.StartPosition = FormStartPosition.CenterScreen;
                    loadingForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    loadingForm.MaximizeBox = false;
                    loadingForm.MinimizeBox = false;

                    Label label = new Label
                    {
                        Text = "Subiendo documento al portal...",
                        AutoSize = true,
                        Location = new Point(20, 20)
                    };
                    loadingForm.Controls.Add(label);

                    ProgressBar progressBar = new ProgressBar
                    {
                        Style = ProgressBarStyle.Marquee,
                        Location = new Point(20, 50),
                        Size = new Size(250, 20)
                    };
                    loadingForm.Controls.Add(progressBar);

                    loadingForm.Show();
                    Application.DoEvents();

                    try
                    {
                        bool success = await UploadDocumentAsync(filePath, cabinetId, categoryId, subcategoryId, documentTitle, documentDesc);
                        loadingForm.Close();

                        if (success)
                        {
                            MessageBox.Show("Documento enviado con éxito al servidor", "Operación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            ResetButton_Click(null, null);
                        }
                    }
                    catch (Exception uploadEx)
                    {
                        loadingForm.Close();
                        throw uploadEx;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al enviar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<bool> UploadDocumentAsync(string filePath, string cabinetId, string categoryId, string subcategoryId, string title, string description)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("No se puede encontrar el archivo para subir.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppSession.Current.AuthToken);

                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                        content.Add(fileContent, "documents[]", Path.GetFileName(filePath));
                        content.Add(new StringContent(cabinetId), "cabinet_id");
                        content.Add(new StringContent(categoryId), "category_id");

                        if (!string.IsNullOrEmpty(subcategoryId))
                            content.Add(new StringContent(subcategoryId), "subcategory_id");

                        if (!string.IsNullOrEmpty(title))
                            content.Add(new StringContent(title), "title");

                        if (!string.IsNullOrEmpty(description))
                            content.Add(new StringContent(description), "description");

                        HttpResponseMessage response = await client.PostAsync("https://ecm.ecmcentral.com/api/v2/documents", content);
                        string responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            return true;
                        }
                        else
                        {
                            MessageBox.Show($"Error al subir el documento: {responseContent}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al subir el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public void EstablecerArchivoSeleccionado(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            // Verificar que estemos en el hilo de UI
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(() => EstablecerArchivoSeleccionado(filePath)));
                return;
            }

            try
            {
                selectedFilePath = filePath;
                
                FileInfo fileInfo = new FileInfo(filePath);
                fileNameLabel.Text = fileInfo.Name;
                fileTypeIcon.Image = FileIcons.GetFileIcon(fileInfo.Extension.ToLower());
                
                if (fileSizeLabel != null)
                {
                    fileSizeLabel.Text = $"Tamaño: {fileInfo.Length / 1024:N0} KB";
                }
                
                filePanelContainer.Visible = true;
                
                if (titulo != null && string.IsNullOrEmpty(titulo.Text))
                {
                    titulo.Text = Path.GetFileNameWithoutExtension(fileInfo.Name);
                }

                Console.WriteLine($"Archivo establecido: {fileInfo.Name}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error estableciendo archivo: {ex.Message}");
            }
        }

        //private async void BtnScanToPortal_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        await ECMVirtualPrinter.ScanAndUploadAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error al escanear y subir documento: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}
    }
}