using System.Text.Json;
using CreditEngine.Domain;
using CreditEngine.Domain.Policy;
using CreditEngine.Infrastructure.Enrichment;
using CreditEngine.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CreditEngine.Api.Endpoints;

public static class CreditRequestEndpoints
{
    public static void MapCreditRequestEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/credit-requests", Create);
        app.MapGet("/credit-requests/{id:guid}", Get);
        app.MapGet("/policies/current", (IOptions<CreditOptions> opt) => Results.Ok(new
        {
            version = CreditPolicy.Version,
            rules = CreditPolicy.Rules.Select(r => r.GetType().Name),
            cdiAnnual = opt.Value.CdiAnnual,
        }));
    }

    private static async Task<IResult> Create(
        CreditRequestDto dto, ICompanyRegistry registry, ICreditBureau bureau, CreditDbContext db,
        IOptions<CreditOptions> credit, IOptions<JsonOptions> json, TimeProvider time, ILogger<CreditRequestDto> log,
        CancellationToken ct)
    {
        var (app, errors) = dto.Validate();
        if (app is null) return Results.ValidationProblem(errors);

        var company = await registry.FindAsync(app.Cnpj, ct);
        var now = time.GetUtcNow();
        var enriched = new EnrichedApplication(app, company, bureau.Score(app.Cnpj), DateOnly.FromDateTime(now.UtcDateTime));
        var decision = CreditDecisionEngine.Decide(enriched, credit.Value.CdiAnnual);

        var opts = json.Value.SerializerOptions;
        var id = Guid.CreateVersion7(now);
        var result = new CreditDecisionDto(id, app.Cnpj.Digits, decision.Status, decision.Rating, decision.PolicyVersion,
            decision.Rules, decision.Offer, now);

        db.CreditRequests.Add(new CreditRequestEntity
        {
            Id = id, Cnpj = app.Cnpj.Digits, Payload = JsonSerializer.Serialize(dto, opts), CreatedAt = now,
        });
        db.CreditDecisions.Add(new CreditDecisionEntity
        {
            Id = Guid.CreateVersion7(now),
            RequestId = id,
            Status = decision.Status.ToString(),
            Rating = decision.Rating.ToString(),
            SpreadAnnual = decision.Offer?.SpreadAnnual,
            AnnualRate = decision.Offer?.AnnualRate,
            MonthlyRate = decision.Offer?.MonthlyRate,
            CetAnnual = decision.Offer?.CetAnnual,
            Iof = decision.Offer?.Iof,
            PolicyVersion = decision.PolicyVersion,
            RuleResults = JsonSerializer.Serialize(decision.Rules, opts),
            Schedule = decision.Offer is null ? null : JsonSerializer.Serialize(decision.Offer.Schedule, opts),
            CreatedAt = now,
        });
        // Outbox: o evento entra na mesma transação da decisão; o OutboxPublisher envia ao SQS depois.
        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.CreateVersion7(now), Type = "CreditDecisionMade", Payload = JsonSerializer.Serialize(result, opts), CreatedAt = now,
        });
        await db.SaveChangesAsync(ct);

        log.LogInformation("Decisão {RequestId} {Status} rating {Rating}", id, decision.Status, decision.Rating);
        return Results.Created($"/credit-requests/{id}", result);
    }

    private static async Task<IResult> Get(Guid id, CreditDbContext db, IOptions<JsonOptions> json, CancellationToken ct)
    {
        var row = await (from r in db.CreditRequests
                         join dec in db.CreditDecisions on r.Id equals dec.RequestId
                         where r.Id == id
                         select new { r.Cnpj, Decision = dec }).AsNoTracking().SingleOrDefaultAsync(ct);
        if (row is null) return Results.NotFound();

        var opts = json.Value.SerializerOptions;
        var d = row.Decision;
        var rating = Enum.Parse<Rating>(d.Rating);
        var offer = d.Schedule is null
            ? null
            : new CreditOffer(rating, d.SpreadAnnual!.Value, d.AnnualRate!.Value, d.MonthlyRate!.Value, d.Iof!.Value,
                d.CetAnnual!.Value, JsonSerializer.Deserialize<List<Installment>>(d.Schedule, opts)!);

        return Results.Ok(new CreditDecisionDto(id, row.Cnpj, Enum.Parse<DecisionStatus>(d.Status), rating, d.PolicyVersion,
            JsonSerializer.Deserialize<List<RuleResult>>(d.RuleResults, opts)!, offer, d.CreatedAt));
    }
}
