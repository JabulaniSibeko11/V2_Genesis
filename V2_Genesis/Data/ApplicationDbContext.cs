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

        public DbSet<Obj_Property_InfoModel> Obj_Property_Info { get; set; }
        public DbSet<Obj_Property_Info_AppealModel> Obj_Property_Info_Appeal { get; set; }
        public DbSet<Que_Property_InfoModel> Que_Property_Info { get; set; }
        public DbSet<Obj_Section1Model> Obj_Section1 { get; set; }
        public DbSet<Obj_Section2Model> Obj_Section2 { get; set; }
        public DbSet<Obj_Section2QueryModel> Obj_Section2Query { get; set; }
        public DbSet<Obj_Section3AgriModel> Obj_Section3Agri { get; set; }
        public DbSet<Obj_Section3BusModel> Obj_Section3Bus { get; set; }
        public DbSet<Obj_Section3ResModel> Obj_Section3Res { get; set; }
        public DbSet<Obj_Section4BusModel> Obj_Section4Bus { get; set; }
        public DbSet<Obj_Section4ResModel> Obj_Section4Res { get; set; }
        public DbSet<Obj_Section5Model> Obj_Section5 { get; set; }
        public DbSet<Obj_Section6Model> Obj_Section6 { get; set; }
        public DbSet<Obj_Section7Model> Obj_Section7 { get; set; }
        public DbSet<Obj_Files> Obj_Files { get; set; }
        public DbSet<Obj_Section_51_Uploads> Obj_Section_51_Uploads { get; set; }
        //public DbSet<LinkedProperties> LinkedProperties { get; set; }
        //public DbSet<LinkedPropertiesSup2> linkedPropertiesSup2s { get; set; }
        //public DbSet<LinkedPropertiesQuery> LinkedPropertiesQuery { get; set; }
        //public DbSet<Notification> Notifications { get; set; }
        public DbSet<Obj_WithdrawalsModel> Obj_Withdrawals { get; set; }
        public DbSet<Que_WithdrawalsModel> Que_Withdrawals { get; set; }
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
