# TaskManagerAPI — Step 11: Raw SQL

## 📌 What This Step Covers
- FromSqlRaw — execute raw SELECT SQL that returns entities
- ExecuteSqlRaw — execute raw UPDATE/DELETE/INSERT SQL
- Stored Procedures — create and call via EF Core
- When to use raw SQL vs LINQ
- SQL injection protection with parameterized queries

---

## 🧠 Why Raw SQL?

EF Core's LINQ is powerful but sometimes you need raw SQL:

```
Use LINQ when:
  ✅ Standard CRUD operations
  ✅ Simple filtering and sorting
  ✅ Readable, maintainable code

Use Raw SQL when:
  ✅ Complex queries LINQ can't express well
  ✅ Performance-critical queries needing specific SQL hints
  ✅ Calling existing stored procedures
  ✅ Bulk UPDATE/DELETE without loading entities
  ✅ Database-specific features (window functions, CTEs etc.)
```

---

## 1️⃣ FromSqlRaw

`FromSqlRaw` executes a raw SQL `SELECT` and maps results to **tracked entities**.

### Basic usage
```csharp
// Simple raw SQL
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)

// {0} = SQL parameter — EF Core replaces it safely
// SQL Server receives: WHERE Role = @p0
// NEVER use string interpolation: $"WHERE Role = '{role}'"  ← SQL INJECTION ❌
```

### Important rules for `FromSqlRaw`
```csharp
// ✅ CORRECT — parameterized (safe)
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)

// ❌ WRONG — string concatenation (SQL injection risk!)
_dbSet.FromSqlRaw($"SELECT * FROM Users WHERE Role = '{role}'")

// ✅ The SQL must SELECT all columns the entity needs
// EF Core maps result to User entity — all columns must be present
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)
//                  ↑ SELECT * ensures all User properties are mapped

// ✅ You can chain LINQ after FromSqlRaw
_dbSet
    .FromSqlRaw("SELECT * FROM Users WHERE IsActive = 1")
    .Where(u => u.Role == "Admin")   // ← LINQ applied on top of raw SQL
    .OrderBy(u => u.FullName)
    .ToListAsync()
```

### With `AsNoTracking`
```csharp
// Read-only raw SQL query — no tracking overhead
_dbSet
    .FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)
    .AsNoTracking()
    .ToListAsync()
```

---

## 2️⃣ ExecuteSqlRaw

`ExecuteSqlRaw` executes raw SQL that **does NOT return entities**.
Used for `UPDATE`, `DELETE`, `INSERT` operations.
Returns the number of rows affected.

### Basic usage
```csharp
// UPDATE — deactivate a single user
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = {0}",
    userId);
// Returns: 1 if updated, 0 if not found

// UPDATE — bulk update all users with a role
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Role = {0}",
    role);
// Returns: number of rows updated
```

### Why use `ExecuteSqlRaw` for bulk updates?
```csharp
// Without ExecuteSqlRaw — loads ALL entities then updates one by one
var users = await _context.Users
    .Where(u => u.Role == "Member")
    .ToListAsync();           // ← loads 1000 users into memory ❌

foreach (var user in users)
    user.IsActive = false;    // ← 1000 individual UPDATEs ❌

await _context.SaveChangesAsync();

// With ExecuteSqlRaw — ONE SQL statement, nothing loaded into memory
await _context.Database.ExecuteSqlRawAsync(
    "UPDATE Users SET IsActive = 0 WHERE Role = {0}", "Member");
// ← ONE UPDATE statement, zero entities in memory ✅
```

---

## 3️⃣ Stored Procedures

Stored Procedures are pre-compiled SQL scripts stored in the database.

### Two types of stored procedures

| Type | Returns | Call with |
|------|---------|-----------|
| Returns rows | `IEnumerable<User>` | `FromSqlRaw("EXEC sp_Name {0}", param)` |
| No rows returned | `int` rows affected | `ExecuteSqlRaw("EXEC sp_Name {0}, {1}", p1, p2)` |

### Our stored procedures

#### `sp_GetActiveUsersByRole` — returns rows
```sql
CREATE PROCEDURE sp_GetActiveUsersByRole
    @Role NVARCHAR(20)
AS
BEGIN
    SELECT *
    FROM   Users
    WHERE  Role     = @Role
      AND  IsActive = 1
    ORDER  BY FullName;
END
```

Called via `FromSqlRaw`:
```csharp
await _dbSet
    .FromSqlRaw("EXEC sp_GetActiveUsersByRole {0}", role)
    .ToListAsync();
// EF Core maps each result row to a User entity ✅
```

#### `sp_UpdateUserRole` — no rows returned
```sql
CREATE PROCEDURE sp_UpdateUserRole
    @UserId  INT,
    @NewRole NVARCHAR(20)
AS
BEGIN
    UPDATE Users
    SET    Role      = @NewRole,
           UpdatedAt = GETUTCDATE()
    WHERE  Id = @UserId;
END
```

Called via `ExecuteSqlRaw`:
```csharp
await _context.Database.ExecuteSqlRawAsync(
    "EXEC sp_UpdateUserRole {0}, {1}",
    userId, newRole);
// Returns: 1 if updated, 0 if not found
```

### Creating stored procedures via migrations
```csharp
// In migration Up() — use migrationBuilder.Sql()
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        CREATE PROCEDURE sp_GetActiveUsersByRole
            @Role NVARCHAR(20)
        AS
        BEGIN
            SELECT * FROM Users
            WHERE Role = @Role AND IsActive = 1
            ORDER BY FullName;
        END
    ");
}

// In migration Down() — drop the procedure
protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetActiveUsersByRole");
}
```

> 💡 Always create stored procedures via migrations so they are version-controlled
> and applied consistently across all environments.

---

## 4️⃣ FromSqlRaw vs ExecuteSqlRaw — Side by Side

| | `FromSqlRaw` | `ExecuteSqlRaw` |
|--|-------------|----------------|
| Returns | `IQueryable<T>` — tracked entities | `int` — rows affected |
| Used for | SELECT queries | UPDATE / DELETE / INSERT |
| Tracking | Yes (use AsNoTracking to disable) | N/A — no entities returned |
| LINQ chainable | ✅ Yes | ❌ No |
| Called on | `_dbSet` | `_context.Database` |
| Stored Procs | ✅ That return rows | ✅ That don't return rows |

---

## 5️⃣ SQL Injection Protection

Always use parameterized queries — NEVER string concatenation or interpolation.

```csharp
// ❌ DANGEROUS — SQL injection vulnerability
var role = "Admin' OR '1'='1";  // attacker input
_dbSet.FromSqlRaw($"SELECT * FROM Users WHERE Role = '{role}'");
// Generates: WHERE Role = 'Admin' OR '1'='1'
// Returns ALL users regardless of role ❌

// ✅ SAFE — parameterized query
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role);
// EF Core sends: WHERE Role = @p0  with value 'Admin'' OR ''1''=''1'
// SQL Server treats the entire value as a string literal ✅
// Returns: zero results (no role named that) ✅
```

### Three safe ways to parameterize
```csharp
// Option 1 — positional {0}, {1}, {2}
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", role)

// Option 2 — SqlParameter objects
var param = new SqlParameter("@role", role);
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = @role", param)

// Option 3 — FormattableString (FromSqlInterpolated)
_dbSet.FromSqlInterpolated($"SELECT * FROM Users WHERE Role = {role}")
// EF Core automatically parameterizes the interpolated values ✅
```

---

## 6️⃣ Full Layer Flow

### `GET /api/users/raw-sql/by-role/Admin`

```
UsersController.GetByRoleRawSql("Admin")
      │
      ▼
UserService.GetByRoleRawSqlAsync("Admin")
      │
      ▼
UserRepository.GetByRoleRawSqlAsync("Admin")
      │
      ▼
_dbSet.FromSqlRaw("SELECT * FROM Users WHERE Role = {0}", "Admin")
      │
SQL:  SELECT * FROM Users WHERE Role = @p0   (@p0 = 'Admin')
      │
      ▼
EF Core maps rows → List<User>
      │
      ▼
UserService maps → List<UserResponseDto>
      │
      ▼
Controller returns 200 OK
```

### `PATCH /api/users/raw-sql/bulk-deactivate/Member`

```
UsersController.BulkDeactivateRawSql("Member")
      │
      ▼
UserService.BulkDeactivateByRoleAsync("Member")
      │
      ▼
UserRepository.BulkDeactivateByRoleAsync("Member")
      │
      ▼
_context.Database.ExecuteSqlRawAsync(
    "UPDATE Users SET IsActive = 0 WHERE Role = {0}", "Member")
      │
SQL:  UPDATE Users SET IsActive = 0, UpdatedAt = GETUTCDATE()
      WHERE Role = @p0   (@p0 = 'Member')
      │
      ▼
Returns: 3 (rows affected)
      │
      ▼
Controller returns 200 OK with RawSqlDemo { RowsAffected = 3 }
```

---

## 🌐 Endpoints Added in This Step

| Method | Endpoint | Method Used | Demonstrates |
|--------|----------|-------------|-------------|
| `GET` | `/api/users/raw-sql/by-role/{role}` | `FromSqlRaw` | Raw SELECT |
| `GET` | `/api/users/raw-sql/by-email?email=` | `FromSqlRaw` | Raw SELECT with param |
| `PATCH` | `/api/users/raw-sql/deactivate/{id}` | `ExecuteSqlRaw` | Raw UPDATE single row |
| `PATCH` | `/api/users/raw-sql/bulk-deactivate/{role}` | `ExecuteSqlRaw` | Raw bulk UPDATE |
| `GET` | `/api/users/sp/active-by-role/{role}` | `FromSqlRaw` + SP | Stored Proc returning rows |
| `PATCH` | `/api/users/sp/update-role/{id}?newRole=` | `ExecuteSqlRaw` + SP | Stored Proc no rows |

---

## 🧪 Test Endpoints

| Endpoint | Expected Result |
|----------|----------------|
| `GET /api/users/raw-sql/by-role/Admin` | All Admin users |
| `GET /api/users/raw-sql/by-role/Member` | All Member users |
| `GET /api/users/raw-sql/by-email?email=alice@app.com` | Alice's user object |
| `PATCH /api/users/raw-sql/deactivate/1` | `rowsAffected: 1` |
| `PATCH /api/users/raw-sql/bulk-deactivate/Member` | `rowsAffected: N` |
| `GET /api/users/sp/active-by-role/Admin` | Active admins via SP |
| `PATCH /api/users/sp/update-role/1?newRole=Admin` | Role updated via SP |

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always use `{0}` parameters, never string interpolation | SQL injection prevention |
| `FromSqlRaw` must SELECT all entity columns | EF Core needs to map all properties |
| `ExecuteSqlRaw` called on `_context.Database` not `_dbSet` | It doesn't return entities |
| Create stored procedures in migrations | Version controlled, consistent across environments |
| Use `DROP PROCEDURE IF EXISTS` in `Down()` | Safe rollback of SP migration |
| Prefer LINQ over raw SQL when possible | LINQ is database-agnostic and safer |
| Use raw SQL for bulk operations | Far more efficient than loading entities |

---

## 🚀 How to Run

```bash
# Create migration for stored procedures
dotnet ef migrations add AddStoredProcedures --output-dir Data/Migrations

# Edit the migration file — add SP creation in Up() and drop in Down()

# Apply
dotnet ef database update

# Run
dotnet run

# Test all endpoints in Swagger
```

---

## ✅ Folder Structure After Step 11

```
TaskManagerAPI/
├── Controllers/
│   └── UsersController.cs                  ← Updated (6 new endpoints)
├── DTOs/
│   └── User/
│       └── RawSqlDemo.cs                   ← NEW
├── Data/
│   └── Migrations/
│       └── XXXXXX_AddStoredProcedures.cs   ← NEW
├── Repositories/
│   ├── Interfaces/
│   │   └── IUserRepository.cs              ← Updated (raw SQL methods)
│   └── Implementations/
│       └── UserRepository.cs               ← Updated (raw SQL implementations)
└── Services/
    ├── Interfaces/
    │   └── IUserService.cs                 ← Updated (raw SQL methods)
    └── Implementations/
        └── UserService.cs                  ← Updated (raw SQL implementations)
```

---

## ✅ What's Next — Step 12: Advanced Features
In the next step we will:
- **Global Query Filters** — automatically filter all queries (e.g. soft delete)
- **Soft Delete** — mark records as deleted instead of removing them
- **Concurrency Handling** — `RowVersion` to prevent conflicting updates
- **Shadow Properties** — DB columns with no C# property
- **Value Converters** — transform values between C# and DB
- **Interceptors** — hook into EF Core operations
