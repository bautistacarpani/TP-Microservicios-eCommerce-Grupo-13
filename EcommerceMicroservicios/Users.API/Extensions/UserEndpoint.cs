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
        app.MapPost("/api/users/register", async (RegisterRequest req, ILogger<Program> logger, UserRepository repo, IHttpClientFactory httpClientFactory) => // Inyección directa y optimizada) 
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

            var user = new User
            {
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


            // =========================================================================
            // COMUNICACIÓN INTER-SERVICE: Notificación de Bienvenida (Fuego y Olvido optimizado)
            // =========================================================================
            _ = Task.Run(async () =>
    {
        try
        {
            var httpClient = httpClientFactory.CreateClient("NotificationsClient");

            // 🔥 EXIGENCIA 5.5: Inyectamos el ID en la cabecera HTTP saliente
            httpClient.DefaultRequestHeaders.Remove("X-Correlation-Id"); // Limpiamos por las dudas
            httpClient.DefaultRequestHeaders.Add("X-Correlation-Id", correlationId.ToString());


            // Reutilizamos el DTO de creación de notificaciones que espera la otra API
            var notificationRequest = new
            {
                UsuarioId = user.Id,
                Mensaje = $"¡Bienvenido {user.Nombre}! Tu cuenta ha sido creada con éxito.",
                Tipo = "Email"
            };

            var respuesta = await httpClient.PostAsJsonAsync("/api/notifications/send", notificationRequest);

            if (respuesta.IsSuccessStatusCode)
            {
                logger.LogInformation("Notificación de bienvenida enviada con éxito para el usuario {UserId}", user.Id);
            }
            else
            {
                logger.LogWarning("La API de Notificaciones respondió con error: {StatusCode}", respuesta.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Capturamos el error aquí para que un fallo en la red de notificaciones 
            // NO le rompa el registro exitoso al usuario final.
            logger.LogError(ex, "No se pudo comunicar con Notifications.API para el usuario {UserId}", user.Id);
        }
    });


            return Results.Created($"/api/users/{user.Id}", new UserResponse(user.Id, user.Nombre, user.Apellido, user.Email, user.FechaRegistro, user.Activo));


        })
        .WithTags("Users")
        .Produces<UserResponse>(StatusCodes.Status201Created) // Contrato de Éxito
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest) // Contrato de Error (Problem Details del TP)
        .Produces<ProblemDetails>(StatusCodes.Status409Conflict); 

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
                await repo.UpdateLoginAttemptsAsync(user.Id.ToString(), user.IntentosFallidos, true);

                logger.LogWarning("Contraseña incorrecta para {Email}. Intento fallido nro: {Intentos}", req.Email, user.IntentosFallidos);


                 // Volvemos a chequear si con este fallo llegó al límite
                var updatedUser = await repo.GetByEmailAsync(req.Email);
                if (updatedUser!.IntentosFallidos >= 3)
                {
                    await repo.LockAccountAsync(user.Id);
                    logger.LogCritical("BLOQUEO: Usuario {Email} bloqueado por intentos.", req.Email);
                    throw new BusinessRuleException("USR-004", "Cuenta bloqueada.");
                }

                throw new BusinessRuleException("USR-003", "Credenciales incorrectas.");
            }



            // ✅ Login exitoso

            await repo.ResetAttemptsAsync(user.Id);

            logger.LogInformation("Login exitoso: {Email}", user.Email);
            return Results.Ok(new UserResponse(user.Id, user.Nombre, user.Apellido, user.Email, user.FechaRegistro, user.Activo));

        }).WithTags("Users")
        .Produces<UserResponse>(StatusCodes.Status200OK) // Contrato de Éxito
        .Produces<ProblemDetails>(StatusCodes.Status400BadRequest) 
        .Produces<ProblemDetails>(StatusCodes.Status404NotFound); 

    }
    
}