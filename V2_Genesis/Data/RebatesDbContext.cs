using Microsoft.EntityFrameworkCore;

using V2_Genesis.Models;

namespace V2_Genesis.Data
{
    public class RebateDBContext : DbContext
    {
       
        public RebateDBContext(DbContextOptions<RebateDBContext> options) : base(options)
        {

        }

        public DbSet<Rebate_Info> Rebate_Infos { get; set; }
        public DbSet<Rebates_Files> Rebates_Files { get; set; }
        public DbSet<Rebate_Section1_PersonalDetails> Rebate_Section1_PersonalDetails { get; set; }
        public DbSet<Rebate_Section2_Addresses> Rebate_Section2_Addresses { get; set; }
        public DbSet<Rebate_Section3_ContactDetails> Rebate_Section3_ContactDetails { get; set; }
        public DbSet<Rebate_Section4_Ownership> Rebate_Section4_Ownerships { get; set; }
        public DbSet<Rebate_Section5_Declaration> Rebate_Section5_Declarations { get; set; }
        public DbSet<Rebate_Section6_FI> Rebate_Section6_FIs { get; set; }
        public DbSet<Rebate_Section7_MinorOccupants> Rebate_Section7_MinorOccupants { get; set; }
        public DbSet<Rebate_Section8_ACS> Rebate_Section8_ACSs { get; set; }
        public DbSet<Rebate_Section9_HeritageDetails> Rebate_Section9_HeritageDetails { get; set; }
        public DbSet<Rebate_Section10_Organisation> Rebate_Section10_Organisations { get; set; }
        public DbSet<Rebate_Section11_SummaryIES> Rebate_Section11_Summaries { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Rebate_Info>().Property(u => u.Rebate_No).HasComputedColumnSql("'REB-RB23-' + cast([Rebate_ID] as varchar)");
            modelBuilder.Entity<Rebate_Section1_PersonalDetails>().Property(u => u.Rebate_Ref_S1).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section2_Addresses>().Property(u => u.Rebate_Ref_S2).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section3_ContactDetails>().Property(u => u.Rebate_Ref_S3).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section4_Ownership>().Property(u => u.Rebate_Ref_S4).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section5_Declaration>().Property(u => u.Rebate_Ref_S5).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section6_FI>().Property(u => u.Rebate_Ref_S6).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section7_MinorOccupants>().Property(u => u.Rebate_Ref_S7).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section8_ACS>().Property(u => u.Rebate_Ref_S8).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section9_HeritageDetails>().Property(u => u.Rebate_Ref_S9).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section10_Organisation>().Property(u => u.Rebate_Ref_S10).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebate_Section11_SummaryIES>().Property(u => u.Rebate_Ref_S11).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
            modelBuilder.Entity<Rebates_Files>().Property(u => u.Rebate_Ref_files).HasComputedColumnSql("'REB-RB23-' + cast([Ref] as varchar)");
        }
    }

}
