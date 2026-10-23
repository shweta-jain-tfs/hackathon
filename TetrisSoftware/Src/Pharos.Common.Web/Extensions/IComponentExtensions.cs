using Microsoft.AspNetCore.Components.Forms;

namespace Pharos.Common.Web.Extensions;

public static class IComponentExtensions
{
    public static string CreateAutomatedTestId(this IComponent component, FieldIdentifier identifier)
    {
        var componentType = component.GetType();
        var typeName = StripGenericFromTypeName(componentType.Name);
        var modelName = identifier.Model.GetType().Name;
        var fieldName = identifier.FieldName;

        var id = $"{typeName}_{modelName}.{fieldName}";

        return id;
    }

    public static string CreateAutomatedTestId(this IComponent component, string name)
    {
        var typeName = StripGenericFromTypeName(component.GetType().Name);
        return $"{typeName}_{name}";
    }

    private static string StripGenericFromTypeName(string name)
    {
        return name.Split('`').First();
    }
}