using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddDbContext<AdvancedDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Add OpenAPI/Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUi();
    app.UseSwaggerUi();
}

// Create a group for todo endpoints
var todoItems = app.MapGroup("/api/todoitems")
    .WithTags("Todos")
    .WithDescription("Operations for managing todo items")
    .WithOpenApi();

// GET /api/todoitems - Get all todos (not deleted)
todoItems.MapGet("/", async (AdvancedDb db) =>
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
todoItems.MapGet("/complete", async (AdvancedDb db) =>
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
todoItems.MapGet("/{id:int}", async (int id, AdvancedDb db) =>
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
todoItems.MapPost("/", async (CreateTodoRequest request, AdvancedDb db) =>
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
todoItems.MapPut("/{id:int}", async (int id, UpdateTodoRequest request, AdvancedDb db) =>
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
todoItems.MapDelete("/{id:int}", async (int id, AdvancedDb db) =>
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