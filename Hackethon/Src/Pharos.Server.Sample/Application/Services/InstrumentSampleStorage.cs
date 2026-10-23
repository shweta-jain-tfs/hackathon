using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Pharos.Server.Application.Settings.Sample;
using Pharos.Server.Domain;
using Pharos.Server.Domain.Enums;
using Pharos.Server.Sample.Application.Interfaces;
using Pharos.Server.Sample.Domain;

namespace Pharos.Server.Sample.Application.Services;

internal class InstrumentSampleStorage : IInstrumentSampleStorage
{
    private readonly IOptionsMonitor<SamplesSettings>? _samplesSettings;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SampleTube>> _tubes = new();

    private readonly ConcurrentDictionary<string, List<SampleDrawer>> _drawers = new();

    public InstrumentSampleStorage()
    {
    }

    public InstrumentSampleStorage(IOptionsMonitor<SamplesSettings> samplesSettings)
    {
        _samplesSettings = samplesSettings;
    }

    public void Clear(string instrumentNumber)
    {
        _drawers.AddOrUpdate(instrumentNumber, [], (_, _) => []);
        _tubes.AddOrUpdate(instrumentNumber, [], (_, _) => []);
    }

    public IQueryable<SampleTube> TubesAsQueryable(string instrumentNumber)
    {
        var tubes = _tubes.GetOrAdd(instrumentNumber, _ => new ConcurrentDictionary<string, SampleTube>());
        return tubes.Values.AsQueryable();
    }

    public SampleDrawer GetDrawer(string instrumentNumber, DrawerType drawerType)
    {
        if (_drawers.TryGetValue(instrumentNumber, out var drawers))
        {
            var drawer = drawers.FirstOrDefault(x => x.DrawerType == drawerType);
            if (drawer != null)
            {
                return drawer;
            }
        }

        return new SampleDrawer
        {
            DrawerType = drawerType,
        };
    }

    public SampleTray? GetTray(string trayId)
    {
        SampleTray? result = null;
        foreach (var drawers in _drawers.Values)
        {
            drawers.ForEach(d => d.Trays.ForEach(t =>
            {
                if (t.Id == trayId && t.Error != SampleTrayError.DuplicateBarcode)
                {
                    result = t;
                }
            }));
            if (result != null)
            {
                return result;
            }
        }
        return result;
    }

    public bool IsTrayInAnyInstrument(string trayId)
    {
        return GetTray(trayId) != null;
    }

    public bool UpdateTraysSummary(string instrumentNumber)
    {
        var summaryChanged = false;
        var tubes = _tubes.GetOrAdd(instrumentNumber, _ => new ConcurrentDictionary<string, SampleTube>());
        var traysSummary = tubes.Values.AsQueryable()
            .Where(x => x.Area == SampleArea.SampleDrawer1 || x.Area == SampleArea.SampleDrawer2)
            .GroupBy(tube => new { tube.Area, tube.TrayPosition })
            .ToDictionary(
                group => group.Key,
                group => new
                {
                    FreePositions = Constants.MaxSampleTubesInTray - group.Count(),
                    TubeErrorCount = group.Count(x => x.Error != SampleTubeError.None)
                }
            );

        // Get all existing trays for both sample drawers
        var existingTrays = GetTrays(instrumentNumber, DrawerType.Sample1)
            .Concat(GetTrays(instrumentNumber, DrawerType.Sample2))
            .ToList();

        foreach (var tray in existingTrays)
        {
            var area = tray.Drawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
            var key = new { Area = area, tray.TrayPosition };

            var updatedFreePositions = traysSummary.TryGetValue(key, out var summary)
                ? summary.FreePositions
                : Constants.MaxSampleTubesInTray;

            var updatedTubeErrorCount = traysSummary.TryGetValue(key, out summary)
                ? summary.TubeErrorCount
                : 0;

            if (tray.FreePositions != updatedFreePositions)
            {
                summaryChanged = true;
                tray.FreePositions = updatedFreePositions;
            }
            if (tray.TubeErrorCount != updatedTubeErrorCount)
            {
                summaryChanged = true;
                tray.TubeErrorCount = updatedTubeErrorCount;
            }
        }

        return summaryChanged;
    }

    public List<SampleTray> GetTrays(string? instrumentNumber = null, DrawerType? drawerType = null)
    {
        var trays = new List<SampleTray>();

        // Filter by instrument number if provided
        var filteredDrawers = string.IsNullOrEmpty(instrumentNumber)
            ? _drawers.Values.SelectMany(d => d)
            : _drawers.TryGetValue(instrumentNumber, out var drawers) ? drawers : Enumerable.Empty<SampleDrawer>();

        // Filter by drawer type if provided
        if (drawerType.HasValue)
        {
            filteredDrawers = filteredDrawers.Where(d => d.DrawerType == drawerType.Value);
        }

        // Collect trays from the filtered drawers
        foreach (var drawer in filteredDrawers)
        {
            trays.AddRange(drawer.Trays);
        }

        return trays;
    }

    public SampleTube? GetTube(string tubeId)
    {
        foreach (var instrumentTubes in _tubes.Values)
        {
            if (instrumentTubes.TryGetValue(tubeId, out var tube))
            {
                return tube;
            }
        }
        return null;
    }

    public List<SampleTube> GetTubesBySampleId(string sampleId, bool includeTubesWithError = false)
    {
        var comparer = GetSampleIdComparer();
        var result = new List<SampleTube>();
        foreach (var instrumentTubes in _tubes.Values)
        {
            result.AddRange(
                instrumentTubes.Values
                    .Where(t => comparer.Equals(t.SampleId, sampleId) && (includeTubesWithError || t.InstrumentError == SampleTubeError.None))
            );
        }
        return result;
    }

    public List<SampleTube> GetTubesBySampleIds(IReadOnlySet<string> sampleIds, bool includeTubesWithError = false)
    {
        var matchingSampleIds = sampleIds.ToHashSet(GetSampleIdComparer());
        var result = new List<SampleTube>();
        foreach (var instrumentTubes in _tubes.Values)
        {
            result.AddRange(
                instrumentTubes.Values
                    .Where(t => t.SampleId != null
                                && matchingSampleIds.Contains(t.SampleId)
                                && (includeTubesWithError || t.InstrumentError == SampleTubeError.None))
            );
        }
        return result;
    }

    private StringComparer GetSampleIdComparer()
    {
        return _samplesSettings?.CurrentValue.CaseInsensitiveSampleId == true
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
    }

    public void AddOrUpdateTube(SampleTube tube, Func<SampleTube, SampleTube> updateTubeFunc)
    {
        CheckTubePosition(tube);

        var instrumentNumber = tube.InstrumentNumber!;
        var instrumentTubes = _tubes.GetOrAdd(instrumentNumber, _ => new ConcurrentDictionary<string, SampleTube>());

        var originalTubeId = tube.TubeId ?? "";
        var temporaryTubeId = GetTemporaryTubeId(instrumentNumber, tube.Area, tube.TubePosition!.Value, tube.TrayPosition);
        var tubeIdentified = !string.IsNullOrEmpty(originalTubeId);

        if (tubeIdentified)
        {
            if (!instrumentTubes.ContainsKey(originalTubeId) && instrumentTubes.TryRemove(temporaryTubeId, out var oldNotIdentifiedTube))
            {
                // there was a tube with the temporary ID, we should update it
                // with the new identified tube info and add it back to storage with the permanent ID.
                var updatedTube = updateTubeFunc(oldNotIdentifiedTube);

                // Make sure that the tube ID is changes from the temporary to the permanent one
                // Don't rely on the calling code
                updatedTube.TubeId = originalTubeId;

                instrumentTubes.AddOrUpdate(originalTubeId, updatedTube, (_, _) => updatedTube);
            }
            else
            {
                // if there is no tube with the temporary ID, we can directly or update the tube with a permanent ID.
                instrumentTubes.AddOrUpdate(originalTubeId, tube, (_, oldVal) =>
                {
                    var result = updateTubeFunc(oldVal);
                    if (result.TubeId != originalTubeId)
                    {
                        throw new InvalidOperationException("The tube ID cannot be changed in the update function, because it leads to 'lost' tubes.");
                    }
                    return result;
                });
            }
        }
        else
        {
            // if the tube is not identified with a permanent ID, we should use the temporary ID for storage.
            instrumentTubes.AddOrUpdate(temporaryTubeId, tube, (_, oldVal) =>
            {
                var result = updateTubeFunc(oldVal);
                if (!string.IsNullOrEmpty(result.TubeId))
                {
                    throw new InvalidOperationException("The tube ID cannot be changed in the update function, because it leads to 'lost' tubes.");
                }
                return result;
            });

        }
    }

    public void RemoveTube(SampleTube tube)
    {
        var tubeId = tube.TubeId;
        if (string.IsNullOrEmpty(tubeId))
        {
            CheckTubePosition(tube);
            tubeId = GetTemporaryTubeId(tube.InstrumentNumber!, tube.Area, tube.TubePosition!.Value, tube.TrayPosition);
        }

        if (_tubes.TryGetValue(tube.InstrumentNumber!, out var instrumentTubes))
        {
            // This condition is required to ensure that we do not remove the wrong tube when there are multiple tubes with the same ID
            // Duplicate tube IDs can occur when a tube is moved from one position to another(in the same instrument or different instruments) and storage is not updated yet.
            // Also, this is to ensure that we remove the correct tube object both in the ISW & SampleStorage when GetSampleDrawerContentsResponse is sent with TubeId = null.
            tubeId = instrumentTubes.FirstOrDefault(x => x.Key == tubeId && x.Value.InstrumentNumber == tube.InstrumentNumber &&
                                         x.Value.Area == tube.Area && x.Value.TubePosition == tube.TubePosition).Key;

            if (!string.IsNullOrEmpty(tubeId))
            {
                instrumentTubes.TryRemove(tubeId, out _);
            }

            if (instrumentTubes.IsEmpty)
            {
                _tubes.TryRemove(tube.InstrumentNumber!, out _);
            }
        }
    }

    public void UpdateDrawer(string instrumentNumber, SampleDrawer drawer)
    {
        drawer.Trays.ForEach(x =>
        {
            x.InstrumentNumber = instrumentNumber;
            x.Drawer = drawer.DrawerType;
        });

        _drawers.AddOrUpdate(
            instrumentNumber,
            [drawer],
            (_, oldVal) =>
            {
                var result = oldVal.Where(x => x.DrawerType != drawer.DrawerType).ToList();
                result.Add(drawer);
                return result;
            });
    }

    public bool UpdateTrayAsUnknownObject(string trayId)
    {
        var tray = GetTray(trayId);
        if (tray == null)
        {
            return false;
        }

        tray.Id = string.Empty;
        tray.Error = SampleTrayError.UnknownObject;
        tray.FreePositions = Constants.MaxSampleTubesInTray;
        tray.TubeErrorCount = 0;

        var area = tray.Drawer == DrawerType.Sample1 ? SampleArea.SampleDrawer1 : SampleArea.SampleDrawer2;
        var tubesInTray = TubesAsQueryable(tray.InstrumentNumber!)
            .Where(x => x.Area == area && x.TrayPosition == tray.TrayPosition)
            .ToList();

        foreach (var tube in tubesInTray)
        {
            RemoveTube(tube);
        }

        return true;
    }

    private static void CheckTubePosition(SampleTube tube)
    {
        if (tube.InstrumentNumber == null)
        {
            throw new InvalidOperationException("The sample tube is not in the instrument.");
        }
        if (tube.TubePosition is null or 0)
        {
            throw new InvalidOperationException("The sample tube has no position.");
        }

        if (tube.Area is SampleArea.SampleDrawer1 or SampleArea.SampleDrawer2 && tube.TrayPosition == null)
        {
            throw new InvalidOperationException("The sample tube has no tray position.");
        }
    }

    private static string GetTemporaryTubeId(string instrumentNumber, SampleArea area, int tubePosition, int? trayPosition = null)
    {
        return $"{instrumentNumber}_{area}_{tubePosition}{(trayPosition.HasValue ? $"_{trayPosition}" : string.Empty)}";
    }
}
