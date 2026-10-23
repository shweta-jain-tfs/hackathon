using System.Globalization;
using Pharos.Client.App.ServiceTools.Services;
using Pharos.Common.Web.Components.FormElements;
using Pharos.Contracts.Parameters;

namespace Pharos.Client.App.ServiceTools.ViewModels.RequestGenerator;

public sealed class EditTestViewModel
{
    private RequestGeneratorTest _originalTest = default!;

    public RequestGeneratorTest EditableTest { get; private set; } = default!;

    public List<DropDownOption<int>> InstrumentDilutionValues { get; private set; } = [];

    public static int PreDilutionFactor => 1;

    public int TotalDilution => PreDilutionFactor * EditableTest.InstrumentDilutionFactor;

    public bool HasChanges =>
        EditableTest.Replicates != _originalTest.Replicates ||
        EditableTest.InstrumentDilutionFactor != _originalTest.InstrumentDilutionFactor ||
        EditableTest.IsActive != _originalTest.IsActive;

    public Task InitializeAsync(RequestGeneratorTest test)
    {
        _originalTest = Clone(test);
        EditableTest = Clone(test);
        SetInstrumentDilutionOptions(EditableTest.AssayTechnology);
        return Task.CompletedTask;
    }

    private void SetInstrumentDilutionOptions(AssayTechnology technology)
    {
        var dilutions = technology switch
        {
            AssayTechnology.ImmunoCap => InstrumentDilutionFactors.ImmunoCap,
            AssayTechnology.ImmunoCapIgG => InstrumentDilutionFactors.ImmunoCapIgG,
            AssayTechnology.EliA => InstrumentDilutionFactors.Elia,
            AssayTechnology.ViewAllergy => InstrumentDilutionFactors.ViewAllergy,
            _ => InstrumentDilutionFactors.ImmunoCap
        };

        InstrumentDilutionValues = dilutions
            .Select(x => new DropDownOption<int>
            {
                Value = x,
                Text = x.ToString(CultureInfo.InvariantCulture)
            })
            .ToList();
    }

    private static RequestGeneratorTest Clone(RequestGeneratorTest test)
    {
        return new RequestGeneratorTest
        {
            AssayTechnology = test.AssayTechnology,
            MethodName = test.MethodName,
            TestName = test.TestName,
            Replicates = test.Replicates,
            InstrumentDilutionFactor = test.InstrumentDilutionFactor,
            IsActive = test.IsActive
        };
    }
}
