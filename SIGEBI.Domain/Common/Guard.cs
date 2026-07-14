namespace SIGEBI.Domain.Common;

public static class Guard
{
    public static int AgainstNonPositive(int value, string parameterName)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(parameterName, "El identificador debe ser mayor que cero.");

        return value;
    }

    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("El valor es obligatorio.", parameterName);

        return value.Trim();
    }
}
