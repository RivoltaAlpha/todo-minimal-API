# Complete Guide: Building a Todo API with ASP.NET Core Minimal APIs

This tutorial walks you through creating a simple Todo API using ASP.NET Core's minimal API approach. Perfect for beginners who want to learn API development with minimal setup and maximum clarity.

## What You'll Build

By the end of this tutorial, you'll have a fully functional Todo API that can:
- ✅ Create new todo items
- ✅ Read/list all todos
- ✅ Update existing todos
- ✅ Delete todos
- ✅ Filter completed todos
- ✅ Include API documentation with Swagger

## Prerequisites

- [.NET 6 SDK or later](https://dotnet.microsoft.com/download)
- [Visual Studio Code](https://code.visualstudio.com/) (recommended)
- Basic knowledge of C# (helpful but not required)

## Step 1: Create Your Project

Open your terminal/command prompt and run these commands:

```bash
# Create a new minimal web API project
dotnet new web -o TodoApi

# Navigate into the project folder
cd TodoApi

# Open in VS Code (optional)
code .
```

**What this does:** Creates a new ASP.NET Core web project with minimal dependencies in a folder called `TodoApi`.

## Step 2: Trust Development Certificates

```bash
dotnet dev-certs https --trust
```

Select **Yes** when prompted to trust the development certificate. This allows HTTPS during development.

## Step 3: Install Required Packages

Add the necessary NuGet packages for database and diagnostics:

```bash
# Add official NuGet source (if needed)
dotnet nuget add source https://api.nuget.org/v3/index.json -n nuget.org

# Add Entity Framework for in-memory database
dotnet add package Microsoft.EntityFrameworkCore.InMemory

# Add database diagnostics
dotnet add package Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore

# Add Swagger for API documentation
dotnet add package NSwag.AspNetCore
```

**What these packages do:**
- `EntityFrameworkCore.InMemory`: Provides an in-memory database for testing
- `Diagnostics.EntityFrameworkCore`: Shows helpful database error pages
- `NSwag.AspNetCore`: Generates API documentation automatically

## Step 4: Create the Data Model

Create a new file called `Todo.cs` in your project folder:

```csharp
public class Todo
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}
```

**What this is:** A simple class that represents a Todo item with an ID, name, and completion status.

## Step 5: Create the Database Context

Create a new file called `TodoDb.cs`:

```csharp
using Microsoft.EntityFrameworkCore;

class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
}
```

**What this is:** The database context that Entity Framework uses to manage your Todo data. It's like a bridge between your code and the database.

## Step 6: Build Your API Endpoints

Replace the contents of `Program.cs` with this complete API:

```csharp
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddDbContext<TodoDb>(opt => opt.UseInMemoryDatabase("TodoList"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

var app = builder.Build();

// GET /todoitems - Get all todos
app.MapGet("/todoitems", async (TodoDb db) =>
    await db.Todos.ToListAsync());

// GET /todoitems/complete - Get only completed todos
app.MapGet("/todoitems/complete", async (TodoDb db) =>
    await db.Todos.Where(t => t.IsComplete).ToListAsync());

// GET /todoitems/{id} - Get a specific todo by ID
app.MapGet("/todoitems/{id}", async (int id, TodoDb db) =>
    await db.Todos.FindAsync(id)
        is Todo todo
            ? Results.Ok(todo)
            : Results.NotFound());

// POST /todoitems - Create a new todo
app.MapPost("/todoitems", async (Todo todo, TodoDb db) =>
{
    db.Todos.Add(todo);
    await db.SaveChangesAsync();
    return Results.Created($"/todoitems/{todo.Id}", todo);
});

// PUT /todoitems/{id} - Update an existing todo
app.MapPut("/todoitems/{id}", async (int id, Todo inputTodo, TodoDb db) =>
{
    var todo = await db.Todos.FindAsync(id);
    if (todo is null) return Results.NotFound();

    todo.Name = inputTodo.Name;
    todo.IsComplete = inputTodo.IsComplete;
    await db.SaveChangesAsync();

    return Results.NoContent();
});

// DELETE /todoitems/{id} - Delete a todo
app.MapDelete("/todoitems/{id}", async (int id, TodoDb db) =>
{
    if (await db.Todos.FindAsync(id) is Todo todo)
    {
        db.Todos.Remove(todo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    return Results.NotFound();
});

app.Run();
```

## Step 7: Run Your API

Start your API server:

```bash
dotnet 
dotnet run --launch-profile https
```

You should see output similar to:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7xxx
      Now listening on: http://localhost:5xxx
```

## Step 8: Test Your API

You can test your API using several methods:

### Option 1: Using swagger ([using browser page](https://localhost:7208/swagger/index.html))
In the request body enter JSON for a to-do item, without specifying the optional id and execute:

```JSON
{
  "name":"walk dog",
  "isComplete":true
}
```

### Option 2: Using your browser
- Navigate to `https://localhost:7xxx/todoitems` to see all todos
- Navigate to `https://localhost:7xxx/todoitems/complete` to see completed todos

### Option 3: Using VS Code REST Client
Install the REST Client extension and create a file called `test.http`:

```http
### Create a new todo
POST https://localhost:7xxx/todoitems
Content-Type: application/json

{
  "name": "Learn Minimal APIs",
  "isComplete": false
}

### Get all todos
GET https://localhost:7xxx/todoitems

### Get completed todos only
GET https://localhost:7xxx/todoitems/complete
```

## Understanding Your API Endpoints

| Method | Endpoint | Description | Example Request Body |
|--------|----------|-------------|---------------------|
| GET | `/todoitems` | Get all todos | - |
| GET | `/todoitems/complete` | Get completed todos | - |
| GET | `/todoitems/{id}` | Get todo by ID | - |
| POST | `/todoitems` | Create new todo | `{"name":"New task","isComplete":false}` |
| PUT | `/todoitems/{id}` | Update existing todo | `{"name":"Updated task","isComplete":true}` |
| DELETE | `/todoitems/{id}` | Delete todo | - |

## Common HTTP Status Codes You'll See

- **200 OK**: Request successful
- **201 Created**: New resource created successfully
- **204 No Content**: Request successful, no content to return
- **404 Not Found**: Resource not found

## Next Steps

Now that you have a working API, consider these improvements:

1. **Add Validation**: Ensure todo names aren't empty
2. **Add Authentication**: Secure your API with user authentication
3. **Use Real Database**: Replace in-memory database with SQL Server or PostgreSQL
4. **Add Logging**: Track what happens in your API
5. **Add Unit Tests**: Test your API endpoints
6. **Deploy to Cloud**: Host your API on Azure, AWS, or other cloud platforms

## Troubleshooting

**Port already in use?**
- Stop the application with `Ctrl+C` and run `dotnet run` again

**Certificate issues?**
- Run `dotnet dev-certs https --trust` again

**Package restore issues?**
- Run `dotnet restore` to restore all packages

**Can't connect to API?**
- Check the console output for the correct port number
- Ensure you're using `https://` not `http://`

## Project Structure

Your final project should look like this:
```
TodoApi/
├── Program.cs          # Main application and API endpoints
├── Todo.cs            # Todo data model
├── TodoDb.cs          # Database context
├── TodoApi.csproj     # Project file with dependencies
└── Properties/
    └── launchSettings.json
```

## What You Learned

- ✅ How to create a minimal API with ASP.NET Core
- ✅ Setting up Entity Framework with in-memory database
- ✅ Creating CRUD (Create, Read, Update, Delete) operations
- ✅ Understanding HTTP methods and status codes
- ✅ Testing APIs with different tools
- ✅ Project structure and file organization

Great job! You've built your first ASP.NET Core API. This foundation will serve you well as you build more complex applications.