using Microsoft.AspNetCore.Mvc;
using MortalKombatCompiler.API.Compiler;
using MortalKombatCompiler.API.Models;

namespace MortalKombatCompiler.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompilerController : ControllerBase
    {
        private readonly CompilerService _compilerService;

        public CompilerController()
        {
            _compilerService = new CompilerService();
        }

        [HttpPost("compile")]
        public ActionResult<CompilationResult> Compile([FromBody] CompilationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SourceCode))
            {
                return BadRequest(new CompilationResult
                {
                    Success = false,
                    Errors = { "El código fuente no puede estar vacío" }
                });
            }

            var result = _compilerService.Compile(request.SourceCode);
            return Ok(result);
        }
    }
}