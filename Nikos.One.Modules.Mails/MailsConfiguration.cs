using Nikos.One.Composition;
using System.Collections.Generic;

namespace Nikos.One.Modules
{
    public class MailsConfiguration : ComponentConfiguration
    {
        public virtual string SmtpServer { get; set; }

        public virtual int Port { get; set; }

        public virtual string ServerUser { get; set; }

        public virtual string ServerPasswort { get; set; }

        public virtual bool UseSsl { get; set; }

        public virtual string ServiceProviderName { get; set; }

        private List<MailTemplate> _mailTemplates;

        public virtual List<MailTemplate> MailTemplates
        {
            get { return _mailTemplates ?? (_mailTemplates = new List<MailTemplate>()); }
            set { _mailTemplates = value; }
        }

        public MailsConfiguration()
        {
            IgnoreErrors = true;
        }
    }
}