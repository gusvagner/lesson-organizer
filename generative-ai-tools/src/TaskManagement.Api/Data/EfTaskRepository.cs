using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Tasks;

namespace TaskManagement.Api.Data;

public sealed class EfTaskRepository(TaskManagementDbContext dbContext) : ITaskRepository
{
    public Task<TaskItem?> FindAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
        OwnedBy(ownerId).SingleOrDefaultAsync(task => task.Id == taskId, cancellationToken);

    public async Task<TaskPage> ListAsync(
        Guid ownerId, TaskItemStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        var matching = OwnedBy(ownerId).AsNoTracking();
        if (status is { } wanted)
        {
            matching = matching.Where(task => task.Status == wanted);
        }

        var totalCount = await matching.CountAsync(cancellationToken);
        var items = await InListOrder(matching).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return new TaskPage(items, totalCount);
    }

    public async Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(Guid ownerId, TaskItem task, CancellationToken cancellationToken)
    {
        if (task.UserId != ownerId)
        {
            throw new InvalidOperationException(
                $"Task {task.Id} belongs to user {task.UserId}, but its update was requested for user {ownerId}; expected the same user.");
        }

        dbContext.Tasks.Update(task);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken) =>
        await OwnedBy(ownerId).Where(task => task.Id == taskId).ExecuteDeleteAsync(cancellationToken) > 0;

    private IQueryable<TaskItem> OwnedBy(Guid ownerId) => dbContext.Tasks.Where(task => task.UserId == ownerId);

    private static IOrderedQueryable<TaskItem> InListOrder(IQueryable<TaskItem> tasks) =>
        tasks.OrderBy(task => task.DueDate == null)
            .ThenBy(task => task.DueDate)
            .ThenBy(task => task.CreatedAt)
            .ThenBy(task => task.Id);
}
