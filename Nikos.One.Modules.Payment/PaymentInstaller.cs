using Nikos.One.Installation;
using Nikos.One.Models.Store;
using System;
using System.Threading.Tasks;

namespace Nikos.One.Modules
{
    public sealed class PaymentInstaller : IModuleInstaller
    {
        private static readonly Guid ConfigurationGuid = new Guid("E20E4A1C-9EAB-4079-A0D1-80C146D51FD8");
        private static readonly Guid TypeRegistrationGuid = new Guid("9E3102F0-E358-4477-BCDE-35DAEBCB4ED7");
        private static readonly Guid DefaultMethodTypeRegistrationGuid = new Guid("96DD3D10-376B-4783-B182-B1C26C15482E");
        private static readonly Guid ComponentReferenceGuid = new Guid("50D65BFD-8ADD-45F0-921B-D6143D0176F8");
        private static readonly Guid DefaultMethodConfigurationGuid = new Guid("7F6DFDA6-584C-468A-B59E-C0B4BC8B1B8F");

        private Store.RandomAccessConfigurationStore _store;
        private PaymentConfiguration _container;
        private TypeRegistration _typeRegistration;

        public void Install(IInstallerService installerService)
        {
            installerService.InstallAssembly(typeof(PaymentInstaller).Assembly, null);
        }

        public async Task<bool> Configure(ExecutionContext executionContext)
        {
            CreateStore(executionContext);
            CreateConfigurations();
            CreateTypeRegistration();
            CreateDefaultMethodTypeRegistration();
            CreateComponentReference();

            await _store.Commit();
            return true;
        }

        private void CreateStore(ExecutionContext executionContext)
        {
            _store = executionContext.Stores.CreateRandomAccessStore();
        }

        private void CreateComponentReference()
        {
            _store.ReadOrAdd<ComponentReference>(ComponentReferenceGuid, c =>
            {
                c.TypeRegistrationId = _typeRegistration.Id;
                c.ConfigurationId = _container.Id;
            });
        }

        private void CreateTypeRegistration()
        {
            _typeRegistration = _store.ReadOrAdd<TypeRegistration>(TypeRegistrationGuid, c =>
            {
                c.AssemblyQualifiedName = typeof(PaymentComponent).AssemblyQualifiedName;
            });
        }

        private void CreateDefaultMethodTypeRegistration()
        {
            _store.ReadOrAdd<TypeRegistration>(DefaultMethodTypeRegistrationGuid, c =>
            {
                c.AssemblyQualifiedName = typeof(SimplePaymentMethodComponent).AssemblyQualifiedName;
            });
        }

        private void CreateConfigurations()
        {
            _container = _store.ReadOrAdd<PaymentConfiguration>(ConfigurationGuid);
            _store.ReadOrAdd<SimplePaymentMethodConfiguration>(DefaultMethodConfigurationGuid);
        }
    }
}