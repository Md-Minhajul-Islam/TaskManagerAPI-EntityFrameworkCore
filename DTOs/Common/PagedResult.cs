namespace TaskManagerAPI.DTOs.Common;

// Generinc wrapper for any paginated response
// T = the DTO type being returned

public class PagedResult<T>
{
    public IEnumerable<T> Items {get; set;} = new List<T>();
    public int TotalCount {get; set;}   // Total records in DB
    public int PageNumber {get; set;}   // Currnt page
    public int PageSize {get; set;}     // Items per page
    public int TotalPages => (int)Math.Ceiling(TotalCount/(double)PageSize);
    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

}