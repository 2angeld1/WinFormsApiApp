using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
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

        /// <summary>
        /// Establece un archivo PDF en el formulario principal
        /// </summary>
        public bool SetFileInActiveForm(FormularioForm form, string pdfPath)
        {
            WatcherLogger.LogActivity($"Estableciendo archivo en formulario: {pdfPath}");

            try
            {
                if (form.InvokeRequired)
                {
                    WatcherLogger.LogActivity("Se requiere InvokeRequired");

                    bool success = false;
                    form.Invoke(new Action(() => {
                        try
                        {
                            // Establecer el archivo PDF en el formulario
                            WatcherLogger.LogActivity("Estableciendo archivo PDF en formulario a través de Invoke");
                            form.EstablecerArchivoSeleccionado(pdfPath);
                            form.Activate();

                            // Mostrar mensaje
                            MessageBox.Show(
                                $"Se ha detectado un nuevo documento impreso:\n\n{Path.GetFileName(pdfPath)}\n\n" +
                                "Por favor, complete los metadatos y pulse 'Enviar' para subirlo al servidor.",
                                "Nuevo documento recibido",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);

                            success = true;
                        }
                        catch (Exception ex)
                        {
                            WatcherLogger.LogError("Error en Invoke al establecer archivo", ex);
                            success = false;
                        }
                    }));

                    return success;
                }
                else
                {
                    WatcherLogger.LogActivity("No se requiere InvokeRequired, procediendo directamente");

                    // Ya estamos en el thread de UI
                    form.EstablecerArchivoSeleccionado(pdfPath);
                    form.Activate();

                    // Mostrar mensaje
                    MessageBox.Show(
                        $"Se ha detectado un nuevo documento impreso:\n\n{Path.GetFileName(pdfPath)}\n\n" +
                        "Por favor, complete los metadatos y pulse 'Enviar' para subirlo al servidor.",
                        "Nuevo documento recibido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    return true;
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al establecer archivo en formulario", ex);
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