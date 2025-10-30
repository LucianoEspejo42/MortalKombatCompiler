using System;
using System.Collections.Generic;
using System.Threading;
using Common.Constants;
using Common.Models;
using MortalKombatCompiler.Common.Models;


namespace InputCapture.InputBuffer
{
    /// <summary>
    /// Buffer que almacena y gestiona la secuencia de inputs temporal
    /// </summary>
    public class InputBuffer
    {
        private List<TimedInput> _currentSequence;
        private Timer _timeoutTimer;
        private DateTime _sequenceStartTime;
        private bool _isActive;

        public event EventHandler<List<TimedInput>> OnSequenceComplete;
        public event EventHandler<List<TimedInput>> OnSequenceTimeout;
        public event EventHandler<TimedInput> OnInputAdded;

        public bool IsActive => _isActive;
        public int InputCount => _currentSequence.Count;

        public InputBuffer()
        {
            _currentSequence = new List<TimedInput>();
        }

        /// <summary>
        /// Agrega un nuevo input a la secuencia actual
        /// </summary>
        public void AddInput(TimedInput input)
        {
            if (input == null) return;

            // Primera entrada - iniciar secuencia
            if (_currentSequence.Count == 0)
            {
                _sequenceStartTime = input.Timestamp;
                _isActive = true;
                StartTimeoutTimer();
            }
            else
            {
                // Resetear el timer con cada nuevo input
                ResetTimeoutTimer();
            }

            _currentSequence.Add(input);
            OnInputAdded?.Invoke(this, input);
        }

        /// <summary>
        /// Completa manualmente la secuencia actual
        /// </summary>
        public void CompleteSequence()
        {
            if (_currentSequence.Count == 0) return;

            StopTimeoutTimer();
            _isActive = false;

            var completedSequence = new List<TimedInput>(_currentSequence);
            OnSequenceComplete?.Invoke(this, completedSequence);

            Clear();
        }

        /// <summary>
        /// Cancela la secuencia actual
        /// </summary>
        public void CancelSequence()
        {
            StopTimeoutTimer();
            _isActive = false;
            Clear();
        }

        /// <summary>
        /// Limpia el buffer
        /// </summary>
        public void Clear()
        {
            _currentSequence.Clear();
            _isActive = false;
        }

        /// <summary>
        /// Obtiene la secuencia actual (copia)
        /// </summary>
        public List<TimedInput> GetCurrentSequence()
        {
            return new List<TimedInput>(_currentSequence);
        }

        /// <summary>
        /// Inicia el timer de timeout
        /// </summary>
        private void StartTimeoutTimer()
        {
            _timeoutTimer?.Dispose();
            _timeoutTimer = new Timer(
                OnTimeoutElapsed,
                null,
                TimingConstants.TIMEOUT_MS,
                Timeout.Infinite
            );
        }

        /// <summary>
        /// Resetea el timer de timeout
        /// </summary>
        private void ResetTimeoutTimer()
        {
            _timeoutTimer?.Change(
                TimingConstants.TIMEOUT_MS,
                Timeout.Infinite
            );
        }

        /// <summary>
        /// Detiene el timer de timeout
        /// </summary>
        private void StopTimeoutTimer()
        {
            _timeoutTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        /// <summary>
        /// Callback cuando se alcanza el timeout
        /// </summary>
        private void OnTimeoutElapsed(object state)
        {
            if (_currentSequence.Count == 0) return;

            _isActive = false;
            var timedOutSequence = new List<TimedInput>(_currentSequence);

            OnSequenceTimeout?.Invoke(this, timedOutSequence);
            Clear();
        }

        /// <summary>
        /// Obtiene estadísticas de la secuencia actual
        /// </summary>
        public SequenceStatistics GetStatistics()
        {
            if (_currentSequence.Count == 0)
                return new SequenceStatistics();

            return new SequenceStatistics
            {
                InputCount = _currentSequence.Count,
                TotalDuration = _currentSequence.Sum(i => i.MillisecondsSincePrevious),
                AverageTiming = _currentSequence.Skip(1).Average(i => i.MillisecondsSincePrevious),
                StartTime = _sequenceStartTime,
                IsActive = _isActive
            };
        }
    }

    public class SequenceStatistics
    {
        public int InputCount { get; set; }
        public int TotalDuration { get; set; }
        public double AverageTiming { get; set; }
        public DateTime StartTime { get; set; }
        public bool IsActive { get; set; }
    }
}