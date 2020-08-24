using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Swagger_for_ServiceComposer.ApiDescription
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class ApiParameterDescriptionAttribute : Attribute
    {
        public string Name { get; set; }
        public bool IsRequired { get; set; }
        public Type Type { get; set; }
        public string Source { get; set; }
    }
}