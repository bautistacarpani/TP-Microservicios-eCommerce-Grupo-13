using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Users.API.Models
{
    public record User
    {
        public Guid Id { get; init; } = Guid.NewGuid();

        public string Nombre { get; init; } = default!;
        public string Apellido { get; init; } = default!;
        public string Email { get; init; } = default!;

        public string PasswordHash { get; init; } = default!; //  NO devolver

        public DateTime FechaRegistro { get; init; } = DateTime.UtcNow;

        public bool Activo { get; set; } = true;

        public int IntentosFallidos { get; set; } = 0;

        public bool BloqueadoPorFraude { get; set; } = false;
    }
}