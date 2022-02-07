using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;

namespace WebApiClient.Tools.Swagger
{
    /// <summary>
    /// 表示WebApiClient的模型描述
    /// </summary>
    public class HttpModel : CSharpCode
    {
        /// <summary>
        /// 获取使用的命名空间
        /// </summary>
        public string NameSpace { get; }
        
        /// <summary>
        /// UseSystemTextJson
        /// </summary>
        public bool UseSystemTextJson { get; }

        /// <summary>
        /// WebApiClient的模型描述
        /// </summary>
        /// <param name="codeArtifact">源代码</param>
        /// <param name="settings">命名空间</param>
        public HttpModel(CodeArtifact codeArtifact, HttpApiSettings settings)
            : base(codeArtifact)
        {
            NameSpace = settings.NameSpace;
            UseSystemTextJson = settings.CSharpGeneratorSettings.JsonLibrary == CSharpJsonLibrary.SystemTextJson;
        }

        /// <summary>
        /// 转换为完整的代码
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var cshtml = CSharpHtml.Views<HttpModel>();
            var source = cshtml.RenderText(this);
            return new CSharpCode(source, TypeName, Type).ToString();
        }
    }
}