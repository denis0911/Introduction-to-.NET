using Microsoft.EntityFrameworkCore;
using Order_Management_API.Features.Orders;


namespace Order_Management_API.Data;

public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            
            entity.Property(e => e.Author)
                .IsRequired()
                .HasMaxLength(100);
            
            entity.Property(e => e.Isbn)
                .IsRequired()
                .HasMaxLength(20);
            
            entity.HasIndex(e => e.Isbn)
                .IsUnique();
            
            entity.Property(e => e.Price)
                .HasPrecision(18,2);
            
            entity.Property(e => e.Category)
                .IsRequired();
            
            entity.Property(e => e.CoverImageUrl)
                .HasMaxLength(500);
        });
    }
}