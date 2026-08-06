using SigortaPro.Application.Common.Models;

namespace SigortaPro.Application.Common.Interfaces;

// JWT access token ve refresh token üretimini soyutlar. Implementasyonu Infrastructure katmanında
// (yalnızca primitif değerlerle çalıştığı için Persistence'a ihtiyaç duymaz).
public interface ITokenService
{
    TokenPair CreateTokenPair(Guid userId, string email, IEnumerable<string> roles);
}
