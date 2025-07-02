using MailSender.Domain.Enums;

namespace MailSender.Domain.Configuration
{
    public class MailSettings
    {
        /// <summary>
        /// Использование префикса в заголовке
        /// </summary>
        public string PrefixTitle { get; set; }
        /// <summary>
        /// Использование способа отправки
        /// </summary>
        public EmailSenderTypeEnum MailSenderType { get; set; }
        /// <summary>
        /// Использование белого списка, отправка разрешена, только тем, кто в белом списке
        /// </summary>
        public bool UseWhiteList { get; set; }
        /// <summary>
        /// Количество попыток отправки, где 1 - в целом сама отправка
        /// </summary>
        public int RetryCount { get; set; }
        /// <summary>
        /// Миллисекунды. Если указано попыток отправки (<see cref="RetryCount"/>) 2, то это время ожидания перед началом второй отправки
        /// </summary>
        public int RetryDelay { get; set; }
        /// <summary>
        /// Максимальное количество дубликатов за период, может быть не задан, тогда проверка на дубли не будет работать
        /// </summary>
        public int? MaxDuplicatesPerPeriod { get; set; }
        /// <summary>
        /// Секунды. Период поиска дублей, может быть не задан, тогда проверка на дубли не будет работать
        /// </summary>
        public int? DuplicatesPeriod { get; set; }
    }
}