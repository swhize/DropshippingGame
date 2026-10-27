using System;

namespace DropshippingGame.Core
{
    /// <summary>
    /// Ruft Ereignis-Handler einzeln abgesichert auf: Wirft ein Abonnent (z. B. eine Anzeige in der
    /// Unity-Schicht), laufen die übrigen Handler und vor allem die Spiellogik danach trotzdem weiter.
    /// Für häufige Ereignisse wie <see cref="Sim.EconomyChanged"/>, Toasts und Sounds.
    /// </summary>
    public static class SafeEvents
    {
        /// <summary>Wird bei einem Fehler in einem Handler aufgerufen (die Unity-Schicht loggt rate-begrenzt).</summary>
        public static Action<string, Exception> OnHandlerError;

        public static void Invoke(Action a, string name)
        {
            if (a == null) return;
            var list = a.GetInvocationList();
            if (list.Length == 1)
            {
                try { a(); }
                catch (Exception e) { Report(name, e); }
                return;
            }
            foreach (var d in list)
            {
                try { ((Action)d)(); }
                catch (Exception e) { Report(name, e); }
            }
        }

        public static void Invoke<T1, T2>(Action<T1, T2> a, T1 x, T2 y, string name)
        {
            if (a == null) return;
            var list = a.GetInvocationList();
            if (list.Length == 1)
            {
                try { a(x, y); }
                catch (Exception e) { Report(name, e); }
                return;
            }
            foreach (var d in list)
            {
                try { ((Action<T1, T2>)d)(x, y); }
                catch (Exception e) { Report(name, e); }
            }
        }

        public static void Invoke<T1, T2, T3>(Action<T1, T2, T3> a, T1 x, T2 y, T3 z, string name)
        {
            if (a == null) return;
            var list = a.GetInvocationList();
            if (list.Length == 1)
            {
                try { a(x, y, z); }
                catch (Exception e) { Report(name, e); }
                return;
            }
            foreach (var d in list)
            {
                try { ((Action<T1, T2, T3>)d)(x, y, z); }
                catch (Exception e) { Report(name, e); }
            }
        }

        private static void Report(string name, Exception e)
        {
            var h = OnHandlerError;
            if (h == null) return;
            try { h(name, e); }
            catch (Exception) { /* Fehlerbericht darf nie selbst stören */ }
        }
    }
}
