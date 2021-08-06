using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using WorkSharp.Wws.Internal;
using Xml.Schema.Linq;

using static WorkSharp.Wws.Internal.XHelper;

namespace WorkSharp.Wws
{
    [DebuggerStepThrough]
    public static class WwsReference
    {
        public static T Create<T>(string type, string value) where T : XTypedElement
        {
            var ns = WwsDefaults.Namespace;
            var xml = El("ref",
                El(ns + "ID",
                    Attr(ns + "type", type),
                    value
                )
            );

            return xml.ConvertTo<T>();
        }

        public static T Create<T>(string parentType, string parentValue, string type, string value) where T : XTypedElement
        {
            var ns = WwsDefaults.Namespace;
            var xml = El("ref",
                El(ns + "ID",
                    Attr(ns + "parent_type", parentType),
                    Attr(ns + "parent_id", parentValue),
                    Attr(ns + "type", type),
                    value
                )
            );

            return xml.ConvertTo<T>();
        }
    }

    public static class WwsReferenceExtensions
    {
        public static string IdOfType(this XTypedElement el, string idType)
        {
            var id = el.IdOfTypeOrNull(idType);

            if (!string.IsNullOrEmpty(id))
                return id;

            var ns = WwsDefaults.Namespace;
            var idTypes = String.Join(", ", ((XElement)el)
                .Elements(ns + "ID")
                .Select(i => i.Attr(ns + "type")));

            var msg = $"No integration ID of type {idType} could be found!";
            if (!String.IsNullOrWhiteSpace(idTypes))
                msg += $" Available integration ID types: {idTypes}.";

            throw new InvalidOperationException(msg);
        }

        public static string? IdOfTypeOrNull(this XTypedElement el, string idType)
        {
            var ns = WwsDefaults.Namespace;
            return ((XElement)el)
                .Elements(ns + "ID")
                .SingleOrDefault(e => e.Attr(ns + "type") == idType)
                ?.Value;
        }
    }
}