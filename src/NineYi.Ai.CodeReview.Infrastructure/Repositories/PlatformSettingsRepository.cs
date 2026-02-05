using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class PlatformSettingsRepository : IPlatformSettingsRepository
{
    private readonly CodeReviewDbContext _context;

    public PlatformSettingsRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<PlatformSettings?> GetByPlatformAsync(GitPlatformType platform, CancellationToken cancellationToken = default)
    {
        return await _context.PlatformSettings
            .FirstOrDefaultAsync(s => s.Platform == platform, cancellationToken);
    }

    public async Task<IEnumerable<PlatformSettings>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PlatformSettings
            .OrderBy(s => s.Platform)
            .ToListAsync(cancellationToken);
    }

    public async Task<PlatformSettings> AddAsync(PlatformSettings settings, CancellationToken cancellationToken = default)
    {
        settings.Id = Guid.NewGuid();
        settings.CreatedAt = DateTime.UtcNow;
        await _context.PlatformSettings.AddAsync(settings, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task<PlatformSettings> UpdateAsync(PlatformSettings settings, CancellationToken cancellationToken = default)
    {
        settings.UpdatedAt = DateTime.UtcNow;
        _context.PlatformSettings.Update(settings);
        await _context.SaveChangesAsync(cancellationToken);
        return settings;
    }

    public async Task<PlatformSettings> UpsertAsync(PlatformSettings settings, CancellationToken cancellationToken = default)
    {
        var existing = await GetByPlatformAsync(settings.Platform, cancellationToken);
        if (existing == null)
        {
            return await AddAsync(settings, cancellationToken);
        }
        else
        {
            existing.AccessToken = settings.AccessToken;
            existing.WebhookSecret = settings.WebhookSecret;
            existing.ApiBaseUrl = settings.ApiBaseUrl;
            return await UpdateAsync(existing, cancellationToken);
        }
    }
}
