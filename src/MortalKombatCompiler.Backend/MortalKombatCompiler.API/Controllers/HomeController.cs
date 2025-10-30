using System;
using Microsoft.AspNetCore.Mvc;

namespace MortalKombatCompiler.API.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                message = "🚀 Mortal Kombat Compiler API está funcionando!",
                version = "1.0.0",
                endpoints = new
                {
                    compile = "POST /api/compiler/compile",
                    health = "GET /health"
                }
            });
        }

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
        }
    }
}