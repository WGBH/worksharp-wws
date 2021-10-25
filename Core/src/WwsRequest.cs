using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WorkSharp.Wws.Internal;
using Xml.Schema.Linq;

using static WorkSharp.Wws.Internal.XHelper;

namespace WorkSharp.Wws
{
    public static class WwsRequest
    {
        public const int MaxItems = 999;

        public static TRequest Create<TRequest>(
            string referenceIdType, params string[] referenceIdValues) where TRequest : XTypedElement
        {
            return Create<TRequest>(1, 100, referenceIdType, referenceIdValues);
        }

        public static TRequest Create<TRequest>(
            string referenceIdType, IEnumerable<string> referenceIdValues) where TRequest : XTypedElement
        {
            return Create<TRequest>(1, 100, referenceIdType, referenceIdValues);
        }

        public static TRequest Create<TRequest>(int page = 1, int count = 100,
            string? referenceIdType = null, IEnumerable<string>? referenceIdValues = null) where TRequest : XTypedElement
        {
            var wd = WwsDefaults.Namespace;
            var type = typeof(TRequest);

            var request = El(wd + typeof(TRequest).Name,
                El(wd + "Response_Filter",
                    El(wd + "Page", page),
                    El(wd + "Count", count)
                )
            );

            if (referenceIdType != null && (referenceIdValues?.Any() ?? false))
            {
                string? reqRefsListElementName = null;

                if (type.GetProperty("Request_References") is PropertyInfo reqRefsProp &&
                    reqRefsProp.PropertyType.GetProperties()
                        .SingleOrDefault(p => p.PropertyType.GetInterface(typeof(IEnumerable<>).Name) != null)
                    is PropertyInfo propertyInfo)
                {
                    reqRefsListElementName = propertyInfo.Name;
                }

                if (reqRefsListElementName == null)
                {
                    throw new InvalidOperationException($"Cannot add request references "
                        + $"to a WWS Request of type {type}.");
                }

                request.Add(
                    El(wd + "Request_References",
                        referenceIdValues.Select(r => El(wd + reqRefsListElementName,
                            El(wd + "ID",
                                Attr(wd + "type", referenceIdType),
                                r
                            )
                        ))
                    )
                );
            }

            return request.ConvertTo<TRequest>();
        }
    }
}