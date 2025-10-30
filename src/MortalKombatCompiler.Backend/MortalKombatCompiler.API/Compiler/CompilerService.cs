using System;
using MortalKombatCompiler.API.Models;

namespace MortalKombatCompiler.API.Compiler
{
    public class CompilerService
    {
        public CompilationResult Compile(string sourceCode)
        {
            try
            {
                var scanner = new Scanner(sourceCode);
                var parser = new Parser(scanner);

                parser.Parse();

                return parser.result;
            }
            catch (Exception ex)
            {
                return new CompilationResult
                {
                    Success = false,
                    Errors = { $"Error fatal durante la compilación: {ex.Message}" }
                };
            }
        }
    }
}