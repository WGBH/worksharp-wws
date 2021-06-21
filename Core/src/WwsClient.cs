using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using WorkSharp.Wws.Internal;
using Xml.Schema.Linq;

using static WorkSharp.Wws.Internal.XHelper;

namespace WorkSharp.Wws
{
    public abstract class WwsClient
    {
        public class Config
        {
            public string Host { get; set; }
            public string UserName { get; set; }
            public string Tenant { get; set; }
            public string Password { get; set; }
        }

        static readonly XNamespace Env = "http://schemas.xmlsoap.org/soap/envelope/";
        static readonly XNamespace Sec = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        readonly Config _config;
        readonly HttpClient _client;

        protected WwsClient(Config config, HttpClient client)
        {
            _config = config;

            client.BaseAddress = new Uri($"https://{config.Host}/ccx/service/{config.Tenant}/");
            _client = client;
        }

        protected async Task<T> ExecuteAsync<T>(XTypedElement request) where T: XTypedElement =>
            (await ExecuteAsync(request)).ConvertTo<T>();

        protected Task<XElement> ExecuteAsync(XTypedElement request)
        {
            if (request == null)
            throw new ArgumentNullException(nameof(request));

            var namespaze = request.GetType().Namespace;
            var endpoint = namespaze.Substring(namespaze.LastIndexOf('.') + 1);

            var fullRequest = BuildFullRequest(request);

            return PostRequest(endpoint, fullRequest);
        }

        XDocument BuildFullRequest(XTypedElement request) =>
            new XDocument(
                El(Env + "Envelope",
                    El(Env + "Header",
                        El(Sec + "Security",
                            El(Sec + "UsernameToken",
                                El(Sec + "Username", _config.UserName + "@" + _config.Tenant),
                                El(Sec + "Password", _config.Password)
                            )
                        ),
                        El(WwsDefaults.Namespace + "Workday_Common_Header",
                            El(WwsDefaults.Namespace + "Include_Reference_Descriptors_In_Response", true)
                        )
                    ),
                    El(Env + "Body", El((XElement) request))
                )
            );

        async Task<XElement> PostRequest(string endpoint, XDocument fullRequest)
        {
            HttpResponseMessage res;

            using (var reqStream = new MemoryStream())
            {
                var writer = XmlWriter.Create(reqStream);
                fullRequest.WriteTo(writer);
                writer.Flush();
                reqStream.Position = 0;

                res = await _client.PostAsync(endpoint + "/" + WwsDefaults.Version, new StreamContent(reqStream));
            }

            if (res.Content.Headers.ContentType.MediaType == MediaTypeNames.Text.Xml)
            {
                using (var resStream = await res.Content.ReadAsStreamAsync())
                {
                    var resEnv = await XDocument.LoadAsync(resStream, default, default);

                    if (res.IsSuccessStatusCode)
                        return resEnv.Root.Element(Env + "Body").Elements().FirstOrDefault();

                    throw new WwsException(resEnv);
                }
            }

            throw new InvalidOperationException("Unexpected WWS Response Content-Type: "
                + res.Content.Headers.ContentType);
        }
    }
}