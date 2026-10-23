using Pharos.Server.Application.Lis;
using Pharos.Server.Request.Domain;
using Riok.Mapperly.Abstractions;

namespace Pharos.Server.Request.Application.Mapping;

[Mapper]
internal static partial class LisMapper
{
    [MapperIgnoreTarget(nameof(LisSample.TubeType))]
    [MapperIgnoreTarget(nameof(LisSample.TestOrders))]
    [MapProperty(nameof(Domain.Request.Source), nameof(LisSample.IsSampleLocallyCreated), Use = nameof(MapIsSampleLocallyCreated))]
    public static partial LisSample ToLisSample(Domain.Request source);


    [MapProperty(nameof(TestOrder.LabTestName), nameof(LisTestOrder.TestName))]
    [MapProperty(nameof(TestOrder.Uid), nameof(LisTestOrder.TestOrderId))]
    [MapperIgnoreTarget(nameof(LisTestOrder.FullTestName))]
    [MapperIgnoreTarget(nameof(LisTestOrder.Action))]
    [MapperIgnoreTarget(nameof(LisTestOrder.TestResult))]
    [MapperIgnoreTarget(nameof(LisTestOrder.ReportConcentration))]
    [MapperIgnoreTarget(nameof(LisTestOrder.ReportClass))]
    [MapperIgnoreTarget(nameof(LisTestOrder.ReportCutOff))]
    [MapperIgnoreTarget(nameof(LisTestOrder.ReportQuotient))]
    [MapperIgnoreTarget(nameof(LisTestOrder.PreDilutionFactor))]
    [MapperIgnoreTarget(nameof(LisTestOrder.PanelExpansionEnabled))]
    [MapperIgnoreTarget(nameof(LisTestOrder.AutoPanelRecognitionFromOneMessage))]
    [MapperIgnoreTarget(nameof(LisTestOrder.AutoPanelRecognitionFromMultipleMessage))]
    [MapperIgnoreTarget(nameof(LisTestOrder.ForceUniqueLaboratoryTestNames))]
    [MapperIgnoreTarget(nameof(LisTestOrder.UseRequestPriorityA))]
    [MapperIgnoreTarget(nameof(LisTestOrder.UseRequestPriorityB))]
    [MapperIgnoreTarget(nameof(LisTestOrder.PanelNames))]
    [MapperIgnoreTarget(nameof(LisTestOrder.ReflexName))]
    [MapperIgnoreTarget(nameof(LisTestOrder.SuppressReflex))]
    [MapperIgnoreTarget(nameof(LisTestOrder.TrayId))]
    [MapperIgnoreTarget(nameof(LisTestOrder.TubePosition))]
    public static partial LisTestOrder ToLisTestOrder(TestOrder source);

    [MapProperty(nameof(LisPatient.PracticePatientId), nameof(Patient.PatientId))]
    [MapperIgnoreTarget(nameof(Patient.Id))]
    [MapperIgnoreTarget(nameof(Patient.Created))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.LastModified))]
    [MapperIgnoreTarget(nameof(Patient.LastModifiedBy))]
    [MapperIgnoreTarget(nameof(Patient.Requests))]
    public static partial void UpdatePatientFromLis(LisPatient source, Patient target);

    [MapProperty(nameof(Patient.PatientId), nameof(LisPatient.PracticePatientId))]
    [MapperIgnoreTarget(nameof(LisPatient.PatientAge))]
    [MapperIgnoreTarget(nameof(LisPatient.CalculateAge))]
    [MapperIgnoreTarget(nameof(LisPatient.Symptoms))]
    [MapperIgnoreTarget(nameof(LisPatient.Samples))]
    public static partial LisPatient ToLisPatient(Patient source);

    private static bool MapIsSampleLocallyCreated(RequestSource source)
    {
        return source != RequestSource.Lis;
    }

    [MapperIgnoreTarget(nameof(LisRequestor.HasNonUniqueRequestorId))]
    private static partial LisRequestor ToLisRequestor(Requestor source);
}
