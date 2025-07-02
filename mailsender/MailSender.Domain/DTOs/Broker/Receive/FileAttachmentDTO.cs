namespace MailSender.Domain.DTOs.Broker.Receive
{
    public class FileAttachmentDTO
    {
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string FileBody { get; set; }
    }
}