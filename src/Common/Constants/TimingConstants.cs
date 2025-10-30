namespace Common.Constants
{
    /// <summary>
    /// Constantes de temporización para el compilador
    /// </summary>
    public static class TimingConstants
    {
        /// <summary>
        /// Tiempo máximo permitido entre inputs (milisegundos)
        /// </summary>
        public const int TIMEOUT_MS = 2000;

        /// <summary>
        /// Tiempo mínimo entre inputs para evitar doble lectura (milisegundos)
        /// </summary>
        public const int DEBOUNCE_MS = 50;

        /// <summary>
        /// Intervalo de polling para XInput (milisegundos)
        /// </summary>
        public const int XINPUT_POLL_INTERVAL = 16; // ~60 FPS

        /// <summary>
        /// Tiempo máximo total para completar una secuencia (milisegundos)
        /// </summary>
        public const int MAX_SEQUENCE_DURATION = 10000; // 10 segundos
    }
}