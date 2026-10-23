using FluentValidation;
using Pharos.Server.Domain;
using Tetris.Protocol.Common;
using Tetris.Protocol.Pharos.Storage;

namespace Pharos.Server.Instrument.Application.Validators;
internal class InstrumentStorageValidator : AbstractValidator<InstrumentStorage>
{
    public InstrumentStorageValidator()
    {
        RuleForEach(c => c.SampleDrawers).SetValidator(new SampleDrawerValidator());

        // Full storage messages must include ReagentDrawer.
        RuleFor(c => c.ReagentDrawer)
            .NotNull()
            .When(c => c.IsFullMessage)
            .WithMessage("ReagentDrawer must be present in a full storage message.");

        When(c => c.ReagentDrawer is not null, () =>
        {
            RuleFor(c => c.ReagentDrawer!).SetValidator(new ReagentDrawerValidator());
        });

        RuleForEach(c => c.Samples).SetValidator(new SampleTubeValidator());

        RuleForEach(c => c.DilutionPlateStacks).SetValidator(new DilutionPlateStackValidator());

        RuleForEach(c => c.Cartridges).SetValidator(new InstrumentStorageCartridgesValidator());

        RuleForEach(c => c.Carriers).SetValidator(new InstrumentStorageCarriersValidator());

        RuleForEach(c => c.SolidWasteBins).SetValidator(new InstrumentStorageSolidWasteBinsValidator());

        RuleForEach(c => c.LiquidWaste).SetValidator(new InstrumentStorageLiquidWasteValidator());

        RuleFor(c => c)
            .Must(HaveValidReagentsCombination)
            .WithMessage("The combination of cartridges and carriers exceeds the allowed limit of 72 carriers or 12 cartridges or their combination.");
    }

    private static bool HaveValidReagentsCombination(InstrumentStorage storage)
    {
        if (storage.ReagentDrawer is null)
        {
            return true;
        }

        var drawerPositions = storage.ReagentDrawer.ReagentLoaders.Select(x => x.ReagentDrawerPosition).Distinct();

        foreach (var drawerPosition in drawerPositions)
        {
            var totalCarriers = storage.Carriers.Count(x =>
                x.ChangeType != EnumChangeType.DeletedObject &&
                x.Position?.PositionDrawer is not null &&
                x.Position.PositionInternalStorage == 0 &&
                x.Position.PositionDrawer.DrawerPosition == drawerPosition);

            var totalCartridges = storage.Cartridges.Count(x =>
                x.ChangeType != EnumChangeType.DeletedObject &&
                x.Position?.PositionDrawer is not null &&
                x.Position.Area == EnumCartridgeArea.ReagentDrawerArea &&
                x.Position.PositionDrawer.DrawerPosition == drawerPosition);

            var totalItems = totalCarriers + totalCartridges;

            if (totalItems > Constants.MaxReagentLoaderPosition || totalCartridges > Constants.MaxReagentCompartments)
            {
                return false;
            }
        }

        return true;
    }
}

internal class ReagentDrawerValidator : AbstractValidator<ReagentDrawer>
{
    public ReagentDrawerValidator()
    {
        RuleFor(x => x.ReagentLoaders)
            .Must(loaders => loaders.Count <= Constants.MaxReagentLoaders)
            .WithMessage("Invalid Reagent Loader Position");

        RuleForEach(x => x.ReagentLoaders).SetValidator(new ReagentLoaderValidator());
    }
}

internal class SampleDrawerValidator : AbstractValidator<SampleDrawer>
{
    public SampleDrawerValidator()
    {
        RuleFor(c => c.Id).Must(a => a != EnumSampleDrawer.UnspecifiedSampleDrawer);
        RuleFor(c => c.Trays)
            .Must(trays => trays.Count <= Constants.MaxTraysInSampleDrawer)
            .WithMessage("The list must have a maximum length of 4.");
    }
}

internal class SampleTubeValidator : AbstractValidator<SampleTube>
{
    public SampleTubeValidator()
    {
            RuleFor(c => c.Position.Area).Must(a => a != EnumSampleArea.UnspecifiedSampleArea);
    }
}

internal class ReagentLoaderValidator : AbstractValidator<ReagentLoader>
{
    public ReagentLoaderValidator()
    {
        RuleFor(c => c.ReagentDrawerPosition).InclusiveBetween(1, Constants.MaxReagentLoaders)
            .WithMessage("Invalid Reagent Loader Position");
    }
}

internal class DilutionPlateStackValidator : AbstractValidator<DilutionPlateStack>
{
    public DilutionPlateStackValidator()
    {
        RuleFor(stack => stack.Position)
            .InclusiveBetween(1, Constants.MaxDilutionPlateStack)
            .WithMessage("Position must be between 1 and 4.");

        RuleFor(stack => stack.NumberOfPlates)
            .InclusiveBetween(0, Constants.MaxDilutionPlateNumber)
            .WithMessage("Number of plates must be between 0 and 5.");
    }
}

internal class InstrumentStorageCartridgesValidator : AbstractValidator<Cartridge>
{
    public InstrumentStorageCartridgesValidator()
    {
        When(cartridge =>
                cartridge.Position is not null &&
                cartridge.Position.Area == EnumCartridgeArea.ReagentDrawerArea &&
                cartridge.Position.PositionDrawer is not null,
            () =>
        {
            RuleFor(cartridge => cartridge.Position!.PositionDrawer!.DrawerPosition)
                .InclusiveBetween(1, 4)
                .WithMessage("DrawerPosition for Cartridge must be between 1 and 4.");

            RuleFor(cartridge => cartridge.Position!.PositionDrawer!.CompartmentPosition)
                .InclusiveBetween(1, Constants.MaxReagentCompartments)
                .WithMessage("CompartmentPosition must be between 1 and 12.");

            RuleFor(cartridge => cartridge.Position!.PositionDrawer!.SubCompartmentPosition)
                .Equal(0)
                .WithMessage("SubCompartmentPosition for Cartridge must be 0.");
        });
    }
}

internal class InstrumentStorageCarriersValidator : AbstractValidator<CarrierInformation>
{
    public InstrumentStorageCarriersValidator()
    {
        When(carrier =>
                carrier.Position is not null &&
                carrier.Position.PositionInternalStorage == 0 &&
                carrier.Position.PositionDrawer is not null,
            () =>
        {
            RuleFor(carrier => carrier.Position!.PositionDrawer!.DrawerPosition)
                .InclusiveBetween(1, 4)
                .WithMessage("DrawerPosition for Carrier must be between 1 and 4.");

            RuleFor(carrier => carrier.Position!.PositionDrawer!.CompartmentPosition)
                .InclusiveBetween(1, Constants.MaxReagentCompartments)
                .WithMessage("CompartmentPosition must be between 1 and 12.");

            RuleFor(carrier => carrier.Position!.PositionDrawer!.SubCompartmentPosition)
                .InclusiveBetween(1, Constants.MaxReagentCarriersInCompartment)
                .WithMessage("SubCompartmentPosition for Carrier must be between 1 and 6.");
        });
    }
}

internal class InstrumentStorageSolidWasteBinsValidator : AbstractValidator<WasteContainer>
{
    public InstrumentStorageSolidWasteBinsValidator()
    {
        RuleFor(bin => bin.Position)
            .InclusiveBetween(1, 2)
            .WithMessage("Position must be between 1 and 2.");

        RuleFor(bin => bin.Type)
            .NotEqual(EnumWasteContainerType.WasteContainerTypeLiquid)
            .WithMessage("Invalid type.");
    }
}

internal class InstrumentStorageLiquidWasteValidator : AbstractValidator<WasteContainer>
{
    public InstrumentStorageLiquidWasteValidator()
    {
        RuleFor(bin => bin.Position)
            .InclusiveBetween(1, 2)
            .WithMessage("Position must be between 1 and 2.");

        RuleFor(bin => bin.Type)
            .NotEqual(EnumWasteContainerType.WasteContainerTypeRecyclable)
            .NotEqual(EnumWasteContainerType.WasteContainerTypeBioHazard)
            .WithMessage("Invalid type.");
    }
}
