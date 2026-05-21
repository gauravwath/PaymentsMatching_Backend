using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaymentsMatching.Models;
using PaymentsMatching.Services;

namespace PaymentsMatching.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class MatchController : ControllerBase
    {
        private readonly IMatchingService _svc;
        private readonly ILogger<MatchController> _log;

        public MatchController(IMatchingService svc, ILogger<MatchController> log)
        {
            _svc = svc;
            _log = log;
        }

        /// <summary>
        /// Upload System CSV and Provider CSV, run the matching algorithm, and persist results.
        /// </summary>
        [HttpPost("run")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(MatchSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> RunMatch(
            IFormFile systemFile,
            IFormFile providerFile)
        {
            if (systemFile is null || systemFile.Length == 0)
                return BadRequest(new { error = "systemFile is required." });

            if (providerFile is null || providerFile.Length == 0)
                return BadRequest(new { error = "providerFile is required." });

            if (!IsCsv(systemFile))
                return BadRequest(new { error = "systemFile must be a .csv file." });

            if (!IsCsv(providerFile))
                return BadRequest(new { error = "providerFile must be a .csv file." });

            try
            {
                var summary = await _svc.RunMatchAsync(systemFile, providerFile);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error running match");
                return StatusCode(500, new { error = "Failed to process files: " + ex.Message });
            }
        }

        /// <summary>
        /// Retrieve a previously run match session. Optional filter: unresolved | resolved | all
        /// </summary>
        [HttpGet("{sessionId:int}")]
        [ProducesResponseType(typeof(MatchSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSession(
            int sessionId,
            [FromQuery] string? filter = "all")
        {
            var result = await _svc.GetSessionAsync(sessionId, filter);
            return result is null ? NotFound() : Ok(result);
        }

        /// <summary>
        /// Resolve a single match result row by accepting System or Provider side.
        /// </summary>
        [HttpPatch("results/{resultId:int}/resolve")]
        [ProducesResponseType(typeof(MatchResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Resolve(
            int resultId,
            [FromBody] ResolveRequest request)
        {
            if (request.ResolutionSide is not ("System" or "Provider"))
                return BadRequest(new { error = "resolutionSide must be 'System' or 'Provider'." });

            var updated = await _svc.ResolveAsync(resultId, request.ResolutionSide);
            return updated is null ? NotFound() : Ok(updated);
        }

        private static bool IsCsv(IFormFile file)
            => file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)
            || file.ContentType.Contains("csv", StringComparison.OrdinalIgnoreCase)
            || file.ContentType == "text/plain";
    }
}
