using System.Linq.Expressions;

namespace Pharos.Server.Parameters.Application.Parsers.ValueTypeParsers;

internal class EnumParser<TEnum> : Parser<TEnum> where TEnum : struct, Enum
{
    private static readonly Func<int, TEnum> _caster = CastIntToEnum();

    private static Func<int, TEnum> CastIntToEnum()
    {
        var p = Expression.Parameter(typeof(int));
        var c = Expression.Convert(p, typeof(TEnum));
        return Expression.Lambda<Func<int, TEnum>>(c, p).Compile();
    }

    public override ParserResult<TEnum> GetValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new ParserResult<TEnum>(default);
        }

        var result = int.Parse(value);
        return new ParserResult<TEnum>(_caster(result));
    }
}