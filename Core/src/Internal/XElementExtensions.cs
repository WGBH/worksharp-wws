// Copyright 2021 WGBH Educational Foundation
// Licensed under the Apache License, Version 2.0

using System;
using System.Xml.Linq;
using Xml.Schema.Linq;

namespace WorkSharp.Wws.Internal
{
    static class XElementExtensions
    {
        public static T ConvertTo<T>(this XElement xml) where T : XTypedElement
        {
            var type = typeof(T);

            var convert = type.GetMethod("op_Explicit", new[] { typeof(XElement) });
            if (convert != null && convert.ReturnType == type)
                return (T) convert.Invoke(xml, new [] {xml});

            throw new MissingMethodException("No suitable cast operator method exists "
                + $"to convert from {typeof(XElement)} to {type}.");
        }

        public static XElement El(this XContainer container, XName name) =>
            container.Element(name);

        public static string Attr(this XElement element, XName name) =>
            element.Attribute(name).Value;
    }
}