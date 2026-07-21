using AHC.Sandbox.Api.Infrastructure;
using AHC.Sandbox.Application;
using AHC.Sandbox.Data;
using AHC.Sandbox.Infrastructure;

namespace AHC.Sandbox.Api
{
    public class Program
    {
        /// <summary>
        /// <para>Application entry point.</para>
        /// <para>
        /// This method configures and starts the ASP.NET Core pipeline:
        /// - Creates the WebApplicationBuilder and registers services required by the API.
        /// - Registers project-level services via the Application, Data and Infrastructure dependency injection extension methods.
        /// - Builds the WebApplication instance.
        /// - Configures middleware for the HTTP request pipeline. In Development environments, OpenAPI is mapped and Swagger UI is enabled.
        /// - Adds authorization and maps controller endpoints, then runs the web host.
        /// </para>
        /// </summary>
        /// <param name="args">Command-line arguments forwarded to the host builder.</param>
        /// <remarks>
        /// <para>
        /// Services registered:
        /// - Controllers via <c>AddControllers()</c>.
        /// - OpenAPI generation via <c>AddEndpointsApiExplorer()</c> and <c>AddOpenApi()</c>.
        /// - Project-specific services via <c>AddApplication()</c>, <c>AddData(IConfiguration)</c>, and <c>AddInfrastructure(IConfiguration)</c>.
        /// </para>
        /// <para>
        /// Development-time behavior:
        /// - When <c>app.Environment.IsDevelopment()</c> is true, the OpenAPI document is mapped and the Swagger UI is enabled.
        /// - The Swagger UI is configured to use the OpenAPI JSON at <c>/openapi/v1.json</c> and labeled "AHC.Sandbox.Api v1".
        /// </para>
        /// <para>
        /// Notes:
        /// - Preserve or extend middleware (e.g., exception handling, HTTPS redirection, CORS) as needed for other environments.
        /// - The current pipeline always enables authorization and controller routing before starting the host.
        /// </para>
        /// </remarks>
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddOpenApi();

            // Generated URLs are lowercased so the OpenAPI document matches the route convention
            // this project already writes down (api/v1/<resource>, lowercase). Without it the
            // [controller] token renders the class name verbatim — "/api/v1/Customers/..." — while
            // AddressesController's explicit templates are lowercase, so the same prefix appeared
            // twice in the document and a generated client would see two resources. Routing itself
            // has always been case-insensitive; this only affects generated links, Location headers
            // and the OpenAPI paths. Safe because no route parameter in this API is a string —
            // every one is :int, so there are no route values to mangle.
            builder.Services.AddRouting(options => options.LowercaseUrls = true);

            builder.Services.AddApplication();
            builder.Services.AddData(builder.Configuration);
            builder.Services.AddInfrastructure(builder.Configuration);

            // AddProblemDetails supplies the IProblemDetailsService the handler writes through, and
            // gives unhandled exceptions a ProblemDetails body instead of an empty 500.
            builder.Services.AddProblemDetails();
            builder.Services.AddExceptionHandler<DatabaseConflictExceptionHandler>();

            var app = builder.Build();

            // Must come before the endpoints it protects. Handlers that return false fall through
            // to the default 500, so this only intercepts what it recognizes.
            app.UseExceptionHandler();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "AHC.Sandbox.Api v1"));
            }

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}