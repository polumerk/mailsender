namespace MailSender.Domain.Configuration
{
    public class SMTPDataSettings
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string From { get; set; }
        public string FromDisplayAddress { get; set; }
        public bool UseDefaultCredentials { get; set; }
        /// <summary>
        /// Секретный
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Секретный
        /// </summary>
        public string Password { get; set; }
    }
}