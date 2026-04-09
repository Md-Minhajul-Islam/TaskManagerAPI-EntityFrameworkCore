# TaskManagerAPI — Step 03: Entity Configuration

## 📌 What This Step Covers
- Data Annotations — configure entities directly on the model class
- Fluent API — configure entities in a separate configuration class
- IEntityTypeConfiguration — dedicated config class per entity
- ModelBuilder — the object that builds the database schema
- Table mapping — map a class to a specific table name
- Column configuration — types, lengths, nullability, defaults, ordering

---

## 🧠 Two Ways to Configure Entities

EF Core gives you **two ways** to tell it how to map your C# models to database tables:

```
Option 1 — Data Annotations    →  attributes on the model class itself
Option 2 — Fluent API          →  separate configuration class (recommended ✅)
```

Both can be used together. **Fluent API always wins** if there is a conflict.

---

## 1️⃣ Data Annotations

Data Annotations are **C# attributes** you place directly on your model class or its properties.

### On `BaseEntity`
```csharp
public abstract class BaseEntity
{
    [Key]                                                    // Primary Key
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]    // Auto-increment
    public int Id { get; set; }

    [Required]                                               // NOT NULL
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }                 // NULL allowed (nullable type)
}
```

### On `User`
```csharp
[Table("Users")]               // Map this class to the "Users" table
public class User : BaseEntity
{
    [Required]                 // NOT NULL in DB
    [MaxLength(100)]           // NVARCHAR(100) in DB
    [Column("FullName")]       // Explicit column name
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    [Column("Email")]
    [EmailAddress]             // API-level validation only — NOT enforced by DB
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    [Column("Role")]
    public string Role { get; set; } = "Member";

    [Required]
    [Column("IsActive")]
    public bool IsActive { get; set; } = true;
}
```

### Complete Data Annotations Reference

| Annotation | Effect | DB Level |
|------------|--------|----------|
| `[Key]` | Marks property as Primary Key | ✅ Yes |
| `[DatabaseGenerated(Identity)]` | Auto-increment column | ✅ Yes |
| `[Required]` | NOT NULL constraint | ✅ Yes |
| `[MaxLength(n)]` | NVARCHAR(n) max length | ✅ Yes |
| `[Column("name")]` | Custom column name | ✅ Yes |
| `[Table("name")]` | Custom table name | ✅ Yes |
| `[NotMapped]` | Exclude property from DB | ✅ Yes |
| `[EmailAddress]` | Email format validation | ❌ API only |
| `[StringLength(n)]` | Max string length | ✅ Yes |
| `[Range(min, max)]` | Numeric range validation | ❌ API only |
| `[ForeignKey("name")]` | Marks foreign key property | ✅ Yes |

---

## 2️⃣ IEntityTypeConfiguration\<T\>

`IEntityTypeConfiguration<T>` is an **EF Core interface** that forces you to implement one method — `Configure()` — where you write all Fluent API configuration for a single entity.

### Why use it?
- Keeps model classes clean — no database concerns on the entity
- One file per entity — easy to find and maintain
- Full power of Fluent API — things Data Annotations can't do
- Registered automatically — no manual wiring needed

### Structure
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;              // ← Must import your Models namespace

namespace TaskManagerAPI.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    //  ↑ IEntityTypeConfiguration<User> is the EF Core interface
    //  ↑ <User> tells EF Core which entity this config is for

    public void Configure(EntityTypeBuilder<User> builder)
    {
        //  ↑ This method is AUTOMATICALLY called by EF Core
        //  ↑ EntityTypeBuilder<User> is the Fluent API entry point
        //  ↑ Everything you configure here affects the Users table

        // all your Fluent API config goes here...
    }
}
```

### Complete `UserConfiguration`
```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // ── Table ──────────────────────────────────────────────────
        builder.ToTable("Users");

        // ── Primary Key ────────────────────────────────────────────
        builder.HasKey(u => u.Id);

        // ── Id ─────────────────────────────────────────────────────
        builder.Property(u => u.Id)
            .UseIdentityColumn()            // Auto-increment (1, 1)
            .HasColumnName("Id");

        // ── FullName ───────────────────────────────────────────────
        builder.Property(u => u.FullName)
            .IsRequired()                   // NOT NULL
            .HasMaxLength(100)              // NVARCHAR(100)
            .HasColumnName("FullName")
            .HasColumnOrder(1);

        // ── Email ──────────────────────────────────────────────────
        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(150)
            .HasColumnName("Email")
            .HasColumnOrder(2);

        // ── Role ───────────────────────────────────────────────────
        builder.Property(u => u.Role)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnName("Role")
            .HasDefaultValue("Member");     // Default value in DB

        // ── IsActive ───────────────────────────────────────────────
        builder.Property(u => u.IsActive)
            .IsRequired()
            .HasColumnName("IsActive")
            .HasDefaultValue(true);

        // ── CreatedAt ──────────────────────────────────────────────
        builder.Property(u => u.CreatedAt)
            .IsRequired()
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("GETUTCDATE()");  // DB generates timestamp

        // ── UpdatedAt ──────────────────────────────────────────────
        builder.Property(u => u.UpdatedAt)
            .IsRequired(false)              // NULL allowed
            .HasColumnName("UpdatedAt");

        // ── Unique Index on Email ──────────────────────────────────
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        // ── Index on IsActive ──────────────────────────────────────
        builder.HasIndex(u => u.IsActive)
            .HasDatabaseName("IX_Users_IsActive");
    }
}
```

---

## 3️⃣ How IEntityTypeConfiguration Gets Registered

You **never** call `UserConfiguration` manually. You tell `AppDbContext` to scan the assembly and find all configuration classes automatically:

```csharp
// Data/AppDbContext.cs
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Scans the entire assembly for every class
    // that implements IEntityTypeConfiguration<T>
    // and calls Configure() on each one automatically
    modelBuilder.ApplyConfigurationsFromAssembly(
        typeof(AppDbContext).Assembly);

    // This means:
    // UserConfiguration.Configure()     → called ✅
    // ProjectConfiguration.Configure()  → called ✅  (when we add it later)
    // TaskConfiguration.Configure()     → called ✅  (when we add it later)
    // You never touch this line again no matter how many entities you add!
}
```

---

## 4️⃣ ModelBuilder

`ModelBuilder` is the object EF Core passes to `OnModelCreating()`. It is the **root entry point** for all Fluent API configuration.

```
ModelBuilder
    │
    ├── modelBuilder.Entity<User>()          → configure User entity directly
    ├── modelBuilder.ApplyConfiguration()   → apply one config class
    └── modelBuilder.ApplyConfigurationsFromAssembly() → apply ALL config classes ✅
```

Inside a configuration class, `EntityTypeBuilder<T>` (the `builder` parameter) is the entity-level entry point:

```
EntityTypeBuilder<User>  (builder)
    │
    ├── builder.ToTable()         → table name
    ├── builder.HasKey()          → primary key
    ├── builder.Property()        → column config
    ├── builder.HasIndex()        → indexes
    ├── builder.HasOne()          → relationships (Step 05)
    └── builder.HasMany()         → relationships (Step 05)
```

---

## 5️⃣ Data Annotations vs Fluent API — Side by Side

| Feature | Data Annotations | Fluent API |
|---------|-----------------|------------|
| Location | On the model class | Separate configuration class |
| Readability | Quick, visible on model | Clean, organized per entity |
| Power | Limited | Full control |
| Default values | ❌ Not supported | ✅ `HasDefaultValue()` |
| Composite keys | ❌ Not supported | ✅ `HasKey(x => new { x.A, x.B })` |
| Column ordering | ❌ Not supported | ✅ `HasColumnOrder()` |
| Complex indexes | ❌ Limited | ✅ Full control |
| Cascade delete | ❌ Limited | ✅ `OnDelete(DeleteBehavior.X)` |
| Wins on conflict | ❌ Loses | ✅ Always wins |
| Recommended for | Simple validation | Production apps ✅ |

---

## 6️⃣ Common Mistakes

### ❌ Missing using for Models namespace
```csharp
// WRONG — User is not found
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class UserConfiguration : IEntityTypeConfiguration<User> // ← Error!
```
```csharp
// CORRECT
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagerAPI.Models;   // ← Must add this!

public class UserConfiguration : IEntityTypeConfiguration<User> // ← Works ✅
```

### ❌ Wrong method casing
```csharp
builder.toTable("Users");   // ❌ C# is case-sensitive — won't compile
builder.ToTable("Users");   // ✅ Correct
```

### ❌ Forgetting ApplyConfigurationsFromAssembly
```csharp
// If you forget this line, NONE of your configuration classes run!
modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
```

---

## 🔄 How It All Connects

```
dotnet run
     │
     ▼
AppDbContext constructor is called by DI
     │
     ▼
OnModelCreating(modelBuilder) runs
     │
     ▼
ApplyConfigurationsFromAssembly() scans the project
     │
     ├── finds UserConfiguration → calls Configure(builder)
     │       └── builder.ToTable("Users")
     │       └── builder.Property(u => u.Email).IsRequired().HasMaxLength(150)...
     │       └── builder.HasIndex(u => u.Email).IsUnique()...
     │
     ▼
EF Core builds the internal model (schema)
     │
     ▼
dotnet ef migrations add → reads the model → generates SQL
     │
     ▼
dotnet ef database update → runs SQL → DB schema is created ✅
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always add `using TaskManagerAPI.Models` in config files | Without it, the entity type won't resolve |
| Fluent API always overrides Data Annotations | Don't configure the same thing in both |
| Use `ApplyConfigurationsFromAssembly` not manual registration | Scales to any number of entities automatically |
| Create one configuration class per entity | Keeps things organized and easy to find |
| Never put business logic in configuration classes | Config is only for DB schema mapping |

---

## 🚀 How to Run

```bash
# Create migration for config changes
dotnet ef migrations add ConfigureUserEntity --output-dir Data/Migrations

# Apply to database
dotnet ef database update

# Run the app
dotnet run
```

---

## ✅ What's Next — Step 04: Keys
In the next step we will:
- Configure **Primary Keys** (single and composite)
- Configure **Alternate Keys** (unique constraints as keys)
- Configure **Foreign Keys** (linking entities together)
- Configure **Composite Keys** (two columns as one primary key)
- See how all key types look in the generated migration SQL
