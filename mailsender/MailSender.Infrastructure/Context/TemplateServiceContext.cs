using System.Reflection;
using MailSender.Domain.Constants;
using MailSender.Domain.Entities.TemplateService;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Context
{
    public class TemplateServiceContext : DbContext
    {
        public TemplateServiceContext(DbContextOptions<TemplateServiceContext> options) : base(options)
        {

        }

        /// <summary>
        /// Построение модели контекста БД
        /// </summary>
        /// <param name="builder"></param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.HasDefaultSchema(ApplicationConstants.ApplicationTemplateServiceScheme.ToLower());
            base.OnModelCreating(builder);
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }

        public DbSet<Output> Outputs { get; set; }
        public DbSet<ContentInfo> ContentsInfo { get; set; }
    }
}