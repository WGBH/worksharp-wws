# WorkSharp: Modern C# Tools For Workday
## Workday Web Services Clients

WorkSharp WWS provides tools to interact with the various Workday Web Services. It provides a lightweight abstraction over the SOAP interface, with a client for each Web Service endpoint. For each operation in a web service, the respective client presents an async method which takes a strongly-typed request object and returns (a Task resulting in) a strongly-typed response. The pre-built NuGet packages contain all the code needed to interact with Workday, no need to mess around with SvcUtil or WSDLs. They support .NET Standard 2.1 and above and work well with dependency injection in ASP.NET Core or in standalone apps.

This repository hosts the code for the three pieces of the project:
- A set of shared core and utility classes that handle communicating with Workday (Core).
- A one-off console app the handles generating the code for the strongly-typed client for a particular Web Service endpoint, based on the operations in that web service (ClientBuilder).
- A build script that uses the ClientBuilder tool along with [LinqToXsdCore](https://github.com/mamift/LinqToXsdCore) to generate the client and request and response types for each web service in a particular Workday Web Services version. The build script then generates NugGet packages from the generated code and optionally uploads them to a NuGet repository.

### How to Use

Add the NuGet package for your desired WWS endpoint and version to your project. Packages are available on NuGet.org for WWS version 30.0 and up. The package name is WorkSharp.Wws.Endpoints.{Service} and the version matches the WWS version, with a potential patch number. For example, for Financial Management, version 36.2, the package is WorkSharp.Wws.Endpoint.Financial_Management, version 36.2.X. Each package has a dependency on WorkSharp.Wws.Core, which itself is dependent on XObjectsCore (the magic sauce that handles lightweight serialization/deserialization of the web service xml to C# objects). There are no other dependencies outside of the the runtime.

The best place to learn about the web services themselves is to browse the [official documentation](https://community.workday.com/sites/default/files/file-hosting/productionapi/index.html). Client types are named the same as the Workday Web Service endpoint, suffixed with “Client” (e.g. `IHuman_ResourcesClient` and `Human_ResourcesClient` for the Human_Resource endpoint). Client methods are named the same as the Workday operation names, suffixed with “Async” (e.g. `Get_WorkersAsync` for the WWS Human_Resources Get_Workers operation).

### Note about Naming Conventions

The client operations and request/response types follow the Workday standard, including using underscores to separate words (e.g. `Financial_Management`). This is uncommon for C# code, but is done for consistency with Workday.

### Examples

Submit a WWS request, inspect the result, and handle exceptions.
```
using System;
using WorkSharp.Wws
using WorkSharp.Wws.Financial_Management

...

var config = new WwsClient.Configuration
{
    Host = "wd2-impl-services1.workday.com",
    Tenant = "MyCoolTenant",
    UserName = "MyNeatoUser",
    Password = "MyRadPassword" // don’t put passwords in real code!
};

using var client = new Financial_ManagementClient(config);

var ccRefIdType = "Cost_Center_Reference_ID";

// Build the request
var req = new Get_Cost_Centers_Request
{
    Request_References = new Cost_Center_Request_ReferencesType
    {
        Cost_Center_Reference = new[]
        {
            // Use WwsReference utility class to create Workday object references
            WwsReference.Create<Cost_CenterObjectType>(ccRefIdType, "CC1000"),
            WwsReference.Create<Cost_CenterObjectType>(ccRefIdType, "CC2000")
        }
    }
};

try
{
    // Call the web service
    var res = await client.Get_Cost_CentersAsync(req);

    // Inspect the results
    var costCenters = res.Response_Data.Cost_Center.Select(cc => new
    {
        // Use IdOfType extension method to abstract a single ID value
        id = cc.Cost_Center_Reference.IdOfType(ccRefIdType),
        name = cc.Cost_Center_Data.Organization_Data.Organization_Name
    });

    foreach (var cc in costCenters)
    {
        // e.g. "CC1000: My Tubular Cost Center"
        Console.WriteLine($"{cc.id}: {cc.name}");
    }
}
catch (WwsException ex)
{
    // Handle exceptions.
    // The WwsException class contains useful details about
    // validation errors returned by the web service.
    Console.Error.WriteLine($"{ex.FaultCode} occurred.");
    foreach (var err in ex.ValidationErrors)
    {
        Console.Error.WriteLine($"{err.Message} @ {err.XPathExpression}");
    }
}
```

Register and use a Web Service client via dependency injection, e.g. in ASP.NET core.
```
using Microsoft.Extensions.DependencyInjection;
using WorkSharp.Wws;

...

// example ASP.NET Core startup class,
// some methods omitted for brevity
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Register the configuration as a singleton service
        services.AddSingleton(new WwsClient.Configuration
        {
            Host = "wd2-impl-services1.workday.com",
            Tenant = "MyCoolTenant",
            UserName = "MyNeatoUser",
            Password = "MyRadPassword" // don’t put passwords in real code!
        });

        // Register the web service client as a typed HTTP client
        // The runtime cleverly manages the lifetime of the underlying HttpClient
        // that the WwsClient uses, conserving networking resources.
        services.AddHttpClient<IHuman_ResourcesClient, Human_ResourcesClient>();
    }
}

public class MyCoolService
{
    readonly IHuman_ResourcesClient _client;

    // Inject the WwsClient as a dependency in your API controller,
    // Razor Page, service, etc.
    public MyCoolService(IHuman_ResourcesClient client)
    {
        _client = client;
    }
}
```

Use a hand-built (raw xml) WWS request body
```
using System;
using System.Xml.Linq;
using WorkSharp.Wws;
using WorkSharp.Wws.Resource_Management;

...

// define a request as raw XML
var xml = @"
    <bsvc:Get_Projects_Request xmlns:bsvc=""urn:com.workday/bsvc"">
        <bsvc:Request_References>
            <bsvc:Project_Reference>
            <bsvc:ID bsvc:type=""Project_ID"">PROJ-491</bsvc:ID>
            </bsvc:Project_Reference>
        </bsvc:Request_References>
    </bsvc:Get_Projects_Request>";

// Parse the xml and cast it to the correct request type
var req = (Get_Projects_Request) XElement.Parse(xml);

// Now we can inspect the request as a strongly-typed object,
// or send it to Workday via the strongly-typed client
```

Use LINQ to XML to inspect a response
```
using System.Linq;
using System.Xml.Linq;
using WorkSharp.Wws;
using WorkSharp.Wws.Resource_Management;

...

// Example strongly-typed WWS response
var res = new Get_Projects_Response
{
    Response_Data = new Project_Response_DataType
    {
        Project = new[]
        {
            new ProjectType
            {
                Project_Data = new Project_DataType
                {
                    Workday_Project_ID = "PRJ-147",
                    Project_Name = "My Awesome Project",
                    Inactive = false
                }
            },
            new ProjectType
            {
                Project_Data = new Project_DataType
                {
                    Workday_Project_ID = "PRJ-324",
                    Project_Name = "My Tubular Project",
                    Inactive = false
                }
            }
        }
    }
};

// Cast strongly-typed response to XElement
// and extract values using LINQ to XML
var projectNames = ((XElement) res)
    .Descendants(WwsDefaults.Namespace + "Project_Name")
    .Select(p => p.Value)
    .ToList(); // ["My Awesome Project", "My Tubular Project"]
```
