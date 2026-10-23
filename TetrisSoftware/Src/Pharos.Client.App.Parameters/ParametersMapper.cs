using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.Parameters;

[Mapper]
public static partial class ParametersMapper
{
    public static partial CreateReflexDefinitionCommand ToCreateReflexDefinitionCommand(this ReflexDefinition source);

    [MapProperty(nameof(ReflexDefinition.QueriedAt), nameof(UpdateReflexDefinitionCommand.ReflexDefinitionQueriedAt))]
    public static partial UpdateReflexDefinitionCommand ToUpdateReflexDefinitionCommand(this ReflexDefinition source);

    public static partial CreateRequestorCommand ToCreateRequestorCommand(this RequestorDetails source);

    public static partial UpdateRequestorCommand ToUpdateRequestorCommand(this RequestorDetails source);

    [MapProperty($"{nameof(MethodDetails.CalibratorCurve)}.{nameof(MethodCalibratorCurve.MaxNumberOfDays)}", nameof(UpdateMethodCommand.MaxNumberOfDays))]
    public static partial UpdateMethodCommand ToUpdateMethodCommand(this MethodDetails source);

    public static partial CreatePanelCommand ToCreateMethodPanelCommand(this MethodPanelDetails source);

    public static partial UpdatePanelCommand ToUpdateMethodPanelCommand(this MethodPanelDetails source);

    public static partial UpdateMethodTestDetailsCommand ToUpdateMethodTestDetailsCommand(this MethodTestDetails source);

    private static int? ToNullableInt(int source)
    {
        return source == 0 ? null : source;
    }
}
