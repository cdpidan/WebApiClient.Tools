using CommandLine;

namespace WebApiClient.Tools.Swagger
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default
                .ParseArguments<SwaggerOptions>(args)
                .WithParsed(options =>
                {
                    var swagger = new Swagger(options);
                    swagger.GenerateFiles();
                });
        }
    }
}