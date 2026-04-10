# TaskManagerAPI — Step 05: Relationships

## 📌 What This Step Covers
- Navigation Properties — how entities reference each other
- One-to-One relationship — User ↔ UserProfile
- One-to-Many relationship — Team → Projects, Project → Tasks etc.
- Many-to-Many relationship — User ↔ Team, TaskItem ↔ Label
- Cascade Delete — what happens to children when parent is deleted
- DeleteBehavior — Cascade, Restrict, SetNull, NoAction
- SQL Server cascade path limitation and how to fix it

---

## 🗺️ Full Relationship Map

```
User ──────────────────── UserProfile        One-to-One
User ──────────────────── TeamMember         One-to-Many
Team ──────────────────── TeamMember         One-to-Many
User ↔ Team  (via TeamMember)                Many-to-Many

Team ──────────────────── Project            One-to-Many
User (Owner) ──────────── Project            One-to-Many
Project ───────────────── Sprint             One-to-Many
Project ───────────────── TaskItem           One-to-Many
Sprint ────────────────── TaskItem           One-to-Many
User (Reporter) ────────── TaskItem          One-to-Many
User (Assignee) ────────── TaskItem          One-to-Many
TaskItem ──────────────── Comment            One-to-Many
User (Author) ─────────── Comment            One-to-Many
TaskItem ↔ Label (via TaskLabel)             Many-to-Many
```

---

## 1️⃣ Navigation Properties

Navigation Properties are C# properties that let you **navigate from one entity to a related entity** without writing JOIN queries.

### Two types of Navigation Properties

```csharp
// Reference Navigation — points to ONE related entity
public User Reporter { get; set; } = null!;        // TaskItem → one User
public Project Project { get; set; } = null!;       // TaskItem → one Project

// Collection Navigation — points to MANY related entities
public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();   // Project → many Tasks
public ICollection<Comment> Comments { get; set; } = new List<Comment>(); // TaskItem → many Comments
```

### Why `null!` on reference navigations?
```csharp
public User Reporter { get; set; } = null!;
// null! tells the compiler "I know this looks null but EF Core will populate it"
// EF Core loads this when you use Include() — Step 06
```

### Why `new List<>()` on collection navigations?
```csharp
public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
// Initializing prevents NullReferenceException when you access Tasks
// before loading from DB e.g. project.Tasks.Count won't crash
```

---

## 2️⃣ One-to-One

One entity row links to **exactly one** row in another table.

### Our example: `User` ↔ `UserProfile`
```
Users table          UserProfiles table
───────────          ──────────────────
Id = 1     ◄──────── UserId = 1   (FK)
Id = 2     ◄──────── UserId = 2   (FK)
```

### Model setup
```csharp
// User.cs — reference navigation
public UserProfile? Profile { get; set; }   // nullable — user might not have a profile yet

// UserProfile.cs — reference navigation back
public int  UserId { get; set; }            // FK column
public User User   { get; set; } = null!;  // navigation back to User
```

### Fluent API configuration
```csharp
// In UserProfileConfiguration.cs
builder.HasOne(up => up.User)          // UserProfile has one User
    .WithOne(u => u.Profile)           // User has one UserProfile (back reference)
    .HasForeignKey<UserProfile>(up => up.UserId)  // FK lives on UserProfile side
    .OnDelete(DeleteBehavior.Cascade); // Delete User → delete their Profile
```

### Key rule
In One-to-One the FK lives on the **dependent** side (the entity that can't exist without the other).
`UserProfile` can't exist without a `User` → FK `UserId` goes on `UserProfiles` table.

---

## 3️⃣ One-to-Many

One entity row links to **many rows** in another table.

### Our examples

#### Team → Projects
```
Teams table        Projects table
───────────        ──────────────
Id = 1   ◄──────── TeamId = 1
         ◄──────── TeamId = 1
         ◄──────── TeamId = 1
Id = 2   ◄──────── TeamId = 2
```

#### Fluent API
```csharp
// In ProjectConfiguration.cs
builder.HasOne(p => p.Team)            // Project has one Team
    .WithMany(t => t.Projects)         // Team has many Projects
    .HasForeignKey(p => p.TeamId)      // FK is TeamId on Projects table
    .OnDelete(DeleteBehavior.Restrict);// Can't delete Team if it has Projects
```

#### TaskItem has TWO foreign keys to User
```csharp
// Reporter — always required
builder.HasOne(t => t.Reporter)
    .WithMany(u => u.ReportedTasks)
    .HasForeignKey(t => t.ReporterId)
    .OnDelete(DeleteBehavior.Restrict);

// Assignee — optional (nullable FK)
builder.HasOne(t => t.Assignee)
    .WithMany(u => u.AssignedTasks)
    .HasForeignKey(t => t.AssigneeId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.NoAction);
```

---

## 4️⃣ Many-to-Many

Many rows in table A link to many rows in table B **through a junction table**.

### Our examples

#### User ↔ Team via TeamMember
```
Users          TeamMembers           Teams
─────          ───────────           ─────
Id=1  ◄──── UserId=1, TeamId=1 ────► Id=1
Id=1  ◄──── UserId=1, TeamId=2 ────► Id=2
Id=2  ◄──── UserId=2, TeamId=1 ────► Id=1
```

#### Junction table model
```csharp
// TeamMember.cs — the junction table
public class TeamMember
{
    public int TeamId { get; set; }       // Part 1 of Composite PK + FK
    public int UserId { get; set; }       // Part 2 of Composite PK + FK
    public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; }

    // Navigation properties to both sides
    public Team Team { get; set; } = null!;
    public User User { get; set; } = null!;
}
```

#### Fluent API
```csharp
// In TeamMemberConfiguration.cs
builder.HasKey(tm => new { tm.TeamId, tm.UserId }); // Composite PK

builder.HasOne(tm => tm.Team)
    .WithMany(t => t.TeamMembers)
    .HasForeignKey(tm => tm.TeamId)
    .OnDelete(DeleteBehavior.Cascade);

builder.HasOne(tm => tm.User)
    .WithMany(u => u.TeamMembers)
    .HasForeignKey(tm => tm.UserId)
    .OnDelete(DeleteBehavior.Cascade);
```

#### TaskItem ↔ Label via TaskLabel
```csharp
// TaskLabel.cs — junction table
public class TaskLabel
{
    public int TaskId  { get; set; }    // FK → TaskItems.Id
    public int LabelId { get; set; }    // FK → Labels.Id

    public TaskItem Task  { get; set; } = null!;
    public Label    Label { get; set; } = null!;
}
```

---

## 5️⃣ DeleteBehavior

Controls what happens to child rows when the parent row is deleted.

| Behavior | SQL | Effect | Use When |
|----------|-----|--------|----------|
| `Cascade` | `ON DELETE CASCADE` | Deleting parent **deletes** all children | Children can't exist without parent (Task → Comments) |
| `Restrict` | `ON DELETE NO ACTION` | Deleting parent **throws error** if children exist | Must clean up children first (User → ReportedTasks) |
| `SetNull` | `ON DELETE SET NULL` | Deleting parent sets FK to **NULL** | Children can exist without parent (Task Assignee) |
| `NoAction` | `ON DELETE NO ACTION` | No action taken — app handles it | Avoid cascade path conflicts in SQL Server |

### Our DeleteBehavior decisions

| Relationship | Behavior | Reason |
|---|---|---|
| User → UserProfile | `Cascade` | Profile meaningless without User |
| Team → TeamMembers | `Cascade` | Membership meaningless without Team |
| User → TeamMembers | `Cascade` | Membership meaningless without User |
| Team → Projects | `Restrict` | Don't accidentally delete all projects |
| User → OwnedProjects | `Restrict` | Don't accidentally delete all projects |
| Project → Sprints | `Cascade` | Sprints belong to the project |
| Project → Tasks | `Cascade` | Tasks belong to the project |
| User → ReportedTasks | `Restrict` | Can't lose who reported a task |
| User → AssignedTasks | `NoAction` | Task can exist unassigned |
| Sprint → Tasks | `NoAction` | Task can exist outside a sprint |
| Task → Comments | `Cascade` | Comments belong to the task |
| User → Comments | `Restrict` | Can't lose who wrote a comment |
| Task → TaskLabels | `Cascade` | Labels link meaningless without task |
| Label → TaskLabels | `Cascade` | Labels link meaningless without label |

---

## ⚠️ SQL Server Cascade Path Limitation

### The Problem
SQL Server does **not allow** multiple cascade paths leading to the same table.

```
                    Projects
                       │ Cascade
                       ▼
Users ─Cascade──► TaskItems ◄──Cascade── Sprints
                                              │
                                         Sprints belong
                                         to Projects
                                              │
                                         So there are
                                         TWO cascade paths
                                         reaching TaskItems ❌
```

SQL Server error:
```
Introducing FOREIGN KEY constraint may cause cycles or multiple cascade paths.
Specify ON DELETE NO ACTION or ON UPDATE NO ACTION
```

### The Fix
Only ONE cascade path is allowed per table. All others must be `NoAction`:

```csharp
// ProjectId → Cascade ✅ (main parent — keep this one)
builder.HasOne(t => t.Project)
    .WithMany(p => p.Tasks)
    .HasForeignKey(t => t.ProjectId)
    .OnDelete(DeleteBehavior.Cascade);

// SprintId → NoAction ✅ (secondary path — must be NoAction)
builder.HasOne(t => t.Sprint)
    .WithMany(s => s.Tasks)
    .HasForeignKey(t => t.SprintId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.NoAction);  // ← NOT SetNull or Cascade

// AssigneeId → NoAction ✅ (two FKs to Users table — must be NoAction)
builder.HasOne(t => t.Assignee)
    .WithMany(u => u.AssignedTasks)
    .HasForeignKey(t => t.AssigneeId)
    .IsRequired(false)
    .OnDelete(DeleteBehavior.NoAction);  // ← NOT SetNull or Cascade
```

### Fix without losing data
If the migration was already generated with wrong values, **edit the migration file directly**:

```csharp
// In XXXXXX_AddRelationshipsFixed.cs — find and change:

// BEFORE ❌
table.ForeignKey(
    name: "FK_TaskItems_Sprints_SprintId",
    column: x => x.SprintId,
    principalTable: "Sprints",
    principalColumn: "Id",
    onDelete: ReferentialAction.SetNull);  // ← wrong

// AFTER ✅
table.ForeignKey(
    name: "FK_TaskItems_Sprints_SprintId",
    column: x => x.SprintId,
    principalTable: "Sprints",
    principalColumn: "Id",
    onDelete: ReferentialAction.NoAction); // ← fixed
```

> 💡 Editing the migration file directly preserves existing data.
> The configuration file change ensures future migrations are generated correctly.

---

## 🔄 How Fluent API Relationship Syntax Works

```csharp
builder
    .HasOne(child => child.Parent)      // This entity has ONE parent
    .WithMany(parent => parent.Children)// Parent has MANY of this entity
    .HasForeignKey(child => child.FKId) // FK column is on THIS entity
    .IsRequired(false)                  // FK is nullable (optional relationship)
    .OnDelete(DeleteBehavior.Cascade);  // What happens when parent is deleted

builder
    .HasOne(a => a.B)                   // One-to-One: A has one B
    .WithOne(b => b.A)                  // B has one A
    .HasForeignKey<A>(a => a.BId)       // Must specify which side holds the FK
    .OnDelete(DeleteBehavior.Cascade);
```

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| FK always goes on the dependent/child side | The entity that can't exist alone holds the FK |
| Initialize collection navigations with `new List<>()` | Prevents NullReferenceException |
| Use `null!` on required reference navigations | EF Core populates it — suppresses nullable warning |
| Only one Cascade path per table in SQL Server | Use `NoAction` for secondary paths |
| Never use `SetNull` when SQL Server has multiple FK paths | Causes cycle error — use `NoAction` |
| Configure relationships in the dependent entity's config | Keeps configuration close to where FK lives |
| Never edit Designer.cs or Snapshot.cs | Auto-managed by EF Core |
| Edit migration file directly to fix without losing data | Safer than reverting when DB has data |

---

## 🚀 How to Run

```bash
# Create migration
dotnet ef migrations add AddRelationships --output-dir Data/Migrations

# If cascade error — edit migration file directly
# Change ReferentialAction.SetNull → ReferentialAction.NoAction for:
#   FK_TaskItems_Sprints_SprintId
#   FK_TaskItems_Users_AssigneeId

# Apply
dotnet ef database update

# Run
dotnet run
```

---

## ✅ What's Next — Step 06: Loading Related Data
In the next step we will:
- **Eager Loading** — load related data upfront using `Include()` and `ThenInclude()`
- **Lazy Loading** — load related data automatically when accessed
- **Explicit Loading** — load related data on demand with `Entry().Collection().LoadAsync()`
- Add loading methods to our repositories
