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
    )
    {
        public UserResponse(string id, string nombre, string apellido, string email, DateTime fechaRegistro, bool activo)
            : this(Guid.Parse(id), nombre, apellido, email, fechaRegistro, activo)
        {
        }
    }
}
