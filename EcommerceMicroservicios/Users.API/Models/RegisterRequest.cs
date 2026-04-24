using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Users.API.Models
{
      public record RegisterRequest(
        string Nombre,
        string Apellido,
        string Email,
        string Password
    );
}

