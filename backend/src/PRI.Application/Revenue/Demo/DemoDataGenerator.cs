using PRI.Domain.Revenue.Entities;
using PRI.Domain.Revenue.Enums;

namespace PRI.Application.Revenue.Demo;

public sealed record DemoDataSet(
    IReadOnlyList<Customer> Customers,
    IReadOnlyList<Invoice> Invoices,
    IReadOnlyList<Payment> Payments);

/// <summary>
/// Açıkça "demo" etiketli (DataSource.Demo), deterministik örnek veri. PayPal Sandbox'ta gerçek başarısız ödeme veya vadesi
/// geçmiş fatura çıkmadığından Recovery/Insight ekranlarını doldurmak için kullanılır. Aynı (şimdi, seed) her zaman aynı veriyi üretir.
/// Senaryolar bilerek eklenir: bekleyen ödemeler, vadesi geçmiş faturalar, kurtarılmış kayıplar, her hata kodundan açık kayıp ve USD dışı ödeme.
/// </summary>
public static class DemoDataGenerator
{
    public const int DefaultSeed = 20261110;

    private static readonly string[] CustomerNames =
    [
        "Acme Studio", "Northwind Traders", "Globex Labs", "Initech Software", "Umbrella Retail", "Stark Fitness",
        "Wayne Logistics", "Hooli Media", "Soylent Foods", "Vandelay Imports", "Pied Piper Cloud", "Wonka Sweets"
    ];

    // Rastgele ödemelerde kullanılan tutarlar. Senaryo ödemeleri bilerek bu kümenin dışındaki tutarları kullanır,
    // böylece "aynı tutar" eşlemesi senaryolarda yanlışlıkla tetiklenmez.
    private static readonly decimal[] BaseAmounts = [19m, 29m, 49m, 79m, 99m, 149m, 249m, 499m];

    public static DemoDataSet Generate(DateTime nowUtc, int seed = DefaultSeed)
    {
        var rng = new Random(seed);
        var customers = CustomerNames
            .Select((name, i) => Customer.Create(
                DataSource.Demo, $"DEMO-CUST-{i + 1:000}", name, $"billing@{Slug(name)}.example.com", nowUtc.AddDays(-120)))
            .ToList();

        var invoices = new List<Invoice>();
        var payments = new List<Payment>();

        Customer PickCustomer() => customers[rng.Next(customers.Count)];

        Payment AddPayment(
            Customer customer, decimal amount, PaymentStatus status, PaymentFailureCode? code, double daysAgo,
            Guid? invoiceId = null, string currency = "USD", int settleMinutes = 4)
        {
            var initiated = nowUtc.AddDays(-daysAgo);
            var payment = Payment.Create(
                customer.Id, DataSource.Demo, $"DEMO-PAY-{payments.Count + 1:0000}", amount, currency, status, code,
                initiated, initiated.AddMinutes(settleMinutes), invoiceId, nowUtc);
            payments.Add(payment);

            return payment;
        }

        Invoice AddInvoice(
            Customer customer, decimal amount, InvoiceStatus status, string providerStatus, int dueDaysFromNow, int issuedDaysAgo,
            double updatedDaysAgo)
        {
            var invoice = Invoice.Create(
                DataSource.Demo, $"DEMO-INV-EXT-{invoices.Count + 1:0000}", $"DEMO-INV-{invoices.Count + 1:0000}", customer.Id,
                amount, "USD", status, providerStatus, DateOnly.FromDateTime(nowUtc).AddDays(dueDaysFromNow),
                nowUtc.AddDays(-issuedDaysAgo), nowUtc.AddDays(-updatedDaysAgo), nowUtc);
            invoices.Add(invoice);

            return invoice;
        }

        // 1) Son ~3 ay: günde 1-3 ödeme. Çoğu tamamlanır; ~%10 başarısız, birkaçı iade, çok yeni olanların bir kısmı bekliyor.
        for (var day = 1; day <= 89; day++)
        {
            var perDay = rng.Next(1, 4);
            for (var n = 0; n < perDay; n++)
            {
                var daysAgo = day - rng.NextDouble() * 0.8;
                var amount = BaseAmounts[rng.Next(BaseAmounts.Length)];
                var roll = rng.Next(100);

                if (roll < 84)
                {
                    AddPayment(PickCustomer(), amount, PaymentStatus.Completed, null, daysAgo);
                }
                else if (roll < 94)
                {
                    if (day <= 2)
                    {
                        AddPayment(PickCustomer(), amount, PaymentStatus.Pending, null, daysAgo);   // henüz "takılmış" sayılmaz (< 3 gün)
                    }
                    else
                    {
                        AddPayment(PickCustomer(), amount, PaymentStatus.Failed, RandomFailureCode(rng), daysAgo);
                    }
                }
                else
                {
                    AddPayment(PickCustomer(), amount, PaymentStatus.Refunded, null, daysAgo);
                }
            }
        }

        // 2) 3 günden uzun süredir bekleyen 3 ödeme (pending_payment kaybı).
        AddPayment(customers[0], 129m, PaymentStatus.Pending, null, 5);
        AddPayment(customers[3], 349m, PaymentStatus.Pending, null, 8);
        AddPayment(customers[6], 59m, PaymentStatus.Pending, null, 12);

        // 3) Başarısız olup 2 gün sonra aynı müşteri/tutarla yeniden denenen ve tamamlanan 4 ödeme (kurtarılmış kayıp).
        (int Customer, decimal Amount, double DaysAgo, PaymentFailureCode Code)[] retried =
        [
            (1, 89m, 20, PaymentFailureCode.CardDeclined),
            (4, 199m, 30, PaymentFailureCode.InsufficientFunds),
            (7, 39m, 45, PaymentFailureCode.ExpiredCard),
            (9, 169m, 60, PaymentFailureCode.CardDeclined)
        ];
        foreach (var (customerIndex, amount, daysAgo, code) in retried)
        {
            AddPayment(customers[customerIndex], amount, PaymentStatus.Failed, code, daysAgo);
            AddPayment(customers[customerIndex], amount, PaymentStatus.Completed, null, daysAgo - 2);
        }

        // 4) Her hata kodundan, tekrar denenmemiş (açık) birer başarısız ödeme.
        AddPayment(customers[2], 55m, PaymentStatus.Failed, PaymentFailureCode.CardDeclined, 3);
        AddPayment(customers[5], 65m, PaymentStatus.Failed, PaymentFailureCode.InsufficientFunds, 4);
        AddPayment(customers[8], 75m, PaymentStatus.Failed, PaymentFailureCode.ExpiredCard, 6);
        AddPayment(customers[10], 85m, PaymentStatus.Failed, PaymentFailureCode.AccountRestricted, 7);
        AddPayment(customers[11], 95m, PaymentStatus.Failed, PaymentFailureCode.Other, 9);

        // 5) Faturalar. Ödenmiş 6 fatura, her biri bağlı tamamlanmış bir ödemeyle.
        (int Customer, decimal Amount, int IssuedDaysAgo)[] paidInvoices =
        [
            (0, 450m, 70), (2, 880m, 55), (4, 1500m, 48), (5, 300m, 33), (7, 2100m, 26), (10, 640m, 15)
        ];
        foreach (var (customerIndex, amount, issuedDaysAgo) in paidInvoices)
        {
            var paidDaysAgo = issuedDaysAgo - 5;
            var invoice = AddInvoice(customers[customerIndex], amount, InvoiceStatus.Paid, "PAID", -(issuedDaysAgo - 14), issuedDaysAgo, paidDaysAgo);
            AddPayment(customers[customerIndex], amount, PaymentStatus.Completed, null, paidDaysAgo, invoice.Id);
        }

        // Başarısız ödeme faturaya bağlı; fatura sonradan başka bir ödemeyle ödendi (kurtarılmış kayıp, 2 adet).
        for (var i = 0; i < 2; i++)
        {
            var customer = customers[i == 0 ? 3 : 8];
            var amount = i == 0 ? 720m : 410m;
            var invoice = AddInvoice(customer, amount, InvoiceStatus.Paid, "PAID", -4, 18, 7);
            AddPayment(customer, amount, PaymentStatus.Failed, PaymentFailureCode.CardDeclined, 10, invoice.Id);
            AddPayment(customer, amount, PaymentStatus.Completed, null, 7, invoice.Id);
        }

        // Vadesi geçmiş 4 gönderilmiş + 1 kısmen ödenmiş fatura (overdue_invoice kaybı).
        AddInvoice(customers[1], 1200m, InvoiceStatus.Sent, "SENT", -3, 20, 20);
        AddInvoice(customers[4], 640m, InvoiceStatus.Sent, "SENT", -9, 30, 30);
        AddInvoice(customers[6], 380m, InvoiceStatus.Sent, "SENT", -21, 40, 40);
        AddInvoice(customers[9], 2250m, InvoiceStatus.Sent, "SENT", -40, 60, 60);
        AddInvoice(customers[11], 900m, InvoiceStatus.PartiallyPaid, "PARTIALLY_PAID", -6, 25, 12);

        // Vadesi gelmemiş, iptal ve taslak faturalar (kayıp değildir).
        AddInvoice(customers[0], 520m, InvoiceStatus.Sent, "SENT", 7, 5, 5);
        AddInvoice(customers[5], 760m, InvoiceStatus.Sent, "SENT", 14, 2, 2);
        AddInvoice(customers[2], 330m, InvoiceStatus.Cancelled, "CANCELLED", -10, 35, 30);
        AddInvoice(customers[7], 150m, InvoiceStatus.Draft, "DRAFT", 30, 1, 1);

        // 6) USD dışı ödemeler: özete girmez (excludedPaymentCount) ve kayıp hesabına katılmaz.
        AddPayment(customers[3], 120m, PaymentStatus.Completed, null, 11, currency: "EUR");
        AddPayment(customers[3], 80m, PaymentStatus.Failed, PaymentFailureCode.CardDeclined, 13, currency: "EUR");

        return new DemoDataSet(customers, invoices, payments);
    }

    private static PaymentFailureCode RandomFailureCode(Random rng)
    {
        var roll = rng.Next(100);

        return roll switch
        {
            < 40 => PaymentFailureCode.CardDeclined,
            < 65 => PaymentFailureCode.InsufficientFunds,
            < 85 => PaymentFailureCode.ExpiredCard,
            < 90 => PaymentFailureCode.AccountRestricted,
            _ => PaymentFailureCode.Other
        };
    }

    private static string Slug(string name) =>
        new(name.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
}
