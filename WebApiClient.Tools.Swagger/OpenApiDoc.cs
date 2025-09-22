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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema.Infrastructure;

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
        public OpenApiDoc(OpenApiDocOptions options) : this(GetDocument(options))
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

            Settings.Reverse = options.Reverse;
            Settings.ApiPrefix = options.ApiPrefix ?? string.Empty;
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
        /// <param name="options"></param>
        /// <returns></returns>
        private static OpenApiDocument GetDocument(OpenApiDocOptions options)
        {
            var swagger = options.OpenApi;
            Console.WriteLine($"正在分析OpenApi：{swagger}");
            if (Uri.TryCreate(swagger, UriKind.Absolute, out _))
            {
                return FromUrlAsync(options).GetAwaiter().GetResult();
            }

            return OpenApiDocument.FromFileAsync(swagger).GetAwaiter().GetResult();
        }

        private static async Task<OpenApiDocument> FromUrlAsync(OpenApiDocOptions options,
            CancellationToken cancellationToken = default)
        {
            var handler = new HttpClientHandler();
            if (options.BasicUserName != null && options.BasicPassword != null)
            {
                handler.Credentials = new System.Net.NetworkCredential(options.BasicUserName, options.BasicPassword);
            }

            using var client = new HttpClient(handler);
            var response = await client.GetAsync(options.OpenApi, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var data = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            // 移除BasePath
            if (!string.IsNullOrWhiteSpace(options.BasePath) && data.Contains($"\"{options.BasePath}/"))
            {
                data = data.Replace($"\"{options.BasePath}/", "\"/");
            }

            if (data.Contains('&'))
            {
                Console.WriteLine("包含字符：&");
                data = data.Replace('&', '＆');
            }

            var jObject = JObject.Parse(data);
            var paths = (JObject)jObject.SelectToken("paths");
            var definitions = (JObject)jObject.SelectToken("definitions");

            // 移除path
            foreach (var path in options.IgnorePaths)
            {
                paths?.Property(path)?.Remove();
            }

            // 移除definition
            foreach (var definition in options.IgnoreDefinitions)
            {
                definitions?.Property(definition)?.Remove();
            }

            // 只生成部分API
            if (options.ApiList.Any() && paths != null && definitions != null)
            {
                var properties = paths.Properties().ToList();
                foreach (var property in properties.Where(property => !options.ApiList.Contains(property.Name)))
                {
                    paths.Property(property.Name)?.Remove();
                }

                var definitionsHashSet = new HashSet<string>();
                foreach (var item in GetDefinitionsFromJson(null, paths))
                {
                    ProcessChild(definitionsHashSet, definitions, item);
                }

                var list = definitions.Properties().ToList();
                foreach (var definition in list.Where(definition => !definitionsHashSet.Contains(definition.Name)))
                {
                    definitions.Property(definition.Name)?.Remove();
                }
            }

            // 处理json错误
            OpenApiDocument openApiDocument;
            while (true)
            {
                try
                {
                    openApiDocument = await OpenApiDocument
                        .FromJsonAsync(jObject.ToString(), options.OpenApi, cancellationToken)
                        .ConfigureAwait(false);
                    break;
                }
                catch (Exception e)
                {
                    var match = Regex.Match(e.Message, "Could not resolve the path '(.*?)'.");
                    if (e.Source != "NJsonSchema" || !match.Success) throw;

                    var processed = false;
                    if (paths != null)
                    {
                        foreach (var path in paths)
                        {
                            if (path.Value?.ToString().Contains($"\"{match.Groups[1].Value}\"") != true)
                                continue;

                            Console.WriteLine($"解析swagger文档异常：{e.Message}，准备移除Path");
                            paths.Property(path.Key)?.Remove();
                            processed = true;
                            break;
                        }
                    }

                    if (!processed && definitions != null)
                    {
                        foreach (var definition in definitions)
                        {
                            if (definition.Value?.ToString().Contains($"\"{match.Groups[1].Value}\"") != true) continue;

                            Console.WriteLine($"解析swagger文档异常：{e.Message}，准备移除Definition");
                            definitions.Property(definition.Key)?.Remove();
                            break;
                        }
                    }
                }
            }

            return openApiDocument;
        }

        /// <summary>
        /// 递归处理实体
        /// </summary>
        /// <param name="definitionsHashSet"></param>
        /// <param name="definitions"></param>
        /// <param name="modelName"></param>
        private static void ProcessChild(ISet<string> definitionsHashSet, JObject definitions, string modelName)
        {
            definitionsHashSet.Add(modelName);
            foreach (var definition in definitions)
            {
                if (definition.Key != modelName) continue;

                foreach (var child in GetDefinitionsFromJson(modelName, definition.Value))
                {
                    ProcessChild(definitionsHashSet, definitions, child);
                }
            }
        }

        private static IEnumerable<string> GetDefinitionsFromJson<T>(string currentModelName, T paths)
        {
            var json = JsonConvert.SerializeObject(paths);
            var matches = Regex.Matches(json, "(?s)\"\\$ref\":\"#/definitions/(.*?)\"");
            var definitionList = matches.Where(x => x.Success)
                .Select(x => x.Groups[1].Value)
                .Where(x => x != currentModelName); //需要排除当前模型，不然会死循环
            return definitionList;
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
                return Array.Empty<CodeArtifact>();
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
                return new HttpApiMethod(operation, (CSharpGeneratorBaseSettings)settings, this,
                    (CSharpTypeResolver)Resolver, _openApiDoc.Settings.TaskReturnType);
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