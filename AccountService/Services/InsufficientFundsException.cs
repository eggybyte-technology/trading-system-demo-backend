using System;

namespace AccountService.Services
{
    /// <summary>
    /// Exception thrown when there are insufficient funds for an operation
    /// </summary>
    public class InsufficientFundsException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InsufficientFundsException class
        /// </summary>
        public InsufficientFundsException() : base("Insufficient funds for requested operation")
        {
        }

        /// <summary>
        /// Initializes a new instance of the InsufficientFundsException class with a specified message
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public InsufficientFundsException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the InsufficientFundsException class with a specified message and inner exception
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <param name="innerException">The exception that is the cause of the current exception</param>
        public InsufficientFundsException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}