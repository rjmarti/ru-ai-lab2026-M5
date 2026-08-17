using SsoAdmin.Application.Auth;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de <see cref="PasswordHasherService.Verify"/>, agregado en Block 3 de spec-FEAT-001.
/// </summary>
public class PasswordHasherServiceTests
{
    private readonly IPasswordHasherService _sut = new PasswordHasherService();

    [Fact]
    public void Verify_ConLaMismaContrasenia_DevuelveTrue()
    {
        string hash = _sut.Hash("admin");

        bool resultado = _sut.Verify(hash, "admin");

        Assert.True(resultado);
    }

    [Fact]
    public void Verify_ConContraseniaDistinta_DevuelveFalse()
    {
        string hash = _sut.Hash("admin");

        bool resultado = _sut.Verify(hash, "wrong");

        Assert.False(resultado);
    }
}
