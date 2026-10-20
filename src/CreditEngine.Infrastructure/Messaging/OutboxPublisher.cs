using Amazon.SQS;
using Amazon.SQS.Model;
using CreditEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CreditEngine.Infrastructure.Messaging;

public sealed class SqsOptions
{
    public string? ServiceUrl { get; set; }
    public string QueueUrl { get; set; } = "";
}

/// <summary>
/// Publica no SQS as mensagens gravadas no outbox. Entrega pelo menos uma vez:
/// o consumidor deve ser idempotente pelo id da decisão.
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopes, IAmazonSQS sqs, IOptions<SqsOptions> options, ILogger<OutboxPublisher> log, TimeProvider time)
    : BackgroundService
{
    private const int BatchSize = 10;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2), time);
        do
        {
            try
            {
                await PublishBatchAsync(ct);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                log.LogError(e, "Falha ao ler o outbox");
            }
        } while (await timer.WaitForNextTickAsync(ct));
    }

    public async Task<int> PublishBatchAsync(CancellationToken ct)
    {
        using var scope = scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditDbContext>();
        var batch = await db.OutboxMessages
            .Where(m => m.SentAt == null)
            .OrderBy(m => m.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(ct);

        var sent = 0;
        foreach (var m in batch)
        {
            try
            {
                var body = $$"""{"type":"{{m.Type}}","data":{{m.Payload}}}""";
                await sqs.SendMessageAsync(new SendMessageRequest(options.Value.QueueUrl, body), ct);
                m.SentAt = time.GetUtcNow();
                sent++;
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                m.Attempts++;
                log.LogWarning(e, "Falha ao publicar {MessageId} (tentativa {Attempts})", m.Id, m.Attempts);
            }
        }

        await db.SaveChangesAsync(ct);
        return sent;
    }
}
