using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CreditEngine.Infrastructure.Messaging;
using CreditEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CreditEngine.Api.Tests;

public class OutboxPublisherTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private sealed class FakeSqs() : AmazonSQSClient(new AnonymousAWSCredentials(), new AmazonSQSConfig { ServiceURL = "http://localhost:1" })
    {
        public bool Fail { get; set; }
        public List<SendMessageRequest> Sent { get; } = [];

        public override Task<SendMessageResponse> SendMessageAsync(SendMessageRequest request, CancellationToken ct = default)
        {
            if (Fail) throw new AmazonSQSException("fila indisponível");
            Sent.Add(request);
            return Task.FromResult(new SendMessageResponse());
        }
    }

    [Fact] public async Task Failure_keeps_message_then_retry_sends_it()
    {
        var id = Guid.NewGuid();
        await using (var db = api.Db())
        {
            db.OutboxMessages.Add(new OutboxMessage { Id = id, Type = "CreditDecisionMade", Payload = """{"id":"x"}""", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
        }

        var sqs = new FakeSqs { Fail = true };
        var publisher = new OutboxPublisher(api.Services.GetRequiredService<IServiceScopeFactory>(), sqs,
            Options.Create(new SqsOptions { QueueUrl = "http://q" }), NullLogger<OutboxPublisher>.Instance, TimeProvider.System);

        Assert.Equal(0, await publisher.PublishBatchAsync(CancellationToken.None));
        await using (var db = api.Db())
        {
            var m = await db.OutboxMessages.AsNoTracking().SingleAsync(x => x.Id == id);
            Assert.Null(m.SentAt);
            Assert.Equal(1, m.Attempts);
        }

        sqs.Fail = false;
        Assert.Equal(1, await publisher.PublishBatchAsync(CancellationToken.None));
        await using (var db = api.Db())
            Assert.NotNull((await db.OutboxMessages.AsNoTracking().SingleAsync(x => x.Id == id)).SentAt);

        var body = Assert.Single(sqs.Sent).MessageBody;
        Assert.Contains("\"type\":\"CreditDecisionMade\"", body);
        Assert.Equal(0, await publisher.PublishBatchAsync(CancellationToken.None)); // nada reenviado
    }
}
