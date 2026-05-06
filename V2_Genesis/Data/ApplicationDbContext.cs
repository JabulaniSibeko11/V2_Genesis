using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Models;
using V2_Genesis.Models.Entities;

namespace V2_Genesis.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        // Add inside ApplicationDbContext class:
        public DbSet<GvList> GvList { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // All Identity tables already exist in Objection DB.
            // Table names stay as ASP.NET Identity defaults.
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.FirstName).HasMaxLength(100);
                entity.Property(u => u.LastName).HasMaxLength(100);
                entity.Property(u => u.IDNumber).HasMaxLength(13);
                entity.Property(u => u.PassportNumber).HasMaxLength(50);
                entity.Property(u => u.CompanyName).HasMaxLength(255);
                entity.Property(u => u.CompanyRegistration).HasMaxLength(100);
                entity.Property(u => u.SAPNumber).HasMaxLength(50);
            });
        }
    }

}
