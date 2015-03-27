using Nikos.One.Composition;
using Nikos.One.Models;
using Nikos.One.Templating;
using Nikos.Toolbelt;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;

namespace Nikos.One.Modules
{
    public class Mails : ProcessComponent<Mails, MailsConfiguration>, ICartActionAsyncEventHandler
    {
        private readonly HtmlEmbedder _htmlEmbedder;
        private readonly ITemplateRenderer _templateRenderer;

        public Mails(ITemplateRenderer templateRenderer)
        {
            _templateRenderer = templateRenderer;
            _htmlEmbedder = new HtmlEmbedder();
        }

        public override void OnInit(ExecutionContext executionContext)
        {
            base.OnInit(executionContext);

            foreach (var mailTemplate in Configuration.MailTemplates)
            {
                mailTemplate.PreproccessedTemplate = mailTemplate.IsEmbedded
                    ? _htmlEmbedder.EmbedExternals(mailTemplate.Template)
                    : mailTemplate.Template;
            }
        }

        public ComponentResult Process(ExecutionContext executionContext, Cart cart, string action, DynamicDictionary<object> data)
        {
            foreach (var mailTemplate in Configuration.MailTemplates)
            {
                ProcessMailTemplate(cart, mailTemplate);
            }

            return null;
        }

        private static MailAddress CreateMailAddress(MailAddressTemplate template, DataRow data)
        {
            var address = GetOrResolveCartValue(data, template.AddressSelector, template.Address);
            var displayName = GetOrResolveCartValue(data, template.DisplayNameSelector, template.DisplayName);

            return new MailAddress(address, displayName);
        }

        private static MailMessage CreateMailAndAddBody(MailTemplate mailTemplate, string mailBody, MailAddress sender, IEnumerable<MailAddress> receivers, IEnumerable<MailAddress> cc, IEnumerable<MailAddress> bcc)
        {
            var mailMessage = new MailMessage
            {
                Subject = mailTemplate.MailSubject,
                Body = mailBody,
                IsBodyHtml = mailTemplate.IsHtmlMail,
                Sender = sender,
                From = sender,
                BodyEncoding = Encoding.UTF8
            };

            mailMessage.To.AddRange(receivers);
            mailMessage.CC.AddRange(cc);
            mailMessage.Bcc.AddRange(bcc);

            mailMessage.Attachments.AddRange(from a in mailTemplate.Attachments
                                             select new Attachment(new MemoryStream(a.Content), new ContentType
                                             {
                                                 MediaType = "application/octet-stream",
                                                 Name = a.Name
                                             }));

            return mailMessage;
        }

        private static string GetOrResolveCartValue(DataRow data, string selector, string value)
        {
            return string.IsNullOrWhiteSpace(selector)
                ? value
                : data.Resolve(selector).Convert<string>();
        }

        private static void SendMail(SmtpClient smtpClient, MailMessage mail)
        {
            smtpClient.Send(mail);
        }

        private MailMessage CreateMail(Cart cart, MailTemplate mailTemplate)
        {
            var sender = CreateMailAddress(mailTemplate.Sender, cart.TotalsItem.Data);
            var to = CreateMailAddresses(mailTemplate.To, cart.TotalsItem.Data);
            var cc = CreateMailAddresses(mailTemplate.Cc, cart.TotalsItem.Data);
            var bcc = CreateMailAddresses(mailTemplate.Bcc, cart.TotalsItem.Data);
            var mailBody = GenerateMailBody(cart, mailTemplate);
            var mail = CreateMailAndAddBody(mailTemplate, mailBody, sender, to, cc, bcc);

            return mail;
        }

        private IEnumerable<MailAddress> CreateMailAddresses(IEnumerable<MailAddressTemplate> to, DataRow data)
        {
            return to.Select(mailAddressTemplate => CreateMailAddress(mailAddressTemplate, data));
        }

        private SmtpClient CreateSmtpClient()
        {
            return new SmtpClient
            {
                Port = Configuration.Port,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Host = Configuration.SmtpServer,
                EnableSsl = Configuration.UseSsl,
                TargetName = Configuration.ServiceProviderName,
                Credentials = new System.Net.NetworkCredential(Configuration.ServerUser, Configuration.ServerPasswort)
            };
        }

        private string GenerateMailBody(Cart cart, MailTemplate mailTemplate)
        {
            var result = _templateRenderer.Render(mailTemplate.TemplatingEngine, mailTemplate.PreproccessedTemplate, cart);
            var mailBody = result.RenderContent();
            return mailBody;
        }

        private void ProcessMailTemplate(Cart cart, MailTemplate mailTemplate)
        {
            var mail = CreateMail(cart, mailTemplate);
            var smtpClient = CreateSmtpClient();

            SendMail(smtpClient, mail);
        }
    }
}