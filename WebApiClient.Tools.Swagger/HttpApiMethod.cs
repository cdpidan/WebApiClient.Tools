﻿using System.Text.RegularExpressions;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using NSwag.CodeGeneration.CSharp.Models;

namespace WebApiClient.Tools.Swagger
{
    /// <summary>
    /// 表示WebApiClient的请求方法数据模型
    /// </summary>
    public class HttpApiMethod : CSharpOperationModel
    {
        private readonly string _settingsTaskReturnType;

        /// <summary>
        /// WebApiClient的请求方法数据模型
        /// </summary>
        /// <param name="operation">Swagger操作</param>
        /// <param name="settings">设置项</param>
        /// <param name="generator">代码生成器</param>
        /// <param name="resolver">语法解析器</param>
        /// <param name="settingsTaskReturnType"></param>
        public HttpApiMethod(OpenApiOperation operation, CSharpGeneratorBaseSettings settings,
            CSharpGeneratorBase generator, CSharpTypeResolver resolver, string settingsTaskReturnType)
            : base(operation, settings, generator, resolver)
        {
            _settingsTaskReturnType = settingsTaskReturnType;
        }

        /// <summary>
        /// 获取方法的返回类型
        /// 默认使用ITask
        /// </summary>
        public override string ResultType
        {
            get
            {
                switch (SyncResultType)
                {
                    case "void":
                        return "Task";
                    case "FileResult":
                        return $"{_settingsTaskReturnType}<HttpResponseMessage>";
                    default:
                        return $"{_settingsTaskReturnType}<{SyncResultType}>";
                }
            }
        }

        /// <summary>
        /// 获取方法好友名称
        /// </summary>
        public override string ActualOperationName
        {
            get
            {
                var name = base.ActualOperationName;
                if (Regex.IsMatch(name, @"^\d") == false)
                {
                    return name;
                }

                name = Regex.Match(Id, @"\w*").Value;
                if (string.IsNullOrEmpty(name))
                {
                    name = "unnamed";
                }

                var names = name.ToCharArray();
                names[0] = char.ToUpper(names[0]);
                return new string(names);
            }
        }

        /// <summary>
        /// 解析参数名称
        /// 将文件参数声明为FormDataFile
        /// </summary>
        /// <param name="parameter">参数</param>
        /// <returns></returns>
        protected override string ResolveParameterType(OpenApiParameter parameter)
        {
            var schema = parameter.ActualSchema;
            if (schema.Type == JsonObjectType.File)
            {
                if (parameter.CollectionFormat == OpenApiParameterCollectionFormat.Multi &&
                    !schema.Type.HasFlag(JsonObjectType.Array))
                {
                    return "IEnumerable<FormDataFile>";
                }

                return "FormDataFile";
            }

            return base.ResolveParameterType(parameter);
        }
    }
}