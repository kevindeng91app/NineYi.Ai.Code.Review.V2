using NineYi.Ai.CodeReview.Domain.Entities;

namespace NineYi.Ai.CodeReview.Domain.Interfaces;

public interface IPlatformSettingsRepository
{
    Task<PlatformSettings?> GetByPlatformAsync(GitPlatformType platform, CancellationToken cancellationToken = default);
    Task<IEnumerable<PlatformSettings>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PlatformSettings> AddAsync(PlatformSettings settings, CancellationToken cancellationToken = default);
    Task<PlatformSettings> UpdateAsync(PlatformSettings settings, CancellationToken cancellationToken = default);
    Task<PlatformSettings> UpsertAsync(PlatformSettings settings, CancellationToken cancellationToken = default);
}
