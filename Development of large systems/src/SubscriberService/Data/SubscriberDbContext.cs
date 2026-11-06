using Microsoft.EntityFrameworkCore;

namespace SubscriberService.Data;

public class SubscriberDbContext : DbContext
{
    public SubscriberDbContext(
        DbContextOptions<SubscriberDbContext> options) : base(options)
    {
    }

    public DbSet<Subscriber> Subscribers => Set<Subscriber>();
}