using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Entities;

namespace RevenuePilot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Business>(e =>
        {
            e.ToTable("businesses");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.TimeZoneId).HasMaxLength(64).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        });

        b.Entity<Customer>(e =>
        {
            e.ToTable("customers");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Segment).HasMaxLength(100);
            e.Property(x => x.ExternalCustomerId).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasAlternateKey(x => new { x.BusinessId, x.Id });          // lets orders reference (business, customer) together
            e.HasIndex(x => new { x.BusinessId, x.ExternalCustomerId }).IsUnique();
            e.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<Order>(e =>
        {
            e.ToTable("orders");
            e.Property(x => x.ExternalOrderId).HasMaxLength(100).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.TotalAmount).HasPrecision(18, 2);
            e.Property(x => x.RefundAmount).HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.BusinessId, x.ExternalOrderId }).IsUnique();   // makes re-imports idempotent
            e.HasIndex(x => new { x.BusinessId, x.OrderDate });
            e.HasIndex(x => new { x.BusinessId, x.CustomerId, x.OrderDate });
            e.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
            // composite key: an order can never point at another business's customer
            e.HasOne<Customer>().WithMany()
                .HasForeignKey(x => new { x.BusinessId, x.CustomerId })
                .HasPrincipalKey(x => new { x.BusinessId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}