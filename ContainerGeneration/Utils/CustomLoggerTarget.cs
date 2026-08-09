using NLog;
using NLog.Targets;

namespace VIBN_Tools.ContainerGeneration.Utils
{
    /// <summary>
    /// A custom logger target that raises an event when a log message is received.
    /// </summary>
    [Target("CustomLogger")]
    public sealed class CustomLoggerTarget : TargetWithLayout
    {
        /// <summary>
        /// Event which is fired when a certain log message is received.
        /// </summary>
        public event EventHandler<string>? LogReceived;

        /// <summary>
        /// Writes the log event information.
        /// This method checks if the log level is Info or Error, formats the log message,
        /// and raises the <see cref="LogReceived"/> event with the formatted message.
        /// </summary>
        /// <param name="logEvent">The log event information.</param>
        protected override void Write(LogEventInfo logEvent)
        {
            if (logEvent.Level == LogLevel.Info || logEvent.Level == LogLevel.Error)
            {
                var formattedMessage = this.Layout.Render(logEvent);
                LogReceived?.Invoke(this, formattedMessage);
            }
        }
    }
}