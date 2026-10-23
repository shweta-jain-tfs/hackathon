using System.Collections;
using Microsoft.AspNetCore.Components.Forms;

namespace Pharos.Client.Common.BlazorFluentValidation;

internal static class PropertyPathHelper
{
    private class Node
    {
        public Node? Parent { get; set; }
        public object? ModelObject { get; set; }
        public string? PropertyName { get; set; }
        public int? Index { get; set; }
    }

    public static string ToFluentPropertyPath(EditContext editContext, FieldIdentifier fieldIdentifier)
    {
        var nodes = new Stack<Node>();
        nodes.Push(new Node()
        {
            ModelObject = editContext.Model,
        });

        while (nodes.Count != 0)
        {
            var currentNode = nodes.Pop();
            var currentModelObject = currentNode.ModelObject;

            if (currentModelObject == fieldIdentifier.Model)
            {
                return BuildPropertyPath(currentNode, fieldIdentifier);
            }

            var nonPrimitiveProperties = currentModelObject?.GetType()
                .GetProperties()
                .Where(prop => IsTraversableType(prop.PropertyType)) ?? [];

            foreach (var nonPrimitiveProperty in nonPrimitiveProperties)
            {
                var instance = nonPrimitiveProperty.GetValue(currentModelObject);

                if (instance == fieldIdentifier.Model)
                {
                    var node = new Node()
                    {
                        Parent = currentNode,
                        PropertyName = nonPrimitiveProperty.Name,
                        ModelObject = instance
                    };

                    return BuildPropertyPath(node, fieldIdentifier);
                }

                if (TryGetEnumerable(instance, out var enumerable))
                {
                    var itemIndex = 0;
                    foreach (var item in enumerable)
                    {
                        nodes.Push(new Node()
                        {
                            ModelObject = item,
                            Parent = currentNode,
                            PropertyName = nonPrimitiveProperty.Name,
                            Index = itemIndex++
                        });
                    }
                }
                else if (instance is not null)
                {
                    nodes.Push(new Node()
                    {
                        ModelObject = instance,
                        Parent = currentNode,
                        PropertyName = nonPrimitiveProperty.Name
                    });
                }
            }
        }

        return string.Empty;
    }

    private static string BuildPropertyPath(Node currentNode, FieldIdentifier fieldIdentifier)
    {
        var pathParts = new List<string>();
        pathParts.Add(fieldIdentifier.FieldName);
        var next = currentNode;

        while (next is not null)
        {
            if (!string.IsNullOrEmpty(next.PropertyName))
            {
                if (next.Index is not null)
                {
                    pathParts.Add($"{next.PropertyName}[{next.Index}]");
                }
                else
                {
                    pathParts.Add(next.PropertyName);
                }
            }

            next = next.Parent;
        }

        pathParts.Reverse();

        return string.Join('.', pathParts);
    }

    /// <summary>
    /// Returns whether a type should be traversed while building a property path.
    /// This excludes terminal value types and <see cref="string"/> to prevent the
    /// infinite-loop issue introduced in Blazored.FluentValidation 2.2.1 after the
    /// GitHub implementation was copied locally.
    /// </summary>
    private static bool IsTraversableType(Type type)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type == typeof(string))
        {
            return false;
        }

        return !(type.IsPrimitive
                 || type.IsEnum
                 || type == typeof(decimal)
                 || type == typeof(DateTime)
                 || type == typeof(DateTimeOffset)
                 || type == typeof(TimeSpan)
                 || type == typeof(Guid)
                 || type == typeof(DateOnly)
                 || type == typeof(TimeOnly));
    }

    /// <summary>
    /// Returns <see langword="true"/> when the instance can be safely iterated as a collection.
    /// Strings are excluded because they implement <see cref="IEnumerable"/> and would otherwise
    /// be traversed character-by-character. This guard was added after the Blazored.FluentValidation
    /// 2.2.1 issue.
    /// </summary>
    private static bool TryGetEnumerable(object? instance, out IEnumerable enumerable)
    {
        if (instance is IEnumerable value and not string)
        {
            enumerable = value;
            return true;
        }

        enumerable = Array.Empty<object>();
        return false;
    }
}