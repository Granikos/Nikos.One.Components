using HtmlAgilityPack;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Nikos.One.Modules
{
    public class HtmlEmbedder
    {
        private readonly static Regex RegexUrl = new Regex(@"url\(\'?(.*[^\'])\'?\)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public string EmbedExternals(string mailBody)
        {
            var document = new HtmlDocument();
            document.LoadHtml(mailBody);

            var wc = new WebClient();

            EmbedExternalStyle(document, wc);
            EmbedStyleTags(document, wc);
            EmbedImageTags(document, wc);

            document.OptionWriteEmptyNodes = true;

            var sb = new StringBuilder();

            using (var w = new StringWriter(sb))
            {
                document.Save(w);
            }

            return sb.ToString();
        }

        private static void EmbedImageTags(HtmlDocument document, WebClient wc)
        {
            var tags = document.DocumentNode.SelectNodes("//img");
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                var src = tag.GetAttributeValue("src", null);

                if (src == null)
                {
                    continue;
                }

                tag.SetAttributeValue("src", EncodeImage(src, wc));
            }
        }

        private static void EmbedExternalStyle(HtmlDocument document, WebClient wc)
        {
            var tags = document.DocumentNode.SelectNodes("//link[@rel='stylesheet']");
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                var src = tag.GetAttributeValue("href", null);
                if (src == null)
                {
                    continue;
                }

                var style = wc.DownloadString(src);
                var newTag = document.CreateElement("style");
                newTag.AppendChild(document.CreateTextNode(style));

                tag.ParentNode.InsertBefore(newTag, tag);
                tag.Remove();
            }
        }

        private static void EmbedStyleTags(HtmlDocument document, WebClient wc)
        {
            var tags = document.DocumentNode.SelectNodes("//style");
            if (tags == null)
            {
                return;
            }

            foreach (var tag in tags)
            {
                var sb = new StringBuilder();
                var start = 0;

                var m = RegexUrl.Match(tag.InnerText);
                while (m.Success)
                {
                    var g = m.Groups[1];
                    sb.Append(tag.InnerText.Substring(start, g.Index));

                    var encodedImage = EncodeImage(g.Value, wc);
                    sb.Append(encodedImage);

                    start = g.Index + g.Length;
                    m = m.NextMatch();
                }

                sb.Append(tag.InnerText.Substring(start));
                tag.RemoveAllChildren();
                tag.AppendChild(document.CreateTextNode(sb.ToString()));
            }
        }

        private static string EncodeImage(string src, WebClient wc)
        {
            string extension = null;
            var ext = Path.GetExtension(src);

            var header = wc.ResponseHeaders["Content-Type"];
            if (header != null && header.Length > 6)
            {
                extension = header.StartsWith("images/", StringComparison.OrdinalIgnoreCase) ? header.Substring(6) : null;
            }

            if (extension == null && !string.IsNullOrWhiteSpace(ext) && ext.Length > 1)
            {
                extension = ext.Substring(1);
            }

            if (extension == null)
            {
                throw new Exception(string.Format("The file extension for the image '{0}' could not be determined.", src));
            }

            var bytes = wc.DownloadData(src);
            var value = Convert.ToBase64String(bytes);
            var encodedImage = string.Format("data:image/{0};base64,{1}", extension, value);
            return encodedImage;
        }
    }
}