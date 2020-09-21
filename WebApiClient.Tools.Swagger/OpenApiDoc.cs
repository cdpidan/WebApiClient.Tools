using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WebApiClient.Tools.Swagger
{
    /// <summary>
    /// 表示Swagger描述
    /// </summary>
    public class OpenApiDoc
    {
        private readonly CSharpTypeResolver _resolver;

        /// <summary>
        /// 获取Swagger文档
        /// </summary>
        public OpenApiDocument Document { get; }

        /// <summary>
        /// 获取Swagger设置项
        /// </summary>
        public HttpApiSettings Settings { get; }

        /// <summary>
        /// Swagger描述
        /// </summary>
        /// <param name="options">选项</param>
        public OpenApiDoc(OpenApiDocOptions options) : this(GetDocument(options.OpenApi))
        {
            if (string.IsNullOrEmpty(options.Namespace) == false)
            {
                Settings.NameSpace = options.Namespace;
                Settings.CSharpGeneratorSettings.Namespace = options.Namespace;
            }

            if (string.IsNullOrWhiteSpace(options.TaskReturnType) == false)
            {
                Settings.TaskReturnType = options.TaskReturnType;
            }
        }

        /// <summary>
        /// Swagger描述
        /// </summary>
        /// <param name="document">Swagger文档</param>
        public OpenApiDoc(OpenApiDocument document)
        {
            Document = document;
            Settings = new HttpApiSettings();

            _resolver = CSharpGeneratorBase
                .CreateResolverWithExceptionSchema(Settings.CSharpGeneratorSettings, document);
        }

        /// <summary>
        /// 获取swagger文档
        /// </summary>
        /// <param name="swagger"></param>
        /// <returns></returns>
        private static OpenApiDocument GetDocument(string swagger)
        {
            Console.WriteLine($"正在分析OpenApi：{swagger}");
            if (Uri.TryCreate(swagger, UriKind.Absolute, out _))
            {
                return OpenApiDocument.FromUrlAsync(swagger).Result;
            }

            return OpenApiDocument.FromFileAsync(swagger).Result;
        }

        /// <summary>
        /// 生成代码并保存到文件
        /// </summary>
        public void GenerateFiles()
        {
            var dir = Path.Combine("output", Settings.NameSpace);
            var apisPath = Path.Combine(dir, "HttpApis");
            var modelsPath = Path.Combine(dir, "HttpModels");

            Directory.CreateDirectory(apisPath);
            Directory.CreateDirectory(modelsPath);

            var apis = new HttpApiProvider(this).GetHttpApis();
            foreach (var api in apis)
            {
                var file = Path.Combine(apisPath, $"{api.TypeName}.cs");
                File.WriteAllText(file, api.ToString(), Encoding.UTF8);
                Console.WriteLine($"输出接口文件：{file}");
            }

            var models = new HttpModelProvider(this).GetHttpModels();
            foreach (var model in models)
            {
                var file = Path.Combine(modelsPath, $"{model.TypeName}.cs");
                File.WriteAllText(file, model.ToString(), Encoding.UTF8);
                Console.WriteLine($"输出模型文件：{file}");
            }

            Console.WriteLine($"共输出{apis.Length + models.Length}个文件..");
        }


        /// <summary>
        /// 表示HttpApi提供者
        /// </summary>
        private class HttpApiProvider : CSharpControllerGenerator
        {
            /// <summary>
            /// swagger
            /// </summary>
            private readonly OpenApiDoc _openApiDoc;

            /// <summary>
            /// api列表
            /// </summary>
            private readonly List<HttpApi> _httpApiList = new List<HttpApi>();

            /// <summary>
            /// HttpApi提供者
            /// </summary>
            /// <param name="openApiDoc"></param>
            public HttpApiProvider(OpenApiDoc openApiDoc)
                : base(openApiDoc.Document, openApiDoc.Settings, openApiDoc._resolver)
            {
                _openApiDoc = openApiDoc;
            }

            /// <summary>
            /// 获取所有HttpApi描述模型
            /// </summary>
            /// <returns></returns>
            public HttpApi[] GetHttpApis()
            {
                _httpApiList.Clear();
                GenerateFile();
                return _httpApiList.ToArray();
            }

            /// <summary>
            /// 生成客户端调用代码
            /// 但实际只为了获得HttpApi实例
            /// </summary>
            protected override IEnumerable<CodeArtifact> GenerateClientTypes(string controllerName,
                string controllerClassName, IEnumerable<CSharpOperationModel> operations)
            {
                var model = new HttpApi(controllerClassName, operations, _openApiDoc.Document, _openApiDoc.Settings);
                _httpApiList.Add(model);
                return new CodeArtifact[0];
            }

            /// <summary>
            /// 生成文件
            /// 这里不生成
            /// </summary>
            protected override string GenerateFile(IEnumerable<CodeArtifact> clientTypes,
                IEnumerable<CodeArtifact> dtoTypes, ClientGeneratorOutputType outputType)
            {
                return string.Empty;
            }

            /// <summary>
            /// 创建操作描述
            /// 这里创建HttpApiOperation
            /// </summary>
            /// <param name="operation"></param>
            /// <param name="settings"></param>
            /// <returns></returns>
            protected override CSharpOperationModel CreateOperationModel(OpenApiOperation operation,
                ClientGeneratorBaseSettings settings)
            {
                return new HttpApiMethod(operation, (CSharpGeneratorBaseSettings) settings, this,
                    (CSharpTypeResolver) Resolver, _openApiDoc.Settings.TaskReturnType);
            }
        }

        /// <summary>
        /// 表示HttpModel提供者
        /// </summary>
        private class HttpModelProvider : CSharpGenerator
        {
            /// <summary>
            /// swagger
            /// </summary>
            private readonly OpenApiDoc _openApiDoc;

            /// <summary>
            /// HttpModel提供者
            /// </summary>
            /// <param name="openApiDoc"></param>
            public HttpModelProvider(OpenApiDoc openApiDoc)
                : base(openApiDoc.Document, openApiDoc.Settings.CSharpGeneratorSettings, openApiDoc._resolver)
            {
                _openApiDoc = openApiDoc;
            }

            /// <summary>
            /// 获取所有HttpModels
            /// </summary>
            /// <returns></returns>
            public HttpModel[] GetHttpModels()
            {
                return GenerateTypes()
                    .Select(item => new HttpModel(item, _openApiDoc.Settings.NameSpace))
                    .ToArray();
            }
        }
    }
}