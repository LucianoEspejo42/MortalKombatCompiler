using System;
using System.Collections.Generic;
using System.Text;
using Common.Models;
using MortalKombatCompiler.Common.Models;


namespace InputCapture
{
    /// <summary>
    /// Convierte una secuencia de inputs a código fuente para el compilador
    /// </summary>
    public class InputToSourceConverter
    {
        /// <summary>
        /// Convierte una secuencia de inputs a código fuente
        /// </summary>
        public string ConvertToSourceCode(List<TimedInput> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            // Encabezado
            sb.AppendLine("SEQUENCE_START");

            // Cada input con su timing
            foreach (var input in sequence)
            {
                sb.Append(input.Command);

                if (input.MillisecondsSincePrevious > 0)
                {
                    sb.Append($" T:{input.MillisecondsSincePrevious}");
                }

                sb.AppendLine();
            }

            // Cierre
            sb.AppendLine("SEQUENCE_END");

            return sb.ToString();
        }

        /// <summary>
        /// Convierte con comentarios adicionales
        /// </summary>
        public string ConvertToSourceCodeWithComments(List<TimedInput> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();

            sb.AppendLine("// Secuencia capturada de XInput");
            sb.AppendLine($"// Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"// Total inputs: {sequence.Count}");
            sb.AppendLine();

            sb.AppendLine("SEQUENCE_START");

            for (int i = 0; i < sequence.Count; i++)
            {
                var input = sequence[i];
                sb.Append($"{input.Command}");

                if (input.MillisecondsSincePrevious > 0)
                {
                    sb.Append($" T:{input.MillisecondsSincePrevious}");
                }

                sb.AppendLine($"  // Input {i + 1}");
            }

            sb.AppendLine("SEQUENCE_END");

            return sb.ToString();
        }
    }
}