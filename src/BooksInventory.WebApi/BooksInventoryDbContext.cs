using Microsoft.EntityFrameworkCore;

namespace BooksInventory.WebApi;

public class BooksInventoryDbContext : DbContext
{
    public DbSet<Book> Books { get; set; }

    public BooksInventoryDbContext(DbContextOptions<BooksInventoryDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasKey(u => u.Id);
        modelBuilder.Entity<Book>()
            .HasIndex(u => u.Title)
            .IsUnique();
    }
}