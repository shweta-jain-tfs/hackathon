using Pharos.Client.Common.Models;
using Pharos.Contracts;
using Riok.Mapperly.Abstractions;

namespace Pharos.Client.App.InstrumentManagement;

[Mapper]
public static partial class InstrumentManagementMapper
{
    public static partial UpdateAssaySchedulerDayCommand ToUpdateAssaySchedulerDayCommand(this AssaySchedulerDay source);

    public static partial UpdateInstrumentCommand ToUpdateInstrumentCommand(this Instrument source);

    public static partial AssaySchedulerDay ToAssaySchedulerDay(this AssaySchedulerDayDto source);
}