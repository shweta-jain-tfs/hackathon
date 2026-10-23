using Pharos.Client.App.Parameters.Reflex.ViewModels;
using Pharos.Common.Web.Components.Modal;

namespace Pharos.Client.App.Parameters.Reflex.Views;

public partial class ConditionView : ComponentBase
{
    [Inject] private IStringLocalizer<Translations> Loc { get; set; } = default!;
    [Inject] private IModalService ModalService { get; set; } = default!;
    [Parameter] public string? DefinitionName { get; set; }
    [Parameter] public ReflexConditionViewModel ViewModel { get; set; } = default!;

    // Callback to notify parent page when test is selected so validation can clear
    [Parameter] public EventCallback OnTestSelected { get; set; }

    private int CurrentStep
    {
        get
        {
            if (IsEmptyDefinitionName())
            {
                return 1;
            }

            if (ViewModel.NullableCompareType != null)
            {
                return 4;
            }

            if (ViewModel.NullableResultType != null)
            {
                return 3;
            }

            if (!string.IsNullOrEmpty(ViewModel.TestName))
            {
                return 2;
            }

            return 1;
        }
    }

    private bool IsEmptyDefinitionName()
    {
        return string.IsNullOrWhiteSpace(DefinitionName);
    }

    #region --- TestsAddView ---

    public async Task ConditionTestAddViewOpen()
    {
        var currentTestOrders = string.IsNullOrEmpty(ViewModel.TestName) || string.IsNullOrEmpty(ViewModel.MethodName)
            ? Enumerable.Empty<object>()
            : [new ReflexTest { TestName = ViewModel.TestName, MethodName = ViewModel.MethodName }];

        var parameters = new ModalParameters
        {
            { nameof(ConditionTestAddView.CurrentTestOrders), currentTestOrders }
        };

        var model = ModalService.Show<ConditionTestAddView>(new ModalOptions { Title = Loc["ConditionTestAddView_Title"], ModalHeight = ModalSize.XLarge, ModalWidth = ModalSize.Large }, parameters);
        var result = await model.Result;

        if (!result.Canceled)
        {
            var data = (MethodTest?)result.Data;

            if (data == null)
            {
                ViewModel.ResetResult();
            }
            else if (!(data.TestName == ViewModel.TestName && data.MethodName == ViewModel.MethodName))
            {
                await ViewModel.ApplyData(data);

                // Notify parent that test was selected
                if (OnTestSelected.HasDelegate)
                {
                    await OnTestSelected.InvokeAsync();
                }
            }
        }
    }

    #endregion --- TestsAddView ---

    private string GetString(string? viewModelTestName)
    {
        if (string.IsNullOrEmpty(viewModelTestName))
        {
            return Loc["ConditionEditView_SelectTest"];
        }

        return viewModelTestName;
    }
}