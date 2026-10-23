namespace Pharos.Server.Lis.HL7.Application.Messages;

/// <summary>
/// Structured representation of an HL7 order message.
/// </summary>
internal sealed class HL7OrderMessage
{
    public List<HL7Patient> Patients { get; } = [];

    /// <summary>
    /// Parses a flat HL7 segment list into the structured order message hierarchy.
    /// </summary>
    public static HL7OrderMessage Parse(HL7Message message)
    {
        var orderMessage = new HL7OrderMessage();

        HL7Patient? currentPatient = null;
        HL7Order? currentOrder = null;
        HL7ObservationRequest? currentObservation = null;
        HL7Specimen? currentSpecimen = null;

        foreach (var segment in message.Segments)
        {
            switch (segment)
            {
                case HL7PatientIdentificationSegment pid:
                    (currentOrder, currentObservation) = (null, null);
                    currentPatient = new HL7Patient(pid);
                    orderMessage.Patients.Add(currentPatient);
                    break;

                case HL7CommentSegment nte when currentPatient != null:
                    currentPatient.Comments.Add(nte);
                    break;

                case HL7CommonOrderSegment orc when currentPatient != null:
                    currentObservation = null;
                    currentOrder = new HL7Order(orc);
                    currentPatient.Orders.Add(currentOrder);
                    break;

                case HL7ObservationRequestSegment obr when currentOrder != null:
                    currentObservation = new HL7ObservationRequest(obr);
                    currentOrder.Observations.Add(currentObservation);
                    break;

                case HL7AdditionalTestInformationSegment obx when currentObservation != null:
                    currentObservation.AdditionalTestInformations.Add(obx);
                    break;

                case HL7SpecimenSegment spm when currentOrder != null:
                    currentSpecimen = new HL7Specimen(spm);
                    currentOrder.Specimen = currentSpecimen;
                    break;

                case HL7SpecimenContainerSegment sac when currentSpecimen != null:
                    currentSpecimen.SpecimenContainers.Add(sac);
                    break;
            }
        }

        return orderMessage;
    }
}

internal sealed class HL7Patient(HL7PatientIdentificationSegment patient)
{
    public HL7PatientIdentificationSegment Patient { get; } = patient;
    public List<HL7CommentSegment> Comments { get; } = [];
    public List<HL7Order> Orders { get; } = [];
}

internal sealed class HL7Order(HL7CommonOrderSegment commonOrder)
{
    public HL7CommonOrderSegment CommonOrder { get; } = commonOrder;
    public List<HL7ObservationRequest> Observations { get; } = [];
    public HL7Specimen? Specimen { get; set; }
}

internal sealed class HL7ObservationRequest(HL7ObservationRequestSegment observationRequest)
{
    public HL7ObservationRequestSegment ObservationRequest { get; } = observationRequest;
    public List<HL7AdditionalTestInformationSegment> AdditionalTestInformations { get; } = [];
}

internal sealed class HL7Specimen(HL7SpecimenSegment specimen)
{
    public HL7SpecimenSegment Specimen { get; } = specimen;
    public List<HL7SpecimenContainerSegment> SpecimenContainers { get; } = [];
}