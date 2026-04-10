# TaskManagerAPI — Step 04: Keys

## 📌 What This Step Covers
- Primary Key — single column identifier
- Composite Key — two columns as one primary key
- Alternate Key — secondary unique key, can be FK target
- Foreign Key — linking one table to another
- How all key types are configured via Fluent API
- How to fix a failed/partial migration

---

## 🔑 The 4 Key Types at a Glance

| Key Type | Configured with | Purpose | Our Example |
|----------|----------------|---------|-------------|
| **Primary Key** | `HasKey(x => x.Id)` | Uniquely identifies each row | `Users.Id` |
| **Composite Key** | `HasKey(x => new { x.A, x.B })` | Two columns together identify a row | `TeamMembers.TeamId + UserId` |
| **Alternate Key** | `HasAlternateKey(x => x.Email)` | Secondary unique key, can be FK target | `Users.Email` |
| **Foreign Key** | `HasForeignKey(x => x.UserId)` | Links one table to another | `UserProfiles.UserId → Users.Id` |

---

## 1️⃣ Primary Key

A Primary Key **uniquely identifies every row** in a table. Every entity must have one.

### Via Data Annotation
```csharp
public class User : BaseEntity
{
    [Key]                                                    // Primary Key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]    // Auto-increment
    public int Id { get; set; }
}
```

### Via Fluent API
```csharp
// In UserConfiguration.Configure()
builder.HasKey(u => u.Id);           // Declares Id as Primary Key

builder.Property(u => u.Id)
    .UseIdentityColumn();            // Auto-increment (1, 1)
```

### What SQL gets generated
```sql
CREATE TABLE Users (
    Id INT IDENTITY(1,1) NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id)
)
```

> 💡 EF Core convention: if your class has a property named `Id` or `<ClassName>Id`,
> EF Core automatically treats it as the Primary Key — no annotation needed.
> But being explicit is always better.

---

## 2️⃣ Composite Key

A Composite Key uses **two or more columns together** as the Primary Key.
No single column is unique on its own — only the combination is unique.

### When to use it
Junction/bridge tables that link two entities — like `TeamMember` which links `Team` and `User`.

```
TeamId = 1, UserId = 1  ✅ unique combination
TeamId = 1, UserId = 2  ✅ same TeamId, different UserId — allowed
TeamId = 2, UserId = 1  ✅ same UserId, different TeamId — allowed
TeamId = 1, UserId = 1  ❌ duplicate combination — rejected!
```

### Model — NO [Key] annotation (can't use annotation for composite keys)
```csharp
[Table("TeamMembers")]
public class TeamMember
{
    // Part 1 of Composite Key — no [Key] annotation here!
    public int TeamId { get; set; }

    // Part 2 of Composite Key — no [Key] annotation here!
    public int UserId { get; set; }

    [Required]
    [MaxLength(20)]
    public string Role { get; set; } = "Member";

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
```

### Fluent API — only way to configure composite keys
```csharp
// In TeamMemberConfiguration.Configure()
builder.HasKey(tm => new { tm.TeamId, tm.UserId });
//              ↑ anonymous object with both columns = composite key
```

### What SQL gets generated
```sql
CREATE TABLE TeamMembers (
    TeamId INT NOT NULL,
    UserId INT NOT NULL,
    CONSTRAINT PK_TeamMembers PRIMARY KEY (TeamId, UserId)
    --                                     ↑ both columns together
)
```

> ⚠️ Composite Keys **cannot** be configured with Data Annotations.
> You must use Fluent API: `HasKey(x => new { x.A, x.B })`.

---

## 3️⃣ Alternate Key

An Alternate Key is a **second unique key** on a table.

### Alternate Key vs Unique Index — what's the difference?

| | Unique Index | Alternate Key |
|--|-------------|---------------|
| Prevents duplicates | ✅ Yes | ✅ Yes |
| Can be referenced as FK target | ❌ No | ✅ Yes |
| Performance for queries | ✅ Better | ✅ Good |
| Configured with | `HasIndex().IsUnique()` | `HasAlternateKey()` |

### Fluent API
```csharp
// In UserConfiguration.Configure()
builder.HasAlternateKey(u => u.Email)
    .HasName("AK_Users_Email");
```

### What SQL gets generated
```sql
ALTER TABLE Users
    ADD CONSTRAINT AK_Users_Email UNIQUE (Email);
```

### ⚠️ Important — can't have both Unique Index AND Alternate Key on same column
If you had a Unique Index on `Email` from a previous migration, you must drop it first:

```csharp
// In migration Up()
migrationBuilder.DropIndex(
    name: "IX_Users_Email",           // Drop old unique index first
    table: "Users");

migrationBuilder.AddUniqueConstraint(
    name: "AK_Users_Email",           // Then add alternate key
    table: "Users",
    column: "Email");

// In migration Down()
migrationBuilder.DropUniqueConstraint(
    name: "AK_Users_Email",
    table: "Users");

migrationBuilder.CreateIndex(
    name: "IX_Users_Email",           // Restore old index
    table: "Users",
    column: "Email",
    unique: true);
```

---

## 4️⃣ Foreign Key

A Foreign Key is a column that **points to a Primary (or Alternate) Key in another table**.
It enforces referential integrity — you can't add a row with a FK value that doesn't exist in the parent table.

### Model
```csharp
[Table("UserProfiles")]
public class UserProfile
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Bio { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? AvatarUrl { get; set; }

    [MaxLength(255)]
    public string? GitHubUrl { get; set; }

    // This is the Foreign Key property
    // It must hold a valid Users.Id value
    public int UserId { get; set; }
}
```

### Fluent API
```csharp
// In UserProfileConfiguration.Configure()
builder.Property(up => up.UserId)
    .IsRequired()
    .HasColumnName("UserId");

// Unique index ensures one profile per user
builder.HasIndex(up => up.UserId)
    .IsUnique()
    .HasDatabaseName("IX_UserProfiles_UserId");
```

### What SQL gets generated
```sql
CREATE TABLE UserProfiles (
    Id     INT IDENTITY(1,1) NOT NULL,
    UserId INT NOT NULL,
    -- FK constraint is added in Step 05 when we configure relationships
    CONSTRAINT PK_UserProfiles PRIMARY KEY (Id)
)
```

> 💡 We configure the FK **column** here in Step 04.
> The full FK **constraint** (with `REFERENCES Users(Id)`) is configured
> in Step 05 when we set up Navigation Properties and Relationships.

---

## 🔧 How All Keys Look in the Migration

```csharp
// PRIMARY KEY — single column
table.PrimaryKey("PK_Users", x => x.Id);

// ALTERNATE KEY — unique constraint as a key
migrationBuilder.AddUniqueConstraint(
    name: "AK_Users_Email",
    table: "Users",
    column: "Email");

// COMPOSITE KEY — two columns together
table.PrimaryKey("PK_TeamMembers", x => new { x.TeamId, x.UserId });

// FOREIGN KEY column (constraint in Step 05)
UserId = table.Column<int>(nullable: false)
```

---

## 🆕 New Models in This Step

### `UserProfile` — One profile per user (One-to-One in Step 05)
```
UserProfiles table
├── Id          INT IDENTITY  PK
├── Bio         NVARCHAR(500) NOT NULL
├── AvatarUrl   NVARCHAR(255) NULL
├── GitHubUrl   NVARCHAR(255) NULL
└── UserId      INT           NOT NULL  ← FK → Users.Id
```

### `TeamMember` — Junction table (Many-to-Many in Step 05)
```
TeamMembers table
├── TeamId      INT           NOT NULL  ← Part 1 of Composite PK
├── UserId      INT           NOT NULL  ← Part 2 of Composite PK
├── Role        NVARCHAR(20)  NOT NULL
└── JoinedAt    DATETIME2     NOT NULL
```

---

## 🚑 Fixing a Failed Migration

### The Problem
If a migration fails halfway through, the database is in an **inconsistent state**:
- Some commands ran ✅
- Some commands didn't ❌
- Running `database update` again causes new errors

### The Fix — Always revert to last clean migration
```bash
# Step 1 — revert to the last successfully applied migration
dotnet ef database update <LastGoodMigrationName>
# e.g.
dotnet ef database update ConfigureUserEntity

# Step 2 — verify it reverted
dotnet ef migrations list
# AddKeys should now show as (Pending ⏳)

# Step 3 — re-apply cleanly
dotnet ef database update
```

### Nuclear option — revert everything
```bash
# Revert ALL migrations (empty database)
dotnet ef database update 0

# Re-apply everything from scratch
dotnet ef database update
```

### Why partial failures happen
```
Migration starts
     │
     ├── Command 1: DropIndex          ✅ succeeded
     ├── Command 2: AddUniqueConstraint ❌ failed  ← stopped here
     └── Command 3: CreateTable        ⏳ never ran

DB is now in broken state:
  - IX_Users_Email is GONE (Command 1 ran)
  - AK_Users_Email does NOT exist (Command 2 failed)

Next run: EF tries DropIndex again → "index does not exist" error
```

> 💡 **Rule** — Whenever a migration fails, always revert to the last clean
> migration before trying again. Never try to re-run a partially applied migration.

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Every entity needs a Primary Key | EF Core won't work without it |
| Composite Keys must use Fluent API | Data Annotations can't configure composite keys |
| Alternate Key > Unique Index when you need FK reference | Only Alternate Keys can be FK targets |
| Can't have Unique Index + Alternate Key on same column | Drop the index first in the migration |
| FK column alone doesn't enforce the constraint | The constraint is added when you configure the relationship |
| Always revert a failed migration before retrying | Partial state causes cascading errors |

---

## 🚀 How to Run

```bash
# Create migration
dotnet ef migrations add AddKeys --output-dir Data/Migrations

# If migration fails halfway — revert first!
dotnet ef database update ConfigureUserEntity
dotnet ef database update

# Normal apply
dotnet ef database update

# Run the app
dotnet run
```

---

## ✅ What's Next — Step 05: Relationships
In the next step we will:
- Wire up **Navigation Properties** on all entities
- Configure **One-to-One** (User → UserProfile)
- Configure **One-to-Many** (Team → Projects, Project → Tasks)
- Configure **Many-to-Many** (User ↔ Team via TeamMember)
- Configure **Cascade Delete** and **DeleteBehavior**
- Add all remaining entities (Team, Project, Sprint, TaskItem, Comment, Label)
