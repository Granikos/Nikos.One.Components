using Nikos.One.Composition;
using Nikos.One.Models;
using Nikos.One.Models.Store;
using System.Collections.Generic;

namespace Nikos.One.Modules
{
    public class PaymentConfiguration : ComponentConfiguration, IViewTemplateContainer
    {
        private List<ObjectReference> _paymentMethods;

        private List<ViewTemplate> _viewTemplate;

        public virtual string ContinuationAction { get; set; }

        [References(typeof(PaymentMethod))]
        public virtual List<ObjectReference> PaymentMethods
        {
            get { return _paymentMethods ?? (_paymentMethods = new List<ObjectReference>()); }
            set { _paymentMethods = value; }
        }

        public virtual PaymentPhase Phase { get; set; }

        public virtual List<ViewTemplate> ViewTemplate
        {
            get { return _viewTemplate ?? (_viewTemplate = new List<ViewTemplate>()); }
            set { _viewTemplate = value; }
        }
    }
}