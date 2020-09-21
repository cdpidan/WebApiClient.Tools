using CommandLine;

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
                    var swagger = new OpenApiDoc(options);
                    swagger.GenerateFiles();
                });
        }
    }
}