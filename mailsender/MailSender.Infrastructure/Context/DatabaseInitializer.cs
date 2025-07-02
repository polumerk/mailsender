using System;
using System.Threading.Tasks;
using MailSender.Infrastructure.Context.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MailSender.Infrastructure.Context
{
    public class DatabaseInitializer : IDatabaseInitializer
    {
        private readonly ApplicationContext _context;
        private readonly ILogger<DatabaseInitializer> _logger;

        public DatabaseInitializer(
            ApplicationContext context,
            ILogger<DatabaseInitializer> logger)
        {
            _logger = logger;
            _context = context;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                if (_context.Database.IsSqlServer() || _context.Database.IsNpgsql())
                {
                    await _context.Database.MigrateAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while initialising the database.");
                throw;
            }
        }
    }
}