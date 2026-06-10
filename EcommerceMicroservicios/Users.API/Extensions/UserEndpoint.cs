using Dapper;
using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Builder;             // 🔥 AGREGADO: Para MapPost y extensiones
using Microsoft.AspNetCore.Http;                // 🔥 AGREGADO: Para StatusCodes y Results
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Users.API.Exceptions;     // Importa tus excepciones personalizadas
using Users.API.Helpers;
using Users.API.Models;          // Importa los modelos (User, DTOs)
using Users.API.Repositories;

namespace Users.API.Extensions;

public static class UsersEndpoints
{
 
    public static void MapUsersEndpoints(this WebApplication app)
    {
        // =========================
        // REGISTER
        // =========================
        app.MapPost("/api/users/register", async (RegisterRequest req, ILogger<Program> logger, UserRepository repo, IHttpClientFactory httpClientFactory, HttpContext context) => // Inyección directa y optimizada) 
        {
            // Log de auditoría: inicio de trámite
            logger.LogInformation("Intento de registro para el email: {Email}", req.Email);

            // 🔴 Validación de datos -> USR-002
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Apellido))
            {
                logger.LogWarning("Registro rechazado: Datos incompletos para {Email}", req.Email);
                throw new ValidationException("USR-002", "Los datos del usuario son inválidos.");
            }

            if (!req.Email.Contains("@"))
            {
                logger.LogWarning("Registro rechazado: Formato de email inválido ({Email})", req.Email);
                throw new ValidationException("USR-002", "El formato del email es inválido.");
            }


            // Validar email duplicado -> USR-001 usando el repositorio optimizado
            var existeUsuario = await repo.GetByEmailAsync(req.Email);
            if (existeUsuario != null)
            {
                logger.LogWarning("Registro fallido: El email {Email} ya se encuentra en el sistema.", req.Email);
                throw new BusinessRuleException("USR-001", $"El email '{req.Email}' ya está registrado.");
            }

            var user = new Models.User
            {
                Id = Guid.NewGuid().ToString(), // 🔥 Forzamos string nativo compatible con la DB
                Nombre = req.Nombre,
                Apellido = req.Apellido,
                Email = req.Email,
                FechaRegistro = DateTime.UtcNow,
                Activo = true,
                IntentosFallidos = 0,
                BloqueadoPorFraude = false,
                PasswordHash = PasswordHelper.HashPassword(req.Password)
            };

           
            await repo.CreateAsync(user);
            logger.LogInformation("Usuario guardado en SQLite con ID: {UserId}", user.Id);

            // 1. Capturamos el Correlation ID del request actual ANTES de abrir el hilo secundario Task.Run
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString(); // Fallback de seguridad
            }

            // Enviar mail de bienvenida a Notifications.API
            try
            {
                var notifClient = httpClientFactory.CreateClient("NotificationsClient");
                notifClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", correlationId.ToString());
                await notifClient.PostAsJsonAsync("api/notifications/send", new
                {
                    usuarioId = user.Id,
                    mensaje = $"¡Bienvenido/a {user.Nombre}! Tu cuenta fue creada exitosamente en nuestro eCommerce. Podés empezar a explorar nuestros productos.",
                    tipo = "Email"
                });
                logger.LogInformation("Mail de bienvenida enviado al usuario {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "No se pudo enviar el mail de bienvenida al usuario {UserId}", user.Id);
            }



            return Results.Created($"/api/users/{user.Id}", new UserResponse(user.Id, user.Nombre, user.Apellido, user.Email, user.FechaRegistro, user.Activo));


        })
        .WithTags("Users")
        .WithSummary("Registra un nuevo usuario en el sistema.")
        .WithDescription("Este endpoint permite crear una nueva cuenta de usuario. Requiere un email único y una contraseña segura. Al registrarse, el usuario recibirá una notificación de bienvenida.")
        .Produces<UserResponse>(StatusCodes.Status201Created) // Contrato de Éxito
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest) // Contrato de Error (Problem Details del TP)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        // Endpoint Pro para validación inter-servicio (Puntos Extra 🚀)
        app.MapGet("/api/users/{id}/exists", async (
            string id,
            UserRepository repo,
            HttpContext context,
            ILogger<Program> logger) =>
        {
            // 1. Intentamos parsear a Guid solo para verificar si el repositorio lo necesita o para pasarlo al método
            if (!Guid.TryParse(id, out var userGuid))
            {
                return Results.BadRequest("El formato del ID es inválido.");
            }
            // 2. Intentamos buscar si el usuario existe en la base de datos
            var user = await repo.GetByIdAsync(userGuid);

            // 3. Extraemos el Correlation ID para los logs estructurados (Punto 5.5)
            if (!context.Request.Headers.TryGetValue("X-Correlation-Id", out var correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            if (user == null)
            {
                // Logueamos el Warning de forma estructurada para Serilog
                logger.LogWarning("Validación de existencia fallida. El usuario con ID {UserId} no existe en el sistema. CorrelationID: {CorrelationId}",
                    id, correlationId);

                // Devolvemos 404 puro (semántico), ideal para consultas rápidas entre APIs
                return Results.NotFound();
            }

            logger.LogInformation("Validación de existencia exitosa para el usuario {UserId}. CorrelationID: {CorrelationId}",
                id, correlationId);

            // Si existe, devolvemos un 200 OK vacío. No hace falta enviar todo el objeto usuario,
            // ahorramos ancho de banda de red en la comunicación interna.
            return Results.Ok(new { email = user.Email });
        })
        .WithName("CheckUserExistence")
        .WithSummary("Verifica si un usuario existe en el sistema por su ID.")
        .WithDescription("Este endpoint es utilizado internamente por otros servicios para validar la existencia de un usuario antes de realizar operaciones relacionadas. Devuelve 200 OK si el usuario existe, o 404 Not Found si no existe.")
        .WithTags("Users")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);


        // =========================
        // LOGIN
        // =========================
        app.MapPost("/api/users/login", async (LoginRequest req, UserRepository repo, ILogger<Program> logger) =>
        {

            // EXPLICACIÓN: Buscamos al usuario por Email para validar sus credenciales.
            var user = await repo.GetByEmailAsync(req.Email);
 
            // 🔴 Usuario inexistente
            if (user is null)
            {
                logger.LogWarning("Login fallido: El usuario {Email} no existe.", req.Email);
                throw new BusinessRuleException("USR-003", "Credenciales incorrectas.");
            }

            // 🔴 Bloqueo por Fraude
            if (user.BloqueadoPorFraude)
            {
                logger.LogCritical("ACCESO DENEGADO: El usuario {Email} intentó ingresar pero está marcado por FRAUDE.", req.Email);
                throw new BusinessRuleException("USR-005", "Su cuenta fue suspendida por razones de seguridad.");
            }

            // 🔴 Bloqueo por Intentos
            if (!user.Activo)
            {
                logger.LogWarning("Intento de acceso a cuenta bloqueada: {Email}", req.Email);
                throw new BusinessRuleException("USR-004", "Su cuenta fue bloqueada por superar el máximo de intentos fallidos.");
            }

            // 🔴 Validación de Password
            if (user.PasswordHash != PasswordHelper.HashPassword(req.Password))
            {
                // Incrementar solo una vez y enviar el Id como string a la firma esperada
                user.IntentosFallidos += 1;
                await repo.UpdateLoginAttemptsAsync(user.Id, user.IntentosFallidos, true);

                logger.LogWarning("Contraseña incorrecta para {Email}. Intento fallido nro: {Intentos}", req.Email, user.IntentosFallidos);


                 // Volvemos a chequear si con este fallo llegó al límite
                var updatedUser = await repo.GetByEmailAsync(req.Email);
                if (updatedUser!.IntentosFallidos >= 3)
                {
                    // 🔥 CORREGIDO: Parseamos el string de vuelta a Guid para que coincida con lo que espera LockAccountAsync
                    if (Guid.TryParse(user.Id, out var userGuid))
                    {
                        await repo.LockAccountAsync(userGuid);
                    }

                    logger.LogCritical("BLOQUEO: Usuario {Email} bloqueado por intentos.", req.Email);
                    throw new BusinessRuleException("USR-004", "Cuenta bloqueada.");
                }
            

                throw new BusinessRuleException("USR-003", "Credenciales incorrectas.");
            }



            // ✅ Login exitoso

            //  Parseamos a Guid para cumplir con la firma original del repositorio
            if (Guid.TryParse(user.Id, out var successfulGuid))
            {
                await repo.ResetAttemptsAsync(successfulGuid);
            }

            logger.LogInformation("Login exitoso: {Email}", user.Email);
            return Results.Ok(new UserResponse(user.Id, user.Nombre, user.Apellido, user.Email, user.FechaRegistro, user.Activo));

        }).WithTags("Users")
        .WithSummary("Permite a un usuario iniciar sesión en el sistema.")
        .WithDescription("Este endpoint permite a un usuario autenticarse en el sistema utilizando su correo electrónico y contraseña. Devuelve un token de acceso si las credenciales son correctas.")
        .Produces<UserResponse>(StatusCodes.Status200OK) // Contrato de Éxito
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest) 
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound); 

    }
    
}