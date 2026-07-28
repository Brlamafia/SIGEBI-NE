using System.Security.Cryptography;

namespace SIGEBI.Application.Security;

public static class PasswordHasher
{
    private const int Iteraciones = 120_000;
    private const int TamanoSal = 16;
    private const int TamanoHash = 32;

    public static string Hash(string password)
    {
        ValidarFortaleza(password);
        var sal = RandomNumberGenerator.GetBytes(TamanoSal);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            sal,
            Iteraciones,
            HashAlgorithmName.SHA256,
            TamanoHash);
        return $"PBKDF2-SHA256${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(encoded))
            return false;
        var partes = encoded.Split('$');
        if (partes.Length != 4 || partes[0] != "PBKDF2-SHA256" ||
            !int.TryParse(partes[1], out var iteraciones))
            return false;
        try
        {
            var sal = Convert.FromBase64String(partes[2]);
            var esperado = Convert.FromBase64String(partes[3]);
            var actual = Rfc2898DeriveBytes.Pbkdf2(
                password,
                sal,
                iteraciones,
                HashAlgorithmName.SHA256,
                esperado.Length);
            return CryptographicOperations.FixedTimeEquals(actual, esperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static void ValidarFortaleza(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8 ||
            !password.Any(char.IsUpper) || !password.Any(char.IsLower) ||
            !password.Any(char.IsDigit))
            throw new ArgumentException(
                "La contraseña debe tener al menos 8 caracteres, mayúscula, minúscula y número.",
                nameof(password));
    }
}
