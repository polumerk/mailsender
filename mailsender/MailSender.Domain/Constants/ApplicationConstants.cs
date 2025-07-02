namespace MailSender.Domain.Constants
{
    public static class ApplicationConstants
    {
        /// <summary>
        /// Путь ConnectionString до БД
        /// </summary>
        public const string ConnectionStringService = "MailSender";
        /// <summary>
        /// Тип БД
        /// </summary>
        public const string ConnectionStringDatabaseType = "MailSenderDatabaseType";
        /// <summary>
        /// SystemSource сервиса + DB Scheme
        /// </summary>
        public const string ApplicationSystemSource = "Email";
        /// <summary>
        /// DB Scheme for exchange tables
        /// </summary>
        public const string ApplicationTemplateServiceScheme = "exch";

        /// <summary>
        /// Разделитель Email
        /// </summary>
        public const string EmailSeparate = ",";
        /// <summary>
        /// Обозначает открывающий разделитель html для вычленения заголовка для email сообщения (subject)
        /// </summary>
        public static string HtmlTitleOpenSymbol = "<title>";
        /// <summary>
        /// Обозначает закрывающий разделитель html для вычленения заголовка для email сообщения (subject)
        /// </summary>
        public static string HtmlTitleCloseSymbol = "</title>";

        /// <summary>
        /// Политики для прав по чтению
        /// </summary>
        public const string PolicyRequireReadWhitelist = "RequireReadWhitelist";
        /// <summary>
        /// Политики для прав по созданию
        /// </summary>
        public const string PolicyRequireCreateWhitelist = "RequireCreateWhitelist";
        /// <summary>
        /// Политики для прав по обновлению
        /// </summary>
        public const string PolicyRequireUpdateWhitelist = "RequireUpdateWhitelist";
        /// <summary>
        /// Политики для прав по удалению
        /// </summary>
        public const string PolicyRequireDeleteWhitelist = "RequireDeleteWhitelist";

        /// <summary>
        /// Успешно.
        /// </summary>
        public const string SUCCESS = "SUCCESS";
        /// <summary>
        /// Ошибка обработки шаблона.
        /// </summary>
        public const string TMPER = "TMPER";
        /// <summary>
        /// Адресат не добавлен в белый список (WL UAT).
        /// </summary>
        public const string WLUAT = "WLUAT";
        /// <summary>
        /// Не найден вложенный файл.
        /// </summary>
        public const string ATFNO = "ATFNO";
        /// <summary>
        /// Ошибка SMTP сервера.
        /// </summary>
        public const string SMTP = "SMTP";
        /// <summary>
        /// Пустой email
        /// </summary>
        public const string EMEMT = "EMEMT";
        /// <summary>
        /// Найден дубликат
        /// </summary>
        public const string DUPLC = "DUPLC";

        /// <summary>
        /// Префикс ключей кеша для дубликатов
        /// </summary>
        public const string CachePrefixForDuplicates = "EmailDPLC_{0}";

        /// <summary>
        /// Ошибка
        /// <br/>
        /// Данные ошибок обработки или предупреждения о некорректной работе.
        /// </summary>
        public const string Error = "Error";
        /// <summary>
        /// Уведомление
        /// <br/>
        /// Данные о выполнении операции или запрос на выполнение определенных действий. На первом этапе результат отправки сообщений (email).
        /// </summary>
        public const string Notification = "Notification";
        /// <summary>
        /// Получение шаблона
        /// </summary>
        public const string TemplateService = "TemplateService";
        /// <summary>
        /// Тип сообщения - уведомление
        /// </summary>
        public const string ApplicationNotificationType = "Email";
    }
}