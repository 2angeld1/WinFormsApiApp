using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using WinFormsApiClient.NewVirtualPrinter;

namespace WinFormsApiClient
{
    static class Program
    {
        // Eliminar referencias al monitor - ya no necesitamos mutex
        // static Mutex monitorMutex; // ELIMINADO

        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static async Task Main(string[] args)
        {
            try
            {
                // Configurar logging desde el inicio
                LogToFile("=== INICIANDO ECM CENTRAL ===");
                LogToFile($"Argumentos recibidos: {string.Join(" ", args)}");
                LogToFile($"Directorio de trabajo: {Environment.CurrentDirectory}");
                LogToFile($"Ejecutable: {Application.ExecutablePath}");

                // Configurar manejo de excepciones no controladas
                Application.ThreadException += (sender, e) =>
                {
                    try
                    {
                        string errorMsg = $"Excepción no controlada en hilo de UI: {e.Exception.Message}";
                        Console.WriteLine(errorMsg);
                        Console.WriteLine($"Stack trace: {e.Exception.StackTrace}");
                        LogToFile($"ERROR UI: {errorMsg}");
                        LogToFile($"Stack trace: {e.Exception.StackTrace}");

                        MessageBox.Show(
                            $"Se ha producido un error en la aplicación:\n{e.Exception.Message}\n\nRevisa el log en Desktop para más detalles.",
                            "Error en la aplicación",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                    catch
                    {
                        // Si falla el manejo de excepciones, no podemos hacer mucho más
                    }
                };

                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    try
                    {
                        var ex = e.ExceptionObject as Exception;
                        string errorMsg = $"Excepción no controlada en AppDomain: {ex?.Message}";
                        Console.WriteLine(errorMsg);
                        LogToFile($"ERROR AppDomain: {errorMsg}");
                        LogToFile($"Stack trace: {ex?.StackTrace}");
                    }
                    catch
                    {
                        // Si falla el manejo de excepciones, no podemos hacer mucho más
                    }
                };

                // Procesar argumentos de línea de comandos
                if (args.Length > 0)
                {
                    LogToFile($"Procesando argumentos de línea de comandos: {args.Length} argumentos");
                    await ProcessCommandLineArguments(args);
                    LogToFile("=== FIN PROCESAMIENTO ARGUMENTOS ===");
                    return;
                }

                // Inicializar el sistema de impresión al iniciar la aplicación
                LogToFile("Inicializando sistema de impresión virtual...");
                await VirtualPrinterService.InitializeAsync();
                LogToFile("Sistema de impresión inicializado");

                LogToFile("Iniciando aplicación normal (sin argumentos)");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new LoginForm());
                LogToFile("=== FIN APLICACIÓN NORMAL ===");
            }
            catch (Exception ex)
            {
                LogToFile($"ERROR CRÍTICO en Main: {ex.Message}");
                LogToFile($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error crítico al iniciar la aplicación:\n{ex.Message}",
                    "Error crítico", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void LogToFile(string message)
        {
            try
            {
                string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_full_log.txt");
                string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                File.AppendAllText(logFile, logEntry + "\r\n");
                Console.WriteLine(logEntry);
            }
            catch
            {
                // Si no podemos escribir el log, al menos mostrar en consola
                Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}");
            }
        }

        private static async Task ProcessCommandLineArguments(string[] args)
        {
            string argument = args[0].ToLower();
            LogToFile($"Procesando argumento principal: '{argument}'");

            if (argument == "/diagnose")
            {
                LogToFile("=== COMANDO /diagnose ===");
                Console.WriteLine("=== INICIANDO DIAGNÓSTICO ===");

                VirtualPrinterService.RunDiagnostics();
                LogToFile("Diagnóstico ejecutado");

                // MOSTRAR RESULTADO EN VENTANA
                string diagFile = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "pdfcreator_diagnosis.txt");

                if (File.Exists(diagFile))
                {
                    string content = File.ReadAllText(diagFile);
                    string preview = content.Length > 500 ? content.Substring(Math.Max(0, content.Length - 500)) : content;

                    LogToFile($"Archivo de diagnóstico creado: {diagFile}");
                    MessageBox.Show($"Diagnóstico completado. Archivo: {diagFile}\n\nÚltimas líneas:\n{preview}",
                        "Diagnóstico", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    LogToFile("ERROR: No se pudo generar archivo de diagnóstico");
                    MessageBox.Show("Diagnóstico completado, pero no se pudo generar el archivo.",
                        "Diagnóstico", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                LogToFile("=== FIN COMANDO /diagnose ===");
                return;
            }
            else if (argument == "/setup")
            {
                LogToFile("=== COMANDO /setup ===");
                Console.WriteLine("=== INICIANDO SETUP ===");

                // Mostrar progreso
                using (var progressForm = new Form())
                {
                    progressForm.Text = "Configurando ECM Central";
                    progressForm.Size = new Size(450, 200);
                    progressForm.StartPosition = FormStartPosition.CenterScreen;
                    progressForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                    progressForm.MaximizeBox = false;
                    progressForm.MinimizeBox = false;

                    var label = new Label
                    {
                        Text = "Configurando sistema de impresión virtual...",
                        AutoSize = true,
                        Location = new Point(20, 20)
                    };
                    progressForm.Controls.Add(label);

                    var progressBar = new ProgressBar
                    {
                        Style = ProgressBarStyle.Marquee,
                        Location = new Point(20, 60),
                        Size = new Size(400, 23)
                    };
                    progressForm.Controls.Add(progressBar);

                    var statusLabel = new Label
                    {
                        Text = "Iniciando...",
                        AutoSize = true,
                        Location = new Point(20, 100),
                        ForeColor = Color.Blue
                    };
                    progressForm.Controls.Add(statusLabel);

                    progressForm.Show();
                    Application.DoEvents();

                    try
                    {
                        // Paso 1: Verificar PDFCreator
                        statusLabel.Text = "Verificando PDFCreator...";
                        Application.DoEvents();
                        LogToFile("Verificando instalación de PDFCreator...");

                        bool pdfCreatorInstalled = PDFCreatorManager.IsPDFCreatorInstalled();
                        LogToFile($"PDFCreator instalado: {pdfCreatorInstalled}");

                        if (!pdfCreatorInstalled)
                        {
                            statusLabel.Text = "PDFCreator no encontrado. Instalando...";
                            Application.DoEvents();
                            LogToFile("PDFCreator no instalado, iniciando instalación...");

                            progressForm.Hide();
                            pdfCreatorInstalled = await PDFCreatorInstaller.EnsurePDFCreatorInstalledAsync();
                            progressForm.Show();

                            LogToFile($"Resultado de instalación: {pdfCreatorInstalled}");

                            if (!pdfCreatorInstalled)
                            {
                                progressForm.Close();
                                LogToFile("ERROR: Instalación de PDFCreator falló");
                                MessageBox.Show("No se pudo instalar PDFCreator. El setup ha fallado.",
                                    "Error de configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return;
                            }
                        }

                        // Paso 2: Configurar PDFCreator
                        statusLabel.Text = "Configurando PDFCreator...";
                        Application.DoEvents();
                        LogToFile("Configurando PDFCreator...");

                        bool configured = PDFCreatorManager.ConfigurePDFCreator();
                        // <-- AQUÍ agrega la verificación de la impresora:
                        if (!PDFCreatorManager.IsPrinterInstalled("ECM Central Printer"))
                        {
                            MessageBox.Show("La impresora virtual 'ECM Central Printer' no está instalada. Por favor, reinstala PDFCreator.",
                                "Impresora no encontrada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }

                        LogToFile($"PDFCreator configurado: {configured}");

                        progressForm.Close();

                        if (configured)
                        {
                            string message = "Sistema de impresión configurado correctamente.\n\n" +
                                "Configuración completada:\n" +
                                $"• PDFCreator: ✓\n" +
                                $"• Impresora: ECM Central Printer\n" +
                                $"• Carpeta de salida: {PDFCreatorManager.OUTPUT_FOLDER}\n" +
                                $"• Aplicación registrada: ✓\n\n" +
                                "Ya puedes usar 'Imprimir a PDF' desde cualquier aplicación.\n\n" +
                                "Usa /createtest para probar el sistema.";

                            LogToFile("Setup completado exitosamente");
                            MessageBox.Show(message, "Configuración completada",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            LogToFile("ERROR: Configuración de PDFCreator falló");
                            MessageBox.Show("Error al configurar PDFCreator.",
                                "Error de configuración", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        progressForm.Close();
                        LogToFile($"ERROR en setup: {ex.Message}");
                        MessageBox.Show($"Error durante el setup: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }

                LogToFile("=== FIN COMANDO /setup ===");
                return;
            }
            else if (argument == "/testpdfcreator")
            {
                LogToFile("=== COMANDO /testpdfcreator ===");

                if (!Directory.Exists(PDFCreatorManager.OUTPUT_FOLDER))
                {
                    Directory.CreateDirectory(PDFCreatorManager.OUTPUT_FOLDER);
                    LogToFile($"Carpeta de salida creada: {PDFCreatorManager.OUTPUT_FOLDER}");
                }

                string testTextFile = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "test_pdfcreator.txt");

                string testText = $@"=== ARCHIVO DE PRUEBA ECM CENTRAL - PDFCREATOR ===
Fecha: {DateTime.Now}
Carpeta: {PDFCreatorManager.OUTPUT_FOLDER}
Aplicación: {Application.ExecutablePath}
Impresora: ECM Central Printer

INSTRUCCIONES PARA PROBAR:
1. Presiona Ctrl+P para imprimir
2. Selecciona 'ECM Central Printer' como impresora
3. Haz clic en 'Imprimir'
4. El PDF se generará automáticamente
5. ECM Central debería abrirse automáticamente

¡PDFCreator no requiere configuración manual!

=== FIN ARCHIVO DE PRUEBA ===";

                File.WriteAllText(testTextFile, testText);
                LogToFile($"Archivo de prueba creado: {testTextFile}");

                MessageBox.Show($"Archivo de prueba creado: {testTextFile}\n\n" +
                    "PASOS PARA PROBAR:\n" +
                    "1. Se abrirá el archivo .txt\n" +
                    "2. Presiona Ctrl+P\n" +
                    "3. Selecciona 'ECM Central Printer'\n" +
                    "4. Haz clic en 'Imprimir'\n" +
                    "5. ECM Central debería abrirse automáticamente\n\n" +
                    "NO necesitas configurar nada manualmente.",
                    "Test de PDFCreator", MessageBoxButtons.OK, MessageBoxIcon.Information);

                try
                {
                    Process.Start(testTextFile);
                    LogToFile("Archivo de prueba abierto");
                }
                catch (Exception ex)
                {
                    LogToFile($"Error abriendo archivo: {ex.Message}");
                }

                LogToFile("=== FIN COMANDO /testpdfcreator ===");
                return;
            }
            else if (argument == "/pdfcreatorlog")
            {
                LogToFile("=== COMANDO /pdfcreatorlog ===");

                string diagPath = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "pdfcreator_diagnosis.txt");

                if (File.Exists(diagPath))
                {
                    string logContent = File.ReadAllText(diagPath);
                    MessageBox.Show($"Diagnóstico de PDFCreator:\n\n{logContent}", "PDFCreator Diagnosis",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LogToFile($"Contenido del diagnóstico:\n{logContent}");
                }
                else
                {
                    MessageBox.Show("No se encontró el diagnóstico. Ejecuta /diagnose primero.", "Log no encontrado",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                LogToFile("=== FIN COMANDO /pdfcreatorlog ===");
                return;
            }

            else if (argument == "/clean")
            {
                LogToFile("=== COMANDO /clean ===");
                Console.WriteLine("=== INICIANDO LIMPIEZA ===");

                int itemsDeleted = 0;
                var resultados = new List<string>();

                try
                {
                    // Limpiar archivos temporales (SIN archivos de monitor)
                    string[] tempFiles = {
                            Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "pdfcreator_diagnosis.txt"),
                            Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "print_jobs.log"),
                            Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "print_errors.log"),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_commands.log"),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_error_log.txt"),
                            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "ecm_full_log.txt")
                        };

                    foreach (string file in tempFiles)
                    {
                        try
                        {
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                                itemsDeleted++;
                                resultados.Add($"✓ Eliminado: {Path.GetFileName(file)}");
                                Console.WriteLine($"Archivo eliminado: {file}");
                            }
                            else
                            {
                                resultados.Add($"• No existe: {Path.GetFileName(file)}");
                            }
                        }
                        catch (Exception ex)
                        {
                            resultados.Add($"✗ Error eliminando {Path.GetFileName(file)}: {ex.Message}");
                            Console.WriteLine($"Error eliminando {file}: {ex.Message}");
                        }
                    }

                    // Limpiar procesos colgados
                    var processes = Process.GetProcessesByName("WinFormsApiClient");
                    int processesKilled = 0;

                    foreach (var proc in processes)
                    {
                        try
                        {
                            if (proc.Id != Process.GetCurrentProcess().Id)
                            {
                                proc.Kill();
                                processesKilled++;
                                Console.WriteLine($"Proceso terminado: {proc.Id}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error terminando proceso {proc.Id}: {ex.Message}");
                        }
                    }

                    if (processesKilled > 0)
                    {
                        resultados.Add($"✓ Terminados {processesKilled} procesos");
                    }

                    // Mostrar resultados
                    string message = $"Limpieza completada.\n\n" +
                        $"Elementos procesados: {itemsDeleted}\n\n" +
                        string.Join("\n", resultados.Take(10)) +
                        (resultados.Count > 10 ? $"\n... y {resultados.Count - 10} más" : "");

                    MessageBox.Show(message, "Limpieza completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Console.WriteLine($"=== LIMPIEZA COMPLETADA - {itemsDeleted} elementos procesados ===");
                }
                catch (Exception ex)
                {
                    LogToFile($"ERROR en limpieza: {ex.Message}");
                    MessageBox.Show($"Error durante la limpieza: {ex.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                LogToFile("=== FIN COMANDO /clean ===");
                return;
            }
            else if (argument.StartsWith("/print:"))
            {
                LogToFile("=== COMANDO /print ===");

                // Ejecutar setup automáticamente antes de imprimir
                LogToFile("Ejecutando setup automático antes de imprimir...");
                PDFCreatorManager.ConfigurePDFCreator();

                string filePath = null;

                // Caso 1: /print:"C:\ruta\archivo.pdf" (todo en un solo argumento)
                if (argument.Length > "/print:".Length)
                {
                    filePath = argument.Substring("/print:".Length).Trim('"');
                }
                // Caso 2: /print: "C:\ruta\archivo.pdf" (dos argumentos)
                else if (args.Length > 1)
                {
                    filePath = args[1].Trim('"');
                }

                LogToFile($"Archivo a procesar RAW: '{filePath}'");

                if (string.IsNullOrEmpty(filePath))
                {
                    LogToFile("ERROR: Ruta de archivo vacía después de procesar");
                    MessageBox.Show("Error: No se proporcionó archivo para procesar", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    LogToFile("=== FIN COMANDO /print ===");
                    return;
                }

                int maxRetries = 10;
                int delayMs = 500;
                bool fileReady = false;

                for (int i = 0; i < maxRetries; i++)
                {
                    if (File.Exists(filePath))
                    {
                        fileReady = true;
                        break;
                    }
                    await Task.Delay(delayMs);
                }

                if (!fileReady)
                {
                    LogToFile($"ERROR: Archivo no encontrado tras {maxRetries} intentos: {filePath}");

                    // Buscar el PDF más reciente en la carpeta de salida
                    string outputDir = Path.GetDirectoryName(filePath);

                    // Si outputDir es nulo o vacío, usa la carpeta de salida configurada
                    if (string.IsNullOrWhiteSpace(outputDir))
                    {
                        outputDir = PDFCreatorManager.OUTPUT_FOLDER;
                        LogToFile($"outputDir era nulo, se usa carpeta de salida por defecto: {outputDir}");
                    }

                    if (!Directory.Exists(outputDir))
                    {
                        LogToFile($"ERROR: Carpeta de salida inválida: '{outputDir}' para filePath: '{filePath}'");
                        MessageBox.Show($"Carpeta de salida inválida: {outputDir}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogToFile("=== FIN COMANDO /print ===");
                        return;
                    }

                    var recentPdf = Directory.GetFiles(outputDir, "*.pdf")
                        .Select(f => new FileInfo(f))
                        .OrderByDescending(f => f.CreationTime)
                        .FirstOrDefault();

                    if (recentPdf != null && recentPdf.CreationTime > DateTime.Now.AddMinutes(-2))
                    {
                        LogToFile($"Archivo PDF reciente encontrado: {recentPdf.FullName}");
                        filePath = recentPdf.FullName;
                        fileReady = true;
                    }
                    else
                    {
                        MessageBox.Show($"Archivo no encontrado: {filePath}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        LogToFile("=== FIN COMANDO /print ===");
                        return;
                    }
                }

                if (File.Exists(filePath))
                {
                    LogToFile($"Archivo existe, procesando: {filePath}");

                    try
                    {
                        await VirtualPrinterService.InitializeAsync();
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);

                        if (string.IsNullOrEmpty(AppSession.Current.AuthToken))
                        {
                            LogToFile("No hay sesión activa, abriendo LoginForm");
                            var loginForm = new LoginForm(filePath);
                            Application.Run(loginForm);
                        }
                        else
                        {
                            LogToFile("Sesión activa, abriendo FormularioForm directamente");
                            var formularioForm = new FormularioForm();
                            formularioForm.Shown += (s, e) =>
                            {
                                formularioForm.EstablecerArchivoSeleccionado(filePath);
                            };
                            Application.Run(formularioForm);
                        }

                        LogToFile("Trabajo de impresión procesado correctamente");
                    }
                    catch (Exception ex)
                    {
                        LogToFile($"Error procesando archivo: {ex.Message}");
                        MessageBox.Show($"Error procesando archivo: {ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                else
                {
                    LogToFile($"ERROR: Archivo no encontrado: {filePath}");
                    MessageBox.Show($"Archivo no encontrado: {filePath}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                LogToFile("=== FIN COMANDO /print ===");
                return;
            }
            else if (argument == "/createtest")
            {
                LogToFile("=== COMANDO /createtest ===");

                if (!Directory.Exists(PDFCreatorManager.OUTPUT_FOLDER))
                {
                    Directory.CreateDirectory(PDFCreatorManager.OUTPUT_FOLDER);
                    LogToFile($"Carpeta de salida creada: {PDFCreatorManager.OUTPUT_FOLDER}");
                }

                string testTextFile = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "test_document.txt");

                string testText = $@"=== ARCHIVO DE PRUEBA ECM CENTRAL ===
Fecha: {DateTime.Now}
Carpeta: {PDFCreatorManager.OUTPUT_FOLDER}
Aplicación: {Application.ExecutablePath}

INSTRUCCIONES PARA PROBAR:
1. Presiona Ctrl+P para imprimir
2. Selecciona 'ECM Central Printer' como impresora
3. Haz clic en 'Imprimir'
4. ECM Central debería abrirse automáticamente
5. Deberías ver el LoginForm

Si todo funciona correctamente, verás la aplicación ECM Central abrirse automáticamente después de imprimir.

=== FIN ARCHIVO DE PRUEBA ===";

                File.WriteAllText(testTextFile, testText);
                LogToFile($"Archivo de prueba creado: {testTextFile}");

                MessageBox.Show($"Archivo de prueba creado: {testTextFile}\n\n" +
                    "PASOS PARA PROBAR:\n" +
                    "1. Se abrirá el archivo .txt\n" +
                    "2. Presiona Ctrl+P\n" +
                    "3. Selecciona 'ECM Central Printer'\n" +
                    "4. Haz clic en 'Imprimir'\n" +
                    "5. ECM Central debería abrirse automáticamente\n\n" +
                    "Revisa el log 'ecm_full_log.txt' en Desktop para ver el progreso.",
                    "Test de PDFCreator", MessageBoxButtons.OK, MessageBoxIcon.Information);

                try
                {
                    Process.Start(testTextFile);
                    LogToFile("Archivo de prueba abierto");
                }
                catch (Exception ex)
                {
                    LogToFile($"Error abriendo archivo: {ex.Message}");
                }

                LogToFile("=== FIN COMANDO /createtest ===");
                return;
            }

            else if (argument == "/testdirect")
            {
                LogToFile("=== COMANDO /testdirect ===");
                string testFile = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "test.pdf");

                if (File.Exists(testFile))
                {
                    LogToFile($"Archivo de prueba encontrado: {testFile}");
                    Application.EnableVisualStyles();
                    Application.SetCompatibleTextRenderingDefault(false);

                    var formularioForm = new FormularioForm();
                    formularioForm.EstablecerArchivoSeleccionado(testFile);

                    Application.Run(formularioForm);
                    LogToFile("Test directo completado");
                }
                else
                {
                    LogToFile($"ERROR: Archivo de prueba no encontrado: {testFile}");
                    MessageBox.Show($"Archivo de prueba no encontrado: {testFile}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                LogToFile("=== FIN COMANDO /testdirect ===");
                return;
            }
            else if (argument == "/batchlog")
            {
                LogToFile("=== COMANDO /batchlog ===");

                string batchLogPath = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "batch_debug.log");

                if (File.Exists(batchLogPath))
                {
                    string logContent = File.ReadAllText(batchLogPath);
                    MessageBox.Show($"Log del batch:\n\n{logContent}", "Batch Debug Log",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LogToFile($"Contenido del batch log:\n{logContent}");
                }
                else
                {
                    MessageBox.Show("No se encontró el log del batch.", "Log no encontrado",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

                LogToFile("=== FIN COMANDO /batchlog ===");
                return;
            }
            else if (argument == "/checkconfig")
            {
                LogToFile("=== COMANDO /checkconfig ===");

                try
                {
                    string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Bullzip", "PDF Printer", "settings.ini");
                    string batchPath = Path.Combine(PDFCreatorManager.OUTPUT_FOLDER, "ecm_process.bat");

                    string report = "=== CONFIGURACIÓN ACTUAL ===\n\n";

                    // Verificar settings.ini
                    if (File.Exists(settingsPath))
                    {
                        report += "✓ settings.ini EXISTE\n";
                        string content = File.ReadAllText(settingsPath);

                        if (content.Contains("RunOnceFile"))
                            report += "✓ RunOnceFile configurado\n";
                        else
                            report += "✗ RunOnceFile NO configurado\n";

                        if (content.Contains("RunOnSuccess"))
                            report += "✓ RunOnSuccess configurado\n";
                        else
                            report += "✗ RunOnSuccess NO configurado\n";
                    }
                    else
                    {
                        report += "✗ settings.ini NO EXISTE\n";
                    }

                    // Verificar batch
                    if (File.Exists(batchPath))
                    {
                        report += "✓ Script batch EXISTE\n";
                        report += $"Ubicación: {batchPath}\n";
                    }
                    else
                    {
                        report += "✗ Script batch NO EXISTE\n";
                    }

                    // Verificar registro
                    try
                    {
                        using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Bullzip\PDF Printer"))
                        {
                            if (key != null)
                            {
                                report += "✓ Registro de usuario EXISTE\n";
                                string runOnceFile = key.GetValue("RunOnceFile")?.ToString();
                                string runOnSuccessFile = key.GetValue("RunOnSuccessFile")?.ToString();

                                report += $"RunOnceFile: {runOnceFile ?? "NO CONFIGURADO"}\n";
                                report += $"RunOnSuccessFile: {runOnSuccessFile ?? "NO CONFIGURADO"}\n";
                            }
                            else
                            {
                                report += "✗ Registro de usuario NO EXISTE\n";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        report += $"✗ Error leyendo registro: {ex.Message}\n";
                    }

                    LogToFile($"Reporte de configuración:\n{report}");
                    MessageBox.Show(report, "Configuración Actual", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    LogToFile($"Error en checkconfig: {ex.Message}");
                    MessageBox.Show($"Error verificando configuración: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                LogToFile("=== FIN COMANDO /checkconfig ===");
                return;
            }
            else
            {
                LogToFile($"Argumento no reconocido: {argument}");
                LogToFile("Iniciando aplicación normalmente");
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new LoginForm());
            }
        }
    }
}