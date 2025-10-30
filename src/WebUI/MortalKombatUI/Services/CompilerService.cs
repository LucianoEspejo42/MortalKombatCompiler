using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using Compiler;
using MortalKombatCompiler.Common.Models;


namespace MortalKombatUI.Services
{
    /// <summary>
    /// Servicio que expone el compilador a la capa de presentación
    /// </summary>
    public class CompilerService
    {
        private readonly CompilerFacade _compiler;
        private readonly ILogger<CompilerService> _logger;

        public CompilerService(ILogger<CompilerService> logger)
        {
            _compiler = new CompilerFacade();
            _logger = logger;
        }

        /// <summary>
        /// Compila código fuente
        /// </summary>
        public async Task<CompilationResult> CompileSourceAsync(string sourceCode)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Compilando código fuente...");

                    var result = _compiler.CompileFromSource(sourceCode);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            $"Compilación exitosa: {result.MoveType} - {result.MoveName}"
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            $"Compilación fallida. Errores: {string.Join(", ", result.Errors)}"
                        );
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error durante la compilación");
                    return new CompilationResult
                    {
                        Success = false,
                        Errors = new List<string> { $"Error: {ex.Message}" }
                    };
                }
            });
        }

        /// <summary>
        /// Compila una secuencia de inputs directamente
        /// </summary>
        public async Task<CompilationResult> CompileSequenceAsync(List<TimedInput> sequence)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation($"Compilando secuencia de {sequence.Count} inputs...");

                    var result = _compiler.CompileFromSequence(sequence);

                    if (result.Success)
                    {
                        _logger.LogInformation(
                            $"Compilación exitosa: {result.MoveType} - {result.MoveName}"
                        );
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al compilar secuencia");
                    return new CompilationResult
                    {
                        Success = false,
                        Errors = new List<string> { $"Error: {ex.Message}" }
                    };
                }
            });
        }

        /// <summary>
        /// Valida si una secuencia parcial es un prefijo válido
        /// </summary>
        public bool IsValidPrefix(List<TimedInput> partialSequence)
        {
            try
            {
                return _compiler.IsValidPrefix(partialSequence);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar prefijo");
                return false;
            }
        }

        /// <summary>
        /// Obtiene movimientos posibles para una secuencia parcial
        /// </summary>
        public async Task<List<string>> GetPossibleMovesAsync(List<TimedInput> partialSequence)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var moves = _compiler.GetPossibleMoves(partialSequence);
                    return moves.Select(m => m.Name).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al obtener movimientos posibles");
                    return new List<string>();
                }
            });
        }
    }
}