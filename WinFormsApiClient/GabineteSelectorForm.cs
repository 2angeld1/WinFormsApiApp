using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WinFormsApiClient
{
    public partial class GabineteSelectorForm : MaterialForm
    {
        public string CabinetId { get; private set; }
        public string CategoryId { get; private set; }
        public string SubcategoryId { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }

        public GabineteSelectorForm()
        {
            InitializeComponent();
            ThemeManager.ApplyLightTheme(this);
            this.Load += new EventHandler(GabineteSelectorForm_Load);
        }

        private void GabineteSelectorForm_Load(object sender, EventArgs e)
        {
            // Cargar los gabinetes disponibles desde la sesión del usuario
            CargarGabinetes();
        }

        private void CargarGabinetes()
        {
            try
            {
                // Limpiar comboboxes
                cabinetComboBox.Items.Clear();
                categoryComboBox.Items.Clear();
                subcategoryComboBox.Items.Clear();

                // Deshabilitar comboboxes hasta que se seleccionen las opciones previas
                categoryComboBox.Enabled = false;
                subcategoryComboBox.Enabled = false;

                // Añadir primer elemento de selección
                cabinetComboBox.Items.Add("Seleccione un gabinete");
                cabinetComboBox.SelectedIndex = 0;

                if (AppSession.Current.Cabinets != null && AppSession.Current.Cabinets.Count > 0)
                {
                    // Crear diccionario para almacenar los gabinetes con sus IDs
                    var cabinetsDict = new Dictionary<string, Dictionary<string, object>>();

                    // Verificar si hay gabinetes disponibles
                    foreach (var cabinetObj in AppSession.Current.Cabinets)
                    {
                        try
                        {
                            // Convertir el objeto a JObject para trabajar con él
                            var jCabinet = Newtonsoft.Json.Linq.JObject.FromObject(cabinetObj);

                            // Extraer el ID y nombre del gabinete
                            string cabinetId = jCabinet["id"]?.ToString() ??
                                               jCabinet["cabinet_id"]?.ToString() ??
                                               Guid.NewGuid().ToString(); // ID único si no existe

                            string cabinetName = jCabinet["cabinet_name"]?.ToString() ??
                                                 jCabinet["name"]?.ToString() ??
                                                 "Gabinete sin nombre";

                            // Crear un objeto personalizado para el ComboBox
                            var cabinetItem = new ComboboxItem
                            {
                                Text = cabinetName,
                                Value = cabinetId,
                                Tag = jCabinet
                            };

                            cabinetComboBox.Items.Add(cabinetItem);

                            // Convertir a Dictionary para mantener la compatibilidad con el código existente
                            cabinetsDict[cabinetId] = jCabinet.ToObject<Dictionary<string, object>>();
                        }
                        catch (Exception ex)
                        {
                            // Registrar error pero continuar con otros gabinetes
                            System.Diagnostics.Debug.WriteLine($"Error procesando gabinete: {ex.Message}");
                        }
                    }

                    // Guardar el diccionario para usarlo en la selección
                    cabinetComboBox.Tag = cabinetsDict;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar gabinetes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Clase auxiliar para los items de ComboBox
        public class ComboboxItem
        {
            public string Text { get; set; }
            public string Value { get; set; }
            public object Tag { get; set; }  // Añadir la propiedad Tag

            public override string ToString()
            {
                return Text;
            }
        }

        private void cabinetComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            categoryComboBox.Items.Clear();
            categoryComboBox.Items.Add("Seleccione una categoría");
            categoryComboBox.SelectedIndex = 0;

            subcategoryComboBox.Items.Clear();
            subcategoryComboBox.Items.Add("Seleccione una subcategoría (opcional)");
            subcategoryComboBox.SelectedIndex = 0;

            subcategoryComboBox.Enabled = false;

            if (cabinetComboBox.SelectedIndex > 0)
            {
                categoryComboBox.Enabled = true;

                try
                {
                    // Obtener el item seleccionado
                    var selectedCabinetItem = (ComboboxItem)cabinetComboBox.SelectedItem;
                    string cabinetId = selectedCabinetItem.Value;

                    // Obtener el diccionario de gabinetes
                    var cabinetsDict = (Dictionary<string, Dictionary<string, object>>)cabinetComboBox.Tag;

                    // Obtener las categorías del gabinete seleccionado
                    if (cabinetsDict.ContainsKey(cabinetId))
                    {
                        var cabinet = cabinetsDict[cabinetId];
                        if (cabinet.ContainsKey("categories"))
                        {
                            var categoriesObj = Newtonsoft.Json.Linq.JObject.FromObject(cabinet["categories"]);
                            foreach (var category in categoriesObj.Properties())
                            {
                                string categoryId = category.Name;
                                string categoryName = category.Value["category_name"].ToString();

                                var categoryItem = new ComboboxItem
                                {
                                    Text = categoryName,
                                    Value = categoryId
                                };

                                categoryComboBox.Items.Add(categoryItem);

                                // Guardar las subcategorías para cada categoría
                                if (category.Value["subcategories"] != null)
                                {
                                    categoryItem.Tag = category.Value["subcategories"].ToObject<Dictionary<string, object>>();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar categorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                categoryComboBox.Enabled = false;
            }
        }

        private void categoryComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            subcategoryComboBox.Items.Clear();
            subcategoryComboBox.Items.Add("Seleccione una subcategoría (opcional)");
            subcategoryComboBox.SelectedIndex = 0;

            if (categoryComboBox.SelectedIndex > 0)
            {
                subcategoryComboBox.Enabled = true;

                try
                {
                    // Obtener el item seleccionado
                    var selectedCategoryItem = (ComboboxItem)categoryComboBox.SelectedItem;

                    // Obtener las subcategorías de la categoría seleccionada
                    var subcategoriesDict = selectedCategoryItem.Tag as Dictionary<string, object>;

                    if (subcategoriesDict != null && subcategoriesDict.Count > 0)
                    {
                        foreach (var subcategory in subcategoriesDict)
                        {
                            string subcategoryId = subcategory.Key;

                            // Obtener el nombre de la subcategoría desde el objeto dinámico
                            var subcategoryObj = Newtonsoft.Json.Linq.JObject.FromObject(subcategory.Value);
                            string subcategoryName = subcategoryObj["subcategory_name"].ToString();

                            var subcategoryItem = new ComboboxItem
                            {
                                Text = subcategoryName,
                                Value = subcategoryId
                            };

                            subcategoryComboBox.Items.Add(subcategoryItem);
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
                subcategoryComboBox.Enabled = false;
            }
        }

        private void confirmButton_Click(object sender, EventArgs e)
        {
            if (cabinetComboBox.SelectedIndex > 0 && categoryComboBox.SelectedIndex > 0)
            {
                // Extraer los IDs reales de los items seleccionados
                var selectedCabinet = (ComboboxItem)cabinetComboBox.SelectedItem;
                var selectedCategory = (ComboboxItem)categoryComboBox.SelectedItem;

                CabinetId = selectedCabinet.Value;
                CategoryId = selectedCategory.Value;

                if (subcategoryComboBox.SelectedIndex > 0)
                {
                    var selectedSubcategory = (ComboboxItem)subcategoryComboBox.SelectedItem;
                    SubcategoryId = selectedSubcategory.Value;
                }
                else
                {
                    SubcategoryId = null; // No se ha seleccionado ninguna subcategoría
                }

                Title = titleTextBox.Text;
                Description = descriptionTextBox.Text;

                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Por favor seleccione al menos un gabinete y una categoría.",
                    "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        // Método corregido para precargar el título
        public void PreCargarTitulo(string titulo)
        {
            if (this.titleTextBox != null)
            {
                this.titleTextBox.Text = titulo;
            }
        }
    }
}