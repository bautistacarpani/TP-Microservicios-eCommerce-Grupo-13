using Serilog;
using Users.API.ExceptionHandlers;
using Users.API.Extensions; 

public partial class Program
{
    private static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
           .WriteTo.Console()
           .CreateLogger();

        // Reemplaza el logging por defecto con Serilog
        builder.Host.UseSerilog();

        //  Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        builder.Services.AddProblemDetails();

        builder.Services.AddHealthChecks(); 

        var app = builder.Build();

        // Swagger UI
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler();
        app.UseSerilogRequestLogging();

        // Endpoints
        app.MapUsersEndpoints();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
