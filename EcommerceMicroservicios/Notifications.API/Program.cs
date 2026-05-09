using Notifications.API.Data;
using Notifications.API.Extensions;
using Notifications.API.Handler;
using Serilog;
using Microsoft.Data.Sqlite;
using System.Data;
using Notifications.API.Repositories;


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
        builder.Services.AddHealthChecks() //para la base de datos
    .AddSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!,
               name: "database_sqlite",
               tags: new[] { "db", "sqlite" });

        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddScoped<NotificationsRepository>();

        // Registramos IDbConnection para que Dapper pueda usarla
        builder.Services.AddScoped<IDbConnection>(sp => 
            new SqliteConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

        var app = builder.Build();

        //ejecutamos BD
        using (var scope = app.Services.CreateScope()) 
        { 
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().Initialize(); 
        }

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

