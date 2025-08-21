using Microsoft.EntityFrameworkCore;

class AdvancedDb : DbContext
{
    public AdvancedDb(DbContextOptions<AdvancedDb> options)
        : base(options) { }

    public DbSet<AdvancedTodo> AdvancedTodos => Set<AdvancedTodo>();
}