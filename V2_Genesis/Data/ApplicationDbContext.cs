using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Models;
using V2_Genesis.Models.Admin;
using V2_Genesis.Models.Entities;
using V2_Genesis.Models.Notifications;
using V2_Genesis.Models.Rates;

namespace V2_Genesis.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public DbSet<GvList> GvList { get; set; }

        public DbSet<LinkedProperties> LinkedProperties { get; set; }

        public DbSet<Obj_Property_InfoModel> Obj_Property_Info { get; set; }

        public DbSet<Obj_Property_Info_AppealModel>
            Obj_Property_Info_Appeal
        { get; set; }

        public DbSet<Que_Property_InfoModel>
            Que_Property_Info
        { get; set; }

        public DbSet<Obj_Section1Model> Obj_Section1 { get; set; }

        public DbSet<Obj_Section2Model> Obj_Section2 { get; set; }

        public DbSet<Obj_Section2QueryModel>
            Obj_Section2Query
        { get; set; }

        public DbSet<Obj_Section3AgriModel>
            Obj_Section3Agri
        { get; set; }

        public DbSet<Obj_Section3BusModel>
            Obj_Section3Bus
        { get; set; }

        public DbSet<Obj_Section3ResModel>
            Obj_Section3Res
        { get; set; }

        public DbSet<Obj_Section4BusModel>
            Obj_Section4Bus
        { get; set; }

        public DbSet<Obj_Section4ResModel>
            Obj_Section4Res
        { get; set; }

        public DbSet<Obj_Section5Model> Obj_Section5 { get; set; }

        public DbSet<Obj_Section6Model> Obj_Section6 { get; set; }

        public DbSet<Obj_Section7Model> Obj_Section7 { get; set; }

        public DbSet<Obj_Files> Obj_Files { get; set; }

        public DbSet<Obj_Section_51_Uploads>
            Obj_Section_51_Uploads
        { get; set; }

        public DbSet<Obj_WithdrawalsModel>
            Obj_Withdrawals
        { get; set; }

        public DbSet<Que_WithdrawalsModel>
            Que_Withdrawals
        { get; set; }

        public DbSet<Notifications> Notifications { get; set; }

        public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

        public DbSet<RateFinancialYear> RateFinancialYears =>
            Set<RateFinancialYear>();

        public DbSet<PropertyRateTariff> PropertyRateTariffs =>
            Set<PropertyRateTariff>();

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ConfigureApplicationUser(builder);
            ConfigureAdminAuditLog(builder);
            ConfigurePropertyInformation(builder);
            ConfigureSectionTables(builder);
            ConfigureSection51Uploads(builder);
            ConfigureRates(builder);
        }

        private static void ConfigureApplicationUser(
            ModelBuilder builder)
        {
            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(x => x.FirstName)
                    .HasMaxLength(100);

                entity.Property(x => x.LastName)
                    .HasMaxLength(100);

                entity.Property(x => x.IDNumber)
                    .HasMaxLength(13);

                entity.Property(x => x.PassportNumber)
                    .HasMaxLength(50);

                entity.Property(x => x.CompanyName)
                    .HasMaxLength(255);

                entity.Property(x => x.CompanyRegistration)
                    .HasMaxLength(100);

                entity.Property(x => x.SAPNumber)
                    .HasMaxLength(50);
            });
        }

        private static void ConfigureAdminAuditLog(
            ModelBuilder builder)
        {
            builder.Entity<AdminAuditLog>(entity =>
            {
                entity.ToTable("AdminAuditLog", "dbo");

                entity.HasKey(x => x.Id);

                entity.Property(x => x.Id)
                    .ValueGeneratedOnAdd();
            });
        }

        private static void ConfigurePropertyInformation(
            ModelBuilder builder)
        {
            builder.Entity<Obj_Property_InfoModel>(entity =>
            {
                entity.HasKey(x => x.Objection_ID);

                entity.Property(x => x.Objection_ID)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Objection_No)
                    .ValueGeneratedOnAddOrUpdate();
            });

            builder.Entity<Obj_Property_Info_AppealModel>(entity =>
            {
                entity.HasKey(x => x.Appeal_ID);

                entity.Property(x => x.Appeal_ID)
                    .ValueGeneratedOnAdd();

                // Appeal_No is assigned by ObjectionFormService after the
                // identity Appeal_ID is generated; it is not computed.
                entity.Property(x => x.Appeal_No)
                    .HasMaxLength(100)
                    .ValueGeneratedNever();
            });

            builder.Entity<Que_Property_InfoModel>(entity =>
            {
                entity.HasKey(x => x.Query_ID);

                entity.Property(x => x.Query_ID)
                    .ValueGeneratedOnAdd();

                entity.Property(x => x.Query_No)
                    .ValueGeneratedOnAddOrUpdate();
            });
        }

        private static void ConfigureSectionTables(
            ModelBuilder builder)
        {
            // The roll databases do not consistently generate the section
            // ID column. Ref is populated by the submission service before
            // Add(), so use it as the EF key and never ask SQL Server to
            // generate or return ID. Section 6 is configured separately
            // because its CLR/database Ref value is text.

            builder.Entity<Obj_Section1Model>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section2Model>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section2QueryModel>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section3AgriModel>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section3BusModel>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section3ResModel>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section4BusModel>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section4ResModel>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section5Model>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Section6Model>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref)
                    .HasMaxLength(100)
                    .ValueGeneratedNever()
                    .IsRequired();
                entity.Ignore(x => x.ID);

                entity.Property(x => x.Objection_Ref_S6)
                    .HasMaxLength(100)
                    .IsRequired(false);
            });

            builder.Entity<Obj_Section7Model>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });

            builder.Entity<Obj_Files>(entity =>
            {
                entity.HasKey(x => x.Ref);
                entity.Property(x => x.Ref).ValueGeneratedNever().IsRequired();
                entity.Ignore(x => x.ID);
            });
        }

        private static void ConfigureSection51Uploads(
            ModelBuilder builder)
        {
            builder.Entity<Obj_Section_51_Uploads>(entity =>
            {
                entity.HasKey(x => x.Objection_Ref_51);

                entity.Property(x => x.Objection_Ref_51)
                    .ValueGeneratedNever()
                    .IsRequired();

                entity.Ignore(x => x.ID);
            });
        }

        private static void ConfigureRates(
            ModelBuilder builder)
        {
            builder.Entity<RateFinancialYear>(entity =>
            {
                entity.HasIndex(x => x.FinancialYear)
                    .IsUnique();
            });

            builder.Entity<PropertyRateTariff>(entity =>
            {
                entity.HasIndex(x => new
                {
                    x.FinancialYearId,
                    x.CategoryCode
                })
                .IsUnique();

                entity.HasOne(x => x.FinancialYear)
                    .WithMany(x => x.Tariffs)
                    .HasForeignKey(x => x.FinancialYearId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
