using Nikos.One.Composition;
using Nikos.One.Models;
using Nikos.One.Runtime;
using Nikos.Toolbelt;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nikos.One.Modules
{
    public sealed class PaymentComponent : ProcessComponent<PaymentComponent, PaymentConfiguration>, ICartAction, IModifierContext
    {
        private readonly IComponentController _componentController;

        public PaymentMethod[] PaymentMethods { get; private set; }

        public PaymentComponent(IComponentController componentController)
        {
            _componentController = componentController;
        }

        public ComponentResult Begin(ExecutionContext executionContext, Cart cart, Relation realtion, Entity entity, IDictionary<string, object> data)
        {
            return Success();
        }

        public ComponentResult End(ExecutionContext executionContext, Cart cart, Relation realtion, Entity entity, IDictionary<string, object> data)
        {
            if (Configuration.Phase == PaymentPhase.Execution)
            {
                return cart.Data["paymentinprogress"].Convert<bool>()
                       ? Wait()
                       : Success();
            }

            var clientPaymentMethods = CreateFilteredClientPaymentMethods(executionContext, cart);

            var clientData = new Dictionary<string, object> { { "paymentMethods", clientPaymentMethods } };

            return Success(null, clientData, ViewTemplates);
        }

        private IEnumerable<ClientPaymentMethod> CreateFilteredClientPaymentMethods(ExecutionContext executionContext, Cart cart)
        {
            var paymentMethods = GetFilteredPaymentMethods(cart);
            var clientPaymentMethods = paymentMethods.Select(m => CreateClientPaymentMethod(executionContext, m));
            return clientPaymentMethods;
        }

        private IEnumerable<PaymentMethod> GetFilteredPaymentMethods(Cart cart)
        {
            var result = PaymentMethods.Where(m => m.Filters == null || !m.Filters.Any() || m.Filters.Compare(cart)).ToList();
            return result;
        }

        public override void OnInit(ExecutionContext executionContext)
        {
            base.OnInit(executionContext);

            Cache.Cleared += delegate
            {
                PaymentMethods = null;
            };

            Init(executionContext);
        }

        public ComponentResult ProcessAfter(ExecutionContext executionContext, Cart cart, string action, IDictionary<string, object> data)
        {
            try
            {
                return ProcessPaymentCore(executionContext, cart, data);
            }
            catch (RequestFatalException ex)
            {
                return Fatal(ex.Title, ex.Message);
            }
            catch (RequestErrorException ex)
            {
                return Fatal(ex.Message, ex.Field);
            }
        }

        public ComponentResult ProcessBefore(ExecutionContext executionContext, Cart cart, string action, IDictionary<string, object> data)
        {
            return null;
        }

        private static ClientPaymentMethod CreateClientPaymentMethod(ExecutionContext executionContext, PaymentMethod method)
        {
            var viewTemplate = method.ViewTemplate.FirstAllowedWithCulture(executionContext);
            return new ClientPaymentMethod
            {
                Id = method.Id,
                Name = method.Name,
                ViewTemplate = viewTemplate == null ? null : viewTemplate.Text,
                Caption = method.Caption.FirstAllowedWithCulture(executionContext) ?? method.Name
            };
        }

        private static Func<IPaymentGateway, ExecutionContext, Cart, PaymentMethod, PaymentConfiguration, IDictionary<string, object>, ComponentResult> GetPaymentGatewayMethod(bool isPaymentInProgress)
        {
            Func<IPaymentGateway, ExecutionContext, Cart, PaymentMethod, PaymentConfiguration, IDictionary<string, object>, ComponentResult> method;

            if (isPaymentInProgress)
            {
                method = (g, e, c, p, conf, d) => g.Continue(e, c, p, conf, d);
            }
            else
            {
                method = (g, e, c, p, conf, d) => g.Process(e, c, p, conf, d);
            }

            return method;
        }

        private static Guid GetPaymentMethodId(Cart cart, IDictionary<string, object> data)
        {
            var paymentMethodIdString = GetPaymentMethodIdString(cart, data);

            return ParsePaymentMethodId(paymentMethodIdString);
        }

        private static string GetPaymentMethodIdString(Cart cart, IDictionary<string, object> data)
        {
            var o = IsPaymentInProgress(cart)
                ? cart.TotalsItem["PaymentMethodId"]
                : data.ValueOrDefault("PaymentMethodId") ?? cart.TotalsItem["PaymentMethodId"];
            return o.Convert<string>();
        }

        private static bool IsPaymentCompleted(Cart cart)
        {
            return cart.Data["paymentcompleted"].Convert<bool>();
        }

        private static bool IsPaymentInProgress(Cart cart)
        {
            return cart.Data["paymentinprogress"].Convert<bool>();
        }

        private static Guid ParsePaymentMethodId(string paymentMethodIdString)
        {
            Guid paymentMethodId;

            if (Guid.TryParse(paymentMethodIdString, out paymentMethodId))
            {
                return paymentMethodId;
            }

            throw new RequestErrorException("The payment method specified is not valid.", "PaymentMethodId");
        }

        private static void RemovePaymentProgressFromCart(Cart cart)
        {
            cart.Data.Data.Remove("paymentinprogress");
            cart.Data.Data.Remove("paymentcompleted");
        }

        private static void UpdatePaymentInfosInCart(Cart cart, Guid paymentMethodId, IPaymentGateway paymentGateway, PaymentMethod paymentMethod)
        {
            cart.TotalsItem["PaymentMethodId"] = paymentMethodId;
            cart.TotalsItem["PaymentCosts"] = paymentMethod.Costs.Convert<int>();
            cart.TotalsItem["PaymentMethod"] = paymentMethod.Name;
            cart.TotalsItem["paymentsummary"] = paymentGateway.GetSummaryText(cart, paymentMethod);
            cart.TotalsItem.Data.DataTable.Columns["paymentsummary"].IsSticky = true;
            cart.TotalsItem.Data.DataTable.Columns["PaymentCosts"].IsSticky = true;
            cart.TotalsItem.Data.DataTable.Columns["PaymentMethodId"].IsSticky = true;
            cart.TotalsItem.Data.DataTable.Columns["PaymentMethod"].IsSticky = true;
        }

        private static void UpdatePaymentProgressInCartData(Cart cart, ComponentResult result)
        {
            switch (result.Type)
            {
                case ComponentResultType.Success:
                    cart.Data["paymentinprogress"] = false;
                    cart.Data["paymentcompleted"] = true;
                    break;

                case ComponentResultType.Wait:
                    cart.Data["paymentinprogress"] = true;
                    cart.Data["paymentcompleted"] = false;
                    break;
            }
        }

        private IPaymentGateway GetPaymentGateway(ExecutionContext executionContext, PaymentMethod paymentMethod)
        {
            var paymentGateway = _componentController.CreateComponent<IPaymentGateway>(executionContext, executionContext.Stores.Default, paymentMethod.Component, null);

            if (paymentGateway == null)
            {
                throw new RequestFatalException("An internal error has occurred.", string.Format("The payment method '{0}' uses the component references '{1}' that could not be resolved.", paymentMethod.Id, paymentMethod.Component.Id));
            }

            return paymentGateway;
        }

        private PaymentMethod GetPaymentMethod(ExecutionContext executionContext, Cart cart, Guid paymentMethodId)
        {
            if (PaymentMethods == null)
            {
                Init(executionContext);
            }

            // ReSharper disable once AssignNullToNotNullAttribute
            var paymenMethods = GetFilteredPaymentMethods(cart);
            var paymentMethod = paymenMethods.FirstOrDefault(pm => pm.Id == paymentMethodId);

            if (paymentMethod == null)
            {
                throw new RequestErrorException(string.Format("The payment method '{0}' is not valid.", paymentMethodId), "PaymentMethodId");
            }

            return paymentMethod;
        }

        private void Init(ExecutionContext executionContext)
        {
            var allPaymentMethods = executionContext.Stores.Default.LoadReferences<PaymentMethod>(executionContext, Configuration.PaymentMethods);

            var enabledPaymentMethods = allPaymentMethods.Where(method => method.IsEnabled);

            PaymentMethods = enabledPaymentMethods.Select(
                method => method
                    .Preload(m => m.Component)
                    .Preload(m => m.Filters)
                    .Preload(m => m.ViewTemplate)
                    .Preload(m => m.Caption)
                    ).ToArray();
        }

        private ComponentResult ProcessPaymentCore(ExecutionContext executionContext, Cart cart, IDictionary<string, object> data)
        {
            if (IsPaymentCompleted(cart))
            {
                return Success();
            }

            if (Configuration.Phase == PaymentPhase.DataCollection)
            {
                RemovePaymentProgressFromCart(cart);
            }

            var isPaymentInProgress = IsPaymentInProgress(cart);
            var paymentMethodId = GetPaymentMethodId(cart, data);
            var paymentMethod = GetPaymentMethod(executionContext, cart, paymentMethodId);
            var paymentGateway = GetPaymentGateway(executionContext, paymentMethod);

            if (Configuration.Phase == PaymentPhase.DataCollection)
            {
                UpdatePaymentInfosInCart(cart, paymentMethodId, paymentGateway, paymentMethod);

                return Success();
            }

            var method = GetPaymentGatewayMethod(isPaymentInProgress);
            var result = method(paymentGateway, executionContext, cart, paymentMethod, Configuration, data);

            if (result is RepeatPaymentEntryResult)
            {
                RemovePaymentProgressFromCart(cart);

                return result;
            }

            UpdatePaymentProgressInCartData(cart, result);

            return result;
        }
    }
}