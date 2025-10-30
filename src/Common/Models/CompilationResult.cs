using System;
using System.Collections.Generic;
using MortalKombatCompiler.Common.Models;

namespace Common.Models
{
    /// <summary>
    /// Resultado de la compilación de una secuencia de inputs
    /// </summary>
    public class CompilationResult
    {
        /// <summary>
        /// Indica si la compilación fue exitosa
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Tipo de movimiento detectado: FATALITY o BRUTALITY
        /// </summary>
        public string MoveType { get; set; }

        /// <summary>
        /// Nombre del movimiento (ej: "SELF DESTRUCT")
        /// </summary>
        public string MoveName { get; set; }

        /// <summary>
        /// Identificador único del movimiento
        /// </summary>
        public string MoveId { get; set; }

        /// <summary>
        /// Lista de errores encontrados durante la compilación
        /// </summary>
        public List<string> Errors { get; set; }

        /// <summary>
        /// Secuencia validada de inputs
        /// </summary>
        public List<TimedInput> ValidatedSequence { get; set; }

        /// <summary>
        /// Código intermedio generado
        /// </summary>
        public string IntermediateCode { get; set; }

        /// <summary>
        /// Duración total de la secuencia en milisegundos
        /// </summary>
        public int TotalDurationMs { get; set; }

        /// <summary>
        /// Timestamp de la compilación
        /// </summary>
        public DateTime CompiledAt { get; set; }

        public CompilationResult()
        {
            Errors = new List<string>();
            ValidatedSequence = new List<TimedInput>();
            CompiledAt = DateTime.Now;
        }
    }
}