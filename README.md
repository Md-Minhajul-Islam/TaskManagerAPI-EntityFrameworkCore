# TaskManagerAPI —

---
## Complete Architecture Review

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

## DI Registration Summary

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

## Complete Request Flow — End to End

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

## All EF Core Topics — Covered ✅

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
