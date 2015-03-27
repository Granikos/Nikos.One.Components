using Nikos.One.Composition;
using Nikos.One.Models;
using System.Collections.Generic;

namespace Nikos.One.Modules
{
    public interface IPaymentGateway : IComponent
    {
        ComponentResult Process(ExecutionContext executionContext, Cart cart, PaymentMethod paymentMethod, PaymentConfiguration paymentContainer, IDictionary<string, object> data);

        ComponentResult Continue(ExecutionContext executionContext, Cart cart, PaymentMethod paymentMethod, PaymentConfiguration paymentContainer, IDictionary<string, object> data);

        string GetSummaryText(Cart cart, PaymentMethod paymentMethod);
    }
}