using TaskManagerAPI.DTOs.Common;
using TaskManagerAPI.DTOs.Task;
using TaskManagerAPI.Models;
using TaskManagerAPI.Repositories.Interfaces;
using TaskManagerAPI.Services.Interfaces;
using TaskManagerAPI.UnitOfWork;

namespace TaskManagerAPI.Services.Implementations;

public class TaskService : ITaskService
{
    private readonly IUnitOfWork _unitOfWork;

    public TaskService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TaskResponseDto?> GetByIdAsync(int id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        return task is null ? null : MapToResponse(task);
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title       = dto.Title,
            Description = dto.Description,
            Priority    = dto.Priority,
            ProjectId   = dto.ProjectId,
            ReporterId  = dto.ReporterId,
            AssigneeId  = dto.AssigneeId,
            SprintId    = dto.SprintId,
            Status      = "Todo"
        };

        await _unitOfWork.Tasks.AddAsync(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(task);
    }

    public async Task<TaskResponseDto?> UpdateAsync(int id, UpdateTaskDto dto)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return null;

        task.Title       = dto.Title;
        task.Description = dto.Description;
        task.Status      = dto.Status;
        task.Priority    = dto.Priority;
        task.AssigneeId  = dto.AssigneeId;
        task.SprintId    = dto.SprintId;

        _unitOfWork.Tasks.Update(task);
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(task);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var task = await _unitOfWork.Tasks.GetByIdAsync(id);
        if (task is null) return false;

        _unitOfWork.Tasks.Remove(task);
        await _unitOfWork.SaveChangesAsync();
        return true;
    }

    // ── LINQ: Where ────────────────────────────────────────────
    public async Task<IEnumerable<TaskResponseDto>> GetByStatusAsync(string status)
    {
        var tasks = await _unitOfWork.Tasks.GetByStatusAsync(status);
        return tasks.Select(MapToResponse);
    }

    public async Task<IEnumerable<TaskResponseDto>> GetByPriorityAsync(string priority)
    {
        var tasks = await _unitOfWork.Tasks.GetByPriorityAsync(priority);
        return tasks.Select(MapToResponse);
    }

    // ── LINQ: Select (Projection) ──────────────────────────────
    public async Task<IEnumerable<TaskSummary>> GetTaskSummariesAsync(int projectId)
        => await _unitOfWork.Tasks.GetTaskSummariesAsync(projectId);

    // ── LINQ: GroupBy ──────────────────────────────────────────
    public async Task<IEnumerable<TaskGroupByStatus>> GetGroupedByStatusAsync(
        int projectId)
        => await _unitOfWork.Tasks.GetGroupedByStatusAsync(projectId);

    // ── LINQ: Aggregation ──────────────────────────────────────
    public async Task<TaskStats> GetProjectStatsAsync(int projectId)
        => await _unitOfWork.Tasks.GetProjectStatsAsync(projectId);

    // ── LINQ: Pagination ───────────────────────────────────────
    public async Task<PagedResult<TaskResponseDto>> GetPagedAsync(
        QueryParameters parameters,
        int projectId)
    {
        var (items, totalCount) = await _unitOfWork.Tasks
            .GetPagedTasksAsync(parameters, projectId);

        return new PagedResult<TaskResponseDto>
        {
            Items      = items.Select(MapToResponse),
            TotalCount = totalCount,
            PageNumber = parameters.PageNumber,
            PageSize   = parameters.PageSize
        };
    }

    private static TaskResponseDto MapToResponse(TaskItem task) => new()
    {
        Id          = task.Id,
        Title       = task.Title,
        Description = task.Description,
        Status      = task.Status,
        Priority    = task.Priority,
        ProjectId   = task.ProjectId,
        ReporterId  = task.ReporterId,
        AssigneeId  = task.AssigneeId,
        SprintId    = task.SprintId,
        CreatedAt   = task.CreatedAt
    };
}