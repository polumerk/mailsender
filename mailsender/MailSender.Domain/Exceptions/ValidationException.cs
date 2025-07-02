using System;
using Dictionaries.Exceptions.Interfaces;

namespace MailSender.Domain.Exceptions
{
    /// <summary>
    /// Исключение при валидации
    /// </summary>
    public class ValidationException : Exception, IDictionaryException
    {
        public string CodeResult { get; set; }

        public ValidationException()
        { }

        public ValidationException(string message)
            : base(message)
        {
        }

        public ValidationException(string message, string codeResult)
            : base(message)
        {
            CodeResult = codeResult;
        }

        public ValidationException(string message, Exception innerException, string codeResult)
            : base(message, innerException)
        {
            CodeResult = codeResult;
        }
    }
}