using System.Reflection;
using FluentValidation;

namespace Pharos.Server.Application.Queries;
public abstract class SortedPartialQueryValidator<TQuery, TDto> : AbstractValidator<TQuery> where TQuery : SortedPartialQuery<TDto>
{
    protected SortedPartialQueryValidator()
    {
        When(_ => _.SortByProperty != null, () =>
            RuleFor(_ => _.SortByProperty)
                .Must(_ => GetAllowedSortProperties().Contains(_))
                .WithMessage(mp => $"Invalid sort by property {mp.SortByProperty}."));

        RuleFor(_ => _.StartIndex)
            .GreaterThanOrEqualTo(0);
        
        When(UseCount, () =>
        {
            RuleFor(_ => _.Count)
                .InclusiveBetween(1, 500);
        });

    }

    protected virtual string[] GetAllowedSortProperties()
    {
        return typeof(TDto)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !(p.GetMethod?.IsVirtual ?? false)) // exclude navigation properties
            .Select(p => p.Name).ToArray();
    }

    protected virtual bool UseCount(TQuery query)
    {
        return true;
    }
}
