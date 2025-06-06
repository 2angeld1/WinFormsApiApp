using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinFormsApiClient
{
    /// <summary>
    /// Clase estática que contiene métodos de extensión para controles de Windows Forms
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Método de extensión para ejecutar una acción en el hilo de UI de forma asíncrona
        /// </summary>
        /// <param name="control">El control en el que se ejecutará la acción</param>
        /// <param name="action">La acción a ejecutar</param>
        /// <returns>Tarea que representa la operación asíncrona</returns>
        public static async Task InvokeAsync(this Control control, Action action)
        {
            if (control.InvokeRequired)
            {
                await Task.Factory.FromAsync(
                    control.BeginInvoke(action),
                    control.EndInvoke);
            }
            else
            {
                action();
            }
        }
    }
}