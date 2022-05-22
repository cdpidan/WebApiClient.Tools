using System;
using CommandLine;
using Newtonsoft.Json;

namespace WebApiClient.Tools.Swagger
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<OpenApiDocOptions>(args)
                .WithParsed(options =>
                {
                    Console.WriteLine(JsonConvert.SerializeObject(options));
                    var swagger = new OpenApiDoc(options);
                    swagger.GenerateFiles();
                });
        }
    }
}