using Microsoft.EntityFrameworkCore;
using Todo.Shared.Entities;

namespace Todo.Shared.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoEntity>().HasIndex(t => t.Name).IsUnique();
    }

    public virtual DbSet<TodoEntity> Todos { get; init; } = null!;
    public virtual DbSet<UserEntity> Users { get; init; } = null!;
}
