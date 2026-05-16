using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;


/// <summary>
/// Modelo requerido para el registro de un nuevo usuario en la plataforma.
/// </summary>
public record RegisterRequest(
    /// <example>bautista.carpani@example.com</example>
    string Email,
    /// <example>ClaveSegura123!</example>
    string Password,
    /// <example>Bautista</example>
    string Nombre,
    /// <example>Carpani</example>
    string Apellido
);



