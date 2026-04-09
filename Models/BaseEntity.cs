namespace TaskManagerAPI.Models
{
    // Every entity inherits from BaseEntity
    public abstract class BaseEntity
    {
        public int Id {get; set;}
        public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
        public DateTime? UpdatedAt {get; set;}
    }
}