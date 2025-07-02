using System.Collections.Generic;
using System.Linq;
using Dictionaries.Configuration;

namespace MailSender.Domain.Configuration
{
    public class MessageQueueExternalSettings : BaseKafkaSettings
    {
        public string RecieveQueue { get; set; }
        public IDictionary<string, string> NotificationQueues { get; set; }
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