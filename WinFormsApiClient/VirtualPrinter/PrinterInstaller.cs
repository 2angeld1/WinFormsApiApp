using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase para instalar y reparar la impresora virtual
    /// </summary>
    public class PrinterInstaller
    {
        private static bool _initialized = false;

        static PrinterInstaller()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente PrinterInstaller...");
                    _initialized = true;
                    Console.WriteLine("Componente PrinterInstaller inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente PrinterInstaller: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Instala automáticamente la impresora virtual usando Microsoft Print to PDF
        /// </summary>
        /// <param name="silent">Si es true, instala sin mostrar mensajes al usuario</param>
        public static async Task<bool> InstallPrinterAsync(bool silent = false)
        {
            // Verificar si ya está instalada la impresora
            if (VirtualPrinterCore.IsPrinterInstalled())
            {
                Console.WriteLine($"La impresora {VirtualPrinterCore.PRINTER_NAME} ya está instalada");

                // Configurar la carpeta de salida
                VirtualPrinterCore.EnsureOutputFolderExists();

                // Configurar automatización del diálogo
                if (VirtualPrinterCore.DRIVER_NAME == "Microsoft Print To PDF")
                {
                    bool configured = ConfigurePDFDialogAutomation();
                    if (!configured && !silent)
                    {
                        MessageBox.Show(
                            $"La impresora {VirtualPrinterCore.PRINTER_NAME} está instalada pero no se pudo configurar " +
                            $"la automatización del diálogo de guardado. Por favor, al imprimir guarde manualmente en: " +
                            VirtualPrinterCore.FIXED_OUTPUT_PATH,
                            "Configuración manual requerida",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }

                return true;
            }

            // Si la impresora no está instalada, verificar permisos de administrador
            if (!VirtualPrinterCore.IsAdministrator())
            {
                if (!silent) // Solo mostrar diálogo si no es modo silencioso
                {
                    DialogResult result = MessageBox.Show(
                        $"La instalación de la impresora {VirtualPrinterCore.PRINTER_NAME} requiere permisos de administrador.\n" +
                        "¿Desea ejecutar el instalador con privilegios elevados?",
                        "Permisos requeridos",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        try
                        {
                            // Obtener la ruta del ejecutable actual
                            string exePath = System.Reflection.Assembly.GetEntryAssembly().Location;

                            // Iniciar proceso con elevación de privilegios
                            ProcessStartInfo startInfo = new ProcessStartInfo
                            {
                                FileName = exePath,
                                UseShellExecute = true,
                                Verb = "runas", // Solicita privilegios de administrador
                                Arguments = "/installprinter" // Argumento para indicar que es una instalación de impresora
                            };

                            Process.Start(startInfo);
                            return true; // Retornamos éxito ya que se inició el proceso de instalación
                        }
                        catch (Exception ex)
                        {
                            if (!silent)
                            {
                                MessageBox.Show($"Error al solicitar permisos de administrador: {ex.Message}",
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                            else
                            {
                                Console.WriteLine($"Error al solicitar permisos de administrador: {ex.Message}");
                            }
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    Console.WriteLine("Instalación en modo silencioso rechazada: se requieren permisos de administrador");
                    return false;
                }
            }

            // Si tenemos permisos de administrador, procedemos con la instalación
            Form loadingForm = null;

            // Solo mostrar formulario de progreso si no es modo silencioso
            if (!silent)
            {
                loadingForm = new Form();
                loadingForm.Text = $"Instalando impresora {VirtualPrinterCore.PRINTER_NAME}";
                loadingForm.Size = new System.Drawing.Size(400, 120);
                loadingForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                loadingForm.StartPosition = FormStartPosition.CenterScreen;
                loadingForm.MaximizeBox = false;
                loadingForm.MinimizeBox = false;

                Label statusLabel = new Label
                {
                    Text = $"Instalando impresora virtual {VirtualPrinterCore.PRINTER_NAME}...",
                    AutoSize = true,
                    Location = new System.Drawing.Point(20, 20)
                };
                loadingForm.Controls.Add(statusLabel);

                ProgressBar progressBar = new ProgressBar
                {
                    Style = ProgressBarStyle.Marquee,
                    Location = new System.Drawing.Point(20, 50),
                    Size = new System.Drawing.Size(350, 23)
                };
                loadingForm.Controls.Add(progressBar);

                // Mostrar el formulario de carga
                loadingForm.Show();
                Application.DoEvents();
            }
            else
            {
                Console.WriteLine($"Instalando impresora {VirtualPrinterCore.PRINTER_NAME} en modo silencioso...");
            }

            try
            {
                // 1. Asegurar que la carpeta de salida existe
                VirtualPrinterCore.EnsureOutputFolderExists();

                // 2. Crear el puerto y la impresora usando PowerShell
                Console.WriteLine("Instalando el driver de impresora Microsoft Print To PDF...");
                PowerShellHelper.RunPowerShellCommand($"Add-PrinterDriver -Name '{VirtualPrinterCore.DRIVER_NAME}' -ErrorAction SilentlyContinue");

                Console.WriteLine($"Creando puerto de impresora: {VirtualPrinterCore.PORT_NAME}");
                PowerShellHelper.RunPowerShellCommand($"Add-PrinterPort -Name '{VirtualPrinterCore.PORT_NAME}' -PrinterHostAddress 'localhost' -ErrorAction SilentlyContinue");

                Console.WriteLine($"Creando impresora: {VirtualPrinterCore.PRINTER_NAME}");
                PowerShellHelper.RunPowerShellCommand($"Add-Printer -Name '{VirtualPrinterCore.PRINTER_NAME}' -DriverName '{VirtualPrinterCore.DRIVER_NAME}' -PortName '{VirtualPrinterCore.PORT_NAME}'");

                // Dar tiempo a que se complete la instalación
                await Task.Delay(2000);

                // 3. Configurar la carpeta de salida
                string outputFolder = VirtualPrinterCore.GetOutputFolderPath();
                string psCommand = $@"
                try {{
                    $printer = Get-Printer -Name '{VirtualPrinterCore.PRINTER_NAME}' -ErrorAction SilentlyContinue
                    if ($printer) {{
                        $outputPath = '{outputFolder.Replace("\\", "\\\\")}'
                        Set-PrintConfiguration -PrinterName '{VirtualPrinterCore.PRINTER_NAME}' -PrintTicketXML '<PrintTicket xmlns=""http://schemas.microsoft.com/windows/2003/08/printing/printticket""><ParameterInit name=""FileNameSettings""><StringParameter name=""DocumentNameExtension"">.pdf</StringParameter><StringParameter name=""Directory"">' + $outputPath + '</StringParameter></ParameterInit></PrintTicket>' -ErrorAction SilentlyContinue
                        Write-Output ""Impresora configurada para usar carpeta: $outputPath""
                    }}
                }} catch {{
                    Write-Output ""Error al configurar carpeta de salida: $($_.Exception.Message)""
                }}";

                PowerShellHelper.RunPowerShellCommand(psCommand);

                // 4. Si utilizamos Microsoft Print to PDF, iniciar la automatización del diálogo de guardado
                bool automated = false;
                if (VirtualPrinterCore.DRIVER_NAME == "Microsoft Print To PDF")
                {
                    Console.WriteLine("Configurando automatización del diálogo de guardado...");
                    automated = ConfigurePDFDialogAutomation();
                }

                if (loadingForm != null)
                    loadingForm.Close();

                // 5. Verificar si la impresora se instaló correctamente
                bool installed = VirtualPrinterCore.IsPrinterInstalled();
                Console.WriteLine($"Verificación de instalación: {(installed ? "Impresora instalada" : "Instalación fallida")}");

                if (installed)
                {
                    if (!silent)
                    {
                        string message = $"La impresora {VirtualPrinterCore.PRINTER_NAME} se ha instalado correctamente.\n\n";

                        if (VirtualPrinterCore.DRIVER_NAME == "Microsoft Print To PDF" && !automated)
                        {
                            message += $"NOTA: Al imprimir, guarde manualmente los archivos en:\n{VirtualPrinterCore.FIXED_OUTPUT_PATH}";
                        }
                        else
                        {
                            message += $"Los PDF se guardarán automáticamente en: {VirtualPrinterCore.FIXED_OUTPUT_PATH}";
                        }

                        MessageBox.Show(
                            message,
                            "Instalación exitosa",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        Console.WriteLine($"Impresora {VirtualPrinterCore.PRINTER_NAME} instalada correctamente en modo silencioso");
                    }

                    return true;
                }
                else
                {
                    if (!silent)
                    {
                        MessageBox.Show(
                            $"No se pudo instalar la impresora {VirtualPrinterCore.PRINTER_NAME}. Por favor, inténtelo de nuevo o contacte con soporte técnico.",
                            "Error de instalación",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        Console.WriteLine($"Error: La instalación silenciosa de la impresora {VirtualPrinterCore.PRINTER_NAME} ha fallado");
                    }

                    return false;
                }
            }
            catch (Exception ex)
            {
                if (loadingForm != null)
                    loadingForm.Close();

                if (!silent)
                {
                    MessageBox.Show($"Error durante la instalación de la impresora: {ex.Message}",
                        "Error de instalación", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine($"Error durante la instalación silenciosa de la impresora: {ex.Message}");
                }
                return false;
            }
        }

        /// <summary>
        /// Configura e inicia la automatización del diálogo de guardado de PDF para Microsoft Print to PDF
        /// </summary>
        public static bool ConfigurePDFDialogAutomation()
        {
            try
            {
                Console.WriteLine("Configurando automatización de diálogo de guardado PDF...");

                // Crear y asegurar que exista la carpeta de destino
                if (!Directory.Exists(VirtualPrinterCore.FIXED_OUTPUT_PATH))
                {
                    Directory.CreateDirectory(VirtualPrinterCore.FIXED_OUTPUT_PATH);
                    Console.WriteLine($"Carpeta de destino creada: {VirtualPrinterCore.FIXED_OUTPUT_PATH}");
                }

                // Iniciar la automatización del diálogo
                bool success = PDFDialogAutomation.StartDialogAutomation();

                if (success)
                {
                    Console.WriteLine("Automatización de diálogo de guardado iniciada correctamente");
                    return true;
                }
                else
                {
                    Console.WriteLine("No se pudo iniciar la automatización del diálogo de guardado");

                    // Mostrar mensaje informativo al usuario
                    MessageBox.Show(
                        "No se pudo configurar la automatización del diálogo de guardado.\n\n" +
                        $"Por favor, cuando imprima documentos, guárdelos manualmente en la carpeta:\n{VirtualPrinterCore.FIXED_OUTPUT_PATH}",
                        "Configuración manual requerida",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);

                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar automatización de diálogo: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configura y repara la impresora virtual si hay problemas
        /// </summary>
        public static async Task<bool> RepairPrinterAsync()
        {
            Console.WriteLine($"Iniciando reparación de la impresora {VirtualPrinterCore.PRINTER_NAME}...");

            try
            {
                // 1. Verificar si hay problemas con el puerto o la impresora
                bool printerExists = VirtualPrinterCore.IsPrinterInstalled();
                Console.WriteLine($"Estado de la impresora: {(printerExists ? "Instalada" : "No encontrada")}");

                if (!printerExists)
                {
                    // Si la impresora no existe, intentar instalarla de nuevo
                    return await InstallPrinterAsync(false);
                }

                // 2. Configurar automatización del diálogo de guardado para Microsoft Print to PDF
                if (VirtualPrinterCore.DRIVER_NAME == "Microsoft Print To PDF")
                {
                    Console.WriteLine("Configurando automatización de diálogo para Microsoft Print To PDF");
                    bool automated = ConfigurePDFDialogAutomation();
                    Console.WriteLine($"Resultado de la automatización de diálogo: {(automated ? "Éxito" : "Fallido")}");
                }

                // 3. Limpiar la cola de impresión
                PrintQueueManager.ClearPrintQueue();

                // 4. Forzar una configuración correcta de la carpeta de salida
                string outputFolder = VirtualPrinterCore.GetOutputFolderPath();

                if (!Directory.Exists(outputFolder))
                    Directory.CreateDirectory(outputFolder);

                // 5. Intentar corregir la configuración de la impresora
                try
                {
                    string psCommand = $@"
                try {{
                    # Reiniciar spooler para liberar cualquier recurso bloqueado
                    Restart-Service -Name Spooler -Force -ErrorAction SilentlyContinue
                    
                    # Esperar a que el servicio reinicie
                    Start-Sleep -Seconds 2
                    
                    # Verificar si la impresora existe
                    $printer = Get-Printer -Name '{VirtualPrinterCore.PRINTER_NAME}' -ErrorAction SilentlyContinue
                    
                    if ($printer) {{
                        # La impresora existe, configurarla correctamente
                        $outputPath = '{outputFolder.Replace("\\", "\\\\")}'
                        
                        # Ajustar configuración de puerto
                        Set-PrintConfiguration -PrinterName '{VirtualPrinterCore.PRINTER_NAME}' -PrintTicketXML '<PrintTicket xmlns=""http://schemas.microsoft.com/windows/2003/08/printing/printticket""><ParameterInit name=""FileNameSettings""><StringParameter name=""DocumentNameExtension"">.pdf</StringParameter><StringParameter name=""Directory"">' + $outputPath + '</StringParameter></ParameterInit></PrintTicket>' -ErrorAction SilentlyContinue
                        
                        # Configuración adicional
                        Set-Printer -Name '{VirtualPrinterCore.PRINTER_NAME}' -RenderingMode XPS -Shared $false -ErrorAction SilentlyContinue
                        
                        Write-Output ""Impresora {VirtualPrinterCore.PRINTER_NAME} reparada correctamente""
                        return $true
                    }} else {{
                        Write-Output ""La impresora no existe, se requiere reinstalación""
                        return $false
                    }}
                }} catch {{
                    Write-Output ""Error al reparar la impresora: $($_.Exception.Message)""
                    return $false
                }}
            ";

                    string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                    Console.WriteLine($"Resultado de la reparación: {result}");

                    // 6. Verificar nuevamente si la impresora está disponible
                    bool available = VirtualPrinterCore.IsPrinterInstalled();

                    if (available)
                    {
                        MessageBox.Show(
                            $"La impresora {VirtualPrinterCore.PRINTER_NAME} ha sido reparada correctamente.\n\n" +
                            $"Los archivos PDF se guardarán en: {VirtualPrinterCore.FIXED_OUTPUT_PATH}",
                            "Reparación completada",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }

                    return available;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error durante la reparación: {ex.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error durante la reparación de la impresora: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Instala el servicio de monitoreo para que se inicie automáticamente con Windows
        /// </summary>
        public static bool InstallBackgroundMonitor()
        {
            try
            {
                // Verificar si el servicio ya está instalado
                if (WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.IsInstalledForAutostart())
                {
                    LogHelper.LogMessage("log_install.txt", "El servicio de monitoreo ya está instalado para inicio automático");

                    // Verificar que la impresora esté instalada
                    if (!VirtualPrinterCore.IsPrinterInstalled())
                    {
                        LogHelper.LogMessage("log_install.txt", "Instalando impresora virtual primero...");
                        Task.Run(async () => await InstallPrinterAsync(true)).Wait();
                    }
                    else
                    {
                        LogHelper.LogMessage("log_install.txt", "La impresora virtual ya está instalada");
                    }

                    // Iniciar el servicio inmediatamente si no está en ejecución
                    Process.Start(Application.ExecutablePath, "/backgroundmonitor");
                    return true;
                }

                // Verificar que la impresora esté instalada
                if (!VirtualPrinterCore.IsPrinterInstalled())
                {
                    LogHelper.LogMessage("log_install.txt", "Instalando impresora virtual primero...");
                    Task.Run(async () => await InstallPrinterAsync(true)).Wait();
                }

                // Instalar el servicio de monitoreo
                bool success = WinFormsApiClient.VirtualWatcher.BackgroundMonitorService.InstallAutostart();

                if (success)
                {
                    LogHelper.LogMessage("log_install.txt", "Servicio de monitoreo instalado correctamente");

                    // Iniciar el servicio inmediatamente
                    Process.Start(Application.ExecutablePath, "/backgroundmonitor");
                }

                return success;
            }
            catch (Exception ex)
            {
                LogHelper.LogMessage("log_install.txt", $"Error al instalar servicio de monitoreo: {ex.Message}");
                return false;
            }
        }
    }
}