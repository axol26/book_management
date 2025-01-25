using Microsoft.EntityFrameworkCore;
using book_management.Models;

namespace book_management.Data
{
    public class LoginDbContext : DbContext
    {
        // Constructor
        public LoginDbContext(DbContextOptions<LoginDbContext> options)
            : base(options)
        {
        }

        // DbSet properties for your entities
        public DbSet<User> Users { get; set; }
        // Add other DbSet properties as needed
    }
}