using Microsoft.EntityFrameworkCore;
using TaskManagerAPI.Data;
using TaskManagerAPI.DTOs.Common;
using TaskManagerAPI.Models;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.Repositories.Implementations;

public class TaskRepository : Repository<TaskItem>, ITaskRepository
{
    public TaskRepository(AppDbContext context) : base(context)
    {
        
    }

    // ── WHERE — filter by status ───────────────────────────────────────────
    // SQL: SELECT * FROM TaskItems WHERE Status = @status AND IsDeleted = 0
    public async Task<IEnumerable<TaskItem>> GetByStatusAsync(string status)
        => await _dbSet
            .Where(t => t.Status == status && !t.IsDeleted)
            .ToListAsync();

    // ── WHERE — filter by priority ─────────────────────────────────────────
    public async Task<IEnumerable<TaskItem>> GetByPriorityAsync(string priority)
        => await _dbSet
            .Where(t => t.Priority == priority && !t.IsDeleted)
            .ToListAsync();

    // ── WHERE — filter by assignee ─────────────────────────────────────────
    public async Task<IEnumerable<TaskItem>> GetByAssigneeAsync(int assigneeId)
        => await _dbSet
            .Where(t => t.AssigneeId == assigneeId && !t.IsDeleted)
            .Include(t => t.Project)
            .OrderBy(t => t.Priority)
            .ToListAsync();

    // ── WHERE — filter by project ──────────────────────────────────────────
    public async Task<IEnumerable<TaskItem>> GetByProjectAsync(int projectId)
        => await _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .ToListAsync();

    // ── SELECT (Projection) ────────────────────────────────────────────────
    // Instead of loading the full entity, project to a smaller shape
    // SQL: SELECT t.Id, t.Title, t.Status, t.Priority, u.FullName
    //      FROM TaskItems t LEFT JOIN Users u ON u.Id = t.AssigneeId
    public async Task<IEnumerable<TaskSummary>> GetTaskSummariesAsync(
        int projectId)
        => await _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .Include(t => t.Assignee)
            .Select(t => new TaskSummary    // ← SELECT projection
            {
                Id           = t.Id,
                Title        = t.Title,
                Status       = t.Status,
                Priority     = t.Priority,
                AssigneeName = t.Assignee != null
                                   ? t.Assignee.FullName
                                   : "Unassigned"
            })
            .ToListAsync();

    // ── ORDER BY — sort by priority ────────────────────────────────────────
    // Priority order: High → Medium → Low
    // SQL: SELECT * FROM TaskItems WHERE ProjectId = @id
    //      ORDER BY CASE Priority WHEN 'High' THEN 1
    //                             WHEN 'Medium' THEN 2
    //                             ELSE 3 END
    public async Task<IEnumerable<TaskItem>> GetSortedByPriorityAsync(
        int projectId)
        => await _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .OrderBy(t =>                           // ← ORDER BY with custom sort
                t.Priority == "High"   ? 1 :
                t.Priority == "Medium" ? 2 : 3)
            .ThenBy(t => t.CreatedAt)               // ← secondary sort
            .ToListAsync();

    // ── GROUP BY — group tasks by status ──────────────────────────────────
    // SQL: SELECT Status, COUNT(*) FROM TaskItems
    //      WHERE ProjectId = @id GROUP BY Status
    public async Task<IEnumerable<TaskGroupByStatus>> GetGroupedByStatusAsync(
        int projectId)
        => await _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .GroupBy(t => t.Status)             // ← GROUP BY Status
            .Select(g => new TaskGroupByStatus
            {
                Status     = g.Key,             // ← the grouped value
                Count      = g.Count(),         // ← COUNT(*)
                TaskTitles = g.Select(t => t.Title).ToList()
            })
            .ToListAsync();

    // ── AGGREGATION — Sum, Count, Average ─────────────────────────────────
    // SQL: SELECT
    //        COUNT(*) as TotalTasks,
    //        SUM(CASE WHEN Status='Done' THEN 1 ELSE 0 END) as CompletedTasks,
    //        ...
    //      FROM TaskItems WHERE ProjectId = @id
    public async Task<TaskStats> GetProjectStatsAsync(int projectId)
    {
        var tasks = await _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted)
            .ToListAsync();

        var total      = tasks.Count;                               // COUNT
        var completed  = tasks.Count(t => t.Status == "Done");     // COUNT with filter
        var inProgress = tasks.Count(t => t.Status == "InProgress");
        var todo       = tasks.Count(t => t.Status == "Todo");

        return new TaskStats
        {
            TotalTasks      = total,
            CompletedTasks  = completed,
            InProgressTasks = inProgress,
            TodoTasks       = todo,
            // Average completion rate as percentage
            CompletionRate  = total == 0 ? 0 :
                              Math.Round((double)completed / total * 100, 2)
        };
    }

    // ── PAGINATION — Skip / Take ───────────────────────────────────────────
    // SQL: SELECT * FROM TaskItems WHERE ...
    //      ORDER BY CreatedAt DESC
    //      OFFSET 0 ROWS FETCH NEXT 10 ROWS ONLY
    public async Task<(IEnumerable<TaskItem> Items, int TotalCount)>
        GetPagedTasksAsync(QueryParameters parameters, int projectId)
    {
        IQueryable<TaskItem> query = _dbSet
            .Where(t => t.ProjectId == projectId && !t.IsDeleted);

        // ── WHERE — search term ────────────────────────────────
        if (!string.IsNullOrEmpty(parameters.SearchTerm))
            query = query.Where(t =>
                t.Title.Contains(parameters.SearchTerm) ||
                (t.Description != null &&
                 t.Description.Contains(parameters.SearchTerm)));

        // ── WHERE — filter by status ───────────────────────────
        if (!string.IsNullOrEmpty(parameters.Status))
            query = query.Where(t => t.Status == parameters.Status);

        // ── WHERE — filter by priority ─────────────────────────
        if (!string.IsNullOrEmpty(parameters.Priority))
            query = query.Where(t => t.Priority == parameters.Priority);

        // ── COUNT before pagination ────────────────────────────
        var totalCount = await query.CountAsync();

        // ── ORDER BY ───────────────────────────────────────────
        query = parameters.SortBy?.ToLower() switch
        {
            "title"    => parameters.SortDescending
                            ? query.OrderByDescending(t => t.Title)
                            : query.OrderBy(t => t.Title),
            "priority" => parameters.SortDescending
                            ? query.OrderByDescending(t => t.Priority)
                            : query.OrderBy(t => t.Priority),
            "status"   => parameters.SortDescending
                            ? query.OrderByDescending(t => t.Status)
                            : query.OrderBy(t => t.Status),
            _          => parameters.SortDescending              // default
                            ? query.OrderByDescending(t => t.CreatedAt)
                            : query.OrderBy(t => t.CreatedAt)
        };

        // ── SKIP / TAKE ────────────────────────────────────────
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}