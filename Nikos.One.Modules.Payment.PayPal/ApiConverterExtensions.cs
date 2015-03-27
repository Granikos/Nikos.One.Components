using Nikos.One.Models;
using Nikos.Toolbelt;
using PayPal.Api.Payments;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Nikos.One.Modules
{
    internal static class ApiConverterExtensions
    {
        public static PayerInfo ToPayerInfo(this DataRow address, PayPalPaymentConfiguration configuration)
        {
            return new PayerInfo
            {
                shipping_address = address.ToShippingAddress(configuration),
                first_name = address[configuration.ShippingAddressFirstNameField].Convert<string>(),
                last_name = address[configuration.ShippingAddressLastNameField].Convert<string>(),
                phone = address[configuration.ShippingAddressPhoneField].Convert<string>()
            };
        }

        public static ShippingAddress ToShippingAddress(this DataRow address, PayPalPaymentConfiguration configuration)
        {
            return new ShippingAddress
            {
                recipient_name = address[configuration.ShippingAddressRecipientNameField].Convert<string>(),
                city = address[configuration.ShippingAddressCityField].Convert<string>(),
                line1 = address[configuration.ShippingAddressAddressLine1Field].Convert<string>(),
                line2 = address[configuration.ShippingAddressAddressLine2Field].Convert<string>(),
                phone = address[configuration.ShippingAddressPhoneField].Convert<string>(),
                state = address[configuration.ShippingAddressStateField].Convert<string>(),
                postal_code = address[configuration.ShippingAddressPostalCodeField].Convert<string>(),
                country_code = address[configuration.ShippingAddressCountryCodeField].Convert<string>()
            };
        }

        public static Transaction ToTransaction(this Cart cart, PayPalPaymentConfiguration configuration)
        {
            var orderNumber = (!string.IsNullOrWhiteSpace(configuration.CartOrderNumberField)
                 ? cart.Data[configuration.CartOrderNumberField].Convert<string>()
                 : cart.Id.ToString())
                               ?? cart.Id.ToString();

            return new Transaction
            {
                amount = cart.ToAmount(configuration),
                item_list = new ItemList { items = cart.ToItems(configuration).ToList() },
                description = orderNumber
            };
        }

        public static Amount ToAmount(this Cart cart, PayPalPaymentConfiguration configuration)
        {
            var cartCurrency = string.IsNullOrWhiteSpace(configuration.CartCurrencyField) ? null : cart[configuration.CartCurrencyField].Convert<string>();
            var totalsRow = cart.TotalsItem;

            var total = totalsRow[configuration.CartTotalField].Convert<decimal>();
            var subtotal = totalsRow[configuration.CartSubTotalField].Convert(0m);
            var shipping = totalsRow[configuration.CartShippingField].Convert(0m);
            var tax = totalsRow[configuration.CartTaxField].Convert(0m);
            var fee = totalsRow[configuration.CartFeeField].Convert(0m);

            return new Amount
            {
                currency = cartCurrency,
                total = total.ToString("0.00", CultureInfo.InvariantCulture),
                details = new Details
                {
                    subtotal = subtotal.ToString("0.00", CultureInfo.InvariantCulture),
                    shipping = shipping.ToString("0.00", CultureInfo.InvariantCulture),
                    tax = tax.ToString("0.00", CultureInfo.InvariantCulture),
                    fee = fee.ToString("0.00", CultureInfo.InvariantCulture)
                }
            };
        }

        public static IEnumerable<Item> ToItems(this Cart cart, PayPalPaymentConfiguration configuration)
        {
            var nameField = configuration.NameField;
            var skuField = configuration.SkuField;
            var currencyField = configuration.CurrencyField;
            var quantityField = configuration.QuantityField;
            var priceField = configuration.PriceField;
            var cartCurrency = string.IsNullOrWhiteSpace(configuration.CartCurrencyField) ? null : cart.Data[configuration.CartCurrencyField].Convert<string>();

            return from item in cart.Items
                   select new Item
                   {
                       sku = skuField == null ? "" : item[skuField].Convert("0"),
                       name = nameField == null ? "" : item[nameField].Convert<string>(),
                       quantity = quantityField == null ? "0" : item[quantityField].Convert("0"),
                       price = priceField == null ? "0" : item[priceField].Convert("0"),
                       currency = currencyField == null ? cartCurrency : item[currencyField].Convert<string>() ?? cartCurrency
                   };
        }
    }
}