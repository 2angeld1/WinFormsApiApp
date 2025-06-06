using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;

namespace WinFormsApiClient.VirtualWatcher
{
    /// <summary>
    /// Clase responsable de la interacción con los formularios de la aplicación
    /// </summary>
    public class FormInteraction
    {
        private static FormInteraction _instance;
        public static FormInteraction Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new FormInteraction();
                return _instance;
            }
        }

        private FormInteraction()
        {
            try
            {
                Console.WriteLine("Iniciando componente FormInteraction...");
                Console.WriteLine("Componente FormInteraction inicializado correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente FormInteraction: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Muestra el formulario de documento para gestionar un nuevo PDF
        /// </summary>
        public async Task<bool> ShowDocumentForm(string pdfPath)
        {
            WatcherLogger.LogActivity($"ShowDocumentForm iniciado con archivo: {pdfPath}");
            try
            {
                // Si tenemos una sesión activa, podemos lanzar el formulario directamente
                if (!string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    WatcherLogger.LogActivity("Sesión activa confirmada, utilizando GabineteSelectorForm para subir documento");

                    // Crear y mostrar formulario para subir el documento
                    using (var selectorForm = new GabineteSelectorForm())
                    {
                        selectorForm.Text = "Seleccionar destino para documento impreso";
                        selectorForm.PreCargarTitulo(Path.GetFileNameWithoutExtension(pdfPath));

                        if (selectorForm.ShowDialog() == DialogResult.OK)
                        {
                            WatcherLogger.LogActivity("Usuario confirmó selección de gabinete, subiendo documento");

                            // El usuario ha seleccionado un destino, subir el documento
                            using (var form = new Form())
                            {
                                form.Text = "Subiendo documento";
                                form.Size = new System.Drawing.Size(300, 100);
                                form.StartPosition = FormStartPosition.CenterScreen;
                                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                                form.MaximizeBox = false;
                                form.MinimizeBox = false;

                                Label label = new Label();
                                label.Text = "Subiendo documento al portal...";
                                label.AutoSize = true;
                                label.Location = new System.Drawing.Point(20, 20);
                                form.Controls.Add(label);

                                ProgressBar progressBar = new ProgressBar();
                                progressBar.Style = ProgressBarStyle.Marquee;
                                progressBar.Location = new System.Drawing.Point(20, 50);
                                progressBar.Size = new System.Drawing.Size(250, 20);
                                form.Controls.Add(progressBar);

                                form.Show();
                                Application.DoEvents();

                                try
                                {
                                    // Subir el documento al servidor
                                    await DocumentUploader.Instance.UploadDocumentAsync(
                                        pdfPath,
                                        selectorForm.CabinetId,
                                        selectorForm.CategoryId,
                                        selectorForm.SubcategoryId,
                                        selectorForm.Title,
                                        selectorForm.Description);

                                    form.Close();

                                    MessageBox.Show(
                                        "El documento se ha subido correctamente al servidor.",
                                        "Subida exitosa",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information);

                                    return true;
                                }
                                catch (Exception ex)
                                {
                                    form.Close();

                                    MessageBox.Show(
                                        $"Error al subir el documento: {ex.Message}",
                                        "Error",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);

                                    return false;
                                }
                            }
                        }
                        else
                        {
                            WatcherLogger.LogActivity("Usuario canceló la selección de gabinete");
                            return false;
                        }
                    }
                }
                else
                {
                    WatcherLogger.LogActivity("No hay sesión activa, mostrando mensaje de error");
                    // No hay sesión activa, mostrar mensaje de error
                    MessageBox.Show(
                        "No hay una sesión activa. Por favor, inicie sesión en la aplicación ECM Central primero.",
                        "Sesión requerida",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    // Iniciar la aplicación para que el usuario inicie sesión
                    ApplicationLauncher.LaunchApplicationWithFile(pdfPath);
                    return true;
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en ShowDocumentForm", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifica si la aplicación está en ejecución y busca el formulario principal
        /// </summary>
        public bool TryGetActiveForm(out FormularioForm activeForm)
        {
            activeForm = null;

            try
            {
                // Buscamos si hay algún formulario abierto y activo
                if (Application.OpenForms.Count > 0)
                {
                    WatcherLogger.LogActivity($"Formularios abiertos: {Application.OpenForms.Count}");

                    foreach (Form form in Application.OpenForms)
                    {
                        WatcherLogger.LogActivity($"Formulario encontrado: {form.GetType().Name}");

                        if (form is FormularioForm formularioForm)
                        {
                            activeForm = formularioForm;
                            WatcherLogger.LogActivity("FormularioForm activo encontrado");
                            return true;
                        }
                    }
                }

                WatcherLogger.LogActivity("No se encontró ningún FormularioForm activo");
                return false;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al buscar formulario activo", ex);
                return false;
            }
        }

        public bool SetFileInActiveForm(FormularioForm form, string filePath)
        {
            if (form == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                WatcherLogger.LogActivity($"Intentando establecer archivo {filePath} en formulario activo");

                // Usar BeginInvoke para no bloquear el hilo actual
                form.BeginInvoke((Action)(() => {
                    try
                    {
                        // Llamar al método de establecer archivo en el formulario
                        form.EstablecerArchivoSeleccionado(filePath);

                        // Mostrar un mensaje al usuario
                        MessageBox.Show(
                            $"Se ha cargado automáticamente el documento:\n{Path.GetFileName(filePath)}\n\n" +
                            "Complete los datos necesarios y pulse 'Enviar' para subirlo al servidor.",
                            "Documento cargado automáticamente",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);

                        WatcherLogger.LogActivity($"Archivo establecido correctamente en formulario activo");
                    }
                    catch (Exception ex)
                    {
                        WatcherLogger.LogError($"Error al establecer archivo en UI", ex);
                    }
                }));

                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en SetFileInActiveForm", ex);
                return false;
            }
        }
        /// <summary>
        /// Muestra un icono en la bandeja del sistema con un tooltip y un manejador de eventos opcional
        /// </summary>
        public static NotifyIcon ShowTrayIcon(string tooltip, EventHandler onDoubleClick = null)
        {
            try
            {
                // Crear icono en la bandeja del sistema
                NotifyIcon trayIcon = new NotifyIcon
                {
                    Icon = GetApplicationIcon(),
                    Text = tooltip,
                    Visible = true
                };

                // Configurar menú contextual
                ContextMenuStrip menu = new ContextMenuStrip();
                menu.Items.Add("Abrir aplicación", null, (s, e) => {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = Application.ExecutablePath,
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        WatcherLogger.LogError("Error al abrir aplicación desde icono", ex);
                    }
                });

                menu.Items.Add("Comprobar estado", null, (s, e) => {
                    try
                    {
                        WatcherLogger.LogSystemDiagnostic();
                        MessageBox.Show("Se ha generado un diagnóstico del sistema en los archivos de log",
                            "ECM Central",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        WatcherLogger.LogError("Error al generar diagnóstico", ex);
                    }
                });

                menu.Items.Add("-");

                menu.Items.Add("Salir", null, (s, e) => {
                    try
                    {
                        trayIcon.Visible = false;
                        trayIcon.Dispose();
                        BackgroundMonitorService.Stop();
                        Application.Exit();
                    }
                    catch (Exception ex)
                    {
                        WatcherLogger.LogError("Error al cerrar aplicación", ex);
                    }
                });

                trayIcon.ContextMenuStrip = menu;

                // Configurar doble clic
                if (onDoubleClick != null)
                    trayIcon.DoubleClick += onDoubleClick;
                else
                    trayIcon.DoubleClick += (s, e) => {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = Application.ExecutablePath,
                                UseShellExecute = true
                            });
                        }
                        catch (Exception ex)
                        {
                            WatcherLogger.LogError("Error al abrir aplicación con doble clic", ex);
                        }
                    };

                return trayIcon;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al mostrar icono en bandeja del sistema", ex);
                return null;
            }
        }

        /// <summary>
        /// Obtiene el icono de la aplicación
        /// </summary>
        private static Icon GetApplicationIcon()
        {
            try
            {
                // Intentar obtener el icono de la aplicación
                return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                // Si falla, devolver un icono predeterminado
                return SystemIcons.Application;
            }
        }
        /// <summary>
        /// Método con soporte de timeout y cancelación
        /// </summary>
        /// <returns>True si la operación fue exitosa, False en caso contrario</returns>
        public bool SetFileInActiveFormWithTimeout(FormularioForm form, string filePath, CancellationToken cancellationToken)
        {
            if (form == null || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;

            try
            {
                WatcherLogger.LogActivity($"Intentando establecer archivo {filePath} en formulario activo (con timeout)");

                // Bloquear para evitar interacciones múltiples
                var syncEvent = new ManualResetEvent(false);
                bool success = false;

                form.BeginInvoke((Action)(() => {
                    try
                    {
                        // Si el formulario está cargando gabinetes, esperar
                        if (!TryWaitForFormReady(form))
                        {
                            WatcherLogger.LogActivity("Formulario no está listo, marcando archivo para procesar después");
                            success = false;
                        }
                        else
                        {
                            // Llamar al método de establecer archivo en el formulario
                            form.EstablecerArchivoSeleccionado(filePath);
                            success = true;
                            WatcherLogger.LogActivity($"Archivo establecido correctamente en formulario activo");
                        }
                    }
                    catch (Exception ex)
                    {
                        WatcherLogger.LogError($"Error al establecer archivo en UI", ex);
                        success = false;
                    }
                    finally
                    {
                        syncEvent.Set(); // Señalizar que hemos terminado
                    }
                }));

                // Esperar a que termine la operación o se cancele
                return syncEvent.WaitOne(10000) && success; // Esperar máximo 10 segundos
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error en SetFileInActiveFormWithTimeout", ex);
                return false;
            }
        }

        // NUEVO: Método para verificar si el formulario está listo
        private bool TryWaitForFormReady(FormularioForm form)
        {
            // Verificar si el formulario tiene la propiedad _cabinetesLoaded
            var cabinetesLoadedField = form.GetType().GetField(
                "_cabinetesLoaded",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (cabinetesLoadedField != null)
            {
                // Intentar esperar a que los gabinetes estén cargados
                for (int i = 0; i < 20; i++) // Máximo 10 segundos (20 * 500ms)
                {
                    bool loaded = (bool)cabinetesLoadedField.GetValue(form);
                    if (loaded)
                        return true;

                    Thread.Sleep(500); // Esperar medio segundo
                }

                return false; // Timeout esperando que los gabinetes se carguen
            }

            // Si no podemos acceder al campo, asumimos que está listo
            return true;
        }
        private static void LogError(string message, Exception ex)
        {
            try
            {
                WatcherLogger.LogError(message, ex);
            }
            catch { /* Ignorar errores de logging */ }
        }

    }
}