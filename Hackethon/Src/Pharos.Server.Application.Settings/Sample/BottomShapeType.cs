using Pharos.Server.Application.Settings.Attributes;

namespace Pharos.Server.Application.Settings.Sample;

/// <summary>
/// Lists available types of the different bottom shapes. 
/// </summary>
/// <remarks>
/// Do not modify numbers for the existing enum entries (they are persisted).
/// </remarks>
public enum BottomShapeType
{
    [DisplayNameLocal("BottomShapeType_VShaped")]
    VShaped = 0,

    [DisplayNameLocal("BottomShapeType_UShaped")]
    UShaped = 1,

    [DisplayNameLocal("BottomShapeType_Flat")]
    Flat = 2
}