using Pharos.Common.Web.Components.FormElements;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class EditQualityControlScheduleViewModel
{
    public EditQualityControlScheduleViewModel(int id, Periodically periodically, int startAfter, int runEvery)
    {
        Id = id;
        StartAfter = startAfter;
        RunEvery = runEvery;
        Periodically = periodically;

        PeriodicalItems =
        [
            new DropDownOption<Periodically>{ Value = Periodically.No, Text = Periodically.No.ToLocalizedString() },
            new DropDownOption<Periodically>{ Value = Periodically.YesByNumberOfTests, Text = Periodically.YesByNumberOfTests.ToLocalizedString() },
            new DropDownOption<Periodically>{ Value = Periodically.YesByTime, Text = Periodically.YesByTime.ToLocalizedString() }
        ];
    }

    public int Id { get; set; }

    public Periodically Periodically
    {
        get;
        set
        {
            field = value;
            if (value == Periodically.No)
            {
                StartAfter = 0;
                RunEvery = 1;
            }
            ValidationNeeded();
        }
    }

    public int StartAfter { get; set; }
    public int RunEvery { get; set; }
    public List<DropDownOption<Periodically>> PeriodicalItems { get; }

    public event EventHandler? ReValidate;

    private void ValidationNeeded()
    {
        ReValidate?.Invoke(this, EventArgs.Empty);
    }
}