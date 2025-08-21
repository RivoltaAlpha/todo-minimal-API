// DTOs.cs
using System.ComponentModel.DataAnnotations;

public record CreateTodoRequest(
    [Required, StringLength(200, MinimumLength = 1)] 
    string Name,
    
    bool IsComplete = false
);

public record UpdateTodoRequest(
    [Required, StringLength(200, MinimumLength = 1)] 
    string Name,
    
    bool IsComplete
);

public record TodoResponse(
    int Id, 
    string Name, 
    bool IsComplete, 
    DateTime CreatedAt
);