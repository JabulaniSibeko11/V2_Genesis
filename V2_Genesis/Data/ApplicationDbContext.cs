// ═══════════════════════════════════════════════════════════════
//  Data/ApplicationDbContext.cs — REPLACE full file
//
//  ROOT CAUSE:
//  Section tables (Obj_Section1 … Obj_Section7, Obj_Files, etc.)
//  were created by the V1 app using stored procedures. They have
//  NO auto-increment 'ID' column. EF Core assumes [Key] long = 
//  IDENTITY and generates OUTPUT INSERTED.[ID] on every INSERT —
//  SQL Server error 207: "Invalid column name 'ID'".
//
//  FIX (in OnModelCreating only — no model file changes):
//  • For each 1:1 section model: 'Ref' IS the natural key (it's
//    the FK to Obj_Property_Info, always unique per objection).
//    Tell EF: key = Ref, ValueGeneratedNever, ignore 'ID'.
//  • For Obj_Section_51_Uploads: uses Objection_Ref_51 as key.
//  • Obj_Withdrawals / Que_Withdrawals: use ID_Withdrawal (correct).
// ═══════════════════════════════════════════════════════════════

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using V2_Genesis.Models;
using V2_Genesis.Models.Entities;

namespace V2_Genesis.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
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
        public DbSet<Obj_WithdrawalsModel> Obj_Withdrawals { get; set; }
        public DbSet<Que_WithdrawalsModel> Que_Withdrawals { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ── ASP.NET Core Identity user properties ─────────────
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

            // ── FIX: Section tables — 'ID' does not exist in DB ───
            //
            // These tables were created by the V1 app using SPs and
            // have no auto-increment ID column. The natural key is
            // 'Ref' (FK to Obj_Property_Info, always set before Add).
            //
            // EF Core fluent API overrides the [Key] data annotation.
            // 'ID' is ignored so EF never includes it in any SQL.

            var sectionTypes = new[]
            {
                typeof(Obj_Section1Model),
                typeof(Obj_Section2Model),
                typeof(Obj_Section2QueryModel),
                typeof(Obj_Section3AgriModel),
                typeof(Obj_Section3BusModel),
                typeof(Obj_Section3ResModel),
                typeof(Obj_Section4BusModel),
                typeof(Obj_Section4ResModel),
                typeof(Obj_Section5Model),
                typeof(Obj_Section6Model),
                typeof(Obj_Section7Model),
                typeof(Obj_Files),           // also stores per-objection files 1:1
            };

            foreach (var type in sectionTypes)
            {
                builder.Entity(type, e =>
                {
                    // Ref is the actual primary key (set by service before Add)
                    e.HasKey("Ref");
                    e.Property<long?>("Ref")
                     .ValueGeneratedNever()   // code sets it, not IDENTITY
                     .IsRequired();           // always populated before save

                    // ID does not exist as a column in these DB tables
                    e.Ignore("ID");

                    // Suppress the incomplete FK navigation attribute on Ref.
                    // These models have no Obj_Property_Info navigation property,
                    // so we configure the relationship manually as just a column.
                    // (If EF complains about the [ForeignKey] annotation, it can
                    //  be removed from each model file — it has no navigation prop.)
                });
            }

            // ── Obj_Section_51_Uploads — string objection ref as key ──
            // No Ref (long) column — uses Objection_Ref_51 as the key.
            builder.Entity<Obj_Section_51_Uploads>(e =>
            {
                e.HasKey(x => x.Objection_Ref_51);
                e.Property(x => x.Objection_Ref_51)
                 .ValueGeneratedNever()
                 .IsRequired();
                e.Ignore(x => x.ID);
            });

            // ── Obj_Property_InfoModel — key is Objection_ID ──────
            // Already declared correctly in the model ([Key] on
            // Objection_ID). No change needed.
            // Ensure Objection_No (computed by DB trigger) is treated
            // as a computed value, not a writable column.
            builder.Entity<Obj_Property_InfoModel>(e =>
            {
                e.Property(x => x.Objection_No)
                 .ValueGeneratedOnAddOrUpdate();  // DB-computed trigger
            });

            // ── Obj_Property_Info_AppealModel — key is Appeal_ID ──
            // Already declared correctly in the model.
        }
    }
}