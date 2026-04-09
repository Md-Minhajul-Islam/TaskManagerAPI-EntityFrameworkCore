# TaskManagerAPI — Step 01: EF Core Basics

## 📌 What This Step Covers
- What is ORM & why we use it
- DbContext & DbSet
- Connection String configuration
- Change Tracking
- SaveChanges / SaveChangesAsync
- Generic Repository Pattern
- Specific Repository Pattern
- Unit of Work Pattern
- Service Layer
- Dependency Injection via Extensions

---

## 🗂️ Folder Structure

```
TaskManagerAPI/
├── Controllers/
│   └── UsersController.cs          ← HTTP layer (routes only, no logic)
├── Data/
│   └── AppDbContext.cs             ← EF Core bridge to the database
├── DTOs/
│   └── User/
│       ├── CreateUserDto.cs        ← Input model for POST
│       ├── UpdateUserDto.cs        ← Input model for PUT
│       └── UserResponseDto.cs      ← Output model for all responses
├── Models/
│   ├── BaseEntity.cs               ← Shared Id, CreatedAt, UpdatedAt
│   └── User.cs                     ← User domain model (maps to DB table)
├── Extensions/
│   └── ServiceCollectionExtensions.cs  ← Organized DI registrations
├── Repositories/
│   ├── Interfaces/
│   │   ├── IRepository.cs          ← Generic CRUD contract
│   │   └── IUserRepository.cs      ← User-specific query contract
│   └── Implementations/
│       ├── Repository.cs           ← Generic CRUD implementation
│       └── UserRepository.cs       ← User-specific query implementation
├── Services/
│   ├── Interfaces/
│   │   └── IUserService.cs         ← Business logic contract
│   └── Implementations/
│       └── UserService.cs          ← Business logic implementation
├── UnitOfWork/
│   ├── IUnitOfWork.cs              ← Contract for coordinating repositories
│   └── UnitOfWork.cs              ← Single SaveChangesAsync for all repos
├── appsettings.json                ← Connection string lives here
└── Program.cs                      ← App entry point (clean, 3 lines)
```

---

## 🔄 Request Flow (How a Request Travels Through the App)

```
HTTP Request  (e.g. POST /api/users)
      │
      ▼
┌─────────────────┐
│  UsersController │  ← Receives HTTP request, calls Service, returns HTTP response
└────────┬────────┘
         │ calls
         ▼
┌─────────────────┐
│   UserService   │  ← Contains business logic (validation, mapping, rules)
└────────┬────────┘
         │ calls
         ▼
┌─────────────────┐
│   IUnitOfWork   │  ← Provides access to repositories + single SaveChangesAsync
└────────┬────────┘
         │ accesses
         ▼
┌──────────────────────┐
│  IUserRepository     │  ← Data access queries (GetByEmail, GetActiveUsers etc.)
│  (via Repository<T>) │
└────────┬─────────────┘
         │ uses
         ▼
┌─────────────────┐
│  AppDbContext   │  ← EF Core: tracks changes, generates SQL, talks to DB
└────────┬────────┘
         │
         ▼
   SQL Server DB
```

---

## 🧠 Core Concepts Explained

### 1. ORM (Object-Relational Mapper)
Without ORM you write raw SQL:
```sql
INSERT INTO Users (FullName, Email) VALUES ('John', 'john@mail.com')
```
With EF Core (ORM) you write C#:
```csharp
await _context.Users.AddAsync(user);
await _context.SaveChangesAsync();
// EF Core generates the SQL for you ✅
```

---

### 2. BaseEntity
```csharp
public abstract class BaseEntity
{
    public int       Id        { get; set; }   // Primary Key for every entity
    public DateTime  CreatedAt { get; set; }   // Set on creation
    public DateTime? UpdatedAt { get; set; }   // Set on every update
}
```
> 💡 Every model inherits from `BaseEntity` so we never forget `Id`, `CreatedAt`, `UpdatedAt`.

---

### 3. DbContext (`AppDbContext`)
```csharp
public class AppDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();  // ← Represents the Users TABLE
}
```
`DbContext` is the **brain of EF Core**. It:
- Manages the database connection
- Tracks every model loaded from DB (Change Tracking)
- Generates SQL when `SaveChangesAsync()` is called
- Acts as a built-in Unit of Work internally

---

### 4. Connection String
Defined in `appsettings.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaskManagerDB;Trusted_Connection=True;"
}
```
Registered in `ServiceCollectionExtensions.cs`:
```csharp
services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
```
> 💡 Never hardcode connection strings. Always read from configuration.

---

### 5. Change Tracking
EF Core **automatically watches** every model it loads from the database.
When you change a property, EF Core knows about it:

```
User loaded from DB   →   EntityState = Unchanged
user.FullName = "Bob" →   EntityState = Modified   (EF detected the change!)
SaveChangesAsync()    →   EF generates UPDATE SQL only for changed columns
```

We use Change Tracking in `AppDbContext.SaveChangesAsync()` to auto-set `UpdatedAt`:
```csharp
foreach (var entry in ChangeTracker.Entries<BaseEntity>())
{
    if (entry.State == EntityState.Modified)
        entry.Entity.UpdatedAt = DateTime.UtcNow;  // ← Automatic!
}
```

---

### 6. EntityState Lifecycle

```
new User()           →  EntityState = Detached   (not tracked yet)
_dbSet.Add(user)     →  EntityState = Added      (INSERT queued)
SaveChangesAsync()   →  EntityState = Unchanged  (saved to DB)

user.FullName = "X"  →  EntityState = Modified   (UPDATE queued)
SaveChangesAsync()   →  EntityState = Unchanged  (saved to DB)

_dbSet.Remove(user)  →  EntityState = Deleted    (DELETE queued)
SaveChangesAsync()   →  EntityState = Detached   (gone from DB)
```

---

### 7. Generic Repository (`IRepository<T>` / `Repository<T>`)
Provides standard CRUD for **any entity** so we don't repeat the same code:
```csharp
// Works for User, Project, Task, Team — any entity!
Task<T?>             GetByIdAsync(int id);
Task<IEnumerable<T>> GetAllAsync();
Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
Task AddAsync(T entity);
void Update(T entity);
void Remove(T entity);
```
> 💡 Every specific repository (e.g. `UserRepository`) **inherits** from `Repository<T>` and gets all this for free.

---

### 8. Specific Repository (`IUserRepository` / `UserRepository`)
Adds entity-specific queries on top of the generic ones:
```csharp
Task<User?> GetByEmailAsync(string email);       // User-specific
Task<IEnumerable<User>> GetActiveUsersAsync();   // User-specific
Task<bool> IsEmailUniqueAsync(string email);     // User-specific
```

---

### 9. Unit of Work (`IUnitOfWork` / `UnitOfWork`)
Groups all repositories under one roof with a **single SaveChangesAsync**:
```csharp
// All repositories share the SAME DbContext instance
_unitOfWork.Users.AddAsync(user);
_unitOfWork.Projects.AddAsync(project);

// ONE save commits BOTH operations atomically ✅
await _unitOfWork.SaveChangesAsync();
```
Without Unit of Work each repository would have its own `SaveChangesAsync()` — if one succeeds and the other fails, your data becomes inconsistent.

---

### 10. Service Layer (`UserService`)
Contains all **business logic** — validation, rules, mapping:
```
Controller  →  receives HTTP, delegates to service
Service     →  validates, applies rules, calls UnitOfWork, maps to DTO
Repository  →  just queries/saves, no business logic
```

Rule: **Repositories never contain business logic. Services never query the DB directly.**

---

### 11. DTOs (Data Transfer Objects)
We never expose raw models to the API consumer:

| DTO | Used for | Why |
|-----|----------|-----|
| `CreateUserDto` | POST body | Only accept fields needed for creation |
| `UpdateUserDto` | PUT body | Only accept fields allowed to change |
| `UserResponseDto` | All responses | Control exactly what gets returned |

> 💡 `Id` is never in a Create DTO (DB assigns it). `Email` is never in Update DTO (emails don't change).

---

### 12. Dependency Injection (DI)

Everything is registered in `Extensions/ServiceCollectionExtensions.cs`:

```csharp
// Registration
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IUserService, UserService>();
```

`AddScoped` means **one instance per HTTP request** — perfect for DbContext and repositories.

| Lifetime | When to use |
|----------|-------------|
| `AddScoped` | DbContext, Repositories, Services (per request) |
| `AddSingleton` | Config, Cache (lives forever) |
| `AddTransient` | Lightweight, stateless utilities (new every time) |

---

## 🔁 Full Example: POST /api/users

```
1. POST /api/users  { "fullName": "Alice", "email": "alice@mail.com", "role": "Admin" }

2. UsersController.Create(dto)
   └─ calls _userService.CreateAsync(dto)

3. UserService.CreateAsync(dto)
   ├─ calls _unitOfWork.Users.IsEmailUniqueAsync("alice@mail.com")  → true
   ├─ creates new User { FullName="Alice", Email="alice@mail.com" }
   ├─ calls _unitOfWork.Users.AddAsync(user)     → EntityState = Added
   ├─ calls _unitOfWork.SaveChangesAsync()       → INSERT INTO Users ...
   │   └─ AppDbContext.SaveChangesAsync override checked for Modified entries
   └─ returns UserResponseDto { Id=1, FullName="Alice", ... }

4. Controller returns 201 Created with UserResponseDto
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Controllers only call Services | Keep HTTP concerns separate from business logic |
| Services only call UnitOfWork | Repositories should never be injected directly into controllers |
| Repositories never call SaveChanges | Only UnitOfWork decides when to commit |
| Always use DTOs on API boundaries | Never expose raw EF models to HTTP responses |
| One DbContext per request | `AddScoped` ensures this automatically |

---

## 🚀 How to Run

```bash
# 1. Make sure SQL Server LocalDB is installed
# 2. Restore packages
dotnet restore

# 3. Run (migrations come in Step 02 — DB not created yet)
dotnet run

# 4. Open Swagger
# https://localhost:{port}/swagger
```

---

## ✅ What's Next — Step 02: Migrations
In the next step we will:
- Install EF Core CLI tools
- Create the first migration (`InitialCreate`)
- Understand the `Up()` and `Down()` methods
- Run `dotnet ef database update` to create the actual database
- Test all endpoints end-to-end with a real database
