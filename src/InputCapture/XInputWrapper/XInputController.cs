using System;
using System.Collections.Generic;
using System.Threading;
using Common.Constants;
using Common.Models;
using SharpDX.XInput;
using MortalKombatCompiler.Common.Models;


namespace InputCapture.XInputWrapper
{
    /// <summary>
    /// Wrapper para controlar un gamepad XInput (Xbox Controller)
    /// </summary>
    public class XInputController : IDisposable
    {
        private Controller _controller;
        private State _previousState;
        private Timer _pollTimer;
        private DateTime _lastInputTime;
        private bool _isRunning;

        public event EventHandler<TimedInput> OnInputReceived;
        public event EventHandler OnTimeout;
        public event EventHandler<string> OnError;

        public bool IsConnected => _controller?.IsConnected ?? false;
        public UserIndex UserIndex { get; }

        /// <summary>
        /// Mapeo de botones del gamepad a comandos del juego
        /// </summary>
        private readonly Dictionary<GamepadButtonFlags, string> _buttonMap = new()
        {
            { GamepadButtonFlags.DPadUp, "UP" },
            { GamepadButtonFlags.DPadDown, "DOWN" },
            { GamepadButtonFlags.DPadLeft, "LEFT" },
            { GamepadButtonFlags.DPadRight, "RIGHT" },
            { GamepadButtonFlags.X, "LP" },        // Low Punch
            { GamepadButtonFlags.Y, "HP" },        // High Punch
            { GamepadButtonFlags.A, "LK" },        // Low Kick
            { GamepadButtonFlags.B, "HK" },        // High Kick
            { GamepadButtonFlags.LeftShoulder, "RUN" },
            { GamepadButtonFlags.RightShoulder, "BL" }  // Block
        };

        public XInputController(UserIndex userIndex = UserIndex.One)
        {
            UserIndex = userIndex;
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                _controller = new Controller(UserIndex);

                if (!_controller.IsConnected)
                {
                    OnError?.Invoke(this, $"Xbox Controller no conectado en UserIndex {UserIndex}");
                    return;
                }

                _previousState = _controller.GetState();
                _lastInputTime = DateTime.Now;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Error al inicializar controller: {ex.Message}");
            }
        }

        /// <summary>
        /// Inicia el polling del gamepad
        /// </summary>
        public void StartPolling()
        {
            if (_isRunning) return;

            _isRunning = true;
            _pollTimer = new Timer(
                Poll,
                null,
                0,
                TimingConstants.XINPUT_POLL_INTERVAL
            );
        }

        /// <summary>
        /// Detiene el polling del gamepad
        /// </summary>
        public void StopPolling()
        {
            _isRunning = false;
            _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Polling del gamepad (ejecutado cada 16ms ~ 60 FPS)
        /// </summary>
        private void Poll(object state)
        {
            if (!_controller.IsConnected)
            {
                OnError?.Invoke(this, "Controller desconectado");
                return;
            }

            try
            {
                var currentState = _controller.GetState();

                // Solo procesar si hay cambios
                if (currentState.PacketNumber != _previousState.PacketNumber)
                {
                    ProcessStateChange(currentState, _previousState);
                }

                // Verificar timeout
                CheckTimeout();

                _previousState = currentState;
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Error en polling: {ex.Message}");
            }
        }

        /// <summary>
        /// Procesa cambios en el estado del gamepad
        /// </summary>
        private void ProcessStateChange(State current, State previous)
        {
            var currentGamepad = current.Gamepad;
            var previousGamepad = previous.Gamepad;

            // Detectar botones presionados (detección de flanco ascendente)
            foreach (var mapping in _buttonMap)
            {
                bool wasPressed = IsButtonPressed(previousGamepad.Buttons, mapping.Key);
                bool isPressed = IsButtonPressed(currentGamepad.Buttons, mapping.Key);

                // Solo detectar cuando se presiona (no cuando se suelta)
                if (isPressed && !wasPressed)
                {
                    EmitInput(mapping.Value);
                }
            }

            // Detectar movimientos del stick analógico izquierdo
            var analogCommand = DetectAnalogStick(currentGamepad, previousGamepad);
            if (analogCommand != null)
            {
                EmitInput(analogCommand);
            }
        }

        /// <summary>
        /// Detecta comandos del stick analógico
        /// </summary>
        private string DetectAnalogStick(Gamepad current, Gamepad previous)
        {
            const float threshold = 0.5f;
            const short deadzone = 7849; // Deadzone estándar de Xbox

            float currentX = current.LeftThumbX / 32768f;
            float currentY = current.LeftThumbY / 32768f;
            float previousX = previous.LeftThumbX / 32768f;
            float previousY = previous.LeftThumbY / 32768f;

            // Aplicar deadzone
            if (Math.Abs(current.LeftThumbX) < deadzone &&
                Math.Abs(current.LeftThumbY) < deadzone)
                return null;

            // Detectar cambio de neutral a dirección
            bool wasNeutral = Math.Abs(previousX) < threshold && Math.Abs(previousY) < threshold;

            if (wasNeutral)
            {
                if (currentY > threshold) return "UP";
                if (currentY < -threshold) return "DOWN";
                if (currentX < -threshold) return "LEFT";
                if (currentX > threshold) return "RIGHT";
            }

            return null;
        }

        /// <summary>
        /// Emite un input detectado
        /// </summary>
        private void EmitInput(string command)
        {
            var now = DateTime.Now;
            var timeSinceLast = (int)(now - _lastInputTime).TotalMilliseconds;

            // Aplicar debounce
            if (timeSinceLast < TimingConstants.DEBOUNCE_MS)
                return;

            var input = new TimedInput
            {
                Command = command,
                Timestamp = now,
                MillisecondsSincePrevious = timeSinceLast
            };

            OnInputReceived?.Invoke(this, input);
            _lastInputTime = now;
        }

        /// <summary>
        /// Verifica si ha pasado el timeout
        /// </summary>
        private void CheckTimeout()
        {
            var timeSinceLast = (DateTime.Now - _lastInputTime).TotalMilliseconds;

            if (timeSinceLast > TimingConstants.TIMEOUT_MS)
            {
                OnTimeout?.Invoke(this, EventArgs.Empty);
                _lastInputTime = DateTime.Now; // Reset para evitar múltiples triggers
            }
        }

        /// <summary>
        /// Verifica si un botón está presionado
        /// </summary>
        private bool IsButtonPressed(GamepadButtonFlags current, GamepadButtonFlags button)
        {
            return (current & button) != 0;
        }

        /// <summary>
        /// Obtiene el estado actual del gamepad
        /// </summary>
        public State GetCurrentState()
        {
            return _controller?.GetState() ?? default;
        }

        /// <summary>
        /// Resetea el timestamp del último input
        /// </summary>
        public void ResetLastInputTime()
        {
            _lastInputTime = DateTime.Now;
        }

        public void Dispose()
        {
            StopPolling();
            _pollTimer?.Dispose();
            _controller = null;
        }
    }
}