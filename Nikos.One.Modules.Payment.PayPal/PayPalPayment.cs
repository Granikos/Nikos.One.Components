using Nikos.One.Composition;
using Nikos.One.Models;
using Nikos.One.Models.Store;
using Nikos.Toolbelt;
using PayPal;
using PayPal.Api.Payments;
using PayPal.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Nikos.One.Modules
{
    public class PayPalPayment : ProcessComponent<PayPalPayment, PayPalPaymentConfiguration>, IPaymentGateway
    {
        private const string CancelValue = "c";
        private const string OkValue = "o";
        private readonly static Regex RegexMonth = new Regex(@"\d{1,2}", RegexOptions.Compiled);
        private readonly static Regex RegexYear = new Regex(@"\d{4,4}", RegexOptions.Compiled);
        private DateTime _authExpireTime;
        private AuthTokenInfo _authToken;

        public ComponentResult Continue(ExecutionContext executionContext, Cart cart, PaymentMethod paymentMethod, PaymentConfiguration paymentContainer, IDictionary<string, object> data)
        {
            if (data.ValueOrDefault(Configuration.CancelParam).Convert<string>() == CancelValue)
            {
                return Repeat(cart, "The payment has been canceled by the user.");
            }

            if (data.ValueOrDefault(Configuration.OkParam).Convert<string>() != OkValue)
            {
                return Fatal("The request data does not contain the expected value for the OK parameter.");
            }

            var o = cart.Data["PayPalPaymentExpiration"];
            if (o == null)
            {
                Log.Error("The cart " + cart.Id + " does not contain the PayPal payment expiration time.");
                return Repeat(cart, "The cart is not in a valid state.");
            }

            var dt = o.Convert<DateTime>();
            if (dt < DateTime.UtcNow)
            {
                return Repeat(cart, "The payment approval has expired.");
            }

            var paymentId = cart.Data["PayPalPaymentID"].Convert<string>();

            if (paymentId == null)
            {
                Log.Error("The cart " + cart.Id + " does not contain the PayPal payment ID.");
                return Repeat(cart, "The internal cart state is not valid.");
            }

            Payment payment;

            try
            {
                var apiContext = CreateApiContext(executionContext, cart);
                payment = Payment.Get(apiContext, paymentId);
            }
            catch (PayPalException ex)
            {
                return HandlePaymentContinuationError(cart, ex, paymentId);
            }

            if (payment == null)
            {
                Log.Error("The payment '" + paymentId + "' is not known to PayPal.");
                return Repeat(cart, "The specified payment is not known to PayPal.");
            }

            var paymentExecution = new PaymentExecution
            {
                payer_id = data.ValueOrDefault("payerid").Convert<string>()
            };

            if (paymentExecution.payer_id == null)
            {
                if (payment.payer == null ||
                    payment.payer.payer_info == null ||
                    payment.payer.payer_info.payer_id == null)
                {
                    Log.Error("Could not retrieve the payer ID for payment ID " + paymentId);
                    return Repeat(cart, "An error occurred communicating with PayPal.");
                }

                paymentExecution.payer_id = payment.payer.payer_info.payer_id;
            }

            try
            {
                var apiContext = CreateApiContext(executionContext, cart);
                payment.Execute(apiContext, paymentExecution);
            }
            catch (PayPalException ex)
            {
                return HandlePaymentContinuationError(cart, ex, paymentId);
            }

            return Success();
        }

        public string GetSummaryText(Cart cart, PaymentMethod paymentMethod)
        {
            switch (paymentMethod.Name.ToLowerInvariant())
            {
                case "paypal":
                    return "PayPal";
            }

            return null;
        }

        public ComponentResult Process(ExecutionContext executionContext, Cart cart, PaymentMethod paymentMethod, PaymentConfiguration paymentContainer, IDictionary<string, object> data)
        {
            var errors = new List<ComponentError>();
            Payment payment;

            using (ThreadCulture.SetInvariantCultureTemporarily())
            {
                payment = CreatePayment(cart, paymentMethod, paymentContainer, data, errors);
            }

            if (errors.Any())
            {
                return Error(errors);
            }

            if (payment == null)
            {
                return Wait();
            }

            Payment newPayment;

            try
            {
                var apiContext = CreateApiContext(executionContext, cart);
                newPayment = payment.Create(apiContext);
            }
            catch (PayPalException ex)
            {
                Log.Info(payment.ConvertToJson(), "Tried sending payment to PayPal");
                return HandlePayPalError(ex, payment.id);
            }

            cart.Data["PayPalPaymentID"] = newPayment.id;
            // TODO: When does PayPal expire a payment? What happens if the buyer approves a payment and we don't execute it?
            cart.Data["PayPalPaymentExpiration"] = DateTime.UtcNow.AddHours(1);

            var redirect = newPayment.links.FirstOrDefault(l => "approval_url".Equals(l.rel, StringComparison.OrdinalIgnoreCase));

            return redirect != null
                ? Redirect(redirect.href)
                : Success();
        }

        private static string GetData(IDictionary<string, object> data, string field, string caption, IList<ComponentError> errors)
        {
            object o;
            if (!data.TryGetValue(field, out o) || o == null)
            {
                errors.Add(new ComponentError("{?The value is missing?}: {?" + caption + "?}.", field));
                return null;
            }

            var s = o.ToString();
            if (!string.IsNullOrWhiteSpace(s))
            {
                return s;
            }

            errors.Add(new ComponentError("{?The value is missing?}: {?" + caption + "?}.", field));
            return null;
        }

        private static ComponentResult Repeat(Cart cart, string reason)
        {
            cart.Data.Data.Remove("PayPalAccessToken");
            cart.Data.Data.Remove("PayPalAccessTokenType");
            cart.Data.Data.Remove("PayPalPaymentID");
            cart.Data.Data.Remove("PayPalAccessToken");
            cart.Data.Data.Remove("PayPalAccessTokenType");

            return new RepeatPaymentEntryResult
            {
                Text = reason + " Please repeat the authorization process."
            };
        }

        private APIContext CreateApiContext(ExecutionContext executionContext, Cart cart)
        {
            if (_authToken == null || _authExpireTime >= DateTime.Now)
            {
                _authExpireTime = DateTime.Now;
                _authToken = PayPalActions.GetAuthenticationToken(executionContext, Configuration.ClientId, Configuration.ClientSecret, Configuration.UseSandBox);
                _authExpireTime += TimeSpan.FromSeconds(_authToken.expires_in);
            }

            var apiContext = new APIContext(_authToken.token_type + " " + _authToken.access_token, Guid.NewGuid().ToString())
            {
                Config = new Dictionary<string, string>()
            };

            if (Configuration.UseSandBox)
            {
                apiContext.Config.Add("mode", "sandbox");
            }

            cart.Data["PayPalRequestId"] = apiContext.RequestId;
            return apiContext;
        }

        private Payment CreatePayment(Cart cart, NamedDataStoreItem paymentMethod, PaymentConfiguration paymentConfiguration, IDictionary<string, object> data, List<ComponentError> errors)
        {
            var paymentMethodDetails = new PaymentMethodDetails
            {
                Name = paymentMethod.Name
            };

            if (!"paypal".Equals(paymentMethod.Name, StringComparison.OrdinalIgnoreCase))
            {
                var expiration = GetData(data, "cc-expiration", "Expiration date", errors);

                if (errors.Any())
                {
                    return null;
                }

                // TODO: add additional validation (e.g. wih regex).

                var cc = new CreditCard
                {
                    number = GetData(data, "cc-number", "Credit card number", errors),
                    cvv2 = GetData(data, "cc-cvv", "CVV", errors).Convert<int>(),
                    first_name = GetData(data, "cc-firstname", "First name", errors),
                    last_name = GetData(data, "cc-lastname", "Last name", errors),
                    expire_month = RegexMonth.Match(expiration).Value.Convert<int>(),
                    expire_year = RegexYear.Match(expiration).Value.Convert<int>()
                };

                if (errors.Any())
                {
                    return null;
                }

                paymentMethodDetails.Instrument = new FundingInstrument
                {
                    credit_card = cc
                };
            }

            var info = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                {"CartParam", Configuration.CartParam},
                {"CancelParam", Configuration.CancelParam},
                {"OkParam", Configuration.OkParam},
                {"CartValue", cart.Id.ToString()},
                {"CancelValue", CancelValue},
                {"OkValue", OkValue},
                {"ActionToken", cart.RegisterExpectedAction(paymentConfiguration.ContinuationAction)}
            };

            info.AddOrUpdate(data);

            var returnUrl = NamedFormat.FormatDictionary(Configuration.ReturnUrl, info);
            var cancelUrl = NamedFormat.FormatDictionary(Configuration.CancelUrl, info);
            var transaction = cart.ToTransaction(Configuration);
            var payment = new Payment
            {
                id = cart.Id.ToString(),
                intent = "sale",
                payer = new Payer
                {
                    payment_method = paymentMethodDetails.Name,
                    funding_instruments = paymentMethodDetails.Instrument == null ? null : new List<FundingInstrument>
                    {
                        paymentMethodDetails.Instrument
                    }
                },
                transactions = new List<Transaction>
                {
                    transaction
                },
                redirect_urls = new RedirectUrls
                {
                    return_url = returnUrl,
                    cancel_url = cancelUrl
                }
            };

            if (!string.IsNullOrEmpty(Configuration.CartShippingAddressField))
            {
                var shippingAddress = cart.TotalsItem[Configuration.CartShippingAddressField] as DataRow;
                if (shippingAddress != null)
                {
                    // payment.payer.payer_info = shippingAddress.ToPayerInfo(Configuration);
                }
            }

            cart.Data["PayPalPaymentId"] = payment.id;

            return payment;
        }

        private ComponentResult HandlePaymentContinuationError(Cart cart, Exception ex, string paymentId)
        {
            IEnumerable<PayPalErrorMessage> payPalMessages;
            var result = HandlePayPalError(ex, paymentId, out payPalMessages);

            var payPalMessage = payPalMessages.FirstOrDefault();
            if (payPalMessage == null || !string.Equals("payment_approval_expired", payPalMessage.Name, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }

            return Repeat(cart, "The PayPal approval has expired.");
        }

        private ComponentResult HandlePayPalError(Exception ex, string paymentId)
        {
            var connectionExceptions = ex.Iterate(x => x.InnerException).OfType<ConnectionException>();
            var internalErrorMessage = "An error occurred submitting payment " + paymentId + string.Join("", connectionExceptions.Select(x => "\r\n" + x.Response));

            return Fatal(null, ex, internalErrorMessage);
        }

        private ComponentResult HandlePayPalError(Exception ex, string paymentId, out IEnumerable<PayPalErrorMessage> payPalMessages)
        {
            var connectionExceptions = ex.Iterate(x => x.InnerException).OfType<ConnectionException>().ToArray();
            payPalMessages = connectionExceptions.Select(e =>
            {
                try
                {
                    return JsonReader.FromJson<PayPalErrorMessage>(e.Response);
                }
                catch
                {
                    return null;
                }
            }).Where(e => e != null).ToList();
            var internalErrorMessage = "An error occurred submitting payment " + paymentId + string.Join("", connectionExceptions.Select(x => "\r\n" + x.Response));

            return Fatal(null, ex, internalErrorMessage);
        }
    }
}