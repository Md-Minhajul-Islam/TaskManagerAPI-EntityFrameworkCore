# TaskManagerAPI — Step 10: Transactions

## 📌 What This Step Covers
- What is a Database Transaction
- BeginTransaction — starting an explicit transaction
- Commit — saving all changes permanently
- Rollback — undoing all changes on failure
- Savepoints — partial rollbacks within a transaction
- Transaction support in UnitOfWork

---

## 🧠 What is a Transaction?

A transaction groups multiple database operations into a **single atomic unit**.
Either ALL operations succeed together, or NONE of them are saved.

```
Without Transaction:
  Operation 1: INSERT User 1  ✅ saved
  Operation 2: INSERT User 2  ✅ saved
  Operation 3: INSERT User 3  ❌ fails
  Result: User 1 and 2 saved, User 3 missing = INCONSISTENT DATA ❌

With Transaction:
  Operation 1: INSERT User 1  ✅ staged
  Operation 2: INSERT User 2  ✅ staged
  Operation 3: INSERT User 3  ❌ fails → ROLLBACK
  Result: NOTHING saved = data stays consistent ✅
```

### Real-world analogy
A bank transfer:
```
BEGIN TRANSACTION
  Deduct $100 from Account A   ← staged
  Add    $100 to   Account B   ← staged
COMMIT                         ← both happen together ✅

If anything fails:
ROLLBACK                       ← neither happens ✅
```

---

## 1️⃣ Transaction Lifecycle

```
BeginTransactionAsync()
        │
        ▼ transaction is open — nothing committed yet
        │
        ├── SaveChangesAsync()    ← stages INSERT/UPDATE/DELETE
        ├── SaveChangesAsync()    ← stages more operations
        │
        ├── CommitAsync()         ← ALL staged changes go to DB permanently ✅
        │         OR
        └── RollbackAsync()       ← ALL staged changes are undone ❌
```

---

## 2️⃣ IUnitOfWork — Transaction Methods

```csharp
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }

    Task<int>                  SaveChangesAsync();

    // ── Transactions ───────────────────────────────────────────
    Task<IDbContextTransaction> BeginTransactionAsync();    // start
    Task                        CommitAsync(IDbContextTransaction transaction);   // save
    Task                        RollbackAsync(IDbContextTransaction transaction); // undo
}
```

---

## 3️⃣ UnitOfWork — Transaction Implementations

```csharp
// BeginTransaction — opens a transaction on the DB connection
public async Task<IDbContextTransaction> BeginTransactionAsync()
    => await _context.Database.BeginTransactionAsync();
// All SaveChangesAsync() calls after this are part of THIS transaction
// Changes are staged in the DB but not visible to other connections yet

// Commit — makes all staged changes permanent
public async Task CommitAsync(IDbContextTransaction transaction)
    => await transaction.CommitAsync();
// After commit: changes are visible to all DB connections ✅

// Rollback — undoes all staged changes
public async Task RollbackAsync(IDbContextTransaction transaction)
    => await transaction.RollbackAsync();
// After rollback: DB is exactly as it was before BeginTransaction ✅
```

---

## 4️⃣ Scenario 1 — Bulk Create (All or Nothing)

```csharp
await using var transaction = await _unitOfWork.BeginTransactionAsync();
try
{
    foreach (var dto in dtos)
    {
        // Validate — any failure triggers rollback
        var isUnique = await _unitOfWork.Users.IsEmailUniqueAsync(dto.Email);
        if (!isUnique)
            throw new InvalidOperationException($"Email '{dto.Email}' exists");

        var user = new User { ... };
        await _unitOfWork.Users.AddAsync(user);

        // SaveChangesAsync inside transaction:
        // Changes are staged in DB — NOT yet committed
        // Still reversible if something fails later
        await _unitOfWork.SaveChangesAsync();
    }

    // All users passed — commit everything
    await _unitOfWork.CommitAsync(transaction);     // ← permanent ✅
}
catch (Exception ex)
{
    // One user failed — undo ALL users
    await _unitOfWork.RollbackAsync(transaction);   // ← all undone ❌
}
```

### Test — Success (all unique emails)
```json
POST /api/users/bulk-create
[
  { "fullName": "Bulk User 1", "email": "bulk1@app.com", "role": "Member" },
  { "fullName": "Bulk User 2", "email": "bulk2@app.com", "role": "Member" },
  { "fullName": "Bulk User 3", "email": "bulk3@app.com", "role": "Admin"  }
]
```
Expected: `200 OK` — all 3 users created ✅

### Test — Rollback (duplicate email mid-way)
```json
POST /api/users/bulk-create
[
  { "fullName": "New User 1", "email": "newbulk1@app.com", "role": "Member" },
  { "fullName": "Duplicate",  "email": "bulk1@app.com",    "role": "Member" },
  { "fullName": "New User 3", "email": "newbulk3@app.com", "role": "Admin"  }
]
```
Expected: `409 Conflict` — zero users created (New User 1 rolled back too!) ❌

---

## 5️⃣ Scenario 2 — Intentional Rollback Demo

Shows that `SaveChangesAsync` inside a transaction does NOT permanently save:

```csharp
await using var transaction = await _unitOfWork.BeginTransactionAsync();
try
{
    // User 1 saved inside transaction
    await _unitOfWork.Users.AddAsync(user1);
    await _unitOfWork.SaveChangesAsync();
    // user1.Id is assigned BUT not visible outside this transaction yet

    // User 2 saved inside transaction
    await _unitOfWork.Users.AddAsync(user2);
    await _unitOfWork.SaveChangesAsync();

    // Simulate failure
    throw new InvalidOperationException("Simulated failure");
}
catch
{
    await _unitOfWork.RollbackAsync(transaction);
    // BOTH users are gone — even though SaveChangesAsync was called ✅
    // SaveChangesAsync inside a transaction ≠ permanently committed
}
```

> 💡 **Key insight** — `SaveChangesAsync()` inside a transaction only writes to
> the **transaction buffer**. It is NOT committed to the DB until `CommitAsync()` is called.
> Calling `RollbackAsync()` undoes everything in the buffer.

**Test:**
```
POST /api/users/transaction-rollback-demo  (no body needed)
```
Expected: `200 OK` with `success: false` — both users rolled back ✅

---

## 6️⃣ Scenario 3 — Savepoints (Partial Rollback)

Savepoints let you roll back **part of a transaction** without losing everything.

```csharp
await using var transaction = await _unitOfWork.BeginTransactionAsync();

// Create User 1 ✅
await _unitOfWork.Users.AddAsync(user1);
await _unitOfWork.SaveChangesAsync();

// Mark a savepoint AFTER user 1
await transaction.CreateSavepointAsync("AfterUser1");
// "Remember this point — I may want to come back here"

// Create User 2 — then something goes wrong
await _unitOfWork.Users.AddAsync(user2);
await _unitOfWork.SaveChangesAsync();

// Roll back ONLY to the savepoint — User 1 is KEPT, User 2 is GONE
await transaction.RollbackToSavepointAsync("AfterUser1");

// Commit — only User 1 is saved
await _unitOfWork.CommitAsync(transaction);  // ← User 1 committed ✅
                                              // User 2 never existed ✅
```

### Savepoint Timeline
```
BEGIN TRANSACTION
  ├── INSERT User 1                 ← staged ✅
  ├── SAVEPOINT "AfterUser1"        ← checkpoint created
  ├── INSERT User 2                 ← staged ✅
  ├── ROLLBACK TO "AfterUser1"      ← User 2 undone, User 1 kept ✅
  └── COMMIT                        ← only User 1 committed ✅
```

**Test:**
```
POST /api/users/savepoint-demo  (no body needed)
```
Expected: `200 OK` with `success: true` — only User 1 saved ✅

---

## 7️⃣ SaveChangesAsync Inside vs Outside Transaction

| | Outside Transaction | Inside Transaction |
|--|--------------------|--------------------|
| When committed | Immediately ✅ | Only after `CommitAsync()` |
| Can be undone | ❌ No | ✅ Yes — until `CommitAsync()` |
| Visible to others | Immediately | Only after `CommitAsync()` |
| Use when | Single operation | Multiple related operations |

---

## 8️⃣ `await using` — Why We Use It

```csharp
// await using — automatically disposes transaction when done
await using var transaction = await _unitOfWork.BeginTransactionAsync();

// If we forget to commit/rollback, disposal auto-rolls back ✅
// This prevents transactions from staying open indefinitely
```

> 💡 Always use `await using` with transactions. If an unhandled exception occurs
> and you forget to call `RollbackAsync`, the `await using` ensures the transaction
> is disposed (which triggers an automatic rollback).

---

## 🌐 Endpoints Added in This Step

| Method | Endpoint | Demonstrates |
|--------|----------|-------------|
| `POST` | `/api/users/bulk-create` | Atomic transaction — all or nothing |
| `POST` | `/api/users/transaction-rollback-demo` | Intentional rollback |
| `POST` | `/api/users/savepoint-demo` | Savepoint — partial rollback |

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always wrap multi-step operations in a transaction | Prevents partial data on failure |
| Use `await using` for transactions | Auto-rollback on disposal if not committed |
| `SaveChangesAsync` inside transaction ≠ committed | Must call `CommitAsync()` to persist |
| Always call `RollbackAsync` in catch block | Prevents transaction staying open |
| Use savepoints for complex multi-stage operations | Allows partial recovery without full rollback |
| Keep transactions short | Long transactions hold DB locks — hurts concurrency |

---

## 🚀 How to Run

```bash
# Create migration for new indexes
dotnet ef migrations add AddPerformanceIndexes --output-dir Data/Migrations

# Apply
dotnet ef database update

# Run
dotnet run

# Test in Swagger:
# 1. POST /api/users/bulk-create         (with JSON body)
# 2. POST /api/users/transaction-rollback-demo  (no body)
# 3. POST /api/users/savepoint-demo      (no body)
```

---

## ✅ Folder Structure After Step 10

```
TaskManagerAPI/
├── Controllers/
│   └── UsersController.cs       ← Updated (3 new endpoints)
├── DTOs/
│   └── User/
│       └── TransactionDemo.cs   ← NEW
├── Services/
│   ├── Interfaces/
│   │   └── IUserService.cs      ← Updated (3 transaction methods)
│   └── Implementations/
│       └── UserService.cs       ← Updated (transaction implementations)
└── UnitOfWork/
    ├── IUnitOfWork.cs           ← Updated (BeginTransaction, Commit, Rollback)
    └── UnitOfWork.cs            ← Updated (transaction implementations)
```

---

## ✅ What's Next — Step 11: Raw SQL
In the next step we will:
- **FromSqlRaw** — execute raw SQL that returns entities
- **ExecuteSqlRaw** — execute raw SQL for INSERT/UPDATE/DELETE
- **Stored Procedures** — call stored procedures via EF Core
- Add raw SQL methods to repositories
