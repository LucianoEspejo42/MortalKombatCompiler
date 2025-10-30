using System;
using System.Collections.Generic;
using System.Text;
using Common.Models;
using Compiler.SymbolTable;
using MortalKombatCompiler.Common.Models;

namespace Compiler.CodeGeneration
{
    /// <summary>
    /// Genera c�digo intermedio a partir de una secuencia validada
    /// </summary>
    public class IntermediateCodeGenerator
    {
        /// <summary>
        /// Genera c�digo intermedio en formato estructurado
        /// </summary>
        public IntermediateCode Generate(List<TimedInput> sequence, MoveDefinition move)
        {
            var code = new IntermediateCode
            {
                MoveType = move.Type,
                MoveName = move.Name,
                MoveId = move.Id,
                TotalDuration = sequence.Sum(i => i.MillisecondsSincePrevious)
            };

            // Generar instrucciones
            for (int i = 0; i < sequence.Count; i++)
            {
                code.Instructions.Add(new CodeInstruction
                {
                    Position = i,
                    Command = sequence[i].Command,
                    Timing = sequence[i].MillisecondsSincePrevious,
                    Description = GetCommandDescription(sequence[i].Command)
                });
            }

            // Agregar metadata
            code.Metadata["Character"] = move.Character;
            code.Metadata["Description"] = move.Description;
            code.Metadata["InputCount"] = sequence.Count;
            code.Metadata["CompiledAt"] = DateTime.Now.ToString("o");

            return code;
        }

        /// <summary>
        /// Genera c�digo intermedio en formato de texto legible
        /// </summary>
        public string GenerateTextFormat(List<TimedInput> sequence, MoveDefinition move)
        {
            var sb = new StringBuilder();

            sb.AppendLine("//================================================");
            sb.AppendLine("// C�DIGO INTERMEDIO GENERADO");
            sb.AppendLine($"// Compilador: Mortal Kombat 3 Ultimate");
            sb.AppendLine($"// Fecha: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("//================================================");
            sb.AppendLine();

            sb.AppendLine($"// Personaje: {move.Character}");
            sb.AppendLine($"// Movimiento: {move.Type} - {move.Name}");
            sb.AppendLine($"// Descripci�n: {move.Description}");
            sb.AppendLine($"// Total de inputs: {sequence.Count}");
            sb.AppendLine($"// Duraci�n total: {sequence.Sum(i => i.MillisecondsSincePrevious)}ms");
            sb.AppendLine();

            sb.AppendLine("EXECUTE {");
            sb.AppendLine($"    MOVE_TYPE: {move.Type}");
            sb.AppendLine($"    MOVE_ID: {move.Id}");
            sb.AppendLine($"    MOVE_NAME: \"{move.Name}\"");
            sb.AppendLine($"    CHARACTER: \"{move.Character}\"");
            sb.AppendLine();
            sb.AppendLine("    SEQUENCE: [");

            foreach (var input in sequence)
            {
                sb.AppendLine($"        {{");
                sb.AppendLine($"            COMMAND: \"{input.Command}\",");
                sb.AppendLine($"            TIMING: {input.MillisecondsSincePrevious}ms,");
                sb.AppendLine($"            DESCRIPTION: \"{GetCommandDescription(input.Command)}\"");
                sb.AppendLine($"        }},");
            }

            sb.AppendLine("    ]");
            sb.AppendLine();
            sb.AppendLine("    ANIMATION: {");
            sb.AppendLine($"        START: true,");
            sb.AppendLine($"        DURATION: {sequence.Sum(i => i.MillisecondsSincePrevious)}ms,");
            sb.AppendLine($"        TYPE: \"{move.Type}\"");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.AppendLine("// FIN DEL C�DIGO INTERMEDIO");

            return sb.ToString();
        }

        /// <summary>
        /// Obtiene la descripci�n de un comando
        /// </summary>
        private string GetCommandDescription(string command)
        {
            return command switch
            {
                "UP" => "Direcci�n Arriba",
                "DOWN" => "Direcci�n Abajo",
                "LEFT" => "Direcci�n Izquierda",
                "RIGHT" => "Direcci�n Derecha",
                "FORWARD" => "Direcci�n Adelante",
                "BACK" => "Direcci�n Atr�s",
                "LP" => "Low Punch (Golpe Bajo)",
                "HP" => "High Punch (Golpe Alto)",
                "LK" => "Low Kick (Patada Baja)",
                "HK" => "High Kick (Patada Alta)",
                "BL" => "Block (Bloqueo)",
                "RUN" => "Run (Correr)",
                _ => "Comando desconocido"
            };
        }
    }
}