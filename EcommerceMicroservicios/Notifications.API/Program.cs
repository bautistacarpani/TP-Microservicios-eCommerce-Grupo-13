using Notifications.API.Handler;
using Notifications.API.Extensions;
using Serilog;
public partial class Program
{
    private static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Reemplaza el logging por defecto con Serilog
        builder.AddAppLogging();
        builder.Services.AddSwaggerGen();

        //  Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        
        builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        builder.Services.AddExceptionHandler<BaseExceptionHandler>();
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
        app.UseAppRequestLogging();

        // Endpoints
        app.MapNotificationEndpoints();
        app.MapHealthChecks("/health");

        app.Run();
    }
}

