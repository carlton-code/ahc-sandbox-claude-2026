using System.ComponentModel.DataAnnotations;

namespace AHC.Sandbox.Infrastructure.Configuration
{
    public sealed class RedisOptions
    {
        public const string SectionName = "Redis";

        [Required]
        public string Configuration { get; set; } = string.Empty;

        public bool UseTls { get; set; } = true;

        // Governs both connect and per-command timeouts (see AddInfrastructure). Kept short
        // deliberately: this is what bounds how much latency a Redis outage adds to a request
        // before CustomerService's fallback-to-database kicks in — not just "how long to wait
        // to connect."
        [Range(100, 30_000)]
        public int ConnectTimeoutMs { get; set; } = 1_000;
    }
}