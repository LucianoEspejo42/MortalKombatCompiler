using System;
using System.Collections.Generic;
using Common.Models;
using InputCapture;
using InputCapture.InputBuffer;
using InputCapture.XInputWrapper;
using SharpDX.XInput;
using MortalKombatCompiler.Common.Models;

namespace MortalKombatUI.Services
{
    /// <summary>
    /// Servicio que integra la captura de inputs con el compilador
    /// </summary>
    public class InputProcessingService : IDisposable
    {
        private readonly XInputController _xinputController;
        private readonly InputBuffer _inputBuffer;
        private readonly InputToSourceConverter _sourceConverter;
        private readonly CompilerService _compilerService;
        private readonly ILogger<InputProcessingService> _logger;

        public event EventHandler<CompilationResult> OnFatalityDetected;
        public event EventHandler<CompilationResult> OnBrutalityDetected;
        public event EventHandler<List<TimedInput>> OnSequenceTimeout;
        public event EventHandler<TimedInput> OnInputCaptured;
        public event EventHandler<string> OnError;

        public bool IsRunning { get; private set; }
        public bool IsControllerConnected => _xinputController?.IsConnected ?? false;

        public InputProcessingService(
            CompilerService compilerService,
            ILogger<InputProcessingService> logger)
        {
            _compilerService = compilerService;
            _logger = logger;

            // Inicializar componentes
            _xinputController = new XInputController(UserIndex.One);
            _inputBuffer = new InputBuffer();
            _sourceConverter = new InputToSourceConverter();

            // Suscribirse a eventos
            _xinputController.OnInputReceived += OnInputReceived;
            _xinputController.OnTimeout += OnControllerTimeout;
            _xinputController.OnError += OnControllerError;

            _inputBuffer.OnInputAdded += OnBufferInputAdded;
            _inputBuffer.OnSequenceComplete += OnBufferSequenceComplete;
            _inputBuffer.OnSequenceTimeout += OnBufferSequenceTimeout;

            _logger.LogInformation("InputProcessingService inicializado");
        }

        /// <summary>
        /// Inicia el procesamiento de inputs
        /// </summary>
        public void Start()
        {
            if (IsRunning) return;

            if (!_xinputController.IsConnected)
            {
                _logger.LogWarning("No se puede iniciar: Xbox Controller no conectado");
                OnError?.Invoke(this, "Xbox Controller no conectado");
                return;
            }

            _xinputController.StartPolling();
            IsRunning = true;

            _logger.LogInformation("Procesamiento de inputs iniciado");
        }

        /// <summary>
        /// Detiene el procesamiento de inputs
        /// </summary>
        public void Stop()
        {
            if (!IsRunning) return;

            _xinputController.StopPolling();
            _inputBuffer.Clear();
            IsRunning = false;

            _logger.LogInformation("Procesamiento de inputs detenido");
        }

        /// <summary>
        /// Resetea la secuencia actual
        /// </summary>
        public void ResetSequence()
        {
            _inputBuffer.Clear();
            _xinputController.ResetLastInputTime();
            _logger.LogInformation("Secuencia reseteada");
        }

        /// <summary>
        /// Callback cuando se recibe un input del controller
        /// </summary>
        private void OnInputReceived(object sender, TimedInput input)
        {
            _logger.LogDebug($"Input recibido: {input.Command} ({input.MillisecondsSincePrevious}ms)");
            _inputBuffer.AddInput(input);
        }

        /// <summary>
        /// Callback cuando se agrega un input al buffer
        /// </summary>
        private void OnBufferInputAdded(object sender, TimedInput input)
        {
            OnInputCaptured?.Invoke(this, input);
        }

        /// <summary>
        /// Callback cuando se completa una secuencia
        /// </summary>
        private async void OnBufferSequenceComplete(object sender, List<TimedInput> sequence)
        {
            _logger.LogInformation($"Secuencia completa con {sequence.Count} inputs");

            try
            {
                // Compilar la secuencia
                var result = await _compilerService.CompileSequenceAsync(sequence);

                if (result.Success)
                {
                    _logger.LogInformation($"{result.MoveType} detectado: {result.MoveName}");

                    // Emitir evento según el tipo
                    if (result.MoveType == "FATALITY")
                        OnFatalityDetected?.Invoke(this, result);
                    else if (result.MoveType == "BRUTALITY")
                        OnBrutalityDetected?.Invoke(this, result);
                }
                else
                {
                    _logger.LogWarning($"Secuencia inválida: {string.Join(", ", result.Errors)}");
                    OnError?.Invoke(this, string.Join("; ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al compilar secuencia");
                OnError?.Invoke(this, $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Callback cuando la secuencia alcanza timeout
        /// </summary>
        private void OnBufferSequenceTimeout(object sender, List<TimedInput> sequence)
        {
            _logger.LogWarning($"Timeout de secuencia con {sequence.Count} inputs");
            OnSequenceTimeout?.Invoke(this, sequence);
        }

        /// <summary>
        /// Callback cuando hay timeout del controller
        /// </summary>
        private void OnControllerTimeout(object sender, EventArgs e)
        {
            _logger.LogDebug("Timeout del controller");
            _inputBuffer.CompleteSequence();
        }

        /// <summary>
        /// Callback cuando hay error del controller
        /// </summary>
        private void OnControllerError(object sender, string error)
        {
            _logger.LogError($"Error del controller: {error}");
            OnError?.Invoke(this, error);
        }

        /// <summary>
        /// Obtiene la secuencia actual
        /// </summary>
        public List<TimedInput> GetCurrentSequence()
        {
            return _inputBuffer.GetCurrentSequence();
        }

        /// <summary>
        /// Obtiene estadísticas de la secuencia actual
        /// </summary>
        public object GetSequenceStatistics()
        {
            return _inputBuffer.GetStatistics();
        }

        public void Dispose()
        {
            Stop();
            _xinputController?.Dispose();
            _logger.LogInformation("InputProcessingService disposed");
        }
    }
}