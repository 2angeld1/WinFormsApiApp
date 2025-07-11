using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using static MaterialSkin.Controls.MaterialForm;

namespace WinFormsApiClient
{
    public static class ThemeManager
    {
        private static readonly MaterialSkinManager materialSkinManager;
        private static Image logoImage;
        private const string LogoFileName = "ecmlogofm.png";
        private const int LogoWidth = 40;
        private const int LogoHeight = 40;

        // Flag para controlar si se debe mostrar el logo pequeño
        public static bool ShowSmallLogo { get; set; } = false; // Cambiado a false para quitar el logo por defecto

        // Inicialización estática
        static ThemeManager()
        {
            materialSkinManager = MaterialSkinManager.Instance;
            LoadLogo();
        }

        /// <summary>
        /// Carga el logo desde el archivo
        /// </summary>
        private static void LoadLogo()
        {
            try
            {
                // Usar una imagen predeterminada si no se encuentra el logo
                CreateDefaultLogo();

                // Buscar el logo en varias ubicaciones posibles, priorizando la carpeta "images"
                string[] possiblePaths = {
            Path.Combine(Application.StartupPath, "images", LogoFileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", LogoFileName),
            Path.Combine(Environment.CurrentDirectory, "images", LogoFileName),
            Path.Combine(Application.StartupPath, LogoFileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogoFileName),
            Path.Combine(Environment.CurrentDirectory, LogoFileName)
        };

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        Console.WriteLine($"Logo encontrado en: {path}");

                        // En lugar de Image.FromFile que mantiene el archivo bloqueado
                        // usamos una copia en memoria del archivo para liberarlo inmediatamente
                        using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                        {
                            // Crear una copia en memoria
                            logoImage = new Bitmap(stream);
                        }
                        // El archivo ya no está bloqueado después de salir del bloque using

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el logo: {ex.Message}");
                // Si hay un error, asegurarse de que tengamos una imagen predeterminada
                CreateDefaultLogo();
            }
        }

        /// <summary>
        /// Crea un logo predeterminado si no se encuentra el archivo
        /// </summary>
        private static void CreateDefaultLogo()
        {
            logoImage = new Bitmap(LogoWidth, LogoHeight);
            using (Graphics g = Graphics.FromImage(logoImage))
            {
                // Dibujar un logo predeterminado
                g.Clear(Color.FromArgb(255, 253, 216, 53)); // Amarillo
                g.DrawString("ECM", new Font("Arial", 16, FontStyle.Bold), Brushes.White, new PointF(2, 8));
            }
        }

        /// <summary>
        /// Aplica el tema oscuro con los colores corporativos a un formulario
        /// </summary>
        public static void ApplyDarkTheme(MaterialForm form)
        {
            materialSkinManager.AddFormToManage(form);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            ApplyColors();

            // Aplicar logo solo si está habilitado
            if (ShowSmallLogo)
            {
                ApplyLogo(form);
            }

            form.FormStyle = FormStyles.ActionBar_None; // Eliminar el estilo ActionBar que afecta el título
            RelocateTitle(form);
        }

        /// <summary>
        /// Aplica el tema claro con los colores corporativos a un formulario
        /// </summary>
        public static void ApplyLightTheme(MaterialForm form)
        {
            materialSkinManager.AddFormToManage(form);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            ApplyColors();

            // Aplicar logo solo si está habilitado
            if (ShowSmallLogo)
            {
                ApplyLogo(form);
            }

            form.FormStyle = FormStyles.ActionBar_None; // Eliminar el estilo ActionBar que afecta el título
            RelocateTitle(form);
        }

        /// <summary>
        /// Aplica los colores corporativos al tema actual
        /// </summary>
        private static void ApplyColors()
        {
            materialSkinManager.ColorScheme = new ColorScheme(
                Primary.Yellow800, // Color predominante (amarillo)
                Primary.Yellow900, // Color oscuro (amarillo oscuro)
                Primary.Yellow700, // Color claro (amarillo claro)
                Accent.Yellow700,  // Color de acento (amarillo dorado)
                TextShade.WHITE    // Color del texto
            );
        }

        /// <summary>
        /// Reubica el título centrado
        /// </summary>
        private static void RelocateTitle(MaterialForm form)
        {
            form.Load += (sender, e) =>
            {
                Label titleLabel = new Label
                {
                    Text = form.Text,
                    ForeColor = Color.White,
                    BackColor = Color.Transparent,
                    Font = new Font("Roboto", 15, FontStyle.Regular),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleCenter, // Centrado
                    Size = new Size(form.Width - 20, 30),
                    Location = new Point(10, 26) // Centrado sin tener en cuenta logo
                };

                form.Controls.Add(titleLabel);
                titleLabel.BringToFront();

                // Cuando cambie el título del formulario, actualizar también nuestro label
                form.TextChanged += (s, args) =>
                {
                    titleLabel.Text = form.Text;
                };

                // Manejar cambio de tamaño del formulario
                form.SizeChanged += (s, args) =>
                {
                    titleLabel.Width = form.Width - 20;
                };
            };
        }

        /// <summary>
        /// Añade el logo al formulario
        /// </summary>
        private static void ApplyLogo(Form form)
        {
            // Verificar si el formulario ya tiene un PictureBox para el logo
            PictureBox logoPictureBox = GetOrCreateLogoPictureBox(form);

            // Configurar el PictureBox
            logoPictureBox.Image = logoImage;
            logoPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            logoPictureBox.Visible = true;

            form.Load += (sender, e) =>
            {
                // Posicionar el logo en la barra de título
                logoPictureBox.Location = new Point(5, 25);
                logoPictureBox.BringToFront();
            };

            // Manejar cambio de tamaño
            form.SizeChanged += (sender, e) =>
            {
                logoPictureBox.BringToFront();
            };
        }

        /// <summary>
        /// Busca o crea un PictureBox para el logo
        /// </summary>
        private static PictureBox GetOrCreateLogoPictureBox(Form form)
        {
            // Buscar si ya existe un PictureBox para el logo
            PictureBox logoPictureBox = null;
            foreach (Control control in form.Controls)
            {
                if (control is PictureBox && control.Name == "ThemeManagerLogoPictureBox")
                {
                    logoPictureBox = (PictureBox)control;
                    break;
                }
            }

            // Si no existe, crear uno nuevo
            if (logoPictureBox == null)
            {
                logoPictureBox = new PictureBox
                {
                    Name = "ThemeManagerLogoPictureBox",
                    Size = new Size(LogoWidth, LogoHeight),
                    BackColor = Color.Transparent // Fondo transparente para el logo
                };
                form.Controls.Add(logoPictureBox);
            }

            return logoPictureBox;
        }

        /// <summary>
        /// Obtiene la imagen del logo
        /// </summary>
        public static Image GetLogoImage()
        {
            return logoImage;
        }
    }
}