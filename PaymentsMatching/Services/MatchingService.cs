using PaymentsMatching.Data;
using PaymentsMatching.Models;
using System.Globalization;

namespace PaymentsMatching.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly AppDbContext _db;

        public MatchingService(AppDbContext db) => _db = db;

        // ----------------------------------------------------------------
        // Run Match
        // ----------------------------------------------------------------
        public async Task<MatchSummaryDto> RunMatchAsync(IFormFile systemFile, IFormFile providerFile)
        {
            var systemRows = await ParseCsvAsync(systemFile);
            var providerRows = await ParseCsvAsync(providerFile);

            // Key: orderId (case-insensitive) + currency (upper-cased)
            var systemMap = systemRows.ToDictionary(
                r => MakeKey(r.OrderId, r.Currency),
                r => r,
                StringComparer.OrdinalIgnoreCase);

            var providerMap = providerRows.ToDictionary(
                r => MakeKey(r.OrderId, r.Currency),
                r => r,
                StringComparer.OrdinalIgnoreCase);

            var allKeys = systemMap.Keys.Union(providerMap.Keys, StringComparer.OrdinalIgnoreCase).ToList();

            var results = new List<MatchResult>();

            foreach (var key in allKeys)
            {
                systemMap.TryGetValue(key, out var sRow);
                providerMap.TryGetValue(key, out var pRow);

                var baseRow = sRow ?? pRow!;
                string status;

                if (sRow?.Amount is not null && pRow?.Amount is not null)
                    status = sRow.Amount == pRow.Amount ? "MATCHED" : "AMOUNTMISMATCH";
                else
                if (sRow?.Amount is not null)
                    status = "ONLYSYSTEM";
                else
                    status = "ONLYPROVIDER";

                results.Add(new MatchResult
                {
                    OrderId = baseRow.OrderId,
                    Currency = baseRow.Currency.ToUpperInvariant(),
                    SystemAmount = sRow?.Amount,
                    ProviderAmount = pRow?.Amount,
                    Status = status,
                    IsResolved = false
                });
            }

            // Persist session
            var session = new MatchSession
            {
                TotalCount = results.Count,
                MatchedCount = results.Count(r => r.Status == "MATCHED"),
                OnlySystemCount = results.Count(r => r.Status == "ONLYSYSTEM"),
                OnlyProviderCount = results.Count(r => r.Status == "ONLYPROVIDER"),
                AmountMismatchCount = results.Count(r => r.Status == "AMOUNTMISMATCH")
            };

            _db.MatchSessions.Add(session);
            await _db.SaveChangesAsync();

            foreach (var r in results) r.SessionId = session.Id;
            _db.MatchResults.AddRange(results);
            await _db.SaveChangesAsync();

            return ToSummaryDto(session, results);
        }

        // ----------------------------------------------------------------
        // Get Session (with optional filter)
        // ----------------------------------------------------------------
        public async Task<MatchSummaryDto?> GetSessionAsync(int sessionId, string? filter)
        {
            var session = await _db.MatchSessions.FindAsync(sessionId);
            if (session is null) return null;

            var query = _db.MatchResults.Where(r => r.SessionId == sessionId);

            query = filter?.ToLowerInvariant() switch
            {
                "resolved" => query.Where(r => r.IsResolved),
                "unresolved" => query.Where(r => !r.IsResolved),
                _ => query
            };

            var results = query.ToList();
            return ToSummaryDto(session, results);
        }

        // ----------------------------------------------------------------
        // Resolve a single result row
        // ----------------------------------------------------------------
        public async Task<MatchResultDto?> ResolveAsync(int resultId, string resolutionSide)
        {
            var result = await _db.MatchResults.FindAsync(resultId);
            if (result is null) return null;

            result.IsResolved = true;
            result.ResolutionSide = resolutionSide;
            result.ResolvedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            return ToDto(result);
        }

        // ----------------------------------------------------------------
        // Helpers
        // ----------------------------------------------------------------
        private static string MakeKey(string orderId, string currency)
            => $"{orderId.Trim()}|{currency.Trim().ToUpperInvariant()}";

        private static async Task<List<CsvRow>> ParseCsvAsync(IFormFile file)
        {

            string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // Generate unique file name
            string extension = Path.GetExtension(file.FileName);
            string uniqueFileName = $"{Guid.NewGuid()}{extension}";

            // Full file path
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            // get the rows from the saved file and return
            return await ReadCSVAsync(filePath);
        }

        static async Task<List<CsvRow>> ReadCSVAsync(string filePath)
        {
            //string filePath = "C:\\InterViews\\Payment_App\\input\\Provider.csv";
            var csv_Row = new List<CsvRow>();

            var lines = await File.ReadAllLinesAsync(filePath);

            // Skip header row
            for (int i = 1; i < lines.Length; i++)
            {
                var columns = lines[i].Split(',');

                decimal? amount = null;

                if (decimal.TryParse(columns[1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsedAmount))
                {
                    amount = parsedAmount;
                }
                var obj = new CsvRow
                {
                    OrderId = columns[0],
                    Amount = amount,
                    Currency = columns[2]
                };

                csv_Row.Add(obj);
            }
            return csv_Row;
        }

        private static MatchSummaryDto ToSummaryDto(MatchSession s, IEnumerable<MatchResult> results)
            => new()
            {
                SessionId = s.Id,
                TotalCount = s.TotalCount,
                MatchedCount = s.MatchedCount,
                OnlySystemCount = s.OnlySystemCount,
                OnlyProviderCount = s.OnlyProviderCount,
                AmountMismatchCount = s.AmountMismatchCount,
                CreatedAt = s.CreatedAt,
                Results = results.Select(ToDto).ToList()
            };

        private static MatchResultDto ToDto(MatchResult r)
            => new()
            {
                Id = r.Id,
                OrderId = r.OrderId,
                Currency = r.Currency,
                SystemAmount = r.SystemAmount,
                ProviderAmount = r.ProviderAmount,
                Status = r.Status,
                IsResolved = r.IsResolved,
                ResolutionSide = r.ResolutionSide,
                ResolvedAt = r.ResolvedAt
            };
    }

    // CsvHelper class map — handles header names with/without spaces
    //public class CsvRowMap : ClassMap<CsvRow>
    //{
    //    public CsvRowMap()
    //    {
    //        Map(m => m.OrderId).Name("orderId");
    //        Map(m => m.Amount).Name("amount");
    //        Map(m => m.Currency).Name("currency");
    //    }
    //}

    public interface IMatchingService
    {
        Task<MatchSummaryDto> RunMatchAsync(IFormFile systemFile, IFormFile providerFile);
        Task<MatchSummaryDto?> GetSessionAsync(int sessionId, string? filter);
        Task<MatchResultDto?> ResolveAsync(int resultId, string resolutionSide);
    }
}


