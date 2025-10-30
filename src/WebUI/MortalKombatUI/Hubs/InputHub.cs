using System;
using System.Collections.Generic;

using System.Threading.Tasks;
using Common.Models;
using Microsoft.AspNetCore.SignalR;
using MortalKombatUI.Services;
using MortalKombatCompiler.Common.Models;


namespace MortalKombatUI.Hubs
{
    /// <summary>
    /// Hub de SignalR para comunicación en tiempo real con el cliente
    /// </summary>
    public class InputHub : Hub
    {
        private readonly CompilerService _compilerService;
        private readonly ILogger<InputHub> _logger;

        public InputHub(
            CompilerService compilerService,
            ILogger<InputHub> logger)
        {
            _compilerService = compilerService;
            _logger = logger;
        }

        /// <summary>
        /// Cliente envía un input para procesar
        /// </summary>
        public async Task ProcessInput(TimedInput input)
        {
            try
            {
                _logger.LogDebug($"Input recibido de cliente: {input.Command}");

                // Reenviar a todos los clientes conectados
                await Clients.All.SendAsync("InputReceived", input);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar input");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        /// <summary>
        /// Cliente envía una secuencia completa para compilar
        /// </summary>
        public async Task CompileSequence(List<TimedInput> sequence)
        {
            try
            {
                _logger.LogInformation($"Compilando secuencia de {sequence.Count} inputs");

                var result = await _compilerService.CompileSequenceAsync(sequence);

                if (result.Success)
                {
                    await Clients.Caller.SendAsync("CompilationSuccess", result);
                }
                else
                {
                    await Clients.Caller.SendAsync("CompilationError", result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al compilar secuencia");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        /// <summary>
        /// Cliente solicita validar si es un prefijo válido
        /// </summary>
        public async Task ValidatePrefix(List<TimedInput> partialSequence)
        {
            try
            {
                bool isValid = _compilerService.IsValidPrefix(partialSequence);
                var possibleMoves = await _compilerService.GetPossibleMovesAsync(partialSequence);

                await Clients.Caller.SendAsync("PrefixValidation", new
                {
                    IsValid = isValid,
                    PossibleMoves = possibleMoves
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar prefijo");
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Cliente conectado: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            _logger.LogInformation($"Cliente desconectado: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}