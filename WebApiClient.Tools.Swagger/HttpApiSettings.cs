using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;
using NSwag.CodeGeneration.OperationNameGenerators;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebApiClient.Tools.Swagger
{
    /// <summary>
    /// 表示WebApiClient接口设置模型
    /// </summary>
    public class HttpApiSettings : CSharpControllerGeneratorSettings
    {
        /// <summary>
        /// 获取或设置命名空间
        /// </summary>
        public string NameSpace { get; set; } = "WebApiClientCore";

        /// <summary> 
        /// Task返回值类型，默认为:ITask
        /// </summary>
        public string TaskReturnType { get; set; }

        /// <summary>
        /// 反转ControllerName和Summary
        /// </summary>
        public bool Reverse { get; set; }

        /// <summary>
        /// 反转ControllerName和Summary
        /// </summary>
        public string ApiPrefix { get; set; }

        /// <summary>
        /// WebApiClient接口设置模型
        /// </summary>
        public HttpApiSettings()
        {
            ResponseArrayType = "List";
            ResponseDictionaryType = "Dictionary";
            ParameterArrayType = "IEnumerable";
            ParameterDictionaryType = "IDictionary";
            TaskReturnType = "ITask";
            ApiPrefix = string.Empty;

            OperationNameGenerator = new OperationNameProvider();
            ParameterNameGenerator = new ParameterNameProvider();
            CSharpGeneratorSettings.TypeNameGenerator = new TypeNameProvider();
            CSharpGeneratorSettings.ClassStyle = CSharpClassStyle.Poco;
            CSharpGeneratorSettings.GenerateJsonMethods = false;
            CSharpGeneratorSettings.JsonLibrary = CSharpJsonLibrary.NewtonsoftJson;
            RouteNamingStrategy = CSharpControllerRouteNamingStrategy.OperationId;
        }

        /// <summary>
        /// 方法名称提供者
        /// </summary>
        private class OperationNameProvider : MultipleClientsFromOperationIdOperationNameGenerator
        {
            /// <summary>
            /// 获取方法对应的类名
            /// </summary>
            /// <param name="document"></param>
            /// <param name="path"></param>
            /// <param name="httpMethod"></param>
            /// <param name="operation"></param>
            /// <returns></returns>
            public override string GetClientName(OpenApiDocument document, string path, string httpMethod,
                OpenApiOperation operation)
            {
                return operation.Tags.FirstOrDefault();
            }
        }

        /// <summary>
        /// 参数名提供者
        /// </summary>
        private class ParameterNameProvider : IParameterNameGenerator
        {
            /// <summary>
            /// 生成参数名
            /// </summary>
            /// <param name="parameter"></param>
            /// <param name="allParameters"></param>
            /// <returns></returns>
            public string Generate(OpenApiParameter parameter, IEnumerable<OpenApiParameter> allParameters)
            {
                if (string.IsNullOrEmpty(parameter.Name))
                {
                    return "unnamed";
                }

                var variableName = CamelCase(parameter.Name
                    .Replace("-", "_")
                    .Replace(".", "_")
                    .Replace(" ", null)
                    .Replace("$", string.Empty)
                    .Replace("[", string.Empty)
                    .Replace("]", string.Empty));

                if (allParameters.Count(p => p.Name == parameter.Name) > 1)
                    return variableName + parameter.Kind;

                return variableName;
            }


            /// <summary>
            /// 骆驼命名
            /// </summary>
            /// <param name="name">名称</param>
            /// <returns></returns>
            private static string CamelCase(string name)
            {
                if (string.IsNullOrEmpty(name) || char.IsUpper(name[0]) == false)
                {
                    return name;
                }

                var charArray = name.ToCharArray();
                for (var i = 0; i < charArray.Length; i++)
                {
                    if (i == 1 && char.IsUpper(charArray[i]) == false)
                    {
                        break;
                    }

                    var hasNext = (i + 1 < charArray.Length);
                    if (i > 0 && hasNext && !char.IsUpper(charArray[i + 1]))
                    {
                        if (char.IsSeparator(charArray[i + 1]))
                        {
                            charArray[i] = char.ToLowerInvariant(charArray[i]);
                        }

                        break;
                    }

                    charArray[i] = char.ToLowerInvariant(charArray[i]);
                }

                return new string(charArray);
            }
        }

        /// <summary>
        /// 类型名称提供者
        /// </summary>
        private class TypeNameProvider : DefaultTypeNameGenerator
        {
            public override string Generate(JsonSchema schema, string typeNameHint,
                IEnumerable<string> reservedTypeNames)
            {
                var prettyName = PrettyName(typeNameHint);
                var typeName = base.Generate(schema, prettyName, reservedTypeNames);
                return typeName;
            }

            /// <summary>
            /// 美化类型名称
            /// </summary>
            /// <param name="name">名称</param>
            /// <returns></returns>
            private static string PrettyName(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return name;
                }

                if (name.Contains("[]"))
                {
                    name = name.Replace("[]", "Array");
                }

                // 处理字典
                if (Regex.IsMatch(name, "Map«(.*?),(.*?)»"))
                {
                    name = Regex.Replace(name, "Map«(.*?),(.*?)»", "Dictionary<$1, $2>");
                }

                var matches = Regex.Matches(name, @"\W");
                if (matches.Count == 0 || matches.Count % 2 > 0)
                {
                    return name;
                }

                //泛型
                var index = -1;
                return Regex.Replace(name, @"\W", m =>
                {
                    index += 1;
                    return index < matches.Count / 2 ? "Of" : null;
                });
            }
        }
    }
}