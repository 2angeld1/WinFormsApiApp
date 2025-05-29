using Newtonsoft.Json;
using System.Collections.Generic;

namespace WinFormsApiClient
{
    public class AppSession
    {
        private static AppSession _current;
        public static AppSession Current => _current ?? (_current = new AppSession());

        public string AuthToken { get; set; }
        public string TempToken { get; set; }
        public string UserEmail { get; set; }
        public string UserName { get; set; }
        public string UserRole { get; set; }
        public Dictionary<string, object> Cabinets { get; set; } // Cambiado de List<object> a Dictionary<string, object>

        // Reiniciar la sesión
        public void Clear()
        {
            AuthToken = null;
            TempToken = null;
            UserEmail = null;
            UserName = null;
            UserRole = null;
            Cabinets = null;
        }
    }

    // Clases para deserializar la respuesta del login según el formato del JSON
    public class LoginResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("two_factor")]
        public bool TwoFactor { get; set; }

        [JsonProperty("temp_token")]
        public string TempToken { get; set; }

        [JsonProperty("data")]
        public LoginData Data { get; set; }

        // Propiedades para manejar la respuesta relacionada con CAPTCHA
        [JsonProperty("failed_attempts")]
        public int FailedAttempts { get; set; }

        [JsonProperty("requires_captcha")]
        public bool RequiresCaptcha { get; set; }
    }

    public class LoginData
    {
        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("user")]
        public UserInfo User { get; set; }

        [JsonProperty("cabinets")]
        public Dictionary<string, object> Cabinets { get; set; } // Cambiado de List<object> a Dictionary<string, object>

        [JsonProperty("menu")]
        public List<string> Menu { get; set; }

        [JsonProperty("permissions")]
        public Dictionary<string, object> Permissions { get; set; }
    }

    public class UserInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("role")]
        public string Role { get; set; }
    }

    // Clases para los gabinetes y categorías
    public class Cabinet
    {
        public string Id { get; set; }
        public string CabinetName { get; set; }
        public Dictionary<string, Category> Categories { get; set; }

        public override string ToString()
        {
            return CabinetName;
        }
    }

    public class Category
    {
        public string Id { get; set; }
        public string CategoryName { get; set; }
        public Dictionary<string, Subcategory> Subcategories { get; set; }

        public override string ToString()
        {
            return CategoryName;
        }
    }

    public class Subcategory
    {
        public string Id { get; set; }
        public string SubcategoryName { get; set; }

        public override string ToString()
        {
            return SubcategoryName;
        }
    }

    // Clase para manejar la respuesta del CAPTCHA si decides implementarlo en el futuro
    public class CaptchaResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("captcha_image")]
        public string CaptchaImage { get; set; }

        [JsonProperty("captcha_token")]
        public string CaptchaToken { get; set; }
    }
}