using System;
using System.Threading.Tasks;

namespace Users.API.Repositories
{
    public static class UserRepositoryExtensions
    {
        public static Task ResetAttemptsAsync(this UserRepository repo, Guid id)
        {
            if (repo is null) throw new ArgumentNullException(nameof(repo));
            // Resetea los intentos a 0 y marca la cuenta como activa
            return repo.UpdateLoginAttemptsAsync(id.ToString(), 0, true);
        }
    }
}