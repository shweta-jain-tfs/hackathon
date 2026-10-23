using MediatR;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Instrument;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Instrument.Application.Interfaces;
using Tetris.Protocol.Instrument;

namespace Pharos.Server.Instrument.Application.Commands.UpdateSettings;

internal record UpdateSettingsCommand(string InstrumentNumber, Guid? SessionId = null) : IStateSynchronizationCommand;

internal class UpdateSettingsCommandHandler(
    ICommunicationSessionManager sessionManager,
    IOptionsSnapshot<InstrumentSettings> instrumentSettings,
    IOptionsSnapshot<TubeTypesSettings> tubeSettings,
    IOptionsSnapshot<TubeBarcodesSettings> tubeBarcodeSettings,
    IPublisher publisher) : StateSynchronizationCommandHandlerBase<UpdateSettingsCommand>(sessionManager, CommunicationSynchronizationState.UpdateSettingsSent, publisher)
{
    protected override async Task DoHandle(UpdateSettingsCommand request, CommunicationSession session, CancellationToken cancellationToken)
    {
        var tubeTypes = tubeSettings.Value.TubeTypeList;
        var instrument = instrumentSettings.Value.GetInstrumentSettings(request.InstrumentNumber);

        var settingsInfo = new Settings
        {
            SleepTimeout = 0,
            LoudspeakerVolume = 0,
            TimeZone = TimeZoneInfo.Local.Id,
            KeepExpiredReagentsOnBoard = instrument.Reagents.KeepExpiredReagentsOnBoard,
            KeepEmptyReagentsOnBoard = instrument.Reagents.KeepEmptyReagentsOnBoard,
            TubeSettings = MapTubeTypes(tubeTypes),
            SoundWhenError = false,
            AutoLockCompartmentTimeout = instrument.DrawerLock.AutoLockCompartmentTimeout,
            PipedInRinse = instrument.Rinse.PipedInRinseInstalled,
            PipedInRinseActive = instrument.Rinse.PipedInActive,
            PipedOutWaste = instrument.Waste.PipedOutWasteInstalled,
            PipedInWasteActive = instrument.Waste.PipedOutActive,
            MaxBottleVolumeDifference = 0,
            CultureName = string.Empty,
            InputLanguage = string.Empty,
            SampleBarcodeUsage = MapSampleBarcodeUsage(tubeBarcodeSettings.Value),
            MaxLiquidWasteFillLevel = instrument.Waste.MaximumLiquidWasteFillingVolume
        };

        await session.Connection.UpdateSettingsAsync(settingsInfo, cancellationToken);
    }

    private static SampleBarcodeConfiguration MapSampleBarcodeUsage(TubeBarcodesSettings settings)
    {
        var result = new SampleBarcodeConfiguration();

        result.SampleBarcodePrefixes.AddRange(GetSampleBarcodePrefixes(settings));

        var enabledSymbologies = GetEnabledSymbologies(settings).ToList();
        result.InternalSymbologies.AddRange(enabledSymbologies);
        result.ExternalSymbologies.AddRange(enabledSymbologies);

        return result;
    }

    private static IEnumerable<string> GetSampleBarcodePrefixes(TubeBarcodesSettings settings)
    {
        return new[]
            {
                settings.Prefix1,
                settings.Prefix2,
                settings.Prefix3,
                settings.Prefix4,
                settings.Prefix5
            }
            .Where(prefix => !string.IsNullOrWhiteSpace(prefix))
            .Select(prefix => prefix.Trim());
    }

    private static IEnumerable<EnumBarcodeSymbology> GetEnabledSymbologies(TubeBarcodesSettings settings)
    {
        if (settings.Code39)
        {
            yield return settings.Code39Checksum
                ? EnumBarcodeSymbology.BarcodeSymbologyCode39CheckSumOn
                : EnumBarcodeSymbology.BarcodeSymbologyCode39ChecksumOff;
        }

        if (settings.Code93)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyCode93;
        }

        if (settings.Code128)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyCode128;
        }

        if (settings.Interleaved2Of5)
        {
            yield return settings.Interleaved2Of5Checksum
                ? EnumBarcodeSymbology.BarcodeSymbologyInterleaved2Of5ChecksumOn
                : EnumBarcodeSymbology.BarcodeSymbologyInterleaved2Of5CheckSumOff;
        }

        if (settings.CodaBar)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyCodaBar;
        }

        if (settings.Ean8)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyEan8;
        }

        if (settings.Industrial2Of5)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyIndustrial2Of5;
        }

        if (settings.Coop2Of5)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyCoop2Of5;
        }

        if (settings.QrCode)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyQrcode;
        }

        if (settings.DataMatrix)
        {
            yield return EnumBarcodeSymbology.BarcodeSymbologyDataMatrix;
        }
    }

    private TubeSettings MapTubeTypes(List<TubeType> tubeTypes)
    {
        var result = new TubeSettings
        {
            DefaultTube = GetDefaultTube()
        };

        result.TubeParameterList.AddRange(tubeTypes.Select(MapTubeParameters));
        result.TubeParameterListSimplified.AddRange(tubeTypes.Select(MapTubeParametersSimplified));

        return result;
    }

    private static TubeParameters MapTubeParameters(TubeType tubeType)
    {
        return new TubeParameters
        {
            TubeTypeIndex = tubeType.TubeDefinitionId,
            BottomDiameter = tubeType.BottomDiameter,
            ConicalDiameter = tubeType.ConicalDiameter,
            ConicalLength = tubeType.ConicalLength,
            GelSeparationLayerDetection = Map(tubeType.GelSeparationLayerDetection),
            InnerBottomLevel = tubeType.InnerBottomLevel,
            InnerDiameter = tubeType.InnerDiameter,
            StraightPartLength = tubeType.StraightPartLength
        };
    }

    private static TubeParametersSimplified MapTubeParametersSimplified(TubeType tubeType)
    {
        return new TubeParametersSimplified
        {
            TubeTypeIndex = tubeType.TubeDefinitionId,
            GelSeparationLayerDetection = Map(tubeType.GelSeparationLayerDetection),
            InnerBottomLevel = tubeType.InnerBottomLevel,
            InnerDiameter = tubeType.InnerDiameter,
            BottomType = Map(tubeType.BottomShape)
        };
    }

    private static EnumGelSeparationLayerDetection Map(GelSeparationLayerDetection gelSeparationLayerDetection)
    {
        return gelSeparationLayerDetection switch
        {
            GelSeparationLayerDetection.OnNoError => EnumGelSeparationLayerDetection.GelSeparationLayerDetectionOnNoError,
            GelSeparationLayerDetection.OnError => EnumGelSeparationLayerDetection.GelSeparationLayerDetectionOnError,
            GelSeparationLayerDetection.DetectionOff => EnumGelSeparationLayerDetection.GelSeparationLayerDetectionOff,
            _ => throw new ArgumentOutOfRangeException(nameof(gelSeparationLayerDetection), gelSeparationLayerDetection, null)
        };
    }

    private static EnumBottomType Map(BottomShapeType bottomShape)
    {
        return bottomShape switch
        {
            BottomShapeType.VShaped => EnumBottomType.VShaped,
            BottomShapeType.UShaped => EnumBottomType.UShaped,
            BottomShapeType.Flat => EnumBottomType.Flat,
            _ => throw new ArgumentOutOfRangeException(nameof(bottomShape), bottomShape, null)
        };
    }

    private int GetDefaultTube()
    {
        var tubeTypes = tubeSettings.Value.TubeTypeList;

        TubeType? defaultTubeType = null;
        if (int.TryParse(tubeSettings.Value.NormalTubeDefinitionId, out var normalTubeId))
        {
            defaultTubeType = tubeTypes.FirstOrDefault(t => t.TubeDefinitionId == normalTubeId);
        }


        if (defaultTubeType != null)
        {
            return defaultTubeType.TubeDefinitionId;
        }
        return tubeTypes.FirstOrDefault()?.TubeDefinitionId ?? -1;

    }
}
