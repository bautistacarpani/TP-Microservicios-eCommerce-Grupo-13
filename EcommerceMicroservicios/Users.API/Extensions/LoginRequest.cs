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
/// <param name="Email">Correo electrónico institucional o personal.</param>
/// <param name="Password">Contraseña segura en texto plano.</param>
public record LoginRequest(string Email,string Password);



