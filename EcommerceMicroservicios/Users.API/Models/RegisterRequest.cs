using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;

public record RegisterRequest(
    [Required][EmailAddress][DefaultValue("maili@example.com")] string Email,
    [Required][MinLength(6)][DefaultValue("ClaveSegura123!")] string Password,
    [Required][DefaultValue("Maria")] string Nombre,
    [Required][DefaultValue("Vazquez")] string Apellido
);



