using System;
using Api.Models;

namespace Api.Data
{
    public static class DbInitializer
    {
        public static void Initialize(WhatsappDbContext context)
        {
            // Automatically creates the database schema & tables on localhost SQL Server if not already created
            context.Database.EnsureCreated();

            // NO DUMMY EMPLOYEES: Only legal in-house employees explicitly added by the company/admin will exist.
        }
    }
}
