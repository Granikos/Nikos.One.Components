using Nikos.One.Models.Store;

namespace Nikos.One.Modules
{
    public class MailAddressTemplate : DataStoreItem
    {
        public string AddressSelector { get; set; }

        public string Address { get; set; }

        public string DisplayNameSelector { get; set; }

        public string DisplayName { get; set; }
    }
}