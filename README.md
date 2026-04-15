# TaskManagerAPI — Step 13: Data Seeding

## 📌 What This Step Covers
- HasData — seed initial data inside entity configurations
- Seed data applied automatically via migrations
- How EF Core tracks changes to seeded data
- Rules for writing good seed data
- What gets generated in migration Up() and Down()

---

## 🧠 What is Data Seeding?

Data Seeding pre-populates your database with initial data when migrations run.

```
Without Seeding:
  dotnet ef database update
  → Empty database — no labels, no default users ❌
  → Every developer must manually insert data ❌
  → Inconsistent data across environments ❌

With Seeding (HasData):
  dotnet ef database update
  → Labels inserted automatically ✅
  → Default admin user inserted automatically ✅
  → Every environment gets identical data ✅
```

### When to use seeding
```
✅ Static reference data  — Labels, Categories, Roles
✅ Default admin user     — System needs at least one admin
✅ Lookup tables          — Status codes, priority levels
✅ Demo/test data         — Development environment defaults

❌ User-generated content  — Posts, tasks, comments
❌ Sensitive data          — Passwords, tokens, keys
❌ Large datasets          — Use scripts instead
❌ Environment-specific    — Use conditional seeding instead
```

---

## 1️⃣ HasData

`HasData` is called inside `IEntityTypeConfiguration.Configure()` and tells EF Core to insert these rows when the migration runs.

### Basic syntax
```csharp
// In LabelConfiguration.cs
builder.HasData(
    new Label
    {
        Id        = 1,              // ← REQUIRED — hardcoded Id
        Name      = "Bug",
        Color     = "#FF0000",
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)  // ← hardcoded
    },
    new Label
    {
        Id        = 2,
        Name      = "Feature",
        Color     = "#00FF00",
        CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    }
    // ... add as many as you need
);
```

### Why hardcode `Id`?
```
EF Core uses the Id to TRACK seeded rows between migrations.

Without hardcoded Id:
  Migration 1: inserts Label { Name="Bug" }  → DB assigns Id = 1
  Migration 2: EF Core doesn't know it's the same row
  → Tries to INSERT again = duplicate row ❌

With hardcoded Id:
  Migration 1: INSERT Label { Id=1, Name="Bug" }  ✅
  Migration 2: EF Core knows Id=1 = the Bug label
  → Compares current seed vs snapshot → generates UPDATE if changed ✅
```

### Why hardcode `CreatedAt`?
```
Without hardcoded CreatedAt:
  Every time you run `dotnet ef migrations add`:
  EF Core sees CreatedAt = DateTime.UtcNow (different each time!)
  → Generates a new UPDATE migration for EVERY entity EVERY time ❌

With hardcoded CreatedAt:
  CreatedAt = new DateTime(2024, 1, 1, ...)  ← never changes
  → EF Core sees no change → no spurious migration ✅
```

---

## 2️⃣ What Gets Generated in the Migration

### Up() — INSERT statements
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.InsertData(
        table: "Labels",
        columns: new[] { "Id", "Name", "Color", "CreatedAt" },
        values: new object[,]
        {
            { 1, "Bug",           "#FF0000", new DateTime(2024,1,1,...) },
            { 2, "Feature",       "#00FF00", new DateTime(2024,1,1,...) },
            { 3, "Improvement",   "#0000FF", new DateTime(2024,1,1,...) },
            { 4, "Documentation", "#FFA500", new DateTime(2024,1,1,...) },
            { 5, "Testing",       "#800080", new DateTime(2024,1,1,...) },
            { 6, "Critical",      "#FF4500", new DateTime(2024,1,1,...) }
        });

    migrationBuilder.InsertData(
        table: "Users",
        columns: new[] { "Id", "FullName", "Email", "Role", "IsActive", "IsDeleted", "CreatedAt" },
        values: new object[,]
        {
            { 1, "System Admin", "admin@taskmanager.com", "ADMIN",  true, false, new DateTime(2024,1,1,...) },
            { 2, "Demo User",    "demo@taskmanager.com",  "MEMBER", true, false, new DateTime(2024,1,1,...) }
        });
}
```

### Down() — DELETE statements
```csharp
protected override void Down(MigrationBuilder migrationBuilder)
{
    // EF Core generates DELETE for every seeded row in Down()
    migrationBuilder.DeleteData(table: "Labels", keyColumn: "Id", keyValue: 1);
    migrationBuilder.DeleteData(table: "Labels", keyColumn: "Id", keyValue: 2);
    migrationBuilder.DeleteData(table: "Labels", keyColumn: "Id", keyValue: 3);
    migrationBuilder.DeleteData(table: "Labels", keyColumn: "Id", keyValue: 4);
    migrationBuilder.DeleteData(table: "Labels", keyColumn: "Id", keyValue: 5);
    migrationBuilder.DeleteData(table: "Labels", keyColumn: "Id", keyValue: 6);
    migrationBuilder.DeleteData(table: "Users",  keyColumn: "Id", keyValue: 1);
    migrationBuilder.DeleteData(table: "Users",  keyColumn: "Id", keyValue: 2);
}
```

---

## 3️⃣ How EF Core Tracks Changes to Seeded Data

EF Core stores the current seed data in `AppDbContextModelSnapshot.cs`.
On the next `migrations add`, it **diffs** the new seed vs the snapshot.

### Change a seeded value → UPDATE migration
```csharp
// Before:
new Label { Id = 1, Name = "Bug", Color = "#FF0000" }

// After (you changed the color):
new Label { Id = 1, Name = "Bug", Color = "#CC0000" }
```

EF Core generates:
```csharp
migrationBuilder.UpdateData(
    table: "Labels",
    keyColumn: "Id",
    keyValue: 1,
    column: "Color",
    value: "#CC0000");       // ← only the changed column ✅
```

### Add a new seeded row → INSERT migration
```csharp
// Added new label:
new Label { Id = 7, Name = "Security", Color = "#000080" }
```

EF Core generates:
```csharp
migrationBuilder.InsertData(
    table: "Labels",
    columns: new[] { "Id", "Name", "Color", "CreatedAt" },
    values: new object[] { 7, "Security", "#000080", ... });
```

### Remove a seeded row → DELETE migration
```csharp
// Removed Label with Id = 6 (Critical)
```

EF Core generates:
```csharp
migrationBuilder.DeleteData(
    table: "Labels",
    keyColumn: "Id",
    keyValue: 6);
```

---

## 4️⃣ Seeded Data in This Step

### Labels (reference data)

| Id | Name | Color | Purpose |
|----|------|-------|---------|
| 1 | Bug | `#FF0000` | Red — marks bugs |
| 2 | Feature | `#00FF00` | Green — new features |
| 3 | Improvement | `#0000FF` | Blue — enhancements |
| 4 | Documentation | `#FFA500` | Orange — docs |
| 5 | Testing | `#800080` | Purple — test tasks |
| 6 | Critical | `#FF4500` | OrangeRed — urgent issues |

### Users (default accounts)

| Id | FullName | Email | Role |
|----|----------|-------|------|
| 1 | System Admin | admin@taskmanager.com | ADMIN |
| 2 | Demo User | demo@taskmanager.com | MEMBER |

---

## 5️⃣ Limitations of HasData

```
❌ Cannot seed shadow properties (CreatedBy, LastLoginAt)
   → Use Interceptors or manual seeding instead

❌ Cannot use DateTime.UtcNow — always hardcode dates
   → Causes spurious migrations on every add

❌ RowVersion cannot be seeded
   → SQL Server auto-manages it

❌ Navigation properties cannot be set in HasData
   → Seed FK values only (e.g. UserId = 1, not User = someUser)

❌ Not suitable for large datasets (1000+ rows)
   → Use SQL scripts or a dedicated seeder class instead
```

---

## 6️⃣ HasData vs Other Seeding Approaches

| Approach | How | When to use |
|----------|-----|-------------|
| `HasData` | Inside `IEntityTypeConfiguration` | Small, static reference data ✅ |
| Migration `Sql()` | Raw SQL in migration `Up()` | Complex data with relationships |
| `DbContext.Database.EnsureCreated` | One-time seed on startup | Dev/test only |
| Dedicated Seeder class | Called from `Program.cs` | Large datasets, conditional seeding |

### Migration `Sql()` approach (alternative)
```csharp
// In migration Up() — for complex seeding
migrationBuilder.Sql(@"
    IF NOT EXISTS (SELECT 1 FROM Labels WHERE Id = 1)
    BEGIN
        INSERT INTO Labels (Id, Name, Color, CreatedAt)
        VALUES (1, 'Bug', '#FF0000', '2024-01-01')
    END
");
```

---

## 🔄 Full Seeding Flow

```
1. You add HasData() to LabelConfiguration + UserConfiguration

2. dotnet ef migrations add SeedInitialData
   │
   EF Core reads current seed data from configuration
   EF Core compares with AppDbContextModelSnapshot (empty for seed)
   EF Core generates: InsertData() for all seeded rows
   │
   Creates: XXXXXX_SeedInitialData.cs

3. dotnet ef database update
   │
   EF Core reads migration file
   Runs Up(): INSERT INTO Labels ..., INSERT INTO Users ...
   Records migration in __EFMigrationsHistory
   │
   Database: Labels table has 6 rows, Users table has 2 rows ✅

4. Later: you change a seeded value
   dotnet ef migrations add UpdateSeedData
   │
   EF Core diffs new seed vs snapshot
   Generates: UpdateData() for changed columns only
   │
   dotnet ef database update
   Runs UPDATE on only the changed rows ✅
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always hardcode `Id` in seed data | EF Core uses it to track seed rows across migrations |
| Always hardcode `CreatedAt` | Prevents spurious UPDATE migrations |
| Only seed static reference data | Don't seed user-generated content |
| Never seed passwords or secrets | Use environment variables / secret managers |
| Seed FK values not navigation properties | `HasData` doesn't support object references |
| Never modify seed data Id | EF Core will DELETE old + INSERT new instead of UPDATE |

---

## 🚀 How to Run

```bash
# Create seed migration
dotnet ef migrations add SeedInitialData --output-dir Data/Migrations

# Apply — INSERTs seeded rows
dotnet ef database update

# Verify
dotnet run
# GET /api/users  → returns System Admin + Demo User ✅
```

---

## ✅ Folder Structure After Step 13

```
TaskManagerAPI/
├── Data/
│   ├── Configurations/
│   │   ├── UserConfiguration.cs        ← Updated (HasData with 2 users)
│   │   └── LabelConfiguration.cs      ← Updated (HasData with 6 labels)
│   └── Migrations/
│       └── XXXXXX_SeedInitialData.cs  ← NEW (InsertData for users + labels)
```

---

## ✅ What's Next — Step 14: Architecture & Best Practices
In the next step we will:
- **AutoMapper** — replace manual mapping with AutoMapper profiles
- **DI Extensions** — final cleanup and organization
- **Final wiring** — ensure all layers are connected correctly
- **Review** — walk through the complete architecture end-to-end
