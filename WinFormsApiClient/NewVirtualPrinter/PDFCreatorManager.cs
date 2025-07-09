using System;
using System.Diagnostics;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;

namespace WinFormsApiClient.NewVirtualPrinter
{
    public static class PDFCreatorManager
    {
        private static string _outputFolder = @"C:\Temp\ECM Central";
        
        public static string OUTPUT_FOLDER 
        { 
            get => _outputFolder;
            set => _outputFolder = value;
        }

        private const string PRINTER_NAME = "ECM Central Printer";

        /// <summary>
        /// Verifica si PDFCreator está instalado
        /// </summary>
        public static bool IsPDFCreatorInstalled()
        {
            try
            {
                // Verificar en registro
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\pdfforge\PDFCreator"))
                {
                    if (key != null) return true;
                }

                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\pdfforge\PDFCreator"))
                {
                    if (key != null) return true;
                }

                // Verificar en carpetas de Program Files
                string[] possiblePaths = {
                    @"C:\Program Files\PDFCreator\",
                    @"C:\Program Files (x86)\PDFCreator\"
                };

                foreach (string path in possiblePaths)
                {
                    if (Directory.Exists(path))
                    {
                        string exePath = Path.Combine(path, "PDFCreator.exe");
                        if (File.Exists(exePath))
                            return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error verificando PDFCreator: {ex.Message}");
                return false;
            }
        }

        public static bool ConfigurePDFCreator()
        {
            try
            {
                if (!Directory.Exists(_outputFolder))
                    Directory.CreateDirectory(_outputFolder);

                using (var key = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\pdfforge\PDFCreator\Settings\ConversionProfiles\0"))
                {
                    using (var autoSaveKey = key.CreateSubKey("AutoSave"))
                    {
                        autoSaveKey.SetValue("Enabled", "True");
                        autoSaveKey.SetValue("TargetDirectory", _outputFolder);
                        autoSaveKey.SetValue("EnsureUniqueFilenames", "True");
                        autoSaveKey.SetValue("FilenameTemplate", "ECM_<DateTime:yyyyMMdd_HHmmss>_<Title>");
                    }

                    using (var scriptKey = key.CreateSubKey("Scripting"))
                    {
                        scriptKey.SetValue("Enabled", "True");
                        scriptKey.SetValue("ScriptFile", Application.ExecutablePath); // Ruta absoluta de tu EXE
                        scriptKey.SetValue("ParameterString", "\"<OutputFilenames>\""); // Solo la ruta del PDF entre comillas
                        scriptKey.SetValue("WaitForScript", "False");
                    }

                    key.SetValue("ShowQuickActions", "False");
                    key.SetValue("OpenViewer", "False");
                    key.SetValue("OpenWithPdfArchitect", "False");
                }

                using (var appKey = Registry.CurrentUser.CreateSubKey(@"SOFTWARE\pdfforge\PDFCreator\Settings\ApplicationSettings"))
                {
                    appKey.SetValue("UpdateInterval", "Never");
                    appKey.SetValue("DisplayUpdateWarning", "False");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando perfil predeterminado de PDFCreator: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Ejecuta diagnóstico de PDFCreator
        /// </summary>
        public static void DiagnosePDFCreator()
        {
            try
            {
                if (!Directory.Exists(_outputFolder))
                    Directory.CreateDirectory(_outputFolder);

                string diagPath = Path.Combine(_outputFolder, "pdfcreator_diagnosis.txt");

                using (var writer = new StreamWriter(diagPath, false))
                {
                    writer.WriteLine("=== DIAGNÓSTICO PDFCREATOR ===");
                    writer.WriteLine($"Fecha: {DateTime.Now}");
                    writer.WriteLine($"Aplicación: {Application.ExecutablePath}");
                    writer.WriteLine($"Carpeta de salida: {_outputFolder}");
                    writer.WriteLine($"Nombre de impresora: {PRINTER_NAME}");
                    writer.WriteLine();

                    bool isInstalled = IsPDFCreatorInstalled();
                    writer.WriteLine($"¿PDFCreator instalado? {(isInstalled ? "SÍ" : "NO")}");

                    if (isInstalled)
                    {
                        // Buscar rutas de instalación
                        string[] possiblePaths = {
                            @"C:\Program Files\PDFCreator\",
                            @"C:\Program Files (x86)\PDFCreator\"
                        };

                        foreach (string path in possiblePaths)
                        {
                            writer.WriteLine($"Verificando: {path}");
                            if (Directory.Exists(path))
                            {
                                writer.WriteLine("  ✓ Directorio existe");
                                string exePath = Path.Combine(path, "PDFCreator.exe");
                                if (File.Exists(exePath))
                                {
                                    var versionInfo = FileVersionInfo.GetVersionInfo(exePath);
                                    writer.WriteLine($"  ✓ PDFCreator.exe encontrado - Versión: {versionInfo.FileVersion}");
                                }
                            }
                        }

                        // Verificar configuración
                        writer.WriteLine();
                        writer.WriteLine("=== CONFIGURACIÓN ===");
                        
                        // Revisar AutoSave del perfil 0
                        using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\pdfforge\PDFCreator\Settings\ConversionProfiles\0\AutoSave"))
                        {
                            if (key != null)
                            {
                                writer.WriteLine("✓ AutoSave configurado");
                                writer.WriteLine($"  Carpeta: {key.GetValue("TargetDirectory")}");
                                writer.WriteLine($"  Habilitado: {key.GetValue("Enabled")}");
                            }
                            else
                            {
                                writer.WriteLine("✗ AutoSave NO configurado");
                            }
                        }

                        // Revisar Scripting del perfil 0
                        using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\pdfforge\PDFCreator\Settings\ConversionProfiles\0\Scripting"))
                        {
                            if (key != null)
                            {
                                writer.WriteLine("✓ Scripting configurado");
                                writer.WriteLine($"  Script: {key.GetValue("ScriptFile")}");
                                writer.WriteLine($"  Parámetros: {key.GetValue("ParameterString")}");
                                writer.WriteLine($"  Habilitado: {key.GetValue("Enabled")}");
                            }
                            else
                            {
                                writer.WriteLine("✗ Scripting NO configurado");
                            }
                        }
                    }

                    writer.WriteLine();
                    writer.WriteLine("=== DIAGNÓSTICO COMPLETADO ===");
                }

                Console.WriteLine($"Diagnóstico de PDFCreator guardado: {diagPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en diagnóstico de PDFCreator: {ex.Message}");
            }
        }

        public static void SetCustomOutputFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder))
                return;

            try
            {
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                _outputFolder = folder;
                Console.WriteLine($"Carpeta de PDFCreator cambiada a: {folder}");
                
                // Reconfigurar con la nueva carpeta
                ConfigurePDFCreator();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error configurando carpeta de PDFCreator: {ex.Message}");
                throw;
            }
        }

        public static bool IsPrinterInstalled(string printerName)
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                if (printer.Equals(printerName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}