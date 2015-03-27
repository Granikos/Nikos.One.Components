using Nikos.Toolbelt;
using RazorEngine;
using RazorEngine.Templating;
using System;
using System.Linq;

namespace Nikos.One.Templating.Engines
{
    public class RazorTemplateEngine : ITemplateEngine
    {
        private Type _templateType;

        public string Name
        {
            get { return "Razor"; }
        }

        public Type TemplateType
        {
            get { return _templateType; }
            set
            {
                if (_templateType == value)
                {
                    return;
                }

                _templateType = value;
                SetTemplateBaseType(value);
            }
        }

        public RazorTemplateEngine()
        {
            TemplateType = typeof(RazorTemplate<>);
        }

        private static void SetTemplateBaseType(Type type)
        {
            Razor.SetTemplateService(new TemplateService(new RazorEngine.Configuration.TemplateServiceConfiguration
            {
                BaseTemplateType = type
            }));
        }

        public TemplateResult Render<T>(string template, T model, object templateContext)
        {
            TemplateBase tmpl;

            // TODO: should we add a switch to render the errors into the template result?
            try
            {
                tmpl = (TemplateBase)Razor.CreateTemplate(template, model);
                ((ITemplate)tmpl).Init(templateContext);
            }
            catch (TemplateCompilationException ex)
            {
                var msg = string.Join("\r\n", ex.Errors.Select(e => string.Format("{0} at ({1},{2}): {3}", e.ErrorNumber, e.Line, e.Column, e.ErrorText)));
                throw new ApplicationException(msg);
            }
            //catch (TemplateCompilationException ex)
            //{
            //    return new TemplateResult(
            //        w =>
            //        {
            //            w.Write(ex.Message.Replace("\r\n", "<br>"));
            //            if (ex.Errors.Any())
            //            {
            //                w.Write("<ul><li>");
            //                w.Write(string.Join("</li><li>", ex.Errors.Select(e => e.ToString().Replace("\r\n", "<br>"))));
            //                w.Write("</li></ul>");
            //            }
            //        },
            //        (n, w) => { });
            //}
            //catch (Exception ex)
            //{
            //    // TODO: figure out which errors to report to user and which to hide.
            //    // TODO: log errors..
            //    return new TemplateResult(
            //        w => w.Write(ex.ToFlatString()),
            //        (n, w) => { });
            //}

            var context = new ExecuteContext();
            var registered = ((IRazorTemplate)tmpl).GetRegisteredValues();

            var result = new TemplateResult(
                w =>
                {
                    try
                    {
                        ((RazorEngine.Templating.ITemplate)tmpl).Run(context, w);
                    }
                    catch (Exception ex)
                    {
                        // TODO: figure out which errors to report to user and which to hide.
                        // TODO: log errors..
                        w.Write(ex.ToFlatString());
                    }
                },
                (n, w) =>
                {
                    try
                    {
                        ((IRazorTemplate)tmpl).SetWriter(w);
                        tmpl.RenderSection(n, false).WriteTo(w);
                    }
                    catch (Exception ex)
                    {
                        // TODO: figure out which errors to report to user and which to hide.
                        // TODO: log errors..
                        w.Write(ex.ToFlatString());
                    }
                },
                registered);

            return result;
        }
    }
}