using System;
using System.Collections.Generic;
using System.Linq;
using MortalKombatCompiler.API.Models;  
using MortalKombatCompiler.API.Compiler;                         

namespace MortalKombatCompiler.API.Compiler
{
    public class Parser
    {
        private readonly Scanner scanner;
        private Token currentToken;

        public List<TimedInput> inputSequence = new List<TimedInput>();
        public CompilationResult result = new CompilationResult();

        private const int TIMEOUT_MS = 2000;
        private const int DEBOUNCE_MS = 50;

        private readonly Dictionary<string, List<string>> cyraxMoves = new Dictionary<string, List<string>>
        {
            { "FATALITY_SELF_DESTRUCT", new List<string> { "DOWN", "DOWN", "UP", "DOWN", "HP" } },
            { "FATALITY_HELICOPTER", new List<string> { "DOWN", "DOWN", "FORWARD", "UP", "RUN" } },
            { "BRUTALITY_CYRAX", new List<string> { "HP", "LK", "HK", "HK", "LP", "LP", "HP", "LP", "LK", "HK", "LK" } },
            { "FRIENDSHIP", new List<string> { "RUN", "RUN", "RUN", "UP" } }
        };

        public Parser(Scanner scanner)
        {
            this.scanner = scanner;
            this.currentToken = scanner.Scan();
        }

        public void Parse()
        {
            try
            {
                ParseSequence();
                ValidateAndIdentifyMove();
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Error de parsing: {ex.Message}");
            }
        }

        private void ParseSequence()
        {
            if (currentToken.val != "SEQUENCE_START")
                throw new Exception("Se esperaba SEQUENCE_START");

            currentToken = scanner.Scan();
            inputSequence.Clear();

            while (currentToken.val != "SEQUENCE_END" && currentToken.kind != 0)
            {
                ParseTimedInput();
            }

            if (currentToken.val != "SEQUENCE_END")
                throw new Exception("Se esperaba SEQUENCE_END");
        }

        private void ParseTimedInput()
        {
            string command = currentToken.val;
            int timing = 0;

            currentToken = scanner.Scan();

            if (currentToken.kind == 13) // TIMING token
            {
                var timingStr = currentToken.val.Substring(2);
                timing = int.Parse(timingStr);
                currentToken = scanner.Scan();
            }

            inputSequence.Add(new TimedInput
            {
                Command = command,
                TimingMs = timing
            });
        }

        private void ValidateAndIdentifyMove()
        {
            for (int i = 0; i < inputSequence.Count; i++)
            {
                var input = inputSequence[i];

                if (i > 0 && input.TimingMs > TIMEOUT_MS)
                    result.Errors.Add($"Timeout excedido en input {i + 1}: {input.TimingMs}ms");

                if (i > 0 && input.TimingMs < DEBOUNCE_MS)
                    result.Errors.Add($"Inputs demasiado rápidos en posición {i + 1}: {input.TimingMs}ms");
            }

            var commands = inputSequence.Select(i => i.Command).ToList();

            foreach (var move in cyraxMoves)
            {
                if (commands.SequenceEqual(move.Value))
                {
                    result.Success = true;
                    result.ValidatedSequence = inputSequence;

                    if (move.Key.StartsWith("FATALITY"))
                    {
                        result.MoveType = "FATALITY";
                        result.MoveName = move.Key.Replace("FATALITY_", "").Replace("_", " ");
                    }
                    else if (move.Key.StartsWith("BRUTALITY"))
                    {
                        result.MoveType = "BRUTALITY";
                        result.MoveName = move.Key.Replace("BRUTALITY_", "").Replace("_", " ");
                    }
                    else if (move.Key.StartsWith("FRIENDSHIP"))
                    {
                        result.MoveType = "FRIENDSHIP";
                        result.MoveName = "CYRAX";
                    }

                    GenerateIntermediateCode();
                    return;
                }
            }

            result.Success = false;
            result.Errors.Add("Secuencia no coincide con ningún movimiento conocido");
        }

        private void GenerateIntermediateCode()
        {
            if (!result.Success) return;

            var code = new System.Text.StringBuilder();
            code.AppendLine("// CÓDIGO INTERMEDIO GENERADO");
            code.AppendLine($"// Movimiento: {result.MoveType} - {result.MoveName}");
            code.AppendLine($"// Total de inputs: {inputSequence.Count}");
            code.AppendLine();
            code.AppendLine("EXECUTE {");
            code.AppendLine($"    MOVE_TYPE: {result.MoveType}");
            code.AppendLine($"    MOVE_NAME: {result.MoveName}");
            code.AppendLine("    SEQUENCE: [");

            foreach (var input in inputSequence)
            {
                code.AppendLine($"        {{ COMMAND: \"{input.Command}\", TIMING: {input.TimingMs} }},");
            }

            code.AppendLine("    ]");
            code.AppendLine("    ANIMATION: START");
            code.AppendLine($"    DURATION: {inputSequence.Sum(i => i.TimingMs)}ms");
            code.AppendLine("}");

            result.GeneratedCode = code.ToString();
        }
    }
}
