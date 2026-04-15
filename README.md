# TaskManagerAPI — Step 14: Architecture & Best Practices

## 📌 What This Step Covers
- AutoMapper — replace manual mapping with profiles
- MappingProfile — define mappings between entities and DTOs
- IMapper — inject and use AutoMapper in services
- ForMember — customize specific property mappings
- DI Extensions — final cleanup and organization
- Full architecture review

---

## 🧠 What is AutoMapper?

AutoMapper automatically copies properties from one object to another — removing the need for manual mapping code.

```csharp
// WITHOUT AutoMapper — manual mapping (what we had before)
private static UserResponseDto MapToResponse(User user) => new()
{
    Id        = user.Id,
    FullName  = user.FullName,
    Email     = user.Email,
    Role      = user.Role,
    IsActive  = user.IsActive,
    CreatedAt = user.CreatedAt
};
// ❌ Repeated for every entity
// ❌ Must update whenever you add a property
// ❌ Easy to forget a field

// WITH AutoMapper — one line
var dto = _mapper.Map<UserResponseDto>(user);
// ✅ AutoMapper copies matching properties automatically
// ✅ Add a property to both classes — mapped automatically
// ✅ Configured once in a Profile class
```

---

## 1️⃣ MappingProfile

A `Profile` class defines **how** AutoMapper maps between types.
Created once — used everywhere in the application.

```csharp
public class UserMappingProfile : Profile
{
    public UserMappingProfile()
    {
        // Simple mapping — property names match, AutoMapper handles it
        CreateMap<User, UserResponseDto>();

        // Source → Destination
        CreateMap<CreateUserDto, User>();

        // With customization — ignore specific properties
        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.Id,        opt => opt.Ignore())
            .ForMember(dest => dest.Email,     opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());

        // Nested mapping — maps UserProfile inside User
        CreateMap<User, UserWithProfileDto>()
            .ForMember(dest => dest.Profile,
                       opt  => opt.MapFrom(src => src.Profile));

        CreateMap<UserProfile, ProfileDto>();
    }
}
```

### How AutoMapper matches properties
```
Source: User                    Destination: UserResponseDto
──────────────────────────────────────────────────────────
Id          int           ────► Id          int          ✅ same name + type
FullName    string        ────► FullName    string       ✅ same name + type
Email       string        ────► Email       string       ✅ same name + type
Role        string        ────► Role        string       ✅ same name + type
IsActive    bool          ────► IsActive    bool         ✅ same name + type
CreatedAt   DateTime      ────► CreatedAt   DateTime     ✅ same name + type
IsDeleted   bool          ────► (not in DTO)             ✅ skipped automatically
RowVersion  byte[]        ────► (not in DTO)             ✅ skipped automatically
```

> 💡 AutoMapper maps by **convention** — matching property names are mapped automatically.
> Extra properties on the source that don't exist on the destination are silently ignored.

---

## 2️⃣ ForMember — Customizing Mappings

`ForMember` lets you customize how a specific property is mapped.

```csharp
// Ignore a property — don't map it at all
CreateMap<UpdateUserDto, User>()
    .ForMember(dest => dest.Id,    opt => opt.Ignore())
    .ForMember(dest => dest.Email, opt => opt.Ignore());
// Reason: Id and Email should never be changed on update

// Map from a different source property
CreateMap<User, UserWithProfileDto>()
    .ForMember(
        dest => dest.Profile,
        opt  => opt.MapFrom(src => src.Profile));
// Explicitly tells AutoMapper where Profile comes from

// Compute a value
CreateMap<User, UserResponseDto>()
    .ForMember(
        dest => dest.FullName,
        opt  => opt.MapFrom(src => src.FullName.Trim()));
// Transform the value during mapping

// Use a constant value
CreateMap<CreateUserDto, User>()
    .ForMember(
        dest => dest.IsActive,
        opt  => opt.MapFrom(_ => true));
// Always set IsActive = true on creation
```

---

## 3️⃣ IMapper — Using AutoMapper in Services

`IMapper` is injected via DI and provides the `Map<T>()` methods.

```csharp
public class UserService : IUserService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper     _mapper;       // ← injected

    public UserService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper     = mapper;
    }
}
```

### Three mapping patterns

#### Pattern 1 — Source → New Destination
```csharp
// Map a single object
var dto = _mapper.Map<UserResponseDto>(user);

// Map a collection
var dtos = _mapper.Map<IEnumerable<UserResponseDto>>(users);
```

#### Pattern 2 — Source → Existing Destination (update)
```csharp
// Map DTO properties onto an EXISTING entity
// Only properties in the DTO are updated — others unchanged
_mapper.Map(dto, user);
// Before: user.FullName = "Alice"
// dto.FullName = "Bob"
// After:  user.FullName = "Bob"  ← updated by AutoMapper
// user.Email unchanged            ← ForMember Ignore() protected it
```

#### Pattern 3 — Source → New Entity for Create
```csharp
// Create a new User entity from CreateUserDto
var user = _mapper.Map<User>(dto);
// AutoMapper creates a new User and copies all matching properties
await _unitOfWork.Users.AddAsync(user);
```

---

## 4️⃣ Registration

### In `ServiceCollectionExtensions.cs`
```csharp
public static IServiceCollection AddAutoMapperProfiles(
    this IServiceCollection services)
{
    // Scans the assembly for all Profile classes automatically
    // Finds UserMappingProfile, registers all CreateMap definitions
    services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
    return services;
}
```

### In `Program.cs`
```csharp
builder.Services.AddAutoMapperProfiles();   // ← one clean line
```

### DI Lifetime
AutoMapper is registered as **Singleton** by default — one instance for the app lifetime.
`IMapper` is thread-safe and stateless — Singleton is appropriate.

---

## 5️⃣ Before vs After AutoMapper

### Before — Manual Mapping
```csharp
// Repeated in every service method
private static UserResponseDto MapToResponse(User user) => new()
{
    Id        = user.Id,
    FullName  = user.FullName,
    Email     = user.Email,
    Role      = user.Role,
    IsActive  = user.IsActive,
    CreatedAt = user.CreatedAt
};

// Usage:
var users = await _unitOfWork.Users.GetAllAsync();
return users.Select(MapToResponse);          // ← called everywhere
```

### After — AutoMapper
```csharp
// Defined ONCE in UserMappingProfile.cs
CreateMap<User, UserResponseDto>();

// Usage — everywhere in the service:
var users = await _unitOfWork.Users.GetAllAsync();
return _mapper.Map<IEnumerable<UserResponseDto>>(users);  // ← one line
```

---

## 6️⃣ Complete Architecture Review

### Layer Responsibilities

```
┌─────────────────────────────────────────────────────────────┐
│                        Controller                           │
│  - Receives HTTP request                                    │
│  - Validates input (model binding)                          │
│  - Calls Service                                            │
│  - Returns HTTP response                                    │
│  - NO business logic, NO data access, NO mapping           │
└──────────────────────────┬──────────────────────────────────┘
                           │ calls
┌──────────────────────────▼──────────────────────────────────┐
│                         Service                             │
│  - Contains ALL business logic                              │
│  - Validates business rules                                 │
│  - Calls UnitOfWork / Repositories                          │
│  - Uses AutoMapper to map entities ↔ DTOs                   │
│  - NO HTTP concerns, NO direct DB queries                   │
└──────────────────────────┬──────────────────────────────────┘
                           │ calls
┌──────────────────────────▼──────────────────────────────────┐
│                       UnitOfWork                            │
│  - Provides access to all repositories                      │
│  - Single SaveChangesAsync for all repos                    │
│  - Transaction management                                   │
│  - NO business logic, NO mapping                           │
└──────────────────────────┬──────────────────────────────────┘
                           │ accesses
┌──────────────────────────▼──────────────────────────────────┐
│                       Repository                            │
│  - Data access ONLY                                         │
│  - Builds and executes queries                              │
│  - Generic base + entity-specific methods                   │
│  - NO business logic, NO mapping, NO HTTP                   │
└──────────────────────────┬──────────────────────────────────┘
                           │ uses
┌──────────────────────────▼──────────────────────────────────┐
│                      AppDbContext                           │
│  - EF Core bridge to SQL Server                             │
│  - Change tracking                                          │
│  - Global Query Filters                                     │
│  - SaveChangesAsync                                         │
└──────────────────────────┬──────────────────────────────────┘
                           │
                    SQL Server DB
```

### File Organization

```
TaskManagerAPI/
│
├── Controllers/           HTTP layer — routes only
├── Services/              Business logic
│   ├── Interfaces/        Contracts
│   └── Implementations/   Logic
├── Repositories/          Data access
│   ├── Interfaces/        Contracts
│   └── Implementations/   Queries
├── UnitOfWork/            Transaction coordination
├── Data/                  EF Core layer
│   ├── AppDbContext.cs
│   ├── Configurations/    IEntityTypeConfiguration per entity
│   ├── Interceptors/      SaveChanges hooks
│   └── Migrations/        DB schema history
├── Models/                Domain entities
├── DTOs/                  Data transfer objects
│   ├── User/
│   └── Common/
├── MappingProfiles/       AutoMapper profiles    ← NEW
└── Extensions/            DI registration helpers
```

---

## 7️⃣ DI Registration Summary

Everything is cleanly organized in `ServiceCollectionExtensions.cs`:

```csharp
// Program.cs — clean and minimal
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDatabase(builder.Configuration);    // DbContext + Interceptors
builder.Services.AddUnitOfWork();                        // IUnitOfWork
builder.Services.AddApplicationServices();               // IUserService etc.
builder.Services.AddAutoMapperProfiles();                // AutoMapper
```

| Method | Registers | Lifetime |
|--------|-----------|---------|
| `AddDatabase` | `AppDbContext`, `AuditInterceptor` | Scoped, Singleton |
| `AddUnitOfWork` | `IUnitOfWork` | Scoped |
| `AddApplicationServices` | `IUserService` | Scoped |
| `AddAutoMapperProfiles` | `IMapper`, `MapperConfiguration` | Singleton |

---

## 8️⃣ Complete Request Flow — End to End

```
POST /api/users
{ "fullName": "Alice", "email": "alice@app.com", "role": "Admin" }

1. UsersController.Create(CreateUserDto dto)
   └─ validates model binding

2. UserService.CreateAsync(dto)
   ├─ _unitOfWork.Users.IsEmailUniqueAsync("alice@app.com") → true
   ├─ _mapper.Map<User>(dto)
   │     AutoMapper: CreateUserDto → User
   │     { FullName="Alice", Email="alice@app.com", Role="Admin" }
   ├─ _unitOfWork.Users.AddAsync(user)
   │     Repository: _dbSet.AddAsync(user)
   │     EntityState → Added
   ├─ _unitOfWork.SaveChangesAsync()
   │     UnitOfWork → AppDbContext.SaveChangesAsync()
   │     AuditInterceptor fires → sets CreatedBy = "System"
   │     EF Core: INSERT INTO Users (...) VALUES (...)
   │     EntityState → Unchanged
   └─ _mapper.Map<UserResponseDto>(user)
         AutoMapper: User → UserResponseDto
         { Id=3, FullName="Alice", Email="alice@app.com", ... }

3. Controller returns 201 Created
   { "id": 3, "fullName": "Alice", "email": "alice@app.com", ... }
```

---

## 9️⃣ All EF Core Topics — Covered ✅

| Topic | Step | Where |
|-------|------|-------|
| ORM, DbContext, DbSet | Step 01 | `AppDbContext`, `Repository<T>` |
| Connection String | Step 01 | `appsettings.json`, `ServiceCollectionExtensions` |
| Change Tracking | Step 01, 08 | `AppDbContext.SaveChangesAsync`, `EntityStateDemo` |
| SaveChangesAsync | Step 01 | `UnitOfWork.SaveChangesAsync` |
| Migrations | Step 02 | `Data/Migrations/` |
| Data Annotations | Step 03 | `Models/User.cs` |
| Fluent API | Step 03 | `Data/Configurations/` |
| IEntityTypeConfiguration | Step 03 | All `*Configuration.cs` files |
| Primary Key | Step 04 | `UserConfiguration.HasKey()` |
| Composite Key | Step 04 | `TeamMemberConfiguration` |
| Alternate Key | Step 04 | `UserConfiguration.HasAlternateKey()` |
| Foreign Key | Step 04 | `UserProfileConfiguration` |
| One-to-One | Step 05 | `User ↔ UserProfile` |
| One-to-Many | Step 05 | `Team → Projects`, `Project → Tasks` |
| Many-to-Many | Step 05 | `User ↔ Team`, `Task ↔ Label` |
| Cascade Delete | Step 05 | `DeleteBehavior.*` in all configs |
| Eager Loading | Step 06 | `Include()`, `ThenInclude()` |
| Lazy Loading | Step 06 | `UseLazyLoadingProxies()`, `virtual` |
| Explicit Loading | Step 06 | `Entry().Reference/Collection().LoadAsync()` |
| Where / Filter | Step 07 | `TaskRepository.GetByStatusAsync()` |
| Select / Projection | Step 07 | `GetTaskSummariesAsync()` |
| OrderBy / Sort | Step 07 | `GetSortedAsync()` |
| GroupBy | Step 07 | `GetGroupedByRoleAsync()` |
| Aggregation | Step 07 | `GetUserStatsAsync()` |
| Pagination | Step 07 | `GetPagedAsync()`, `Skip/Take` |
| AsNoTracking | Step 08 | `Repository.GetAllAsNoTrackingAsync()` |
| EntityState | Step 08 | `GetEntityStateDemoAsync()` |
| Indexes | Step 09 | `UserConfiguration.HasIndex()` |
| Split Queries | Step 09 | `AsSplitQuery()`, global setting |
| Compiled Queries | Step 09 | `EF.CompileAsyncQuery()` |
| Transactions | Step 10 | `UnitOfWork.BeginTransactionAsync()` |
| Savepoints | Step 10 | `CreateSavepointAsync()` |
| FromSqlRaw | Step 11 | `UserRepository.GetByRoleRawSqlAsync()` |
| ExecuteSqlRaw | Step 11 | `DeactivateUserRawSqlAsync()` |
| Stored Procedures | Step 11 | `sp_GetActiveUsersByRole` |
| Global Query Filters | Step 12 | `HasQueryFilter()` in `AppDbContext` |
| Soft Delete | Step 12 | `SoftDeleteAsync()`, `IsDeleted` |
| Concurrency | Step 12 | `RowVersion`, `DbUpdateConcurrencyException` |
| Shadow Properties | Step 12 | `CreatedBy`, `LastLoginAt` |
| Value Converters | Step 12 | `HasConversion()` on `Role` |
| Interceptors | Step 12 | `AuditInterceptor` |
| Data Seeding | Step 13 | `HasData()` in configurations |
| Repository Pattern | Step 01-14 | `IRepository<T>`, `IUserRepository` |
| Unit of Work | Step 01-14 | `IUnitOfWork`, `UnitOfWork` |
| DTOs | Step 01-14 | `DTOs/User/`, `DTOs/Common/` |
| AutoMapper | Step 14 | `UserMappingProfile`, `IMapper` |
| Dependency Injection | Step 01-14 | `ServiceCollectionExtensions` |

---

## ⚡ Key Rules — Final Summary

| Rule | Reason |
|------|--------|
| Controller only calls Service | HTTP concerns separate from business logic |
| Service only calls UnitOfWork | Repositories never injected into controllers |
| Repository never calls SaveChanges | UnitOfWork decides when to commit |
| Always use DTOs on API boundaries | Never expose raw EF entities |
| Use AsNoTracking for read-only | No snapshot overhead |
| Use Eager Loading in production | Avoids N+1 queries |
| Use Global Filters for soft delete | Auto-applied — never forgotten |
| Always use parameterized raw SQL | Prevents SQL injection |
| Wrap multi-step ops in transaction | All or nothing — prevents inconsistent data |
| Always hardcode Id and dates in HasData | Prevents spurious migrations |
| Use AutoMapper for all entity↔DTO mapping | Eliminates repetitive mapping code |

---

## 🚀 Final Project Complete! 🎉

All 14 steps completed:

```
✅ Step 01 — EF Core Basics
✅ Step 02 — Migrations
✅ Step 03 — Entity Configuration
✅ Step 04 — Keys
✅ Step 05 — Relationships
✅ Step 06 — Loading Related Data
✅ Step 07 — Querying with LINQ
✅ Step 08 — Tracking
✅ Step 09 — Performance Optimization
✅ Step 10 — Transactions
✅ Step 11 — Raw SQL
✅ Step 12 — Advanced Features
✅ Step 13 — Data Seeding
✅ Step 14 — Architecture & Best Practices
```
