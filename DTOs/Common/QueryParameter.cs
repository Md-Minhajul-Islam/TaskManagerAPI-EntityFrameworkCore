namespace TaskManagerAPI.DTOs.Common;

// Reusable input model for filtering, sorting and pagination
// Used by andy endpoint that return a list

public class QueryParameters
{
    // Pagination
    private const int MaxPageSize = 50;
    private int _pageSize = 10;

    public int PageNumber {get; set;} = 1;
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    // Filtering
    public string? SearchTerm {get; set;}   // Search in title/name
    public string? Status {get; set;}   // filter by status
    public string? Priority {get; set;} // filter by priority

    // Sorting
    public string SortBy {get; set;} = "CreatedAt";
    public bool SortDescending {get; set;} = true;
}