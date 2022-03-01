using System;

namespace ServiceComposer.OpenApi.Attributes;


[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class ApiParameterDescriptionAttribute : Attribute
{
    public string Name { get; set; }
    public bool IsRequired { get; set; }
    public Type Type { get; set; }
    public string Source { get; set; }
}
