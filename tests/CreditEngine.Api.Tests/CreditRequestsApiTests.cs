using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CreditEngine.Domain;
using Microsoft.EntityFrameworkCore;

namespace CreditEngine.Api.Tests;

public class CreditRequestsApiTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    private static object Body(string cnpj = "11222333000181", int termMonths = 24) => new
    {
        cnpj, annualRevenue = 50_000_000m, ebitda = 10_000_000m, netDebt = 10_000_000m,
        requestedAmount = 5_000_000m, termMonths, collateral = "Receivables", amortization = "Price",
    };

    private async Task<(HttpResponseMessage Resp, JsonElement Json)> Post(object body)
    {
        var resp = await api.CreateClient().PostAsJsonAsync("/credit-requests", body);
        return (resp, await resp.Content.ReadFromJsonAsync<JsonElement>());
    }

    [Fact] public async Task Approved_persists_request_decision_and_outbox()
    {
        api.Registry.Respond = _ => new CompanyProfile("ATIVA", new DateOnly(2015, 1, 1), "6201501");
        var (resp, json) = await Post(Body());

        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        Assert.Equal("Approved", json.GetProperty("status").GetString());
        var id = json.GetProperty("id").GetGuid();
        Assert.Equal($"/credit-requests/{id}", resp.Headers.Location!.OriginalString);

        await using var db = api.Db();
        Assert.True(await db.CreditRequests.AnyAsync(r => r.Id == id));
        Assert.True(await db.CreditDecisions.AnyAsync(d => d.RequestId == id));
        var outbox = (await db.OutboxMessages.AsNoTracking().ToListAsync()).Where(m => m.Payload.Contains(id.ToString())).ToList();
        var msg = Assert.Single(outbox);
        Assert.Equal("CreditDecisionMade", msg.Type);
        Assert.Null(msg.SentAt);
    }

    [Fact] public async Task Registry_unavailable_is_referred()
    {
        api.Registry.Respond = _ => null;
        var (_, json) = await Post(Body());
        Assert.Equal("Referred", json.GetProperty("status").GetString());
    }

    [Fact] public async Task Unknown_cnpj_is_rejected_with_reason()
    {
        api.Registry.Respond = _ => new CompanyProfile("INEXISTENTE", default, "");
        var (_, json) = await Post(Body());
        Assert.Equal("Rejected", json.GetProperty("status").GetString());
        var rule = json.GetProperty("rules").EnumerateArray().Single(r => r.GetProperty("name").GetString() == "ActiveRegistration");
        Assert.Equal("CNPJ não encontrado", rule.GetProperty("reason").GetString());
    }

    [Theory]
    [InlineData("11111111111111", 24, "cnpj")]
    [InlineData("11222333000181", 0, "termMonths")]
    public async Task Invalid_input_is_400_with_field(string cnpj, int term, string field)
    {
        var (resp, json) = await Post(Body(cnpj, term));
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.True(json.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact] public async Task Invalid_enum_is_400()
    {
        var (resp, json) = await Post(new { cnpj = "11222333000181", annualRevenue = 1m, ebitda = 1m, netDebt = 0m, requestedAmount = 1m, termMonths = 12, collateral = "Ouro", amortization = "Price" });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        Assert.True(json.GetProperty("errors").TryGetProperty("collateral", out _));
    }

    [Fact] public async Task Get_returns_trail_and_404_for_unknown()
    {
        api.Registry.Respond = _ => new CompanyProfile("ATIVA", new DateOnly(2015, 1, 1), "6201501");
        var (_, created) = await Post(Body());
        var client = api.CreateClient();

        var got = await client.GetFromJsonAsync<JsonElement>($"/credit-requests/{created.GetProperty("id").GetGuid()}");
        Assert.Equal(8, got.GetProperty("rules").GetArrayLength());
        Assert.Equal("2026.10.1", got.GetProperty("policyVersion").GetString());
        Assert.Equal(24, got.GetProperty("offer").GetProperty("schedule").GetArrayLength());

        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/credit-requests/{Guid.NewGuid()}")).StatusCode);
    }

    [Fact] public async Task Policy_and_health()
    {
        var client = api.CreateClient();
        var policy = await client.GetFromJsonAsync<JsonElement>("/policies/current");
        Assert.Equal("2026.10.1", policy.GetProperty("version").GetString());
        Assert.Equal(8, policy.GetProperty("rules").GetArrayLength());
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/health")).StatusCode);
    }
}
