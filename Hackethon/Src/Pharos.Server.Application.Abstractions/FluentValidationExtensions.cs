using FluentValidation;

namespace Pharos.Server.Application;

public static class FluentValidationExtensions
{
    public static bool IsDistinct<TSource, TResult>(this IEnumerable<TSource> elements, Func<TSource, TResult> selector)
    {
        var hashSet = new HashSet<TResult>();
        foreach (var element in elements.Select(selector))
        {
            if (!hashSet.Add(element))
            {
                return false;
            }
        }

        return true;
    }

    public static IRuleBuilder<T, IEnumerable<TSource>> Unique<T, TSource, TResult>(this IRuleBuilder<T, IEnumerable<TSource>> ruleBuilder, Func<TSource, TResult> selector, string? message = null)
    {
        if (selector == null)
        {
            throw new ArgumentNullException(nameof(selector), "Cannot pass a null selector.");
        }

        ruleBuilder
            .Must(x => x.IsDistinct(selector))
            .WithMessage(message ?? "Elements are not unique.");
        return ruleBuilder;
    }
}