using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Entities;

namespace RevenuePilot.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityActivity> OpportunityActivities => Set<OpportunityActivity>();

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
            e.HasAlternateKey(x => new { x.BusinessId, x.Id });
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
            e.HasIndex(x => new { x.BusinessId, x.ExternalOrderId }).IsUnique();
            e.HasIndex(x => new { x.BusinessId, x.OrderDate });
            e.HasIndex(x => new { x.BusinessId, x.CustomerId, x.OrderDate });
            e.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Customer>().WithMany()
                .HasForeignKey(x => new { x.BusinessId, x.CustomerId })
                .HasPrincipalKey(x => new { x.BusinessId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Opportunity>(e =>
        {
            e.ToTable("opportunities", t =>
            {
                t.HasCheckConstraint("ck_opportunities_values_not_negative",
                    "\"EstimatedValue\" >= 0 AND \"RecoveredValue\" >= 0");
                // Recovered revenue can only exist on an opportunity whose status is Recovered.
                t.HasCheckConstraint("ck_opportunities_recovered_only_when_recovered",
                    "\"RecoveredValue\" = 0 OR \"Status\" = 'Recovered'");
            });
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Priority).HasMaxLength(20).IsRequired();
            e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.EstimatedValue).HasPrecision(18, 2);
            e.Property(x => x.RecoveredValue).HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
            e.HasAlternateKey(x => new { x.BusinessId, x.Id });
            e.HasIndex(x => new { x.BusinessId, x.Status });
            // One open opportunity per customer, enforced by the database as well as the code.
            e.HasIndex(x => new { x.BusinessId, x.CustomerId })
                .IsUnique()
                .HasFilter("\"Status\" IN ('New', 'InProgress')");
            e.HasOne<Business>().WithMany().HasForeignKey(x => x.BusinessId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Customer>().WithMany()
                .HasForeignKey(x => new { x.BusinessId, x.CustomerId })
                .HasPrincipalKey(x => new { x.BusinessId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<OpportunityActivity>(e =>
        {
            e.ToTable("opportunity_activities");
            e.Property(x => x.FromStatus).HasMaxLength(20);
            e.Property(x => x.ToStatus).HasMaxLength(20).IsRequired();
            e.Property(x => x.Note).HasMaxLength(500);
            e.Property(x => x.RecoveredValue).HasPrecision(18, 2);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
            e.HasIndex(x => new { x.BusinessId, x.OpportunityId, x.CreatedAt });
            e.HasOne<Opportunity>().WithMany()
                .HasForeignKey(x => new { x.BusinessId, x.OpportunityId })
                .HasPrincipalKey(x => new { x.BusinessId, x.Id })
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}