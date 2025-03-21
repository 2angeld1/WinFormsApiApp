using System;
using System.Drawing;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json;
namespace WinFormsApiClient
{
    public partial class LoginForm : MaterialForm
    {

        public LoginForm()
        {
            InitializeComponent(); // Se llama a InitializeComponent() para inicializar los controles

            this.Load += new EventHandler(LoginForm_Load);

            // Configuración del tema MaterialSkin
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.DARK; // Tema claro
            materialSkinManager.ColorScheme = new ColorScheme(Primary.Blue600, Primary.Blue900, Primary.Blue500, Accent.LightBlue200, TextShade.WHITE);
        }
        private void LoginForm_Load(object sender, EventArgs e)
        {
            // Asegúrate de que la contraseña esté oculta por defecto
            this.passwordTextBox.Password = true;
        }
        private async void LoginButton_Click(object sender, EventArgs e)
        {
            // Obtener las credenciales del formulario
            string email = emailTextBox.Text;
            string password = passwordTextBox.Text;

            // Llamar al método de autenticación asincrónica
            var loginResponse = await LoginAsync(email, password);

            if (loginResponse != null && loginResponse.Success)
            {
                Hide(); // Ocultar el formulario de login
                MessageBox.Show("Login exitoso!", "Bienvenido", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Mostrar el formulario principal
                FormularioForm formulario = new FormularioForm();
                formulario.ShowDialog();

                // Cerrar el formulario de login
                Close();
            }
            else
            {
                // Si el login falla, mostrar el error
                MessageBox.Show($"Error: {loginResponse?.Message ?? "Credenciales incorrectas"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            // Crear el contenido en formato JSON
            var content = new StringContent("{\n    \"email\": \"" + email + "\",\n    \"password\": \"" + password + "\",\n    \"api_key\": \"9206a88c64a877fca8d2ff2fa4e289219ab8202fdf7008f11388c9b737451cac\"\n}", Encoding.UTF8, "application/json");
            request.Content = content;

            try
            {
                // Enviar la solicitud al servidor
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Leer la respuesta del servidor
                var responseString = await response.Content.ReadAsStringAsync();

                // Deserializar la respuesta JSON
                return ParseLoginResponse(responseString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al realizar la solicitud: " + ex.Message);
                return null;
            }
        }

        private LoginResponse ParseLoginResponse(string responseString)
        {
            // Aquí se espera que la respuesta tenga un formato JSON similar a esto:
            // {
            //    "token": "your_token",
            //    "message": "Login successful",
            //    "success": true
            // }

            // Deserializar la respuesta para obtener el token y el mensaje
            try
            {
                var loginResponse = JsonConvert.DeserializeObject<LoginResponse>(responseString);
                return loginResponse;
            }
            catch (Exception)
            {
                MessageBox.Show("Error al procesar la respuesta del servidor.");
                return null;
            }
        }
    }
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}