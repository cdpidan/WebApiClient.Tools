using CommandLine;
using NJsonSchema.CodeGeneration.CSharp;

namespace WebApiClient.Tools.Swagger
{
    /// <summary>
    /// 表示命令选项
    /// </summary>
    public class OpenApiDocOptions
    {
        /// <summary>
        /// openApi的json本地文件路径或远程Uri地址
        /// </summary>
        [Option('o', "openapi", MetaValue = "OpenApi", Required = true, HelpText = "OpenApi的本地文件路径或远程Uri地址")]
        public string OpenApi { get; set; }

        /// <summary>
        /// 代码的命名空间
        /// </summary>
        [Option('n', "namespace", MetaValue = "Namespace", Required = false,
            HelpText = "代码的命名空间，如WebApiClient.Swagger")]
        public string Namespace { get; set; }

        /// <summary>
        /// 代码的命名空间
        /// </summary>
        [Option('t', "task", MetaValue = "TaskReturnType", Required = false, HelpText = "Task返回值类型，如: ITask、Task")]
        public string TaskReturnType { get; set; }

        /// <summary>
        /// Json类型
        /// </summary>
        [Option('l', "library", MetaValue = "JsonLibrary", Required = false, HelpText = "Json类型：0 NewtonsoftJson；1 SystemTextJson")]
        public CSharpJsonLibrary JsonLibrary { get; set; }
    }
}