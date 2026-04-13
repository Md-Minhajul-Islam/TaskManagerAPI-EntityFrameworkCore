# TaskManagerAPI — Step 08: Tracking

## 📌 What This Step Covers
- Change Tracking — how EF Core watches entities
- EntityState — Added, Modified, Deleted, Unchanged, Detached
- AsNoTracking() — disable tracking for read-only queries
- AsNoTrackingWithIdentityResolution() — middle ground
- When to use tracking vs no tracking

---

## 🧠 What is Change Tracking?

When EF Core loads an entity from the database it takes a **snapshot** of its original values and **watches** every property for changes.

```
var user = await _context.Users.FindAsync(1);
// EF Core:
//   1. Loads user from DB
//   2. Takes a snapshot: { FullName: "Alice", Email: "alice@mail.com" }
//   3. Starts watching user for changes

user.FullName = "Bob";
// EF Core detects: FullName changed from "Alice" to "Bob"
// EntityState → Modified

await _context.SaveChangesAsync();
// EF Core generates: UPDATE Users SET FullName = 'Bob' WHERE Id = 1
// Only changed columns are included in the UPDATE ✅
```

---

## 1️⃣ EntityState Lifecycle

Every tracked entity has an `EntityState`. It tells EF Core what SQL to generate at `SaveChanges`.

```
new User()              →  Detached    (not tracked at all)
       │
       ▼ _dbSet.Add(user)
    Added               →  INSERT queued
       │
       ▼ SaveChangesAsync()
    Unchanged           →  saved to DB, now tracked
       │
       ▼ user.FullName = "Bob"
    Modified            →  UPDATE queued
       │
       ▼ SaveChangesAsync()
    Unchanged           →  saved, back to watching
       │
       ▼ _dbSet.Remove(user)
    Deleted             →  DELETE queued
       │
       ▼ SaveChangesAsync()
    Detached            →  gone from DB and tracking
```

### What SaveChanges does per state

| EntityState | Triggered by | SQL Generated |
|-------------|-------------|---------------|
| `Unchanged` | Load from DB, no changes | Nothing — skipped |
| `Added` | `_dbSet.Add(entity)` | `INSERT INTO ...` |
| `Modified` | Property value changed | `UPDATE ... SET changed_cols` |
| `Deleted` | `_dbSet.Remove(entity)` | `DELETE FROM ...` |
| `Detached` | Never tracked / manually detached | Nothing — not managed |

### Code example — all 5 states
```csharp
// Detached — not yet tracked
var user = new User { FullName = "Alice" };
// _context.Entry(user).State == Detached

// Added — queued for INSERT
await _context.Users.AddAsync(user);
// _context.Entry(user).State == Added

// Unchanged — saved, now tracked
await _context.SaveChangesAsync();
// _context.Entry(user).State == Unchanged

// Modified — change detected automatically
user.FullName = "Bob";
// _context.Entry(user).State == Modified

// Deleted — queued for DELETE
_context.Users.Remove(user);
// _context.Entry(user).State == Deleted

// Detached — manually removed from tracking
_context.Entry(user).State = EntityState.Detached;
// _context.Entry(user).State == Detached
```

---

## 2️⃣ AsNoTracking()

`AsNoTracking()` tells EF Core to load data **without creating a tracking snapshot**.

```csharp
// With tracking (default) — EF Core creates a snapshot
var users = await _context.Users.ToListAsync();
// EF Core: loads + snapshots every user = more memory + CPU

// Without tracking — just loads data, no snapshot
var users = await _context.Users
    .AsNoTracking()
    .ToListAsync();
// EF Core: loads users, no snapshot = faster + less memory ✅
```

### Performance difference
```
100 users loaded WITH tracking:
  - 100 entity snapshots created in memory
  - Change tracker watches all 100 objects
  - Extra CPU to detect changes on SaveChanges

100 users loaded WITHOUT tracking:
  - 0 snapshots
  - 0 change tracking overhead
  - Results thrown away after use
  - ✅ Noticeably faster for large datasets
```

### In Repository
```csharp
// Read-only methods use AsNoTracking
public async Task<IEnumerable<T>> GetAllAsNoTrackingAsync()
    => await _dbSet
        .AsNoTracking()
        .ToListAsync();

public async Task<T?> GetByIdAsNoTrackingAsync(int id)
    => await _dbSet
        .AsNoTracking()
        .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == id);
```

---

## 3️⃣ AsNoTrackingWithIdentityResolution()

Middle ground between tracking and no-tracking.

```csharp
var tasks = await _context.Tasks
    .AsNoTrackingWithIdentityResolution()
    .Include(t => t.Project)
    .ToListAsync();
```

### The problem it solves
```
Without identity resolution:
  Task 1 → Project { Id=1, Name="Alpha" }  ← one object
  Task 2 → Project { Id=1, Name="Alpha" }  ← DIFFERENT object, same data
  Task 3 → Project { Id=1, Name="Alpha" }  ← ANOTHER object, same data
  = 3 duplicate Project objects in memory ❌

With AsNoTrackingWithIdentityResolution:
  Task 1 → Project { Id=1, Name="Alpha" }  ← one object
  Task 2 → same Project object ↑            ← reused ✅
  Task 3 → same Project object ↑            ← reused ✅
  = 1 Project object shared across all tasks ✅
```

| | Tracking | AsNoTracking | AsNoTrackingWithIdentityResolution |
|--|----------|-------------|-----------------------------------|
| Change detection | ✅ Yes | ❌ No | ❌ No |
| Memory (snapshots) | ❌ High | ✅ Low | ✅ Low |
| Duplicate entities | ✅ Resolved | ❌ Duplicates possible | ✅ Resolved |
| Performance | Slowest | Fastest | Middle |

---

## 4️⃣ When to Use Each

| Scenario | Recommended | Reason |
|----------|------------|--------|
| `GET` all records (display only) | `AsNoTracking` ✅ | Read-only, no changes needed |
| `GET` by id for display | `AsNoTracking` ✅ | Read-only |
| `GET` by id to **update** | Regular tracking ✅ | Need change detection |
| `GET` by id to **delete** | Regular tracking ✅ | Need to track Deleted state |
| Reports / dashboards | `AsNoTracking` ✅ | Pure reads, max performance |
| Lists with related data (no dupes) | `AsNoTrackingWithIdentityResolution` ✅ | Avoids duplicate objects |
| Creating a new entity | Regular tracking ✅ | Need Added state |
| Batch reads (1000+ rows) | `AsNoTracking` ✅ | Huge memory saving |

---

## 5️⃣ Checking EntityState in Code

```csharp
// Check state of a specific entity
var state = _context.Entry(user).State;

// Check all tracked entities
foreach (var entry in _context.ChangeTracker.Entries())
{
    Console.WriteLine($"{entry.Entity.GetType().Name}: {entry.State}");
}

// Check all tracked entities of a specific type
foreach (var entry in _context.ChangeTracker.Entries<User>())
{
    Console.WriteLine($"User {entry.Entity.Id}: {entry.State}");
}
```

---

## 6️⃣ Manually Setting EntityState

```csharp
// Force an entity to Modified (useful for disconnected scenarios)
_context.Entry(user).State = EntityState.Modified;
await _context.SaveChangesAsync();
// Generates UPDATE for ALL columns (not just changed ones)

// Detach an entity — stop tracking it
_context.Entry(user).State = EntityState.Detached;
// EF Core no longer watches this entity

// Mark specific properties as modified
_context.Entry(user).Property(u => u.FullName).IsModified = true;
// Only FullName is included in UPDATE — other columns ignored
```

---

## 7️⃣ AppDbContext SaveChangesAsync Override

Our `AppDbContext` uses Change Tracking to auto-set `UpdatedAt`:

```csharp
public override async Task<int> SaveChangesAsync(
    CancellationToken cancellationToken = default)
{
    // ChangeTracker.Entries<BaseEntity>() returns all tracked BaseEntity instances
    foreach (var entry in ChangeTracker.Entries<BaseEntity>())
    {
        if (entry.State == EntityState.Modified)
        {
            // EF Core told us this entity was modified
            // → auto-set UpdatedAt before saving
            entry.Entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    return await base.SaveChangesAsync(cancellationToken);
}
```

---

## 8️⃣ EntityState Demo Response

`GET /api/users/1/entity-state-demo` returns:

```json
{
  "userId": 1,
  "fullName": "Alice Johnson",
  "stateAfterLoad":   "Unchanged",
  "stateAfterChange": "Modified",
  "stateAfterDetach": "Detached",
  "stateAfterAdd":    "Added",
  "stateAfterRemove": "Deleted",
  "explanation": "EntityState shows what EF Core will do at SaveChanges: Added=INSERT, Modified=UPDATE, Deleted=DELETE, Unchanged=nothing, Detached=not tracked"
}
```

---

## 🌐 Endpoints Added in This Step

| Method | Endpoint | Demonstrates |
|--------|----------|-------------|
| `GET` | `/api/users/no-tracking` | `AsNoTracking` — faster read |
| `GET` | `/api/users/{id}/entity-state-demo` | All 5 `EntityState` values |

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Use `AsNoTracking` for all GET / read-only queries | Faster, less memory |
| Use regular tracking when you need to update or delete | Change detection needed |
| Never call `SaveChanges` with `AsNoTracking` entities | Changes won't be detected |
| `AsNoTrackingWithIdentityResolution` for queries with related data | Avoids duplicate objects |
| Detach entities you don't want to accidentally save | Prevents unintended DB changes |
| Only changed columns are included in UPDATE | EF Core is smart — not all columns |

---

## 🚀 How to Run

```bash
# No new migrations — no model changes
dotnet build
dotnet run

# Test endpoints
GET /api/users/no-tracking
GET /api/users/1/entity-state-demo
```

---

## ✅ Folder Structure After Step 8

```
TaskManagerAPI/
├── Controllers/
│   └── UsersController.cs              ← Updated (2 new endpoints)
├── DTOs/
│   └── User/
│       └── EntityStateDemo.cs          ← NEW
├── Repositories/
│   ├── Interfaces/
│   │   ├── IRepository.cs              ← Updated (AsNoTracking methods)
│   │   └── IUserRepository.cs          ← Updated (GetEntityState, DetachEntity)
│   └── Implementations/
│       ├── Repository.cs               ← Updated (AsNoTracking implementations)
│       └── UserRepository.cs           ← Updated (GetEntityState, DetachEntity)
├── Services/
│   ├── Interfaces/
│   │   └── IUserService.cs             ← Updated (2 new methods)
│   └── Implementations/
│       └── UserService.cs              ← Updated (NoTracking + EntityState demo)
```

---

## ✅ What's Next — Step 09: Performance Optimization
In the next step we will:
- **Indexes** — configure indexes for faster queries
- **Split Queries** — fix cartesian explosion with multiple Includes
- **Compiled Queries** — pre-compile frequently used queries
- **AsNoTracking** in read-only repository methods (applied project-wide)
