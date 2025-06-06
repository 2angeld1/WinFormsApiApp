using System;
using System.IO;
using System.Threading;
using System.Diagnostics;
using WinFormsApiClient.VirtualWatcher;
using Microsoft.Win32;

namespace WinFormsApiClient.VirtualPrinter
{
    /// <summary>
    /// Clase que monitorea eventos relacionados con la impresión en modo pasivo (solo logs)
    /// </summary>
    public class BufferedPrintListener : IDisposable
    {
        // Thread para el monitoreo en segundo plano
        private static Thread _monitorThread;
        private static volatile bool _shouldStop = false;

        // Ruta para logs de diagnóstico
        private static readonly string _logPath = Path.Combine(
            VirtualPrinterCore.FIXED_OUTPUT_PATH, "print_listener_logs");

        // Singleton
        private static BufferedPrintListener _instance;
        public static BufferedPrintListener Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new BufferedPrintListener();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Constructor privado (singleton)
        /// </summary>
        private BufferedPrintListener()
        {
            try
            {
                // Crear carpeta para logs
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);

                string startupLog = Path.Combine(_logPath, "listener_startup.log");
                File.AppendAllText(startupLog, $"[{DateTime.Now}] Iniciando BufferedPrintListener en modo pasivo (solo logs)\r\n");

                // Iniciar el thread de monitoreo SOLO para logs (no inicia monitores)
                _monitorThread = new Thread(MonitorForLogsOnly)
                {
                    IsBackground = true,
                    Name = "PrintLogMonitor",
                    Priority = ThreadPriority.BelowNormal // Prioridad baja, solo es para logs
                };

                _monitorThread.Start();
                File.AppendAllText(startupLog, $"[{DateTime.Now}] Thread de monitoreo de logs iniciado\r\n");

                // Registrar manejador para eventos de sistema
                SystemEvents.PowerModeChanged += SystemEvents_PowerModeChanged;
                SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "listener_startup_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al iniciar BufferedPrintListener: {ex.Message}\r\n{ex.StackTrace}\r\n");
            }
        }

        /// <summary>
        /// Thread que solo monitorea para fines de registro (no inicia nuevos monitores)
        /// </summary>
        private void MonitorForLogsOnly()
        {
            try
            {
                string threadLog = Path.Combine(_logPath, "passive_monitor.log");
                File.AppendAllText(threadLog, $"[{DateTime.Now}] Thread de monitoreo pasivo iniciado\r\n");

                while (!_shouldStop)
                {
                    try
                    {
                        // Simplemente verificamos ocasionalmente si hay un monitor activo (para logs)
                        if (DateTime.Now.Minute % 5 == 0 && DateTime.Now.Second < 5) // Cada 5 minutos
                        {
                            bool monitorActive = BackgroundMonitorService.IsActuallyRunning();
                            File.AppendAllText(threadLog, $"[{DateTime.Now}] Verificación periódica - Monitor activo: {monitorActive}\r\n");
                        }

                        // Pausa para no consumir recursos
                        Thread.Sleep(2000); // 2 segundos entre verificaciones
                    }
                    catch (Exception ex)
                    {
                        // Registrar error pero seguir monitoreando
                        if (DateTime.Now.Second % 30 == 0) // Limitar a un registro cada 30 segundos
                        {
                            string errorLog = Path.Combine(_logPath, "passive_monitor_error.log");
                            File.AppendAllText(errorLog, $"[{DateTime.Now}] Error en monitoreo pasivo: {ex.Message}\r\n");
                        }

                        Thread.Sleep(5000);
                    }
                }
            }
            catch (Exception ex)
            {
                string fatalLog = Path.Combine(_logPath, "passive_monitor_fatal.log");
                File.AppendAllText(fatalLog, $"[{DateTime.Now}] Error fatal en thread de monitoreo pasivo: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Maneja el evento de cambio de modo de energía
        /// </summary>
        private void SystemEvents_PowerModeChanged(object sender, Microsoft.Win32.PowerModeChangedEventArgs e)
        {
            try
            {
                if (e.Mode == Microsoft.Win32.PowerModes.Resume)
                {
                    string resumeLog = Path.Combine(_logPath, "system_events.log");
                    File.AppendAllText(resumeLog, $"[{DateTime.Now}] Sistema reanudado de suspensión\r\n");

                    // Reiniciar el thread de monitoreo pasivo si es necesario
                    if (_monitorThread == null || !_monitorThread.IsAlive)
                    {
                        _shouldStop = false;
                        _monitorThread = new Thread(MonitorForLogsOnly)
                        {
                            IsBackground = true,
                            Name = "PrintLogMonitor_Resume"
                        };
                        _monitorThread.Start();
                        File.AppendAllText(resumeLog, $"[{DateTime.Now}] Thread de monitoreo reiniciado\r\n");
                    }
                }
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "power_event_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al manejar cambio de energía: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Maneja el evento de finalización de sesión
        /// </summary>
        private void SystemEvents_SessionEnding(object sender, Microsoft.Win32.SessionEndingEventArgs e)
        {
            try
            {
                string sessionLog = Path.Combine(_logPath, "system_events.log");
                File.AppendAllText(sessionLog, $"[{DateTime.Now}] Sesión finalizando: {e.Reason}\r\n");

                // Detener el monitoreo limpiamente
                _shouldStop = true;
            }
            catch { /* Ignorar errores al finalizar */ }
        }

        /// <summary>
        /// Libera recursos
        /// </summary>
        public void Dispose()
        {
            try
            {
                // Detener el thread de monitoreo
                _shouldStop = true;

                if (_monitorThread != null && _monitorThread.IsAlive)
                {
                    _monitorThread.Join(1000); // Esperar hasta 1 segundo
                }

                // Desregistrar eventos
                SystemEvents.PowerModeChanged -= SystemEvents_PowerModeChanged;
                SystemEvents.SessionEnding -= SystemEvents_SessionEnding;

                string disposeLog = Path.Combine(_logPath, "listener_dispose.log");
                File.AppendAllText(disposeLog, $"[{DateTime.Now}] BufferedPrintListener liberado correctamente\r\n");
            }
            catch (Exception ex)
            {
                string errorLog = Path.Combine(_logPath, "dispose_error.log");
                File.AppendAllText(errorLog, $"[{DateTime.Now}] Error al liberar recursos: {ex.Message}\r\n");
            }
        }
    }
}