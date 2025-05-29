using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WinFormsApiClient
{
    public partial class FormularioForm : MaterialForm
    {
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
            // Establecer el icono del formulario usando el icono global
            this.Icon = AppIcon.DefaultIcon;
            ThemeManager.ApplyLightTheme(this);
            this.Load += new EventHandler(FormularioForm_Load);
        }

        private async void FormularioForm_Load(object sender, EventArgs e)
        {
            Console.WriteLine("=== FormularioForm_Load iniciado ===");
            try
            {
                // Configurar la interfaz con el nuevo diseño
                ConfigurarInterfaz();

                // Verificar que existe una sesión activa
                Console.WriteLine($"Verificando sesión - Token: {(string.IsNullOrEmpty(AppSession.Current.AuthToken) ? "No válido" : "Válido")}");

                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    Console.WriteLine("Error: No hay token de autenticación válido");
                    MessageBox.Show("La sesión no es válida. Por favor, inicie sesión nuevamente.", "Error de sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                // Personalizar el título con el nombre del usuario
                Console.WriteLine($"Usuario actual: {AppSession.Current.UserName}");
                this.Text = $"Gestor de documentos - {AppSession.Current.UserName}";

                // Verificar licencias disponibles
                Console.WriteLine("Verificando licencias disponibles...");
                try
                {
                    // Activa el bypass de licencias para desarrollo y pruebas
                    ECMVirtualPrinter.SetLicenseBypass(true);
                    Console.WriteLine("Bypass de verificación de licencia activado para desarrollo");

                    // En entorno de producción se usaría:
                    // await ECMVirtualPrinter.CheckLicenseAsync();
                }
                catch (Exception licEx)
                {
                    Console.WriteLine($"Error al verificar licencias: {licEx.Message}");
                    Console.WriteLine(licEx.StackTrace);

                    // Si falla la verificación, activamos el bypass como respaldo
                    ECMVirtualPrinter.SetLicenseBypass(true);
                    Console.WriteLine("Bypass de licencia activado debido al error");
                }

                // Habilitar botón del scanner según la licencia
                //BtnScanToPortal.Enabled = (ECMVirtualPrinter.UserLicense & PrintProcessor.LicenseFeatures.Scan) == PrintProcessor.LicenseFeatures.Scan;
                Console.WriteLine($"Botón Scan habilitado: {BtnScanToPortal.Enabled}");

                // Cargar gabinetes disponibles
                Console.WriteLine("Iniciando carga de gabinetes...");
                CargarGabinetes();

                // Configurar eventos para los cambios de selección en dropdowns
                Console.WriteLine("Configurando eventos de dropdown...");
                dropdown1.SelectedIndexChanged += Dropdown1_SelectedIndexChanged;
                dropdown2.SelectedIndexChanged += Dropdown2_SelectedIndexChanged;

                // Deshabilitar inicialmente los dropdowns de categoría y subcategoría
                dropdown2.Enabled = false;
                dropdown3.Enabled = false;

                Console.WriteLine("=== FormularioForm_Load completado exitosamente ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ERROR CRÍTICO EN FormularioForm_Load ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al cargar el formulario principal: {ex.Message}",
                    "Error interno", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarInterfaz()
        {
            // En ConfigurarInterfaz(), añade esta configuración para el fileDialog
            fileDialog.Filter = "Archivos permitidos (*.pdf;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp|" +
                              "Documentos PDF (*.pdf)|*.pdf|" +
                              "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";
            fileDialog.Title = "Seleccionar documento o imagen";

            // Configurar el panel para mostrar archivos seleccionados
            filePanelContainer.Controls.Add(fileTypeIcon);
            filePanelContainer.Controls.Add(fileNameLabel);
            filePanelContainer.Controls.Add(RemoveFileButton);
            filePanel.Controls.Add(filePanelContainer);

            // Configuración del panel de botones - sin BtnPrintToPortal
            buttonTablePanel.Controls.Add(BtnScanToPortal, 0, 0); // Scanner en la posición superior izquierda
            buttonTablePanel.Controls.Add(SubmitButton, 0, 1);    // Enviar en la posición inferior izquierda
            buttonTablePanel.Controls.Add(ResetButton, 1, 1);     // Limpiar en la posición inferior derecha

            // El espacio superior derecho queda vacío al eliminar el botón de impresión

            // Configurar estilo del botón de selección de archivo
            SelectFileButton.BackColor = Color.FromArgb(33, 150, 243);
            SelectFileButton.ForeColor = Color.White;

            // Mostrar el panel de archivo vacío al inicio
            fileNameLabel.Text = "Seleccione un archivo PDF o imagen";
            fileTypeIcon.Image = null;
        }

        private void CargarGabinetes()
        {
            Console.WriteLine("=== CargarGabinetes iniciado ===");
            try
            {
                // Limpiar dropdowns
                dropdown1.Items.Clear();
                dropdown2.Items.Clear();
                dropdown3.Items.Clear();
                Console.WriteLine("Dropdowns limpiados");

                // Añadir primer elemento de selección
                dropdown1.Items.Add("Seleccione un archivador");
                dropdown1.SelectedIndex = 0;
                Console.WriteLine("Elemento inicial añadido a dropdown1");

                Console.WriteLine($"Estado de Cabinets: {(AppSession.Current.Cabinets == null ? "NULL" : "No NULL")}");
                if (AppSession.Current.Cabinets != null)
                {
                    Console.WriteLine($"Cantidad de gabinetes: {AppSession.Current.Cabinets.Count}");
                }

                // Esta es la sección clave que debemos ajustar
                if (AppSession.Current.Cabinets != null && AppSession.Current.Cabinets.Count > 0)
                {
                    // No necesitamos crear un diccionario adicional, ya que AppSession.Current.Cabinets ya es un diccionario
                    var cabinetsDict = new Dictionary<string, Dictionary<string, object>>();
                    Console.WriteLine("Procesando gabinetes disponibles...");

                    foreach (var cabinet in AppSession.Current.Cabinets)
                    {
                        try
                        {
                            string cabinetId = cabinet.Key;
                            Console.WriteLine($"Procesando gabinete con ID: {cabinetId}");

                            // Convertir el valor a JObject para trabajar con él
                            var jCabinet = JObject.FromObject(cabinet.Value);

                            // Extraer el nombre del gabinete
                            string cabinetName = jCabinet["cabinet_name"]?.ToString() ??
                                                "Gabinete sin nombre";

                            Console.WriteLine($"Gabinete ID: {cabinetId}, Nombre: {cabinetName}");

                            // Crear un objeto personalizado para el ComboBox
                            var cabinetItem = new ComboboxItem
                            {
                                Text = cabinetName,
                                Value = cabinetId,
                                Tag = jCabinet
                            };

                            dropdown1.Items.Add(cabinetItem);
                            Console.WriteLine("Gabinete añadido al dropdown1");

                            // Convertir a Dictionary para el uso en el resto del código
                            cabinetsDict[cabinetId] = jCabinet.ToObject<Dictionary<string, object>>();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"=== Error procesando gabinete ===");
                            Console.WriteLine($"Mensaje: {ex.Message}");
                            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                            Console.WriteLine("================================");
                        }
                    }

                    // Guardar el diccionario para usarlo más tarde
                    dropdown1.Tag = cabinetsDict;
                    Console.WriteLine($"Total de gabinetes procesados: {AppSession.Current.Cabinets.Count}, Añadidos al dropdown: {dropdown1.Items.Count - 1}");
                }
                else
                {
                    Console.WriteLine("No hay gabinetes disponibles para cargar");

                    // Opcional: Mostrar mensaje al usuario
                    Label noGabinetsLabel = new Label();
                    noGabinetsLabel.Text = "No hay gabinetes disponibles. Por favor, contacte con el administrador.";
                    noGabinetsLabel.AutoSize = true;
                    noGabinetsLabel.ForeColor = System.Drawing.Color.Red;
                    noGabinetsLabel.Location = new Point(dropdown1.Location.X, dropdown1.Location.Y + dropdown1.Height + 10);
                    this.Controls.Add(noGabinetsLabel);
                }

                Console.WriteLine("=== CargarGabinetes completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ERROR EN CARGA DE GABINETES ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=================================");

                MessageBox.Show($"Error al cargar los gabinetes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Dropdown1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("=== Dropdown1_SelectedIndexChanged iniciado ===");
            Console.WriteLine($"Índice seleccionado: {dropdown1.SelectedIndex}");

            // Limpiar el dropdown de categorías y subcategorías
            dropdown2.Items.Clear();
            dropdown3.Items.Clear();

            dropdown2.Items.Add("Seleccione una categoría");
            dropdown3.Items.Add("Seleccione una subcategoría (opcional)");

            dropdown3.Enabled = false;

            if (dropdown1.SelectedIndex > 0)
            {
                dropdown2.Enabled = true;
                Console.WriteLine("dropdown2 habilitado");

                try
                {
                    // Obtener el item seleccionado
                    var selectedCabinetItem = (ComboboxItem)dropdown1.SelectedItem;
                    string cabinetId = selectedCabinetItem.Value;
                    Console.WriteLine($"Gabinete seleccionado - ID: {cabinetId}, Nombre: {selectedCabinetItem.Text}");

                    // Obtener el diccionario de gabinetes
                    var cabinetsDict = (Dictionary<string, Dictionary<string, object>>)dropdown1.Tag;
                    Console.WriteLine($"Gabinetes disponibles en diccionario: {cabinetsDict?.Count ?? 0}");

                    // Obtener las categorías del gabinete seleccionado
                    if (cabinetsDict.ContainsKey(cabinetId))
                    {
                        Console.WriteLine("Gabinete encontrado en diccionario");
                        var cabinet = cabinetsDict[cabinetId];

                        if (cabinet.ContainsKey("categories"))
                        {
                            Console.WriteLine("Categorías encontradas en gabinete");
                            var categoriesObj = JObject.FromObject(cabinet["categories"]);
                            Console.WriteLine($"Propiedades de categorías: {string.Join(", ", categoriesObj.Properties().Select(p => p.Name))}");

                            foreach (var category in categoriesObj.Properties())
                            {
                                string categoryId = category.Name;
                                string categoryName = category.Value["category_name"]?.ToString() ?? "Categoría sin nombre";
                                Console.WriteLine($"Procesando categoría - ID: {categoryId}, Nombre: {categoryName}");

                                var categoryItem = new ComboboxItem
                                {
                                    Text = categoryName,
                                    Value = categoryId
                                };

                                dropdown2.Items.Add(categoryItem);

                                // Guardar las subcategorías para cada categoría
                                if (category.Value["subcategories"] != null)
                                {
                                    categoryItem.Tag = category.Value["subcategories"].ToObject<Dictionary<string, object>>();
                                    Console.WriteLine($"Subcategorías encontradas para categoría {categoryName}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("El gabinete no contiene categorías");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: Gabinete con ID {cabinetId} no encontrado en diccionario");
                    }

                    if (dropdown2.Items.Count > 0)
                    {
                        dropdown2.SelectedIndex = 0;
                        Console.WriteLine($"Total categorías cargadas: {dropdown2.Items.Count - 1}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=== ERROR EN CARGA DE CATEGORÍAS ===");
                    Console.WriteLine($"Mensaje: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    Console.WriteLine("===================================");

                    MessageBox.Show($"Error al cargar categorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                dropdown2.Enabled = false;
                Console.WriteLine("dropdown2 deshabilitado");
            }

            Console.WriteLine("=== Dropdown1_SelectedIndexChanged completado ===");
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
                    // Obtener el item seleccionado
                    var selectedCategoryItem = (ComboboxItem)dropdown2.SelectedItem;

                    // Obtener las subcategorías de la categoría seleccionada
                    var subcategoriesDict = selectedCategoryItem.Tag as Dictionary<string, object>;

                    if (subcategoriesDict != null && subcategoriesDict.Count > 0)
                    {
                        foreach (var subcategory in subcategoriesDict)
                        {
                            string subcategoryId = subcategory.Key;

                            // Obtener el nombre de la subcategoría desde el objeto dinámico
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

        // Método para remover el archivo seleccionado
        private void RemoveFileButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== RemoveFileButton_Click iniciado ===");
            fileLabel.Text = "Ningún archivo seleccionado";
            fileNameLabel.Text = "";
            filePanelContainer.Visible = false;
            Console.WriteLine("Archivo removido correctamente");
            Console.WriteLine("=== RemoveFileButton_Click completado ===");
        }

        // Lógica para seleccionar un archivo
        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== SelectFileButton_Click iniciado ===");
            try
            {
                // Configurar el diálogo para permitir solo archivos PDF, JPG, PNG y WEBP
                fileDialog.Filter = "Archivos permitidos (*.pdf;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp|" +
                                    "Documentos PDF (*.pdf)|*.pdf|" +
                                    "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";
                fileDialog.Title = "Seleccionar documento o imagen";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string extension = Path.GetExtension(fileDialog.FileName).ToLower();
                    // Verificar que sea un tipo de archivo permitido
                    if (extension == ".pdf" || extension == ".jpg" || extension == ".jpeg" ||
                        extension == ".png" || extension == ".webp")
                    {
                        fileLabel.Text = fileDialog.FileName;
                        fileNameLabel.Text = Path.GetFileName(fileDialog.FileName);

                        // Mostrar icono según tipo de archivo
                        fileTypeIcon.Image = FileIcons.GetFileIcon(extension);

                        filePanelContainer.Visible = true;
                        Console.WriteLine($"Archivo seleccionado: {fileDialog.FileName}");
                    }
                    else
                    {
                        MessageBox.Show("Por favor seleccione un archivo PDF o imagen (JPG, PNG, WEBP).",
                            "Tipo de archivo no soportado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Console.WriteLine($"Tipo de archivo no soportado: {extension}");
                    }
                }
                else
                {
                    Console.WriteLine("Selección de archivo cancelada por el usuario");
                }
                Console.WriteLine("=== SelectFileButton_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN SelectFileButton_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al seleccionar archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== ResetButton_Click iniciado ===");
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

                Console.WriteLine("Todos los campos han sido limpiados");
                MessageBox.Show("Todos los campos han sido limpiados", "Limpieza completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine("=== ResetButton_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN ResetButton_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al reiniciar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Lógica para el botón de enviar
        private async void SubmitButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== SubmitButton_Click iniciado ===");
            try
            {
                // Validar selecciones
                Console.WriteLine($"Validando selecciones - dropdown1.SelectedIndex: {dropdown1.SelectedIndex}, dropdown2.SelectedIndex: {dropdown2.SelectedIndex}");
                if (dropdown1.SelectedIndex <= 0 || dropdown2.SelectedIndex <= 0)
                {
                    Console.WriteLine("Validación fallida: No se seleccionaron archivador y/o categoría");
                    MessageBox.Show("Por favor seleccione un archivador y una categoría", "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Verificar si se seleccionó un archivo
                if (string.IsNullOrEmpty(fileLabel.Text) || !File.Exists(fileLabel.Text))
                {
                    Console.WriteLine("Validación fallida: No se seleccionó un archivo válido");
                    MessageBox.Show("Por favor seleccione un archivo para subir", "Archivo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar tipo de archivo
                string extension = Path.GetExtension(fileLabel.Text).ToLower();
                if (extension != ".pdf" && extension != ".jpg" && extension != ".jpeg" &&
                    extension != ".png" && extension != ".webp")
                {
                    Console.WriteLine($"Validación fallida: Tipo de archivo no permitido: {extension}");
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

                Console.WriteLine($"Gabinete seleccionado: ID={cabinetId}, Nombre={((ComboboxItem)dropdown1.SelectedItem).Text}");
                Console.WriteLine($"Categoría seleccionada: ID={categoryId}, Nombre={((ComboboxItem)dropdown2.SelectedItem).Text}");
                Console.WriteLine($"Título: {documentTitle}");
                Console.WriteLine($"Descripción: {documentDesc}");
                Console.WriteLine($"Archivo: {filePath}");

                if (dropdown3.SelectedIndex > 0)
                {
                    subcategoryId = ((ComboboxItem)dropdown3.SelectedItem).Value;
                    Console.WriteLine($"Subcategoría seleccionada: ID={subcategoryId}, Nombre={((ComboboxItem)dropdown3.SelectedItem).Text}");
                }
                else
                {
                    Console.WriteLine("No se seleccionó subcategoría");
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

                    // Mostrar el form de carga
                    loadingForm.Show();
                    Application.DoEvents();

                    try
                    {
                        // Realizar el envío real del documento
                        bool success = await UploadDocumentAsync(filePath, cabinetId, categoryId, subcategoryId, documentTitle, documentDesc);

                        // Cerrar el formulario de carga
                        loadingForm.Close();

                        if (success)
                        {
                            MessageBox.Show("Documento enviado con éxito al servidor", "Operación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // Limpiar el formulario después de un envío exitoso
                            ResetButton_Click(null, null);
                        }
                        // Si no fue exitoso, el método UploadDocumentAsync ya habrá mostrado un mensaje de error
                    }
                    catch (Exception uploadEx)
                    {
                        // Cerrar el formulario de carga en caso de error
                        loadingForm.Close();
                        throw uploadEx; // Relanzar la excepción para que sea manejada en el catch exterior
                    }
                }

                Console.WriteLine("=== SubmitButton_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN SubmitButton_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al enviar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para subir realmente el documento al portal
        private async Task<bool> UploadDocumentAsync(string filePath, string cabinetId, string categoryId, string subcategoryId, string title, string description)
        {
            Console.WriteLine("=== UploadDocumentAsync iniciado ===");
            try
            {
                // Verificar que existe el archivo
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("No se puede encontrar el archivo para subir.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                using (HttpClient client = new HttpClient())
                {
                    // Añadir el token de autenticación al encabezado
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppSession.Current.AuthToken);

                    // Crear el contenido multipart para enviar el archivo y los metadatos
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        // Leer el archivo
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                        // Agregar el archivo al contenido multipart
                        content.Add(fileContent, "documents[]", Path.GetFileName(filePath));

                        // Agregar metadatos
                        content.Add(new StringContent(cabinetId), "cabinet_id");
                        content.Add(new StringContent(categoryId), "category_id");

                        if (!string.IsNullOrEmpty(subcategoryId))
                            content.Add(new StringContent(subcategoryId), "subcategory_id");

                        if (!string.IsNullOrEmpty(title))
                            content.Add(new StringContent(title), "title");

                        if (!string.IsNullOrEmpty(description))
                            content.Add(new StringContent(description), "description");

                        // Enviar la solicitud
                        Console.WriteLine("Enviando solicitud a la API...");
                        HttpResponseMessage response = await client.PostAsync("https://ecm.ecmcentral.com/api/v2/documents", content);

                        // Leer la respuesta
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Respuesta recibida: {responseContent}");

                        // Verificar si la solicitud fue exitosa
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Documento subido con éxito");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"Error al subir el documento: {response.StatusCode}");
                            MessageBox.Show($"Error al subir el documento: {responseContent}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN UploadDocumentAsync ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("======================================");

                MessageBox.Show($"Error al subir el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private async void BtnScanToPortal_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== BtnScanToPortal_Click iniciado ===");
            try
            {
                Console.WriteLine("Llamando a ECMVirtualPrinter.ScanAndUploadAsync()");
                // Usar la función mejorada con verificación de licencia e instalación automática
                //await ECMVirtualPrinter.ScanAndUploadAsync();
                Console.WriteLine("=== BtnScanToPortal_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN BtnScanToPortal_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al escanear y subir documento: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnPrintToPortal_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== BtnPrintToPortal_Click iniciado ===");
            try
            {
                // En lugar de usar una ruta fija, permitimos al usuario seleccionar el archivo
                string filePath = string.Empty;

                Console.WriteLine("Verificando si hay un archivo seleccionado en el formulario");
                // Si ya hay un archivo seleccionado en el formulario, usarlo
                if (!string.IsNullOrEmpty(fileLabel.Text) && File.Exists(fileLabel.Text))
                {
                    filePath = fileLabel.Text;
                    Console.WriteLine($"Usando archivo seleccionado: {filePath}");
                }
                else
                {
                    // Si no hay archivo seleccionado, pedir al usuario que seleccione uno
                    Console.WriteLine("No hay archivo seleccionado en el formulario o la ruta no existe");

                    using (OpenFileDialog printDialog = new OpenFileDialog())
                    {
                        printDialog.Filter = "Documentos (*.pdf)|*.pdf|Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos los archivos (*.*)|*.*";
                        printDialog.Title = "Seleccionar documento para imprimir";

                        if (printDialog.ShowDialog() == DialogResult.OK)
                        {
                            filePath = printDialog.FileName;
                            Console.WriteLine($"Archivo seleccionado para impresión: {filePath}");

                            // También lo establecemos en el formulario
                            EstablecerArchivoSeleccionado(filePath);
                        }
                        else
                        {
                            Console.WriteLine("Selección de archivo cancelada por el usuario");
                            return; // Usuario canceló, salir del método
                        }
                    }
                }

                // Verificar la extensión del archivo
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".pdf")
                {
                    // Configurar Microsoft Edge como aplicación predeterminada para PDFs
                    Console.WriteLine("Configurando Microsoft Edge como aplicación predeterminada para PDFs");
                    try
                    {
                        // Establecer Microsoft Edge como programa predeterminado para PDFs
                        SetMicrosoftEdgeAsDefaultPDFReader();
                    }
                    catch (Exception edgeEx)
                    {
                        Console.WriteLine($"Error al configurar Edge como predeterminado: {edgeEx.Message}");
                        // No interrumpimos el flujo si falla la configuración
                    }
                }

                // Verificar si la impresora virtual está instalada
                if (!ECMVirtualPrinter.IsPrinterInstalled())
                {
                    DialogResult result = MessageBox.Show(
                        "La impresora virtual ECM Central no está instalada. ¿Desea instalarla ahora?",
                        "Impresora no encontrada",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Console.WriteLine("Iniciando instalación de impresora virtual");

                        // Mostrar un indicador de progreso
                        using (Form waitForm = new Form())
                        {
                            waitForm.Text = "Instalando impresora";
                            waitForm.Size = new Size(300, 100);
                            waitForm.StartPosition = FormStartPosition.CenterScreen;
                            waitForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                            waitForm.MaximizeBox = false;
                            waitForm.MinimizeBox = false;

                            Label label = new Label
                            {
                                Text = "Instalando impresora virtual ECM Central...",
                                AutoSize = true,
                                Location = new Point(20, 20)
                            };
                            waitForm.Controls.Add(label);

                            ProgressBar progressBar = new ProgressBar
                            {
                                Style = ProgressBarStyle.Marquee,
                                Location = new Point(20, 50),
                                Size = new Size(250, 20)
                            };
                            waitForm.Controls.Add(progressBar);

                            // Mostrar el formulario y comenzar la instalación
                            waitForm.Show();
                            Application.DoEvents();

                            try
                            {
                                // Instalar la impresora
                                bool installed = await ECMVirtualPrinter.InstallPrinterAsync();
                                waitForm.Close();

                                if (!installed)
                                {
                                    MessageBox.Show(
                                        "No se pudo instalar la impresora virtual. Por favor, inténtelo de nuevo o contacte con soporte técnico.",
                                        "Error de instalación",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            catch (Exception installEx)
                            {
                                waitForm.Close();
                                throw new Exception("Error al instalar la impresora: " + installEx.Message, installEx);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Usuario canceló la instalación de la impresora");
                        return; // Usuario no quiere instalar, salir del método
                    }
                }

                try
                {
                    // Modificación: Usar método directo de impresión sin mostrar mensajes de error
                    Console.WriteLine("Llamando a ECMVirtualPrinter.PrintDocumentAsync()");

                    // Mostrar solo un mensaje para guiar al usuario en el proceso de impresión
                    DialogResult msgResult = MessageBox.Show(
                        "Se abrirá el documento para imprimir.\n\n" +
                        "Por favor, asegúrese de seleccionar la impresora 'ECM Central printer' en el diálogo de impresión que aparecerá.\n\n" +
                        "¿Desea continuar?",
                        "Imprimir a ECM Central",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (msgResult == DialogResult.Yes)
                    {
                        // Llamar al método del ECMVirtualPrinter pero capturando excepciones específicas
                        try
                        {
                            await ECMVirtualPrinter.PrintDocumentAsync(filePath);
                        }
                        catch (System.ComponentModel.Win32Exception win32Ex)
                        {
                            // Esta es la excepción que aparece y queremos ignorar
                            Console.WriteLine($"Se produjo una excepción Win32Exception, pero la ignoramos: {win32Ex.Message}");

                            // Esperar un momento antes de continuar, puede ser que la impresión esté en proceso
                            await Task.Delay(2000);

                            // Preguntar al usuario si la impresión se completó correctamente
                            DialogResult printResult = MessageBox.Show(
                                "¿Se ha completado correctamente la impresión del documento?",
                                "Confirmar impresión",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (printResult == DialogResult.Yes)
                            {
                                // El proceso fue exitoso, buscar el archivo PDF generado
                                string outputFolder = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                    "ECM Central");

                                if (Directory.Exists(outputFolder))
                                {
                                    // Buscar el archivo PDF más reciente
                                    var recentFiles = Directory.GetFiles(outputFolder, "*.pdf")
                                        .Select(f => new FileInfo(f))
                                        .OrderByDescending(f => f.CreationTime)
                                        .ToList();

                                    if (recentFiles.Count > 0)
                                    {
                                        // Establecer el archivo recién creado en el formulario
                                        EstablecerArchivoSeleccionado(recentFiles[0].FullName);
                                    }
                                }
                            }
                            // No mostrar mensaje de error, seguir con la ejecución normal
                        }
                    }
                }
                catch (Exception printEx) when (!(printEx is System.ComponentModel.Win32Exception))
                {
                    // Capturar otros tipos de excepciones, pero NO las Win32Exception
                    Console.WriteLine($"Error en el proceso de impresión: {printEx.Message}");
                    throw; // Re-lanzar la excepción para que sea manejada por el catch exterior
                }

                Console.WriteLine("=== BtnPrintToPortal_Click completado ===");
            }
            catch (Exception ex)
            {
                // Verificar si es la excepción específica que queremos ignorar
                if (ex is System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine($"Ignorando excepción Win32Exception: {ex.Message}");
                    Console.WriteLine("=== BtnPrintToPortal_Click completado (con excepción ignorada) ===");
                }
                else
                {
                    Console.WriteLine($"=== ERROR EN BtnPrintToPortal_Click ===");
                    Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                    Console.WriteLine($"Mensaje: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }
                    Console.WriteLine("========================================");

                    // Solo mostrar mensaje para excepciones que no sean Win32Exception
                    MessageBox.Show(
                        $"Error al imprimir documento: {ex.Message}\n\n" +
                        "Para utilizar esta función, asegúrese de tener:\n" +
                        "1. La impresora virtual ECM Central instalada\n" +
                        "2. Microsoft Edge como visor de PDF predeterminado",
                        "Error de impresión",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Configura Microsoft Edge como el lector de PDF predeterminado a nivel del sistema
        /// </summary>
        private void SetMicrosoftEdgeAsDefaultPDFReader()
        {
            try
            {
                // Ruta a Microsoft Edge (en instalaciones normales)
                string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

                // Verificar si existe la ruta estándar, sino, buscar alternativas
                if (!File.Exists(edgePath))
                {
                    // Ruta alternativa
                    edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";

                    // Si sigue sin existir, intentar encontrarlo
                    if (!File.Exists(edgePath))
                    {
                        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                        // Buscar en ubicaciones posibles
                        string[] possiblePaths = {
                    Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
                    Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe")
                };

                        foreach (string path in possiblePaths)
                        {
                            if (File.Exists(path))
                            {
                                edgePath = path;
                                break;
                            }
                        }

                        if (!File.Exists(edgePath))
                        {
                            throw new FileNotFoundException("No se pudo encontrar la ruta a Microsoft Edge");
                        }
                    }
                }

                // Usar el Registro de Windows para establecer la asociación de archivo PDF a Microsoft Edge
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\.pdf"))
                {
                    key.SetValue("", "MSEdgePDF");
                }

                // Crear la clave para la aplicación
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\MSEdgePDF\shell\open\command"))
                {
                    key.SetValue("", $"\"{edgePath}\" --single-argument %1");
                }

                // Notificar el cambio al sistema (no es necesario un reinicio)
                Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

                Console.WriteLine("Microsoft Edge ha sido configurado como el lector de PDF predeterminado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar Edge como predeterminado: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imprime un documento PDF utilizando Microsoft Edge directamente
        /// </summary>
        private async Task<bool> PrintWithEdge(string filePath)
        {
            try
            {
                // Verificar que el archivo existe
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("El archivo PDF no existe", filePath);
                }

                // Ruta a Microsoft Edge
                string edgePath = FindMicrosoftEdgePath();

                // Si no se encuentra Edge, usar el método estándar
                if (string.IsNullOrEmpty(edgePath))
                {
                    return false;
                }

                // Mostrar mensaje informativo
                MessageBox.Show(
                    $"Se abrirá Microsoft Edge para imprimir el archivo PDF.\n" +
                    $"Por favor, seleccione la impresora 'ECM Central printer' en el diálogo de impresión que aparecerá.",
                    "Imprimir con Microsoft Edge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Iniciar Edge con el argumento de impresión
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = edgePath,
                    Arguments = $"\"{filePath}\" --print",
                    UseShellExecute = true
                };

                // Iniciar el proceso
                using (Process process = Process.Start(startInfo))
                {
                    // Edge se inicia de forma asíncrona, esperamos a que el usuario confirme
                    // No podemos saber cuándo termina la impresión, así que mostramos un diálogo
                    DialogResult result = MessageBox.Show(
                        "¿Ha completado la impresión con Microsoft Edge?\n\n" +
                        "Presione 'Sí' cuando haya terminado de imprimir el documento.",
                        "Confirmación de impresión",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Intentar cerrar Edge si sigue abierto
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.CloseMainWindow();
                            }
                        }
                        catch
                        {
                            // Ignorar cualquier error al cerrar Edge
                        }

                        // Buscar el archivo PDF en la carpeta de salida
                        await Task.Delay(1000); // Esperar un segundo para que el archivo se guarde

                        string outputFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "ECM Central");

                        if (Directory.Exists(outputFolder))
                        {
                            // Buscar el archivo PDF más reciente
                            var files = new DirectoryInfo(outputFolder).GetFiles("*.pdf")
                                .OrderByDescending(f => f.CreationTime)
                                .Take(1)
                                .ToList();

                            if (files.Count > 0)
                            {
                                // Establecer el archivo recién creado en el formulario
                                EstablecerArchivoSeleccionado(files[0].FullName);
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir con Edge: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Busca la ruta de instalación de Microsoft Edge
        /// </summary>
        private string FindMicrosoftEdgePath()
        {
            string[] possiblePaths = {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Buscar usando las variables de entorno
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            possiblePaths = new string[] {
        Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
        Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe")
    };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Clase para interactuar con funciones nativas de Windows
        /// </summary>
        private static class Native
        {
            public const int SHCNE_ASSOCCHANGED = 0x08000000;
            public const int SHCNF_IDLIST = 0x0000;

            [System.Runtime.InteropServices.DllImport("shell32.dll")]
            public static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
        }

        // Método que debe ser agregado a la clase FormularioForm
        public void EstablecerArchivoSeleccionado(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                {
                    Console.WriteLine($"Ruta de archivo inválida: {filePath}");
                    return;
                }

                string extension = Path.GetExtension(filePath).ToLower();
                // Verificar que sea un tipo de archivo permitido
                if (extension == ".pdf" || extension == ".jpg" || extension == ".jpeg" ||
                    extension == ".png" || extension == ".webp")
                {
                    fileLabel.Text = filePath;
                    fileNameLabel.Text = Path.GetFileName(filePath);

                    // Mostrar icono según tipo de archivo
                    fileTypeIcon.Image = FileIcons.GetFileIcon(extension);

                    filePanelContainer.Visible = true;
                    Console.WriteLine($"Archivo establecido automáticamente: {filePath}");

                    // Sugerir un título basado en el nombre del archivo
                    if (string.IsNullOrEmpty(titulo.Text))
                    {
                        titulo.Text = Path.GetFileNameWithoutExtension(filePath);
                    }
                }
                else
                {
                    Console.WriteLine($"Tipo de archivo no soportado: {extension}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN EstablecerArchivoSeleccionado ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("==========================================");
            }
        }
    }
}