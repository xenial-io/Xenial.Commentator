using System.Text.RegularExpressions;

namespace Xenial.Commentator.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// https://github.com/stiang/remove-markdown/blob/master/index.js
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string StripMarkdownTags(string content)
        {
            // Headers
            content = Regex.Replace(content, "/\n={2,}/g", "\n");
            // Strikethrough
            //content = Regex.Replace(content, "/~~/g", "");
            // Codeblocks
            //content = Regex.Replace(content, "/`{3}.*\n/g", "");
            // HTML Tags
            content = Regex.Replace(content, "/<[^>]*>/g", "");
            // Remove setext-style headers
            content = Regex.Replace(content, "/^[=\\-]{2,}\\s*$/g", "");
            // Footnotes
            content = Regex.Replace(content, "/\\[\\^.+?\\](\\: .*?$)?/g", "");
            content = Regex.Replace(content, "/\\s{0,2}\\[.*?\\]: .*?$/g", "");
            // Images
            content = Regex.Replace(content, "/\\!\\[.*?\\][\\[\\(].*?[\\]\\)]/g", "");
            // Links
            // content = Regex.Replace(content, "/\\[(.*?)\\][\\[\\(].*?[\\]\\)]/g", "$1");
            return content;
        }
    }
}