using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nostra.DataLoad.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>());

            // Check if the database already has data
            if (context.Database.GetPendingMigrations().Any())
            {
                await context.Database.MigrateAsync();
            }

            // Add your seed data logic here
            // Example:
            // if (!context.YourEntity.Any())
            // {
            //     context.YourEntity.AddRange(
            //         new YourEntity { Property1 = "Value1", Property2 = "Value2" },
            //         new YourEntity { Property1 = "Value3", Property2 = "Value4" }
            //     );
            //     await context.SaveChangesAsync();
            // }

            await context.SaveChangesAsync();
        }
    }
}