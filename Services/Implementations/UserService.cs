using TaskManagerAPI.DTOs.User;
using TaskManagerAPI.Models;
using TaskManagerAPI.Services.Interfaces;
using TaskManagerAPI.UnitOfWork;

namespace TaskManagerAPI.Services.Implementations;

// Service layer contains all business logic
// It uses UnitOfWork to access repositories
// and calls SaveChangesAsync once per operation
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;

    public UserService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var users = await _unitOfWork.Users.GetAllAsync();
        return users.Select(MapToResponse);
    }

    public async Task<UserResponseDto?> GetByIdAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        return user is null ? null : MapToResponse(user);
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserDto dto)
    {
        var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);
        if(!isUnique)
            throw new InvalidOperationException($"Email '{dto.Email}' is already taken");

        var user = new User
        {
            FullName = dto.FullName,
            Email = dto.Email,
            Role = dto.Role
        };

        await _unitOfWork.Users.AddAsync(user);

        // SaveChangesAsync — EF Core generates INSERT SQL
        // EntityState goes: Detached → Added → Unchanged (after save)
        await _unitOfWork.SaveChangesAsync();
        return MapToResponse(user);

    }


    public async Task<UserResponseDto?> UpdateAsync(int id, UpdateUserDto dto)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return null;

        // Change Tracking detects these modifications automatically
        // EntityState goes: Unchanged → Modified (after property change)
        user.FullName = dto.FullName;
        user.Role     = dto.Role;
        user.IsActive = dto.IsActive;

        _unitOfWork.Users.Update(user);

        // SaveChangesAsync — EF Core generates UPDATE SQL
        await _unitOfWork.SaveChangesAsync();

        return MapToResponse(user);
    }


     public async Task<bool> DeleteAsync(int id)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(id);
        if (user is null) return false;

        // EntityState goes: Unchanged → Deleted
        _unitOfWork.Users.Remove(user);

        // SaveChangesAsync — EF Core generates DELETE SQL
        await _unitOfWork.SaveChangesAsync();

        return true;
    }


    // Private mapper - AutoMapper replaces this in Step 14
    private static UserResponseDto MapToResponse(User user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        Role = user.Role,
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt
    };
}