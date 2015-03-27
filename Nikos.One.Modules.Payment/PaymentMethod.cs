using Nikos.One.Models;
using Nikos.One.Models.Store;
using System.Collections.Generic;

namespace Nikos.One.Modules
{
    public class PaymentMethod : NamedDataStoreItem
    {
        private List<StringReference> _caption;
        private List<ColumnComparison> _filters;

        private List<ViewTemplate> _viewTemplate;

        public virtual List<StringReference> Caption
        {
            get { return _caption ?? (_caption = new List<StringReference>()); }
            set { _caption = value; }
        }

        public virtual ComponentReference Component { get; set; }

        public decimal? Costs { get; set; }

        public virtual List<ColumnComparison> Filters
        {
            get { return _filters ?? (_filters = new List<ColumnComparison>()); }
            set { _filters = value; }
        }

        public bool IsEnabled { get; set; }

        public virtual List<ViewTemplate> ViewTemplate
        {
            get { return _viewTemplate ?? (_viewTemplate = new List<ViewTemplate>()); }
            set { _viewTemplate = value; }
        }
    }
}