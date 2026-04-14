# TaskManagerAPI — Step 09: Performance Optimization

## 📌 What This Step Covers
- Indexes — single, composite, filtered
- Split Queries — fix cartesian explosion with multiple Includes
- Compiled Queries — pre-compile frequently used queries
- AsNoTracking — applied to all read-only queries
- Global Split Query — set as default for all queries

---

## 🧠 Why Performance Matters

Without optimization a simple query can silently become slow:

```
100 users, each with 10 TeamMembers and 5 OwnedProjects

Without Split Query:
  EF Core does ONE query with JOINs
  Result set = 100 × 10 × 5 = 5,000 rows
  Most data is duplicated = cartesian explosion ❌

Without Indexes:
  SQL Server scans EVERY row to find Role = 'Admin'
  Full table scan on 1,000,000 users = very slow ❌

Without AsNoTracking:
  EF Core creates a snapshot for every loaded entity
  1,000 users loaded = 1,000 snapshots in memory ❌

Without Compiled Queries:
  Every call to GetByEmailAsync translates LINQ → SQL from scratch
  Called 10,000 times/day = 10,000 unnecessary translations ❌
```

---

## 1️⃣ Indexes

An index is a **sorted data structure** that lets SQL Server find rows without scanning the entire table.

### Types of indexes we use

#### Single Column Index
Speeds up filtering or sorting on ONE column.
```csharp
// In UserConfiguration.cs
builder.HasIndex(u => u.Role)
    .HasDatabaseName("IX_Users_Role");
// SQL: CREATE INDEX IX_Users_Role ON Users (Role)
// Speeds up: WHERE Role = 'Admin'
```

#### Composite Index
Speeds up queries that filter on TWO columns together.
```csharp
builder.HasIndex(u => new { u.IsActive, u.Role })
    .HasDatabaseName("IX_Users_IsActive_Role");
// SQL: CREATE INDEX IX_Users_IsActive_Role ON Users (IsActive, Role)
// Speeds up: WHERE IsActive = 1 AND Role = 'Admin'
```

#### Filtered Index
Indexes only a **subset** of rows — smaller index, faster lookups.
```csharp
builder.HasIndex(u => u.IsActive)
    .HasFilter("[IsActive] = 1")
    .HasDatabaseName("IX_Users_IsActive_Filtered");
// SQL: CREATE INDEX IX_Users_IsActive_Filtered
//      ON Users (IsActive) WHERE IsActive = 1
// Only active users are indexed — index is much smaller ✅
```

### How indexes help

```
Without index — Full Table Scan:
  SQL Server: "Find all users where Role = 'Admin'"
  → reads EVERY row one by one until done
  → 1,000,000 rows = very slow ❌

With index — Index Seek:
  SQL Server: "Find all users where Role = 'Admin'"
  → jumps directly to 'Admin' in the sorted index
  → finds all matches instantly ✅
```

### Index column order matters (Composite)
```
Index: (IsActive, Role)

Efficient queries:
  WHERE IsActive = 1                    ✅ uses index (leftmost column)
  WHERE IsActive = 1 AND Role = 'Admin' ✅ uses index (both columns)

Inefficient queries:
  WHERE Role = 'Admin'                  ❌ can't use index (skipped leftmost)
```

> 💡 Always put the most selective column FIRST in a composite index.
> The leftmost column must be in the WHERE clause for the index to be used.

### All indexes added in this step

| Index Name | Columns | Type | Speeds Up |
|-----------|---------|------|-----------|
| `IX_Users_Role` | `Role` | Single | `WHERE Role = ?` |
| `IX_Users_IsActive` | `IsActive` | Single | `WHERE IsActive = ?` |
| `IX_Users_IsActive_Role` | `IsActive, Role` | Composite | `WHERE IsActive = ? AND Role = ?` |
| `IX_Users_IsActive_Filtered` | `IsActive` WHERE `IsActive=1` | Filtered | Active user queries |

---

## 2️⃣ Split Queries

When you use multiple `Include()` calls EF Core by default does ONE query with multiple JOINs. This causes **cartesian explosion**.

### The Problem — Cartesian Explosion
```
User has:
  - 3 TeamMembers
  - 5 OwnedProjects

Single query result (default):
  Row 1: User | TeamMember 1 | Project 1
  Row 2: User | TeamMember 1 | Project 2
  Row 3: User | TeamMember 1 | Project 3
  Row 4: User | TeamMember 1 | Project 4
  Row 5: User | TeamMember 1 | Project 5
  Row 6: User | TeamMember 2 | Project 1
  Row 7: User | TeamMember 2 | Project 2
  ...
  Row 15: User | TeamMember 3 | Project 5

Total = 3 × 5 = 15 rows for ONE user
Most data is duplicated ❌
```

### The Fix — AsSplitQuery
```csharp
// Option 1 — on a specific query
_dbSet
    .Include(u => u.TeamMembers)
    .Include(u => u.OwnedProjects)
    .AsSplitQuery()             // ← EF Core sends separate SQL per Include
    .FirstOrDefaultAsync(u => u.Id == id);

// EF Core sends:
// Query 1: SELECT * FROM Users WHERE Id = @id
// Query 2: SELECT * FROM TeamMembers WHERE UserId = @id
// Query 3: SELECT * FROM Projects WHERE OwnerId = @id
// No duplication ✅
```

```csharp
// Option 2 — global default (our approach)
// In ServiceCollectionExtensions.cs
options.UseSqlServer(connectionString,
    sqlOptions => sqlOptions
        .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
// Every query uses split query automatically ✅
// No need to add AsSplitQuery() on every query
```

### Single Query vs Split Query

| | Single Query | Split Query |
|--|-------------|------------|
| Number of SQL queries | 1 | 1 per Include |
| Data duplication | ❌ Cartesian explosion | ✅ No duplication |
| Network round trips | 1 | Multiple |
| Best for | Few simple Includes | Multiple collection Includes |
| Configured with | Default | `AsSplitQuery()` or global |

> 💡 Since we set Split Query globally, our `GetWithAllRelatedAsync` works
> correctly without any code change — the global setting handles it.

---

## 3️⃣ Compiled Queries

EF Core translates LINQ to SQL **every time** a query is called. Compiled Queries cache this translation so it only happens **once at startup**.

### How it works
```
Without compiled query (default):
  Call 1: LINQ → translate → SQL → execute → result
  Call 2: LINQ → translate → SQL → execute → result   ← translation again!
  Call 3: LINQ → translate → SQL → execute → result   ← translation again!
  Called 10,000 times = 10,000 translations ❌

With compiled query:
  Startup: LINQ → translate → SQL (cached)
  Call 1:  cached SQL → execute → result             ← no translation!
  Call 2:  cached SQL → execute → result             ← no translation!
  Call 3:  cached SQL → execute → result             ← no translation!
  Called 10,000 times = 0 extra translations ✅
```

### Compiled query syntax
```csharp
// Declare as static readonly — compiled ONCE when class is loaded
private static readonly Func<AppDbContext, string, Task<User?>>
    GetByEmailCompiled =
        EF.CompileAsyncQuery((AppDbContext ctx, string email) =>
            ctx.Users.FirstOrDefault(
                u => u.Email.ToLower() == email.ToLower()));
//  ↑ return type                      ↑ parameters
//  Func<DbContext, param1Type, returnType>

// Use it in a method
public async Task<User?> GetByEmailAsync(string email)
    => await GetByEmailCompiled(_context, email);
//                              ↑ pass DbContext + parameters
```

### Compiled query returning IAsyncEnumerable
```csharp
// For queries that return multiple rows use IAsyncEnumerable
private static readonly Func<AppDbContext, IAsyncEnumerable<User>>
    GetActiveUsersCompiled =
        EF.CompileAsyncQuery((AppDbContext ctx) =>
            ctx.Users
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName));

// Consume with await foreach
public async Task<IEnumerable<User>> GetActiveUsersAsync()
{
    var users = new List<User>();
    await foreach (var user in GetActiveUsersCompiled(_context))
        users.Add(user);
    return users;
}
```

### Compiled queries added in this step

| Compiled Query | Used in | Benefit |
|---------------|---------|---------|
| `GetByEmailCompiled` | `GetByEmailAsync` | High-frequency — called on every login |
| `GetActiveUsersCompiled` | `GetActiveUsersAsync` | Common list query |
| `GetByRoleCompiled` | `GetByRoleAsync` | Frequent filter query |

---

## 4️⃣ AsNoTracking — Project-Wide

All read-only repository methods use `AsNoTracking()`:

```csharp
// In Repository<T>
public async Task<IEnumerable<T>> GetAllAsNoTrackingAsync()
    => await _dbSet.AsNoTracking().ToListAsync();

public async Task<T?> GetByIdAsNoTrackingAsync(int id)
    => await _dbSet.AsNoTracking()
        .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
```

Used in `PerformanceDemoAsync`:
```csharp
// AsNoTracking — loads data without snapshot overhead
var users = await _unitOfWork.Users.GetAllAsNoTrackingAsync();
```

---

## 5️⃣ Performance Demo Endpoint

`GET /api/users/performance-demo` returns:

```json
{
  "totalUsers": 5,
  "activeUsers": 4,
  "byRole": [
    { "role": "Admin",  "count": 2 },
    { "role": "Member", "count": 3 }
  ],
  "optimizations": [
    "AsNoTracking — no change tracking snapshot created",
    "Compiled queries on GetByEmail, GetActiveUsers, GetByRole",
    "Composite index on IsActive + Role columns",
    "Filtered index on IsActive = 1 — smaller index",
    "AsSplitQuery on GetWithAllRelatedAsync — no cartesian explosion"
  ]
}
```

---

## 🔄 Full Optimization Picture

```
GET /api/users/performance-demo
         │
         ▼
UserService.GetPerformanceDemoAsync()
         │
         ├── GetAllAsNoTrackingAsync()
         │       └── AsNoTracking() → no snapshot ✅
         │
         ├── GroupBy(u => u.Role) → uses IX_Users_Role index ✅
         │
         └── Count(u => u.IsActive) → uses IX_Users_IsActive_Filtered ✅

GET /api/users/1/with-all-related
         │
         ▼
UserRepository.GetWithAllRelatedAsync(1)
         │
         ├── Include(u => u.Profile)
         ├── Include(u => u.TeamMembers).ThenInclude(tm => tm.Team)
         ├── Include(u => u.OwnedProjects)
         ├── Include(u => u.AssignedTasks)
         │
         └── Global SplitQuery → separate SQL per Include ✅
             No cartesian explosion ✅
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Add indexes on columns you filter or sort by | Full table scan without index |
| Put most selective column first in composite index | Index only used if leftmost column is in WHERE |
| Use filtered indexes for commonly filtered values | Smaller index = faster |
| Set Split Query globally | Prevents cartesian explosion on all queries |
| Use compiled queries for high-frequency queries | Eliminates LINQ→SQL translation overhead |
| Always use AsNoTracking for read-only queries | No snapshot = less memory + faster |
| Don't over-index | Too many indexes slow down INSERT/UPDATE/DELETE |

---

## 🚀 How to Run

```bash
# Create migration for new indexes
dotnet ef migrations add AddPerformanceIndexes --output-dir Data/Migrations

# Apply
dotnet ef database update

# Run
dotnet run

# Test
GET /api/users/performance-demo
GET /api/users/1/with-all-related  ← uses global split query
```

---

## ✅ Folder Structure After Step 9

```
TaskManagerAPI/
├── Data/
│   ├── Configurations/
│   │   └── UserConfiguration.cs        ← Updated (indexes added)
│   └── Migrations/
│       └── XXXXXX_AddPerformanceIndexes.cs  ← NEW
├── Repositories/
│   └── Implementations/
│       └── UserRepository.cs           ← Updated (compiled queries)
├── Services/
│   ├── Interfaces/
│   │   └── IUserService.cs             ← Updated (performance demo)
│   └── Implementations/
│       └── UserService.cs              ← Updated (performance demo)
├── Controllers/
│   └── UsersController.cs              ← Updated (performance demo endpoint)
└── Extensions/
    └── ServiceCollectionExtensions.cs  ← Updated (global split query)
```

---

## ✅ What's Next — Step 10: Transactions
In the next step we will:
- **BeginTransaction** — start an explicit transaction
- **Commit** — save all changes atomically
- **Rollback** — undo all changes on failure
- **Savepoints** — partial rollbacks within a transaction
- Add transaction support to `UnitOfWork`
