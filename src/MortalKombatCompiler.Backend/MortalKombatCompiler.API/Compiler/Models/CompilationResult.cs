using System.Collections.Generic;

namespace MortalKombatCompiler.API.Models
{
    public class CompilationResult
    {
        public bool Success { get; set; }
        public string MoveType { get; set; } // "FATALITY" o "BRUTALITY"
        public string MoveName { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<TimedInput> ValidatedSequence { get; set; }
        public string GeneratedCode { get; set; }
    }
}