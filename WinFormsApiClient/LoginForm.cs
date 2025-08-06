using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApiClient.NewVirtualPrinter;

namespace WinFormsApiClient
{
    public partial class LoginForm : MaterialForm
    {
        private string pendingFilePath;

        public LoginForm() : this(null)
        {
        }

        public LoginForm(string filePath)
        {
            InitializeComponent();

            // Establecer el icono del formulario usando el icono global
            this.Icon = AppIcon.DefaultIcon;

            // Aplicar el tema oscuro del ThemeManager
            ThemeManager.ApplyDarkTheme(this);
            this.TitleBarBackColor = Color.Black; // Si tienes acceso directo a esta propiedad
            // Solo guardar el archivo pendiente
            pendingFilePath = filePath;
        }

        private async void LoginForm_Load(object sender, EventArgs e)
        {
            try
            {
                // Asegúrate de que la contraseña esté oculta por defecto
                this.passwordTextBox.Password = true;

                // Limpiar cualquier sesión anterior
                AppSession.Current.Clear();

                // Intentar inicializar el sistema de impresión silenciosamente
                await InitializeVirtualPrinterAsync();

                // Cargar la imagen de ilustración
                LoadIllustrationImage();

                // Configurar algunos detalles visuales adicionales
                ConfigureVisualElements();

                // Asegurarse de que el formulario se dibuje correctamente
                this.Update();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en LoginForm_Load: {ex.Message}");

                // Crear log de error
                string errorLog = Path.Combine(Path.GetTempPath(), "loginform_error.txt");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error en LoginForm_Load: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

        /// <summary>
        /// Intenta inicializar el sistema de impresión virtual
        /// </summary>
        private async Task InitializeVirtualPrinterAsync()
        {
            try
            {
                Console.WriteLine("Inicializando sistema de impresión virtual...");
                bool initialized = await VirtualPrinterService.InitializeAsync();
                
                if (initialized)
                {
                    Console.WriteLine("Sistema de impresión inicializado correctamente");
                }
                else
                {
                    Console.WriteLine("No se pudo inicializar completamente el sistema de impresión");
                }
            }
            catch (Exception ex)
            {
                // Solo registramos el error, no bloqueamos el flujo de login
                Console.WriteLine($"Error al inicializar sistema de impresión: {ex.Message}");
            }
        }

        private void ConfigureVisualElements()
{
    // Fondo negro y texto blanco para el botón de inicio de sesión
    loginButton.BackColor = Color.Black;
    loginButton.ForeColor = Color.White;
    loginButton.UseAccentColor = false; // Asegúrate que no sobrescriba el color

    // Si MaterialSkin permite sombra/borde (no todos los controles lo soportan directamente)
    loginButton.FlatStyle = FlatStyle.Flat;
    loginButton.FlatAppearance.BorderColor = Color.DarkGray;
    loginButton.FlatAppearance.BorderSize = 2;

    // Para simular sombra, puedes agregar un panel debajo o manejar Paint, pero MaterialSkin no lo tiene nativo.
    // Ejemplo simple de borde: ya está arriba.

    // Otros elementos visuales...
    logoPanel.BackColor = Color.Transparent;
    welcomeLabel.Location = new Point(welcomeLabel.Location.X, welcomeLabel.Location.Y);
    subtitleLabel.Location = new Point(subtitleLabel.Location.X, welcomeLabel.Location.Y + welcomeLabel.Height + 10);
}

        private void LoadIllustrationImage()
        {
            try
            {
                string imagesFolder = Path.Combine(Application.StartupPath, "images");
                string illustrationPath = Path.Combine(imagesFolder, "illustration.png");

                // Verificar si existe la carpeta y el archivo
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                    Console.WriteLine($"Se ha creado la carpeta de imágenes: {imagesFolder}");
                }

                if (File.Exists(illustrationPath))
                {
                    // Usar el método de Stream para evitar bloqueos de archivo
                    using (FileStream stream = new FileStream(illustrationPath, FileMode.Open, FileAccess.Read))
                    {
                        logoPictureBox.Image = new Bitmap(stream);
                    }
                    Console.WriteLine($"Imagen de ilustración cargada desde: {illustrationPath}");
                }
                else
                {
                    // Usa el logo de ThemeManager si no hay imagen personalizada
                    logoPictureBox.Image = ThemeManager.GetLogoImage();
                    Console.WriteLine("Logo ECM Central aplicado desde ThemeManager.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar imagen de ilustración: {ex.Message}");
                logoPictureBox.Image = ThemeManager.GetLogoImage();
            }
        }
        
        private void CreateECMCentralLogoImage()
        {
            try
            {
                // Crear una imagen de ECM Central como reemplazo
                Bitmap logoImg = new Bitmap(300, 260);
                using (Graphics g = Graphics.FromImage(logoImg))
                {
                    // Fondo transparente
                    g.Clear(Color.Transparent);

                    // Logo de ECM Central
                    int circleDiameter = 150;
                    int circleX = (logoImg.Width - circleDiameter) / 2;
                    int circleY = (logoImg.Height - circleDiameter) / 2;

                    // Dibujar círculo gris
                    using (SolidBrush grayBrush = new SolidBrush(Color.FromArgb(100, 100, 100)))
                    {
                        g.FillEllipse(grayBrush, circleX, circleY, circleDiameter, circleDiameter);
                    }

                    // Dibujar semicírculo amarillo
                    using (SolidBrush yellowBrush = new SolidBrush(Color.FromArgb(255, 215, 0)))
                    {
                        Rectangle halfCircle = new Rectangle(circleX, circleY, circleDiameter, circleDiameter);
                        g.FillPie(yellowBrush, halfCircle, 180, 180);
                    }

                    // Dibujar texto ECM Central
                    using (Font font = new Font("Arial", 24, FontStyle.Bold))
                    {
                        // "ecm" en amarillo
                        g.DrawString("ecm", font, new SolidBrush(Color.FromArgb(255, 215, 0)),
                            circleX + circleDiameter + 10, circleY + 30);

                        // "central" en gris
                        g.DrawString("central", font, new SolidBrush(Color.FromArgb(100, 100, 100)),
                            circleX + circleDiameter + 10, circleY + 70);
                    }
                }

                logoPictureBox.Image = logoImg;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear imagen de logo: {ex.Message}");
            }
        }

        private async void LoginButton_Click(object sender, EventArgs e)
{
    string email = emailTextBox.Text;
    string password = passwordTextBox.Text;

    if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
    {
        MessageBox.Show("Por favor complete todos los campos", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return;
    }

    AppSession.Current.UserEmail = email;
    loginButton.Enabled = false;

    try
    {
        var loginResponse = await LoginAsync(email, password);

        if (loginResponse != null && loginResponse.Success)
        {
            // ---- NUEVA LÓGICA AQUÍ ----
            if (loginResponse.TwoFactor) // <--- Cambia el nombre si tu modelo usa otro (puede ser loginResponse.two_factor)
            {
                // Guardar el temp_token en sesión para usarlo luego
                AppSession.Current.TempToken = loginResponse.TempToken; // <--- Asegúrate que el modelo tenga TempToken

                // Mostrar formulario para ingresar el código de Google Authenticator
                var tfaForm = new TwoFactorForm();
                var tfaResult = tfaForm.ShowDialog();

                if (tfaResult != DialogResult.OK)
                {
                    // El usuario canceló o falló en la verificación
                    MessageBox.Show("La autenticación de dos factores falló o fue cancelada.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    loginButton.Enabled = true;
                    return;
                }
                // Si llegamos aquí, el TFA fue exitoso y los datos ya están en AppSession
            }
            else
            {
                // Si no requiere TFA, guardar los datos de sesión normalmente
                AppSession.Current.AuthToken = loginResponse.Data.Token;
                AppSession.Current.UserName = loginResponse.Data.User.Name;
                AppSession.Current.UserEmail = loginResponse.Data.User.Email;
                AppSession.Current.UserRole = loginResponse.Data.User.Role;
                AppSession.Current.Cabinets = loginResponse.Data.Cabinets;
            }

            // Ocultar el formulario de login
            this.Hide();

            var formularioForm = new FormularioForm();

            // Si hay archivo pendiente, establecerlo usando el evento Shown
            if (!string.IsNullOrEmpty(pendingFilePath) && File.Exists(pendingFilePath))
            {
                formularioForm.Shown += (s, ev) =>
                {
                    try
                    {
                        formularioForm.EstablecerArchivoSeleccionado(pendingFilePath);

                        MessageBox.Show(
                            $"Se ha cargado automáticamente el documento:\n{Path.GetFileName(pendingFilePath)}",
                            "Documento cargado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        Console.WriteLine($"Archivo establecido: {pendingFilePath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error estableciendo archivo: {ex.Message}");
                    }
                };
            }

            formularioForm.ShowDialog();
            this.Close();
        }
        else
        {
            MessageBox.Show($"Error: {loginResponse?.Message ?? "Credenciales incorrectas"}",
                "Error de inicio de sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error en login: {ex.Message}");
        MessageBox.Show($"Error al iniciar sesión: {ex.Message}",
            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
    finally
    {
        loginButton.Enabled = true;
    }
}

        // Método para alternar la visibilidad de la contraseña
        private void ShowPasswordButton_Click(object sender, EventArgs e)
        {
            // Alternar entre mostrar y ocultar la contraseña
            if (this.passwordTextBox.Password)
            {
                // Mostrar la contraseña
                this.passwordTextBox.Password = false;
                ShowPasswordButton.IconChar = FontAwesome.Sharp.IconChar.EyeSlash;
            }
            else
            {
                // Ocultar la contraseña
                this.passwordTextBox.Password = true;
                ShowPasswordButton.IconChar = FontAwesome.Sharp.IconChar.Eye;
            }
        }

        private async Task<LoginResponse> LoginAsync(string email, string password)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://ecm.ecmcentral.com/api/v2/auth/login");

            // Crear el contenido en formato JSON
            var loginData = new
            {
                email,
                password,
                api_key = "9206a88c64a877fca8d2ff2fa4e289219ab8202fdf7008f11388c9b737451cac"
            };

            var jsonContent = JsonConvert.SerializeObject(loginData);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            request.Content = content;

            try
            {
                // Enviar la solicitud al servidor
                var response = await client.SendAsync(request);

                // Si la respuesta no es exitosa, capturar el error
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    MessageBox.Show($"Error al realizar la solicitud:\nStatus Code: {response.StatusCode}\nContenido: {errorContent}",
                        "Error de autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    Console.WriteLine("=== Error de autenticación ===");
                    Console.WriteLine($"Status Code: {response.StatusCode}");
                    Console.WriteLine($"Contenido: {errorContent}");
                    Console.WriteLine("===============================");

                    return null;
                }

                // Leer la respuesta del servidor
                var responseString = await response.Content.ReadAsStringAsync();

                Console.WriteLine("=== Respuesta exitosa ===");
                Console.WriteLine(responseString);
                Console.WriteLine("==========================");

                // Deserializar la respuesta JSON
                return JsonConvert.DeserializeObject<LoginResponse>(responseString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al realizar la solicitud: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                Console.WriteLine("=== Error en la solicitud ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=============================");

                return null;
            }
        }
    }
}
