using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using WinFormsApiClient.VirtualPrinter;
using WinFormsApiClient.VirtualWatcher;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para gestionar el procesamiento de impresiones
    /// </summary>
    public class PrintProcessor
    {
        public const string API_LICENSE_ENDPOINT = "https://ecm.ecmcentral.com/api/v2/check-license";

        // Modo desarrollo: bypass de verificación de licencia
        private static bool _bypassLicenseCheck = true; // true para desarrollo, false para producción

        // Licencia del usuario actual
        public static LicenseFeatures UserLicense { get; private set; } = LicenseFeatures.None;

        private static bool _initialized = false;

        static PrintProcessor()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente PrintProcessor...");
                    _initialized = true;
                    Console.WriteLine("Componente PrintProcessor inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente PrintProcessor: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        // Funcionalidades disponibles según licencia
        [Flags]
        public enum LicenseFeatures
        {
            None = 0,
            Print = 1,
            Upload = 2,
            All = Print | Upload
        }

        public static void SetLicenseBypass(bool bypass)
        {
            _bypassLicenseCheck = bypass;
            if (bypass)
            {
                UserLicense = LicenseFeatures.All;
                Console.WriteLine("Modo desarrollo: Bypass de verificación de licencia activado");
            }
        }

        /// <summary>
        /// Verifica si los controladores del escáner están instalados
        /// </summary>
        public static async Task<bool> CheckLicenseAsync()
        {
            // Si el bypass está activado, retornamos true sin verificar con el servidor
            if (_bypassLicenseCheck)
            {
                UserLicense = LicenseFeatures.All;
                Console.WriteLine("Verificación de licencia omitida (modo desarrollo)");
                return true;
            }
            try
            {
                // Verificar si hay una sesión activa
                if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                {
                    MessageBox.Show("Debe iniciar sesión para verificar su licencia.",
                        "Sesión no válida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                using (HttpClient client = new HttpClient())
                {
                    // Añadir el token de autenticación
                    client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", AppSession.Current.AuthToken);

                    // Llamar al endpoint de verificación de licencias
                    HttpResponseMessage response = await client.GetAsync(API_LICENSE_ENDPOINT);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonResponse = await response.Content.ReadAsStringAsync();
                        var licenseResponse = JsonConvert.DeserializeObject<LicenseResponse>(jsonResponse);

                        if (licenseResponse.Success)
                        {
                            // Activar las funcionalidades según la licencia
                            UserLicense = LicenseFeatures.None;

                            foreach (string feature in licenseResponse.Features)
                            {
                                switch (feature.ToLower())
                                {
                                    case "print":
                                        UserLicense |= LicenseFeatures.Print;
                                        break;
                                    case "upload":
                                        UserLicense |= LicenseFeatures.Upload;
                                        break;
                                }
                            }

                            return true;
                        }
                        else
                        {
                            MessageBox.Show($"No se pudo verificar su licencia: {licenseResponse.Message}",
                                "Error de licencia", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return false;
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Error al verificar licencia: {response.StatusCode}",
                            "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al verificar la licencia: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Imprime un documento a través de la impresora virtual ECM Central y automáticamente lo adjunta al formulario
        /// </summary>
        public static async Task PrintDocumentAsync(string filePath = null)
        {
            string logFile = string.Empty;

            try
            {
                // PRIMER PASO OBLIGATORIO: Iniciar el monitor en segundo plano antes de comenzar
                // y esperar hasta que esté completamente inicializado
                WatcherLogger.LogActivity("Iniciando monitor en segundo plano antes de imprimir");
                bool monitorStarted = await EnsureBackgroundMonitorIsRunningAsync();

                if (!monitorStarted)
                {
                    MessageBox.Show(
                        "No se pudo iniciar el monitor en segundo plano necesario para la impresión.\n" +
                        "Por favor, ejecute la aplicación con el parámetro /backgroundmonitor e intente nuevamente.",
                        "Error de inicialización",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Verificación explícita de la carpeta de logs
                string logFolder = VirtualPrinterCore.GetLogFolderPath();
                Console.WriteLine($"Verificando carpeta de logs en: {logFolder}");

                // Forzar creación de la carpeta si no existe
                if (!Directory.Exists(logFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(logFolder);
                        Console.WriteLine($"Carpeta de logs creada al imprimir: {logFolder}");
                    }
                    catch (Exception folderEx)
                    {
                        Console.WriteLine($"Error al crear carpeta de logs: {folderEx.Message}");
                        // Intentar en el escritorio
                        logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ECM_Logs");
                        if (!Directory.Exists(logFolder))
                            Directory.CreateDirectory(logFolder);
                    }
                }

                logFile = Path.Combine(logFolder, $"ecm_print_{DateTime.Now:yyyyMMdd}.log");

                // Intento directo de escritura para verificar permisos
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Iniciando proceso de impresión\r\n");
                Console.WriteLine($"Archivo de log creado correctamente: {logFile}");

                // Asegurar que la aplicación esté en ejecución (con un timeout de 45 segundos)
                LogHelper.LogMessage(logFile, "Verificando si la aplicación está en ejecución...");
                if (!ApplicationManager.EnsureApplicationIsRunning(45))
                {
                    string errorMsg = "No se pudo iniciar la aplicación en el tiempo esperado (45 segundos).";
                    LogHelper.LogMessage(logFile, $"ERROR: {errorMsg}");

                    MessageBox.Show(
                        $"{errorMsg}\n\nRevise el archivo de registro en:\n{logFile}",
                        "Error al iniciar aplicación",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Verificar licencias
                LogHelper.LogMessage(logFile, "Verificando licencia...");
                if (!await CheckLicenseAsync())
                {
                    LogHelper.LogMessage(logFile, "ERROR: No se pudo verificar la licencia o no tiene permisos suficientes");
                    return;
                }

                // Verificar si tiene permiso para imprimir
                if ((UserLicense & LicenseFeatures.Print) != LicenseFeatures.Print)
                {
                    string errorMsg = "Su cuenta no tiene permisos para utilizar la función de impresión. Contacte con el administrador.";
                    LogHelper.LogMessage(logFile, $"ERROR: {errorMsg}");

                    MessageBox.Show(
                        errorMsg,
                        "Función no disponible",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Verificar si la impresora está instalada
                LogHelper.LogMessage(logFile, "Verificando si la impresora está instalada...");
                if (!VirtualPrinterCore.IsPrinterInstalled())
                {
                    LogHelper.LogMessage(logFile, "La impresora no está instalada. Consultando al usuario...");
                    DialogResult result = MessageBox.Show(
                        "La impresora virtual 'ECM Central printer' no está instalada.\n" +
                        "¿Desea instalarla ahora?",
                        "Impresora no encontrada",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        LogHelper.LogMessage(logFile, "Iniciando instalación de impresora...");
                        if (!await PrinterInstaller.InstallPrinterAsync())
                        {
                            LogHelper.LogMessage(logFile, "ERROR: La instalación de la impresora falló");
                            return;
                        }
                        LogHelper.LogMessage(logFile, "Impresora instalada correctamente");
                    }
                    else
                    {
                        LogHelper.LogMessage(logFile, "Usuario canceló la instalación de la impresora");
                        return;
                    }
                }
                else
                {
                    // Si la impresora está instalada pero podría tener problemas, intentar repararla
                    LogHelper.LogMessage(logFile, "Impresora encontrada. Verificando/reparando configuración...");
                    await PrinterInstaller.RepairPrinterAsync();
                }

                // Iniciar proceso de impresión con timeout global
                int timeoutSeconds = 120; // 2 minutos máximo para todo el proceso
                using (var timeoutCancellation = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
                {
                    try
                    {
                        // Limpiar la cola de impresión antes de imprimir
                        LogHelper.LogMessage(logFile, "Limpiando cola de impresión...");
                        PrintQueueManager.ClearPrintQueue();

                        // Verificar que se proporcionó una ruta de archivo
                        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                        {
                            LogHelper.LogMessage(logFile, "No se proporcionó un archivo válido. Solicitando al usuario...");
                            // Si no hay una ruta válida, permitir al usuario seleccionar un archivo
                            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog())
                            {
                                openFileDialog.Filter = "Documentos (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Todos los archivos (*.*)|*.*";
                                openFileDialog.Title = "Seleccione un archivo para imprimir";

                                if (openFileDialog.ShowDialog() == DialogResult.OK)
                                {
                                    filePath = openFileDialog.FileName;
                                    LogHelper.LogMessage(logFile, $"Usuario seleccionó el archivo: {filePath}");
                                }
                                else
                                {
                                    LogHelper.LogMessage(logFile, "Usuario canceló la selección de archivo");
                                    // El usuario canceló la selección
                                    return;
                                }
                            }
                        }

                        // Verificar que el archivo existe
                        if (!File.Exists(filePath))
                        {
                            string errorMsg = $"El archivo seleccionado no existe: {filePath}";
                            LogHelper.LogMessage(logFile, $"ERROR: {errorMsg}");

                            MessageBox.Show(errorMsg,
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }

                        // Establecer la carpeta de salida PDF para monitorear los archivos nuevos
                        string pdfOutputFolder = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                            VirtualPrinterCore.OUTPUT_FOLDER);

                        if (!Directory.Exists(pdfOutputFolder))
                        {
                            LogHelper.LogMessage(logFile, $"Creando carpeta de salida: {pdfOutputFolder}");
                            Directory.CreateDirectory(pdfOutputFolder);
                        }

                        // Obtener lista de archivos PDF antes de imprimir (para comparar después)
                        LogHelper.LogMessage(logFile, "Obteniendo lista de archivos PDF existentes...");
                        var existingFiles = new System.Collections.Generic.HashSet<string>(Directory.GetFiles(pdfOutputFolder, "*.pdf"));
                        LogHelper.LogMessage(logFile, $"Encontrados {existingFiles.Count} archivos PDF existentes");

                        // Obtener la extensión para determinar el método de impresión adecuado
                        string extension = Path.GetExtension(filePath).ToLower();
                        bool isPdf = extension == ".pdf";
                        LogHelper.LogMessage(logFile, $"Tipo de archivo: {extension}, ¿Es PDF? {isPdf}");

                        // Intentar imprimir usando la ruta directa si es PDF
                        if (isPdf)
                        {
                            LogHelper.LogMessage(logFile, "Intentando impresión directa para PDF...");
                            // Si es un PDF, intentar imprimir directamente a la impresora
                            try
                            {
                                // Intento directo usando PrintDocument (.NET nativo)
                                Task<bool> printTask = PrintPdfDirectlyAsync(filePath, VirtualPrinterCore.PRINTER_NAME);

                                // Esperar a que termine la impresión o se alcance el timeout
                                if (await Task.WhenAny(printTask, Task.Delay(30000, timeoutCancellation.Token)) == printTask)
                                {
                                    bool printed = await printTask;
                                    if (!printed)
                                    {
                                        LogHelper.LogMessage(logFile, "La impresión directa falló. Usando método alternativo.");
                                        throw new Exception("No se pudo imprimir directamente. Usando método alternativo.");
                                    }
                                    else
                                    {
                                        LogHelper.LogMessage(logFile, "Impresión directa completada con éxito");
                                    }
                                }
                                else
                                {
                                    LogHelper.LogMessage(logFile, "TIMEOUT: La impresión directa tardó demasiado. Intentando método alternativo.");
                                    throw new TimeoutException("La impresión directa tardó demasiado. Intentando método alternativo.");
                                }
                            }
                            catch (Exception ex)
                            {
                                // Si falla, registrar el error y volver al método tradicional
                                LogHelper.LogMessage(logFile, $"Error en impresión directa: {ex.Message}. Usando método tradicional.");
                                await PrintUsingDefaultMethodAsync(filePath);
                            }
                        }
                        else
                        {
                            // Para otros tipos de archivos, usar el método tradicional
                            LogHelper.LogMessage(logFile, "Usando método tradicional de impresión...");
                            await PrintUsingDefaultMethodAsync(filePath);
                        }

                        // Buscar archivos PDF nuevos en la carpeta de salida (esperar un poco para que se complete el guardado)
                        LogHelper.LogMessage(logFile, "Esperando a que se complete el guardado del PDF...");
                        await Task.Delay(3000, timeoutCancellation.Token); // Esperar 3 segundos

                        LogHelper.LogMessage(logFile, "Buscando archivos PDF nuevos...");
                        var newPdfFiles = new System.Collections.Generic.List<string>();
                        foreach (string pdfFile in Directory.GetFiles(pdfOutputFolder, "*.pdf"))
                        {
                            if (!existingFiles.Contains(pdfFile))
                            {
                                LogHelper.LogMessage(logFile, $"Nuevo PDF encontrado: {pdfFile}");
                                newPdfFiles.Add(pdfFile);
                            }
                        }

                        // Si encontramos archivos nuevos, usar el más reciente
                        if (newPdfFiles.Count > 0)
                        {
                            string outputPath = newPdfFiles.OrderByDescending(f => new FileInfo(f).CreationTime).First();
                            LogHelper.LogMessage(logFile, $"Usando el PDF más reciente: {outputPath}");

                            // Adjuntar el PDF automáticamente al formulario
                            LogHelper.LogMessage(logFile, "Subiendo PDF al portal...");
                            await DocumentUploader.UploadToPortalAsync(outputPath);
                            LogHelper.LogMessage(logFile, "Subida completada con éxito");
                        }
                        else
                        {
                            LogHelper.LogMessage(logFile, "No se encontraron nuevos PDFs. Usando el archivo original.");
                            // Si no se encontró ningún PDF nuevo, usar el archivo original
                            MessageBox.Show(
                                "No se encontró ningún archivo PDF generado por la impresión.\n" +
                                "Se utilizará el archivo original para adjuntarlo al formulario.",
                                "Archivo PDF no encontrado",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);

                            // Adjuntar el archivo original
                            LogHelper.LogMessage(logFile, $"Subiendo archivo original al portal: {filePath}");
                            await DocumentUploader.UploadToPortalAsync(filePath);
                            LogHelper.LogMessage(logFile, "Subida completada con éxito");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        string errorMsg = "La operación fue cancelada por el usuario.";
                        LogHelper.LogMessage(logFile, errorMsg);
                        MessageBox.Show(errorMsg, "Operación cancelada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (TimeoutException)
                    {
                        string errorMsg = $"La operación ha excedido el tiempo máximo de espera ({timeoutSeconds} segundos).";
                        LogHelper.LogMessage(logFile, $"ERROR: {errorMsg}");
                        MessageBox.Show(
                            $"{errorMsg}\n\nRevise el archivo de registro en:\n{logFile}",
                            "Tiempo de espera agotado",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception ex)
                    {
                        string errorMsg = $"Error durante la impresión: {ex.Message}";
                        LogHelper.LogMessage(logFile, $"ERROR: {errorMsg}\nStack Trace: {ex.StackTrace}");

                        MessageBox.Show(
                            $"{errorMsg}\n\nRevise el archivo de registro para más detalles en:\n{logFile}",
                            "Error de impresión",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                // Capturar cualquier error no controlado
                try
                {
                    if (!string.IsNullOrEmpty(logFile))
                    {
                        LogHelper.LogMessage(logFile, $"ERROR CRÍTICO: {ex.Message}\nStack Trace: {ex.StackTrace}");
                    }

                    // Si falla el registro, intentar escribir en el escritorio
                    File.WriteAllText(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_print_error.log"),
                        $"[{DateTime.Now}] ERROR CRÍTICO: {ex.Message}\n{ex.StackTrace}");
                }
                catch { /* Último intento fallido, no podemos hacer más */ }

                MessageBox.Show($"Error al imprimir el documento: {ex.Message}",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        /// <summary>
        /// Asegura que el monitor en segundo plano esté activo antes de continuar con la impresión
        /// </summary>
        public static async Task<bool> EnsureBackgroundMonitorIsRunningAsync()
        {
            try
            {
                // Verificar si ya está en ejecución mediante el servicio
                if (WinFormsApiClient.VirtualWatcher.BackgroundMonitorService._isRunning)
                {
                    Console.WriteLine("Monitor ya en ejecución según BackgroundMonitorService");
                    return true;
                }

                // Verificar si hay algún proceso monitor ejecutándose mediante archivo de marcador
                string markerFilePath = Path.Combine(Path.GetTempPath(), "ecm_monitor_running.marker");
                bool markerExists = File.Exists(markerFilePath);

                if (markerExists)
                {
                    try
                    {
                        string pidContent = File.ReadAllText(markerFilePath);
                        if (int.TryParse(pidContent, out int pid))
                        {
                            try
                            {
                                Process process = Process.GetProcessById(pid);
                                if (!process.HasExited)
                                {
                                    Console.WriteLine($"Monitor ya en ejecución con PID: {pid}");
                                    return true;
                                }
                            }
                            catch (ArgumentException)
                            {
                                // El proceso no existe, eliminar el marcador
                                try { File.Delete(markerFilePath); } catch { }
                                markerExists = false;
                            }
                        }
                    }
                    catch
                    {
                        markerExists = false;
                    }
                }

                if (!markerExists)
                {
                    Console.WriteLine("Iniciando monitor en segundo plano explícitamente");

                    // Iniciar el monitor en segundo plano mediante proceso separado
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = "/backgroundmonitor",
                        UseShellExecute = true, // Importante: true para mostrar la ventana/icono
                        CreateNoWindow = false  // Importante: false para que se cree la ventana
                    };

                    Process monitorProcess = Process.Start(startInfo);

                    // Esperar a que el monitor esté activo (verificar el archivo de marcador)
                    int maxWaitSeconds = 10; // Máximo tiempo de espera en segundos
                    bool monitorActive = false;

                    for (int i = 0; i < maxWaitSeconds; i++)
                    {
                        await Task.Delay(1000); // Esperar 1 segundo

                        // Verificar si el archivo de marcador ya existe (el monitor está activo)
                        if (File.Exists(markerFilePath))
                        {
                            monitorActive = true;
                            Console.WriteLine("Monitor iniciado exitosamente");
                            break;
                        }

                        Console.WriteLine($"Esperando al monitor... ({i + 1}/{maxWaitSeconds})");
                    }

                    return monitorActive;
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar/iniciar monitor de fondo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Imprime un archivo PDF directamente a la impresora sin mostrar diálogos
        /// </summary>
        private static async Task<bool> PrintPdfDirectlyAsync(string filePath, string printerName)
        {
            try
            {
                Console.WriteLine($"Intentando imprimir directamente el PDF: {filePath} a {printerName}");

                // Esperar un poco para asegurar que el sistema está listo
                await Task.Delay(1000);

                // Usamos PowerShell para imprimir directamente
                string psCommand = $@"
            try {{
                Add-Type -AssemblyName System.Drawing
                Add-Type -AssemblyName System.Windows.Forms
                
                # Establecer la impresora como predeterminada temporalmente
                $originalDefault = (New-Object -ComObject WScript.Network).GetDefaultPrinterName()
                (New-Object -ComObject WScript.Network).SetDefaultPrinter('{printerName}')
                
                $pdfPath = '{filePath.Replace("\\", "\\\\")}'
                
                if (-not (Test-Path -Path $pdfPath -PathType Leaf)) {{
                    Write-Output ""ERROR: No se encontró el archivo PDF""
                    return $false
                }}
                
                # Crear un PrintDocument
                $printDoc = New-Object System.Drawing.Printing.PrintDocument
                $printDoc.PrinterSettings.PrinterName = '{printerName}'
                $printDoc.PrinterSettings.PrintToFile = $false
                $printDoc.DocumentName = [System.IO.Path]::GetFileNameWithoutExtension($pdfPath)
                
                # Configuración
                $printDoc.Print()
                
                # Restaurar la impresora predeterminada
                (New-Object -ComObject WScript.Network).SetDefaultPrinter($originalDefault)
                
                Write-Output ""PDF enviado a la impresora correctamente""
                return $true
            }}
            catch {{
                Write-Output ""ERROR: $($_.Exception.Message)""
                return $false
            }}
        ";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                Console.WriteLine($"Resultado de impresión directa: {result}");

                return result.Contains("correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en impresión directa: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Imprime un documento usando el método tradicional (abriendo la aplicación asociada)
        /// </summary>
        private static async Task PrintUsingDefaultMethodAsync(string filePath)
        {
            // Crear un proceso para abrir el archivo con la aplicación predeterminada
            Process process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true,
                Verb = "print"
            };

            // Iniciar el proceso
            process.Start();

            // Informar al usuario
            MessageBox.Show(
                $"Se ha iniciado la impresión del archivo: {Path.GetFileName(filePath)}\n\n" +
                "Por favor, seleccione la impresora 'ECM Central printer' en el diálogo que aparecerá.\n" +
                "Una vez finalizada la impresión, el documento se adjuntará automáticamente al formulario.",
                "Imprimir a ECM Central",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Esperar a que el usuario termine la impresión
            DialogResult captureResult = MessageBox.Show(
                "¿Ha completado la impresión?",
                "Impresión completada",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (captureResult == DialogResult.Yes)
            {
                // Limpiar la cola de impresión después de confirmar
                PrintQueueManager.ClearPrintQueue();
            }
            else
            {
                throw new OperationCanceledException("Operación cancelada por el usuario");
            }
        }
    }
}