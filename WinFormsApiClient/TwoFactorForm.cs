using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApiClient
{
    public partial class TwoFactorForm : MaterialForm
    {
        public TwoFactorForm()
        {
            InitializeComponent();
            ThemeManager.ApplyDarkTheme(this);
        }

        private void TwoFactorForm_Load(object sender, EventArgs e)
        {
            // Mensaje que informa al usuario que debe ingresar el código de verificación
            tfaMessageLabel.Text = "Por favor, ingrese el código de verificación que se envió a su dispositivo.";
        }

        private async void verifyButton_Click(object sender, EventArgs e)
        {
            string code = tfaCodeTextBox.Text.Trim();

            if (string.IsNullOrEmpty(code))
            {
                MessageBox.Show("Por favor ingrese el código de verificación", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Deshabilitar el botón para evitar múltiples envíos
            verifyButton.Enabled = false;

            try
            {
                // Caso especial: TFA obligatorio para usuarios que no lo tienen configurado en el servidor
                if (AppSession.Current.TempToken == "TFA_REQUIRED")
                {
                    // En este caso, ya tenemos el token completo guardado en AppSession
                    // Verificamos un código fijo (o puedes implementar otra lógica)
                    if (code == "123456") // Código de ejemplo para TFA obligatorio
                    {
                        DialogResult = DialogResult.OK;
                        Close();
                        return;
                    }
                    else
                    {
                        MessageBox.Show("Código de verificación inválido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        verifyButton.Enabled = true;
                        return;
                    }
                }

                // Caso normal: Usuario con TFA configurado en el servidor
                var verifyResult = await VerifyTfaAsync(code);

                if (verifyResult != null && verifyResult.Success)
                {
                    // Guardar el token de autenticación en la sesión
                    AppSession.Current.AuthToken = verifyResult.Data.Token;
                    AppSession.Current.UserName = verifyResult.Data.User.Name;
                    AppSession.Current.UserEmail = verifyResult.Data.User.Email;
                    AppSession.Current.UserRole = verifyResult.Data.User.Role;
                    AppSession.Current.Cabinets = verifyResult.Data.Cabinets;

                    DialogResult = DialogResult.OK;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Error: {verifyResult?.Message ?? "Código de verificación inválido"}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    verifyButton.Enabled = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al verificar el código: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                verifyButton.Enabled = true;
            }
        }

        private async Task<LoginResponse> VerifyTfaAsync(string code)
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, "https://ecm.ecmcentral.com/api/v2/auth/verify-tfa");

            // Agregar el token temporal como encabezado de autorización
            request.Headers.Add("Authorization", $"Bearer {AppSession.Current.TempToken}");

            // Crear el contenido en formato JSON
            var content = new StringContent(JsonConvert.SerializeObject(new
            {
                two_factor_code = code
            }), Encoding.UTF8, "application/json");

            request.Content = content;

            try
            {
                // Enviar la solicitud al servidor
                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();

                // Leer la respuesta del servidor
                var responseString = await response.Content.ReadAsStringAsync();

                // Deserializar la respuesta JSON
                return JsonConvert.DeserializeObject<LoginResponse>(responseString);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al verificar el código: " + ex.Message);
                return null;
            }
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}