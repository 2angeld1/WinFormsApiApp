using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase para manejar el lanzamiento de la aplicación
    /// </summary>
    public static class ApplicationLauncher
    {
        /// <summary>
        /// Inicia el monitor de fondo silenciosamente
        /// </summary>
        public static bool StartBackgroundMonitorSilently()
        {
            try
            {
                // Verificar si ya está en ejecución
                if (BackgroundMonitorService._isRunning)
                {
                    WatcherLogger.LogActivity("Monitor de fondo ya activo");
                    return true;
                }

                // Verificar por archivos de marcador
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");

                // Si existe el archivo de marcador, verificar si el proceso sigue vivo
                if (File.Exists(markerFilePath))
                {
                    try
                    {
                        string pidContent = File.ReadAllText(markerFilePath);
                        int pid;
                        if (int.TryParse(pidContent, out pid))
                        {
                            try
                            {
                                Process process = Process.GetProcessById(pid);
                                if (!process.HasExited)
                                {
                                    // El proceso sigue en ejecución
                                    WatcherLogger.LogActivity($"Monitor ya en ejecución (PID: {pid})");
                                    return true;
                                }
                            }
                            catch (ArgumentException)
                            {
                                // El proceso ya no existe, eliminar el marcador
                                try { File.Delete(markerFilePath); } catch { }
                            }
                        }
                    }
                    catch { /* Error leyendo el archivo, ignorar */ }
                }

                // Iniciar el monitor en segundo plano
                WatcherLogger.LogActivity("Iniciando monitor en segundo plano silenciosamente");

                // Iniciar el proceso con argumentos específicos para modo silencioso
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = "/backgroundmonitor /silent",
                    UseShellExecute = false,   // No usar shell para evitar ventanas de consola
                    CreateNoWindow = true,     // No mostrar ventana de consola
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                // Iniciar el proceso
                Process monitorProcess = Process.Start(startInfo);

                // Guardar el PID en un archivo para futuras verificaciones
                if (monitorProcess != null)
                {
                    try
                    {
                        File.WriteAllText(markerFilePath, monitorProcess.Id.ToString());
                    }
                    catch { /* Ignorar errores al escribir el archivo */ }
                }

                // Esperar a que se inicialice
                System.Threading.Thread.Sleep(1500);

                WatcherLogger.LogActivity("Monitor en segundo plano iniciado silenciosamente");
                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al iniciar monitor de fondo silenciosamente", ex);
                return false;
            }
        }

        /// <summary>
        /// Lanza la aplicación ECM Central con un archivo específico
        /// </summary>
        public static void LaunchApplicationWithFile(string filePath)
        {
            try
            {
                // Verificar si ya está abierto algún formulario relevante
                foreach (Form form in Application.OpenForms)
                {
                    if (form.GetType().Name == "FormularioForm" || form.GetType().Name == "LoginForm")
                    {
                        // La aplicación ya está abierta, marcar el archivo para procesamiento
                        string markerFile = Path.Combine(
                            VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                            "pending_file.marker");

                        File.WriteAllText(markerFile, filePath);

                        // Activar la ventana existente
                        try
                        {
                            form.Invoke(new Action(() => {
                                form.WindowState = FormWindowState.Normal;
                                form.Activate();
                            }));
                        }
                        catch { /* Ignorar errores al activar la ventana */ }

                        WatcherLogger.LogActivity($"Aplicación ya abierta, marcando archivo para procesamiento: {filePath}");
                        return;
                    }
                }

                // La aplicación no está abierta, lanzarla con el argumento adecuado
                WatcherLogger.LogActivity($"Lanzando aplicación con archivo: {filePath}");

                // Evitar múltiples lanzamientos simultáneos
                string lockFile = Path.Combine(
                    VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH,
                    ".app_launch.lock");

                if (File.Exists(lockFile))
                {
                    try
                    {
                        DateTime lockTime = File.GetLastWriteTime(lockFile);
                        if ((DateTime.Now - lockTime).TotalSeconds < 10)
                        {
                            // Lanzamiento muy reciente, marcar el archivo y salir
                            File.WriteAllText(
                                Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "pending_file.marker"),
                                filePath);

                            WatcherLogger.LogActivity($"Lanzamiento reciente detectado, archivo marcado: {filePath}");
                            return;
                        }
                    }
                    catch { /* Ignorar errores leyendo el archivo */ }
                }

                // Crear/actualizar archivo de bloqueo
                try { File.WriteAllText(lockFile, DateTime.Now.ToString()); } catch { }

                // Lanzar la aplicación
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = Application.ExecutablePath,
                    Arguments = $"/print:\"{filePath}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);
                WatcherLogger.LogActivity($"Aplicación lanzada para procesar archivo: {filePath}");
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al lanzar aplicación con archivo", ex);
            }
        }
    }
}