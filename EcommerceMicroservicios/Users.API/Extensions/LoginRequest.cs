using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Users.API.Extensions;

/// <summary>
/// Credenciales requeridas para iniciar sesión en el sistema.
/// </summary>
public record LoginRequest(
    /// <example>bautista.carpani@example.com</example>
    string Email,
    /// <example>ClaveSegura123!</example>
    string Password
);
