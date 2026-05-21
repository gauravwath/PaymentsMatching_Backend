using System.ComponentModel.DataAnnotations;

namespace PaymentsMatching.Models
{

    // ------------------------------------------------------------------
    // Database entities
    // ------------------------------------------------------------------

    public class MatchSession
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int TotalCount { get; set; }
        public int MatchedCount { get; set; }
        public int OnlySystemCount { get; set; }
        public int OnlyProviderCount { get; set; }
        public int AmountMismatchCount { get; set; }

        public ICollection<MatchResult> Results { get; set; } = new List<MatchResult>();
    }

    public class MatchResult
    {
        public int Id { get; set; }
        public int SessionId { get; set; }

        [MaxLength(100)] public string OrderId { get; set; } = string.Empty;
        [MaxLength(10)] public string Currency { get; set; } = string.Empty;

        public decimal? SystemAmount { get; set; }
        public decimal? ProviderAmount { get; set; }

        [MaxLength(30)] public string Status { get; set; } = string.Empty;

        public bool IsResolved { get; set; }
        [MaxLength(20)] public string? ResolutionSide { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    // ------------------------------------------------------------------
    // DTO / request-response shapes
    // ------------------------------------------------------------------

    public class CsvRow
    {
        public string OrderId { get; set; } = string.Empty;
        public decimal? Amount { get; set; } = null;
        public string Currency { get; set; } = string.Empty;
    }

    public class MatchResultDto
    {
        public int Id { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal? SystemAmount { get; set; }
        public decimal? ProviderAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
        public string? ResolutionSide { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }

    public class MatchSummaryDto
    {
        public int SessionId { get; set; }
        public int TotalCount { get; set; }
        public int MatchedCount { get; set; }
        public int OnlySystemCount { get; set; }
        public int OnlyProviderCount { get; set; }
        public int AmountMismatchCount { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<MatchResultDto> Results { get; set; } = new();
    }

    public class ResolveRequest
    {
        [Required] public string ResolutionSide { get; set; } = string.Empty;   // "System" | "Provider"
    }
}
