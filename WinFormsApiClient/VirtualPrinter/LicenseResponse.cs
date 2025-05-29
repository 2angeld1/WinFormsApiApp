using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para deserializar la respuesta de verificación de licencia
    /// </summary>
    public class LicenseResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("features")]
        public List<string> Features { get; set; }

        static LicenseResponse()
        {
            try
            {
                Console.WriteLine("Iniciando componente LicenseResponse...");
                Console.WriteLine("Componente LicenseResponse inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente LicenseResponse: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}