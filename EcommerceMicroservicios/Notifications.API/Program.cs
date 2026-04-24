using Notifications.API.ExceptionHandler;
using Notifications.API.Extensions;
using Serilog;
public partial class Program
{
    private static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // Reemplaza el logging por defecto con Serilog
        builder.Host.UseSerilog();

        //  Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Swagger UI
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        builder.Services.AddExceptionHandler<BaseExceptionHandler>();
        builder.Services.AddProblemDetails();

        app.UseExceptionHandler();


        // Endpoints
        app.MapNotificationEndpoints();

        app.Run();
    }
}

