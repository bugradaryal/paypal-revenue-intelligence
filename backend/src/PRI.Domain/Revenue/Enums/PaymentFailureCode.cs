namespace PRI.Domain.Revenue.Enums;

public enum PaymentFailureCode
{
    CardDeclined = 1,
    InsufficientFunds = 2,
    ExpiredCard = 3,
    AccountRestricted = 4,
    Other = 5
}
