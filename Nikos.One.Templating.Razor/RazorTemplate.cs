using RazorEngine.Templating;
using System.Collections.Generic;
using System.IO;

namespace Nikos.One.Templating.Engines
{
    public class RazorTemplate<T> : TemplateBase<T>, ITemplate, IRazorTemplate
    {
        void IRazorTemplate.SetWriter(TextWriter writer)
        {
            RazorTemplateFixer.Action(this, writer);
        }

        public virtual Dictionary<string, List<string>> GetRegisteredValues()
        {
            return null;
        }

        public virtual void Init(object templateContext)
        {
        }
    }
}