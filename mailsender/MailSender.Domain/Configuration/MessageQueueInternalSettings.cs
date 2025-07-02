using System.Collections.Generic;
using System.Linq;
using Dictionaries.Configuration;

namespace MailSender.Domain.Configuration
{
    public class MessageQueueInternalSettings : BaseKafkaSettings
    {
        public string RecieveQueue { get; set; }
        public string TemplateRecieveQueue { get; set; }
        public string TemplateSendQueue { get; set; }
        public IDictionary<string, string> NotificationQueues { get; set; }
        public string ErrorQueue { get; set; }
        public string GetNotificationQueueBySystem(string system)
        {
            system = system.ToLower().Trim();

            if (NotificationQueues.Any() && NotificationQueues.ContainsKey(system))
            {
                return NotificationQueues[system];
            }

            return null;
        }
    }
}