﻿using AngleSharp.Parser.Html;
using RazorEngine;
using RazorEngine.Templating;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WebApiClient.Tools.Swagger
{
    [DebuggerDisplay("Class = {Class}")]
    public class HttpModel : Code
    {
        public string AspNetNamespace { get; private set; }

        private static readonly ViewTempate view = new ViewTempate("HttpModel");

        static HttpModel()
        {
            Engine.Razor.AddTemplate(view.ViewName, view);
        }

        private HttpModel(string code, string nameSpace)
            : base(code)
        {
            this.AspNetNamespace = nameSpace;
        }

        public static HttpModel[] FromCodes(string codes, string nameSpace)
        {
            var builder = new StringBuilder();
            var list = new List<HttpModel>();
            var reader = new StringReader(codes);

            while (reader.Peek() >= 0)
            {
                var str = reader.ReadLine();
                builder.AppendLine(str.Replace("«", "<").Replace("»", ">"));

                if (string.Equals(str, "}") == true)
                {
                    list.Add(new HttpModel(builder.ToString(), nameSpace));
                    builder.Clear();
                }
            }
            return list.ToArray();
        }

        public override string ToString()
        {
            var html = Engine.Razor.RunCompile(view.ViewName, this.GetType(), this);
            var source = new HtmlParser().Parse(html).Body.InnerText;
            return new Code(source).ToString();
        }
    }
}
