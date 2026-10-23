using FluentValidation;

namespace Pharos.Client.App.Parameters.QualityControl.ViewModels;

public class LabQualityControlDetailViewModelValidator : AbstractValidator<LabQualityControlDetailViewModel>
{
    public LabQualityControlDetailViewModelValidator(IStringLocalizer<Translations> loc)
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage(loc["LabQualityControlDetailView_ValidationError_ValueRequired"]);

        RuleFor(x => x.IdOnBarcode)
            .NotEmpty()
            .WithMessage(loc["LabQualityControlDetailView_ValidationError_ValueRequired"]);
        
        RuleFor(x => x.TubeTypeId)
            .GreaterThan(0)
            .WithMessage(loc["LabQualityControlDetailView_ValidationError_SelectValue"]);
    }
}