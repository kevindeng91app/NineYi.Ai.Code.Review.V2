using Microsoft.EntityFrameworkCore;
using NineYi.Ai.CodeReview.Domain.Entities;
using NineYi.Ai.CodeReview.Domain.Interfaces;
using NineYi.Ai.CodeReview.Infrastructure.Data;

namespace NineYi.Ai.CodeReview.Infrastructure.Repositories;

public class ReviewLogRepository : IReviewLogRepository
{
    private readonly CodeReviewDbContext _context;

    public ReviewLogRepository(CodeReviewDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewLogs
            .Include(r => r.FileLogs)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<ReviewLog?> GetByPullRequestAsync(Guid repositoryId, int pullRequestNumber, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewLogs
            .Include(r => r.FileLogs)
            .Where(r => r.RepositoryId == repositoryId && r.PullRequestNumber == pullRequestNumber)
            .OrderByDescending(r => r.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IEnumerable<ReviewLog>> GetByRepositoryIdAsync(Guid repositoryId, int pageSize = 20, int page = 1, CancellationToken cancellationToken = default)
    {
        return await _context.ReviewLogs
            .Where(r => r.RepositoryId == repositoryId)
            .OrderByDescending(r => r.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewLog> AddAsync(ReviewLog reviewLog, CancellationToken cancellationToken = default)
    {
        _context.ReviewLogs.Add(reviewLog);
        await _context.SaveChangesAsync(cancellationToken);
        return reviewLog;
    }

    public async Task UpdateAsync(ReviewLog reviewLog, CancellationToken cancellationToken = default)
    {
        _context.ReviewLogs.Update(reviewLog);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddFileLogAsync(ReviewFileLog fileLog, CancellationToken cancellationToken = default)
    {
        _context.ReviewFileLogs.Add(fileLog);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
