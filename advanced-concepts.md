# Advanced Minimal API Concepts: MapGroup, TypedResults, and Security

This guide covers advanced concepts that will make your Minimal APIs more organized, type-safe, and secure. Build upon the basic Todo API to implement professional-grade features.

## Table of Contents

1. [MapGroup API - Organizing Related Endpoints](#mapgroup-api---organizing-related-endpoints)
2. [TypedResults API - Type-Safe Responses](#typedresults-api---type-safe-responses)
3. [Preventing Over-Posting - Security Best Practices](#preventing-over-posting---security-best-practices)
4. [Complete Example Implementation](#complete-example-implementation)
5. [Testing Your Enhanced API](#testing-your-enhanced-api)

---

## MapGroup API - Organizing Related Endpoints

### The Problem
As your API grows, you end up with repetitive URL prefixes and scattered endpoint definitions:

```csharp
// Repetitive and hard to maintain
app.MapGet("/todoitems", ...);
app.MapGet("/todoitems/complete", ...);
app.MapPost("/todoitems", ...);
app.MapPut("/todoitems/{id}", ...);
app.MapDelete("/todoitems/{id}", ...);

app.MapGet("/users", ...);
app.MapPost("/users", ...);
// ... more endpoints
```

### The Solution: MapGroup

MapGroup allows you to organize related endpoints under a common URL prefix:

```csharp
// Create a group for todo-related endpoints
var todoItems = app.MapGroup("/todoitems");

// Add endpoints to the group (notice no /todoitems prefix needed)
todoItems.MapGet("/", GetAllTodos);
todoItems.MapGet("/complete", GetCompleteTodos);
todoItems.MapGet("/{id:int}", GetTodoById);
todoItems.MapPost("/", CreateTodo);
todoItems.MapPut("/{id:int}", UpdateTodo);
todoItems.MapDelete("/{id:int}", DeleteTodo);
```

### Benefits of MapGroup

1. **Reduces Code Duplication**: No need to repeat URL prefixes
2. **Better Organization**: Related endpoints are grouped together
3. **Easier Maintenance**: Change the base path in one place
4. **Bulk Configuration**: Apply middleware or policies to entire groups

### Advanced MapGroup Features

```csharp
// Apply tags for OpenAPI documentation
var todoItems = app.MapGroup("/todoitems")
    .WithTags("Todos")
    .WithDescription("Operations for managing todo items");

// Apply authentication to entire group
var protectedTodos = app.MapGroup("/api/todos")
    .RequireAuthorization();

// Apply rate limiting to group
var rateLimitedApi = app.MapGroup("/api/public")
    .RequireRateLimiting("PublicPolicy");
```

---

## TypedResults API - Type-Safe Responses

### The Problem with Results Class

Using the basic `Results` class isn't type-safe and can lead to runtime errors:

```csharp
// Not type-safe - could return wrong type
app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    return todo is not null ? Results.Ok(todo) : Results.NotFound();
    // Compiler can't verify the return types match
});
```

### The Solution: TypedResults

`TypedResults` provides compile-time type safety for your API responses:

```csharp
// Type-safe with compile-time checking
app.MapGet("/todoitems/{id}", async (int id, TodoDb db) => 
{
    var todo = await db.Todos.FindAsync(id);
    return todo is not null 
        ? TypedResults.Ok(todo)           // Returns Ok<Todo>
        : TypedResults.NotFound();        // Returns NotFound
});
```

### Common TypedResults Methods

```csharp
// Success responses
TypedResults.Ok(data)                    // 200 with data
TypedResults.Created(uri, data)          // 201 with location and data
TypedResults.Accepted(uri, data)         // 202 with location and data
TypedResults.NoContent()                 // 204 no content

// Client error responses
TypedResults.BadRequest(error)           // 400 with error details
TypedResults.NotFound()                  // 404 not found
TypedResults.Conflict(error)             // 409 conflict
TypedResults.UnprocessableEntity(error)  // 422 validation error

// Custom responses
TypedResults.Problem(details)            // RFC 7807 problem details
TypedResults.ValidationProblem(errors)   // Validation problem details
```

### Benefits of TypedResults

1. **Compile-Time Safety**: Catch errors before runtime
2. **Better IntelliSense**: IDE provides better code completion
3. **Automatic OpenAPI**: Better API documentation generation
4. **Consistent Responses**: Standardized response format

---

## Preventing Over-Posting - Security Best Practices

### The Problem: Over-Posting Attack

Over-posting occurs when clients send more data than expected, potentially modifying fields they shouldn't have access to:

```csharp
public class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
    public bool IsDeleted { get; set; }      // Internal field
    public DateTime CreatedAt { get; set; }  // Should be set by server
    public string? CreatedBy { get; set; }   // Should be set by server
}

// Vulnerable: Client could send IsDeleted=true or modify CreatedBy
app.MapPost("/todoitems", async (Todo todo, TodoDb db) => {
    // Client could manipulate internal fields!
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/todoitems/{todo.Id}", todo);
});
```

### Solution 1: Data Transfer Objects (DTOs)

Create separate classes for input and output:

```csharp
// Input DTO - only fields clients can set
public record CreateTodoRequest(string Name, bool IsComplete = false);

// Output DTO - only fields clients should see
public record TodoResponse(int Id, string Name, bool IsComplete, DateTime CreatedAt);

// Update DTO - only fields that can be updated
public record UpdateTodoRequest(string Name, bool IsComplete);
```

### Solution 2: Explicit Property Mapping

```csharp
app.MapPost("/todoitems", async (CreateTodoRequest request, TodoDb db) =>
{
    // Explicitly map only allowed properties
    var todo = new Todo
    {
        Name = request.Name,
        IsComplete = request.IsComplete,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "system" // Set by server
        // IsDeleted not set - defaults to false
    };
    
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    
    // Return only safe data
    var response = new TodoResponse(todo.Id, todo.Name, todo.IsComplete, todo.CreatedAt);
    return TypedResults.Created($"/todoitems/{todo.Id}", response);
});
```

### Solution 3: Model Binding with Validation

```csharp
// Add validation attributes to your DTOs
public record CreateTodoRequest(
    [Required, StringLength(200)] string Name,
    bool IsComplete = false
);

// Use built-in validation
app.MapPost("/todoitems", async (CreateTodoRequest request, TodoDb db) =>
{
    // Validation happens automatically
    // Invalid requests return 400 Bad Request with validation errors
    
    var todo = new Todo
    {
        Name = request.Name,
        IsComplete = request.IsComplete,
        CreatedAt = DateTime.UtcNow
    };
    
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    
    return TypedResults.Created($"/todoitems/{todo.Id}", 
        new TodoResponse(todo.Id, todo.Name, todo.IsComplete, todo.CreatedAt));
});
```

---

## Complete Example Implementation

Here's how to implement all these concepts together:

### 1. Create the DTOs

```csharp
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
```

### 2. Enhanced Todo Model

```csharp
// Todo.cs
public class Todo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public bool IsDeleted { get; set; } = false;  // Internal field
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
}
```

### 3. Complete Program.cs with All Concepts

```csharp
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Create a group for todo endpoints
var todoItems = app.MapGroup("/api/todoitems")
    .WithTags("Todos")
    .WithDescription("Operations for managing todo items")
    .WithOpenApi();

// GET /api/todoitems - Get all todos (not deleted)
todoItems.MapGet("/", async (TodoDb db) =>
{
    var todos = await db.Todos
        .Where(t => !t.IsDeleted)
        .Select(t => new TodoResponse(t.Id, t.Name, t.IsComplete, t.CreatedAt))
        .ToListAsync();
    
    return TypedResults.Ok(todos);
})
.WithName("GetAllTodos")
.WithSummary("Get all todo items")
.Produces<TodoResponse[]>();

// GET /api/todoitems/complete - Get completed todos
todoItems.MapGet("/complete", async (TodoDb db) =>
{
    var completeTodos = await db.Todos
        .Where(t => t.IsComplete && !t.IsDeleted)
        .Select(t => new TodoResponse(t.Id, t.Name, t.IsComplete, t.CreatedAt))
        .ToListAsync();
    
    return TypedResults.Ok(completeTodos);
})
.WithName("GetCompleteTodos")
.WithSummary("Get completed todo items")
.Produces<TodoResponse[]>();

// GET /api/todoitems/{id} - Get specific todo
todoItems.MapGet("/{id:int}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos
        .Where(t => t.Id == id && !t.IsDeleted)
        .Select(t => new TodoResponse(t.Id, t.Name, t.IsComplete, t.CreatedAt))
        .FirstOrDefaultAsync();

    return todo is not null 
        ? TypedResults.Ok(todo)
        : TypedResults.NotFound();
})
.WithName("GetTodoById")
.WithSummary("Get a specific todo item")
.Produces<TodoResponse>()
.Produces(404);

// POST /api/todoitems - Create new todo
todoItems.MapPost("/", async (CreateTodoRequest request, TodoDb db) =>
{
    var todo = new Todo
    {
        Name = request.Name,
        IsComplete = request.IsComplete,
        CreatedAt = DateTime.UtcNow,
        CreatedBy = "api-user" // In real app, get from authentication
    };
    
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    
    var response = new TodoResponse(todo.Id, todo.Name, todo.IsComplete, todo.CreatedAt);
    return TypedResults.Created($"/api/todoitems/{todo.Id}", response);
})
.WithName("CreateTodo")
.WithSummary("Create a new todo item")
.Produces<TodoResponse>(201)
.ProducesValidationProblem();

// PUT /api/todoitems/{id} - Update existing todo
todoItems.MapPut("/{id:int}", async (int id, UpdateTodoRequest request, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    
    if (todo is null || todo.IsDeleted)
        return TypedResults.NotFound();
    
    // Only update allowed fields
    todo.Name = request.Name;
    todo.IsComplete = request.IsComplete;
    // CreatedAt, CreatedBy, IsDeleted remain unchanged
    
    await db.SaveChangesAsync();
    
    return TypedResults.NoContent();
})
.WithName("UpdateTodo")
.WithSummary("Update an existing todo item")
.Produces(204)
.Produces(404)
.ProducesValidationProblem();

// DELETE /api/todoitems/{id} - Soft delete todo
todoItems.MapDelete("/{id:int}", async (int id, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    
    if (todo is null || todo.IsDeleted)
        return TypedResults.NotFound();
    
    // Soft delete - don't actually remove from database
    todo.IsDeleted = true;
    await db.SaveChangesAsync();
    
    return TypedResults.NoContent();
})
.WithName("DeleteTodo")
.WithSummary("Delete a todo item")
.Produces(204)
.Produces(404);

app.Run();
```

---

## Testing Your Enhanced API

### 1. Using HTTP Client (test.http file)

```http
### Get all todos
GET https://localhost:7xxx/api/todoitems

### Create a new todo (valid)
POST https://localhost:7xxx/api/todoitems
Content-Type: application/json

{
  "name": "Learn Advanced Minimal APIs",
  "isComplete": false
}

### Create a new todo (invalid - too short name)
POST https://localhost:7xxx/api/todoitems
Content-Type: application/json

{
  "name": "",
  "isComplete": false
}

### Try to over-post (this will be ignored)
POST https://localhost:7xxx/api/todoitems
Content-Type: application/json

{
  "name": "Malicious Todo",
  "isComplete": false,
  "isDeleted": true,
  "createdBy": "hacker"
}

### Update a todo
PUT https://localhost:7xxx/api/todoitems/1
Content-Type: application/json

{
  "name": "Updated todo name",
  "isComplete": true
}

### Get completed todos
GET https://localhost:7xxx/api/todoitems/complete

### Delete a todo (soft delete)
DELETE https://localhost:7xxx/api/todoitems/1
```

### 2. Swagger UI Testing

1. Run your application: `dotnet run`
2. Navigate to `https://localhost:7xxx/swagger`
3. Use the interactive UI to test all endpoints
4. Notice the improved documentation with proper response types

---

## Key Benefits Summary

### MapGroup API
- ✅ Organizes related endpoints
- ✅ Reduces code duplication
- ✅ Enables bulk configuration
- ✅ Improves maintainability

### TypedResults API
- ✅ Compile-time type safety
- ✅ Better IDE support
- ✅ Automatic OpenAPI documentation
- ✅ Consistent response format

### Over-Posting Prevention
- ✅ Enhanced security
- ✅ Data validation
- ✅ Clear API contracts
- ✅ Prevents data corruption

## Next Steps

1. **Add Authentication**: Implement JWT or API key authentication
2. **Add Caching**: Use Redis or in-memory caching for better performance
3. **Add Logging**: Implement structured logging with Serilog
4. **Add Health Checks**: Monitor your API's health status
5. **Add Rate Limiting**: Prevent API abuse
6. **Add Database Migrations**: Use real databases with Entity Framework migrations

## Troubleshooting

**Validation not working?**
- Ensure you're using the correct DTO types in your endpoints
- Check that validation attributes are properly imported

**TypedResults not recognized?**
- Make sure you're using .NET 7 or later
- Import `Microsoft.AspNetCore.Http.HttpResults`

**MapGroup not organizing properly?**
- Verify you're calling methods on the group variable, not `app`
- Check that route templates don't conflict

This advanced implementation provides a solid foundation for production-ready APIs with proper organization, type safety, and security measures.