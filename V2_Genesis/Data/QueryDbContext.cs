using Microsoft.EntityFrameworkCore;
using V2_Genesis.Models;

namespace V2_Genesis.Data
{
    public class QueryDbContext : DbContext
    {
        public QueryDbContext(DbContextOptions<QueryDbContext> options)
            : base(options) { }

        // ── Query-specific header table ─────────────────────────────────
        public DbSet<Que_Property_InfoModel> Que_Property_Info { get; set; }

        public DbSet<LinkedProperties> LinkedPropertiesQuery { get; set; }

        public DbSet<Que_WithdrawalsModel> Que_Withdrawals { get; set; }

        // ── Shared section tables (same schema as Objection DB) ─────────
        public DbSet<Obj_Section1Model> Obj_Section1 { get; set; }
        public DbSet<Obj_Section2Model> Obj_Section2 { get; set; }
        public DbSet<Obj_Section2QueryModel> Obj_Section2Query { get; set; }
        public DbSet<Obj_Section3ResModel> Obj_Section3Res { get; set; }
        public DbSet<Obj_Section3BusModel> Obj_Section3Bus { get; set; }
        public DbSet<Obj_Section3AgriModel> Obj_Section3Agri { get; set; }
        public DbSet<Obj_Section4BusModel> Obj_Section4Bus { get; set; }
        public DbSet<Obj_Section4ResModel> Obj_Section4Res { get; set; }
        public DbSet<Obj_Section5Model> Obj_Section5 { get; set; }
        public DbSet<Obj_Section6Model> Obj_Section6 { get; set; }
        public DbSet<Obj_Section7Model> Obj_Section7 { get; set; }
        public DbSet<Obj_Files> Obj_Files { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LinkedProperties>(entity =>
            {
                entity.ToTable("LinkedPropertiesQuery", "dbo");
                entity.Ignore(x => x.ID);
                entity.HasKey(x => new { x.IDProperty, x.UserID });
                entity.Property(x => x.IDProperty).HasColumnName("IDProperty");
                entity.Property(x => x.UserID).HasColumnName("UserID");
                entity.Ignore(x => x.PropertyFrom);
            });

            modelBuilder.Entity<Que_WithdrawalsModel>(entity =>
            {
                entity.ToTable("Que_Withdrawals", "dbo");
                entity.HasKey(x => x.ID_Withdrawal);
                entity.Property(x => x.ID_Withdrawal)
                    .HasColumnName("ID_Withdrawal");
                entity.Property(x => x.Query_Withdrawn)
                    .HasColumnName("Query_Withdrawn");
                entity.Property(x => x.User)
                    .HasColumnName("User");
            });
        }
    }


}
