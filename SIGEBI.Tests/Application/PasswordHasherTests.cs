using SIGEBI.Application.Security;

namespace SIGEBI.Tests.Application;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_Y_Verify_ProtegenLaContrasena()
    {
        const string password = "Segura123";

        var hash = PasswordHasher.Hash(password);

        Assert.NotEqual(password, hash);
        Assert.True(PasswordHasher.Verify(password, hash));
        Assert.False(PasswordHasher.Verify("Incorrecta123", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("corta1A")]
    [InlineData("sinmayuscula1")]
    [InlineData("SINMINUSCULA1")]
    [InlineData("SinNumeros")]
    public void Hash_RechazaContrasenasDebiles(string password)
    {
        Assert.Throws<ArgumentException>(() => PasswordHasher.Hash(password));
    }
}
