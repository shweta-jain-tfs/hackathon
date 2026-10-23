using System.Globalization;
using Microsoft.AspNetCore.Identity;

namespace Pharos.Server.Identity.Infrastructure.Authorization;
public class AuthorizationResult
{
    private readonly List<IdentityError> _errors = new();

    public bool Succeeded { get; protected set; }
    public string[]? LocalizationStrings { get; set; }

    public IEnumerable<IdentityError> Errors => _errors;

    public static AuthorizationResult Success { get; } = new() { Succeeded = true };


    internal static AuthorizationResult Failed(List<IdentityError>? errors)
    {
        var result = new AuthorizationResult { Succeeded = false };
        if (errors != null)
        {
            result._errors.AddRange(errors);
        }
        return result;
    }

    public override string ToString()
    {
        return Succeeded ?
               "Succeeded" :
               string.Format(CultureInfo.InvariantCulture, "{0} : {1}", "Failed", string.Join(",", Errors.Select(x => x.Code).ToList()));
    }
}