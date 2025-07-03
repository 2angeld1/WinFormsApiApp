using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinFormsApiClient.VirtualPrinter;
using WinFormsApiClient.VirtualWatcher;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase principal que maneja la funcionalidad básica de la impresora virtual
    /// </summary>
    public class VirtualPrinterCore
    {
        public const string PRINTER_NAME = "ECM Central Printer";
        public const string PORT_NAME = "PORTPROMPT:";
        public const string DRIVER_NAME = "Microsoft Print To PDF";
        public const string OUTPUT_FOLDER = "ECM Central";
        public const string LOG_FOLDER = "Logs";
        // Nueva constante con la ruta fija
        public const string FIXED_OUTPUT_PATH = @"C:\Temp\ECM Central";

        private static bool _initialized = false;

        static VirtualPrinterCore()
        {
            try
            {
                if (!_initialized)
                {
                    Console.WriteLine("Iniciando componente VirtualPrinterCore...");

                    // Crear la carpeta de logs al inicio de la aplicación
                    string logPath = Path.Combine(Application.StartupPath, LOG_FOLDER);
                    if (!Directory.Exists(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                        Console.WriteLine($"Carpeta de logs creada al inicializar: {logPath}");
                    }

                    // Verificar permisos de escritura creando un archivo de prueba
                    string testFile = Path.Combine(logPath, "permissions_test.txt");
                    File.WriteAllText(testFile, $"Test de permisos de escritura: {DateTime.Now}");
                    File.Delete(testFile); // Eliminar el archivo de prueba
                    Console.WriteLine("Verificación de permisos de escritura: OK");

                    // Crear un archivo de inicialización para confirmar que la carpeta funciona
                    string initLogFile = Path.Combine(logPath, "init_log.txt");
                    File.WriteAllText(initLogFile, $"[{DateTime.Now}] Carpeta de logs inicializada correctamente");

                    // NUEVO: Iniciar el BufferedPrintListener para detectar impresiones
                    try
                    {
                        var printListener = BufferedPrintListener.Instance;
                        File.AppendAllText(initLogFile, $"[{DateTime.Now}] BufferedPrintListener inicializado correctamente\r\n");
                    }
                    catch (Exception listenerEx)
                    {
                        File.AppendAllText(initLogFile, $"[{DateTime.Now}] Error al iniciar BufferedPrintListener: {listenerEx.Message}\r\n");
                    }

                    // Crear la carpeta fija para la salida de PDFs
                    EnsureOutputFolderExists();

                    _initialized = true;
                    Console.WriteLine("Componente VirtualPrinterCore inicializado correctamente");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al inicializar componente VirtualPrinterCore: {ex.Message}");
                Console.WriteLine($"Tipo de excepción: {ex.GetType().Name}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                try
                {
                    // Intentar crear en el escritorio como alternativa
                    string desktopPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "ECM_Logs");

                    if (!Directory.Exists(desktopPath))
                        Directory.CreateDirectory(desktopPath);

                    // Verificar permisos en el escritorio
                    string testDesktopFile = Path.Combine(desktopPath, "permissions_test.txt");
                    File.WriteAllText(testDesktopFile, $"Test de permisos en escritorio: {DateTime.Now}");
                    File.Delete(testDesktopFile);
                    Console.WriteLine("Verificación de permisos en escritorio: OK");

                    // Guardar un log de inicialización para confirmar que funciona
                    string initLogFile = Path.Combine(desktopPath, "init_log.txt");
                    File.WriteAllText(initLogFile, $"[{DateTime.Now}] Carpeta de logs inicializada correctamente en el escritorio");

                    Console.WriteLine($"Carpeta de logs creada en el escritorio: {desktopPath}");
                }
                catch (Exception desktopEx)
                {
                    Console.WriteLine($"Error crítico: Fallo al inicializar componente VirtualPrinterCore: {desktopEx.Message}");
                    // Si también falla, al menos lo registramos en la consola
                    Console.WriteLine("No se pudo crear ninguna carpeta de logs");
                }
            }
        }
        public static bool ConfigureAllCommonFolders()
        {
            try
            {
                string targetPath = FIXED_OUTPUT_PATH;

                // Asegurarse de que la carpeta existe y tiene los permisos correctos
                ConfigureFolderPermissions();

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders", true))
                {
                    if (key != null)
                    {
                        // Configurar todas las carpetas comunes
                        key.SetValue("Personal", targetPath); // Mis Documentos
                        key.SetValue("{F42EE2D3-909F-4907-8871-4C22FC0BF756}", targetPath); // Posible carpeta de PDF
                        key.SetValue("My Pictures", targetPath); // Mis Imágenes
                        key.SetValue("{374DE290-123F-4565-9164-39C4925E467B}", targetPath); // Descargas

                        Console.WriteLine("Múltiples carpetas predeterminadas configuradas");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar carpetas comunes: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Configura Microsoft Print to PDF para guardar automáticamente en la carpeta destino
        /// </summary>
        public static bool ConfigurePrintToPDFDefaultFolder()
        {
            try
            {
                string targetPath = FIXED_OUTPUT_PATH;

                // Asegurarse de que la carpeta existe
                Directory.CreateDirectory(targetPath);

                // Las claves del registro que controlan la ubicación predeterminada
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders";

                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyPath, true))
                {
                    if (key != null)
                    {
                        // Guardar la ubicación anterior
                        string previousPath = key.GetValue("{F42EE2D3-909F-4907-8871-4C22FC0BF756}") as string;
                        if (!string.IsNullOrEmpty(previousPath))
                        {
                            // Guardar como respaldo
                            key.SetValue("ECMCentral_PreviousPDFPath", previousPath);
                        }

                        // Establecer la nueva ubicación
                        key.SetValue("{F42EE2D3-909F-4907-8871-4C22FC0BF756}", targetPath);

                        Console.WriteLine($"Configuración de Microsoft Print to PDF actualizada para guardar en: {targetPath}");
                        return true;
                    }
                }

                // Si la clave principal no funciona, intentar con claves alternativas
                try
                {
                    // Intentar con PowerShell para mayor compatibilidad
                    string psCommand = $@"
            try {{
                $path = '{targetPath.Replace(@"\", @"\\")}'
                Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders' -Name '{{F42EE2D3-909F-4907-8871-4C22FC0BF756}}' -Value $path -Type String
                Write-Output 'Configuración actualizada mediante PowerShell'
                $true
            }} catch {{
                Write-Output $_.Exception.Message
                $false
            }}";

                    string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                    return result.Contains("Configuración actualizada");
                }
                catch (Exception psEx)
                {
                    Console.WriteLine($"Error al configurar con PowerShell: {psEx.Message}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar Microsoft Print to PDF: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Configura permisos en la carpeta de destino para permitir el acceso a todos los usuarios
        /// </summary>
        public static bool ConfigureFolderPermissions()
        {
            try
            {
                string folder = FIXED_OUTPUT_PATH;

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                try
                {
                    // Configurar los permisos usando PowerShell
                    string psCommand = $@"
$folder = '{folder.Replace(@"\", @"\\")}' 
$acl = Get-Acl $folder
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule('Users', 'FullControl', 'ContainerInherit, ObjectInherit', 'None', 'Allow')
$acl.SetAccessRule($rule)
Set-Acl $folder $acl
Write-Output 'Permisos configurados correctamente'";

                    string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);

                    // Verificar que la operación fue exitosa
                    if (result.Contains("Permisos configurados correctamente"))
                    {
                        Console.WriteLine($"Permisos de carpeta configurados: {folder}");
                        return true;
                    }
                }
                catch (Exception psEx)
                {
                    Console.WriteLine($"Error al configurar permisos con PowerShell: {psEx.Message}");

                    // Intentar método alternativo con Process si PowerShell falla
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = "icacls",
                            Arguments = $"\"{folder}\" /grant \"Users:(OI)(CI)F\" /T",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true
                        };

                        using (Process process = Process.Start(psi))
                        {
                            string output = process.StandardOutput.ReadToEnd();
                            process.WaitForExit();

                            Console.WriteLine($"Resultado de configuración de permisos con icacls: {output}");
                            return process.ExitCode == 0;
                        }
                    }
                    catch (Exception cmdEx)
                    {
                        Console.WriteLine($"Error al configurar permisos con icacls: {cmdEx.Message}");
                    }
                }

                // En última instancia, verificar simplemente que podemos escribir
                string testFile = Path.Combine(folder, "permissions_test.txt");
                File.WriteAllText(testFile, $"Test de permisos: {DateTime.Now}");
                File.Delete(testFile);
                Console.WriteLine("Verificación de permisos básicos: OK");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar permisos: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Configura la impresora Microsoft Print to PDF para guardar automáticamente sin diálogo
        /// </summary>
        public static bool ConfigurePrinterAutoSaveSettings()
        {
            try
            {
                string psCommand = $@"
        try {{
            # Intentar configurar preferencias de impresora para guardar sin diálogo
            $printerName = '{PRINTER_NAME}'
            $outputPath = '{FIXED_OUTPUT_PATH.Replace(@"\", @"\\")}'
            
            # Crear/modificar archivo de configuración si es posible
            $devmodeFile = [System.IO.Path]::Combine($env:TEMP, 'ecm_print_settings.dat')
            
            # Configurar impresora para guardar sin diálogo
            Set-PrintConfiguration -PrinterName $printerName -Color $false
            
            # Configuración directa de Microsoft Print to PDF si es posible
            $regPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders'
            if (Test-Path $regPath) {{
                $prevValue = (Get-ItemProperty -Path $regPath -Name 'Personal' -ErrorAction SilentlyContinue).Personal
                Set-ItemProperty -Path $regPath -Name 'Personal' -Value $outputPath
                Write-Output ""Configuración actualizada para carpeta Personal""
                
                # También configurar la ruta para Microsoft Print to PDF específicamente
                Set-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows NT\CurrentVersion\Windows' -Name 'Device' -Value 'Microsoft Print to PDF,winspool,Ne00:' -ErrorAction SilentlyContinue
            }}
            
            # Crear archivo de prueba en la carpeta de salida
            $testFile = Join-Path -Path $outputPath -ChildPath ""print_config_test.txt""
            Set-Content -Path $testFile -Value ""Prueba de configuración: $(Get-Date)""
            
            Write-Output ""Configuración de impresora actualizada correctamente""
            $true
        }}
        catch {{
            Write-Output $_.Exception.Message
            $false
        }}";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                return result.Contains("Configuración actualizada correctamente");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar opciones de guardado automático: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Aplica una configuración directa para Microsoft Print to PDF usando un script PowerShell elevado
        /// </summary>
        public static bool ApplyDirectDriverConfiguration()
        {
            try
            {
                string targetPath = FIXED_OUTPUT_PATH;

                // Asegurarse de que la carpeta existe
                if (!Directory.Exists(targetPath))
                {
                    Directory.CreateDirectory(targetPath);
                }

                // Este enfoque utiliza un script PowerShell más completo y con elevación si es necesario
                string scriptPath = Path.Combine(Path.GetTempPath(), "configure_pdf_printer.ps1");

                // Crear un script PowerShell para configurar Microsoft Print to PDF
                string scriptContent = $@"
# Script para configurar Microsoft Print to PDF para guardar sin diálogo
$outputFolder = '{FIXED_OUTPUT_PATH.Replace(@"\", @"\\")}'
$printerName = '{PRINTER_NAME}'

# Crear la carpeta si no existe
if (-not (Test-Path -Path $outputFolder)) {{
    New-Item -Path $outputFolder -ItemType Directory -Force
}}

# 1. Intentar configurar a través del registro de Windows
try {{
    # Configurar todas las ubicaciones conocidas de documentos
    $shellFolders = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Shell Folders'
    $userShellFolders = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders'
    
    # Configurar cada ubicación que Microsoft Print to PDF podría usar
    Set-ItemProperty -Path $shellFolders -Name 'Personal' -Value $outputFolder -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $userShellFolders -Name 'Personal' -Value $outputFolder -ErrorAction SilentlyContinue
    
    # GUID específicos conocidos
    Set-ItemProperty -Path $userShellFolders -Name '{{F42EE2D3-909F-4907-8871-4C22FC0BF756}}' -Value $outputFolder -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $shellFolders -Name '{{F42EE2D3-909F-4907-8871-4C22FC0BF756}}' -Value $outputFolder -ErrorAction SilentlyContinue
    
    # Configuración general de archivos recientes
    Set-ItemProperty -Path $shellFolders -Name 'Recent' -Value $outputFolder -ErrorAction SilentlyContinue
    Set-ItemProperty -Path $userShellFolders -Name 'Recent' -Value $outputFolder -ErrorAction SilentlyContinue
    
    # Configurar el puerto PORTPROMPT:
    $regDriversPath = 'HKLM:\SYSTEM\CurrentControlSet\Control\Print\Environments\Windows x64\Drivers\Version-3\Microsoft Print To PDF'
    if (Test-Path $regDriversPath) {{
        # Intento con permisos de administrador
        Set-ItemProperty -Path $regDriversPath -Name 'PrinterDriverAttributes' -Value 1 -ErrorAction SilentlyContinue
    }}
}} catch {{
    Write-Output ""Error en configuración de registro: $($_.Exception.Message)""
}}

# 2. Configurar la impresora directamente
try {{
    # Configurar la impresora Microsoft Print to PDF para no mostrar diálogo
    $printers = Get-Printer | Where-Object {{ $_.Name -eq $printerName }}
    if ($printers) {{
        # Configurar la impresora para guardar automáticamente
        $printer = $printers[0]
        
        # Usar PrintTicket XML para configurar la carpeta de salida
        $ticketXml = @""
<PrintTicket xmlns=""http://schemas.microsoft.com/windows/2003/08/printing/printticket"">
  <ParameterInit name=""FileNameSettings"">
    <StringParameter name=""DocumentNameExtension"">.pdf</StringParameter>
    <StringParameter name=""Directory"">{targetPath}</StringParameter>
    <StringParameter name=""PromptUser"">false</StringParameter>
  </ParameterInit>
</PrintTicket>
""@
        
        # Aplicar configuración
        Set-PrintConfiguration -PrinterName $printer.Name -PrintTicketXml $ticketXml -ErrorAction SilentlyContinue
        
        # Configurar la impresora por defecto temporalmente
        (New-Object -ComObject WScript.Network).SetDefaultPrinter($printerName)
        
        Write-Output ""Configuración de impresora aplicada correctamente""
    }}
}} catch {{
    Write-Output ""Error en configuración de impresora: $($_.Exception.Message)""
}}

# 3. Configurar permisos en la carpeta destino
try {{
    # Asegurarse que todos los usuarios pueden escribir
    $acl = Get-Acl -Path $outputFolder
    $allUsersRule = New-Object System.Security.AccessControl.FileSystemAccessRule(""Users"", ""FullControl"", ""ContainerInherit,ObjectInherit"", ""None"", ""Allow"")
    $acl.SetAccessRule($allUsersRule)
    Set-Acl -Path $outputFolder -AclObject $acl
    
    # Establecer AtributosPorDefecto para el registro
    $key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey(""Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"", $true)
    if ($key) {{
        $key.SetValue(""HideFileExt"", 0, [Microsoft.Win32.RegistryValueKind]::DWord)
        $key.SetValue(""Hidden"", 1, [Microsoft.Win32.RegistryValueKind]::DWord)
    }}
    
    Write-Output ""Permisos configurados correctamente""
}} catch {{
    Write-Output ""Error al configurar permisos: $($_.Exception.Message)""
}}

# Verificar si todos los pasos fueron exitosos
Write-Output ""Configuración completa de Microsoft Print to PDF para guardar en: $outputFolder""
";

                // Guardar el script en un archivo temporal
                File.WriteAllText(scriptPath, scriptContent);

                // Ejecutar el script PowerShell con elevación para obtener los permisos necesarios
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-ExecutionPolicy Bypass -NoProfile -File \"{scriptPath}\"",
                    UseShellExecute = true,
                    Verb = "runas", // Solicita elevación de permisos
                    CreateNoWindow = false // Mostramos la ventana para ver el progreso
                };

                using (Process process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        process.WaitForExit(30000); // Esperar hasta 30 segundos
                        Console.WriteLine("Script de configuración avanzada ejecutado");

                        // Si el proceso finalizó correctamente
                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("Configuración de Microsoft Print to PDF completada exitosamente");

                            // Verificar que el directorio existe y tiene los permisos correctos
                            if (Directory.Exists(targetPath))
                            {
                                // Intentar escribir un archivo de prueba
                                string testFile = Path.Combine(targetPath, "config_test.txt");
                                try
                                {
                                    File.WriteAllText(testFile, $"Configuración verificada: {DateTime.Now}");
                                    if (File.Exists(testFile))
                                    {
                                        File.Delete(testFile);
                                        return true;
                                    }
                                }
                                catch
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al aplicar configuración directa: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Prepara el sistema para la impresión asegurando que el monitor en segundo plano está activo
        /// </summary>
        public static bool PrepareForPrinting()
        {
            try
            {
                WatcherLogger.LogActivity("Preparando sistema para impresión");

                // Asegurar que la carpeta de salida existe
                EnsureOutputFolderExists();

                // Iniciar el monitor en segundo plano si no está activo
                if (!WinFormsApiClient.VirtualWatcher.ApplicationLauncher.StartBackgroundMonitorSilently())
                {
                    Console.WriteLine("No se pudo iniciar el monitor en segundo plano");
                    return false;
                }

                WatcherLogger.LogActivity("Sistema listo para impresión");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al preparar sistema para impresión: {ex.Message}");
                return false;
            }
        }
        public static bool SetupPrinterConfiguration()
        {
            try
            {
                // Asegurar que la carpeta existe
                EnsureOutputFolderExists();

                // Solo configuramos Bullzip, eliminando todas las demás opciones
                Console.WriteLine("Configurando exclusivamente Bullzip PDF Printer...");
                WatcherLogger.LogActivity("Configurando Bullzip PDF Printer como única opción");

                // Ejecutar configuración de Bullzip
                bool success = ConfigureBullzipPrinter();

                if (success)
                {
                    WatcherLogger.LogActivity("✓ Bullzip PDF Printer configurado correctamente");

                    // Crear archivo de instrucciones
                    string instructionsPath = Path.Combine(FIXED_OUTPUT_PATH, "INSTRUCCIONES_BULLZIP.txt");
                    File.WriteAllText(instructionsPath,
                        $"INSTRUCCIONES PARA USAR BULLZIP PDF PRINTER\r\n" +
                        $"=======================================\r\n\r\n" +
                        $"La impresora está configurada para guardar automáticamente en:\r\n" +
                        $"{FIXED_OUTPUT_PATH}\r\n\r\n" +
                        $"No se necesita ninguna acción adicional, solo seleccione 'ECM Central Printer' al imprimir.\r\n" +
                        $"Los archivos se guardarán automáticamente en la carpeta indicada.");

                    return true;
                }
                else
                {
                    WatcherLogger.LogError("No se pudo configurar Bullzip PDF Printer",
                        new Exception("Fallo en configuración de impresora"));
                    return false;
                }
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al configurar la impresora", ex);
                return false;
            }
        }
        /// <summary>
        /// Configura el controlador de impresión para forzar la salida del archivo
        /// </summary>
        public static bool ForceDirectPrintOutput()
        {
            try
            {
                string outputFolder = FIXED_OUTPUT_PATH;

                // Asegurar que la carpeta existe
                if (!Directory.Exists(outputFolder))
                {
                    Directory.CreateDirectory(outputFolder);
                }

                // 1. ENFOQUE DIRECTO: Crear un archivo PrinterPort.ini
                // Algunas versiones de Microsoft Print to PDF respetan este archivo
                string tempFolder = Path.GetTempPath();
                string printerPortIni = Path.Combine(tempFolder, "PrinterPort.ini");

                string iniContent = $@"[Port]
PortName=PORTPROMPT:
FileName={outputFolder}\%USERNAME%-%DOCUMENTNAME%-%TIME:~0,2%%TIME:~3,2%%TIME:~6,2%.pdf
Prompt=0
";
                File.WriteAllText(printerPortIni, iniContent);

                // 2. Crear script que ejecuta PDFtoPrinter.exe (herramienta de línea de comandos)
                // Esta es una alternativa que utiliza una herramienta externa
                string batchContent = $@"@echo off
REM Configuración de impresión directa para Microsoft Print to PDF
SET OUTPUT_DIR={outputFolder}
SET TIMESTAMP=%date:~6,4%%date:~3,2%%date:~0,2%_%time:~0,2%%time:~3,2%%time:~6,2%
SET TIMESTAMP=%TIMESTAMP: =0%

REM Iniciar un monitor de procesos para detectar el diálogo de Microsoft Print to PDF
PowerShell -WindowStyle Hidden -Command ""
Add-Type -AssemblyName System.Windows.Forms
while ($true) {{
    $dialog = Get-Process | Where-Object {{ $_.MainWindowTitle -like '*Microsoft Print to PDF*' }}
    if ($dialog) {{
        [System.Windows.Forms.SendKeys]::SendWait('{FIXED_OUTPUT_PATH.Replace(@"\", @"\\")}' + '\documento_' + (Get-Date -Format 'yyyyMMdd_HHmmss') + '.pdf')
        Start-Sleep -Milliseconds 500
        [System.Windows.Forms.SendKeys]::SendWait('{{ENTER}}')
        break
    }}
    Start-Sleep -Milliseconds 100
}}
""

echo Monitor de diálogo PDF iniciado correctamente.
";

                // Guardar el script batch
                string batchPath = Path.Combine(outputFolder, "pdf_monitor.bat");
                File.WriteAllText(batchPath, batchContent);

                // 3. Crear un archivo de ayuda
                string helpContent = $@"
=== CONFIGURACIÓN PARA MICROSOFT PRINT TO PDF ===

Para evitar que aparezca el diálogo de guardar:

1. Si aparece la ventana para guardar, seleccione esta carpeta:
   {outputFolder}

2. Si desea que se guarde automáticamente, ejecute el archivo:
   {batchPath}
   
3. Los archivos se procesarán automáticamente una vez guardados.
";

                string helpPath = Path.Combine(outputFolder, "AYUDA_PRINTING.txt");
                File.WriteAllText(helpPath, helpContent);

                // 4. Configurar un watcher adicional para estar más seguros
                // Modificar PDFDialogAutomation para que también monitoree este diálogo
                PDFDialogAutomation.MonitorDialogsForAllUsers();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al forzar salida directa: {ex.Message}");
                return false;
            }
        }
        public static bool ConfigureBullzipPrinter()
{
    try
    {
        string outputFolder = FIXED_OUTPUT_PATH;

        // Crear archivo de log para diagnóstico
        string printLogPath = Path.Combine(FIXED_OUTPUT_PATH, "print_hook_logs");
        if (!Directory.Exists(printLogPath))
            Directory.CreateDirectory(printLogPath);

        string hookLog = Path.Combine(printLogPath, "bullzip_hook.log");
        File.AppendAllText(hookLog, $"[{DateTime.Now}] Configurando Bullzip para la impresión\r\n");

        // Asegurar que la carpeta de salida existe
        if (!Directory.Exists(outputFolder))
            Directory.CreateDirectory(outputFolder);

        Console.WriteLine($"Configurando Bullzip PDF Printer para carpeta: {outputFolder}");
        WatcherLogger.LogActivity($"Configurando Bullzip PDF Printer para carpeta: {outputFolder}");

        // MODIFICADO: Usar el VBScript en lugar de crear batch aquí
        // 1. Crear/verificar que existe el VBScript en PDFDialogAutomation
        bool vbsCreated = PDFDialogAutomation.CreateSilentVBScript();
        if (!vbsCreated)
        {
            File.AppendAllText(hookLog, $"[{DateTime.Now}] Error: No se pudo crear el VBScript\r\n");
            return false;
        }

        // 2. Obtener la ruta del VBScript
        string vbsPath = Path.Combine(outputFolder, "SilentLauncherForMoverPDFs.vbs");
        File.AppendAllText(hookLog, $"[{DateTime.Now}] VBScript creado/verificado: {vbsPath}\r\n");

        // 3. Configurar Bullzip para que use el VBScript
        string bullzipConfigFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Bullzip", "PDF Printer");

        if (!Directory.Exists(bullzipConfigFolder))
            Directory.CreateDirectory(bullzipConfigFolder);

        string iniPath = Path.Combine(bullzipConfigFolder, "settings.ini");

        StringBuilder iniContent = new StringBuilder();
        iniContent.AppendLine("[PDF Printer]");
        iniContent.AppendLine($"Output={outputFolder}");
        iniContent.AppendLine("ShowSaveAS=never");
        iniContent.AppendLine("ShowSettings=never");
        iniContent.AppendLine("ShowProgress=no");
        iniContent.AppendLine("ShowProgressFinished=no");
        iniContent.AppendLine("ConfirmOverwrite=no");
        iniContent.AppendLine("OpenViewer=no");

        // CLAVE: Usar el VBScript en lugar del batch directo
        iniContent.AppendLine($"RunOnSuccess=\"wscript.exe \\\"{vbsPath}\\\"\"");
        iniContent.AppendLine("RunOnSuccessParameters=\"%s\"");

        // También ejecutar al INICIO del trabajo para mayor seguridad
        iniContent.AppendLine("RunOnPrinterEvent=1");
        iniContent.AppendLine($"RunPrinterEvent=\"wscript.exe \\\"{vbsPath}\\\"\"");
        iniContent.AppendLine("RunPrinterEventTiming=before");

        iniContent.AppendLine($"FilenameTemplate=ECM_{DateTime.Now:yyyyMMdd}_<time:HHmmss>");
        iniContent.AppendLine("RememberLastFolders=no");
        iniContent.AppendLine("RememberLastName=no");
        iniContent.AppendLine("RetainDialog=no");
        iniContent.AppendLine("DefaultSaveSettings=yes");
        iniContent.AppendLine("Debug=1"); // Activar modo debug de Bullzip
        iniContent.AppendLine($"DebugFile={outputFolder}\\logs\\bullzip_debug.log");

        File.WriteAllText(iniPath, iniContent.ToString());
        File.AppendAllText(hookLog, $"[{DateTime.Now}] Configuración de Bullzip actualizada: {iniPath}\r\n");

        // 4. Asegurar que el VBScript tiene los permisos necesarios para ejecutarse
        try
        {
            EnsureScriptPermissions(vbsPath);
            File.AppendAllText(hookLog, $"[{DateTime.Now}] Permisos aplicados al VBScript\r\n");
        }
        catch (Exception permEx)
        {
            File.AppendAllText(hookLog, $"[{DateTime.Now}] Error al aplicar permisos: {permEx.Message}\r\n");
        }

        // 5. Crear instrucciones actualizadas
        string instructionsPath = Path.Combine(outputFolder, "INSTRUCCIONES.txt");
        string instructions = $@"INSTRUCCIONES PARA USAR ECM CENTRAL CON BULLZIP
=============================================

1. Para imprimir documentos:
   - Seleccione 'ECM Central Printer' o 'Bullzip PDF Printer' al imprimir
   - El archivo PDF se guardará automáticamente en esta carpeta
   - La aplicación ECM Central procesará automáticamente el archivo

2. Sistema de procesamiento:
   - Bullzip ejecutará automáticamente: {vbsPath}
   - El VBScript iniciará el monitor de PDFs silenciosamente
   - Los archivos se procesarán automáticamente

3. Si necesita procesar manualmente un archivo PDF:
   - Colóquelo en esta carpeta ({outputFolder})
   - La aplicación lo detectará automáticamente

4. Si encuentra algún problema:
   - Ejecute manualmente el archivo VBScript: {vbsPath}

Configurado el {DateTime.Now}
";
        File.WriteAllText(instructionsPath, instructions);

        Console.WriteLine("✓ Configuración de Bullzip completada con éxito usando VBScript");
        WatcherLogger.LogActivity("Configuración de Bullzip completada con éxito usando VBScript");

        return true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al configurar Bullzip: {ex.Message}");
        WatcherLogger.LogError("Error al configurar Bullzip", ex);
        return false;
    }
}
        /// <summary>
        /// Implementa soluciones para garantizar que el monitor se inicie con Bullzip
        /// sin entrar en bucles infinitos
        /// </summary>
        public static bool EnsureMonitorForBullzip()
        {
            try
            {
                WatcherLogger.LogActivity("Configurando inicio automático de monitor para Bullzip");

                // Verificar si el monitor ya está activo
                string diagFile = Path.Combine(FIXED_OUTPUT_PATH, "bullzip_monitor_config.log");
                File.AppendAllText(diagFile, $"[{DateTime.Now}] Iniciando configuración de monitor para Bullzip\r\n");

                bool monitorActivo = BackgroundMonitorService.IsActuallyRunning();
                File.AppendAllText(diagFile, $"[{DateTime.Now}] Monitor activo según IsActuallyRunning(): {monitorActivo}\r\n");

                if (monitorActivo)
                {
                    WatcherLogger.LogActivity("El monitor ya está activo - no es necesario iniciarlo");
                    File.AppendAllText(diagFile, $"[{DateTime.Now}] No se iniciará otra instancia porque ya hay una activa\r\n");
                }
                else
                {
                    // Iniciar el monitor de manera directa y confiable
                    File.AppendAllText(diagFile, $"[{DateTime.Now}] Iniciando monitor directamente...\r\n");

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = Application.ExecutablePath,
                        Arguments = "/backgroundmonitor /silent",
                        UseShellExecute = true,
                        WindowStyle = ProcessWindowStyle.Minimized
                    };

                    Process monitorProcess = Process.Start(startInfo);

                    if (monitorProcess != null)
                    {
                        File.AppendAllText(diagFile, $"[{DateTime.Now}] Monitor iniciado con PID: {monitorProcess.Id}\r\n");
                        WatcherLogger.LogActivity($"✓ Monitor iniciado directamente con éxito.");
                    }
                    else
                    {
                        File.AppendAllText(diagFile, $"[{DateTime.Now}] Error al iniciar monitor directamente\r\n");
                        WatcherLogger.LogError("No se pudo iniciar el monitor directamente", null);
                    }
                }

                // Crear script anti-bucle para configurar con Bullzip
                string outputFolder = FIXED_OUTPUT_PATH;
                string startScriptPath = Path.Combine(outputFolder, "ecm_monitor_start.bat");

                // Script mejorado que evita bucles y es más robusto
                string scriptContent = @"@echo off
setlocal EnableDelayedExpansion

REM Script para iniciar monitor ECM Central - Versión Anti-Bucle
echo [%date% %time%] Iniciando script ecm_monitor_start.bat > ""%TEMP%\ecm_script.log""

REM Comprobar si hay un marcador de monitor activo
set MARKER_FILE=""%TEMP%\ecm_monitor_running.marker""
if exist %MARKER_FILE% (
    echo [%date% %time%] Marcador encontrado, comprobando si el proceso existe >> ""%TEMP%\ecm_script.log""
    
    REM Leer el PID del archivo
    set /p MONITOR_PID=<%MARKER_FILE%
    
    REM Verificar si ese proceso existe
    tasklist /FI ""PID eq !MONITOR_PID!"" /FI ""IMAGENAME eq WinFormsApiClient.exe"" 2>NUL | find ""!MONITOR_PID!"" > NUL
    if !ERRORLEVEL! == 0 (
        echo [%date% %time%] Monitor ya activo (PID: !MONITOR_PID!), saliendo >> ""%TEMP%\ecm_script.log""
        goto :EOF
    ) else (
        echo [%date% %time%] Monitor no encontrado, el marcador está obsoleto >> ""%TEMP%\ecm_script.log""
    )
)

REM Verificar también mediante la búsqueda general de procesos
for /f ""tokens=1"" %%p in ('wmic process where ""name='WinFormsApiClient.exe' and CommandLine like '%%backgroundmonitor%%'"" get ProcessId 2^>NUL') do (
    if not ""%%p""==""ProcessId"" (
        echo [%date% %time%] Monitor ya en ejecución (PID: %%p) >> ""%TEMP%\ecm_script.log""
        goto :EOF
    )
)

REM Si llegamos aquí, no hay monitor en ejecución, así que lo iniciamos
echo [%date% %time%] Iniciando monitor... >> ""%TEMP%\ecm_script.log""
start """" /min ""[EXECUTABLE_PATH]"" /backgroundmonitor /silent

exit /b 0
";

                // Reemplazar el marcador con la ruta real del ejecutable
                scriptContent = scriptContent.Replace("[EXECUTABLE_PATH]", Application.ExecutablePath);
                File.WriteAllText(startScriptPath, scriptContent);
                File.AppendAllText(diagFile, $"[{DateTime.Now}] Script anti-bucle creado en: {startScriptPath}\r\n");

                // Dar permisos adecuados al script
                EnsureScriptPermissions(startScriptPath);

                // Configurar Bullzip para usar este script
                string bullzipConfigFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Bullzip", "PDF Printer");

                if (!Directory.Exists(bullzipConfigFolder))
                    Directory.CreateDirectory(bullzipConfigFolder);

                string iniPath = Path.Combine(bullzipConfigFolder, "settings.ini");

                // Leer la configuración actual o crear nueva
                if (File.Exists(iniPath))
                {
                    // Actualizar configuración existente
                    string iniContent = File.ReadAllText(iniPath);

                    // Configurar RunOnStart - evitar comandos que puedan entrar en bucle
                    if (iniContent.Contains("RunOnStart="))
                    {
                        iniContent = System.Text.RegularExpressions.Regex.Replace(
                            iniContent,
                            @"RunOnStart=.*\r?\n",
                            $"RunOnStart=\"{startScriptPath}\"\r\n");
                    }
                    else if (iniContent.Contains("[PDF Printer]"))
                    {
                        // Insertar después de la sección [PDF Printer]
                        int pos = iniContent.IndexOf("[PDF Printer]") + "[PDF Printer]".Length;
                        iniContent = iniContent.Insert(pos, $"\r\nRunOnStart=\"{startScriptPath}\"\r\n");
                    }
                    else
                    {
                        // Agregar nueva sección
                        iniContent += $"\r\n[PDF Printer]\r\nRunOnStart=\"{startScriptPath}\"\r\n";
                    }

                    // Desactivar cualquier timer o polling que pueda causar bucles
                    if (iniContent.Contains("PollInterval="))
                    {
                        iniContent = System.Text.RegularExpressions.Regex.Replace(
                            iniContent,
                            @"PollInterval=.*\r?\n",
                            "PollInterval=0\r\n");
                    }

                    File.WriteAllText(iniPath, iniContent);
                }
                else
                {
                    // Crear configuración nueva
                    string iniContent = $"[PDF Printer]\r\nRunOnStart=\"{startScriptPath}\"\r\nOutput={outputFolder}\r\n";
                    File.WriteAllText(iniPath, iniContent);
                }

                File.AppendAllText(diagFile, $"[{DateTime.Now}] Configuración de Bullzip actualizada para uso de script anti-bucle\r\n");

                // También crear un script post-procesamiento
                string postScriptPath = Path.Combine(outputFolder, "ecm_post_process.bat");
                string postScriptContent = $@"@echo off
REM Script de post-procesamiento para Bullzip
echo [%date% %time%] Post-procesando archivo: %%1 > ""{Path.Combine(outputFolder, "bullzip_post.log")}""

REM Verificar si el archivo existe
if not exist ""%%1"" (
  echo Archivo no encontrado: %%1 >> ""{Path.Combine(outputFolder, "bullzip_post.log")}""
  exit /b 1
)

REM Esperar a que el archivo se complete (importante)
timeout /t 4 /nobreak > nul

REM Verificar que el archivo ya no está bloqueado
set ATTEMPTS=0
:CHECK_FILE_LOCK
set /a ATTEMPTS+=1
if %ATTEMPTS% GTR 15 goto PROCESS_ANYWAY

REM Intentar abrir el archivo para verificar si está bloqueado
copy ""%%1"" ""%%1.test"" > nul 2>&1
if not exist ""%%1.test"" (
  echo Intento %ATTEMPTS%: Archivo bloqueado, esperando... >> ""{Path.Combine(outputFolder, "bullzip_post.log")}""
  timeout /t 2 /nobreak > nul
  goto CHECK_FILE_LOCK
) else (
  del ""%%1.test"" > nul 2>&1
  echo Archivo listo para procesamiento >> ""{Path.Combine(outputFolder, "bullzip_post.log")}""
)

:PROCESS_ANYWAY
REM Procesar el archivo PDF mediante la aplicación principal
echo Iniciando procesamiento del archivo >> ""{Path.Combine(outputFolder, "bullzip_post.log")}""
start """" /b ""{Application.ExecutablePath}"" /silentprint=""%%1""

exit /b 0
";
                File.WriteAllText(postScriptPath, postScriptContent);
                EnsureScriptPermissions(postScriptPath);
                File.AppendAllText(diagFile, $"[{DateTime.Now}] Script de post-procesamiento creado en: {postScriptPath}\r\n");

                // Actualizar configuración de Bullzip para post-procesamiento
                if (File.Exists(iniPath))
                {
                    string iniContent = File.ReadAllText(iniPath);

                    // Actualizar RunOnSuccess
                    if (iniContent.Contains("RunOnSuccess="))
                    {
                        iniContent = System.Text.RegularExpressions.Regex.Replace(
                            iniContent,
                            @"RunOnSuccess=.*\r?\n",
                            $"RunOnSuccess=\"{postScriptPath}\"\r\n");
                    }
                    else if (iniContent.Contains("[PDF Printer]"))
                    {
                        int pos = iniContent.IndexOf("[PDF Printer]") + "[PDF Printer]".Length;
                        iniContent = iniContent.Insert(pos, $"\r\nRunOnSuccess=\"{postScriptPath}\"\r\n");
                    }

                    File.WriteAllText(iniPath, iniContent);
                    File.AppendAllText(diagFile, $"[{DateTime.Now}] Configuración de post-procesamiento actualizada\r\n");
                }

                // Crear también un mecanismo de monitoreo de archivos
                ConfigureBullzipFileWatcher(outputFolder);

                // Asegurar que el monitor esté activo si no se pudo iniciar antes
                if (!monitorActivo && !BackgroundMonitorService.IsActuallyRunning())
                {
                    try
                    {
                        WatcherLogger.LogActivity("El monitor sigue inactivo, intentando métodos alternativos");
                        File.AppendAllText(diagFile, $"[{DateTime.Now}] Intentando métodos alternativos para iniciar monitor\r\n");

                        // Último intento como administrador si es posible
                        if (IsAdministrator())
                        {
                            ProcessStartInfo elevatedStartInfo = new ProcessStartInfo
                            {
                                FileName = Application.ExecutablePath,
                                Arguments = "/backgroundmonitor",
                                UseShellExecute = true,
                                Verb = "runas"
                            };

                            Process.Start(elevatedStartInfo);
                            File.AppendAllText(diagFile, $"[{DateTime.Now}] Monitor iniciado con privilegios elevados\r\n");
                        }
                        else
                        {
                            BackgroundMonitorService.Start();
                            File.AppendAllText(diagFile, $"[{DateTime.Now}] Monitor iniciado mediante llamada directa a BackgroundMonitorService.Start()\r\n");
                        }
                    }
                    catch (Exception ex)
                    {
                        WatcherLogger.LogError("Error en métodos alternativos para iniciar monitor", ex);
                        File.AppendAllText(diagFile, $"[{DateTime.Now}] Error en métodos alternativos: {ex.Message}\r\n");
                    }
                }

                WatcherLogger.LogActivity("Configuración de monitor para Bullzip completada");
                File.AppendAllText(diagFile, $"[{DateTime.Now}] Configuración completada exitosamente\r\n");

                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    string errorLog = Path.Combine(FIXED_OUTPUT_PATH, "bullzip_monitor_error.log");
                    File.AppendAllText(errorLog,
                        $"[{DateTime.Now}] Error en EnsureMonitorForBullzip: {ex.Message}\r\n" +
                        $"Stack trace: {ex.StackTrace}\r\n");

                    WatcherLogger.LogError("Error en EnsureMonitorForBullzip", ex);
                }
                catch { /* Si ni siquiera podemos loguear, no hay mucho que hacer */ }

                // Intento de emergencia para iniciar el monitor
                try
                {
                    Process.Start(Application.ExecutablePath, "/backgroundmonitor");
                }
                catch { }

                return false;
            }
        }
        /// <summary>
        /// Asegura que un script tenga los permisos adecuados para su ejecución
        /// </summary>
        /// <param name="scriptPath">Ruta al script que necesita permisos</param>
        public static void EnsureScriptPermissions(string scriptPath)
        {
            try
            {
                // Verificar que el archivo existe
                if (!File.Exists(scriptPath))
                {
                    WatcherLogger.LogError($"No se puede establecer permisos para un archivo inexistente: {scriptPath}");
                    return;
                }

                // Establecer ACL para el script usando un comando PowerShell
                string psCommand = $@"
try {{
    $acl = Get-Acl '{scriptPath.Replace("\\", "\\\\")}'
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule('Everyone', 'FullControl', 'Allow')
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path '{scriptPath.Replace("\\", "\\\\")}' -AclObject $acl
    Write-Output 'Permisos establecidos correctamente'
}} catch {{
    Write-Output ""Error: $($_.Exception.Message)""
}}";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);

                // Verificar éxito
                if (result.Contains("correctamente"))
                {
                    WatcherLogger.LogActivity($"Permisos establecidos correctamente para: {scriptPath}");
                }
                else
                {
                    WatcherLogger.LogError($"Error al establecer permisos: {result}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar permisos del script: {ex.Message}");
                WatcherLogger.LogError("Error al establecer permisos de script", ex);
            }
        }
        public static bool ConfigureDirectBullzipStartTrigger()
        {
            try
            {
                // Ruta a la configuración de Bullzip
                string bullzipConfigFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Bullzip", "PDF Printer");

                // Crear la carpeta si no existe
                if (!Directory.Exists(bullzipConfigFolder))
                    Directory.CreateDirectory(bullzipConfigFolder);

                string iniPath = Path.Combine(bullzipConfigFolder, "settings.ini");

                // Crear un script pequeño que inicie nuestro monitor
                string startScriptPath = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "start_monitor.bat");
                string startScriptContent = $@"@echo off
REM Script de activación automática para el monitor de ECM Central
echo Iniciando monitor para Bullzip > ""{Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "bullzip_trigger.log")}""
start """" ""{Application.ExecutablePath}"" /backgroundmonitor /silent
exit
";
                File.WriteAllText(startScriptPath, startScriptContent);

                // Leer la configuración actual o crear nueva
                string iniContent;
                if (File.Exists(iniPath))
                {
                    iniContent = File.ReadAllText(iniPath);

                    // Reemplazar o agregar las configuraciones necesarias
                    if (iniContent.Contains("[PDF Printer]"))
                    {
                        // Si ya existe la sección, actualizar las entradas
                        var lines = iniContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                        // Buscar o agregar RunOnStart
                        bool foundRunOnStart = false;
                        for (int i = 0; i < lines.Count; i++)
                        {
                            if (lines[i].StartsWith("RunOnStart="))
                            {
                                lines[i] = $"RunOnStart=\"{startScriptPath}\"";
                                foundRunOnStart = true;
                                break;
                            }
                        }

                        if (!foundRunOnStart)
                        {
                            // Agregar después de la sección [PDF Printer]
                            int sectionIndex = lines.IndexOf("[PDF Printer]");
                            if (sectionIndex >= 0)
                                lines.Insert(sectionIndex + 1, $"RunOnStart=\"{startScriptPath}\"");
                        }

                        iniContent = string.Join(Environment.NewLine, lines);
                    }
                    else
                    {
                        // Si no existe la sección, agregarla
                        iniContent += $"\r\n[PDF Printer]\r\nRunOnStart=\"{startScriptPath}\"\r\n";
                    }
                }
                else
                {
                    // Crear una nueva configuración básica
                    iniContent = $"[PDF Printer]\r\nRunOnStart=\"{startScriptPath}\"\r\nOutput={VirtualPrinterCore.FIXED_OUTPUT_PATH}\r\n";
                }

                // Guardar la configuración modificada
                File.WriteAllText(iniPath, iniContent);

                // Registrar la acción realizada
                WatcherLogger.LogActivity($"Configurado Bullzip para iniciar automáticamente el monitor: {startScriptPath}");

                return true;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error al configurar inicio automático en Bullzip", ex);
                return false;
            }
        }
        public static bool ConfigureBullzipRegistry()
        {
            try
            {
                // Crear nuestro script que inicie el monitor
                string monitorScript = Path.Combine(VirtualPrinterCore.FIXED_OUTPUT_PATH, "bullzip_monitor_trigger.bat");
                string scriptContent = $@"@echo off
start """" /min ""{Application.ExecutablePath}"" /backgroundmonitor /silent
exit
";
                File.WriteAllText(monitorScript, scriptContent);

                // Modificar registro para que Bullzip ejecute nuestro script automáticamente
                string psCommand = $@"
try {{
    # Encontrar la clave de registro para Bullzip
    $regPath = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall' | 
               Where-Object {{ $_.GetValue('DisplayName') -like '*Bullzip*' -or $_.GetValue('DisplayName') -like '*PDF Printer*' }} |
               Select-Object -ExpandProperty PSPath
    
    # Si no encontramos la clave, buscar en 32-bit
    if (!$regPath) {{
        $regPath = Get-ChildItem 'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall' | 
                  Where-Object {{ $_.GetValue('DisplayName') -like '*Bullzip*' -or $_.GetValue('DisplayName') -like '*PDF Printer*' }} |
                  Select-Object -ExpandProperty PSPath
    }}
    
    # Ubicación del script a ejecutar
    $scriptPath = '{monitorScript.Replace("\\", "\\\\")}'
    
    # Si encontramos la clave, configurar el inicio automático
    if ($regPath) {{
        # Obtener el directorio de instalación
        $installDir = (Get-ItemProperty -Path $regPath -Name 'InstallLocation').InstallLocation
        
        # Encontrar carpeta con settings.bin y modificar
        $settingsFolder = Join-Path -Path $installDir -ChildPath 'scripts'
        
        # Crear archivo de configuración de postscript
        $configFile = Join-Path -Path $settingsFolder -ChildPath 'onDocumentOpen.ps'
        $configContent = @""
%!PS
% Script que se ejecuta al abrir cada documento
(Iniciando monitor ECM) print
($scriptPath) system
% Continuamos con el procesamiento normal
""@
        
        Set-Content -Path $configFile -Value $configContent
        
        # Configurar en el registro para que use este script
        New-ItemProperty -Path $regPath -Name 'AutoStartMonitor' -Value 1 -PropertyType DWORD -Force
        
        Write-Output ""Configuración de Bullzip completada exitosamente""
        return $true
    }}
    
    Write-Output ""No se encontró la instalación de Bullzip""
    return $false
}}
catch {{
    Write-Output ""Error: $($_.Exception.Message)""
    return $false
}}";

                string result = PowerShellHelper.RunPowerShellCommandWithOutput(psCommand);
                bool success = result.Contains("exitosamente");

                if (success)
                {
                    WatcherLogger.LogActivity("Configuración de registro de Bullzip actualizada correctamente");
                }
                else
                {
                    WatcherLogger.LogError("Error al configurar registro de Bullzip", new Exception(result));
                }

                return success;
            }
            catch (Exception ex)
            {
                WatcherLogger.LogError("Error crítico al configurar Bullzip", ex);
                return false;
            }
        }

        /// <summary>
        /// Verifica que el monitor de fondo esté activo y lo inicia si es necesario
        /// </summary>
        public static bool EnsureBackgroundMonitorActive()
        {
            try
            {
                // Verificar si el monitor ya está activo
                if (WinFormsApiClient.VirtualWatcher.BackgroundMonitorService._isRunning)
                {
                    return true;
                }

                // Iniciar el monitor silenciosamente
                return WinFormsApiClient.VirtualWatcher.ApplicationLauncher.StartBackgroundMonitorSilently();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar/iniciar monitor de fondo: {ex.Message}");
                return false;
            }
        }
        /// <summary>
        /// Configura un FileSystemWatcher para detectar nuevos archivos creados por Bullzip
        /// </summary>
        private static void ConfigureBullzipFileWatcher(string folderPath)
        {
            try
            {
                // Crear directorio si no existe
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                // Crear un FileSystemWatcher para la carpeta de salida de Bullzip
                FileSystemWatcher watcher = new FileSystemWatcher
                {
                    Path = folderPath,
                    Filter = "*.pdf",
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                    EnableRaisingEvents = true
                };

                // Agregar manejador de eventos para nuevos archivos
                watcher.Created += BullzipPDF_FileCreated;

                Console.WriteLine($"FileSystemWatcher configurado para la carpeta Bullzip: {folderPath}");
                WatcherLogger.LogActivity($"Monitor de archivos Bullzip configurado para: {folderPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al configurar monitor de archivos Bullzip: {ex.Message}");
                WatcherLogger.LogError("Error al configurar watcher de Bullzip", ex);
            }
        }
        /// <summary>
        /// Manejador de eventos que se activa cuando se crea un nuevo archivo PDF
        /// </summary>
        private static void BullzipPDF_FileCreated(object sender, FileSystemEventArgs e)
        {
            try
            {
                // Reducir logs - solo registrar detecciones significativas
                Console.WriteLine($"PDF detectado: {Path.GetFileName(e.FullPath)}");

                // Esperar más tiempo para asegurar que el archivo está completo
                System.Threading.Thread.Sleep(2000);

                // Verificar que el archivo exista
                if (!File.Exists(e.FullPath))
                {
                    return;
                }

                // Verificar tamaño
                var fileInfo = new FileInfo(e.FullPath);
                if (fileInfo.Length == 0)
                {
                    return;
                }

                // Si el archivo está en uso, programar verificaciones periódicas
                if (!FileMonitor.IsFileReady(e.FullPath))
                {
                    ScheduleFileCheck(e.FullPath);
                    return;
                }

                // Si llegamos aquí, el archivo está listo para procesarse
                WatcherLogger.LogActivity($"Archivo PDF listo: {Path.GetFileName(e.FullPath)}");

                // Verificar si ya hay una instancia en ejecución
                bool shouldLaunchApp = true;
                try
                {
                    Process[] existingProcesses = Process.GetProcessesByName("WinFormsApiClient");
                    if (existingProcesses.Length > 1) // Si hay más de una instancia (contando la actual)
                    {
                        shouldLaunchApp = false;
                    }
                }
                catch (Exception)
                {
                    // Ignorar errores de verificación de procesos
                }

                // Procesar el archivo
                if (shouldLaunchApp)
                {
                    LaunchApplicationWithFile(e.FullPath);
                }
                else
                {
                    try
                    {
                        WinFormsApiClient.VirtualWatcher.DocumentProcessor.Instance.ProcessNewPrintJob(e.FullPath);
                    }
                    catch (Exception)
                    {
                        // Como alternativa, intentar lanzar la aplicación de todos modos
                        LaunchApplicationWithFile(e.FullPath);
                    }
                }
            }
            catch (Exception ex)
            {
                // Reducir verbosidad de logs
                Console.WriteLine($"Error al procesar PDF: {ex.Message}");

                // Intento de recuperación - intentar procesar el archivo de todos modos
                try
                {
                    if (File.Exists(e.FullPath) && new FileInfo(e.FullPath).Length > 0)
                    {
                        // Esperar un poco más
                        System.Threading.Thread.Sleep(3000);
                        LaunchApplicationWithFile(e.FullPath);
                    }
                }
                catch { /* Ignorar errores en recuperación */ }
            }
        }

        // Planificar verificación posterior para cuando el archivo esté listo
        private static readonly Dictionary<string, System.Windows.Forms.Timer> _pendingFiles =
            new Dictionary<string, System.Windows.Forms.Timer>();
        public static void CreatePendingFileMarker(string filePath)
        {
            try
            {
                // Asegurarse que la carpeta existe
                EnsureOutputFolderExists();

                // Crear el archivo marcador con la ruta completa
                string markerFile = Path.Combine(FIXED_OUTPUT_PATH, "pending_pdf.marker");
                File.WriteAllText(markerFile, filePath);

                // También guardar en ubicación temporal para mayor seguridad
                string tempFile = Path.Combine(Path.GetTempPath(), "ECM_pending_file.txt");
                File.WriteAllText(tempFile, filePath);

                Console.WriteLine($"Marcador de archivo pendiente creado en: {markerFile}");
                Console.WriteLine($"Archivo pendiente: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear marcador de archivo pendiente: {ex.Message}");
            }
        }
        public static void ScheduleFileCheck(string filePath)
        {
            // Detener timer anterior si existe
            if (_pendingFiles.ContainsKey(filePath) && _pendingFiles[filePath] != null)
            {
                _pendingFiles[filePath].Stop();
                _pendingFiles[filePath].Dispose();
            }

            // Crear nuevo timer para verificar en 2 segundos
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 2000;
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                _pendingFiles.Remove(filePath);

                // Verificar de nuevo
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > 0 && FileMonitor.IsFileReady(filePath))
                    {
                        // Archivo listo para procesar - reducir logs
                        Console.WriteLine($"PDF listo: {Path.GetFileName(filePath)}");

                        // Procesar el archivo
                        if (BackgroundMonitorService.IsActuallyRunning())
                        {
                            try
                            {
                                WinFormsApiClient.VirtualWatcher.DocumentProcessor.Instance.ProcessNewPrintJob(filePath);
                            }
                            catch (Exception)
                            {
                                // Si falla, intentar lanzar la aplicación
                                LaunchApplicationWithFile(filePath);
                            }
                        }
                        else
                        {
                            // Iniciar aplicación
                            LaunchApplicationWithFile(filePath);
                        }
                    }
                    else
                    {
                        // Seguir esperando
                        ScheduleFileCheck(filePath);
                    }
                }

                timer.Dispose();
            };

            _pendingFiles[filePath] = timer;
            timer.Start();
        }
        /// <summary>
        /// Lanza la aplicación ECM Central para procesar un archivo específico
        /// </summary>
        public static void LaunchApplicationWithFile(string filePath)
        {
            try
            {
                // Obtener la ruta de la aplicación
                string appPath = Application.ExecutablePath;

                // Lanzar la aplicación con argumentos para procesar el archivo
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = appPath,
                    Arguments = $"/processfile=\"{filePath}\"",
                    UseShellExecute = true
                };

                Process.Start(startInfo);

                WatcherLogger.LogActivity($"Aplicación lanzada para procesar archivo: {filePath}");
                Console.WriteLine($"ECM Central iniciado para procesar archivo: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al lanzar la aplicación: {ex.Message}");
                WatcherLogger.LogError("Error al lanzar ECM Central con archivo", ex);
            }
        }
        /// <summary>
        /// Asegura que la carpeta de salida exista y tenga permisos adecuados
        /// </summary>
        public static void EnsureOutputFolderExists()
        {
            try
            {
                if (!Directory.Exists(FIXED_OUTPUT_PATH))
                {
                    Directory.CreateDirectory(FIXED_OUTPUT_PATH);
                    Console.WriteLine($"Carpeta de salida PDF creada: {FIXED_OUTPUT_PATH}");
                }

                // Verificar permisos de escritura
                string testFile = Path.Combine(FIXED_OUTPUT_PATH, "test_write.tmp");
                File.WriteAllText(testFile, $"Test de permisos: {DateTime.Now}");
                File.Delete(testFile);
                Console.WriteLine($"Verificación de permisos en carpeta de salida: OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear/verificar carpeta de salida: {ex.Message}");
                // Intentamos crear en la carpeta de documentos como respaldo (comportamiento original)
                try
                {
                    string docFolder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        OUTPUT_FOLDER);

                    if (!Directory.Exists(docFolder))
                        Directory.CreateDirectory(docFolder);

                    Console.WriteLine($"Usando carpeta de respaldo: {docFolder}");
                }
                catch (Exception backupEx)
                {
                    Console.WriteLine($"Error al crear carpeta de respaldo: {backupEx.Message}");
                }
            }
        }

        /// <summary>
        /// Obtiene la ruta a la carpeta de salida para archivos PDF
        /// </summary>
        public static string GetOutputFolderPath()
        {
            try
            {
                // Asegurarse de que la carpeta existe
                EnsureOutputFolderExists();
                return FIXED_OUTPUT_PATH;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al obtener carpeta de salida: {ex.Message}");
                // En caso de error, volver al comportamiento original
                string docFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    OUTPUT_FOLDER);
                return docFolder;
            }
        }

        /// <summary>
        /// Verifica si el usuario tiene permisos de administrador
        /// </summary>
        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Verifica si la impresora ECM Central está instalada
        /// </summary>
        public static bool IsPrinterInstalled()
        {
            // Primero intentamos el método estándar con PrinterSettings
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.Equals(PRINTER_NAME, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            // Si no funciona, intentamos usar WMI como alternativa
            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT * FROM Win32_Printer WHERE Name LIKE '%" + PRINTER_NAME + "%'"))
                {
                    ManagementObjectCollection printers = searcher.Get();
                    return printers.Count > 0;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al verificar impresora con WMI: {ex.Message}");
            }

            // Como último recurso, verificamos con PowerShell
            try
            {
                var result = PowerShellHelper.RunPowerShellCommandWithOutput($"Get-Printer -Name '{PRINTER_NAME}' | Select-Object -ExpandProperty Name");
                return !string.IsNullOrEmpty(result);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Obtiene la ruta completa de la carpeta de logs
        /// </summary>
        public static string GetLogFolderPath()
        {
            // Usamos el directorio de la aplicación en lugar de C:\
            string appFolder = Application.StartupPath;
            string logPath = Path.Combine(appFolder, LOG_FOLDER);

            try
            {
                // Crear la carpeta si no existe
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                    Console.WriteLine($"Carpeta de logs creada: {logPath}");
                }
                return logPath;
            }
            catch (Exception ex)
            {
                // Si falla, intentar usar el escritorio como último recurso
                Console.WriteLine($"Error al crear carpeta de logs: {ex.Message}");
                string desktopPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    "ECM_Logs");

                if (!Directory.Exists(desktopPath))
                    Directory.CreateDirectory(desktopPath);

                return desktopPath;
            }
        }
    }
}