using System;
using System.Drawing;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json;
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

            // Aplicar el tema claro del ThemeManager
            ThemeManager.ApplyDarkTheme(this);

            // Guardar el archivo pendiente
            pendingFilePath = filePath;
            if (!string.IsNullOrEmpty(pendingFilePath))
            {
                // Guardar en un archivo temporal para recuperarlo si es necesario
                string tempFile = Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt");
                File.WriteAllText(tempFile, pendingFilePath);
                Console.WriteLine($"Archivo pendiente guardado en: {tempFile}");
            }

            this.Load += new EventHandler(LoginForm_Load);
        }

        private async void LoginForm_Load(object sender, EventArgs e)
        {
            // Verificar si hay un archivo pendiente guardado
            if (string.IsNullOrEmpty(pendingFilePath))
            {
                string tempFile = Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt");
                if (File.Exists(tempFile))
                {
                    try
                    {
                        pendingFilePath = File.ReadAllText(tempFile);
                        Console.WriteLine($"Archivo pendiente recuperado: {pendingFilePath}");

                        // Limpiar el archivo temporal
                        File.Delete(tempFile);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error al recuperar archivo pendiente: {ex.Message}");
                    }
                }
            }
            // Asegúrate de que la contraseña esté oculta por defecto
            this.passwordTextBox.Password = true;

            // Limpiar cualquier sesión anterior
            AppSession.Current.Clear();

            // Intentar instalar silenciosamente la impresora virtual
            await InstallVirtualPrinterAsync();

            // Cargar la imagen de ilustración
            LoadIllustrationImage();
            // Verificar si hay archivos pendientes
            CheckForPendingPdfFiles();
            // Configurar algunos detalles visuales adicionales
            ConfigureVisualElements();

            // Procesar archivo pendiente si hay alguno
            if (!string.IsNullOrEmpty(pendingFilePath) && File.Exists(pendingFilePath))
            {
                // Guardar el archivo pendiente para procesarlo después del login
                Console.WriteLine($"Archivo pendiente para procesar después del login: {pendingFilePath}");
            }

            // Asegurarse de que el formulario se dibuje correctamente
            this.Update();
        }

        /// <summary>
        /// Intenta instalar la impresora virtual si no está instalada
        /// </summary>
        private async Task InstallVirtualPrinterAsync()
        {
            try
            {
                // Verificar si la impresora ya está instalada
                if (!ECMVirtualPrinter.IsPrinterInstalled())
                {
                    Console.WriteLine("Impresora virtual no detectada, intentando instalar automáticamente...");

                    // Intentar instalación silenciosa con permisos de administrador
                    if (ECMVirtualPrinter.IsAdministrator())
                    {
                        await ECMVirtualPrinter.InstallPrinterAsync(true); // true = modo silencioso
                    }
                    else
                    {
                        // Si no tenemos permisos, mostrar un mensaje pequeño que no interrumpa el flujo
                        Console.WriteLine("No se tienen permisos para instalar la impresora automáticamente");

                        // Opcional: Podríamos mostrar una notificación no invasiva al usuario
                        // LinkLabel en vez de MessageBox para no interrumpir el login
                    }
                }
                else
                {
                    Console.WriteLine("Impresora virtual ya instalada y lista para usar");
                }
            }
            catch (Exception ex)
            {
                // Solo registramos el error, no bloqueamos el flujo de login
                Console.WriteLine($"Error al verificar/instalar impresora virtual: {ex.Message}");
            }
        }

        private void ConfigureVisualElements()
        {
            // Cambiar el color de fondo del panel izquierdo para no usar amarillo
            logoPanel.BackColor = Color.Transparent; // Blanco en lugar de amarillo dorado

            // Configurar el botón de inicio de sesión para que coincida con el esquema de colores
            loginButton.UseAccentColor = true;

            // Ajustar las posiciones si es necesario
            // Esto se puede ajustar para asegurar que todo quede bien ubicado
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
                    // Si no existe, usar el logo de ECM Central en su lugar o crear una imagen temporal
                    Console.WriteLine($"Imagen de ilustración no encontrada en: {illustrationPath}");
                    CreateECMCentralLogoImage();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar imagen de ilustración: {ex.Message}");
                CreateECMCentralLogoImage();
            }
        }
        // Agregar este método a LoginForm.cs
        private void CheckForPendingPdfFiles()
        {
            try
            {
                // Verificar si existe un archivo marcador
                string markerFile = Path.Combine(
                    VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                    "pending_pdf.marker");

                if (File.Exists(markerFile))
                {
                    // Leer la ruta del archivo pendiente
                    string filePath = File.ReadAllText(markerFile).Trim();

                    // Eliminar el marcador
                    try { File.Delete(markerFile); } catch { }

                    if (File.Exists(filePath))
                    {
                        // Procesar el archivo pendiente
                        WinFormsApiClient.VirtualWatcher.DocumentProcessor.Instance.ProcessNewPrintJob(filePath);

                        MessageBox.Show(
                            $"Se ha procesado un documento PDF que estaba pendiente.",
                            "Documento procesado",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar archivos pendientes: {ex.Message}");
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

                    // Logo de ECM Central (basado en el diseño que veo en la imagen)
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
                // Si falla, no hacer nada para evitar errores adicionales
            }
        }
        private async void LoginButton_Click(object sender, EventArgs e)
        {
            // Obtener las credenciales del formulario
            string email = emailTextBox.Text;
            string password = passwordTextBox.Text;

            // Validación básica
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Por favor complete todos los campos", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Guardar el email del usuario
            AppSession.Current.UserEmail = email;

            // Desactivar el botón de login para evitar múltiples intentos
            loginButton.Enabled = false;

            try
            {
                // Llamar al método de autenticación asincrónica
                var loginResponse = await LoginAsync(email, password);

                if (loginResponse != null && loginResponse.Success)
                {
                    // Login exitoso - Ignoramos la verificación TFA y vamos directamente al formulario principal

                    // Guardar el token y la información del usuario
                    if (loginResponse.TwoFactor)
                    {
                        // Si la API devuelve TFA, pero decidimos ignorarlo
                        // Mostrar un mensaje opcional para informar al usuario
                        // MessageBox.Show("Autenticación de dos factores omitida", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Como omitimos el TFA, no tenemos el token completo todavía
                        // En un entorno de producción esto debería manejarse correctamente
                        // Aquí, para demo, simplemente iremos al formulario principal
                        MessageBox.Show("Tu cuenta requiere verificación de dos factores, pero se ha omitido para esta demo",
                            "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        // Login exitoso sin verificación de dos factores
                        AppSession.Current.AuthToken = loginResponse.Data.Token;
                        AppSession.Current.UserName = loginResponse.Data.User.Name;
                        AppSession.Current.UserRole = loginResponse.Data.User.Role;
                        AppSession.Current.Cabinets = loginResponse.Data.Cabinets;
                    }

                    // Mostrar el formulario principal
                    Hide();
                    MessageBox.Show("Login exitoso!", "Bienvenido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    // Verificar si hay archivo pendiente y asegurarse de que existe
                    bool hasPendingFile = !string.IsNullOrEmpty(pendingFilePath) && File.Exists(pendingFilePath);
                    Console.WriteLine($"¿Hay archivo pendiente? {hasPendingFile}, Ruta: {pendingFilePath}");

                    // Crear el formulario principal
                    FormularioForm formulario = new FormularioForm();

                    // Si hay un archivo pendiente, configurarlo después de que el formulario se muestre
                    if (hasPendingFile)
                    {
                        // Mostrar el formulario pero mantener referencia para configurar el archivo
                        formulario.Show();

                        // Dar tiempo para que el formulario se inicialice completamente
                        await Task.Delay(500);

                        // Configurar el archivo
                        Console.WriteLine($"Estableciendo archivo pendiente en FormularioForm: {pendingFilePath}");
                        formulario.EstablecerArchivoSeleccionado(pendingFilePath);

                        // Informar al usuario
                        MessageBox.Show(
                            $"Se ha cargado automáticamente el documento impreso:\n{Path.GetFileName(pendingFilePath)}\n\n" +
                            "Complete los datos necesarios y pulse 'Enviar' para subirlo al servidor.",
                            "Documento cargado automáticamente",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        // Limpiar la referencia al archivo pendiente
                        pendingFilePath = null;
                    }
                    else
                    {
                        // Si no hay archivo pendiente, mostrar el formulario normalmente
                        formulario.ShowDialog();
                    }

                    // Cerrar el formulario de login
                    Close();
                }
                else
                {
                    // Si el login falla, mostrar el error
                    MessageBox.Show($"Error: {loginResponse?.Message ?? "Credenciales incorrectas"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    loginButton.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error en el proceso de login: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                ShowPasswordButton.IconChar = FontAwesome.Sharp.IconChar.EyeSlash;  // Ícono de "ocultar"
            }
            else
            {
                // Ocultar la contraseña
                this.passwordTextBox.Password = true;
                ShowPasswordButton.IconChar = FontAwesome.Sharp.IconChar.Eye;  // Ícono de "mostrar"
            }
        }
        private async Task<LoginResponse> LoginAsync(string email, string password)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://ecm.ecmcentral.com/api/v2/auth/login");

            // Crear el contenido en formato JSON con objeto anónimo y JsonConvert
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

                // Si la respuesta no es exitosa, capturamos el contenido del error
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();

                    // Mostrar el error en un cuadro de mensaje
                    MessageBox.Show($"Error al realizar la solicitud:\nStatus Code: {response.StatusCode}\nContenido: {errorContent}",
                        "Error de autenticación", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    // Registrar el error en la consola
                    Console.WriteLine("=== Error de autenticación ===");
                    Console.WriteLine($"Status Code: {response.StatusCode}");
                    Console.WriteLine($"Contenido: {errorContent}");
                    Console.WriteLine("===============================");

                    return null;
                }

                // Leer la respuesta del servidor
                var responseString = await response.Content.ReadAsStringAsync();

                // Registrar la respuesta exitosa en la consola
                Console.WriteLine("=== Respuesta exitosa ===");
                Console.WriteLine(responseString);
                Console.WriteLine("==========================");

                // Deserializar la respuesta JSON
                return JsonConvert.DeserializeObject<LoginResponse>(responseString);
            }
            catch (Exception ex)
            {
                // Mostrar el error en un cuadro de mensaje
                MessageBox.Show("Error al realizar la solicitud: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                // Registrar el error en la consola
                Console.WriteLine("=== Error en la solicitud ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("=============================");

                return null;
            }
        }
    }
}