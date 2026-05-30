using Serilog;
using Serilog.Events;
using Products.API;
using Products.API.ExceptionHandlers;
using Products.API.Services;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.Hosting.Diagnostics", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le => le.Level >= LogEventLevel.Error)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"))
    .WriteTo.Logger(lc => lc
        .Filter.ByIncludingOnly(le =>
        {
            var esSerilogMiddleware = Serilog.Filters.Matching
                .FromSource("Serilog.AspNetCore.RequestLoggingMiddleware")(le);
            if (!esSerilogMiddleware) return false;
            if (le.Properties.TryGetValue("RequestPath", out var p) &&
                p is Serilog.Events.ScalarValue s && s.Value is string path)
                return !path.Contains("/health") && !path.Contains("/swagger");
            return true;
        })
        .WriteTo.File(
            path: "logs/products-audit.log",
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} | {RequestMethod} | {RequestPath} | {StatusCode}{NewLine}",
            rollingInterval: RollingInterval.Day))
// ARCHIVO: logs de negocio (Information y Warning)- AGREGADO
.WriteTo.Logger(lc => lc
    .Filter.ByIncludingOnly(le =>
        le.Level >= LogEventLevel.Information &&
        le.Level < LogEventLevel.Error)
    .WriteTo.File(
        path: "logs/products-business.log",
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} | {Level:u3} | {Message:lj}{NewLine}",
        rollingInterval: RollingInterval.Day))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

builder.Services.AddHealthChecks()
    .AddCheck<SqliteHealthCheck>("sqlite-db", tags: new[] { "database" })
    .AddCheck("api-status", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: new[] { "api" });

builder.Services.AddHealthChecksUI(setup =>
{
    setup.SetEvaluationTimeInSeconds(60);
    setup.AddHealthCheckEndpoint("Products.API", "/health");
}).AddInMemoryStorage();

builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<ProductRepository>();
builder.Services.AddSingleton<DatabaseInitializer>();
builder.Services.AddHttpClient("OrdersClient", client =>
{
    client.BaseAddress = new Uri("http://localhost:5016"); // puerto de Orders.API
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
    scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().Initialize();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseHttpsRedirection();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, ex) =>
        (ex != null) ? LogEventLevel.Error :
        httpContext.Request.Path.StartsWithSegments("/health")
            ? LogEventLevel.Verbose : LogEventLevel.Information;
});

app.MapControllers();

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecksUI(setup => setup.UIPath = "/health-ui");
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("api"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("database"),
    ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
});



app.MapProductEndpoints();

app.Run();

