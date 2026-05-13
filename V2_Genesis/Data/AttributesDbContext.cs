using Microsoft.EntityFrameworkCore;
using V2_Genesis.Models.Attributes;

namespace V2_Genesis.Data
{
    public class AttributesDbContext : DbContext
    {
        public AttributesDbContext(DbContextOptions<AttributesDbContext> options)
            : base(options) { }

        public DbSet<LinkedPropertyAttr> LinkedProperties { get; set; } = null!;
        public DbSet<AttrPropertyDetails> AttrPropertyDetails { get; set; }
        public DbSet<AttrValuationDetails> AttrValuationDetails { get; set; }
        public DbSet<AttrAccess> AttrAccess { get; set; }
        public DbSet<AttrContactInfo> AttrContactInfo { get; set; }
        public DbSet<AttrPrimaryAttributes> AttrPrimaryAttributes { get; set; }
        public DbSet<AttrSecondaryAttributes> AttrSecondaryAttributes { get; set; }
        public DbSet<AttrCalculations> AttrCalculations { get; set; }

        public DbSet<AttrRepresentative> AttrRepresentatives { get; set; } = null!;

        public DbSet<AttrDeclaration> AttrDeclarations { get; set; }

        public DbSet<AttrBusinessBuildings> AttrBusinessBuildings { get; set; }
        public DbSet<AttrBusinessSections> AttrBusinessSections { get; set; }
        public DbSet<AttrBusinessGeneral> AttrBusinessGeneral { get; set; }

        public DbSet<AttrDrcBuildings> AttrDrcBuildings { get; set; }
        public DbSet<AttrDrcImprovements> AttrDrcImprovements { get; set; }
        public DbSet<AttrDrcVacantLand> AttrDrcVacantLand { get; set; }
        public DbSet<AttrDrcMarketValueDemolition> AttrDrcMarketValueDemolition { get; set; }

        public DbSet<AttrPropertyInfo> AttrPropertyInfo { get; set; }
        public DbSet<AttrPropertyInfoAuditTrail> AttrPropertyInfoAuditTrail { get; set; }
        public DbSet<AttrWithdrawals> AttrWithdrawals { get; set; }
        public DbSet<AttrFiles> AttrFiles { get; set; }

    }
}