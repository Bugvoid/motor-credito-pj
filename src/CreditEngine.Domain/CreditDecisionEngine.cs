using CreditEngine.Domain.Policy;

namespace CreditEngine.Domain;

public enum DecisionStatus { Approved, Rejected, Referred }

public sealed record CreditOffer(
    Rating Rating,
    decimal SpreadAnnual,
    decimal AnnualRate,
    decimal MonthlyRate,
    decimal Iof,
    decimal CetAnnual,
    IReadOnlyList<Installment> Schedule);

public sealed record CreditDecision(
    DecisionStatus Status,
    string PolicyVersion,
    Rating Rating,
    IReadOnlyList<RuleResult> Rules,
    CreditOffer? Offer);

public static class CreditDecisionEngine
{
    public static CreditDecision Decide(EnrichedApplication a, decimal cdiAnnual)
    {
        var app = a.Application;
        var rules = CreditPolicy.Evaluate(a);
        var rating = RatingCalculator.Calculate(app.NetDebt, app.Ebitda, app.AnnualRevenue, a.BureauScore);

        // Sem cadastro nunca há aprovação automática: vai para análise manual.
        if (a.Company is null) return new(DecisionStatus.Referred, CreditPolicy.Version, rating, rules, null);
        if (rules.Any(r => !r.Passed)) return new(DecisionStatus.Rejected, CreditPolicy.Version, rating, rules, null);

        var spread = Pricing.SpreadAnnual(rating, app.TermMonths, app.Collateral);
        var annual = Pricing.AnnualRate(cdiAnnual, spread);
        var monthly = Pricing.MonthlyRate(annual);
        var schedule = Schedule.Build(app.RequestedAmount, monthly, app.TermMonths, app.Amortization);
        var iof = CreditIof.Calculate(schedule, app.RequestedAmount);
        var cet = Cet.Annual(app.RequestedAmount - iof, schedule);

        return new(DecisionStatus.Approved, CreditPolicy.Version, rating, rules,
            new CreditOffer(rating, spread, annual, monthly, iof, cet, schedule));
    }
}
