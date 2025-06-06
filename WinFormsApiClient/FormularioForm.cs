using MaterialSkin;
using MaterialSkin.Controls;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Menu;

namespace WinFormsApiClient
{
    public partial class FormularioForm : MaterialForm
    {
        private static readonly object _fileSelectionLock = new object();
        private bool _isProcessingFile = false;
        private System.Windows.Forms.Timer _fileLoadWatchdog;
        private bool _cabinetesLoaded = false;
        private Queue<string> _pendingFiles = new Queue<string>();
        private System.Windows.Forms.Timer _pendingFilesTimer;
        private System.Windows.Forms.Timer _uiWatchdogTimer;
        private DateTime _lastUIActivity;

        private Label statusLabel;
        private string selectedFilePath;
        private Label fileSizeLabel;

        // Clase auxiliar para los items de ComboBox
        private class ComboboxItem
        {
            public string Text { get; set; }
            public string Value { get; set; }
            public object Tag { get; set; }

            public override string ToString()
            {
                return Text;
            }
        }

        // Clase auxiliar para gestionar los iconos de archivos
        private class FileIcons
        {
            public static Image GetFileIcon(string extension)
            {
                if (string.IsNullOrEmpty(extension))
                    return null;

                extension = extension.ToLower();

                if (extension == ".pdf")
                    return CreateIconPlaceholder("PDF", Color.Red);
                else if (extension == ".jpg" || extension == ".jpeg")
                    return CreateIconPlaceholder("JPG", Color.Blue);
                else if (extension == ".png")
                    return CreateIconPlaceholder("PNG", Color.Green);
                else if (extension == ".webp")
                    return CreateIconPlaceholder("WEBP", Color.Purple);
                else
                    return CreateIconPlaceholder("?", Color.Gray);
            }

            private static Image CreateIconPlaceholder(string text, Color color)
            {
                Bitmap bmp = new Bitmap(32, 32);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.Clear(color);
                    g.DrawRectangle(Pens.White, 0, 0, 31, 31);

                    using (Font font = new Font("Arial", 8, FontStyle.Bold))
                    using (StringFormat sf = new StringFormat())
                    {
                        sf.Alignment = StringAlignment.Center;
                        sf.LineAlignment = StringAlignment.Center;
                        g.DrawString(text, font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);
                    }
                }
                return bmp;
            }
        }

        public FormularioForm()
        {
            InitializeComponent();

            // Inicializar sistema de diagnóstico
            Diagnostics.Initialize();
            Diagnostics.LogInfo("FormularioForm inicializado");

            this.Icon = AppIcon.DefaultIcon;
            ThemeManager.ApplyLightTheme(this);

            // Crear e inicializar el control statusLabel
            statusLabel = new Label();
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(20, this.ClientSize.Height - 30);
            statusLabel.Text = "Listo";
            statusLabel.ForeColor = Color.Gray;
            this.Controls.Add(statusLabel);

            this.Load += new EventHandler(FormularioForm_Load);
            ConfigurarAntiBloqueo();
            ConfigurarAntiBloqueoEspecifico();
            ConfigurarProcesadorArchivosPendientes();

            // Registrar evento de cierre para finalizar diagnóstico
            this.FormClosing += (s, e) => Diagnostics.Shutdown();

            ConfigurarDetectorDeBloqueo();
        }

        private void ConfigurarDetectorDeBloqueo()
        {
            // Inicializar el timestamp de actividad
            _lastUIActivity = DateTime.Now;

            // Configurar el timer para verificar responsividad de la UI
            _uiWatchdogTimer = new System.Windows.Forms.Timer();
            _uiWatchdogTimer.Interval = 5000; // Verificar cada 5 segundos

            // Variable para seguimiento de la duración del periodo de inactividad
            int inactivityWarnings = 0;

            _uiWatchdogTimer.Tick += (s, e) => {
                // Si han pasado más de 20 segundos desde la última actividad (aumentado de 10 a 20)
                TimeSpan inactiveTime = DateTime.Now - _lastUIActivity;

                // Verificar si hay alguna operación de procesamiento en curso
                bool processingFiles = _isProcessingFile || _pendingFiles.Count > 0;

                // Solo considerar bloqueo si no hay operaciones en curso
                if (inactiveTime.TotalSeconds > 20 && !processingFiles)
                {
                    inactivityWarnings++;
                    Console.WriteLine($"Posible inactividad detectada ({inactivityWarnings}), intentando actualizar la interfaz");

                    // Forzar actualización de la UI
                    Application.DoEvents();

                    // Verificar si es un periodo de inactividad natural o un problema real
                    if (inactivityWarnings >= 3)
                    {
                        // Tras 3 advertencias (15 segundos adicionales), considerar un problema real
                        Console.WriteLine("ALERTA CRÍTICA: Posible congelación de la aplicación detectada");

                        // Reiniciar el contador tras mostrar la alerta crítica
                        inactivityWarnings = 0;

                        // Intentar recuperar la aplicación liberando recursos si el problema persiste
                        if (inactiveTime.TotalSeconds > 60)
                        {
                            LiberarRecursosYRecuperar();
                        }
                    }
                }
                else
                {
                    // Si hay actividad o procesamiento, reiniciar el contador de advertencias
                    if (inactivityWarnings > 0)
                    {
                        inactivityWarnings = 0;
                        Console.WriteLine("Actividad detectada, reiniciando contador de alertas");
                    }
                }
            };

            _uiWatchdogTimer.Start();

            // Actualizar timestamp en eventos de UI
            this.MouseMove += (s, e) => _lastUIActivity = DateTime.Now;
            this.KeyDown += (s, e) => _lastUIActivity = DateTime.Now;
            this.Click += (s, e) => _lastUIActivity = DateTime.Now; // Añadido evento Click

            // Añadir manejadores para los principales controles interactivos
            foreach (Control control in this.Controls)
            {
                AddActivityHandlers(control);
            }
        }

        // Método auxiliar para añadir los manejadores de eventos de actividad recursivamente
        private void AddActivityHandlers(Control control)
        {
            // Añadir manejadores para este control
            control.MouseMove += (s, e) => _lastUIActivity = DateTime.Now;
            control.Click += (s, e) => _lastUIActivity = DateTime.Now;
            control.MouseClick += (s, e) => _lastUIActivity = DateTime.Now;

            // Añadir recursivamente para todos los controles hijos
            foreach (Control child in control.Controls)
            {
                AddActivityHandlers(child);
            }
        }
        private void LiberarRecursosYRecuperar()
        {
            try
            {
                // Cancelar operaciones pendientes
                // ...

                // Liberar recursos
                GC.Collect();
                GC.WaitForPendingFinalizers();

                // Reiniciar estados de procesamiento
                UseWaitCursor = false;
                statusLabel.Text = "Recuperado de bloqueo. Por favor, intente nuevamente.";

                // Notificar al usuario
                MessageBox.Show(
                    "La aplicación se ha recuperado de un posible bloqueo.\n" +
                    "Por favor, intente la operación nuevamente.",
                    "Recuperación de bloqueo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al intentar recuperar de bloqueo: {ex.Message}");
            }
        }
        private void ConfigurarProcesadorArchivosPendientes()
        {
            try
            {
                // Inicializar la cola si no existe
                if (_pendingFiles == null)
                    _pendingFiles = new Queue<string>();

                // Configurar timer con intervalo gradual y adaptable
                _pendingFilesTimer = new System.Windows.Forms.Timer();
                _pendingFilesTimer.Interval = 500; // Comenzar con 500ms
                int consecutiveErrors = 0;

                _pendingFilesTimer.Tick += (s, e) =>
                {
                    try
                    {
                        // Verificar marcadores primero - ANTES de procesar la cola existente
                        BuscarArchivosPendientes();

                        // Si hay muchos errores consecutivos, aumentar el intervalo para dar tiempo al sistema
                        if (consecutiveErrors > 3)
                        {
                            _pendingFilesTimer.Interval = Math.Min(5000, _pendingFilesTimer.Interval * 2);
                            Diagnostics.LogWarning($"Aumentando intervalo del timer a {_pendingFilesTimer.Interval}ms debido a errores consecutivos");
                            consecutiveErrors = 0;
                        }

                        // Solo procesar archivos pendientes si los gabinetes están cargados
                        if (_cabinetesLoaded && _pendingFiles.Count > 0 && !_isProcessingFile)
                        {
                            Diagnostics.LogInfo($"Procesando archivo pendiente. Cola: {_pendingFiles.Count} archivos");
                            _isProcessingFile = true;

                            string nextFile = _pendingFiles.Dequeue();

                            // Actualizar la actividad de UI para prevenir falsos positivos de bloqueo
                            _lastUIActivity = DateTime.Now;

                            // Procesar archivo de forma segura en hilo de UI con timeout
                            try
                            {
                                if (FileExistsSafe(nextFile))
                                {
                                    // Usar BeginInvoke con un callback para manejar finalización
                                    this.BeginInvoke(new Action(() =>
                                    {
                                        try
                                        {
                                            // Activar mecanismo de protección contra bloqueos específico para carga de archivo
                                            if (_fileLoadWatchdog != null)
                                            {
                                                _fileLoadWatchdog.Stop();
                                                _fileLoadWatchdog.Start();
                                            }

                                            ProcesarArchivoSeleccionadoDirectamente(nextFile);

                                            // Proceso exitoso, reducir el intervalo a un valor más rápido
                                            if (_pendingFilesTimer.Interval > 500)
                                            {
                                                _pendingFilesTimer.Interval = 500;
                                            }

                                            consecutiveErrors = 0;
                                        }
                                        catch (Exception exInner)
                                        {
                                            consecutiveErrors++;
                                            Diagnostics.LogError($"Error al procesar archivo en BeginInvoke: {nextFile}", exInner);
                                        }
                                        finally
                                        {
                                            _isProcessingFile = false;

                                            // Desactivar watchdog de archivo específico
                                            if (_fileLoadWatchdog != null)
                                            {
                                                _fileLoadWatchdog.Stop();
                                            }
                                        }
                                    }));
                                }
                                else
                                {
                                    Diagnostics.LogWarning($"Archivo ya no existe: {nextFile}");
                                    _isProcessingFile = false;
                                }
                            }
                            catch (Exception ex)
                            {
                                consecutiveErrors++;
                                Diagnostics.LogError($"Error al procesar archivo pendiente: {nextFile}", ex);
                                _isProcessingFile = false;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        consecutiveErrors++;
                        Diagnostics.LogError("Error en el timer de archivos pendientes", ex);
                        _isProcessingFile = false;
                    }
                };

                // Iniciar el timer inmediatamente
                _pendingFilesTimer.Start();
                Diagnostics.LogInfo("Procesador de archivos pendientes configurado e iniciado");
            }
            catch (Exception ex)
            {
                Diagnostics.LogError("Error al configurar procesador de archivos pendientes", ex);
            }
        }

        private void BuscarArchivosPendientes()
        {
            try
            {
                // Lista de posibles ubicaciones donde puede estar guardada la ruta del archivo
                string[] markerPaths = {
            Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt"),
            Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "pending_pdf.marker"),
            Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "pending_file.marker"), // Añadir este que faltaba
            Path.Combine(VirtualPrinter.VirtualPrinterCore.FIXED_OUTPUT_PATH, "last_bullzip_file.txt")
        };

                foreach (string markerPath in markerPaths)
                {
                    // Verificar si el marcador existe con un timeout adecuado para evitar bloqueos
                    if (FileExistsSafe(markerPath))
                    {
                        try
                        {
                            // Leer el archivo marcador de forma segura
                            string filePath = ReadFileSafe(markerPath);

                            // Continuar solo si se obtuvo contenido válido
                            if (string.IsNullOrWhiteSpace(filePath))
                            {
                                Diagnostics.LogWarning($"Marcador vacío o inválido: {markerPath}");
                                TryDeleteFile(markerPath);
                                continue;
                            }

                            // Limpiar y normalizar la ruta
                            filePath = filePath.Trim().Replace("\"", "");

                            Diagnostics.LogInfo($"Archivo pendiente encontrado en {markerPath}: {filePath}");

                            // Verificar de forma segura si el archivo existe
                            if (FileExistsSafe(filePath))
                            {
                                // Validar si ya está en la cola para evitar duplicados
                                bool isDuplicate = false;
                                foreach (string existingPath in _pendingFiles)
                                {
                                    if (string.Equals(existingPath, filePath, StringComparison.OrdinalIgnoreCase))
                                    {
                                        isDuplicate = true;
                                        break;
                                    }
                                }

                                if (!isDuplicate)
                                {
                                    _pendingFiles.Enqueue(filePath);
                                    Diagnostics.LogInfo($"Archivo añadido a la cola: {filePath}");
                                }

                                // Borrar el marcador para evitar procesamiento repetido
                                TryDeleteFile(markerPath);
                            }
                            else
                            {
                                Diagnostics.LogWarning($"Archivo indicado en marcador no existe: {filePath}");
                                TryDeleteFile(markerPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Diagnostics.LogError($"Error al procesar marcador {markerPath}", ex);
                            // Intentar borrar el marcador problemático para evitar problemas futuros
                            TryDeleteFile(markerPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Diagnostics.LogError("Error en BuscarArchivosPendientes", ex);
            }
        }

        // Métodos auxiliares de seguridad para operaciones de archivos
        private bool FileExistsSafe(string path)
        {
            try
            {
                // Utilizar un timeout para evitar bloqueos en acceso a archivos
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                {
                    return Task.Run(() => File.Exists(path), timeoutCts.Token).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Diagnostics.LogError($"Error al verificar existencia de archivo: {path}", ex);
                return false;
            }
        }

        private string ReadFileSafe(string path)
        {
            try
            {
                // Utilizar un timeout para evitar bloqueos en lectura de archivos
                using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
                {
                    return Task.Run(() => {
                        // Usar FileShare.ReadWrite para permitir lectura incluso si está abierto por otro proceso
                        using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fileStream))
                        {
                            return reader.ReadToEnd();
                        }
                    }, timeoutCts.Token).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                Diagnostics.LogError($"Error al leer archivo: {path}", ex);
                return string.Empty;
            }
        }

        private void TryDeleteFile(string path)
        {
            try
            {
                // Intentar eliminar sin bloquear
                Task.Run(() => {
                    try { if (File.Exists(path)) File.Delete(path); }
                    catch { /* Ignorar errores de eliminación */ }
                });
            }
            catch { /* Ignorar errores en la tarea */ }
        }
        private async void FormularioForm_Load(object sender, EventArgs e)
        {
            // Watchdog timer más corto
            System.Windows.Forms.Timer loadWatchdog = new System.Windows.Forms.Timer();
            loadWatchdog.Interval = 10000; // 10 segundos
            loadWatchdog.Tick += (s, args) => {
                Diagnostics.LogWarning("ALERTA: FormularioForm_Load está tardando demasiado, forzando finalización");
                loadWatchdog.Stop();
                try
                {
                    // Configurar interfaz básica para que al menos sea usable
                    ConfigurarInterfazMinima();
                }
                catch { /* ignorar errores */ }
            };
            loadWatchdog.Start();

            try
            {
                Diagnostics.LogInfo("FormularioForm_Load iniciado");

                // Configurar la interfaz con el nuevo diseño - operación más crítica primero
                ConfigurarInterfaz();

                // Verificar sesión de forma ligera
                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    Diagnostics.LogError("Error: No hay token de autenticación válido");
                    MessageBox.Show("La sesión no es válida. Por favor, inicie sesión nuevamente.",
                        "Error de sesión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    loadWatchdog.Stop();
                    this.Close();
                    return;
                }

                // Actualizar el título con el nombre del usuario
                this.Text = $"Gestor de documentos - {AppSession.Current.UserName}";

                // Establecer bypass de licencia para desarrollo (evita bloqueos en esta etapa)
                ECMVirtualPrinter.SetLicenseBypass(true);

                // CORRECCIÓN: Cargar gabinetes de forma segura para la UI
                try
                {
                    // Preparar datos en segundo plano
                    var cabinetsDictTask = await Task.Run(() => PrepararDatosGabinetes());

                    // Actualizar UI en el hilo principal
                    if (cabinetsDictTask != null)
                    {
                        // Ahora aplicamos los datos al UI desde el hilo principal
                        ActualizarUIConGabinetes(cabinetsDictTask);
                    }
                }
                catch (Exception gabEx)
                {
                    Diagnostics.LogError($"Error al cargar gabinetes: {gabEx.Message}", gabEx);
                }

                // Configurar eventos para los dropdowns
                dropdown1.SelectedIndexChanged += Dropdown1_SelectedIndexChanged;
                dropdown2.SelectedIndexChanged += Dropdown2_SelectedIndexChanged;

                // Deshabilitar inicialmente los dropdowns dependientes
                dropdown2.Enabled = false;
                dropdown3.Enabled = false;

                // Todo ha ido bien, detener el watchdog
                loadWatchdog.Stop();
                Diagnostics.LogInfo("FormularioForm_Load completado exitosamente");
            }
            catch (Exception ex)
            {
                loadWatchdog.Stop();
                Diagnostics.LogError($"ERROR CRÍTICO en FormularioForm_Load: {ex.Message}", ex);
                MessageBox.Show($"Error al cargar el formulario: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para preparar los datos de gabinetes en segundo plano
        private Dictionary<string, Dictionary<string, object>> PrepararDatosGabinetes()
        {
            Diagnostics.LogInfo("Preparando datos de gabinetes en segundo plano");

            try
            {
                if (AppSession.Current.Cabinets == null || AppSession.Current.Cabinets.Count == 0)
                {
                    Diagnostics.LogWarning("No hay gabinetes disponibles para cargar");
                    return null;
                }

                // Crear un diccionario para los gabinetes
                var cabinetsDict = new Dictionary<string, Dictionary<string, object>>();
                Diagnostics.LogInfo($"Procesando {AppSession.Current.Cabinets.Count} gabinetes");

                foreach (var cabinet in AppSession.Current.Cabinets)
                {
                    try
                    {
                        string cabinetId = cabinet.Key;
                        Diagnostics.LogInfo($"Procesando gabinete con ID: {cabinetId}");

                        // Convertir el valor a JObject para trabajar con él
                        var jCabinet = JObject.FromObject(cabinet.Value);

                        // Extraer el nombre del gabinete
                        string cabinetName = jCabinet["cabinet_name"]?.ToString() ?? "Gabinete sin nombre";
                        Diagnostics.LogInfo($"Gabinete ID: {cabinetId}, Nombre: {cabinetName}");

                        // Guardar los datos para uso posterior
                        cabinetsDict[cabinetId] = jCabinet.ToObject<Dictionary<string, object>>();
                        cabinetsDict[cabinetId]["display_name"] = cabinetName;
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.LogError($"Error procesando gabinete: {ex.Message}", ex);
                    }
                }

                Diagnostics.LogInfo($"Datos de gabinetes preparados correctamente: {cabinetsDict.Count} gabinetes");
                return cabinetsDict;
            }
            catch (Exception ex)
            {
                Diagnostics.LogError("Error al preparar datos de gabinetes", ex);
                return null;
            }
        }

        private void ActualizarUIConGabinetes(Dictionary<string, Dictionary<string, object>> cabinetsDict)
        {
            try
            {
                Diagnostics.LogInfo("Actualizando UI con datos de gabinetes");

                // Limpiar dropdowns (ahora en el hilo UI)
                dropdown1.Items.Clear();
                dropdown2.Items.Clear();
                dropdown3.Items.Clear();

                // Añadir primer elemento de selección
                dropdown1.Items.Add("Seleccione un archivador");
                dropdown1.SelectedIndex = 0;

                if (cabinetsDict != null && cabinetsDict.Count > 0)
                {
                    foreach (var cabinet in cabinetsDict)
                    {
                        string cabinetId = cabinet.Key;
                        string cabinetName = cabinet.Value["display_name"]?.ToString() ?? "Gabinete sin nombre";

                        // Crear un objeto personalizado para el ComboBox
                        var cabinetItem = new ComboboxItem
                        {
                            Text = cabinetName,
                            Value = cabinetId,
                            Tag = cabinet.Value
                        };

                        dropdown1.Items.Add(cabinetItem);
                        Diagnostics.LogInfo($"Añadido gabinete a UI: {cabinetName}");
                    }

                    // Guardar el diccionario para usarlo más tarde
                    dropdown1.Tag = cabinetsDict;
                    Diagnostics.LogInfo($"Total de gabinetes añadidos al dropdown: {dropdown1.Items.Count - 1}");
                }
                else
                {
                    Diagnostics.LogWarning("No hay gabinetes disponibles para mostrar en la UI");

                    // Mostrar mensaje al usuario
                    Label noGabinetsLabel = new Label();
                    noGabinetsLabel.Text = "No hay gabinetes disponibles. Por favor, contacte con el administrador.";
                    noGabinetsLabel.AutoSize = true;
                    noGabinetsLabel.ForeColor = System.Drawing.Color.Red;
                    noGabinetsLabel.Location = new Point(dropdown1.Location.X, dropdown1.Location.Y + dropdown1.Height + 10);
                    this.Controls.Add(noGabinetsLabel);
                }

                // IMPORTANTE: Marcar que los gabinetes se han cargado
                _cabinetesLoaded = true;
                Diagnostics.LogInfo("Gabinetes cargados completamente - Listo para procesar archivos");

                // Si hay archivos pendientes, iniciar el timer para procesarlos
                if (_pendingFiles.Count > 0)
                {
                    Diagnostics.LogInfo($"Iniciando procesamiento de {_pendingFiles.Count} archivos pendientes");
                    _pendingFilesTimer.Start();
                }
            }
            catch (Exception ex)
            {
                Diagnostics.LogError("Error al actualizar UI con gabinetes", ex);
                MessageBox.Show($"Error al cargar los gabinetes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarAntiBloqueoEspecifico()
        {
            _fileLoadWatchdog = new System.Windows.Forms.Timer();
            _fileLoadWatchdog.Interval = 8000; // Reducir a 8 segundos para detectar antes los bloqueos
            _fileLoadWatchdog.Tick += (s, e) => {
                if (_isProcessingFile)
                {
                    Diagnostics.LogWarning("ALERTA: Posible bloqueo en carga de archivo detectado");

                    try
                    {
                        // Forzar limpieza de estado
                        _isProcessingFile = false;

                        // Registrar diagnóstico antes de intentar recuperación
                        Diagnostics.LogMemoryUsage();
                        Diagnostics.LogThreadInfo();

                        // Intentar volver a un estado consistente
                        filePanelContainer.Visible = true;
                        fileNameLabel.Text = "Se produjo un error al cargar el archivo. Intente seleccionarlo nuevamente.";
                        fileTypeIcon.Image = null;

                        // Forzar actualización de UI
                        this.SuspendLayout();
                        this.ResumeLayout(true);
                        this.Refresh();
                        Application.DoEvents();

                        // Notificar al usuario
                        MessageBox.Show(
                            "Se detectó un problema al cargar el archivo. La aplicación ha sido recuperada automáticamente.\n\n" +
                            "Se ha creado un archivo de diagnóstico en el escritorio.",
                            "Recuperación automática",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.LogError("Error durante recuperación", ex);
                    }
                    finally
                    {
                        _fileLoadWatchdog.Stop();
                    }
                }
            };

            // Iniciar el watchdog tanto para selección manual como automática
            this.BeginInvoke((Action)(() => {
                SelectFileButton.Click += (s, e) => {
                    Diagnostics.LogInfo("Iniciando watchdog por clic en SelectFileButton");
                    _fileLoadWatchdog.Start();
                };
            }));
        }        
        // Método de respaldo para asegurar UI mínima en caso de problemas
        private void ConfigurarInterfazMinima()
        {
            try
            {
                // Configuración mínima para que la UI sea usable
                filePanelContainer.Visible = false;

                // Habilitar controles esenciales
                SelectFileButton.Enabled = true;
                ResetButton.Enabled = true;

                // Mensaje al usuario
                MessageBox.Show("Se ha producido un error durante la carga del formulario. " +
                    "Algunas funciones podrían no estar disponibles.",
                    "Advertencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch { /* Ignorar errores */ }
        }
        private void ConfigurarInterfaz()
        {
            // En ConfigurarInterfaz(), añade esta configuración para el fileDialog
            fileDialog.Filter = "Archivos permitidos (*.pdf;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp|" +
                              "Documentos PDF (*.pdf)|*.pdf|" +
                              "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";
            fileDialog.Title = "Seleccionar documento o imagen";

            // Configurar el panel para mostrar archivos seleccionados
            filePanelContainer.Controls.Add(fileTypeIcon);
            filePanelContainer.Controls.Add(fileNameLabel);
            filePanelContainer.Controls.Add(RemoveFileButton);
            filePanel.Controls.Add(filePanelContainer);

            // Configuración del panel de botones - sin BtnPrintToPortal
            buttonTablePanel.Controls.Add(BtnScanToPortal, 0, 0); // Scanner en la posición superior izquierda
            buttonTablePanel.Controls.Add(SubmitButton, 0, 1);    // Enviar en la posición inferior izquierda
            buttonTablePanel.Controls.Add(ResetButton, 1, 1);     // Limpiar en la posición inferior derecha

            // El espacio superior derecho queda vacío al eliminar el botón de impresión

            // Configurar estilo del botón de selección de archivo
            SelectFileButton.BackColor = Color.FromArgb(33, 150, 243);
            SelectFileButton.ForeColor = Color.White;

            // Mostrar el panel de archivo vacío al inicio
            fileNameLabel.Text = "Seleccione un archivo PDF o imagen";
            fileTypeIcon.Image = null;
        }

        private void CargarGabinetes()
        {
            Console.WriteLine("=== CargarGabinetes iniciado ===");
            try
            {
                // Limpiar dropdowns
                dropdown1.Items.Clear();
                dropdown2.Items.Clear();
                dropdown3.Items.Clear();
                Console.WriteLine("Dropdowns limpiados");

                // Añadir primer elemento de selección
                dropdown1.Items.Add("Seleccione un archivador");
                dropdown1.SelectedIndex = 0;
                Console.WriteLine("Elemento inicial añadido a dropdown1");

                Console.WriteLine($"Estado de Cabinets: {(AppSession.Current.Cabinets == null ? "NULL" : "No NULL")}");
                if (AppSession.Current.Cabinets != null)
                {
                    Console.WriteLine($"Cantidad de gabinetes: {AppSession.Current.Cabinets.Count}");
                }

                // Esta es la sección clave que debemos ajustar
                if (AppSession.Current.Cabinets != null && AppSession.Current.Cabinets.Count > 0)
                {
                    // No necesitamos crear un diccionario adicional, ya que AppSession.Current.Cabinets ya es un diccionario
                    var cabinetsDict = new Dictionary<string, Dictionary<string, object>>();
                    Console.WriteLine("Procesando gabinetes disponibles...");

                    foreach (var cabinet in AppSession.Current.Cabinets)
                    {
                        try
                        {
                            string cabinetId = cabinet.Key;
                            Console.WriteLine($"Procesando gabinete con ID: {cabinetId}");

                            // Convertir el valor a JObject para trabajar con él
                            var jCabinet = JObject.FromObject(cabinet.Value);

                            // Extraer el nombre del gabinete
                            string cabinetName = jCabinet["cabinet_name"]?.ToString() ??
                                                "Gabinete sin nombre";

                            Console.WriteLine($"Gabinete ID: {cabinetId}, Nombre: {cabinetName}");

                            // Crear un objeto personalizado para el ComboBox
                            var cabinetItem = new ComboboxItem
                            {
                                Text = cabinetName,
                                Value = cabinetId,
                                Tag = jCabinet
                            };

                            dropdown1.Items.Add(cabinetItem);
                            Console.WriteLine("Gabinete añadido al dropdown1");

                            // Convertir a Dictionary para el uso en el resto del código
                            cabinetsDict[cabinetId] = jCabinet.ToObject<Dictionary<string, object>>();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"=== Error procesando gabinete ===");
                            Console.WriteLine($"Mensaje: {ex.Message}");
                            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                            Console.WriteLine("================================");
                        }
                    }

                    // Guardar el diccionario para usarlo más tarde
                    dropdown1.Tag = cabinetsDict;
                    Console.WriteLine($"Total de gabinetes procesados: {AppSession.Current.Cabinets.Count}, Añadidos al dropdown: {dropdown1.Items.Count - 1}");
                }
                else
                {
                    Console.WriteLine("No hay gabinetes disponibles para cargar");

                    // Opcional: Mostrar mensaje al usuario
                    Label noGabinetsLabel = new Label();
                    noGabinetsLabel.Text = "No hay gabinetes disponibles. Por favor, contacte con el administrador.";
                    noGabinetsLabel.AutoSize = true;
                    noGabinetsLabel.ForeColor = System.Drawing.Color.Red;
                    noGabinetsLabel.Location = new Point(dropdown1.Location.X, dropdown1.Location.Y + dropdown1.Height + 10);
                    this.Controls.Add(noGabinetsLabel);
                }

                Console.WriteLine("=== CargarGabinetes completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== ERROR EN CARGA DE GABINETES ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("=================================");

                MessageBox.Show($"Error al cargar los gabinetes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }


        private void Dropdown1_SelectedIndexChanged(object sender, EventArgs e)
        {
            Console.WriteLine("=== Dropdown1_SelectedIndexChanged iniciado ===");
            Console.WriteLine($"Índice seleccionado: {dropdown1.SelectedIndex}");

            // Limpiar el dropdown de categorías y subcategorías
            dropdown2.Items.Clear();
            dropdown3.Items.Clear();

            dropdown2.Items.Add("Seleccione una categoría");
            dropdown3.Items.Add("Seleccione una subcategoría (opcional)");

            dropdown3.Enabled = false;

            if (dropdown1.SelectedIndex > 0)
            {
                dropdown2.Enabled = true;
                Console.WriteLine("dropdown2 habilitado");

                try
                {
                    // Obtener el item seleccionado
                    var selectedCabinetItem = (ComboboxItem)dropdown1.SelectedItem;
                    string cabinetId = selectedCabinetItem.Value;
                    Console.WriteLine($"Gabinete seleccionado - ID: {cabinetId}, Nombre: {selectedCabinetItem.Text}");

                    // Obtener el diccionario de gabinetes
                    var cabinetsDict = (Dictionary<string, Dictionary<string, object>>)dropdown1.Tag;
                    Console.WriteLine($"Gabinetes disponibles en diccionario: {cabinetsDict?.Count ?? 0}");

                    // Obtener las categorías del gabinete seleccionado
                    if (cabinetsDict.ContainsKey(cabinetId))
                    {
                        Console.WriteLine("Gabinete encontrado en diccionario");
                        var cabinet = cabinetsDict[cabinetId];

                        if (cabinet.ContainsKey("categories"))
                        {
                            Console.WriteLine("Categorías encontradas en gabinete");
                            var categoriesObj = JObject.FromObject(cabinet["categories"]);
                            Console.WriteLine($"Propiedades de categorías: {string.Join(", ", categoriesObj.Properties().Select(p => p.Name))}");

                            foreach (var category in categoriesObj.Properties())
                            {
                                string categoryId = category.Name;
                                string categoryName = category.Value["category_name"]?.ToString() ?? "Categoría sin nombre";
                                Console.WriteLine($"Procesando categoría - ID: {categoryId}, Nombre: {categoryName}");

                                var categoryItem = new ComboboxItem
                                {
                                    Text = categoryName,
                                    Value = categoryId
                                };

                                dropdown2.Items.Add(categoryItem);

                                // Guardar las subcategorías para cada categoría
                                if (category.Value["subcategories"] != null)
                                {
                                    categoryItem.Tag = category.Value["subcategories"].ToObject<Dictionary<string, object>>();
                                    Console.WriteLine($"Subcategorías encontradas para categoría {categoryName}");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("El gabinete no contiene categorías");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Error: Gabinete con ID {cabinetId} no encontrado en diccionario");
                    }

                    if (dropdown2.Items.Count > 0)
                    {
                        dropdown2.SelectedIndex = 0;
                        Console.WriteLine($"Total categorías cargadas: {dropdown2.Items.Count - 1}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("=== ERROR EN CARGA DE CATEGORÍAS ===");
                    Console.WriteLine($"Mensaje: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    Console.WriteLine("===================================");

                    MessageBox.Show($"Error al cargar categorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                dropdown2.Enabled = false;
                Console.WriteLine("dropdown2 deshabilitado");
            }

            Console.WriteLine("=== Dropdown1_SelectedIndexChanged completado ===");
        }

        private void Dropdown2_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Limpiar el dropdown de subcategorías
            dropdown3.Items.Clear();
            dropdown3.Items.Add("Seleccione una subcategoría (opcional)");
            dropdown3.SelectedIndex = 0;

            if (dropdown2.SelectedIndex > 0)
            {
                dropdown3.Enabled = true;

                try
                {
                    // Obtener el item seleccionado
                    var selectedCategoryItem = (ComboboxItem)dropdown2.SelectedItem;

                    // Obtener las subcategorías de la categoría seleccionada
                    var subcategoriesDict = selectedCategoryItem.Tag as Dictionary<string, object>;

                    if (subcategoriesDict != null && subcategoriesDict.Count > 0)
                    {
                        foreach (var subcategory in subcategoriesDict)
                        {
                            string subcategoryId = subcategory.Key;

                            // Obtener el nombre de la subcategoría desde el objeto dinámico
                            var subcategoryObj = JObject.FromObject(subcategory.Value);
                            string subcategoryName = subcategoryObj["subcategory_name"].ToString();

                            var subcategoryItem = new ComboboxItem
                            {
                                Text = subcategoryName,
                                Value = subcategoryId
                            };

                            dropdown3.Items.Add(subcategoryItem);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar subcategorías: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                dropdown3.Enabled = false;
            }
        }

        // Método para remover el archivo seleccionado
        private void RemoveFileButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== RemoveFileButton_Click iniciado ===");
            fileLabel.Text = "Ningún archivo seleccionado";
            fileNameLabel.Text = "";
            filePanelContainer.Visible = false;
            Console.WriteLine("Archivo removido correctamente");
            Console.WriteLine("=== RemoveFileButton_Click completado ===");
        }

        // Lógica para seleccionar un archivo
        private void SelectFileButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== SelectFileButton_Click iniciado ===");
            try
            {
                // Configurar el diálogo para permitir solo archivos PDF, JPG, PNG y WEBP
                fileDialog.Filter = "Archivos permitidos (*.pdf;*.jpg;*.jpeg;*.png;*.webp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp|" +
                                    "Documentos PDF (*.pdf)|*.pdf|" +
                                    "Imágenes (*.jpg;*.jpeg;*.png;*.webp)|*.jpg;*.jpeg;*.png;*.webp";
                fileDialog.Title = "Seleccionar documento o imagen";

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    string extension = Path.GetExtension(fileDialog.FileName).ToLower();
                    // Verificar que sea un tipo de archivo permitido
                    if (extension == ".pdf" || extension == ".jpg" || extension == ".jpeg" ||
                        extension == ".png" || extension == ".webp")
                    {
                        fileLabel.Text = fileDialog.FileName;
                        fileNameLabel.Text = Path.GetFileName(fileDialog.FileName);

                        // Mostrar icono según tipo de archivo
                        fileTypeIcon.Image = FileIcons.GetFileIcon(extension);

                        filePanelContainer.Visible = true;
                        Console.WriteLine($"Archivo seleccionado: {fileDialog.FileName}");
                    }
                    else
                    {
                        MessageBox.Show("Por favor seleccione un archivo PDF o imagen (JPG, PNG, WEBP).",
                            "Tipo de archivo no soportado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        Console.WriteLine($"Tipo de archivo no soportado: {extension}");
                    }
                }
                else
                {
                    Console.WriteLine("Selección de archivo cancelada por el usuario");
                }
                Console.WriteLine("=== SelectFileButton_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN SelectFileButton_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al seleccionar archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== ResetButton_Click iniciado ===");
            try
            {
                // Limpiar todos los campos del formulario
                titulo.Text = string.Empty;
                desc.Text = string.Empty;

                // Resetear los comboboxes 
                dropdown1.SelectedIndex = 0;
                dropdown2.Items.Clear();
                dropdown3.Items.Clear();
                dropdown2.Items.Add("Seleccione una categoría");
                dropdown3.Items.Add("Seleccione una subcategoría (opcional)");
                dropdown2.SelectedIndex = 0;
                dropdown3.SelectedIndex = 0;
                dropdown2.Enabled = false;
                dropdown3.Enabled = false;

                // Limpiar el panel de archivo
                fileLabel.Text = "Ningún archivo seleccionado";
                fileNameLabel.Text = "";
                filePanelContainer.Visible = false;

                // Actualizar el timestamp de actividad de UI
                _lastUIActivity = DateTime.Now;

                Console.WriteLine("Todos los campos han sido limpiados");
                MessageBox.Show("Todos los campos han sido limpiados", "Limpieza completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Console.WriteLine("=== ResetButton_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN ResetButton_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al reiniciar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        // Lógica para el botón de enviar
        private async void SubmitButton_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== SubmitButton_Click iniciado ===");
            try
            {
                // Validar selecciones
                Console.WriteLine($"Validando selecciones - dropdown1.SelectedIndex: {dropdown1.SelectedIndex}, dropdown2.SelectedIndex: {dropdown2.SelectedIndex}");
                if (dropdown1.SelectedIndex <= 0 || dropdown2.SelectedIndex <= 0)
                {
                    Console.WriteLine("Validación fallida: No se seleccionaron archivador y/o categoría");
                    MessageBox.Show("Por favor seleccione un archivador y una categoría", "Campos requeridos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Verificar si se seleccionó un archivo
                if (string.IsNullOrEmpty(fileLabel.Text) || !File.Exists(fileLabel.Text))
                {
                    Console.WriteLine("Validación fallida: No se seleccionó un archivo válido");
                    MessageBox.Show("Por favor seleccione un archivo para subir", "Archivo requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Validar tipo de archivo
                string extension = Path.GetExtension(fileLabel.Text).ToLower();
                if (extension != ".pdf" && extension != ".jpg" && extension != ".jpeg" &&
                    extension != ".png" && extension != ".webp")
                {
                    Console.WriteLine($"Validación fallida: Tipo de archivo no permitido: {extension}");
                    MessageBox.Show("El archivo seleccionado no es un formato permitido. Por favor seleccione un archivo PDF o imagen (JPG, PNG, WEBP).",
                        "Tipo de archivo no soportado", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Obtener IDs seleccionados
                string cabinetId = ((ComboboxItem)dropdown1.SelectedItem).Value;
                string categoryId = ((ComboboxItem)dropdown2.SelectedItem).Value;
                string subcategoryId = null;
                string documentTitle = titulo.Text;
                string documentDesc = desc.Text;
                string filePath = fileLabel.Text;

                Console.WriteLine($"Gabinete seleccionado: ID={cabinetId}, Nombre={((ComboboxItem)dropdown1.SelectedItem).Text}");
                Console.WriteLine($"Categoría seleccionada: ID={categoryId}, Nombre={((ComboboxItem)dropdown2.SelectedItem).Text}");
                Console.WriteLine($"Título: {documentTitle}");
                Console.WriteLine($"Descripción: {documentDesc}");
                Console.WriteLine($"Archivo: {filePath}");

                if (dropdown3.SelectedIndex > 0)
                {
                    subcategoryId = ((ComboboxItem)dropdown3.SelectedItem).Value;
                    Console.WriteLine($"Subcategoría seleccionada: ID={subcategoryId}, Nombre={((ComboboxItem)dropdown3.SelectedItem).Text}");
                }
                else
                {
                    Console.WriteLine("No se seleccionó subcategoría");
                }

                // Mostrar formulario de carga durante el proceso
                using (Form loadingForm = new Form())
                {
                    loadingForm.Text = "Subiendo documento";
                    loadingForm.Size = new Size(300, 100);
                    loadingForm.StartPosition = FormStartPosition.CenterScreen;
                    loadingForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    loadingForm.MaximizeBox = false;
                    loadingForm.MinimizeBox = false;

                    Label label = new Label
                    {
                        Text = "Subiendo documento al portal...",
                        AutoSize = true,
                        Location = new Point(20, 20)
                    };
                    loadingForm.Controls.Add(label);

                    ProgressBar progressBar = new ProgressBar
                    {
                        Style = ProgressBarStyle.Marquee,
                        Location = new Point(20, 50),
                        Size = new Size(250, 20)
                    };
                    loadingForm.Controls.Add(progressBar);

                    // Mostrar el form de carga
                    loadingForm.Show();
                    Application.DoEvents();

                    try
                    {
                        // Realizar el envío real del documento
                        bool success = await UploadDocumentAsync(filePath, cabinetId, categoryId, subcategoryId, documentTitle, documentDesc);

                        // Cerrar el formulario de carga
                        loadingForm.Close();

                        if (success)
                        {
                            MessageBox.Show("Documento enviado con éxito al servidor", "Operación exitosa", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            // Limpiar el formulario después de un envío exitoso
                            ResetButton_Click(null, null);
                        }
                        // Si no fue exitoso, el método UploadDocumentAsync ya habrá mostrado un mensaje de error
                    }
                    catch (Exception uploadEx)
                    {
                        // Cerrar el formulario de carga en caso de error
                        loadingForm.Close();
                        throw uploadEx; // Relanzar la excepción para que sea manejada en el catch exterior
                    }
                }

                Console.WriteLine("=== SubmitButton_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN SubmitButton_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al enviar el formulario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Método para subir realmente el documento al portal
        private async Task<bool> UploadDocumentAsync(string filePath, string cabinetId, string categoryId, string subcategoryId, string title, string description)
        {
            Console.WriteLine("=== UploadDocumentAsync iniciado ===");
            try
            {
                // Verificar que existe el archivo
                if (!File.Exists(filePath))
                {
                    MessageBox.Show("No se puede encontrar el archivo para subir.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                using (HttpClient client = new HttpClient())
                {
                    // Añadir el token de autenticación al encabezado
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AppSession.Current.AuthToken);

                    // Crear el contenido multipart para enviar el archivo y los metadatos
                    using (MultipartFormDataContent content = new MultipartFormDataContent())
                    {
                        // Leer el archivo
                        byte[] fileBytes = File.ReadAllBytes(filePath);
                        ByteArrayContent fileContent = new ByteArrayContent(fileBytes);
                        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");

                        // Agregar el archivo al contenido multipart
                        content.Add(fileContent, "documents[]", Path.GetFileName(filePath));

                        // Agregar metadatos
                        content.Add(new StringContent(cabinetId), "cabinet_id");
                        content.Add(new StringContent(categoryId), "category_id");

                        if (!string.IsNullOrEmpty(subcategoryId))
                            content.Add(new StringContent(subcategoryId), "subcategory_id");

                        if (!string.IsNullOrEmpty(title))
                            content.Add(new StringContent(title), "title");

                        if (!string.IsNullOrEmpty(description))
                            content.Add(new StringContent(description), "description");

                        // Enviar la solicitud
                        Console.WriteLine("Enviando solicitud a la API...");
                        HttpResponseMessage response = await client.PostAsync("https://ecm.ecmcentral.com/api/v2/documents", content);

                        // Leer la respuesta
                        string responseContent = await response.Content.ReadAsStringAsync();
                        Console.WriteLine($"Respuesta recibida: {responseContent}");

                        // Verificar si la solicitud fue exitosa
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine("Documento subido con éxito");
                            return true;
                        }
                        else
                        {
                            Console.WriteLine($"Error al subir el documento: {response.StatusCode}");
                            MessageBox.Show($"Error al subir el documento: {responseContent}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN UploadDocumentAsync ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Console.WriteLine("======================================");

                MessageBox.Show($"Error al subir el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        private async void BtnScanToPortal_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== BtnScanToPortal_Click iniciado ===");
            try
            {
                Console.WriteLine("Llamando a ECMVirtualPrinter.ScanAndUploadAsync()");
                // Usar la función mejorada con verificación de licencia e instalación automática
                //await ECMVirtualPrinter.ScanAndUploadAsync();
                Console.WriteLine("=== BtnScanToPortal_Click completado ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== ERROR EN BtnScanToPortal_Click ===");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Mensaje: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }
                Console.WriteLine("========================================");

                MessageBox.Show($"Error al escanear y subir documento: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnPrintToPortal_Click(object sender, EventArgs e)
        {
            Console.WriteLine("=== BtnPrintToPortal_Click iniciado ===");
            try
            {
                // En lugar de usar una ruta fija, permitimos al usuario seleccionar el archivo
                string filePath = string.Empty;

                Console.WriteLine("Verificando si hay un archivo seleccionado en el formulario");
                // Si ya hay un archivo seleccionado en el formulario, usarlo
                if (!string.IsNullOrEmpty(fileLabel.Text) && File.Exists(fileLabel.Text))
                {
                    filePath = fileLabel.Text;
                    Console.WriteLine($"Usando archivo seleccionado: {filePath}");
                }
                else
                {
                    // Si no hay archivo seleccionado, pedir al usuario que seleccione uno
                    Console.WriteLine("No hay archivo seleccionado en el formulario o la ruta no existe");

                    using (OpenFileDialog printDialog = new OpenFileDialog())
                    {
                        printDialog.Filter = "Documentos (*.pdf)|*.pdf|Imágenes (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|Todos los archivos (*.*)|*.*";
                        printDialog.Title = "Seleccionar documento para imprimir";

                        if (printDialog.ShowDialog() == DialogResult.OK)
                        {
                            filePath = printDialog.FileName;
                            Console.WriteLine($"Archivo seleccionado para impresión: {filePath}");

                            // También lo establecemos en el formulario
                            EstablecerArchivoSeleccionado(filePath);
                        }
                        else
                        {
                            Console.WriteLine("Selección de archivo cancelada por el usuario");
                            return; // Usuario canceló, salir del método
                        }
                    }
                }

                // Verificar la extensión del archivo
                string extension = Path.GetExtension(filePath).ToLower();
                if (extension == ".pdf")
                {
                    // Configurar Microsoft Edge como aplicación predeterminada para PDFs
                    Console.WriteLine("Configurando Microsoft Edge como aplicación predeterminada para PDFs");
                    try
                    {
                        // Establecer Microsoft Edge como programa predeterminado para PDFs
                        SetMicrosoftEdgeAsDefaultPDFReader();
                    }
                    catch (Exception edgeEx)
                    {
                        Console.WriteLine($"Error al configurar Edge como predeterminado: {edgeEx.Message}");
                        // No interrumpimos el flujo si falla la configuración
                    }
                }

                // Verificar si la impresora virtual está instalada
                if (!ECMVirtualPrinter.IsPrinterInstalled())
                {
                    DialogResult result = MessageBox.Show(
                        "La impresora virtual ECM Central no está instalada. ¿Desea instalarla ahora?",
                        "Impresora no encontrada",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Console.WriteLine("Iniciando instalación de impresora virtual");

                        // Mostrar un indicador de progreso
                        using (Form waitForm = new Form())
                        {
                            waitForm.Text = "Instalando impresora";
                            waitForm.Size = new Size(300, 100);
                            waitForm.StartPosition = FormStartPosition.CenterScreen;
                            waitForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                            waitForm.MaximizeBox = false;
                            waitForm.MinimizeBox = false;

                            Label label = new Label
                            {
                                Text = "Instalando impresora virtual ECM Central...",
                                AutoSize = true,
                                Location = new Point(20, 20)
                            };
                            waitForm.Controls.Add(label);

                            ProgressBar progressBar = new ProgressBar
                            {
                                Style = ProgressBarStyle.Marquee,
                                Location = new Point(20, 50),
                                Size = new Size(250, 20)
                            };
                            waitForm.Controls.Add(progressBar);

                            // Mostrar el formulario y comenzar la instalación
                            waitForm.Show();
                            Application.DoEvents();

                            try
                            {
                                // Instalar la impresora
                                bool installed = await ECMVirtualPrinter.InstallPrinterAsync();
                                waitForm.Close();

                                if (!installed)
                                {
                                    MessageBox.Show(
                                        "No se pudo instalar la impresora virtual. Por favor, inténtelo de nuevo o contacte con soporte técnico.",
                                        "Error de instalación",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Error);
                                    return;
                                }
                            }
                            catch (Exception installEx)
                            {
                                waitForm.Close();
                                throw new Exception("Error al instalar la impresora: " + installEx.Message, installEx);
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Usuario canceló la instalación de la impresora");
                        return; // Usuario no quiere instalar, salir del método
                    }
                }

                try
                {
                    // Modificación: Usar método directo de impresión sin mostrar mensajes de error
                    Console.WriteLine("Llamando a ECMVirtualPrinter.PrintDocumentAsync()");

                    // Mostrar solo un mensaje para guiar al usuario en el proceso de impresión
                    DialogResult msgResult = MessageBox.Show(
                        "Se abrirá el documento para imprimir.\n\n" +
                        "Por favor, asegúrese de seleccionar la impresora 'ECM Central printer' en el diálogo de impresión que aparecerá.\n\n" +
                        "¿Desea continuar?",
                        "Imprimir a ECM Central",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (msgResult == DialogResult.Yes)
                    {
                        // Llamar al método del ECMVirtualPrinter pero capturando excepciones específicas
                        try
                        {
                            await ECMVirtualPrinter.PrintDocumentAsync(filePath);
                        }
                        catch (System.ComponentModel.Win32Exception win32Ex)
                        {
                            // Esta es la excepción que aparece y queremos ignorar
                            Console.WriteLine($"Se produjo una excepción Win32Exception, pero la ignoramos: {win32Ex.Message}");

                            // Esperar un momento antes de continuar, puede ser que la impresión esté en proceso
                            await Task.Delay(2000);

                            // Preguntar al usuario si la impresión se completó correctamente
                            DialogResult printResult = MessageBox.Show(
                                "¿Se ha completado correctamente la impresión del documento?",
                                "Confirmar impresión",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (printResult == DialogResult.Yes)
                            {
                                // El proceso fue exitoso, buscar el archivo PDF generado
                                string outputFolder = Path.Combine(
                                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                    "ECM Central");

                                if (Directory.Exists(outputFolder))
                                {
                                    // Buscar el archivo PDF más reciente
                                    var recentFiles = Directory.GetFiles(outputFolder, "*.pdf")
                                        .Select(f => new FileInfo(f))
                                        .OrderByDescending(f => f.CreationTime)
                                        .ToList();

                                    if (recentFiles.Count > 0)
                                    {
                                        // Establecer el archivo recién creado en el formulario
                                        EstablecerArchivoSeleccionado(recentFiles[0].FullName);
                                    }
                                }
                            }
                            // No mostrar mensaje de error, seguir con la ejecución normal
                        }
                    }
                }
                catch (Exception printEx) when (!(printEx is System.ComponentModel.Win32Exception))
                {
                    // Capturar otros tipos de excepciones, pero NO las Win32Exception
                    Console.WriteLine($"Error en el proceso de impresión: {printEx.Message}");
                    throw; // Re-lanzar la excepción para que sea manejada por el catch exterior
                }

                Console.WriteLine("=== BtnPrintToPortal_Click completado ===");
            }
            catch (Exception ex)
            {
                // Verificar si es la excepción específica que queremos ignorar
                if (ex is System.ComponentModel.Win32Exception)
                {
                    Console.WriteLine($"Ignorando excepción Win32Exception: {ex.Message}");
                    Console.WriteLine("=== BtnPrintToPortal_Click completado (con excepción ignorada) ===");
                }
                else
                {
                    Console.WriteLine($"=== ERROR EN BtnPrintToPortal_Click ===");
                    Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                    Console.WriteLine($"Mensaje: {ex.Message}");
                    Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                        Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                    }
                    Console.WriteLine("========================================");

                    // Solo mostrar mensaje para excepciones que no sean Win32Exception
                    MessageBox.Show(
                        $"Error al imprimir documento: {ex.Message}\n\n" +
                        "Para utilizar esta función, asegúrese de tener:\n" +
                        "1. La impresora virtual ECM Central instalada\n" +
                        "2. Microsoft Edge como visor de PDF predeterminado",
                        "Error de impresión",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Configura Microsoft Edge como el lector de PDF predeterminado a nivel del sistema
        /// </summary>
        private void SetMicrosoftEdgeAsDefaultPDFReader()
        {
            try
            {
                // Ruta a Microsoft Edge (en instalaciones normales)
                string edgePath = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe";

                // Verificar si existe la ruta estándar, sino, buscar alternativas
                if (!File.Exists(edgePath))
                {
                    // Ruta alternativa
                    edgePath = @"C:\Program Files\Microsoft\Edge\Application\msedge.exe";

                    // Si sigue sin existir, intentar encontrarlo
                    if (!File.Exists(edgePath))
                    {
                        string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

                        // Buscar en ubicaciones posibles
                        string[] possiblePaths = {
                    Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
                    Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe")
                };

                        foreach (string path in possiblePaths)
                        {
                            if (File.Exists(path))
                            {
                                edgePath = path;
                                break;
                            }
                        }

                        if (!File.Exists(edgePath))
                        {
                            throw new FileNotFoundException("No se pudo encontrar la ruta a Microsoft Edge");
                        }
                    }
                }

                // Usar el Registro de Windows para establecer la asociación de archivo PDF a Microsoft Edge
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\.pdf"))
                {
                    key.SetValue("", "MSEdgePDF");
                }

                // Crear la clave para la aplicación
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Classes\MSEdgePDF\shell\open\command"))
                {
                    key.SetValue("", $"\"{edgePath}\" --single-argument %1");
                }

                // Notificar el cambio al sistema (no es necesario un reinicio)
                Native.SHChangeNotify(Native.SHCNE_ASSOCCHANGED, Native.SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);

                Console.WriteLine("Microsoft Edge ha sido configurado como el lector de PDF predeterminado");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar Edge como predeterminado: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Imprime un documento PDF utilizando Microsoft Edge directamente
        /// </summary>
        private async Task<bool> PrintWithEdge(string filePath)
        {
            try
            {
                // Verificar que el archivo existe
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException("El archivo PDF no existe", filePath);
                }

                // Ruta a Microsoft Edge
                string edgePath = FindMicrosoftEdgePath();

                // Si no se encuentra Edge, usar el método estándar
                if (string.IsNullOrEmpty(edgePath))
                {
                    return false;
                }

                // Mostrar mensaje informativo
                MessageBox.Show(
                    $"Se abrirá Microsoft Edge para imprimir el archivo PDF.\n" +
                    $"Por favor, seleccione la impresora 'ECM Central printer' en el diálogo de impresión que aparecerá.",
                    "Imprimir con Microsoft Edge",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                // Iniciar Edge con el argumento de impresión
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = edgePath,
                    Arguments = $"\"{filePath}\" --print",
                    UseShellExecute = true
                };

                // Iniciar el proceso
                using (Process process = Process.Start(startInfo))
                {
                    // Edge se inicia de forma asíncrona, esperamos a que el usuario confirme
                    // No podemos saber cuándo termina la impresión, así que mostramos un diálogo
                    DialogResult result = MessageBox.Show(
                        "¿Ha completado la impresión con Microsoft Edge?\n\n" +
                        "Presione 'Sí' cuando haya terminado de imprimir el documento.",
                        "Confirmación de impresión",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        // Intentar cerrar Edge si sigue abierto
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.CloseMainWindow();
                            }
                        }
                        catch
                        {
                            // Ignorar cualquier error al cerrar Edge
                        }

                        // Buscar el archivo PDF en la carpeta de salida
                        await Task.Delay(1000); // Esperar un segundo para que el archivo se guarde

                        string outputFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            "ECM Central");

                        if (Directory.Exists(outputFolder))
                        {
                            // Buscar el archivo PDF más reciente
                            var files = new DirectoryInfo(outputFolder).GetFiles("*.pdf")
                                .OrderByDescending(f => f.CreationTime)
                                .Take(1)
                                .ToList();

                            if (files.Count > 0)
                            {
                                // Establecer el archivo recién creado en el formulario
                                EstablecerArchivoSeleccionado(files[0].FullName);
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al imprimir con Edge: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Busca la ruta de instalación de Microsoft Edge
        /// </summary>
        private string FindMicrosoftEdgePath()
        {
            string[] possiblePaths = {
                @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
                @"C:\Program Files\Microsoft\Edge\Application\msedge.exe"
            };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            // Buscar usando las variables de entorno
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

            possiblePaths = new string[] {
        Path.Combine(programFiles, @"Microsoft\Edge\Application\msedge.exe"),
        Path.Combine(programFilesX86, @"Microsoft\Edge\Application\msedge.exe")
    };

            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }

        /// <summary>
        /// Clase para interactuar con funciones nativas de Windows
        /// </summary>
        private static class Native
        {
            public const int SHCNE_ASSOCCHANGED = 0x08000000;
            public const int SHCNF_IDLIST = 0x0000;

            [System.Runtime.InteropServices.DllImport("shell32.dll")]
            public static extern void SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
        }

        public async void EstablecerArchivoSeleccionado(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Console.WriteLine($"Archivo inválido o no existe: {filePath}");
                return;
            }

            Console.WriteLine($"Estableciendo archivo seleccionado: {filePath}");

            // Ejecutar el procesamiento en otro hilo para no bloquear la UI
            await Task.Run(() => {
                try
                {
                    // Verificar si el archivo existe y está accesible
                    if (!File.Exists(filePath))
                        return;

                    // Crear una copia de trabajo para evitar bloqueos de archivo
                    string tempFile = Path.Combine(
                        Path.GetTempPath(),
                        $"ecm_copy_{Guid.NewGuid().ToString().Substring(0, 8)}_{Path.GetFileName(filePath)}");

                    try
                    {
                        File.Copy(filePath, tempFile, true);

                        // Actualizar la UI en el hilo principal
                        this.BeginInvoke((Action)(() => {
                            ProcesarArchivoSeleccionadoDirectamente(tempFile);
                        }));
                    }
                    catch (Exception copyEx)
                    {
                        Console.WriteLine($"Error al copiar archivo: {copyEx.Message}");

                        // Si falla la copia, intentar con el original
                        this.BeginInvoke((Action)(() => {
                            ProcesarArchivoSeleccionadoDirectamente(filePath);
                        }));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error en procesamiento asíncrono: {ex.Message}");
                }
            });
        }
        private async void ProcesarArchivoSeleccionadoDirectamente(string filePath)
        {
            // Crear un identificador único para esta operación
            string operationId = Guid.NewGuid().ToString().Substring(0, 8);
            Diagnostics.LogInfo($"[{operationId}] Iniciando procesamiento de archivo: {filePath}");

            try
            {
                // Indicador visual de procesamiento
                UseWaitCursor = true;

                // Validaciones básicas en el hilo UI antes de cualquier operación async
                if (string.IsNullOrEmpty(filePath))
                {
                    Diagnostics.LogWarning($"[{operationId}] Ruta de archivo inválida");
                    UseWaitCursor = false;
                    return;
                }

                if (!File.Exists(filePath))
                {
                    Diagnostics.LogWarning($"[{operationId}] El archivo no existe: {filePath}");
                    UseWaitCursor = false;
                    return;
                }

                // Guardar el archivo que estamos procesando para acceso posterior
                selectedFilePath = filePath;

                // Inicializar variables con valores predeterminados
                string fileName = Path.GetFileName(filePath); // Valor por defecto
                string fileExtension = Path.GetExtension(filePath).ToLower(); // Valor por defecto
                long fileSize = 0;

                // Operación más pesada en Task.Run para evitar bloquear la UI
                await Task.Run(() => {
                    try
                    {
                        Diagnostics.LogInfo($"[{operationId}] Obteniendo información del archivo en segundo plano");

                        FileInfo info = new FileInfo(filePath);
                        fileName = info.Name; // Actualizar con valor real
                        fileExtension = info.Extension.ToLower(); // Actualizar con valor real
                        fileSize = info.Length;

                        // Simular operación que toma tiempo (para pruebas)
                        Thread.Sleep(100);

                        Diagnostics.LogInfo($"[{operationId}] Información obtenida: {fileName}, {fileSize} bytes");
                    }
                    catch (Exception ex)
                    {
                        Diagnostics.LogError($"[{operationId}] Error al obtener información del archivo", ex);
                        // No relanzar la excepción - usaremos los valores por defecto
                    }
                });

                // Ya en el hilo de UI, actualizar la interfaz de forma segura
                Diagnostics.LogInfo($"[{operationId}] Actualizando UI con información del archivo");

                // Actualizar campos de la UI de forma segura con comprobaciones de null
                if (fileNameLabel != null)
                    fileNameLabel.Text = fileName;

                if (fileLabel != null)
                    fileLabel.Text = filePath;

                // Actualizar icono según extensión
                if (fileTypeIcon != null)
                    fileTypeIcon.Image = FileIcons.GetFileIcon(fileExtension);

                // Mostrar el panel con la información del archivo
                if (filePanelContainer != null)
                    filePanelContainer.Visible = true;

                // Si el título está vacío, usar el nombre del archivo
                if (titulo != null && string.IsNullOrEmpty(titulo.Text))
                    titulo.Text = Path.GetFileNameWithoutExtension(filePath);

                // Quitar indicador de procesamiento
                UseWaitCursor = false;

                // Actualizar tiempo de última actividad de UI para prevenir falsos positivos de bloqueo
                _lastUIActivity = DateTime.Now;

                Diagnostics.LogInfo($"[{operationId}] Procesamiento de archivo completado exitosamente");
            }
            catch (Exception ex)
            {
                // Capturar cualquier error no manejado
                Diagnostics.LogError($"[{operationId}] Error general en ProcesarArchivoSeleccionadoDirectamente", ex);
                UseWaitCursor = false;
            }
        }
        private void ConfigurarAntiBloqueo()
        {
            // Crear un timer que detectará si la interfaz se bloquea
            System.Windows.Forms.Timer antiFreeze = new System.Windows.Forms.Timer();
            antiFreeze.Interval = 5000; // Verificar cada 5 segundos

            DateTime lastActivity = DateTime.Now;

            // Actualizar la marca de tiempo en diversos eventos de usuario
            this.MouseMove += (s, e) => lastActivity = DateTime.Now;
            this.KeyDown += (s, e) => lastActivity = DateTime.Now;
            this.Click += (s, e) => lastActivity = DateTime.Now;

            antiFreeze.Tick += (s, e) => {
                // Si no ha habido actividad en 30 segundos y hay operaciones pendientes
                if ((DateTime.Now - lastActivity).TotalSeconds > 30 &&
                    !string.IsNullOrEmpty(fileLabel.Text) && filePanelContainer.Visible)
                {
                    Console.WriteLine("Posible bloqueo detectado, intentando recuperar la interfaz");

                    // Forzar una actualización completa de la interfaz
                    this.SuspendLayout();
                    this.ResumeLayout(true);
                    this.Refresh();

                    // Si el problema persiste, sugerir al usuario que reinicie
                    if ((DateTime.Now - lastActivity).TotalSeconds > 60)
                    {
                        antiFreeze.Stop();
                        MessageBox.Show(
                            "La aplicación parece estar respondiendo lentamente. Se recomienda guardar su trabajo y reiniciar la aplicación.",
                            "Rendimiento reducido",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
            };

            antiFreeze.Start();
        }
    }
}