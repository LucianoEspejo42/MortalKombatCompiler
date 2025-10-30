using System.Collections.Generic;
using Common.Models;

namespace Compiler.SymbolTable
{
    /// <summary>
    /// Define todos los movimientos especiales de Cyrax
    /// </summary>
    public class MoveDefinition
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; } // FATALITY, BRUTALITY
        public List<string> Sequence { get; set; }
        public string Description { get; set; }
        public string Character { get; set; }

        public MoveDefinition()
        {
            Sequence = new List<string>();
        }
    }

    public static class CyraxMoves
    {
        public static readonly MoveDefinition FatalitySelfDestruct = new MoveDefinition
        {
            Id = "FATALITY_SELF_DESTRUCT",
            Name = "Self-Destruct",
            Type = "FATALITY",
            Character = "Cyrax",
            Sequence = new List<string> { "DOWN", "DOWN", "UP", "DOWN", "HP" },
            Description = "Cyrax se autodestruye en una explosión masiva"
        };

        public static readonly MoveDefinition FatalityHelicopter = new MoveDefinition
        {
            Id = "FATALITY_HELICOPTER",
            Name = "Helicopter",
            Type = "FATALITY",
            Character = "Cyrax",
            Sequence = new List<string> { "DOWN", "DOWN", "FORWARD", "UP", "RUN" },
            Description = "Cyrax usa sus hélices para destrozar al oponente"
        };

        public static readonly MoveDefinition Brutality = new MoveDefinition
        {
            Id = "BRUTALITY_CYRAX",
            Name = "Cyrax Brutality",
            Type = "BRUTALITY",
            Character = "Cyrax",
            Sequence = new List<string> { "HP", "LK", "HK", "HK", "LP", "LP", "HP", "LP", "LK", "HK", "LK" },
            Description = "Combinación devastadora de 11 golpes consecutivos"
        };

        public static List<MoveDefinition> AllMoves = new List<MoveDefinition>
        {
            FatalitySelfDestruct,
            FatalityHelicopter,
            Brutality
        };

        /// <summary>
        /// Busca un movimiento por su secuencia de comandos
        /// </summary>
        public static MoveDefinition FindBySequence(List<string> sequence)
        {
            return AllMoves.FirstOrDefault(m => m.Sequence.SequenceEqual(sequence));
        }

        /// <summary>
        /// Verifica si una secuencia es un prefijo válido de algún movimiento
        /// </summary>
        public static bool IsValidPrefix(List<string> sequence)
        {
            if (sequence == null || sequence.Count == 0) return false;

            return AllMoves.Any(move =>
                sequence.Count < move.Sequence.Count &&
                sequence.SequenceEqual(move.Sequence.Take(sequence.Count))
            );
        }
    }
}