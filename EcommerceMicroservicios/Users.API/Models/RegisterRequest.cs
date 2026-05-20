using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Users.API.Models;



/// <summary>
/// Estructura para el registro de un nuevo usuario.
/// </summary>
/// <param name="Email" example="juan.perez@email.com" >Correo electrónico institucional o personal.</param>
/// <param name="Password" example="ClaveSegura123!" >Contraseña segura en texto plano.</param>
/// <param name="Nombre" example="Juan" >Primer nombre del usuario.</param>
/// <param name="Apellido" example="Perez" >Apellido del usuario.</param>
public record RegisterRequest(string Email, string Password, string Nombre, string Apellido);



