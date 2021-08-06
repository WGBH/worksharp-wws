// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System.Diagnostics;
using System.Xml.Linq;

namespace WorkSharp.Wws.Internal
{
    [DebuggerStepThrough]
    static class XHelper
    {
        public static XNamespace Ns(string xmlNamespace) =>
            (XNamespace)xmlNamespace;

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
    }
}