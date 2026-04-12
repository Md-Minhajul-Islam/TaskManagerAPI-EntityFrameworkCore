# TaskManagerAPI — Step 06: Loading Related Data

## 📌 What This Step Covers
- Eager Loading — `Include()` and `ThenInclude()`
- Lazy Loading — automatic loading via proxies
- Explicit Loading — `Entry().Reference/Collection().LoadAsync()`
- Adding loading methods to `IUserRepository` / `UserRepository`
- Adding loading methods to `IUserService` / `UserService`
- New DTOs for loading responses
- Controller calling Service only (never UnitOfWork directly)

---

## 🧠 Three Loading Strategies

| Strategy | How | SQL Queries | Use When |
|----------|-----|-------------|----------|
| **Eager** | `Include()` / `ThenInclude()` | 1 query with JOINs | You know upfront what you need ✅ |
| **Lazy** | Access navigation property | 1 extra query per property access | Quick prototyping only ⚠️ |
| **Explicit** | `Entry().Reference/Collection().LoadAsync()` | 1 extra query per call | Load selectively after initial load |

---

## 1️⃣ Eager Loading

Eager Loading loads related data **in the same SQL query** using JOINs.
You tell EF Core exactly what to load upfront using `Include()`.

### `Include()` — first level
```csharp
// Loads User + Profile in ONE query
// SQL: SELECT * FROM Users u
//      LEFT JOIN UserProfiles up ON up.UserId = u.Id
//      WHERE u.Id = @id
await _dbSet
    .Include(u => u.Profile)
    .FirstOrDefaultAsync(u => u.Id == id);
```

### `ThenInclude()` — second level
```csharp
// Loads User → TeamMembers → Team in ONE query
// SQL: JOIN TeamMembers tm ON tm.UserId = u.Id
//      JOIN Teams t ON t.Id = tm.TeamId
await _dbSet
    .Include(u => u.TeamMembers)         // first level
        .ThenInclude(tm => tm.Team)      // second level (inside TeamMembers)
    .FirstOrDefaultAsync(u => u.Id == id);
```

### Multiple `Include()` chains
```csharp
// Load multiple relations in ONE query
await _dbSet
    .Include(u => u.Profile)
    .Include(u => u.TeamMembers)
        .ThenInclude(tm => tm.Team)
    .Include(u => u.OwnedProjects)
    .Include(u => u.AssignedTasks)
    .FirstOrDefaultAsync(u => u.Id == id);
```

---

## 2️⃣ Generic Repository — `GetByIdWithIncludesAsync`

The base `Repository<T>` has a method that accepts Include functions as parameters:

```csharp
// IRepository.cs
Task<T?> GetByIdWithIncludesAsync(
    int id,
    params Func<IQueryable<T>, IQueryable<T>>[] includes);

Task<IEnumerable<T>> GetAllWithIncludesAsync(
    params Func<IQueryable<T>, IQueryable<T>>[] includes);
```

### Understanding `Func<IQueryable<T>, IQueryable<T>>`

```
Func  <  IQueryable<T>  ,  IQueryable<T>  >
            ↑                    ↑
         INPUT               OUTPUT
      query before         same query but
      Include added        with Include added

// A concrete example:
q => q.Include(u => u.Profile)
↑              ↑
q = the        adds .Include(Profile) to
input query    the query and returns it
```

### How it works inside `Repository<T>`
```csharp
public async Task<T?> GetByIdWithIncludesAsync(
    int id,
    params Func<IQueryable<T>, IQueryable<T>>[] includes)
{
    IQueryable<T> query = _dbSet;       // Step 1: plain query

    foreach (var include in includes)
        query = include(query);         // Step 2: apply each Include function

    return await query
        .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
}
```

### How `UserRepository` calls it
```csharp
// Passes ONE include function
public async Task<User?> GetWithProfileAsync(int id)
    => await GetByIdWithIncludesAsync(
            id,
            q => q.Include(u => u.Profile)     // ← the Func
       );

// Passes MULTIPLE include functions
public async Task<User?> GetWithAllRelatedAsync(int id)
    => await GetByIdWithIncludesAsync(
            id,
            q => q.Include(u => u.Profile),                     // function 1
            q => q.Include(u => u.TeamMembers)
                    .ThenInclude(tm => tm.Team),                 // function 2
            q => q.Include(u => u.OwnedProjects),               // function 3
            q => q.Include(u => u.AssignedTasks)                // function 4
       );
```

### The `params` keyword
```csharp
// params = pass any number of arguments without creating an array manually
// C# wraps them into an array automatically

// Without params — verbose:
GetByIdWithIncludesAsync(id, new Func<IQueryable<User>, IQueryable<User>>[]
{
    q => q.Include(u => u.Profile),
    q => q.Include(u => u.TeamMembers)
});

// With params — clean:
GetByIdWithIncludesAsync(id,
    q => q.Include(u => u.Profile),
    q => q.Include(u => u.TeamMembers)
);
```

---

## 3️⃣ Explicit Loading

Explicit Loading loads related data **on demand** with a **separate SQL query**.
You already have the entity and decide later what to load.

```csharp
// Step 1 — load User with NO related data
var user = await _unitOfWork.Users.GetByIdAsync(id);

// Step 2 — explicitly load one relation at a time
// Each fires a SEPARATE SQL query

// Reference = single navigation property (one entity)
await _context.Entry(user)
    .Reference(u => u.Profile)
    .LoadAsync();
// SQL: SELECT * FROM UserProfiles WHERE UserId = @userId

// Collection = collection navigation property (many entities)
await _context.Entry(user)
    .Collection(u => u.TeamMembers)
    .LoadAsync();
// SQL: SELECT * FROM TeamMembers WHERE UserId = @userId
```

### In `UserRepository`
```csharp
public async Task LoadProfileAsync(User user)
    => await _context.Entry(user)
        .Reference(u => u.Profile)      // Reference = single entity
        .LoadAsync();

public async Task LoadTeamsAsync(User user)
    => await _context.Entry(user)
        .Collection(u => u.TeamMembers) // Collection = many entities
        .LoadAsync();
```

### When to use Explicit Loading
- You loaded an entity but conditionally need its relations
- You want full control over when extra queries fire
- Loading large collections only when specifically needed

---

## 4️⃣ Lazy Loading

Lazy Loading loads related data **automatically** when you access a navigation property.
EF Core wraps entities in a proxy class that fires SQL when properties are accessed.

### Setup — install package
```bash
dotnet add package Microsoft.EntityFrameworkCore.Proxies
```

### Setup — enable in `ServiceCollectionExtensions.cs`
```csharp
options.UseSqlServer(connectionString)
       .UseLazyLoadingProxies();   // ← enables lazy loading
```

### Setup — mark ALL navigation properties as `virtual`
```csharp
// Every navigation property MUST be virtual for lazy loading to work
// EF Core proxy overrides virtual properties to fire SQL automatically

public virtual UserProfile?             Profile     { get; set; }
public virtual ICollection<TeamMember>  TeamMembers { get; set; } = new List<TeamMember>();
public virtual ICollection<Project>     OwnedProjects { get; set; } = new List<Project>();
```

### How it works
```csharp
// Load User with NO Include()
var user = await _unitOfWork.Users.GetByIdAsync(id);

// Accessing Profile property AUTOMATICALLY fires a SQL query!
// EF Core proxy intercepts this access
var bio = user.Profile?.Bio;
// SQL fires here: SELECT * FROM UserProfiles WHERE UserId = @userId
```

### ⚠️ N+1 Problem — why Lazy Loading is dangerous
```csharp
// ❌ DANGEROUS — 101 SQL queries for 100 users!
var users = await _context.Users.ToListAsync();  // 1 query
foreach (var user in users)
{
    var bio = user.Profile?.Bio;
    // ↑ fires 1 SQL per user = 100 extra queries!
}
// Total: 101 queries 😱

// ✅ CORRECT — use Eager Loading instead
var users = await _context.Users
    .Include(u => u.Profile)    // 1 query with JOIN
    .ToListAsync();
// Total: 1 query ✅
```

> 💡 Use Lazy Loading only for quick prototyping.
> Always use Eager Loading in production.

---

## 5️⃣ Full Layer Flow

### Rule — Controller only calls Service, never UnitOfWork directly

```
Controller  →  calls IUserService only              ✅
Service     →  calls IUnitOfWork                    ✅
UnitOfWork  →  calls IUserRepository                ✅
Repository  →  queries AppDbContext                 ✅

Controller  →  calls IUnitOfWork directly           ❌ WRONG
Controller  →  calls IUserRepository directly       ❌ WRONG
```

### Example: GET /api/users/5/with-profile

```
1. GET /api/users/5/with-profile

2. UsersController.GetWithProfile(5)
   └─ calls _userService.GetWithProfileAsync(5)

3. UserService.GetWithProfileAsync(5)
   └─ calls _unitOfWork.Users.GetWithProfileAsync(5)

4. UserRepository.GetWithProfileAsync(5)
   └─ calls GetByIdWithIncludesAsync(5, q => q.Include(u => u.Profile))

5. Repository<T>.GetByIdWithIncludesAsync(5, ...)
   └─ query = _dbSet
   └─ query = q.Include(u => u.Profile)   ← include applied
   └─ query.FirstOrDefaultAsync(u => u.Id == 5)

6. SQL:
   SELECT u.*, up.*
   FROM Users u
   LEFT JOIN UserProfiles up ON up.UserId = u.Id
   WHERE u.Id = 5

7. UserService maps result → UserWithProfileDto
8. Controller returns 200 OK with UserWithProfileDto
```

---

## 6️⃣ New DTOs in This Step

### `UserWithProfileDto`
```csharp
public class UserWithProfileDto
{
    public int        Id              { get; set; }
    public string     FullName        { get; set; }
    public string     Email           { get; set; }
    public string     LoadingStrategy { get; set; }  // explains which strategy was used
    public ProfileDto? Profile        { get; set; }
}

public class ProfileDto
{
    public string  Bio       { get; set; }
    public string? AvatarUrl { get; set; }
    public string? GitHubUrl { get; set; }
}
```

### `UserWithTeamsDto`
```csharp
public class UserWithTeamsDto
{
    public int    Id       { get; set; }
    public string FullName { get; set; }
    public string Email    { get; set; }
    public IEnumerable<TeamDto> Teams { get; set; }
}

public class TeamDto
{
    public int      Id       { get; set; }
    public string   Name     { get; set; }
    public string   Role     { get; set; }
    public DateTime JoinedAt { get; set; }
}
```

---

## 7️⃣ API Endpoints Added

| Method | Endpoint | Strategy | SQL Queries |
|--------|----------|----------|-------------|
| `GET` | `/api/users/{id}/with-profile` | Eager — `Include()` | 1 with JOIN |
| `GET` | `/api/users/{id}/with-teams` | Eager — `Include()` + `ThenInclude()` | 1 with JOINs |
| `GET` | `/api/users/{id}/explicit-load` | Explicit — `LoadAsync()` | 3 separate |
| `GET` | `/api/users/{id}/lazy-load` | Lazy — property access | 2 separate |

---

## 8️⃣ `Reference` vs `Collection` in Explicit Loading

| Method | Used for | Example |
|--------|----------|---------|
| `.Reference()` | Single navigation property (one entity) | `user.Profile` |
| `.Collection()` | Collection navigation property (many entities) | `user.TeamMembers` |

```csharp
// Reference — loads one related entity
_context.Entry(user).Reference(u => u.Profile).LoadAsync();

// Collection — loads many related entities
_context.Entry(user).Collection(u => u.TeamMembers).LoadAsync();
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Controller only calls Service | Never inject UnitOfWork or Repository into Controller |
| Service only calls UnitOfWork | Never query DbContext directly from Service |
| Use Eager Loading in production | Avoids N+1 queries — predictable performance |
| Mark all navigation properties `virtual` | Required for Lazy Loading proxies to work |
| Use `Reference()` for single nav props | `Collection()` is for ICollection properties only |
| `ThenInclude()` chains a second level | Always comes after `Include()`, not standalone |
| Avoid Lazy Loading in loops | N+1 problem destroys performance |

---

## 🚀 How to Run

```bash
# Install lazy loading package
dotnet add package Microsoft.EntityFrameworkCore.Proxies

# No new migrations needed — no model changes
dotnet build
dotnet run

# Test in Swagger
# POST /api/users first to create a user
# Then test:
# GET /api/users/1/with-profile
# GET /api/users/1/with-teams
# GET /api/users/1/explicit-load
# GET /api/users/1/lazy-load
```

---

## ✅ Folder Structure After Step 6

```
TaskManagerAPI/
├── Controllers/
│   └── UsersController.cs              ← Updated (4 new endpoints, calls Service only)
├── DTOs/
│   └── User/
│       ├── UserWithProfileDto.cs       ← NEW
│       └── UserWithTeamsDto.cs         ← NEW
├── Models/
│   └── *.cs                            ← All navigation properties marked virtual
├── Repositories/
│   ├── Interfaces/
│   │   ├── IRepository.cs              ← Updated (GetByIdWithIncludesAsync)
│   │   └── IUserRepository.cs         ← Updated (loading methods)
│   └── Implementations/
│       ├── Repository.cs               ← Updated (include pipeline)
│       └── UserRepository.cs          ← Updated (Eager + Explicit methods)
├── Services/
│   ├── Interfaces/
│   │   └── IUserService.cs             ← Updated (loading methods)
│   └── Implementations/
│       └── UserService.cs              ← Updated (loading implementations)
└── Extensions/
    └── ServiceCollectionExtensions.cs  ← Updated (UseLazyLoadingProxies)
```

---

## ✅ What's Next — Step 07: Querying Data with LINQ
In the next step we will:
- **Filter** with `Where()`
- **Project** with `Select()`
- **Sort** with `OrderBy()` / `OrderByDescending()`
- **Group** with `GroupBy()`
- **Aggregate** with `Sum()`, `Count()`, `Average()`
- **Paginate** with `Skip()` / `Take()` via `PagedResult<T>`
