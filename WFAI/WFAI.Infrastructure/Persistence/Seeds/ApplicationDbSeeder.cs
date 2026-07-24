using WFAI.Infrastructure.Persistence.Contexts;

namespace WFAI.Infrastructure.Persistence.DbInitializers
{
    public class ApplicationDbSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly IdentityDbSeeder identityDbSeeder;
        private readonly FeaturesDbSeeder featuresDbSeeder;

        public ApplicationDbSeeder(ApplicationDbContext context,  IdentityDbSeeder identityDbSeeder,FeaturesDbSeeder featuresDbSeeder)
        {
            _context = context;
            this.identityDbSeeder = identityDbSeeder;
            this.featuresDbSeeder = featuresDbSeeder;
        }

      

        public async Task SeedApplicationDatabaseAsync()
        {
            await CheckAndApplyPendingMigrationAsync();

            await identityDbSeeder.SeedIdentityDatabaseAsync();
            await featuresDbSeeder.SeedFeaturesDatabaseAsync();
        }

        private async Task CheckAndApplyPendingMigrationAsync()
        {
            if (_context.Database.IsRelational())
            {
                if ((await _context.Database.GetPendingMigrationsAsync()).Any())
                {
                    await _context.Database.MigrateAsync();
                }
            }
        }

    }
}