# TaskManagerAPI — Step 12: Advanced Features

## 📌 What This Step Covers
- Soft Delete — mark as deleted instead of removing
- Global Query Filters — auto-filter every query
- Concurrency Handling — RowVersion / DbUpdateConcurrencyException
- Shadow Properties — DB columns with no C# property
- Value Converters — transform values between C# and DB
- Interceptors — hook into EF Core save operations

---

## 1️⃣ Soft Delete

Soft Delete means **never actually deleting a row**. Instead you set a flag `IsDeleted = true`.

### Why Soft Delete?
```
Hard Delete (actual DELETE):
  DELETE FROM Users WHERE Id = 1
  → Row is gone forever ❌
  → No audit trail ❌
  → Related data can become orphaned ❌

Soft Delete (flag approach):
  UPDATE Users SET IsDeleted = 1 WHERE Id = 1
  → Row stays in DB ✅
  → Audit trail preserved ✅
  → Can be restored ✅
  → Appears deleted to the application ✅
```

### Implementation
```csharp
// Instead of Remove()
_dbSet.Remove(user);                    // ❌ hard delete

// We do soft delete
user.IsDeleted = true;
_dbSet.Update(user);                    // ✅ soft delete
await _context.SaveChangesAsync();
// SQL: UPDATE Users SET IsDeleted = 1 WHERE Id = @id
```

---

## 2️⃣ Global Query Filters

A Global Query Filter is a **WHERE clause automatically added to every query** on an entity.
Configured once — applied everywhere.

### Setup in `AppDbContext`
```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Every query on User automatically gets WHERE IsDeleted = 0
    modelBuilder.Entity<User>()
        .HasQueryFilter(u => !u.IsDeleted);
}
```

### How it works
```csharp
// You write:
var users = await _context.Users.ToListAsync();

// EF Core actually executes:
// SELECT * FROM Users WHERE IsDeleted = 0
//                           ↑ injected automatically!

// You write:
var user = await _context.Users.FindAsync(1);

// EF Core actually executes:
// SELECT * FROM Users WHERE Id = 1 AND IsDeleted = 0
//                                     ↑ always added!
```

### Bypassing the filter — `IgnoreQueryFilters()`
```csharp
// See ALL users including soft-deleted ones
var allUsers = await _context.Users
    .IgnoreQueryFilters()           // ← bypass WHERE IsDeleted = 0
    .ToListAsync();
// SQL: SELECT * FROM Users   (no IsDeleted filter)
```

### Global Filter affects related data too
```csharp
// If Project has a Global Filter (IsDeleted = 0)
// Loading a User's OwnedProjects automatically filters deleted projects
var user = await _context.Users
    .Include(u => u.OwnedProjects)   // ← only returns non-deleted projects!
    .FirstOrDefaultAsync(u => u.Id == 1);
```

---

## 3️⃣ Shadow Properties

Shadow Properties are **columns in the database that have NO C# property** on the entity class.
EF Core manages them internally — accessed via `EF.Property<T>()` or `entry.Property()`.

### When to use shadow properties
```
Use shadow properties for:
  ✅ Audit fields you don't want polluting your model (CreatedBy, LastLoginAt)
  ✅ DB-level concerns that shouldn't be in the domain model
  ✅ Properties used only for filtering/sorting — not needed in C#
```

### Setup in configuration
```csharp
// In UserConfiguration.cs
// No C# property on User class — exists only in DB
builder.Property<DateTime?>("LastLoginAt")
    .HasColumnName("LastLoginAt")
    .IsRequired(false);

builder.Property<string>("CreatedBy")
    .HasColumnName("CreatedBy")
    .HasMaxLength(100)
    .IsRequired(false);
```

### Reading and writing shadow properties
```csharp
// WRITE — via ChangeTracker entry
_context.Entry(user)
    .Property<DateTime?>("LastLoginAt")
    .CurrentValue = DateTime.UtcNow;

// READ — via ChangeTracker entry
var createdBy = _context.Entry(user)
    .Property<string>("CreatedBy")
    .CurrentValue;

// USE IN QUERY — via EF.Property<T>()
var recentLogins = await _context.Users
    .OrderByDescending(u => EF.Property<DateTime?>(u, "LastLoginAt"))
    .ToListAsync();
```

---

## 4️⃣ Value Converters

Value Converters transform a property's value **between C# and the database**.

```
C# value  →  [converter]  →  DB value
DB value  →  [converter]  →  C# value
```

### Our example — store Role as uppercase
```csharp
builder.Property(u => u.Role)
    .HasConversion(
        v => v.ToUpper(),   // C# → DB: "Admin" becomes "ADMIN"
        v => v              // DB → C#: "ADMIN" stays "ADMIN"
    );
```

### Common Value Converter uses
```csharp
// Store enum as string
builder.Property(u => u.Status)
    .HasConversion(
        v => v.ToString(),              // C#: Status.Active → DB: "Active"
        v => Enum.Parse<Status>(v)      // DB: "Active" → C#: Status.Active
    );

// Store bool as "Y"/"N" string
builder.Property(u => u.IsActive)
    .HasConversion(
        v => v ? "Y" : "N",            // C#: true → DB: "Y"
        v => v == "Y"                  // DB: "Y" → C#: true
    );

// Store list as comma-separated string
builder.Property(u => u.Tags)
    .HasConversion(
        v => string.Join(",", v),       // C#: ["a","b"] → DB: "a,b"
        v => v.Split(",").ToList()      // DB: "a,b" → C#: ["a","b"]
    );
```

---

## 5️⃣ Concurrency Handling — RowVersion

Concurrency conflicts happen when **two users try to update the same row simultaneously**.

### The Problem
```
Time 1: User A reads row  → { FullName: "Alice", RowVersion: [1,2,3] }
Time 1: User B reads row  → { FullName: "Alice", RowVersion: [1,2,3] }

Time 2: User A saves      → { FullName: "Bob",   RowVersion: [4,5,6] }  ✅
Time 2: User B tries save → { FullName: "Carol",  RowVersion: [1,2,3] }  ← stale!

Without concurrency check:
  User B's save OVERWRITES User A's changes silently ❌

With RowVersion:
  EF Core: WHERE Id = @id AND RowVersion = [1,2,3]
  SQL Server: RowVersion is now [4,5,6] — WHERE fails → 0 rows affected
  EF Core throws: DbUpdateConcurrencyException ✅
```

### Setup

Model:
```csharp
[Timestamp]
public byte[] RowVersion { get; set; } = Array.Empty<byte>();
```

Configuration:
```csharp
builder.Property(u => u.RowVersion)
    .IsRowVersion()         // SQL Server auto-increments this on every UPDATE
    .HasColumnName("RowVersion")
    .IsRequired();
```

### Handling the exception
```csharp
try
{
    _context.Users.Update(user);
    await _context.SaveChangesAsync();
    // EF Core SQL:
    // UPDATE Users SET FullName = @name
    // WHERE Id = @id AND RowVersion = @rowVersion
    // ↑ if RowVersion changed → 0 rows affected → exception thrown
}
catch (DbUpdateConcurrencyException ex)
{
    // Another user modified this record between our read and write
    // Options:
    //   1. Tell user to reload and try again (client wins)
    //   2. Keep database values (database wins)
    //   3. Merge changes manually
    throw new InvalidOperationException(
        "Record was modified by another user. Please reload and try again.");
}
```

### Sending RowVersion from client
```
GET /api/users/1  →  response includes RowVersion as base64 string

Client stores the RowVersion
Client sends it back in X-Row-Version header on PUT request

Server reads header → converts from base64 → bytes
Server includes in WHERE clause via EF Core
```

---

## 6️⃣ Interceptors

Interceptors hook into EF Core pipeline **before or after operations**.

### Types of interceptors

| Interceptor | Hooks into |
|-------------|-----------|
| `SaveChangesInterceptor` | `SaveChangesAsync` / `SaveChanges` |
| `DbCommandInterceptor` | Every SQL command |
| `DbConnectionInterceptor` | DB connection open/close |
| `DbTransactionInterceptor` | Transaction begin/commit/rollback |

### Our `AuditInterceptor`
```csharp
public class AuditInterceptor : SaveChangesInterceptor
{
    // Called BEFORE changes are saved to DB
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData      eventData,
        InterceptionResult<int> result,
        CancellationToken       cancellationToken = default)
    {
        var context = eventData.Context;

        foreach (var entry in context.ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                // Auto-set shadow property on every new User
                entry.Property("CreatedBy").CurrentValue = "System";
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
```

### Registering the interceptor
```csharp
// In ServiceCollectionExtensions.cs
services.AddSingleton<AuditInterceptor>();   // register in DI

services.AddDbContext<AppDbContext>((sp, options) =>
    options.UseSqlServer(connectionString)
           .AddInterceptors(sp.GetRequiredService<AuditInterceptor>())
           //                 ↑ EF Core calls this on every SaveChangesAsync
);
```

### Why Singleton for interceptor?
```
Interceptor is registered as Singleton:
  - Created ONCE for the lifetime of the app
  - Shared across all DbContext instances
  - Fine because AuditInterceptor has no state

DbContext is Scoped (per request):
  - New instance per HTTP request
  - Each instance uses the same Singleton interceptor
```

---

## 7️⃣ How Everything Works Together

```
POST /api/users  (creates a new user)
        │
        ▼
UserService.CreateAsync(dto)
        │
        ▼
_unitOfWork.Users.AddAsync(user)      ← user.IsDeleted = false by default
        │
        ▼
_unitOfWork.SaveChangesAsync()
        │
        ▼
AuditInterceptor.SavingChangesAsync() ← interceptor fires!
  └── entry.State == Added
  └── sets shadow property "CreatedBy" = "System"
        │
        ▼
EF Core SQL:
  INSERT INTO Users (FullName, Email, Role, IsDeleted, CreatedBy, ...)
  VALUES (@name, @email, 'MEMBER', 0, 'System', ...)
  --                      ↑ value converter: "Member" → "MEMBER"
  --                               ↑ global filter default


DELETE /api/users/soft-delete/1
        │
        ▼
UserService.SoftDeleteAsync(1)
        │
        ▼
user.IsDeleted = true
_unitOfWork.SaveChangesAsync()
        │
        ▼
EF Core SQL:
  UPDATE Users SET IsDeleted = 1, UpdatedAt = @now WHERE Id = 1

GET /api/users  (after soft delete)
        │
        ▼
EF Core SQL:
  SELECT * FROM Users WHERE IsDeleted = 0
  --                        ↑ Global Query Filter auto-injected!
  -- User 1 NOT returned ✅

GET /api/users/including-deleted
        │
        ▼
_dbSet.IgnoreQueryFilters().ToListAsync()
EF Core SQL:
  SELECT * FROM Users   ← no filter!
  -- User 1 IS returned ✅
```

---

## 🌐 Endpoints Added in This Step

| Method | Endpoint | Demonstrates |
|--------|----------|-------------|
| `DELETE` | `/api/users/soft-delete/{id}` | Soft Delete |
| `GET` | `/api/users` | Global Filter hides deleted |
| `GET` | `/api/users/including-deleted` | IgnoreQueryFilters |
| `GET` | `/api/users/{id}/advanced-demo` | Shadow Props + Global Filter |
| `PUT` | `/api/users/{id}/concurrency-update` | RowVersion concurrency |

---

## 🧪 Test Sequence

```
1. GET  /api/users                     → see all users (none deleted)
2. DELETE /api/users/soft-delete/1     → soft delete user 1
3. GET  /api/users                     → user 1 NOT in list (global filter)
4. GET  /api/users/including-deleted   → user 1 IS in list (filter ignored)
5. GET  /api/users/1/advanced-demo     → see shadow props + filter counts
6. PUT  /api/users/1/concurrency-update
         Header: X-Row-Version: <base64 from GET>
         Body: { "fullName": "...", "role": "...", "isActive": true }
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always use Soft Delete in production | Data recovery, audit trail, referential integrity |
| Global Query Filters apply to related data via Include | Soft-deleted children are hidden automatically |
| Use `IgnoreQueryFilters()` for admin/audit views | To see all records including deleted |
| Shadow properties for DB-only concerns | Keeps domain model clean |
| Always handle `DbUpdateConcurrencyException` | Silent data loss without it |
| Register interceptors as Singleton | They are stateless — one instance is fine |
| Value Converters run on every read and write | Keep them lightweight — no complex logic |

---

## 🚀 How to Run

```bash
# Create migration for new columns
dotnet ef migrations add AddAdvancedFeatures --output-dir Data/Migrations
dotnet ef database update

# Run
dotnet run
```

---

## ✅ Folder Structure After Step 12

```
TaskManagerAPI/
├── Controllers/
│   └── UsersController.cs                  ← Updated (4 new endpoints)
├── DTOs/
│   └── User/
│       └── AdvancedFeaturesDemo.cs         ← NEW
├── Data/
│   ├── AppDbContext.cs                     ← Updated (Global Query Filters)
│   ├── Configurations/
│   │   └── UserConfiguration.cs           ← Updated (Shadow Props, ValueConverter, RowVersion)
│   ├── Interceptors/
│   │   └── AuditInterceptor.cs            ← NEW
│   └── Migrations/
│       └── XXXXXX_AddAdvancedFeatures.cs  ← NEW
├── Extensions/
│   └── ServiceCollectionExtensions.cs     ← Updated (interceptor registered)
├── Models/
│   └── User.cs                            ← Updated (RowVersion added)
├── Repositories/
│   ├── Interfaces/
│   │   └── IUserRepository.cs             ← Updated (soft delete + shadow props)
│   └── Implementations/
│       └── UserRepository.cs              ← Updated (soft delete + shadow props)
└── Services/
    ├── Interfaces/
    │   └── IUserService.cs                ← Updated (advanced methods)
    └── Implementations/
        └── UserService.cs                 ← Updated (advanced implementations)
```

---

## ✅ What's Next — Step 13: Data Seeding
In the next step we will:
- **HasData** — seed initial data in entity configurations
- **Seed with migrations** — initial data applied when DB is created
- Seed roles, default users, and labels
