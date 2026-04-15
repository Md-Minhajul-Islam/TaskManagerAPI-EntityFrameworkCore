using AutoMapper;
using TaskManagerAPI.DTOs.User;
using TaskManagerAPI.Models;

namespace TaskManagerAPI.MappingProfiles;

// MappingProfile defines HOW AutoMapper converts
// between source and destination types
// AutoMapper scans for all profiles in the assembly
// and registers them automatically
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // ── User → UserResponseDto ─────────────────────────────────────────
        // Simple mapping — property names match exactly
        // AutoMapper maps them automatically by convention
        CreateMap<User, UserResponseDto>();
        
        // ── CreateUserDto → User ───────────────────────────────────────────
        // Input DTO → Entity
        // Only maps fields present in CreateUserDto
        // Id, CreatedAt, UpdatedAt are NOT in CreateUserDto — ignored
        CreateMap<CreateUserDto, User>();

        // ── UpdateUserDto → User ───────────────────────────────────────────
        // Used when updating an existing entity
        // ForMember — customize how a specific property is mapped
        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());
        // Ignore Id, Email, CreatedAt — these should never change on update
    
        // ── User → UserWithProfileDto ──────────────────────────────────────
        CreateMap<User, UserWithProfileDto>()
            .ForMember(
                dest => dest.Profile,
                opt  => opt.MapFrom(src => src.Profile));
    }
}

