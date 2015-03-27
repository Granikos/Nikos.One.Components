using Nikos.One.Composition;
using Nikos.One.Models;
using System.Collections.Generic;

namespace Nikos.One.Modules
{
    public class SimplePaymentMethodComponent : ProcessComponent<SimplePaymentMethodComponent, SimplePaymentMethodConfiguration>, IPaymentGateway
    {
        public ComponentResult Process(ExecutionContext executionContext, Cart cart, PaymentMethod paymentMethod,
            PaymentConfiguration paymentContainer, IDictionary<string, object> data)
        {
            return Success();
        }

        public ComponentResult Continue(ExecutionContext executionContext, Cart cart, PaymentMethod paymentMethod,
            PaymentConfiguration paymentContainer, IDictionary<string, object> data)
        {
            return Success();
        }

        public string GetSummaryText(Cart cart, PaymentMethod paymentMethod)
        {
            return string.IsNullOrWhiteSpace(Configuration.Summary)
                ? paymentMethod.Name
                : Configuration.Summary;
        }
    }
}