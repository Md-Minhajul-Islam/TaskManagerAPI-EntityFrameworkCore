using Microsoft.AspNetCore.Mvc;
using TaskManagerAPI.DTOs.Common;
using TaskManagerAPI.DTOs.Task;
using TaskManagerAPI.Services.Interfaces;

namespace TaskManagerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;

    public TasksController(ITaskService taskService)
    {
        _taskService = taskService;
    }

    // GET: api/tasks/1
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var task = await _taskService.GetByIdAsync(id);
        return task is null ? NotFound() : Ok(task);
    }

    // POST: api/tasks
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskDto dto)
    {
        var task = await _taskService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    // PUT: api/tasks/1
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateTaskDto dto)
    {
        var task = await _taskService.UpdateAsync(id, dto);
        return task is null ? NotFound() : Ok(task);
    }

    // DELETE: api/tasks/1
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _taskService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // GET: api/tasks/by-status/Todo
    // Demonstrates: WHERE filtering
    [HttpGet("by-status/{status}")]
    public async Task<IActionResult> GetByStatus(string status)
    {
        var tasks = await _taskService.GetByStatusAsync(status);
        return Ok(tasks);
    }

    // GET: api/tasks/by-priority/High
    // Demonstrates: WHERE filtering
    [HttpGet("by-priority/{priority}")]
    public async Task<IActionResult> GetByPriority(string priority)
    {
        var tasks = await _taskService.GetByPriorityAsync(priority);
        return Ok(tasks);
    }

    // GET: api/tasks/project/1/summaries
    // Demonstrates: SELECT projection
    [HttpGet("project/{projectId}/summaries")]
    public async Task<IActionResult> GetSummaries(int projectId)
    {
        var summaries = await _taskService.GetTaskSummariesAsync(projectId);
        return Ok(summaries);
    }

    // GET: api/tasks/project/1/grouped-by-status
    // Demonstrates: GROUP BY
    [HttpGet("project/{projectId}/grouped-by-status")]
    public async Task<IActionResult> GetGroupedByStatus(int projectId)
    {
        var grouped = await _taskService.GetGroupedByStatusAsync(projectId);
        return Ok(grouped);
    }

    // GET: api/tasks/project/1/stats
    // Demonstrates: COUNT, SUM, AVG aggregations
    [HttpGet("project/{projectId}/stats")]
    public async Task<IActionResult> GetStats(int projectId)
    {
        var stats = await _taskService.GetProjectStatsAsync(projectId);
        return Ok(stats);
    }

    // GET: api/tasks/project/1/paged?pageNumber=1&pageSize=10&status=Todo
    // Demonstrates: Skip / Take pagination
    [HttpGet("project/{projectId}/paged")]
    public async Task<IActionResult> GetPaged(
        int projectId,
        [FromQuery] QueryParameters parameters)
    {
        var result = await _taskService.GetPagedAsync(parameters, projectId);
        return Ok(result);
    }
}