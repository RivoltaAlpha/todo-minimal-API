using Microsoft.EntityFrameworkCore;

// A DbContext is a class that manages database connections and operations for your models. 
// It’s like a bridge between your code and the database. You define which models (tables) it manages:
class TodoDb : DbContext
{
    public TodoDb(DbContextOptions<TodoDb> options)
        : base(options) { }

    public DbSet<Todo> Todos => Set<Todo>();
    // DbSet<TodoItem> means you want to store and query TodoItem objects in the database.
}