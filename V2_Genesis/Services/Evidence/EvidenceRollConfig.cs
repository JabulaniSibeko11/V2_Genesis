namespace V2_Genesis.Services.Evidence
{
    public record EvidenceRollConfig(
     string ValidateSp,       // userDocuments / userDocuments_Sup1 etc.
     string EvidenceCountSp,  // userDocumentsEvidence / _Sup1 etc.
     string UpdateFileSp,     // UpdateObjFiles
     string UpdateCountSp,    // UpdateEvidenceCount
     string EvidenceSp,       // Evidence SP for file list
     string FileRootPath,     // from appsettings ObjectionRolls
     string AppealRootPath,   // appeal file path
     string ConnectionKey
 );

    public static class EvidenceRollRegistry
    {
        public static IReadOnlyDictionary<string, EvidenceRollConfig> Build(
            IConfiguration config)
        {
            string ObjPath(string key) =>
                config[$"ObjectionRolls:{key}:FileRootPath"] ?? string.Empty;
            string AppPath(string key) =>
                config[$"ObjectionRolls:{key}:AppealRootPath"] ?? string.Empty;
            string QueryPath(string key) =>
                config[$"ObjectionRolls:{key}:QueryRootPath"] ?? string.Empty;
            return new Dictionary<string, EvidenceRollConfig>
            {
                ["Objection"] = new(
                    ValidateSp: "userDocuments",
                    EvidenceCountSp: "userDocumentsEvidence",
                    UpdateFileSp: "UpdateObjFiles",
                    UpdateCountSp: "UpdateEvidenceCount",
                    EvidenceSp: "Evidence",
                    FileRootPath: ObjPath("Objection"),
                    AppealRootPath: AppPath("Objection"),
                    ConnectionKey: "DefaultConnection"),

                ["Objection_Supp1"] = new(
                    ValidateSp: "userDocuments_Sup1",
                    EvidenceCountSp: "userDocumentsEvidence_Sup1",
                    UpdateFileSp: "UpdateObjFiles_Sup1",
                    UpdateCountSp: "UpdateEvidenceCount_Sup1",
                    EvidenceSp: "Evidence_Sup1",
                    FileRootPath: ObjPath("Objection_Supp1"),
                    AppealRootPath: AppPath("Objection_Supp1"),
                    ConnectionKey: "Sup1Connection"),

                ["Objection_Supp2"] = new(
                    ValidateSp: "userDocuments_Sup2",
                    EvidenceCountSp: "userDocumentsEvidence_Sup2",
                    UpdateFileSp: "UpdateObjFiles_Sup2",
                    UpdateCountSp: "UpdateEvidenceCount_Sup2",
                    EvidenceSp: "Evidence_Sup2",
                    FileRootPath: ObjPath("Objection_Supp2"),
                    AppealRootPath: AppPath("Objection_Supp2"),
                    ConnectionKey: "Sup2Connection"),

                ["Objection_Supp3"] = new(
                    ValidateSp: "userDocuments_Sup3",
                    EvidenceCountSp: "userDocumentsEvidence_Sup3",
                    UpdateFileSp: "UpdateObjFiles_Sup3",
                    UpdateCountSp: "UpdateEvidenceCount_Sup3",
                    EvidenceSp: "Evidence_Sup3",
                    FileRootPath: ObjPath("Objection_Supp3"),
                    AppealRootPath: AppPath("Objection_Supp3"),
                    ConnectionKey: "Sup3Connection"),

                ["Objection_Query"] = new(
                    ValidateSp: "userDocuments",
                    EvidenceCountSp: "userDocumentsEvidence",
                    UpdateFileSp: "UpdateObjFiles",
                    UpdateCountSp: "UpdateEvidenceCount",
                    EvidenceSp: "Evidence",
                    FileRootPath: QueryPath("Objection_Query"),
                    AppealRootPath: AppPath("Objection"),
                    ConnectionKey: "DefaultConnection"),
            };
        }
    }
}
