using System.Data;
using Microsoft.Data.Sqlite;
using Notifications.API.Data;
using Notifications.API.Extensions;
using Notifications.API.Handler;
using Notifications.API.Repositories;
using Serilog;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. SISTEMA DE LOGS (Serilog)
        builder.AddAppLogging();

        // 2. DOCUMENTACIÓN (Swagger con Endpoints Explorer)
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            // Buscamos el archivo XML autogenerado por este microservicio
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        // 3. MANEJO GLOBAL DE EXCEIPCIONES
        builder.Services.AddExceptionHandler<NotFoundExceptionHandler>();
        builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        builder.Services.AddExceptionHandler<BaseExceptionHandler>();
        builder.Services.AddProblemDetails();

        // 4. PERSISTENCIA E INFRAESTRUCTURA
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddScoped<NotificationsRepository>();

        // 5. CONFIGURACIÓN ENCAPSULADA DE HEALTH CHECKS (Punto 4.4 del TP)
        // Eliminamos el bloque repetido; esta extensión ya maneja SQLite y APIStatusCheck
        builder.Services.AddAppHealthChecks(builder.Configuration);

        // =========================================================================
        // CONSTRUCCIÓN DE LA APLICACIÓN
        // =========================================================================
        var app = builder.Build();

        // 6. INICIALIZACIÓN DE LA BASE DE DATOS SQLITE
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().Initialize();
        }

        // 7. PIPELINE DE MIDDLEWARES & RUTAS
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler();

        // Middleware de auditoría de logs
        app.UseAppRequestLogging();

        // 8. ENDPOINTS DE NEGOCIO (Notifications)
        app.MapNotificationEndpoints();

        // 9. ENDPOINTS DE MONITOREO (Punto 4.5 del TP)
        app.UseAppHealthChecks();

        app.Run();
    }
}

}

