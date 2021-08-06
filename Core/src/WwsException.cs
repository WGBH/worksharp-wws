using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace WorkSharp.Wws
{
    public class WwsException : Exception
    {
        public class ValidationError
        {
            public String Message { get; }
            public String DetailMessage { get; }
            public String XPathExpression { get; }

            internal ValidationError(string message, string detailMessage, string xPathExpression)
            {
                Message = message;
                DetailMessage = detailMessage;
                XPathExpression = xPathExpression;
            }
        }
        public override string Message { get; }
        public string?  FaultCode { get; }
        public IReadOnlyList<ValidationError> ValidationErrors { get; }

        internal WwsException(XDocument WwsResponse)
        {
            var wd = WwsDefaults.Namespace;

            Message = WwsResponse.Descendants("faultstring").FirstOrDefault()?.Value
                ?? "A Workday Web Services exception occurred";
            FaultCode = WwsResponse.Descendants("faultcode").FirstOrDefault()?.Value;

            var validationErrorsXml = WwsResponse.Descendants(wd + "Validation_Error");
            var validationErrors = new List<ValidationError>();

            foreach(var error in validationErrorsXml)
            {
                var message = error.Element(wd + "Message")?.Value;
                var detailMessage = error.Element(wd + "Detail_Message")?.Value;
                var xPathExpression = error.Element(wd + "Xpath")?.Value;

                if (message != null && detailMessage != null && xPathExpression != null)
                    validationErrors.Add(new ValidationError(message, detailMessage, xPathExpression));
             }

            ValidationErrors = validationErrors.AsReadOnly();
        }
    }
}