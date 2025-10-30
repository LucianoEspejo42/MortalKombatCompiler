using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Common.Models;
using Microsoft.AspNetCore.Mvc;
using MortalKombatUI.Services;
using MortalKombatCompiler.Common.Models;

namespace MortalKombatUI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompilerController : ControllerBase
    {
        private readonly CompilerService _compilerService;
        private readonly ILogger<CompilerController> _logger;

        public CompilerController(
            CompilerService compilerService,
            ILogger<CompilerController> logger)
        {
            _compilerService = compilerService;
            _logger = logger;
        }

        /// <summary>
        /// Compila código fuente
        /// POST /api/compiler/compile
        /// </summary>
        [HttpPost("compile")]
        public async Task<ActionResult<CompilationResult>> CompileSource([FromBody] CompileRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.SourceCode))
                {
                    return BadRequest(new { error = "El código fuente no puede estar vacío" });
                }

                var result = await _compilerService.CompileSourceAsync(request.SourceCode);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al compilar código fuente");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Compila una secuencia de inputs
        /// POST /api/compiler/compile-sequence
        /// </summary>
        [HttpPost("compile-sequence")]
        public async Task<ActionResult<CompilationResult>> CompileSequence([FromBody] List<TimedInput> sequence)
        {
            try
            {
                if (sequence == null || sequence.Count == 0)
                {
                    return BadRequest(new { error = "La secuencia no puede estar vacía" });
                }

                var result = await _compilerService.CompileSequenceAsync(sequence);

                if (result.Success)
                {
                    return Ok(result);
                }
                else
                {
                    return BadRequest(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al compilar secuencia");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Valida si una secuencia es un prefijo válido
        /// POST /api/compiler/validate-prefix
        /// </summary>
        [HttpPost("validate-prefix")]
        public ActionResult<PrefixValidationResponse> ValidatePrefix([FromBody] List<TimedInput> sequence)
        {
            try
            {
                bool isValid = _compilerService.IsValidPrefix(sequence);

                return Ok(new PrefixValidationResponse
                {
                    IsValid = isValid,
                    InputCount = sequence.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar prefijo");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene movimientos posibles para una secuencia parcial
        /// POST /api/compiler/possible-moves
        /// </summary>
        [HttpPost("possible-moves")]
        public async Task<ActionResult<List<string>>> GetPossibleMoves([FromBody] List<TimedInput> sequence)
        {
            try
            {
                var moves = await _compilerService.GetPossibleMovesAsync(sequence);
                return Ok(moves);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener movimientos posibles");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    // DTOs
    public class CompileRequest
    {
        public string SourceCode { get; set; }
    }

    public class PrefixValidationResponse
    {
        public bool IsValid { get; set; }
        public int InputCount { get; set; }
    }
}