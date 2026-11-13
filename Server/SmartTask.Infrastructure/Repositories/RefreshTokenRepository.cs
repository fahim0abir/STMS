using Microsoft.EntityFrameworkCore;
using SmartTask.Domain.Entities;
using SmartTask.Infrastructure.Persistence;

namespace SmartTask.Infrastructure.Repositories;

public class RefreshTokenRepository : Repository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context)
        : base(context)
    {
    }

    public async Task<RefreshToken?> GetByTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == token);
    }

    public async Task<RefreshToken?> GetByJwtIdAsync(string jwtId)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(r => r.JwtId == jwtId);
    }
}
