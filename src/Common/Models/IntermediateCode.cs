using System.Collections.Generic;

namespace Common.Models
{
    /// <summary>
    /// Representa el código intermedio generado por el compilador
    /// </summary>
    public class IntermediateCode
    {
        public string MoveType { get; set; }
        public string MoveName { get; set; }
        public string MoveId { get; set; }
        public List<CodeInstruction> Instructions { get; set; }
        public int TotalDuration { get; set; }
        public Dictionary<string, object> Metadata { get; set; }

        public IntermediateCode()
        {
            Instructions = new List<CodeInstruction>();
            Metadata = new Dictionary<string, object>();
        }

        public string ToJson()
        {
            return System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    public class CodeInstruction
    {
        public int Position { get; set; }
        public string Command { get; set; }
        public int Timing { get; set; }
        public string Description { get; set; }
    }
}