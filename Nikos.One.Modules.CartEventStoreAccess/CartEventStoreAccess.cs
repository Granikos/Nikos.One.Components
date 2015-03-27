using Nikos.One.Composition;
using Nikos.One.Models;
using Nikos.One.Models.Messages;
using Nikos.One.Runtime;
using Nikos.Toolbelt;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Nikos.One.Modules
{
    public class CartEventStoreAccess : ProcessComponent<CartEventStoreAccess, CartEventStoreAccessConfiguration>, ICartActionSyncEventHandler
    {
        private readonly IComponentController _componentController;
        private readonly IDataLoader _dataLoader;
        private IDataTableReader[] _dataTableReaders;

        public CartEventStoreAccess(IComponentController componentController, IDataLoader dataLoader)
        {
            _componentController = componentController;
            _dataLoader = dataLoader;
        }

        public override void OnInit(ExecutionContext executionContext)
        {
            base.OnInit(executionContext);

            _dataTableReaders = LoadDataReaders(executionContext);
        }

        private IDataTableReader[] LoadDataReaders(ExecutionContext executionContext)
        {
            var store = executionContext.Stores[null];
            var innerComponents = new[] { Configuration.InnerComponent };
            var l = new List<IDataTableReader>();

            foreach (var componentReference in innerComponents)
            {
                var tableReader = _componentController.CreateComponent<IDataTableReader>(executionContext, store, componentReference, null);

                if (tableReader == null)
                {
                    throw new Exception("The component reference '" + componentReference.Id + "' with the configuration ID '" + componentReference.ConfigurationId + "' did not reference a valid IIDataTableReader.");
                }

                l.Add(tableReader);
            }
            return l.ToArray();
        }

        public ComponentResult Process(ExecutionContext executionContext, Cart cart, string action, DynamicDictionary<object> data)
        {
            var task = _dataLoader.LoadData(executionContext, new OrderRequest(), _dataTableReaders, cart);

            Task.WaitAll(task);

            return Success();
        }
    }
}