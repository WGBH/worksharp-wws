// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;
using System.Xml.Linq;

namespace WorkSharp.Wws.Builder
{
    [DebuggerStepThrough]
    static class XHelper
    {
        public static XNamespace Ns(string xmlNamespace) =>
            (XNamespace) xmlNamespace;

        public static XElement El(XName name) =>
            new XElement(name);

        public static XElement El(XName name, object content) =>
            new XElement(name, content);

        public static XElement El(XName name, params object[] content) =>
            new XElement(name, content);

        public static XElement El(XElement other) =>
            new XElement(other);

        public static XElement El(XStreamingElement other) =>
            new XElement(other);

        public static XAttribute Attr(XName name, object value) =>
            new XAttribute(name, value);

        public static XAttribute Attr(XAttribute other) =>
            new XAttribute(other);

        public static XElement El(this XContainer container, XName name) =>
            container.Element(name);

        public static string Attr(this XElement element, XName name) =>
            element.Attribute(name).Value;

        public static string TrimNs(this string Name) =>
            Name.Substring(Name.IndexOf(':') + 1);
    }
}