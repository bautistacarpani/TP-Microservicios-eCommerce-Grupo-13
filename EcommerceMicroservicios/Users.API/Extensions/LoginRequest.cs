using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Users.API.Extensions;

public record LoginRequest(
    [Required][EmailAddress][DefaultValue("bautista.carpani@example.com")] string Email,
    [Required][DefaultValue("ClaveSegura123!")] string Password
);

