using TaskManagerAPI.DTOs.Common;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Repositories.Interfaces;

public interface ITaskRepository : IRepository<TaskItem>
{
    // ── Filtering (Where) ──────────────────────────────────────
    Task<IEnumerable<TaskItem>> GetByStatusAsync(string status);
    Task<IEnumerable<TaskItem>> GetByPriorityAsync(string priority);
    Task<IEnumerable<TaskItem>> GetByAssigneeAsync(int assigneeId);
    Task<IEnumerable<TaskItem>> GetByProjectAsync(int projectId);

    // ── Projection (Select) ────────────────────────────────────
    Task<IEnumerable<TaskSummary>> GetTaskSummariesAsync(int projectId);

    // ── Sorting (OrderBy) ──────────────────────────────────────
    Task<IEnumerable<TaskItem>> GetSortedByPriorityAsync(int projectId);

    // ── Grouping (GroupBy) ─────────────────────────────────────
    Task<IEnumerable<TaskGroupByStatus>> GetGroupedByStatusAsync(int projectId);

    // ── Aggregation (Sum, Count, Avg) ──────────────────────────
    Task<TaskStats> GetProjectStatsAsync(int projectId);

    // ── Pagination (Skip / Take) ───────────────────────────────
    Task<(IEnumerable<TaskItem> Items, int TotalCount)> GetPagedTasksAsync(
        QueryParameters parameters,
        int projectId);
}

// ── Projection DTOs (used only in repository layer) ────────────────────────
public class TaskSummary
{
    public int    Id       { get; set; }
    public string Title    { get; set; } = string.Empty;
    public string Status   { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? AssigneeName { get; set; }
}

public class TaskGroupByStatus
{
    public string Status { get; set; } = string.Empty;
    public int    Count  { get; set; }
    public IEnumerable<string> TaskTitles { get; set; } = new List<string>();
}

public class TaskStats
{
    public int     TotalTasks       { get; set; }
    public int     CompletedTasks   { get; set; }
    public int     InProgressTasks  { get; set; }
    public int     TodoTasks        { get; set; }
    public double  CompletionRate   { get; set; }  // percentage
}