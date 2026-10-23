using System.Runtime.CompilerServices;

namespace Pharos.Client.Common.ViewModels;

/// <summary>
/// View model base that wraps a model and tracks modifications made through property changes.
/// </summary>
/// <remarks>
/// Pick this when the UI needs:
/// - A stateful, long-lived per-item view model instance
/// - Built-in dirty tracking via <see cref="IsModified"/>
///
/// Do not use this as a row view model in virtualized / lazy-loading lists. Virtualization can drop and recreate rows,
/// which will also drop the item view model instance and any UI-only state.
///
/// Alternatives:
/// <see cref="VirtualGridViewModel{TModel,TKey}"/> (virtualized lists).
/// </remarks>
public class ViewModelItem<TModel> : ViewModelBase, IDisposable
{
    private bool _isModified;

    public ViewModelItem(TModel model)
    {
        Model = model;
    }

    public TModel Model { get; private set; }

    public virtual bool IsModified
    {
        get => _isModified;
        private set => SetField(ref _isModified, value);
    }

    public void Dispose()
    {
        Disposing();
        GC.SuppressFinalize(this);
    }

    protected virtual void Disposing()
    {
    }

    protected void ModifiedChanged()
    {
        OnPropertyChanged(nameof(IsModified));
    }

    public void SetAsUnmodified()
    {
        _isModified = false;
        OnSetAsUnmodified();
        ModifiedChanged();
    }

    protected virtual void OnSetAsUnmodified()
    {
    }

    public void SetAsModified()
    {
        _isModified = true;
        ModifiedChanged();
    }

    protected sealed override bool SetField<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
    {
        var change = base.SetField(ref field, newValue, propertyName);
        if (change && propertyName != nameof(IsModified))
        {
            IsModified = true;
        }

        return change;
    }

    protected sealed override bool SetProperty<T>(T oldValue, T newValue, Action<T> setter, [CallerMemberName] string? propertyName = null)
    {
        var change = base.SetProperty(oldValue, newValue, setter, propertyName);
        if (change && propertyName != nameof(IsModified))
        {
            IsModified = true;
        }

        return change;
    }
}