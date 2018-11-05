﻿using AngleSharp.Parser.Html;
using NJsonSchema.CodeGeneration;

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
        public string AspNetNamespace { get; private set; }

        /// <summary>
        /// WebApiClient的模型描述
        /// </summary>
        /// <param name="codeArtifact">源代码</param>
        /// <param name="nameSpace">命名空间</param>
        public HttpModel(CodeArtifact codeArtifact, string nameSpace)
           : base(codeArtifact)
        {
            this.AspNetNamespace = nameSpace;
        }

        /// <summary>
        /// 转换为完整的代码
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var html = ViewTempate.View(this);
            var source = new HtmlParser().Parse(html).Body.InnerText;
            return new CSharpCode(source, this.TypeName, this.Type).ToString();
        }
    }
}
