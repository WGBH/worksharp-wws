// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using HandlebarsDotNet;

using static WorkSharp.Wws.Builder.XHelper;

namespace WorkSharp.Wws.Builder
{
    class Operation
    {
        static string RemoveForbiddenChars(string value) =>
            value.Replace("-", "").Replace(".", "");

        readonly string _name;
        readonly string _documentation;
        readonly string _requestType;
        readonly string? _responseType;

        public Operation(string name, string documentation, string requestType, string? responseType)
        {
            _name = name;
            _documentation = documentation;
            _requestType = requestType;
            _responseType = responseType;
        }

        public string CsXmlDocComment => "///" + _documentation.Replace("\n", "\n///");
        public string CsName => RemoveForbiddenChars(_name);
        public string CsRequestType => RemoveForbiddenChars(_requestType);
        public string ResponseTypeTask =>
            _responseType == null? "Task" : "Task<" + RemoveForbiddenChars(_responseType) +">";
        public string BaseMethod =>
            _responseType == null? "ExecuteAsync" : "ExecuteAsync<" + RemoveForbiddenChars(_responseType) +">";
    }

    class Endpoint
    {
        readonly string _documentation;

        public string Name { get; }
        public string Version { get; }
        public IReadOnlyList<Operation> Operations { get; }

        public Endpoint(string name, string version, string documentation, IReadOnlyList<Operation> operations)
        {
            Name = name;
            Version = version;
            _documentation = documentation;
            Operations = operations;
        }

        public string CsXmlDocComment => "///" + _documentation.Replace("\n", "\n///");
    }

    static class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
                throw new InvalidOperationException("No Wsdl file specified.");

            var endpoint = await ParseWsdlAsync(args[0]);

            var template = await GetTemplateAsync();

            var clientCode = template(endpoint);

            await File.WriteAllTextAsync(endpoint.Name + "Client.cs", clientCode);

            Console.WriteLine($"{endpoint.Name}Client written with {endpoint.Operations.Count} operations.");
        }

        static async Task<Endpoint> ParseWsdlAsync(string filename)
        {
            using var file = File.OpenRead(filename);

            var xml = await XDocument.LoadAsync(file, default, default);

            var xsd = Ns("http://www.w3.org/2001/XMLSchema");
            var wd = Ns("urn:com.workday/bsvc");
            var wsdl = Ns("http://schemas.xmlsoap.org/wsdl/");

            var endpointName = xml.Root.Attr("name");
            var endpointDoc = xml.Root.El(wsdl + "documentation").Value;
            var endpointVersion = xml.Root
                .El(wsdl + "types").El(xsd + "schema")
                .Elements(xsd + "attribute")
                .Single(a => a.Attr("name") == "version")
                .Attr(wd + "fixed");

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

            return new Endpoint(endpointName, endpointVersion, endpointDoc, operations.ToImmutableList());
        }

        static async Task<HandlebarsTemplate<object, object>> GetTemplateAsync()
        {
            var templateText = await File.ReadAllTextAsync(Path.Join(AppContext.BaseDirectory, "client.cs.hbs"));
            var config = new HandlebarsConfiguration
            {
                NoEscape = true,
                ThrowOnUnresolvedBindingExpression = true
            };
            return Handlebars.Create(config).Compile(templateText);
        }
    }
}
