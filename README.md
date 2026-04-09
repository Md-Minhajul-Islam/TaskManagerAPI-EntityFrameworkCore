# TaskManagerAPI — Step 02: Migrations

## 📌 What This Step Covers
- What is a Migration
- Creating migrations
- Updating the database
- Understanding Up() and Down() methods
- Removing migrations
- Migration CLI commands cheat sheet

---

## 🧠 What is a Migration?

When you change your C# models, the database doesn't automatically know about it.
A **Migration** is EF Core's way of keeping the database **in sync** with your models.

```
Your C# Model changed
        │
        ▼
dotnet ef migrations add    ← EF Core reads your models and generates SQL instructions
        │
        ▼
dotnet ef database update   ← EF Core runs those SQL instructions on the real database
        │
        ▼
   DB is in sync ✅
```

Think of a migration as a **versioned change script** for your database — just like git commits are versioned changes for your code.

---

## 🗂️ What Gets Generated

Running `dotnet ef migrations add InitialCreate` creates:

```
Data/Migrations/
├── 20240101000000_InitialCreate.cs            ← YOUR migration (Up + Down)
├── 20240101000000_InitialCreate.Designer.cs   ← EF Core metadata (never touch this)
└── AppDbContextModelSnapshot.cs               ← Current DB state snapshot (never touch this)
```

| File | Purpose |
|------|---------|
| `XXXXXX_InitialCreate.cs` | Contains `Up()` and `Down()` methods you can read |
| `XXXXXX_InitialCreate.Designer.cs` | EF Core internal metadata — auto-generated, don't edit |
| `AppDbContextModelSnapshot.cs` | Snapshot of the current model state — EF Core uses this to generate the next migration diff |

---

## 🔼 Up() and Down() — The Heart of Every Migration

Every migration file has exactly two methods:

```csharp
public partial class InitialCreate : Migration
{
    // ✅ Up() → APPLY the migration (runs on: dotnet ef database update)
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id        = table.Column<int>().Annotation("SqlServer:Identity", "1, 1"),
                FullName  = table.Column<string>(nullable: false),
                Email     = table.Column<string>(nullable: false),
                Role      = table.Column<string>(nullable: false),
                IsActive  = table.Column<bool>(nullable: false),
                CreatedAt = table.Column<DateTime>(nullable: false),
                UpdatedAt = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });
    }

    // ↩️ Down() → UNDO the migration (runs on: dotnet ef database update 0)
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Users");
    }
}
```

| Method | Triggered by | What it does |
|--------|-------------|--------------|
| `Up()` | `dotnet ef database update` | Applies changes — creates tables, adds columns etc. |
| `Down()` | `dotnet ef database update 0` | Undoes changes — drops tables, removes columns etc. |

> 💡 `Up()` and `Down()` are always exact opposites of each other.
> EF Core generates both automatically from your model changes.

---

## ⚡ CLI Commands

### Create a migration
```bash
dotnet ef migrations add <MigrationName> --output-dir Data/Migrations
```
- Reads your current models
- Compares with `AppDbContextModelSnapshot.cs`
- Generates a new migration file with the diff

### Apply migrations to database
```bash
dotnet ef database update
```
- Runs all pending `Up()` methods
- Creates the database if it doesn't exist
- Records applied migrations in `__EFMigrationsHistory` table

### List all migrations
```bash
dotnet ef migrations list
```
Output:
```
20240101000000_InitialCreate  (Applied ✅)
20240102000000_AddProjectTable (Pending ⏳)
```

### Revert to a specific migration
```bash
# Revert everything (runs all Down() methods)
dotnet ef database update 0

# Revert to a specific migration
dotnet ef database update InitialCreate
```

### Remove the last migration
```bash
# ⚠️ Only works if the migration has NOT been applied to the DB yet
dotnet ef migrations remove
```

---

## 🔄 Full Migration Workflow

```
1. Change your Model  (add a property, new class etc.)
         │
         ▼
2. dotnet ef migrations add <Name>
         │
         EF Core compares your models with the last snapshot
         Generates Up() → what to do
         Generates Down() → how to undo it
         │
         ▼
3. Review the generated migration file  ← always do this!
         │
         ▼
4. dotnet ef database update
         │
         Runs Up() on the database
         Records migration in __EFMigrationsHistory table
         │
         ▼
5. ✅ Database is in sync with your Models
```

---

## ↩️ How to Safely Remove a Migration

```
Did you already run database update?
         │
    YES  │  NO
    ▼         ▼
dotnet ef     dotnet ef
database      migrations
update 0      remove ✅
    │
    ▼
dotnet ef
migrations
remove ✅
```

> ⚠️ Never manually delete migration files. Always use `dotnet ef migrations remove`.
> Manual deletion breaks the snapshot and corrupts future migrations.

---

## 🗃️ __EFMigrationsHistory Table

When you run `database update`, EF Core creates a special table in your database:

```sql
SELECT * FROM __EFMigrationsHistory
```

| MigrationId | ProductVersion |
|-------------|---------------|
| 20240101000000_InitialCreate | 8.0.0 |

EF Core uses this table to know which migrations have already been applied so it never runs the same migration twice.

---

## 🔁 Full Example Flow (This Step)

```
1. dotnet ef migrations add InitialCreate --output-dir Data/Migrations
   └─ EF Core reads User model + BaseEntity
   └─ Generates Up() → CREATE TABLE Users (...)
   └─ Generates Down() → DROP TABLE Users

2. dotnet ef database update
   └─ Connects to SQL Server using connection string
   └─ Creates TaskManagerDB database
   └─ Runs Up() → creates Users table
   └─ Inserts record into __EFMigrationsHistory

3. dotnet run → open Swagger
   └─ POST /api/users → INSERT INTO Users ✅
   └─ GET  /api/users → SELECT * FROM Users ✅
   └─ PUT  /api/users/1 → UPDATE Users SET ... ✅
   └─ DELETE /api/users/1 → DELETE FROM Users ✅
```

---

## 🧪 Endpoints Tested in This Step

| Method | Endpoint | Expected Result |
|--------|----------|----------------|
| `POST` | `/api/users` | `201 Created` — user saved to DB |
| `GET` | `/api/users` | `200 OK` — list of all users |
| `GET` | `/api/users/1` | `200 OK` — single user |
| `PUT` | `/api/users/1` | `200 OK` — user updated |
| `POST` | `/api/users` (duplicate email) | `409 Conflict` — business rule enforced |
| `DELETE` | `/api/users/1` | `204 No Content` — user deleted |
| `GET` | `/api/users/1` (after delete) | `404 Not Found` |

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always review generated migration files | EF Core might generate unexpected changes |
| Never edit `Designer.cs` or `Snapshot.cs` | These are auto-managed by EF Core |
| Never manually delete migration files | Always use `dotnet ef migrations remove` |
| Revert DB before removing an applied migration | Otherwise EF Core and DB go out of sync |
| Name migrations descriptively | `AddProjectTable` not `Migration2` |
| One concern per migration | Don't mix unrelated changes in one migration |

---

## 🚀 How to Run

```bash
# 1. Install EF Core CLI (once per machine)
dotnet tool install --global dotnet-ef

# 2. Create migration
dotnet ef migrations add InitialCreate --output-dir Data/Migrations

# 3. Apply to database
dotnet ef database update

# 4. Run the app
dotnet run

# 5. Open Swagger
# https://localhost:{port}/swagger
```

---

## ✅ What's Next — Step 03: Entity Configuration
In the next step we will:
- Use **Data Annotations** to configure columns directly on models
- Use **Fluent API** for more advanced configuration in `OnModelCreating`
- Use **IEntityTypeConfiguration** to keep configuration organized per entity
- Configure table names, column types, max lengths, required fields, indexes and more
