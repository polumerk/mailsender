namespace MailSender.Domain.Enums
{
    /// <summary>
    /// Статус сообщения
    /// </summary>
    public enum StatusEnum
    {
        Created = 1,
        StartSended,
        EndSended,
        Error,
        TemplateReceived,
        Retry,
        Duplicated,
        StartSendedViaCommon,
        EndSendedViaCommon,
        StartSendedViaReserve,
        EndSendedViaReserve
    }
}