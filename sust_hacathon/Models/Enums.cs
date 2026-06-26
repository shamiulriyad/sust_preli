namespace QueueStormInvestigator.Models;



public static class CaseType
{
    public const string WrongTransfer = "wrong_transfer";
    public const string PaymentFailed = "payment_failed";
    public const string RefundRequest = "refund_request";
    public const string DuplicatePayment = "duplicate_payment";
    public const string MerchantSettlementDelay = "merchant_settlement_delay";
    public const string AgentCashInIssue = "agent_cash_in_issue";
    public const string PhishingOrSocialEngineering = "phishing_or_social_engineering";
    public const string Other = "other";

    public static readonly HashSet<string> All = new()
    {
        WrongTransfer, PaymentFailed, RefundRequest, DuplicatePayment,
        MerchantSettlementDelay, AgentCashInIssue, PhishingOrSocialEngineering, Other
    };
}

public static class Department
{
    public const string CustomerSupport = "customer_support";
    public const string DisputeResolution = "dispute_resolution";
    public const string PaymentsOps = "payments_ops";
    public const string MerchantOperations = "merchant_operations";
    public const string AgentOperations = "agent_operations";
    public const string FraudRisk = "fraud_risk";

    public static readonly HashSet<string> All = new()
    {
        CustomerSupport, DisputeResolution, PaymentsOps,
        MerchantOperations, AgentOperations, FraudRisk
    };

   
    public static string DefaultFor(string caseType) => caseType switch
    {
        CaseType.WrongTransfer => DisputeResolution,
        CaseType.PaymentFailed => PaymentsOps,
        CaseType.RefundRequest => DisputeResolution,
        CaseType.DuplicatePayment => PaymentsOps,
        CaseType.MerchantSettlementDelay => MerchantOperations,
        CaseType.AgentCashInIssue => AgentOperations,
        CaseType.PhishingOrSocialEngineering => FraudRisk,
        _ => CustomerSupport
    };


    public static string DefaultFor(string caseType, string evidenceVerdict)
    {
        if (caseType == CaseType.RefundRequest)
        {
            return evidenceVerdict == EvidenceVerdict.Inconsistent ? DisputeResolution : CustomerSupport;
        }
        return DefaultFor(caseType);
    }
}

public static class EvidenceVerdict
{
    public const string Consistent = "consistent";
    public const string Inconsistent = "inconsistent";
    public const string InsufficientData = "insufficient_data";

    public static readonly HashSet<string> All = new() { Consistent, Inconsistent, InsufficientData };
}

public static class Severity
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Critical = "critical";

    public static readonly HashSet<string> All = new() { Low, Medium, High, Critical };
}

public static class TransactionStatus
{
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Pending = "pending";
    public const string Reversed = "reversed";
}