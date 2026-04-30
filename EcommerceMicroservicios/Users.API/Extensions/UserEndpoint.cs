using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Users.API.Helpers;
using Users.API.Models;          // Importa los modelos (User, DTOs)
using Users.API.Exceptions;     // Importa tus excepciones personalizadas

namespace Users.API.Extensions;

public static class UsersEndpoints
{
    // Método de extensión: permite usar app.MapUsersEndpoints() en Program.cs
    public static void MapUsersEndpoints(this WebApplication app)
    {
        var users = new List<User>(); // Lista en memoria (simula base de datos)

        // =========================
        // REGISTER
        // =========================
        app.MapPost("/api/users/register", (RegisterRequest req) =>
        {
            // 🔴 Validación de datos → USR-002
            if (string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password) ||
                string.IsNullOrWhiteSpace(req.Nombre) ||
                string.IsNullOrWhiteSpace(req.Apellido))

            {
                // Lanza excepción que tu handler convierte en HTTP 400
                throw new ValidationException("USR-002", "Los datos del usuario son inválidos.");
            }


            //  Validar formato básico de email
            if (!req.Email.Contains("@"))
            {
                throw new ValidationException("USR-002", "El formato del email es inválido.");
            }

            //  Validar password mínima
            if (req.Password.Length < 8)
            {
                throw new ValidationException("USR-002", "La contraseña debe tener al menos 8 caracteres.");
            }


            // 🔴 Validar email duplicado → USR-001
            if (users.Any(u => u.Email == req.Email))
            {
                // Si ya existe ese email, lanza error de negocio
                throw new BusinessRuleException("USR-001", $"El email '{req.Email}' ya está registrado.");
            }

            // Crear usuario (modelo interno)
            var user = new User
            {
                Nombre = req.Nombre,
                Apellido = req.Apellido,
                Email = req.Email,
                FechaRegistro = DateTime.UtcNow,
                Activo = true,
                IntentosFallidos = 0,
                BloqueadoPorFraude = false,
                // convierte contraseña en un hash
                PasswordHash = PasswordHelper.HashPassword(req.Password)
            };

            users.Add(user); // Guarda el usuario en la lista

            // Construir respuesta SIN password (DTO)
            var response = new UserResponse(
                user.Id,
                user.Nombre,
                user.Apellido,
                user.Email,
                user.FechaRegistro,
                user.Activo
            );

            // Devuelve 201 Created con el usuario creado
            return Results.Created("/api/users/register", response);
        })
        .WithTags("Users"); // Agrupa en Swagger

        // =========================
        // LOGIN
        // =========================
            // Busca usuario por email
            app.MapPost("/api/users/login", (LoginRequest req) =>
            {
                var user = users.FirstOrDefault(u => u.Email == req.Email);

                // Usuario inexistente → credenciales incorrectas
                if (user is null)
                {
                    throw new BusinessRuleException("USR-003", "Credenciales incorrectas.");
                }

                // Bloqueado manualmente por fraude
                if (user.BloqueadoPorFraude)
                {
                    throw new BusinessRuleException("USR-005", "Su cuenta fue suspendida por razones de seguridad.");
                }

                // Bloqueado por intentos fallidos
                if (!user.Activo)
                {
                    throw new BusinessRuleException("USR-004", "Su cuenta fue bloqueada por superar el máximo de intentos fallidos.");
                }

                // Password incorrecta
                if (user.PasswordHash != PasswordHelper.HashPassword(req.Password))
                    {
                    user.IntentosFallidos++;

                    // Si llega a 3 → bloquear
                    if (user.IntentosFallidos >= 3)
                    {
                        user.Activo = false;

                        throw new BusinessRuleException(
                            "USR-004",
                            "Su cuenta fue bloqueada por superar el máximo de intentos fallidos."
                        );
                    }

                    throw new BusinessRuleException("USR-003", "Credenciales incorrectas.");
                }

                // Login exitoso → resetear contador
                user.IntentosFallidos = 0;

                var response = new UserResponse(
                    user.Id,
                    user.Nombre,
                    user.Apellido,
                    user.Email,
                    user.FechaRegistro,
                    user.Activo
                );

              
            // Devuelve 200 OK con datos del usuario
            return Results.Ok(response);
        })
        .WithTags("Users"); // Agrupa en Swagger
    }
}