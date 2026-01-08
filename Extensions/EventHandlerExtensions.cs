using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic; // Eventuell benötigt, falls GetInvocationList nicht automatisch erkannt wird

namespace KS.Foundation
{

    public static class EventHandlerExtensions
    {
        /// <summary>
        /// Invoke an event asynchronously. Each subscriber to the event will be invoked in parallel using Tasks.
        /// </summary>
        /// <param name="someEvent">The event to be invoked asynchronously.</param>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="args">The args of the event.</param>
        /// <typeparam name="TEventArgs">The type of <see cref="EventArgs"/> to be used with the event.</typeparam>
        public static void InvokeAsync<TEventArgs>(this EventHandler<TEventArgs> someEvent, object sender, TEventArgs args)
            where TEventArgs : EventArgs
        {
            // Null-Check für das Event
            if (someEvent == null)
            {
                return;
            }

            // Event-Handler in eine Liste kopieren, um Thread-Sicherheit beim Durchlaufen zu gewährleisten
            var eventListeners = someEvent.GetInvocationList();
            
            // Eine Liste von Tasks, um alle asynchronen Aufrufe zu verfolgen
            var invocationTasks = new List<Task>();

            foreach (EventHandler<TEventArgs> methodToInvoke in eventListeners)
            {
                // Starten Sie jede Methode in einem separaten Task im Thread-Pool
                // Wir verwenden Task.Run, da dies der moderne Ersatz für ThreadPool/BeginInvoke ist.
                var task = Task.Run(() => 
                {
                    // Führen Sie den eigentlichen Event-Handler aus
                    methodToInvoke.Invoke(sender, args);
                });
                
                invocationTasks.Add(task);
            }
            
            // Optional: Wenn Sie sicherstellen wollen, dass Ausnahmen protokolliert werden
            // Diese Tasks laufen jetzt im Hintergrund ('fire and forget'). 
            // Wenn ein Handler abstürzt, wird die Ausnahme im Task gespeichert, aber nicht sofort die Hauptanwendung zum Absturz bringen.
            // Sie können diese Logik erweitern, um Fehler in den Tasks zu behandeln, falls nötig.
        }
    }

}
