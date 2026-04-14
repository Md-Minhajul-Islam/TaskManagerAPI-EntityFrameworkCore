using Microsoft.AspNetCore.Mvc;
using TaskManagerAPI.DTOs.User;
using TaskManagerAPI.Services.Interfaces;

namespace TaskManagerAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    // GET: api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    // GET: api/users/5
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetByIdAsync(id);
        return user is null ? NotFound() : Ok(user);
    }

    // POST: api/users
    [HttpPost]
    public async Task<IActionResult> Create(CreateUserDto dto)
    {
        try
        {
            var user = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    // PUT: api/users/5
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateUserDto dto)
    {
        var user = await _userService.UpdateAsync(id, dto);
        return user is null ? NotFound() : Ok(user);
    }

    // DELETE: api/users/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteAsync(id);
        return result ? NoContent() : NotFound();
    }

    // GET: api/users/5/with-profile  — Eager Loading
    [HttpGet("{id}/with-profile")]
    public async Task<IActionResult> GetWithProfile(int id)
    {
        var result = await _userService.GetWithProfileAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // GET: api/users/5/with-teams  — Eager Loading + ThenInclude
    [HttpGet("{id}/with-teams")]
    public async Task<IActionResult> GetWithTeams(int id)
    {
        var result = await _userService.GetWithTeamsAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // GET: api/users/5/explicit-load  — Explicit Loading
    [HttpGet("{id}/explicit-load")]
    public async Task<IActionResult> GetWithExplicitLoad(int id)
    {
        var result = await _userService.GetWithExplicitLoadAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // GET: api/users/5/lazy-load  — Lazy Loading
    [HttpGet("{id}/lazy-load")]
    public async Task<IActionResult> GetWithLazyLoad(int id)
    {
        var result = await _userService.GetWithLazyLoadAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    // GET: api/users/no-tracking
    // Demonstrates: AsNoTracking — faster read-only query
    [HttpGet("no-tracking")]
    public async Task<IActionResult> GetAllNoTracking()
    {
        var users = await _userService.GetAllNoTrackingAsync();
        return Ok(users);
    }

    // GET: api/users/5/entity-state-demo
    // Demonstrates: EntityState lifecycle — Added, Modified, Deleted, Unchanged, Detached
    [HttpGet("{id}/entity-state-demo")]
    public async Task<IActionResult> EntityStateDemo(int id)
    {
        try
        {
            var demo = await _userService.GetEntityStateDemoAsync(id);
            return Ok(demo);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }


    // POST: api/users/bulk-create
    // Demonstrates: atomic transaction — all or nothing
    [HttpPost("bulk-create")]
    public async Task<IActionResult> BulkCreate(List<CreateUserDto> dtos)
    {
        var result = await _userService.BulkCreateUsersAsync(dtos);
        return result.Success ? Ok(result) : Conflict(result);
    }

    // POST: api/users/transaction-rollback-demo
    // Demonstrates: intentional rollback
    [HttpPost("transaction-rollback-demo")]
    public async Task<IActionResult> TransactionRollbackDemo()
    {
        var result = await _userService.TransactionWithRollbackDemoAsync();
        return Ok(result);
    }

    // POST: api/users/savepoint-demo
    // Demonstrates: savepoints — partial rollback
    [HttpPost("savepoint-demo")]
    public async Task<IActionResult> SavepointDemo()
    {
        var result = await _userService.SavepointDemoAsync();
        return Ok(result);
    }

    // GET: api/users/performance-demo
    // Demonstrates: AsNoTracking + indexes + compiled queries + split query
    [HttpGet("performance-demo")]
    public async Task<IActionResult> PerformanceDemo()
        => Ok(await _userService.GetPerformanceDemoAsync());


    // GET: api/users/raw-sql/by-role/Admin
    // Demonstrates: FromSqlRaw
    [HttpGet("raw-sql/by-role/{role}")]
    public async Task<IActionResult> GetByRoleRawSql(string role)
        => Ok(await _userService.GetByRoleRawSqlAsync(role));

    // GET: api/users/raw-sql/by-email?email=alice@app.com
    // Demonstrates: FromSqlRaw with parameter
    [HttpGet("raw-sql/by-email")]
    public async Task<IActionResult> GetByEmailRawSql([FromQuery] string email)
    {
        var user = await _userService.GetByEmailRawSqlAsync(email);
        return user is null ? NotFound() : Ok(user);
    }

    // PATCH: api/users/raw-sql/deactivate/1
    // Demonstrates: ExecuteSqlRaw
    [HttpPatch("raw-sql/deactivate/{id}")]
    public async Task<IActionResult> DeactivateRawSql(int id)
        => Ok(await _userService.DeactivateUserRawSqlAsync(id));

    // PATCH: api/users/raw-sql/bulk-deactivate/Member
    // Demonstrates: ExecuteSqlRaw bulk update
    [HttpPatch("raw-sql/bulk-deactivate/{role}")]
    public async Task<IActionResult> BulkDeactivateRawSql(string role)
        => Ok(await _userService.BulkDeactivateByRoleAsync(role));

    // GET: api/users/sp/active-by-role/Admin
    // Demonstrates: Stored Procedure via FromSqlRaw
    [HttpGet("sp/active-by-role/{role}")]
    public async Task<IActionResult> GetActiveByRoleSp(string role)
        => Ok(await _userService.GetActiveUsersByRoleSpAsync(role));

    // PATCH: api/users/sp/update-role/1?newRole=Admin
    // Demonstrates: Stored Procedure via ExecuteSqlRaw
    [HttpPatch("sp/update-role/{id}")]
    public async Task<IActionResult> UpdateRoleSp(
        int id, [FromQuery] string newRole)
        => Ok(await _userService.UpdateUserRoleSpAsync(id, newRole));


}