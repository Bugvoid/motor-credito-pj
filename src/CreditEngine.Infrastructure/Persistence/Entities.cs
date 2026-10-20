namespace CreditEngine.Infrastructure.Persistence;

public sealed class CreditRequestEntity
{
    public Guid Id { get; set; }
    public required string Cnpj { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreditDecisionEntity
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public required string Status { get; set; }
    public required string Rating { get; set; }
    public decimal? SpreadAnnual { get; set; }
    public decimal? AnnualRate { get; set; }
    public decimal? MonthlyRate { get; set; }
    public decimal? CetAnnual { get; set; }
    public decimal? Iof { get; set; }
    public required string PolicyVersion { get; set; }
    public required string RuleResults { get; set; }
    public string? Schedule { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public required string Type { get; set; }
    public required string Payload { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public int Attempts { get; set; }
}
