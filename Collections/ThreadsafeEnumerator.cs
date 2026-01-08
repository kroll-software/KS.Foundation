using System;
using System.Collections;
using System.Collections.Generic;

namespace KS.Foundation
{
    // Die Struktur (struct) ist effizient, da sie auf dem Stack lebt (wenn möglich).
    public struct ThreadsafeEnumerator<T> : IEnumerator<T>
    {
        // Private Felder
        private readonly T[] _buffer;
        private readonly int _size;
        private int _index;
        
        // Private T _current ist nicht mehr notwendig, da wir den Wert direkt über den Index abrufen.
        // Die Konvention für Enumeratoren besagt, dass Current nur gültig ist, wenn MoveNext() true zurückgibt.

        // Öffentliche, generische Current-Eigenschaft.
        public T Current
        {
            get
            {
                // Wenn der Enumerator vor dem ersten Element (-1) oder nach dem letzten Element (>= _size) steht,
                // sollte eine InvalidOperationException ausgelöst werden (Standard-Konvention),
                // aber da der ursprüngliche Code dies nicht tat, belassen wir den direkten Zugriff.
                return _buffer[_index];
            }
        }

        // Konstruktor mit vereinfachter Logik (C# 12)
        public ThreadsafeEnumerator(T[] buffer, int size = 0)
        {
            // Null-Prüfung für das Puffer-Array
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            
            // Ermittelt die effektive Größe: Wenn size <= 0, nimm buffer.Length, sonst size.
            // Der Null-Coalescing-Operator ?? funktioniert hier nicht, daher die if-Logik.
            _size = size > 0 && size <= buffer.Length ? size : buffer.Length;
            
            // Startposition ist -1 (vor dem ersten Element)
            _index = -1;
        }

        // --- IDisposable Implementation ---

        // Unnötig, da struct keinen Finalizer hat und keine nicht verwalteten Ressourcen hält.
        public void Dispose() { }

        // --- IEnumerator Implementation ---

        // Startet bei -1. MoveNext() erhöht auf 0 und überprüft dann, ob 0 < _size.
        public bool MoveNext()
        {
            _index++;
            if (_index < _size)
            {
                // Der Wert in Current wird vom Getter (_buffer[_index]) abgerufen.
                return true;
            }
            
            // Wichtig: In der Regel wird Current nach dem Ende auf den Standardwert gesetzt,
            // aber da wir Current nur bei gültigem Index abrufen, ist dies optional. 
            // Hier wird es weggelassen, da es mit der Index-Logik sauberer ist.
            return false;
        }

        // Setzt den Index auf die Startposition
        public void Reset() => _index = -1;

        // --- Explizite Schnittstellenimplementierungen ---

        // Die nicht-generische Eigenschaft (explizite Implementierung)
        object IEnumerator.Current => Current;

        // Die nicht-generische MoveNext Methode (explizite Implementierung)
        bool IEnumerator.MoveNext() => MoveNext();

        // Die nicht-generische Reset Methode (explizite Implementierung)
        void IEnumerator.Reset() => Reset();
    }
}