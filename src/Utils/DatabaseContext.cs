using Microsoft.EntityFrameworkCore;

namespace MPESA_V2_APIV2_MSISDN_DECRYPTER
{
    class DatabaseContext : DbContext // Remove the DbContextOptions<> parameter
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<UserRecord> Users => Set<UserRecord>();

        public DbSet<PhoneNumber> PhoneNumbers => Set<PhoneNumber>();

    }
}