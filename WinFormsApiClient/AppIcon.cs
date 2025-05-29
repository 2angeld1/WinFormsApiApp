using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace WinFormsApiClient
{
    /// <summary>
    /// Clase estática para manejar el icono global de la aplicación
    /// </summary>
    public static class AppIcon
    {
        private static Icon defaultIcon;

        /// <summary>
        /// Icono predeterminado para todos los formularios de la aplicación
        /// </summary>
        public static Icon DefaultIcon
        {
            get
            {
                if (defaultIcon == null)
                {
                    LoadDefaultIcon();
                }
                return defaultIcon;
            }
        }

        /// <summary>
        /// Carga el icono desde el archivo ecmicon.ico
        /// </summary>
        private static void LoadDefaultIcon()
        {
            try
            {
                // Buscar el icono en varias ubicaciones posibles
                string[] possiblePaths = {
                    Path.Combine(Application.StartupPath, "images", "ecmicon.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "ecmicon.ico"),
                    "images/ecmicon.ico", // Ruta relativa
                    "ecmicon.ico" // En el directorio raíz
                };

                foreach (string path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        defaultIcon = new Icon(path);
                        Console.WriteLine($"Icono cargado desde: {path}");
                        return;
                    }
                }

                // Si no se encuentra el icono, mostrar mensaje (solo en modo debug)
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    Console.WriteLine("No se encontró el archivo ecmicon.ico en ninguna ubicación conocida.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar el icono: {ex.Message}");
                // Si hay error, usar un icono predeterminado del sistema
                defaultIcon = SystemIcons.Application;
            }
        }
    }
}