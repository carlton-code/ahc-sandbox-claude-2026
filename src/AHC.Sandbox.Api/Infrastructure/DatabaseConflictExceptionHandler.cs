using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AHC.Sandbox.Api.Infrastructure
{
    /// <summary>
    /// <para>
    /// Translates a database constraint violation into <c>409 Conflict</c> instead of letting it
    /// surface as an unhandled 500.
    /// </para>
    /// <para>
    /// The motivating case is <c>DELETE /api/v1/customers/{id}</c>. Every foreign key in this
    /// database is <c>NO_ACTION</c>, and every customer is referenced by something — an address, a
    /// rewards tier, an order, or a <c>SalesIntelligence.CustomerRecommendations</c> row. So
    /// deleting any seeded customer violates a foreign key by design. That's the schema correctly
    /// refusing to let a customer vanish while orders still point at them; the only way to make the
    /// delete "succeed" would be <c>ON DELETE CASCADE</c>, which would destroy order history. See
    /// <c>docs/adr/0009-customer-delete-refuses-rather-than-cascades.md</c>.
    /// </para>
    /// <para>
    /// This matches on the SQL error number rather than probing for dependent rows first. A probe
    /// would have to enumerate every foreign key pointing at the table — including schemas this
    /// codebase otherwise never touches — and would still race with a concurrent insert between the
    /// probe and the delete. The database already knows the answer; this asks it once and
    /// translates the reply.
    /// </para>
    /// </summary>
    public class DatabaseConflictExceptionHandler : IExceptionHandler
    {
        // 547  — FOREIGN KEY / CHECK constraint conflict (the DELETE case above).
        // 2627 — UNIQUE / PRIMARY KEY constraint violation.
        // 2601 — duplicate key row in a unique index.
        // All three mean "the request conflicts with the data that's already there", which is 409.
        private static readonly int[] ConflictErrorNumbers = [547, 2627, 2601];

        private readonly ILogger<DatabaseConflictExceptionHandler> _logger;
        private readonly IProblemDetailsService _problemDetailsService;

        public DatabaseConflictExceptionHandler(
            ILogger<DatabaseConflictExceptionHandler> logger,
            IProblemDetailsService problemDetailsService)
        {
            _logger = logger;
            _problemDetailsService = problemDetailsService;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            // Returning false hands the exception back to the pipeline unhandled, which is what
            // should happen to anything that isn't a constraint conflict — this handler must not
            // swallow real bugs.
            if (exception is not DbUpdateException dbUpdateException ||
                dbUpdateException.InnerException is not SqlException sqlException ||
                !ConflictErrorNumbers.Contains(sqlException.Number))
            {
                return false;
            }

            _logger.LogInformation(
                exception,
                "Database constraint {SqlErrorNumber} blocked {Method} {Path}; returning 409.",
                sqlException.Number,
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

            // Deliberately generic: the SQL message names constraints, tables and columns, which
            // shouldn't be echoed to a caller. The specifics are in the log above.
            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Conflict",
                    Detail = "The request conflicts with existing data and could not be completed. "
                           + "This usually means other records still reference the resource."
                }
            });
        }
    }
}
