namespace Pharos.Client.Views;

public partial class LockScreenView
{
    private bool _hasTaskCards = true;

    private void OnHasCardsChanged(bool hasCards)
    {
        if (_hasTaskCards != hasCards)
        {
            _hasTaskCards = hasCards;
            StateHasChanged();
        }
    }
}