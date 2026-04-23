using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace Users.API.Helpers;

public static class PasswordHelper
{
    public static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create(); // crea algoritmo hash

        var bytes = Encoding.UTF8.GetBytes(password); //convierte texto en bytes

        var hash = sha256.ComputeHash(bytes);// genera el hash

        return Convert.ToBase64String(hash); //lo convierte en string 
    }
}