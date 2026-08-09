namespace VIBN_Tools.ContainerGeneration.Utils
{
    /// <summary>
    /// Generic wrapper class to encapsulate results of a operation in a consistent manner.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="Value">The value of the result.</param>
    /// <param name="IsSuccess">Indicates whether the operation was successful.</param>
    /// <param name="ErrorMessage">The error message if the operation failed.</param>
    public record Result<T>(T Value, bool IsSuccess, string ErrorMessage)
    {
        /// <summary>
        /// Gets a value indicating whether the operation was successful.
        /// </summary>
        public bool IsSuccess { get; private set; } = IsSuccess;

        /// <summary>
        /// Gets the error message if the operation failed.
        /// </summary>
        public string ErrorMessage { get; private set; } = ErrorMessage;

        /// <summary>
        /// Sets the result as failed with the specified error message.
        /// </summary>
        /// <param name="message">The error message.</param
        public void SetFailed(string message)
        {
            IsSuccess = false;
            ErrorMessage = message;
        }

        /// <summary>
        /// Creates a successful result with the specified value.
        /// </summary>
        /// <param name="value">The value of the result.</param>
        public static Result<T> Success(T value) => new(value, true, string.Empty);

        /// <summary>
        /// Creates a failed result with the specified error message.
        /// </summary>
        /// <param name="errorMessage">The error message.</param>
        /// <returns>A failed <see cref="Result{T}"/>.</returns>
        public static Result<T> Failure(string errorMessage) => new(default!, false, errorMessage);
    }
}
