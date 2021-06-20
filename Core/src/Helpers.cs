using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace Workday.Wws
 {
    public class WwsException : Exception
    {
        public override string Message { get; }

        internal WwsException(XDocument WwsResponse)
        {
            Message = WwsResponse.Descendants("faultstring").FirstOrDefault()?.Value
                ?? "A workday web service exception occurred";
        }
    }

    public static class WwsDefaults
    {
        public static readonly XNamespace Namespace = "urn:com.workday/bsvc";
        public static readonly string Version = "v" +
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
    }
}