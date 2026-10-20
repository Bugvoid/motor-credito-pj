using Microsoft.EntityFrameworkCore;

namespace CreditEngine.Infrastructure.Persistence;

public sealed class CreditDbContext(DbContextOptions<CreditDbContext> options) : DbContext(options)
{
    public DbSet<CreditRequestEntity> CreditRequests => Set<CreditRequestEntity>();
    public DbSet<CreditDecisionEntity> CreditDecisions => Set<CreditDecisionEntity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CreditRequestEntity>(e =>
        {
            e.ToTable("credit_requests");
            e.Property(x => x.Cnpj).HasMaxLength(14);
            e.Property(x => x.Payload).HasColumnType("jsonb");
        });
        b.Entity<CreditDecisionEntity>(e =>
        {
            e.ToTable("credit_decisions");
            e.HasIndex(x => x.RequestId).IsUnique();
            e.HasOne<CreditRequestEntity>().WithOne().HasForeignKey<CreditDecisionEntity>(x => x.RequestId);
            e.Property(x => x.RuleResults).HasColumnType("jsonb");
            e.Property(x => x.Schedule).HasColumnType("jsonb");
            e.Property(x => x.SpreadAnnual).HasPrecision(12, 8);
            e.Property(x => x.AnnualRate).HasPrecision(12, 8);
            e.Property(x => x.MonthlyRate).HasPrecision(12, 8);
            e.Property(x => x.CetAnnual).HasPrecision(12, 8);
            e.Property(x => x.Iof).HasPrecision(18, 2);
        });
        b.Entity<OutboxMessage>(e =>
        {
            e.ToTable("outbox_messages");
            e.Property(x => x.Payload).HasColumnType("jsonb");
            e.HasIndex(x => x.CreatedAt).HasFilter("sent_at IS NULL");
        });
    }
}
