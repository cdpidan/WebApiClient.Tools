using System.Threading.Tasks;
using RazorEngineCore;

namespace WebApiClient.Tools.Swagger
{
    /// <summary>
    /// html模板
    /// </summary>
    public class HtmlTemplate : RazorEngineTemplateBase
    {
        /// <summary>
        /// html标签转换
        /// </summary>
        /// <param name="obj"></param>
        public override async Task WriteAsync(object obj = null)
        {
            var text = obj?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Replace("<", "&lt;").Replace(">", "&gt;");
            }

            await base.WriteAsync(text);
        }
    }

    /// <summary>
    /// html模板
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HtmlTemplate<T> : HtmlTemplate
    {
        public new T Model { get; set; }
    }
}