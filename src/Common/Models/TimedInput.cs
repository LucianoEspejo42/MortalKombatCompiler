using System;
using MortalKombatCompiler.Common.Models; // Luego borrar si no es necesario


namespace MortalKombatCompiler.Common.Models
{
    /// <summary>
    /// Representa un input del joystick con información temporal
    /// </summary>
    public class TimedInput
    {
        /// <summary>
        /// Comando ejecutado (UP, DOWN, HP, LK, etc.)
        /// </summary>
        public string Command { get; set; }
        public int TimingMs { get; set; }

        /// <summary>
        /// Timestamp cuando se capturó el input
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Milisegundos transcurridos desde el input anterior
        /// </summary>
        public int MillisecondsSincePrevious { get; set; }

        public TimedInput()
        {
            Timestamp = DateTime.Now;
        }

        public TimedInput(string command, int timing)
        {
            Command = command;
            MillisecondsSincePrevious = timing;
            Timestamp = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Command} T:{MillisecondsSincePrevious}";
        }
    }
}
