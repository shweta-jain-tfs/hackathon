using Pharos.Server.Application.Instrument;
using Pharos.Server.Application.Lis;
using Pharos.Server.Application.Parameters;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Result.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Result.Application.Mapping;

[Mapper]
internal partial class LisTestResultMapper(IInstrumentManager instrumentManager, IMethodsService methodsService)
{
    public async Task<List<LisSampleTestResult>> ToLisTestResults(List<SampleTestResult> source)
    {
        var target = new List<LisSampleTestResult>(source.Count);

        source.ForEach(x =>
        {
            var lisResult = ToLisSampleTestResultPartial(x);
            lisResult.Pending = x.Status is not TestResultStatus.Aborted &&
                                x.ApprovalStatus is not ResultApprovalStatus.Rejected &&
                                x.ApprovalStatus is not ResultApprovalStatus.Approved;
            lisResult.HasError = x.Status is TestResultStatus.Aborted || x.ApprovalStatus is ResultApprovalStatus.Rejected;
            lisResult.ApprovedByUser = x.LastModifiedBy?.ToString();
            target.Add(lisResult);
        });

        await MapDynamicFields(source, target);
        return target;
    }

    public async Task<List<LisQualityControlTestResult>> ToLisTestResults(List<QualityControlTestResult> source)
    {
        var target = new List<LisQualityControlTestResult>(source.Count);

        source.ForEach(x => target.Add(ToLisQualityControlTestResultPartial(x)));
        await MapDynamicFields(source, target);
        return target;
    }

    private async Task MapDynamicFields<TTestResult, TLisTestResult>(List<TTestResult> source, List<TLisTestResult> target)
        where TTestResult : TestResult where TLisTestResult : LisTestResult
    {
        var methodTestPairs = source.Select(x => (x.MethodName, x.TestName)).Distinct().ToArray();
        var unitsByMethodTest = await methodsService.GetMethodTestConcentrationUnits(methodTestPairs);

        for (var i = 0; i < source.Count; i++)
        {
            target[i].InstrumentName = instrumentManager.FindInstrument(source[i].InstrumentNumber)?.DisplayName ?? source[i].InstrumentNumber;

            if (unitsByMethodTest.TryGetValue((source[i].MethodName, source[i].TestName), out var unit))
            {
                target[i].Units = unit;
            }
            if (source[i].Replicates is { Count: > 0 })
            {
                target[i].Reagents = source[i].Replicates!
                    .Where(r => r.TestReplicateReagents != null)
                    .SelectMany(r => r.TestReplicateReagents!)
                    .DistinctBy(r => new { r.ReagentType, r.LotNumber })
                    .Select(ToLisReagent)
                    .ToList();
            }
        }
    }

    [IncludeMappingConfiguration(nameof(ToBaseLisTestResult))]
    [MapProperty(nameof(SampleTestResult.MeanClass), nameof(LisSampleTestResult.Class))]
    [MapProperty(nameof(SampleTestResult.MeanCutOff), nameof(LisSampleTestResult.CutOff))]
    [MapProperty(nameof(SampleTestResult.MeanQuotientAsText), nameof(LisSampleTestResult.Quotient))]
    [MapperIgnoreTarget(nameof(LisSampleTestResult.Pending))]
    [MapperIgnoreTarget(nameof(LisSampleTestResult.HasError))]
    [MapperIgnoreTarget(nameof(LisSampleTestResult.ApprovedByUser))]
    private partial LisSampleTestResult ToLisSampleTestResultPartial(SampleTestResult source);


    [IncludeMappingConfiguration(nameof(ToBaseLisTestResult))]
    private partial LisQualityControlTestResult ToLisQualityControlTestResultPartial(QualityControlTestResult source);


    [MapProperty(nameof(TestResult.MeanConcentrationAsText), nameof(LisTestResult.Concentration))]
    [MapProperty(nameof(TestResult.MeanResponseAsText), nameof(LisTestResult.Ru))]
    [MapperIgnoreTarget(nameof(LisTestResult.InstrumentName))]
    [MapperIgnoreTarget(nameof(LisTestResult.Reagents))]
    [MapperIgnoreTarget(nameof(LisTestResult.Units))]
    private partial LisTestResult ToBaseLisTestResult(TestResult source);

    private partial LisReagent ToLisReagent(TestReplicateReagent source);
}
