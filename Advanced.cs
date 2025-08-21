// Todo.cs
public class AdvancedTodo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsDeleted { get; set; } = false;  // Internal field
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
}