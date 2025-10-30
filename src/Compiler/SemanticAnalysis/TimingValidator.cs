using System.Collections.Generic;
using Common.Constants;
using Common.Models;
using MortalKombatCompiler.Common.Models;

namespace Compiler.SemanticAnalysis
{
    /// <summary>
    /// Valida las restricciones temporales de las secuencias de inputs
    /// </summary>
    public class TimingValidator
    {
        public List<string> Errors { get; private set; }

        public TimingValidator()
        {
            Errors = new List<string>();
        }

        /// <summary>
        /// Valida toda la secuencia de inputs
        /// </summary>
        public bool ValidateSequence(List<TimedInput> sequence)
        {
            Errors.Clear();

            if (sequence == null || sequence.Count == 0)
            {
                Errors.Add("Error: La secuencia está vacía");
                return false;
            }

            bool isValid = true;

            // Validar cada input (empezando desde el segundo)
            for (int i = 1; i < sequence.Count; i++)
            {
                if (!ValidateInput(sequence[i], i))
                {
                    isValid = false;
                }
            }

            // Validar duración total
            if (!ValidateTotalDuration(sequence))
            {
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Valida un input individual
        /// </summary>
        private bool ValidateInput(TimedInput input, int position)
        {
            bool isValid = true;

            // Validar timeout
            if (input.MillisecondsSincePrevious > TimingConstants.TIMEOUT_MS)
            {
                Errors.Add($"Error en input {position + 1}: Timeout excedido. " +
                          $"Tiempo transcurrido: {input.MillisecondsSincePrevious}ms, " +
                          $"Máximo permitido: {TimingConstants.TIMEOUT_MS}ms");
                isValid = false;
            }

            // Validar debounce
            if (input.MillisecondsSincePrevious < TimingConstants.DEBOUNCE_MS)
            {
                Errors.Add($"Error en input {position + 1}: Inputs demasiado rápidos. " +
                          $"Tiempo transcurrido: {input.MillisecondsSincePrevious}ms, " +
                          $"Mínimo requerido: {TimingConstants.DEBOUNCE_MS}ms");
                isValid = false;
            }

            // Validar que el timing no sea negativo
            if (input.MillisecondsSincePrevious < 0)
            {
                Errors.Add($"Error en input {position + 1}: Timing negativo detectado: {input.MillisecondsSincePrevious}ms");
                isValid = false;
            }

            return isValid;
        }

        /// <summary>
        /// Valida la duración total de la secuencia
        /// </summary>
        private bool ValidateTotalDuration(List<TimedInput> sequence)
        {
            int totalDuration = sequence.Sum(i => i.MillisecondsSincePrevious);

            if (totalDuration > TimingConstants.MAX_SEQUENCE_DURATION)
            {
                Errors.Add($"Error: Duración total de la secuencia excede el límite. " +
                          $"Duración: {totalDuration}ms, Máximo: {TimingConstants.MAX_SEQUENCE_DURATION}ms");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Obtiene estadísticas de timing de la secuencia
        /// </summary>
        public Dictionary<string, object> GetTimingStatistics(List<TimedInput> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return new Dictionary<string, object>();

            var timings = sequence.Skip(1).Select(i => i.MillisecondsSincePrevious).ToList();

            return new Dictionary<string, object>
            {
                { "TotalDuration", sequence.Sum(i => i.MillisecondsSincePrevious) },
                { "AverageTiming", timings.Any() ? timings.Average() : 0 },
                { "MinTiming", timings.Any() ? timings.Min() : 0 },
                { "MaxTiming", timings.Any() ? timings.Max() : 0 },
                { "InputCount", sequence.Count }
            };
        }
    }
}