using TaskManagerAPI.DTOs.User;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.Services.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto?> GetByIdAsync(int id);
    Task<UserResponseDto> CreateAsync(CreateUserDto dto);
    Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto);
    Task<bool> DeleteAsync(int id); 

    // ── Loading Related Data ───────────────────────────────────
    Task<UserWithProfileDto?>  GetWithProfileAsync(int id);
    Task<UserWithTeamsDto?>    GetWithTeamsAsync(int id);
    Task<UserWithProfileDto?>  GetWithExplicitLoadAsync(int id);
    Task<UserWithProfileDto?>  GetWithLazyLoadAsync(int id);


    // ── Tracking Demo ──────────────────────────────────────────
    Task<EntityStateDemo> GetEntityStateDemoAsync(int id);
    Task<IEnumerable<UserResponseDto>> GetAllNoTrackingAsync();


    // ── Transactions ───────────────────────────────────────────
    Task<TransactionDemo> BulkCreateUsersAsync(List<CreateUserDto> dtos);
    Task<TransactionDemo> TransactionWithRollbackDemoAsync();
    Task<TransactionDemo> SavepointDemoAsync();
}