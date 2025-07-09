// using System;
// using System.Diagnostics;
// using System.IO;
// using System.Windows.Forms;

// public class PrinterHandler
// {
//     public static void PrintToCloud(string filePath)
//     {
//         try
//         {
//             // Verificar que se proporcionó una ruta de archivo
//             if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
//             {
//                 // Si no hay una ruta válida, permitir al usuario seleccionar un archivo
//                 using (OpenFileDialog openFileDialog = new OpenFileDialog())
//                 {
//                     openFileDialog.Filter = "Documentos (*.pdf;*.docx;*.doc)|*.pdf;*.docx;*.doc|Todos los archivos (*.*)|*.*";
//                     openFileDialog.Title = "Seleccione un archivo para imprimir";

//                     if (openFileDialog.ShowDialog() == DialogResult.OK)
//                     {
//                         filePath = openFileDialog.FileName;
//                     }
//                     else
//                     {
//                         // El usuario canceló la selección
//                         return;
//                     }
//                 }
//             }

//             // Verificar nuevamente que el archivo existe
//             if (!File.Exists(filePath))
//             {
//                 MessageBox.Show("El archivo seleccionado no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//                 return;
//             }

//             // Usar un método diferente para imprimir, más compatible con diferentes tipos de archivos
//             try
//             {
//                 // Primero intentamos abrir el archivo con la aplicación predeterminada
//                 Process process = new Process();
//                 process.StartInfo = new ProcessStartInfo
//                 {
//                     FileName = filePath,
//                     UseShellExecute = true,
//                     Verb = "open"
//                 };

//                 process.Start();

//                 // Esperar un poco para que la aplicación se abra
//                 System.Threading.Thread.Sleep(1000);

//                 // Mostrar mensaje para que el usuario use el menú "Imprimir" de la aplicación
//                 MessageBox.Show("El archivo se ha abierto. Por favor, use el menú Archivo > Imprimir de la aplicación para imprimirlo " +
//                     "y luego cierre la aplicación para continuar.", "Imprimir documento", MessageBoxButtons.OK, MessageBoxIcon.Information);

//                 // Opcional: Esperar a que el usuario cierre la aplicación
//                 process.WaitForExit();

//                 // Consultar al usuario si desea subir el archivo
//                 DialogResult result = MessageBox.Show(
//                     "¿Desea subir este documento al portal?",
//                     "Subir al portal",
//                     MessageBoxButtons.YesNo,
//                     MessageBoxIcon.Question);

//                 if (result == DialogResult.Yes)
//                 {
//                     // Mostrar mensaje de subida (aquí implementarías la subida real)
//                     MessageBox.Show($"Subiendo el archivo {Path.GetFileName(filePath)} al portal...");

//                     // Aquí llamarías a una función para subir el archivo
//                     UploadToPortal(filePath);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 MessageBox.Show($"Error al abrir el archivo: {ex.Message}\n\nIntentando método alternativo...");

//                 // Método alternativo usando ShellExecute
//                 ProcessStartInfo psi = new ProcessStartInfo
//                 {
//                     FileName = "rundll32.exe",
//                     Arguments = $"shell32.dll,ShellExec_RunDLL {filePath}",
//                     UseShellExecute = true,
//                     CreateNoWindow = true
//                 };

//                 Process.Start(psi);
//             }
//         }
//         catch (Exception ex)
//         {
//             MessageBox.Show($"Error al procesar el archivo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//         }
//     }

//     public static void UploadToPortal(string filePath)
//     {
//         try
//         {
//             if (File.Exists(filePath))
//             {
//                 // Aquí implementarías la lógica real para subir el archivo al portal
//                 // Usando el token de autenticación desde AppSession
//                 string token = WinFormsApiClient.AppSession.Current.AuthToken;

//                 // Simulación de subida (reemplaza esto con la implementación real)
//                 System.Threading.Thread.Sleep(1000); // Simular procesamiento
//                 MessageBox.Show($"Archivo {Path.GetFileName(filePath)} subido exitosamente al portal.",
//                     "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
//             }
//             else
//             {
//                 MessageBox.Show("El archivo especificado ya no existe o no se puede acceder a él.",
//                     "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
//             }
//         }
//         catch (Exception ex)
//         {
//             MessageBox.Show($"Error al subir el archivo al portal: {ex.Message}",
//                 "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
//         }
//     }
// }