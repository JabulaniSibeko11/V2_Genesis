using Dapper;
using System.Data;
using System.Data.SqlClient;
using V2_Genesis.Data;
using V2_Genesis.Models;
using V2_Genesis.Models.Results.Section78;
using V2_Genesis.Models.ViewModels.Section78;
using V2_Genesis.Services.Interfaces;

namespace V2_Genesis.Services.Implementations
{
    public class Section78Service : ISection78Service
    {
        private readonly IConfiguration _config;
        private readonly QueryDbContext _qdb;
        private readonly string _queryConn;

        private const string SP_DETAIL = "IndexObjection";
        private const string SP_LINKED = "DashboardLinkedQ";
        private const string SP_SUBMITTED = "DashboardObjectionQ";

        public Section78Service(IConfiguration config, QueryDbContext qdb)
        {
            _config = config;
            _qdb = qdb;
            _queryConn = config.GetConnectionString("QueryConnection")
                ?? throw new InvalidOperationException(
                    "QueryConnection missing from appsettings.");
        }

        // ── Property detail ──────────────────────────────────────────────
        public async Task<Section78PropertyDetail?> GetPropertyDetailAsync(
            string unitKey, string? valuationKey)
        {
            try
            {
                await using var conn = new SqlConnection(_queryConn);
                return await conn.QueryFirstOrDefaultAsync<Section78PropertyDetail>(
                    SP_DETAIL,
                    new { UnitKey = unitKey, ValuationKey = valuationKey ?? "" },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 30);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[Section78] GetPropertyDetail failed: {ex.Message}");
                return null;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  SUBMIT — mirrors the V1 POST action logic, now in service layer
        // ════════════════════════════════════════════════════════════════
        public async Task<Section78SubmitResult> SubmitQueryAsync(
            // ── Query header ─────────────────────────────────────────
            Que_Property_InfoModel que,
            // ── Shared sections ──────────────────────────────────────
            Obj_Section1Model obj1,
            Obj_Section2Model obj2,
            Obj_Section2QueryModel que1,
            Obj_Section3ResModel objR3,
            Obj_Section3BusModel objB3,
            Obj_Section3AgriModel objA3,
            Obj_Section4BusModel objB4,
            Obj_Section4ResModel objR4,
            Obj_Section5Model obj5,
            Obj_Section6Model obj6,
            Obj_Section7Model obj7,
            Obj_Files obj_file,
            // ── File upload ──────────────────────────────────────────
            List<IFormFile> files,
            List<IFormFile> fileR,
            // ── Config ───────────────────────────────────────────────
            string reviewStat,
            string uploadRootPath,
            string propertyType,
            string userId)
        {
            bool isReview = reviewStat == "R";

            // ── 1. Query header ────────────────────────────────────────
            que.UserID = userId;
            que.Query_Status = "Que-Lodging";
            que.Sub_typ = isReview ? 1 : 0;

            _qdb.Que_Property_Info.Add(que);
            await _qdb.SaveChangesAsync();

            // ── Helper: build reference string ────────────────────────
            string Ref(long id) => isReview
       ? $"Que-GV23-{id}-R"
       : $"Que-GV23-{id}";

            string queryRef = Ref(que.Query_ID);

            // ── 2. Section 1 ───────────────────────────────────────────
            obj1.Ref = que.Query_ID;
            obj1.Objection_Ref_S1 = que.Query_No ?? queryRef;
            _qdb.Obj_Section1.Add(obj1);
            await _qdb.SaveChangesAsync();

            // ── 3. Section 2 ───────────────────────────────────────────
            obj2.Ref = que.Query_ID;
            obj2.Objection_Ref_S2 = queryRef;
            _qdb.Obj_Section2.Add(obj2);
            await _qdb.SaveChangesAsync();

            // ── 4. Section 2 Query (S78 checkboxes A-H) ───────────────
            que1.Ref = que.Query_ID;
            que1.Objection_Ref_SQ = queryRef;
            _qdb.Obj_Section2Query.Add(que1);
            await _qdb.SaveChangesAsync();

            // ── 5. Section 3 (all three property types) ───────────────
            objA3.Ref = que.Query_ID;
            objA3.Objection_Ref_SA3 = queryRef;
            _qdb.Obj_Section3Agri.Add(objA3);
            await _qdb.SaveChangesAsync();

            objB3.Ref = que.Query_ID;
            objB3.Objection_Ref_SB3 = queryRef;
            _qdb.Obj_Section3Bus.Add(objB3);
            await _qdb.SaveChangesAsync();

            objR3.Ref = que.Query_ID;
            objR3.Objection_Ref_SR3 = queryRef;
            _qdb.Obj_Section3Res.Add(objR3);
            await _qdb.SaveChangesAsync();

            // ── 6. Section 4 ───────────────────────────────────────────
            objB4.Ref = que.Query_ID;
            objB4.Objection_Ref_SB4 = queryRef;
            _qdb.Obj_Section4Bus.Add(objB4);
            await _qdb.SaveChangesAsync();

            objR4.Ref = que.Query_ID;
            objR4.Objection_Ref_SR4 = queryRef;
            _qdb.Obj_Section4Res.Add(objR4);
            await _qdb.SaveChangesAsync();

            // ── 7. Section 5 ───────────────────────────────────────────
            obj5.Ref = que.Query_ID;
            obj5.Objection_Ref_S5 = queryRef;
            _qdb.Obj_Section5.Add(obj5);
            await _qdb.SaveChangesAsync();

            // ── 8. Section 6 ───────────────────────────────────────────
            obj6.Ref = que.Query_ID;
            obj6.Objection_Ref_S6 = queryRef;
            _qdb.Obj_Section6.Add(obj6);
            await _qdb.SaveChangesAsync();

            // ── 9. Section 7 (declaration + signature) ─────────────────
            obj7.Ref = que.Query_ID;
            obj7.Objection_Ref_S7 = queryRef;
            obj7.RandomPin = GeneratePin();
            obj7.Section51Pin = GeneratePin();
            _qdb.Obj_Section7.Add(obj7);
            await _qdb.SaveChangesAsync();

            // ── 10. Files ──────────────────────────────────────────────
            string folder = Path.Combine(uploadRootPath, queryRef);
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            // Rep letter
            foreach (var file in fileR)
            {
                var repFolder = Path.Combine(folder, "Rep_Letter");
                if (!Directory.Exists(repFolder))
                    Directory.CreateDirectory(repFolder);

                var name = Path.GetFileName(file.FileName);
                obj_file.Rep_letter = name;
                var path = Path.Combine(repFolder, name);
                await using var s = File.Create(path);
                await file.CopyToAsync(s);
            }

            // Evidence files
            int count = 0;
            foreach (var file in files)
            {
                count++;
                var name = Path.GetFileName(file.FileName);
                var path = Path.Combine(folder, name);

                // Assign to Files1..Files10
                SetFileSlot(obj_file, count, name);

                await using var s = File.Create(path);
                await file.CopyToAsync(s);
            }

            obj_file.Ref = que.Query_ID;
            obj_file.Objection_Ref_files = queryRef;
            obj_file.Evidence_count = count;
            _qdb.Obj_Files.Add(obj_file);
            await _qdb.SaveChangesAsync();

            return new Section78SubmitResult
            {
                QueryRef = queryRef,
                QueryId = que.Query_ID,
                RandomPin = obj7.RandomPin,
                IsReview = isReview,
                IsMulti = propertyType == "Multi",
                FileCount = count,
                Files = new[] {
                obj_file.Files1, obj_file.Files2, obj_file.Files3,
                obj_file.Files4, obj_file.Files5, obj_file.Files6,
                obj_file.Files7, obj_file.Files8, obj_file.Files9,
                obj_file.Files10 },
                Section6 = obj6
            };
        }

        // ── Pin generator ─────────────────────────────────────────────
        private static string GeneratePin()
            => new Random().Next(100000, 999999).ToString();

        private static void SetFileSlot(Obj_Files f, int n, string name)
        {
            switch (n)
            {
                case 1: f.Files1 = name; break;
                case 2: f.Files2 = name; break;
                case 3: f.Files3 = name; break;
                case 4: f.Files4 = name; break;
                case 5: f.Files5 = name; break;
                case 6: f.Files6 = name; break;
                case 7: f.Files7 = name; break;
                case 8: f.Files8 = name; break;
                case 9: f.Files9 = name; break;
                case 10: f.Files10 = name; break;
            }
        }

        // ── Dashboard reads (unchanged) ───────────────────────────────
        public async Task<List<Section78LinkedResult>> GetLinkedAsync(string userId)
        {
            try
            {
                await using var conn = new SqlConnection(_queryConn);
                var rows = await conn.QueryAsync<Section78LinkedResult>(
                    SP_LINKED,
                    new { userName = userId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
                return rows.ToList();
            }
            catch { return new(); }
        }

        public async Task<List<Section78SubmittedResult>> GetSubmittedAsync(string userId)
        {
            try
            {
                await using var conn = new SqlConnection(_queryConn);
                var rows = await conn.QueryAsync<Section78SubmittedResult>(
                    SP_SUBMITTED,
                    new { userName = userId },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: 60);
                return rows.ToList();
            }
            catch { return new(); }
        }
    }
}
