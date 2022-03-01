using System;

namespace ServiceComposer.OpenApi.Attributes;

/// <summary>
/// A filter that specifies the type and property name of the composition value returned by this handler.
/// </summary>
/// <remarks>
/// The composition properties identified here are used to combine into a single model returned by the
/// HttpGet handler.  The model is only used for documentation purposes.
/// </remarks>
/// <remarks>
/// The s
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ProducesCompositionResponseTypeAttribute : Attribute
{
    private readonly string _compositionPropertyName;
    private readonly Type _type;

    /// <summary>
    /// Initializes an instance of <see cref="ProducesCompositionResponseTypeAttribute"/>.
    /// </summary>
    /// <param name="compositionPropertyName">The name of the property to give to the merged viewmodel.</param>
    /// <param name="type">The <see cref="Type"/> of object that is going to be written in the response.</param>
    public ProducesCompositionResponseTypeAttribute(string compositionPropertyName, Type type)
    {
        _compositionPropertyName = compositionPropertyName;
        _type = type;
    }
}