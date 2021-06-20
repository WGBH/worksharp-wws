using System.Diagnostics;
using Workday.Wws.Internal;
using Xml.Schema.Linq;

using static Workday.Wws.Internal.XHelper;

namespace Workday.Wws
{
    [DebuggerStepThrough]
    public static class WwsReference<T> where T : XTypedElement
    {
        public static T From(string type, string value)
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

        public static T From(string parentType, string parentValue, string type, string value)
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
}