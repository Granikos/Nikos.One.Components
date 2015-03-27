using RazorEngine.Templating;
using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace Nikos.One.Templating.Engines
{
    internal class RazorTemplateFixer
    {
        public static Action<TemplateBase, TextWriter> Action { get; private set; }

        static RazorTemplateFixer()
        {
            var contextFieldInfo = typeof(TemplateBase).GetField("_context", BindingFlags.Instance | BindingFlags.NonPublic);
            var writerPropertyInfo = typeof(ExecuteContext).GetProperty("CurrentWriter", BindingFlags.Instance | BindingFlags.NonPublic);

            var inputParameter = Expression.Parameter(typeof(TemplateBase));
            var writerParameter = Expression.Parameter(typeof(TextWriter));
            var fieldExpression = Expression.Field(inputParameter, contextFieldInfo);
            var setExpression = Expression.Call(fieldExpression, writerPropertyInfo.SetMethod, new Expression[] { writerParameter });
            var lambdaExpression = Expression.Lambda<Action<TemplateBase, TextWriter>>(setExpression, inputParameter, writerParameter);

            Action = lambdaExpression.Compile();
        }
    }
}