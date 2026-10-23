using System.ComponentModel;
using System.Runtime.CompilerServices;
using Pharos.Client.Common.ViewModels.Interfaces;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// Root base class for view models that only provides property change notifications and setter helpers.
/// </summary>
/// <remarks>
/// Pick this when you need:
/// - INotifyPropertyChanged support
/// - Helpers to set fields/properties and raise PropertyChanged
///
/// This base does not include loading/busy state, save behavior, or modification tracking.
/// </remarks>
public abstract class ViewModelBase : IViewModel
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string? propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual bool SetProperty<T>(T oldValue, T newValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
        {
            return false;
        }

        setter(newValue);
        OnPropertyChanged(propertyName);
        return true;
    }

    protected virtual bool SetField<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, newValue))
        {
            return false;
        }

        field = newValue;
        OnPropertyChanged(propertyName);
        return true;
    }
}