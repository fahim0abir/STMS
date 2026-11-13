using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Entities;

namespace SmartTask.Infrastructure.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenAsync(string token);
    Task<RefreshToken?> GetByJwtIdAsync(string jwtId);
}
