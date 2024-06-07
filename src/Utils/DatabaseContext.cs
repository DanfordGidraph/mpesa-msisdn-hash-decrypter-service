using Microsoft.EntityFrameworkCore;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    public class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options) // Remove the DbContextOptions<> parameter
    {
        public DbSet<UserRecord> Users => Set<UserRecord>();

        public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    }

    public class CustomDatabaseContextFactory
    {
        public DatabaseContext CreateDbContext(string? database_path = null)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
            string rootDbPath = Path.Combine(Directory.GetCurrentDirectory(), "src/data/sqlite/database.sqlite");

            optionsBuilder.UseSqlite(database_path ?? rootDbPath);

            return new DatabaseContext(optionsBuilder.Options);
        }
    }
}