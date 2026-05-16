using System.Data;
using Microsoft.Data.Sqlite;
using Serilog;
using Users.API.Data;
using Users.API.ExceptionHandlers;
using Users.API.Extensions;
using Users.API.Repositories;

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
            // Buscamos el archivo XML de documentación que configuramos en el paso 1
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });
        o

        // 3. MANEJO GLOBAL DE EXCEPCIONES
        builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
        builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
        builder.Services.AddProblemDetails();

        // 4. PERSISTENCIA E INFRAESTRUCTURA
        builder.Services.AddSingleton<DatabaseInitializer>();
        builder.Services.AddScoped<UserRepository>();

        // 5. CONFIGURACIÓN ENCAPSULADA DE HEALTH CHECKS (Punto 4.4)
        // Borramos el bloque duplicado; esta extensión ya registra Sqlite y ApiStatusCheck
        builder.Services.AddAppHealthChecks(builder.Configuration);

        // 6. CLIENTES HTTP (Comunicación Inter-Service)
        builder.Services.AddHttpClient("NotificationsClient", client =>
        {
            var url = builder.Configuration["ServicesUrls:NotificationsApi"];
            client.BaseAddress = new Uri(url!);
            client.Timeout = TimeSpan.FromSeconds(5);
        });

        // =========================================================================
        // CONSTRUCCIÓN DE LA APLICACIÓN
        // =========================================================================
        var app = builder.Build();

        // 7. INICIALIZACIÓN DE LA BASE DE DATOS SQLITE
        using (var scope = app.Services.CreateScope())
        {
            scope.ServiceProvider.GetRequiredService<DatabaseInitializer>().Initialize();
        }

        // 8. PIPELINE DE MIDDLEWARES & RUTAS
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseExceptionHandler();

        // Middleware de auditoría de logs
        app.UseAppRequestLogging();

        // 9. ENDPOINTS DE NEGOCIO (Users)
        app.MapUsersEndpoints();

        // 10. ENDPOINTS DE MONITOREO (Punto 4.5)
        // Borramos la línea suelta de MapHealthChecks ya que UseAppHealthChecks expone tanto /health como /health-ui
        app.UseAppHealthChecks();

        app.Run();
    }
}
