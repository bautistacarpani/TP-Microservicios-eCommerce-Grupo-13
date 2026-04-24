using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Users.API.Models
{
    public record UserResponse(
    Guid Id,
    string Nombre,
    string Apellido,
    string Email,
    DateTime FechaRegistro,
    bool Activo
);
}
