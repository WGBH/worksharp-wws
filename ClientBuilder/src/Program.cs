using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Scriban;
using static Workday.Wws.Builder.XHelper;

namespace Workday.Wws.Builder
{
    record Operation(string Name, string Documentation, string RequestType, string ResponseType)
    {
        public string DocumentationAsCsXmlDocComment => Documentation.Replace("\n", "\n///");
        public string NameSafe => Name.Replace("-", "");
        public string RequestTypeSafe => RequestType.Replace("-", "");
        public string ResponseTypeSafe =>
            ResponseType == null? "Task" : "Task<" + ResponseType.Replace("-", "") +">";
        public string Method =>
            ResponseType == null? "ExecuteAsync" : "ExecuteAsync<" + ResponseType.Replace("-", "") +">";
    }

    record Endpoint(string Name, string Documentation, IReadOnlyList<Operation> Operations)
    {
        public string DocumentationAsCsXmlDocComment => Documentation.Replace("\n", "\n///");
    }

    static class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
                throw new InvalidOperationException("No Wsdl file specified.");

            var endpoint = await ParseWsdlAsync(args[0]);

            var template = await GetTemplateAsync();

            var clientCode = template.Render(endpoint);

            await File.WriteAllTextAsync(endpoint.Name + "Client.cs", clientCode);

            Console.WriteLine($"{endpoint.Name}Client written with {endpoint.Operations.Count} operations.");
        }

        static async Task<Endpoint> ParseWsdlAsync(string filename)
        {
            using var file = File.OpenRead(filename);

            var xml = await XDocument.LoadAsync(file, default, default);

            var wsdl = Ns("http://schemas.xmlsoap.org/wsdl/");

            var endpointName = xml.Root.Attr("name");
            var endpointDoc = xml.Root.El(wsdl + "documentation").Value;

            var operationsXml = xml.Root.El(wsdl + "portType").Elements(wsdl + "operation");
            var messagesXml = xml.Root.Elements(wsdl + "message");

            var operations = new List<Operation>();

            foreach (var operation in operationsXml)
            {
                var operName = operation.Attr("name");

                var operDoc = operation.El(wsdl + "documentation").Value;

                var inputMessage = operation.El(wsdl + "input").Attr("message").TrimNs();
                var requestType = messagesXml
                    .Single(m => m.Attr("name") == inputMessage)
                    .Elements(wsdl + "part")
                    .Single(m => m.Attr("name") == "body")
                    .Attr("element").TrimNs();

                var outputMessage = operation.El(wsdl + "output").Attr("message").TrimNs();
                var responseType = messagesXml.Single(m => m.Attr("name") == outputMessage)
                    .Elements(wsdl + "part")
                    .SingleOrDefault(m => m.Attr("name") == "body")
                    ?.Attr("element").TrimNs();

                operations.Add(new Operation(operName, operDoc, requestType, responseType));
            }

            return new Endpoint(endpointName, endpointDoc, operations.ToImmutableList());
        }

        static async Task<Template> GetTemplateAsync()
        {
            var templateText = await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, "client.cs.template"));
            return Template.Parse(templateText);
        }
    }
}
