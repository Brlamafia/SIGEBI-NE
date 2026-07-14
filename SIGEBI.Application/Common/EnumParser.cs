using SIGEBI.Application.Exceptions;

namespace SIGEBI.Application.Common;

public static class EnumParser
{
    public static TEnum ParseDefined<TEnum>(string value, string fieldName)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result)
            || !Enum.IsDefined(result))
        {
            throw new BusinessRuleException($"El valor de {fieldName} no es válido.");
        }

        return result;
    }
}
