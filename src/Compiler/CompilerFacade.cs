using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common.Models;
using Compiler.CodeGeneration;
using Compiler.SemanticAnalysis;
using Compiler.SymbolTable;
using MortalKombatCompiler.Common.Models;
using Generated;

namespace Compiler
{
    /// <summary>
    /// Fachada principal del compilador - Punto de entrada unificado
    /// </summary>
    public class CompilerFacade
    {
        private readonly TimingValidator _timingValidator;
        private readonly SequenceValidator _sequenceValidator;
        private readonly IntermediateCodeGenerator _codeGenerator;

        public CompilerFacade()
        {
            _timingValidator = new TimingValidator();
            _sequenceValidator = new SequenceValidator();
            _codeGenerator = new IntermediateCodeGenerator();
        }

        /// <summary>
        /// Compila una secuencia de inputs desde código fuente
        /// </summary>
        public CompilationResult CompileFromSource(string sourceCode)
        {
            var result = new CompilationResult();

            try
            {
                // Parsear el código fuente usando COCO/R
                var scanner = new Generated.Scanner(
                    new MemoryStream(Encoding.UTF8.GetBytes(sourceCode))
                );
                var parser = new Generated.Parser(scanner);

                // Ejecutar el parser
                parser.Parse();

                // Obtener la secuencia parseada
                var inputSequence = parser.inputSequence;

                if (parser.errors.count > 0)
                {
                    result.Success = false;
                    result.Errors.Add("Errores de sintaxis detectados:");
                    // Aquí se agregarían los errores del parser
                    return result;
                }

                // Continuar con el análisis semántico
                return CompileFromSequence(inputSequence);
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Error al compilar: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Compila una secuencia de inputs ya parseada
        /// </summary>
        public CompilationResult CompileFromSequence(List<TimedInput> sequence)
        {
            var result = new CompilationResult();

            try
            {
                // 1. Validar timings
                if (!_timingValidator.ValidateSequence(sequence))
                {
                    result.Success = false;
                    result.Errors.AddRange(_timingValidator.Errors);
                    return result;
                }

                // 2. Identificar el movimiento
                var move = _sequenceValidator.IdentifyMove(sequence);
                if (move == null)
                {
                    result.Success = false;
                    result.Errors.AddRange(_sequenceValidator.Errors);
                    return result;
                }

                // 3. Generar código intermedio
                var intermediateCode = _codeGenerator.Generate(sequence, move);
                var textCode = _codeGenerator.GenerateTextFormat(sequence, move);

                // 4. Llenar el resultado
                result.Success = true;
                result.MoveType = move.Type;
                result.MoveName = move.Name;
                result.MoveId = move.Id;
                result.ValidatedSequence = sequence;
                result.IntermediateCode = textCode;
                result.TotalDurationMs = sequence.Sum(i => i.MillisecondsSincePrevious);

                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Errors.Add($"Error durante la compilación: {ex.Message}");
                return result;
            }
        }

        /// <summary>
        /// Valida si una secuencia parcial es un prefijo válido
        /// </summary>
        public bool IsValidPrefix(List<TimedInput> partialSequence)
        {
            return _sequenceValidator.IsValidPrefixSequence(partialSequence);
        }

        /// <summary>
        /// Obtiene movimientos posibles basados en el prefijo actual
        /// </summary>
        public List<MoveDefinition> GetPossibleMoves(List<TimedInput> partialSequence)
        {
            return _sequenceValidator.GetPossibleMoves(partialSequence);
        }

        /// <summary>
        /// Obtiene estadísticas de timing de una secuencia
        /// </summary>
        public Dictionary<string, object> GetTimingStatistics(List<TimedInput> sequence)
        {
            return _timingValidator.GetTimingStatistics(sequence);
        }
    }
}