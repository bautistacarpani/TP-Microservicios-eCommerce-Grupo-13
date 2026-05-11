using Microsoft.Data.Sqlite;
using Serilog;
using System.Data;
using Users.API.Data;
using Users.API.ExceptionHandlers;
using Users.API.Extensions; 
using Users.API.Repositories;

public partial class Program
{
    private static void Main(string[] args)
    {

        var builder = WebApplication.CreateBuilder(args);

        // referencia a logging extensions
        builder.AddAppLogging();

        //  Swagger
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddHealthChecks();
        builder.Services.AddHealthChecks() //para la base de datos
    .AddSqlite(builder.Configuration.GetConnectionString("DefaultConnection")!,
               name: "database_sqlite",
               tags: new[] { "db", "sqlite" });
       
        builder.Services.AddScoped<UserRepository>();


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
        app.MapUsersEndpoints();
        app.MapHealthChecks("/health");

        app.Run();
    }
}
