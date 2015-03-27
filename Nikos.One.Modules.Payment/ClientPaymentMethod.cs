using System;

namespace Nikos.One.Modules
{
    internal sealed class ClientPaymentMethod
    {
        public Guid Id { get; set; }

        public string Name { get; set; }

        public string ViewTemplate { get; set; }

        public string Caption { get; set; }
    }
}