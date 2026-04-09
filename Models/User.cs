namespace TaskManagerAPI.Models
{
    public class User : BaseEntity
    {
        public string FullName {get; set;} = string.Empty;
        public string Email {get; set;} = string.Empty;
        public string Role {get; set;} = "Member"; // "Admin", "Member"
        public bool IsActive {get; set;} = true;
    }
} 
