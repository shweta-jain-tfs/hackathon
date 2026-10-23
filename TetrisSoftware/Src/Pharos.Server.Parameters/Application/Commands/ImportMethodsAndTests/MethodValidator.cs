using FluentValidation;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Parameters.Methods;

namespace Pharos.Server.Parameters.Application.Commands.ImportMethodsAndTests;

internal class MethodValidator : AbstractValidator<Method>
{
    public MethodValidator()
    {
        RuleFor(x => x.MethodName).NotEmpty().MaximumLength(Constants.MethodNameLength);
        RuleFor(x => x.MethodNameLis).MaximumLength(Method.LisMethodNameLength);
        RuleFor(x => x.FullMethodName).MaximumLength(Method.FullMethodNameLength);
        RuleFor(x => x.MethodVersion).GreaterThan(0);
        RuleFor(x => x.Technology).IsInEnum();
        RuleFor(x => x.CalibratorPiercingOrder).IsInEnum();
        RuleFor(x => x.CalibratorPipettingOrder).IsInEnum();

        RuleFor(x => x.AnalyticalRun).NotEmpty().SetValidator(new MethodAnalyticalRunValidator());

        RuleFor(x => x.Calculation).NotEmpty().SetValidator(new MethodCalculationValidator());

        RuleFor(x => x.Presentation).NotEmpty().SetValidator(new MethodPresentationValidator());

        RuleFor(x => x.Reporting).NotEmpty().SetValidator(new MethodReportingValidator());

        RuleFor(x => x.Reagents).NotEmpty();
        RuleForEach(x => x.Reagents).SetValidator(new MethodReagentValidator());

        RuleFor(x => x.CalibrationCurve).NotEmpty().SetValidator(new MethodCalibrationCurveValidator());

        RuleFor(x => x.CurveControls).NotEmpty();
        RuleForEach(x => x.CurveControls).SetValidator(new MethodCurveControlsValidator());

        RuleFor(x => x.ConcentrationUnits).NotEmpty();
        RuleForEach(x => x.ConcentrationUnits).SetValidator(new MethodConcentrationUnitsValidator());

        RuleFor(x => x.PhadiaQualityControlTestsForParser).NotEmpty();
        RuleForEach(x => x.PhadiaQualityControlTestsForParser).SetValidator(new PhadiaQualityControlTestValidator());

        RuleForEach(x => x.ClassTableBelows).SetValidator(new MethodClassTableBelowsValidator());

        RuleForEach(x => x.ClassTables).SetValidator(new MethodClassTablesValidator());

    }
}

internal class MethodAnalyticalRunValidator : AbstractValidator<MethodAnalyticalRun>
{
    public MethodAnalyticalRunValidator()
    {
        RuleFor(x => x.DefaultMaxAnalyticalRunSize).GreaterThan(0);
    }
}

internal class MethodCalculationValidator : AbstractValidator<MethodCalculation>
{
    public MethodCalculationValidator()
    {
        RuleFor(x => x.CalculationType).IsInEnum();
        RuleFor(x => x.RobustWeight).IsInEnum();
        RuleFor(x => x.NormalWeight).IsInEnum();
        RuleFor(x => x.PredictedVarianceA).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PredictedVarianceB).GreaterThanOrEqualTo(0);
        RuleFor(x => x.MinCoefficientOfCorrelation).InclusiveBetween(0, 1);
        RuleFor(x => x.CalibratorStartingPoint).InclusiveBetween(0, 5);
        RuleFor(x => x.CalibratorsForLeastSquareCalc).InclusiveBetween(2, 6);
    }
}

internal class MethodPresentationValidator : AbstractValidator<MethodPresentation>
{
    public MethodPresentationValidator()
    {
        RuleFor(x => x.XAxisText).MaximumLength(MethodPresentation.AxisTextMaxLength);
        RuleFor(x => x.XAxisUnit).MaximumLength(MethodPresentation.AxisUnitTextMaxLength);
        RuleFor(x => x.YAxisText).MaximumLength(MethodPresentation.AxisTextMaxLength);
        RuleFor(x => x.YAxisUnit).MaximumLength(MethodPresentation.AxisUnitTextMaxLength);
        RuleFor(x => x.FixedYAxisLowerLimit).InclusiveBetween(0, 100000);
        RuleFor(x => x.FixedYAxisUpperLimit).InclusiveBetween(0, 100000);
    }
}

internal class MethodReportingValidator : AbstractValidator<MethodReporting>
{
    public MethodReportingValidator()
    {
        RuleFor(x => x.DefaultTextBelowCutOff).MaximumLength(MethodReporting.ClassResultTextLength);
        RuleFor(x => x.DefaultTextAboveCutOff).MaximumLength(MethodReporting.ClassResultTextLength);
        RuleFor(x => x.DefaultTextQuantitativeBelow).MaximumLength(MethodReporting.QuantitativeResultTextLength);
        RuleFor(x => x.DefaultTextQuantitativeAbove).MaximumLength(MethodReporting.QuantitativeResultTextLength);
        RuleFor(x => x.DefaultTextQuantitativeDoubtful).MaximumLength(MethodReporting.QuantitativeResultTextLength);
        RuleFor(x => x.DefaultMinConcentrationToReport).InclusiveBetween(0, 99999);

    }
}

internal class MethodReagentValidator : AbstractValidator<MethodReagent>
{
    public MethodReagentValidator()
    {
        RuleFor(x => x.Volume).InclusiveBetween(0, 9999);
    }
}

internal class MethodCalibrationCurveValidator : AbstractValidator<MethodCalibrationCurve>
{
    public MethodCalibrationCurveValidator()
    {
        RuleFor(x => x.DefaultMaxNumberOfDays).GreaterThan(0);
        RuleFor(x => x.MaxBadReplicates).InclusiveBetween(0, 75);

        RuleFor(x => x.CalibrationPoints).NotEmpty();
        RuleFor(x => x.QuotientIndex).Must((curve, index) => index > 0 && index <= (curve.CalibrationPoints?.Count ?? 0));
        RuleForEach(x => x.CalibrationPoints).SetValidator(new MethodCalibratorPointValidator());

    }
}

internal class MethodCalibratorPointValidator : AbstractValidator<MethodCalibrationPoint>
{
    public MethodCalibratorPointValidator()
    {
        RuleFor(x => x.CalibratorName).MaximumLength(MethodCalibrationPoint.CalibratorNameLength);
        RuleFor(x => x.ExpectedConcentration).InclusiveBetween(0, 99999);
        RuleFor(x => x.Replicates).InclusiveBetween(1, 5);
        RuleFor(x => x.IdealResponse).InclusiveBetween(0, 99999);
        RuleFor(x => x.ExpectedStdDev).InclusiveBetween(0, 99999);
        RuleFor(x => x.UpperResponseLimit).InclusiveBetween(0, 99999);
        RuleFor(x => x.LowerResponseLimit).InclusiveBetween(0, 99999);
        RuleFor(x => x.StdDevPrecisionLimit).InclusiveBetween(0, 999);
        RuleFor(x => x.StdDevLimit).InclusiveBetween(0, 999);
        RuleFor(x => x.UpperLogarithmicLimit).InclusiveBetween(-999, 999);
        RuleFor(x => x.LowerLogarithmicLimit).InclusiveBetween(-999, 999);
        RuleFor(x => x.AdjustmentForAlfaBeta1Beta2).InclusiveBetween(0, 99999);
    }
}

internal class MethodCurveControlsValidator : AbstractValidator<MethodCurveControl>
{
    public MethodCurveControlsValidator()
    {
        RuleFor(x => x.CurveControlName).MaximumLength(MethodCurveControl.CurveControlNameLength);
        RuleFor(x => x.Replicates).InclusiveBetween(1, 5);
        RuleFor(x => x.ExpectedConcentration).InclusiveBetween(0, 99999);
        RuleFor(x => x.ExpectedStdDev).InclusiveBetween(0, 99999);
        RuleFor(x => x.InnerStdDevLimit).InclusiveBetween(0, 999);
        RuleFor(x => x.OuterStdDevLimit).InclusiveBetween(0, 999);
        RuleFor(x => x.AdjustmentForAlfaBeta1Beta2).InclusiveBetween(0, 999);
    }
}

internal class MethodConcentrationUnitsValidator : AbstractValidator<MethodConcentrationUnit>
{
    public MethodConcentrationUnitsValidator()
    {
        RuleFor(x => x.ConcentrationUnit).MaximumLength(MethodConcentrationUnit.ConcentrationUnitMaxLength);
    }
}

internal class PhadiaQualityControlTestValidator : AbstractValidator<PhadiaQualityControlTestForParser>
{
    private static readonly List<int> _dilutionFactors = [1, 2, 5, 10, 20, 50, 100, 200, 1000];

    public PhadiaQualityControlTestValidator()
    {
        RuleFor(x => x.QualityControlName).MaximumLength(Constants.QualityControlNameLength);
        RuleFor(x => x.QualityControlType).IsInEnum();
        RuleFor(x => x.PreDilutionFactor).InclusiveBetween(1, 1000);

        //For the template file the instrument dilution is 0
        RuleFor(x => x.TestName).NotEmpty();
        When(x => x.TestName.Equals("tmpl", StringComparison.Ordinal), () => RuleFor(x => x.InstrumentDilutionFactor).Empty());
        When(x => x.TestName != "tmpl", () => RuleFor(x => x.InstrumentDilutionFactor).NotEmpty().Must(x => _dilutionFactors.Contains(x!.Value)));
    }
}

internal class MethodClassTableBelowsValidator : AbstractValidator<MethodClassTableBelow>
{
    public MethodClassTableBelowsValidator()
    {
        RuleFor(x => x.TextBelowConcentration).MaximumLength(8);
    }
}

internal class MethodClassTablesValidator : AbstractValidator<MethodClassTable>
{
    public MethodClassTablesValidator()
    {
        RuleFor(x => x.Concentration).InclusiveBetween(0, 99999);
        RuleFor(x => x.TextAboveConcentration).MaximumLength(8);
    }
}
