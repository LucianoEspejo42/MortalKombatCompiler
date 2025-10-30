using System.Collections.Generic;
using Common.Models;
using Compiler.SymbolTable;
using MortalKombatCompiler.Common.Models;


namespace Compiler.SemanticAnalysis
{
    /// <summary>
    /// Valida la secuencia de comandos contra los movimientos conocidos
    /// </summary>
    public class SequenceValidator
    {
        public List<string> Errors { get; private set; }

        public SequenceValidator()
        {
            Errors = new List<string>();
        }

        /// <summary>
        /// Identifica el movimiento basado en la secuencia de comandos
        /// </summary>
        public MoveDefinition IdentifyMove(List<TimedInput> sequence)
        {
            Errors.Clear();

            if (sequence == null || sequence.Count == 0)
            {
                Errors.Add("Error: La secuencia está vacía");
                return null;
            }

            // Extraer solo los comandos
            var commands = sequence.Select(i => i.Command).ToList();

            // Buscar coincidencia exacta
            var move = CyraxMoves.FindBySequence(commands);

            if (move == null)
            {
                Errors.Add($"Error: La secuencia [{string.Join(", ", commands)}] " +
                          "no coincide con ninguna Fatality o Brutality conocida de Cyrax.");
                return null;
            }

            return move;
        }

        /// <summary>
        /// Verifica si la secuencia actual es un prefijo válido
        /// </summary>
        public bool IsValidPrefixSequence(List<TimedInput> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return false;

            var commands = sequence.Select(i => i.Command).ToList();
            return CyraxMoves.IsValidPrefix(commands);
        }

        /// <summary>
        /// Obtiene sugerencias de posibles movimientos basados en el prefijo actual
        /// </summary>
        public List<MoveDefinition> GetPossibleMoves(List<TimedInput> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return CyraxMoves.AllMoves;

            var commands = sequence.Select(i => i.Command).ToList();

            return CyraxMoves.AllMoves
                .Where(move =>
                    commands.Count < move.Sequence.Count &&
                    commands.SequenceEqual(move.Sequence.Take(commands.Count))
                )
                .ToList();
        }

        /// <summary>
        /// Obtiene el siguiente input esperado para cada movimiento posible
        /// </summary>
        public Dictionary<string, string> GetNextExpectedInputs(List<TimedInput> sequence)
        {
            var possibleMoves = GetPossibleMoves(sequence);
            var result = new Dictionary<string, string>();

            int currentLength = sequence?.Count ?? 0;

            foreach (var move in possibleMoves)
            {
                if (currentLength < move.Sequence.Count)
                {
                    string nextInput = move.Sequence[currentLength];
                    result[move.Name] = nextInput;
                }
            }

            return result;
        }
    }
}