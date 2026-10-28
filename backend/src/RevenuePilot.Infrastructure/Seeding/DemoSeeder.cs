using Microsoft.EntityFrameworkCore;
using RevenuePilot.Domain.Entities;
using RevenuePilot.Infrastructure.Persistence;

namespace RevenuePilot.Infrastructure.Seeding;

/// <summary>Creates a fictional business with synthetic customers. Dates are relative to "now".</summary>
public static class DemoSeeder
{
    private static readonly string[] Names =
    {
        "Karoo Logistics", "Bluegum Dental", "Ndlovu Accounting", "Protea Print Co", "Jacaranda Studio",
        "Highveld Fitness", "Baobab Legal", "Fynbos Foods", "Tshwane Tutoring", "Marula Marketing",
        "Sable Security", "Kudu Cleaning", "Impala Interiors", "Springbok Signs", "Mopane Mechanics",
        "Lekker Laundry", "Rooibos Realty", "Mamba IT Support", "Gecko Gardens", "Zebra Couriers",
        "Aloe Wellness", "Hadeda Hire", "Cheetah Cycles", "Pangolin Pest Control", "Sandveld Stationers",
        "Waterberg Welding", "Kalahari Kitchens", "Overberg Optics", "Magaliesberg Mills",
        "Lowveld Logistics", "Cederberg Couriers", "Namaqua Nurseries"
    };
    private static readonly string[] Segments = { "Retainer", "Project", "Subscription" };

    public static async Task<string> SeedAsync(AppDbContext db, DateTime nowUtc, bool reseed, CancellationToken ct = default)
    {
        var existing = await db.Businesses.Where(b => b.IsDemo).Select(b => b.Id).ToListAsync(ct);
        if (existing.Count > 0 && !reseed)
            return "Demo data already exists. Run 'dotnet run -- reseed' to rebuild it.";

        foreach (var id in existing)
        {
            await db.Orders.Where(o => o.BusinessId == id).ExecuteDeleteAsync(ct);
            await db.Customers.Where(c => c.BusinessId == id).ExecuteDeleteAsync(ct);
            await db.Businesses.Where(b => b.Id == id).ExecuteDeleteAsync(ct);
        }

        var rnd = new Random(20261028); // fixed seed: same shape every time
        var business = new Business
        {
            Name = "Lekker Office Supplies (Demo)",
            Industry = "Office supplies",
            BusinessModel = "B2B repeat orders",
            Currency = "ZAR",
            TimeZoneId = "Africa/Johannesburg",
            IsDemo = true,
            CreatedAt = nowUtc
        };
        var customers = new List<Customer>();
        var orders = new List<Order>();

        for (var i = 0; i < Names.Length; i++)
        {
            var mode = i >= 29 ? "new" : i % 7 == 1 ? "declining" : i % 7 == 4 ? "churned" : "steady";
            var baseValue = 800m + rnd.Next(0, 8200);
            var gapDays = 14 + rnd.Next(0, 27);
            var start = nowUtc.AddDays(-(mode == "new" ? 55 : 240 + rnd.Next(0, 25)));
            var end = mode == "churned" ? nowUtc.AddDays(-(45 + rnd.Next(0, 60))) : nowUtc;

            var customer = new Customer
            {
                BusinessId = business.Id,
                ExternalCustomerId = $"DEMO-C{i + 1:D3}",
                Name = Names[i],
                Email = new string(Names[i].ToLowerInvariant().Where(char.IsLetter).ToArray()) + "@example.test",
                Segment = Segments[i % 3],
                CreatedAt = start
            };
            customers.Add(customer);

            var n = 0;
            for (var t = start; t <= end; t = t.AddDays(gapDays * (0.85 + rnd.NextDouble() * 0.3)))
            {
                n++;
                var amount = baseValue * (decimal)(0.9 + rnd.NextDouble() * 0.2);
                if (mode == "declining" && t > nowUtc.AddDays(-130)) amount *= 0.5m;
                amount = Math.Round(amount, 2);

                var roll = rnd.NextDouble();
                var status = roll < 0.04 ? OrderStatus.Failed : roll < 0.06 ? OrderStatus.Cancelled : OrderStatus.Paid;
                var refund = status == OrderStatus.Paid && rnd.NextDouble() < 0.05 ? Math.Round(amount * 0.3m, 2) : 0m;

                orders.Add(new Order
                {
                    BusinessId = business.Id,
                    CustomerId = customer.Id,
                    ExternalOrderId = $"DEMO-{i + 1:D3}-{n:D3}",
                    OrderDate = t,
                    Status = status,
                    TotalAmount = amount,
                    RefundAmount = refund,
                    Currency = "ZAR",
                    CreatedAt = nowUtc
                });
            }
        }

        db.Businesses.Add(business);
        db.Customers.AddRange(customers);
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync(ct);

        var paid = orders.Count(o => o.Status == OrderStatus.Paid);
        return $"Seeded '{business.Name}': {customers.Count} customers, {orders.Count} orders ({paid} paid).";
    }
}