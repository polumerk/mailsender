using System.ComponentModel.DataAnnotations;

namespace MailSender.Domain.Entities
{
    public class Status
    {
        [Key]
        public int Id { get; set; }
        [MaxLength(256)]
        public string Name { get; set; }
        [MaxLength(512)]
        public string Description { get; set; }
    }
}