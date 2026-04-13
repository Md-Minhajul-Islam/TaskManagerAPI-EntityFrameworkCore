# TaskManagerAPI — Step 07: Querying Data with LINQ

## 📌 What This Step Covers
- Where — filtering rows
- Select — projecting to a smaller shape
- OrderBy / OrderByDescending / ThenBy — sorting
- GroupBy — grouping results
- Count / Sum / Average — aggregations
- Skip / Take — pagination
- QueryParameters — reusable filter/sort/page input model
- PagedResult<T> — reusable paginated response wrapper
- ITaskRepository / TaskRepository — entity-specific queries
- ITaskService / TaskService — business logic for tasks
- TasksController — HTTP endpoints

---

## 🧠 LINQ → SQL Translation

Every LINQ method translates to a SQL clause:

| LINQ Method | SQL Equivalent |
|-------------|---------------|
| `Where(x => x.Status == "Todo")` | `WHERE Status = 'Todo'` |
| `Select(x => new { x.Id, x.Title })` | `SELECT Id, Title` |
| `OrderBy(x => x.CreatedAt)` | `ORDER BY CreatedAt ASC` |
| `OrderByDescending(x => x.CreatedAt)` | `ORDER BY CreatedAt DESC` |
| `ThenBy(x => x.Title)` | `, Title ASC` |
| `GroupBy(x => x.Status)` | `GROUP BY Status` |
| `Count()` | `COUNT(*)` |
| `Sum(x => x.Points)` | `SUM(Points)` |
| `Average(x => x.Points)` | `AVG(Points)` |
| `Skip(20)` | `OFFSET 20 ROWS` |
| `Take(10)` | `FETCH NEXT 10 ROWS ONLY` |
| `Any(x => ...)` | `EXISTS (SELECT 1 WHERE ...)` |
| `FirstOrDefault(x => ...)` | `SELECT TOP 1 WHERE ...` |

---

## 1️⃣ Where — Filtering

`Where()` filters rows based on a condition — generates a `WHERE` clause.

```csharp
// Simple filter
_dbSet.Where(t => t.Status == "Todo")
// SQL: SELECT * FROM TaskItems WHERE Status = 'Todo'

// Multiple conditions
_dbSet.Where(t => t.Status == "Todo" && !t.IsDeleted)
// SQL: SELECT * FROM TaskItems WHERE Status = 'Todo' AND IsDeleted = 0

// Chained Where (AND conditions)
query = query.Where(t => t.Priority == "High");
query = query.Where(t => t.ProjectId == 1);
// SQL: WHERE Priority = 'High' AND ProjectId = 1

// Contains — like LIKE '%term%'
query = query.Where(t => t.Title.Contains("login"));
// SQL: WHERE Title LIKE '%login%'
```

---

## 2️⃣ Select — Projection

`Select()` maps entities to a different shape — generates a `SELECT` with specific columns.
Use it to avoid loading columns you don't need.

```csharp
// Project to anonymous type
_dbSet.Select(t => new { t.Id, t.Title, t.Status })
// SQL: SELECT Id, Title, Status FROM TaskItems

// Project to a DTO
_dbSet
    .Where(t => t.ProjectId == projectId)
    .Include(t => t.Assignee)
    .Select(t => new TaskSummary
    {
        Id           = t.Id,
        Title        = t.Title,
        Status       = t.Status,
        Priority     = t.Priority,
        AssigneeName = t.Assignee != null
                           ? t.Assignee.FullName
                           : "Unassigned"
    })
// SQL: SELECT t.Id, t.Title, t.Status, t.Priority, u.FullName
//      FROM TaskItems t LEFT JOIN Users u ON u.Id = t.AssigneeId
//      WHERE t.ProjectId = @id
```

> 💡 Always project to DTOs at the repository level when you don't need the full entity.
> This reduces data transfer between DB and app.

---

## 3️⃣ OrderBy — Sorting

`OrderBy()` sorts results ascending, `OrderByDescending()` sorts descending.
`ThenBy()` adds a secondary sort.

```csharp
// Simple ascending sort
_dbSet.OrderBy(t => t.CreatedAt)
// SQL: ORDER BY CreatedAt ASC

// Descending sort
_dbSet.OrderByDescending(t => t.CreatedAt)
// SQL: ORDER BY CreatedAt DESC

// Primary + secondary sort
_dbSet
    .OrderBy(t => t.Priority)
    .ThenBy(t => t.CreatedAt)
// SQL: ORDER BY Priority ASC, CreatedAt ASC

// Custom sort order (not alphabetical)
_dbSet.OrderBy(t =>
    t.Priority == "High"   ? 1 :
    t.Priority == "Medium" ? 2 : 3)
// SQL: ORDER BY CASE Priority
//              WHEN 'High' THEN 1
//              WHEN 'Medium' THEN 2
//              ELSE 3 END
```

---

## 4️⃣ GroupBy — Grouping

`GroupBy()` groups rows by a key — generates a `GROUP BY` clause.
The result is groups where `g.Key` is the grouped value and `g` contains the items.

```csharp
_dbSet
    .Where(t => t.ProjectId == projectId)
    .GroupBy(t => t.Status)             // ← group by Status column
    .Select(g => new TaskGroupByStatus
    {
        Status     = g.Key,             // ← the grouped value ("Todo", "InProgress" etc.)
        Count      = g.Count(),         // ← COUNT(*) per group
        TaskTitles = g.Select(t => t.Title).ToList()  // ← items in each group
    })

// SQL: SELECT Status, COUNT(*), ...
//      FROM TaskItems
//      WHERE ProjectId = @id
//      GROUP BY Status
```

### Understanding the GroupBy result
```
Input rows:
  { Id=1, Status="Todo",       Title="Task A" }
  { Id=2, Status="Todo",       Title="Task B" }
  { Id=3, Status="InProgress", Title="Task C" }
  { Id=4, Status="Done",       Title="Task D" }

After GroupBy(t => t.Status):
  Group 1: Key="Todo"        → [Task A, Task B]
  Group 2: Key="InProgress"  → [Task C]
  Group 3: Key="Done"        → [Task D]

After Select:
  { Status="Todo",        Count=2, TaskTitles=["Task A", "Task B"] }
  { Status="InProgress",  Count=1, TaskTitles=["Task C"] }
  { Status="Done",        Count=1, TaskTitles=["Task D"] }
```

---

## 5️⃣ Aggregation — Count, Sum, Average

```csharp
// COUNT — total rows
var total = await _dbSet.CountAsync();
// SQL: SELECT COUNT(*) FROM TaskItems

// COUNT with filter
var completed = await _dbSet.CountAsync(t => t.Status == "Done");
// SQL: SELECT COUNT(*) FROM TaskItems WHERE Status = 'Done'

// COUNT in memory (after ToListAsync)
var tasks = await _dbSet.ToListAsync();
var completed = tasks.Count(t => t.Status == "Done");  // LINQ to Objects

// Completion rate (average / percentage)
var rate = Math.Round((double)completed / total * 100, 2);
// e.g. 3 done / 10 total = 30.00%

// AnyAsync — checks if any row matches
var exists = await _dbSet.AnyAsync(t => t.Title == "Fix Bug");
// SQL: SELECT TOP 1 1 FROM TaskItems WHERE Title = 'Fix Bug'
```

---

## 6️⃣ Pagination — Skip / Take

Pagination loads a **subset** of rows instead of all rows at once.

### How Skip / Take work
```
Total rows: 50 tasks
Page size: 10

Page 1: Skip(0).Take(10)   → rows 1-10
Page 2: Skip(10).Take(10)  → rows 11-20
Page 3: Skip(20).Take(10)  → rows 21-30

Formula: Skip = (pageNumber - 1) * pageSize
```

### Code
```csharp
var pageNumber = 2;
var pageSize   = 10;

var items = await _dbSet
    .OrderBy(t => t.CreatedAt)
    .Skip((pageNumber - 1) * pageSize)   // skip 10
    .Take(pageSize)                       // take 10
    .ToListAsync();

// SQL: SELECT * FROM TaskItems
//      ORDER BY CreatedAt
//      OFFSET 10 ROWS
//      FETCH NEXT 10 ROWS ONLY
```

> ⚠️ Always `OrderBy` before `Skip/Take`.
> Without ordering, SQL Server can return rows in any order — pagination becomes unreliable.

---

## 7️⃣ QueryParameters — Reusable Input Model

`QueryParameters` is a single object that carries all filter/sort/page options:

```csharp
public class QueryParameters
{
    public int    PageNumber    { get; set; } = 1;
    public int    PageSize      { get; set; } = 10;  // max 50
    public string? SearchTerm   { get; set; }        // title/description search
    public string? Status       { get; set; }        // filter by status
    public string? Priority     { get; set; }        // filter by priority
    public string  SortBy       { get; set; } = "CreatedAt";
    public bool    SortDescending { get; set; } = true;
}
```

### How it's used in the paged query
```csharp
IQueryable<TaskItem> query = _dbSet.Where(t => t.ProjectId == projectId);

// Apply search
if (!string.IsNullOrEmpty(parameters.SearchTerm))
    query = query.Where(t => t.Title.Contains(parameters.SearchTerm));

// Apply status filter
if (!string.IsNullOrEmpty(parameters.Status))
    query = query.Where(t => t.Status == parameters.Status);

// Count BEFORE pagination (total matching records)
var totalCount = await query.CountAsync();

// Apply sort
query = parameters.SortBy switch
{
    "title"  => parameters.SortDescending
                    ? query.OrderByDescending(t => t.Title)
                    : query.OrderBy(t => t.Title),
    _        => parameters.SortDescending
                    ? query.OrderByDescending(t => t.CreatedAt)
                    : query.OrderBy(t => t.CreatedAt)
};

// Apply pagination
var items = await query
    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
    .Take(parameters.PageSize)
    .ToListAsync();
```

---

## 8️⃣ PagedResult\<T\> — Reusable Response Wrapper

```csharp
public class PagedResult<T>
{
    public IEnumerable<T> Items      { get; set; }   // the data
    public int TotalCount            { get; set; }   // total matching records in DB
    public int PageNumber            { get; set; }   // current page
    public int PageSize              { get; set; }   // items per page
    public int TotalPages  => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext     => PageNumber < TotalPages;
}
```

### Example response
```json
{
  "items": [...],
  "totalCount": 47,
  "pageNumber": 2,
  "pageSize": 10,
  "totalPages": 5,
  "hasPrevious": true,
  "hasNext": true
}
```

---

## 9️⃣ Full Layer Flow

```
GET /api/tasks/project/1/paged?pageNumber=1&pageSize=2&status=Todo&sortBy=title
         │
         ▼
TasksController.GetPaged(projectId: 1, parameters: QueryParameters)
         │  calls
         ▼
TaskService.GetPagedAsync(parameters, projectId: 1)
         │  calls
         ▼
TaskRepository.GetPagedTasksAsync(parameters, projectId: 1)
         │
         ├── query = _dbSet.Where(t => t.ProjectId == 1 && !t.IsDeleted)
         ├── query = query.Where(t => t.Status == "Todo")
         ├── totalCount = query.CountAsync()   → 3
         ├── query = query.OrderBy(t => t.Title)
         ├── query = query.Skip(0).Take(2)
         └── items = query.ToListAsync()
         │
         SQL:
         SELECT * FROM TaskItems
         WHERE ProjectId = 1 AND IsDeleted = 0 AND Status = 'Todo'
         ORDER BY Title ASC
         OFFSET 0 ROWS FETCH NEXT 2 ROWS ONLY
         │
         ▼
TaskService maps items → IEnumerable<TaskResponseDto>
TaskService wraps in PagedResult<TaskResponseDto>
         │
         ▼
Controller returns 200 OK with PagedResult
```

---

## 🧪 Test JSONs

### POST /api/tasks — Create Tasks (run all 5 to populate data)

```json
{
  "title": "Design Login Page",
  "description": "Create responsive login page with validation",
  "priority": "High",
  "projectId": 1,
  "reporterId": 1,
  "assigneeId": 1,
  "sprintId": null
}
```

```json
{
  "title": "Fix Payment Bug",
  "description": "Payment fails on checkout for international cards",
  "priority": "High",
  "projectId": 1,
  "reporterId": 1,
  "assigneeId": 2,
  "sprintId": null
}
```

```json
{
  "title": "Write Unit Tests",
  "description": "Add unit tests for auth module",
  "priority": "Medium",
  "projectId": 1,
  "reporterId": 1,
  "assigneeId": null,
  "sprintId": null
}
```

```json
{
  "title": "Update Documentation",
  "description": "Update API docs with new endpoints",
  "priority": "Low",
  "projectId": 1,
  "reporterId": 2,
  "assigneeId": null,
  "sprintId": null
}
```

```json
{
  "title": "Setup CI/CD Pipeline",
  "description": "Configure GitHub Actions for automated deployment",
  "priority": "Medium",
  "projectId": 1,
  "reporterId": 1,
  "assigneeId": 1,
  "sprintId": null
}
```

### PUT /api/tasks/1 — Update Status to InProgress
```json
{
  "title": "Design Login Page",
  "description": "Create responsive login page with validation",
  "status": "InProgress",
  "priority": "High",
  "assigneeId": 1,
  "sprintId": null
}
```

---

## 🌐 GET Endpoints to Test

| Endpoint | Demonstrates | Expected Result |
|----------|-------------|-----------------|
| `GET /api/tasks/1` | Basic get by id | Single task |
| `GET /api/tasks/by-status/Todo` | WHERE filter | Tasks with Status=Todo |
| `GET /api/tasks/by-status/InProgress` | WHERE filter | Tasks with Status=InProgress |
| `GET /api/tasks/by-priority/High` | WHERE filter | High priority tasks |
| `GET /api/tasks/project/1/summaries` | SELECT projection | Lightweight task list |
| `GET /api/tasks/project/1/grouped-by-status` | GROUP BY | Tasks grouped by status |
| `GET /api/tasks/project/1/stats` | COUNT aggregation | Total, completed, rate |
| `GET /api/tasks/project/1/paged` | Default pagination | Page 1, 10 items |
| `GET /api/tasks/project/1/paged?pageNumber=1&pageSize=2` | Custom page size | 2 items per page |
| `GET /api/tasks/project/1/paged?status=Todo` | Filter + paginate | Only Todo tasks |
| `GET /api/tasks/project/1/paged?priority=High&sortBy=title&sortDescending=false` | Sort + paginate | High priority, sorted A-Z |
| `GET /api/tasks/project/1/paged?searchTerm=login` | Search + paginate | Tasks with "login" in title |

---

## ⚡ Key Rules to Remember

| Rule | Reason |
|------|--------|
| Always `OrderBy` before `Skip/Take` | Without ordering pagination is unreliable |
| Count BEFORE Skip/Take | Count after pagination gives you page size, not total |
| Use `Select` to project, don't load full entity when not needed | Better performance |
| Cap `PageSize` with a max value | Prevent clients requesting thousands of rows |
| Build queries as `IQueryable<T>` before calling `ToListAsync()` | Query is only sent to DB when materialized |
| Chain `Where()` calls for dynamic filters | EF Core combines them into one SQL `WHERE` |

---

## 💡 IQueryable vs IEnumerable

```csharp
// IQueryable<T> — query NOT yet sent to DB
// Each chained method adds to the SQL query
IQueryable<TaskItem> query = _dbSet.Where(t => t.ProjectId == 1);
query = query.Where(t => t.Status == "Todo");
query = query.OrderBy(t => t.Title);
// Still no SQL sent!

// ToListAsync() — NOW the SQL is sent to DB
var items = await query.ToListAsync();
// SQL: SELECT * FROM TaskItems WHERE ProjectId=1 AND Status='Todo' ORDER BY Title

// IEnumerable<T> — data already in memory
// Filtering happens in C#, not SQL — less efficient
var items = await _dbSet.ToListAsync();         // ALL rows loaded from DB ❌
var filtered = items.Where(t => t.Status == "Todo");  // filtered in C# memory ❌
```

> 💡 Always build your complete query as `IQueryable<T>` before calling
> `ToListAsync()` — this way EF Core sends ONE optimized SQL query.

---

## 🚀 How to Run

```bash
# No new migrations needed — no model changes
dotnet build
dotnet run

# Step 1: Create a project first (needed for projectId = 1)
# POST /api/projects  (if you have the endpoint)
# Or directly insert via SQL Server Management Studio

# Step 2: Create tasks using the POST JSONs above

# Step 3: Test all GET endpoints
```

---

## ✅ Folder Structure After Step 7

```
TaskManagerAPI/
├── Controllers/
│   ├── UsersController.cs
│   └── TasksController.cs                   ← NEW
├── DTOs/
│   ├── Common/
│   │   ├── QueryParameters.cs               ← NEW
│   │   └── PagedResult.cs                   ← NEW
│   └── Task/
│       ├── TaskResponseDto.cs               ← NEW
│       ├── CreateTaskDto.cs                 ← NEW
│       └── UpdateTaskDto.cs                 ← NEW
├── Repositories/
│   ├── Interfaces/
│   │   ├── IRepository.cs                   ← Updated (LINQ methods)
│   │   └── ITaskRepository.cs               ← NEW
│   └── Implementations/
│       ├── Repository.cs                    ← Updated (LINQ implementations)
│       └── TaskRepository.cs               ← NEW
├── Services/
│   ├── Interfaces/
│   │   └── ITaskService.cs                  ← NEW
│   └── Implementations/
│       └── TaskService.cs                   ← NEW
└── UnitOfWork/
    ├── IUnitOfWork.cs                       ← Updated (Tasks added)
    └── UnitOfWork.cs                        ← Updated (Tasks added)
```

---

## ✅ What's Next — Step 08: Tracking
In the next step we will:
- Deep dive into **Change Tracking** — how EF Core watches entities
- **AsNoTracking()** — when and why to disable tracking
- **EntityState** — Added, Modified, Deleted, Unchanged, Detached
- **AsNoTrackingWithIdentityResolution()** — middle ground
- Add `AsNoTracking` to read-only repository methods
