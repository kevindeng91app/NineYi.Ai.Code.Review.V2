using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class RepositoryRepository : IRepositoryRepository
{
    private readonly CodeReviewDbContext _context;

    public RepositoryRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<Repository?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.RuleMappings)
            .ThenInclude(rm => rm.Rule)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<Repository?> GetByPlatformIdAsync(GitPlatformType platform, string platformRepositoryId, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.RuleMappings)
            .ThenInclude(rm => rm.Rule)
            .FirstOrDefaultAsync(r => r.Platform == platform && r.PlatformRepositoryId == platformRepositoryId, cancellationToken);
    }

    public async Task<Repository?> GetByFullNameAsync(GitPlatformType platform, string fullName, CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Include(r => r.RuleMappings)
            .ThenInclude(rm => rm.Rule)
            .FirstOrDefaultAsync(r => r.Platform == platform && r.FullName == fullName, cancellationToken);
    }

    public async Task<IEnumerable<Repository>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Repositories
            .Where(r => r.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<Repository> AddAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        _context.Repositories.Add(repository);
        await _context.SaveChangesAsync(cancellationToken);
        return repository;
    }

    public async Task UpdateAsync(Repository repository, CancellationToken cancellationToken = default)
    {
        repository.UpdatedAt = DateTime.UtcNow;
        _context.Repositories.Update(repository);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var repository = await _context.Repositories.FindAsync(new object[] { id }, cancellationToken);
        if (repository != null)
        {
            _context.Repositories.Remove(repository);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
