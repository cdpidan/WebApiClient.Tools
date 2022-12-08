using System.Collections.Generic;
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
        /// 忽略的API路径
        /// </summary>
        [Option('p', "IgnorePaths", HelpText = "忽略的API路径")]
        public IEnumerable<string> IgnorePaths { get; set; }

        /// <summary>
        /// 忽略的实体定义
        /// </summary>
        [Option('d', "IgnoreDefinitions", HelpText = "忽略的实体定义")]
        public IEnumerable<string> IgnoreDefinitions { get; set; }

        /// <summary>
        /// 只生成这些接口
        /// </summary>
        [Option('l', "ApiList", HelpText = "只生成这些接口")]
        public IEnumerable<string> ApiList { get; set; }

        /// <summary>
        /// Json类型
        /// </summary>
        [Option('j', "library", MetaValue = "JsonLibrary", Required = false, HelpText = "Json类型：0 NewtonsoftJson；1 SystemTextJson")]
        public CSharpJsonLibrary JsonLibrary { get; set; }
    }
}