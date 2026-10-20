namespace CreditEngine.Domain.Policy;

public abstract class Rule : IEligibilityRule
{
    public abstract RuleResult Evaluate(EnrichedApplication a);

    protected RuleResult Result(bool passed, string reason) => new(GetType().Name, passed, reason);
}

public sealed class ActiveRegistration : Rule
{
    public override RuleResult Evaluate(EnrichedApplication a) => a.Company?.RegistrationStatus switch
    {
        null => Result(false, "dados cadastrais indisponíveis"),
        "INEXISTENTE" => Result(false, "CNPJ não encontrado"),
        "ATIVA" => Result(true, "situação cadastral ATIVA"),
        var s => Result(false, $"situação cadastral {s}"),
    };
}

public sealed class MinimumAge : Rule
{
    public override RuleResult Evaluate(EnrichedApplication a)
    {
        if (a.Company is null) return Result(false, "dados cadastrais indisponíveis");
        var passed = a.Company.OpenedOn <= a.Today.AddYears(-2);
        return Result(passed, $"aberta em {a.Company.OpenedOn:yyyy-MM-dd} (mínimo 2 anos)");
    }
}

public sealed class RevenueRange : Rule
{
    public const decimal Min = 4_800_000m, Max = 300_000_000m;

    public override RuleResult Evaluate(EnrichedApplication a)
    {
        var r = a.Application.AnnualRevenue;
        return Result(r is >= Min and <= Max, $"faturamento {r:N2} (faixa {Min:N0} a {Max:N0})");
    }
}

public sealed class AmountToRevenue : Rule
{
    public const decimal MaxShare = 0.30m;

    public override RuleResult Evaluate(EnrichedApplication a)
    {
        var limit = a.Application.AnnualRevenue * MaxShare;
        return Result(a.Application.RequestedAmount <= limit, $"valor {a.Application.RequestedAmount:N2} (limite 30% = {limit:N2})");
    }
}

public sealed class TermRange : Rule
{
    public const int Min = 6, Max = 60;

    public override RuleResult Evaluate(EnrichedApplication a) =>
        Result(a.Application.TermMonths is >= Min and <= Max, $"prazo {a.Application.TermMonths} meses (6 a 60)");
}

public sealed class MinimumBureauScore : Rule
{
    public const int Min = 300;

    public override RuleResult Evaluate(EnrichedApplication a) =>
        Result(a.BureauScore >= Min, $"score {a.BureauScore} (mínimo {Min})");
}

public sealed class PostLoanLeverage : Rule
{
    public const decimal Max = 4.5m;

    public override RuleResult Evaluate(EnrichedApplication a)
    {
        var app = a.Application;
        if (app.Ebitda <= 0) return Result(false, "EBITDA não positivo");
        var leverage = (app.NetDebt + app.RequestedAmount) / app.Ebitda;
        return Result(leverage <= Max, $"alavancagem pós-crédito {leverage:N2}x (máximo {Max}x)");
    }
}

public sealed class RatingNotE : Rule
{
    public override RuleResult Evaluate(EnrichedApplication a)
    {
        var app = a.Application;
        var rating = RatingCalculator.Calculate(app.NetDebt, app.Ebitda, app.AnnualRevenue, a.BureauScore);
        return Result(rating != Rating.E, $"rating {rating}");
    }
}
