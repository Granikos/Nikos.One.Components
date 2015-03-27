using Nikos.One.Models.Store;
using System.Collections.Generic;

namespace Nikos.One.Modules
{
    public class MailTemplate : DataStoreItem
    {
        public virtual string Template { get; set; }

        public virtual string TemplatingEngine { get; set; }

        public virtual bool IsEmbedded { get; set; }

        public virtual string MailSubject { get; set; }

        public virtual bool IsHtmlMail { get; set; }

        internal string PreproccessedTemplate { get; set; }

        private List<File> _attachments;

        public virtual List<File> Attachments
        {
            get { return _attachments ?? (_attachments = new List<File>()); }
            set { _attachments = value; }
        }

        public virtual MailAddressTemplate Sender { get; set; }

        private List<MailAddressTemplate> _to;

        public virtual List<MailAddressTemplate> To
        {
            get { return _to ?? (_to = new List<MailAddressTemplate>()); }
            set { _to = value; }
        }

        private List<MailAddressTemplate> _cc;

        public virtual List<MailAddressTemplate> Cc
        {
            get { return _cc ?? (_cc = new List<MailAddressTemplate>()); }
            set { _cc = value; }
        }

        private List<MailAddressTemplate> _bcc;

        public virtual List<MailAddressTemplate> Bcc
        {
            get { return _bcc ?? (_bcc = new List<MailAddressTemplate>()); }
            set { _bcc = value; }
        }
    }
}