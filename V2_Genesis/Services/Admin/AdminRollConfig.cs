namespace V2_Genesis.Services.Admin
{
    public record AdminRollConfig(
      string ObjSearchSp,      // search objections by value
      string AppSearchSp,      // search appeals by appeal no
      string PropSearchSp,     // search appeals by property
      string StatsSp,          // summary stats
      string ConnectionKey     // which DB
  );

    public static class AdminRollRegistry
    {
        public static readonly IReadOnlyDictionary<string, AdminRollConfig> Configs =
            new Dictionary<string, AdminRollConfig>
            {
                ["Objection"] = new(
                    ObjSearchSp: "GetObjectionDetailsBySearch",
                    AppSearchSp: "GV_App_Search",
                    PropSearchSp: "GV_Prop_Search",
                    StatsSp: "AdminDashboardStats",
                    ConnectionKey: "DefaultConnection"
                ),
                ["Objection_Supp1"] = new(
                    ObjSearchSp: "GetObjectionDetailsBySearch_Sup1",
                    AppSearchSp: "GV_App_Search_Sup1",
                    PropSearchSp: "GV_Prop_Search_Sup1",
                    StatsSp: "AdminDashboardStats_Sup1",
                    ConnectionKey: "Sup1Connection"
                ),
                ["Objection_Supp2"] = new(
                    ObjSearchSp: "GetObjectionDetailsBySearch_Sup2",
                    AppSearchSp: "GV_App_Search_Sup2",
                    PropSearchSp: "GV_Prop_Search_Sup2",
                    StatsSp: "AdminDashboardStats_Sup2",
                    ConnectionKey: "Sup2Connection"
                ),
                ["Objection_Supp3"] = new(
                    ObjSearchSp: "GetObjectionDetailsBySearch_Sup3",
                    AppSearchSp: "GV_App_Search_Sup3",
                    PropSearchSp: "GV_Prop_Search_Sup3",
                    StatsSp: "AdminDashboardStats_Sup3",
                    ConnectionKey: "Sup3Connection"
                ),
                ["Objection_Supp4"] = new(
                    ObjSearchSp: "GetObjectionDetailsBySearch_Sup4",
                    AppSearchSp: "GV_App_Search_Sup4",
                    PropSearchSp: "GV_Prop_Search_Sup4",
                    StatsSp: "AdminDashboardStats_Sup4",
                    ConnectionKey: "Sup4Connection"
                ),
                ["Objection_Supp5"] = new(
                    ObjSearchSp: "GetObjectionDetailsBySearch_Sup5",
                    AppSearchSp: "GV_App_Search_Sup5",
                    PropSearchSp: "GV_Prop_Search_Sup5",
                    StatsSp: "AdminDashboardStats_Sup5",
                    ConnectionKey: "Sup5Connection"
                ),
            };
    }
}
