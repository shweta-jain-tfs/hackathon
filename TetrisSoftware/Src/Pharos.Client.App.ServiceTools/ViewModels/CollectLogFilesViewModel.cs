using Pharos.Client.Common.App.Interfaces;

namespace Pharos.Client.App.ServiceTools.ViewModels;

internal sealed class CollectLogFilesViewModel : ViewModelBase
{
    public CollectLogFilesViewModel(IDateTime dateTime)
    {
        var today = DateOnly.FromDateTime(dateTime.Now.Date);
        Request = new LogDumpCollectionRequest
        {
            DateMode = LogCollectionDateMode.SingleDate,
            SingleDate = today,
            FromDate = today,
            ToDate = today
        };
    }

    public LogDumpCollectionRequest Request { get; }

    public LogCollectionDateMode DateMode
    {
        get => Request.DateMode;
        set
        {
            if (Request.DateMode != value)
            {
                Request.DateMode = value;
                OnPropertyChanged(nameof(DateMode));
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public DateOnly SingleDate
    {
        get => Request.SingleDate!.Value;
        set
        {
            if (Request.SingleDate != value)
            {
                Request.SingleDate = value;
                OnPropertyChanged(nameof(SingleDate));
            }
        }
    }

    public DateOnly FromDate
    {
        get => Request.FromDate!.Value;
        set
        {
            if (Request.FromDate != value)
            {
                Request.FromDate = value;
                OnPropertyChanged(nameof(FromDate));
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public DateOnly ToDate
    {
        get => Request.ToDate!.Value;
        set
        {
            if (Request.ToDate != value)
            {
                Request.ToDate = value;
                OnPropertyChanged(nameof(ToDate));
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public LogFileSelection PharosFiles
    {
        get => Request.PharosFiles;
        set
        {
            if (Request.PharosFiles != value)
            {
                Request.PharosFiles = value;
                OnPropertyChanged(nameof(PharosFiles));
                OnPropertyChanged(nameof(CanStart));
            }
        }
    }

    public LogFileSelection IswFiles
    {
        get;
        set => SetField(ref field, value);
    }

    public bool CanStart => PharosFiles is LogFileSelection.Default or LogFileSelection.All
                            && (DateMode == LogCollectionDateMode.SingleDate || FromDate <= ToDate);
}
