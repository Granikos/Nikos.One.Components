using Nikos.One.Composition;
using Nikos.One.Models.Store;

namespace Nikos.One.Modules
{
    public class PayPalPaymentConfiguration : ComponentConfiguration
    {
        public bool UseSandBox { get; set; }

        public virtual string ClientId { get; set; }

        public virtual string ClientSecret { get; set; }

        public string NameField { get; set; }

        public string SkuField { get; set; }

        public string CurrencyField { get; set; }

        public string CartCurrencyField { get; set; }

        public string QuantityField { get; set; }

        public string PriceField { get; set; }

        public string CartTotalField { get; set; }

        public string CartTaxField { get; set; }

        public string CartSubTotalField { get; set; }

        public string CartShippingField { get; set; }

        public string CartFeeField { get; set; }

        public string CartOrderNumberField { get; set; }

        public string CartParam { get; set; }

        public string OkParam { get; set; }

        public string CancelParam { get; set; }

        public string CancelUrl { get; set; }

        public string ReturnUrl { get; set; }

        public string CartShippingAddressField { get; set; }

        public string ShippingAddressFirstNameField { get; set; }

        public string ShippingAddressLastNameField { get; set; }

        public string ShippingAddressPhoneField { get; set; }

        public string ShippingAddressRecipientNameField { get; set; }

        public string ShippingAddressCityField { get; set; }

        public string ShippingAddressAddressLine1Field { get; set; }

        public string ShippingAddressAddressLine2Field { get; set; }

        public string ShippingAddressStateField { get; set; }

        public string ShippingAddressPostalCodeField { get; set; }

        public string ShippingAddressCountryCodeField { get; set; }

        public PayPalPaymentConfiguration()
        {
            UseSandBox = true;
        }
    }
}