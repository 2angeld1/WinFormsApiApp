using System;
using System.IO;
using System.Threading.Tasks;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase de utilidad para manejar operaciones con archivos
    /// </summary>
    public class FileUtils
    {
        private static bool _initialized = false;

        static FileUtils()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente FileUtils...");
                    _initialized = true;
                    Console.WriteLine("Componente FileUtils inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente FileUtils: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Verifica si un archivo está listo para ser procesado (no está bloqueado)
        /// </summary>
        public static bool IsFileReady(string filePath)
        {
            try
            {
                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Crea carpeta si no existe, con manejo de errores
        /// </summary>
        public static bool EnsureFolderExists(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                    Console.WriteLine($"Carpeta creada: {folderPath}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear carpeta {folderPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Copia un archivo con manejo de errores
        /// </summary>
        public static bool CopyFile(string sourcePath, string destinationPath, bool overwrite = true)
        {
            try
            {
                if (File.Exists(sourcePath))
                {
                    // Crear directorio de destino si no existe
                    string destinationDirectory = Path.GetDirectoryName(destinationPath);
                    if (!Directory.Exists(destinationDirectory))
                    {
                        Directory.CreateDirectory(destinationDirectory);
                    }

                    File.Copy(sourcePath, destinationPath, overwrite);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al copiar archivo {sourcePath} a {destinationPath}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Elimina un archivo con manejo de errores
        /// </summary>
        public static bool DeleteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al eliminar archivo {filePath}: {ex.Message}");
                return false;
            }
        }
    }
}