using Nikos.One.Composition;
using Nikos.One.Models.Store;

namespace Nikos.One.Modules
{
    public class CartEventStoreAccessConfiguration : ComponentConfiguration
    {
        public virtual ComponentReference InnerComponent { get; set; }
    }
}