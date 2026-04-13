using TaskManagerAPI.DTOs.Common;
using TaskManagerAPI.DTOs.Task;
using TaskManagerAPI.Repositories.Interfaces;

namespace TaskManagerAPI.Services.Interfaces;

public interface ITaskService
{
    Task<TaskResponseDto?>                      GetByIdAsync(int id);
    Task<TaskResponseDto>                       CreateAsync(CreateTaskDto dto);
    Task<TaskResponseDto?>                      UpdateAsync(int id, UpdateTaskDto dto);
    Task<bool>                                  DeleteAsync(int id);

    // ── LINQ Operations ────────────────────────────────────────
    Task<IEnumerable<TaskResponseDto>>          GetByStatusAsync(string status);
    Task<IEnumerable<TaskResponseDto>>          GetByPriorityAsync(string priority);
    Task<IEnumerable<TaskSummary>>              GetTaskSummariesAsync(int projectId);
    Task<IEnumerable<TaskGroupByStatus>>        GetGroupedByStatusAsync(int projectId);
    Task<TaskStats>                             GetProjectStatsAsync(int projectId);
    Task<PagedResult<TaskResponseDto>>          GetPagedAsync(
                                                    QueryParameters parameters,
                                                    int projectId);
}