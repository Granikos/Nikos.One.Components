using PayPal.Api.Payments;

namespace Nikos.One.Modules
{
    internal class PaymentMethodDetails
    {
        public FundingInstrument Instrument { get; set; }

        public string Name { get; set; }
    }
}