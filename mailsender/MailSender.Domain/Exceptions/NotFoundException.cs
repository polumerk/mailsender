using System;
using Dictionaries.Exceptions.Interfaces;

namespace MailSender.Domain.Exceptions
{
    /// <summary>
    /// Исключение при отсутствии объекта.
    /// </summary>
    public class NotFoundException : Exception, IDictionaryException
    {
        public string CodeResult { get; set; }

        public NotFoundException()
        { }

        public NotFoundException(string message, string codeResult)
            : base(message)
        {
            CodeResult = codeResult;
        }

        public NotFoundException(string message, Exception innerException, string codeResult)
            : base(message, innerException)
        {
            CodeResult = codeResult;
        }
    }
}