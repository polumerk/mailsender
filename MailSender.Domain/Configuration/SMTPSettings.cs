using System;
using System.Collections.Generic;
using System.Linq;

namespace MailSender.Domain.Configuration
{
    public class SMTPSettings
    {
        /// <summary>
        /// Использование резервных настроек отправки при окончании на указанные строки
        /// </summary>
        public string[] ReserveEmailEndWith { get; set; }
        /// <summary>
        /// Список, для которых не требуется использования резервного, хотя подходят в <see cref="ReserveEmailEndWith"/>
        /// </summary>
        public string[] ReserveEmailBlackList { get; set; }

        public SMTPDataSettings Common { get; set; }
        public SMTPDataSettings Reserve { get; set; }

        public (string[] CommonEmails, string[] ReserveEmails) GetSeparatedEmails(string[] emails)
        {
            var commonEmails = new List<string>();
            var reserveEmails = new List<string>();

            if (ReserveEmailEndWith?.Any() == true)
            {
                foreach (var email in emails)
                {
                    if (ReserveEmailEndWith.Any(x => email.EndsWith(x)))
                    {
                        if (ReserveEmailBlackList?.Any() == true)
                        {
                            if (ReserveEmailBlackList.Contains(email))
                            {
                                commonEmails.Add(email);
                            }
                            else
                            {
                                reserveEmails.Add(email);
                            }
                        }
                        else
                        {
                            reserveEmails.Add(email);
                        }
                    }
                    else
                    {
                        commonEmails.Add(email);
                    }
                }
            }
            else
            {
                return (emails, Array.Empty<string>());
            }

            return (commonEmails.ToArray(), reserveEmails.ToArray());
        }
    }
}