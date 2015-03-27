using Nikos.One.Installation;
using Nikos.One.Models.Store;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Nikos.One.Modules
{
    public sealed class PayPalPaymentInstaller : IModuleInstaller
    {
        public void Install(IInstallerService installerService)
        {
            installerService.InstallAssembly(typeof(PayPalPaymentInstaller).Assembly, null);
        }

        public async Task<bool> Configure(ExecutionContext executionContext)
        {
            var store = executionContext.Stores.CreateRandomAccessStore();

            var typeRegistration = store.ReadOrAdd<TypeRegistration>(new Guid("AFCD0BBF-9B21-4E0E-8433-7ABF39908EAB"), c =>
            {
                c.AssemblyQualifiedName = typeof(PayPalPayment).AssemblyQualifiedName;
            });

            var configuration = store.ReadOrAdd<PayPalPaymentConfiguration>(new Guid("ABF5F440-AE94-4FB6-9184-8F3427EF953A"), c =>
            {
                c.UseSandBox = true;
                c.NameField = "Name";
                c.SkuField = "ProductID";
                c.CartCurrencyField = "Currency";
                c.QuantityField = "quantity";
                c.PriceField = "price";
                c.CartTotalField = "total";
                c.CartTaxField = "tax";
                c.CartSubTotalField = "subtotal";
                c.CartShippingField = "shipping";
                c.CartFeeField = "fee";
                c.CartParam = "id";
                c.OkParam = "r";
                c.CancelParam = "r";
            });

            var componentReference = store.ReadOrAdd<ComponentReference>(new Guid("8CAEE112-DC80-4A8A-BAFA-218FAFD4933F"), c =>
            {
                c.TypeRegistrationId = typeRegistration.Id;
                c.ConfigurationId = configuration.Id;
            });

            var paymentMethods = new[]
            {
                store.ReadOrAdd<PaymentMethod>(new Guid("FFBB8EAF-DA32-4196-83D2-C0CDAE503E5C"), c=>
                {
                    c.IsEnabled = false;
                    c.Name = "PayPal";
                }),
                store.ReadOrAdd<PaymentMethod>(new Guid("AA5A12EC-DAC4-4D41-8490-53C314E852A4"), c=>
                {
                    c.IsEnabled = false;
                    c.Name = "MasterCard";
                }),
                store.ReadOrAdd<PaymentMethod>(new Guid("49029C6C-36E9-4360-A850-97BA5F51A16B"), c=>
                {
                    c.IsEnabled = false;
                    c.Name = "AmericanExpress";
                }),
                store.ReadOrAdd<PaymentMethod>(new Guid("05B85620-D965-468A-9893-9CAA225F7D8E"), c=>
                {
                    c.IsEnabled = false;
                    c.Name = "Discover";
                }),
                store.ReadOrAdd<PaymentMethod>(new Guid("0A4A0405-F89B-4162-9765-76C31F21F134"), c=>
                {
                    c.IsEnabled = false;
                    c.Name = "Visa";
                })
            };

            var configs = store.Read<PaymentConfiguration>();

            foreach (var paymentMethod in paymentMethods)
            {
                foreach (var config in configs)
                {
                    paymentMethod.Component = componentReference;
                    store.Add(paymentMethod);

                    if (config.PaymentMethods.All(m => m.Value != paymentMethod.Id))
                    {
                        config.PaymentMethods.Add(new ObjectReference
                        {
                            Value = paymentMethod.Id
                        });
                    }
                }
            }

            await store.Commit();

            return true;
        }
    }
}