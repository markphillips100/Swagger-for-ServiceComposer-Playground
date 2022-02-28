using System;

#nullable enable

namespace ServiceComposerAttributes;

/// <summary>
/// A filter that specifies the type of the value and status code returned by the action.
/// </summary>
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
    /// <param name="statusCode">The HTTP response status code.</param>
    public ProducesCompositionResponseTypeAttribute(string compositionPropertyName, Type type)
    {
        _compositionPropertyName = compositionPropertyName;
        _type = type;
    }
}