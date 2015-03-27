using System.Collections.Generic;
using System.IO;

namespace Nikos.One.Templating.Engines
{
    internal interface IRazorTemplate
    {
        void SetWriter(TextWriter writer);

        Dictionary<string, List<string>> GetRegisteredValues();
    }
}