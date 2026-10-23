using Pharos.Server.Domain.Enums;
using Pharos.Server.Domain.Parameters.Methods;
using Riok.Mapperly.Abstractions;
using Tetris.Protocol.Instrument.Methods;
using Method = Pharos.Server.Domain.Parameters.Methods.Method;

namespace Pharos.Server.Instrument.Application.Mapping;

[Mapper]
internal static partial class MethodMapper
{
    [MapProperty(nameof(@Method.CalibrationCurve.CalibrationPoints), nameof(Tetris.Protocol.Instrument.Methods.Method.CalibratorPoints))]
    [MapProperty(nameof(@Method.CalibrationCurve.MethodTest.TestName), nameof(Tetris.Protocol.Instrument.Methods.Method.CalibratorTestName))]
    [MapProperty(nameof(Method.CurveControls), nameof(Tetris.Protocol.Instrument.Methods.Method.CurveControlPoints))]
    [MapProperty(nameof(@Method.AnalyticalRun.MaxAnalyticalRunSize), nameof(Tetris.Protocol.Instrument.Methods.Method.MaxAnalyticalRunSize))]
    [MapProperty(nameof(@Method.AnalyticalRun.RunCalibrationOnCarrierLotChange), nameof(Tetris.Protocol.Instrument.Methods.Method.RunCalibrationOnCarrierLotChange))]
    [MapperIgnoreTarget(nameof(Tetris.Protocol.Instrument.Methods.Method.ConjugateName))]
    [MapperIgnoreTarget(nameof(Tetris.Protocol.Instrument.Methods.Method.WashConcentrateName))]
    [MapperIgnoreTarget(nameof(Tetris.Protocol.Instrument.Methods.Method.WashAdditiveName))]
    public static partial Tetris.Protocol.Instrument.Methods.Method ToInstrumentMethod(this Method source);

    [MapProperty(nameof(MethodCurveControl.CurveControlName), nameof(CalCcPoint.PointName))]
    [MapProperty(nameof(MethodCurveControl.Replicates), nameof(CalCcPoint.NumberOfReplicates))]
    private static partial CalCcPoint ToCalCcPoint(MethodCurveControl source);

    [MapProperty(nameof(MethodCalibrationPoint.CalibratorName), nameof(CalCcPoint.PointName))]
    [MapProperty(nameof(MethodCalibrationPoint.Replicates), nameof(CalCcPoint.NumberOfReplicates))]
    private static partial CalCcPoint ToCalCcPoint(MethodCalibrationPoint source);

    [MapProperty(nameof(MethodTest.DiluentIdentity), nameof(Test.DiluentName))]
    [MapProperty(nameof(MethodTest.DevelopmentSolutionIdentity), nameof(Test.DevelopmentName))]
    private static partial Test ToTest(MethodTest source);

    [MapperIgnoreTargetValue(EnumAssayTechnology.UnspecifiedAssayTechnology)]
    [MapEnumValue(AssayTechnology.ViewAllergy, EnumAssayTechnology.ImmunoCapViewAllergy)]
    private static partial EnumAssayTechnology MapAssayTechnology(AssayTechnology source);

    [MapperIgnoreTargetValue(EnumCalibratorOrder.UnspecifiedCalibratorOrder)]
    private static partial EnumCalibratorOrder MapCalibratorOrder(CalibratorOrder source);
}
