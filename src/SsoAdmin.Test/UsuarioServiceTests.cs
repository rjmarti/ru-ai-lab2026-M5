using Microsoft.EntityFrameworkCore;
using SsoAdmin.Application.Usuarios;
using SsoAdmin.Data;
using SsoAdmin.Models;

namespace SsoAdmin.Test;

/// <summary>
/// Tests de <see cref="UsuarioService"/> (Block 4 de spec-FEAT-001), contra una base SQLite en
/// archivo temporal, siguiendo el mismo patrón que <c>AuthenticationServiceTests</c> de Block 3.
/// </summary>
public class UsuarioServiceTests : IAsyncLifetime
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"ssoadmin-usuario-test-{Guid.NewGuid():N}.db");
    private SsoAdminDbContext _db = null!;
    private IUsuarioService _sut = null!;

    public async Task InitializeAsync()
    {
        DbContextOptions<SsoAdminDbContext> options = new DbContextOptionsBuilder<SsoAdminDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _db = new SsoAdminDbContext(options);
        await _db.Database.MigrateAsync();

        _sut = new UsuarioService(_db);
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    [Fact]
    public async Task CrearAsync_PersisteUsuarioActivo()
    {
        UsuarioDto usuario = await _sut.CrearAsync("Juan");

        Assert.True(usuario.Id > 0);
        Assert.Equal("Juan", usuario.Nombre);
        Assert.True(usuario.Activo);

        Usuario? persistido = await _db.Usuarios.FindAsync(usuario.Id);
        Assert.NotNull(persistido);
        Assert.True(persistido!.Activo);
    }

    [Fact]
    public async Task EditarNombreAsync_ActualizaElNombre()
    {
        UsuarioDto creado = await _sut.CrearAsync("Juan");

        UsuarioDto? editado = await _sut.EditarNombreAsync(creado.Id, "Nuevo Nombre");

        Assert.NotNull(editado);
        Assert.Equal("Nuevo Nombre", editado!.Nombre);
    }

    [Fact]
    public async Task DarDeBajaAsync_DejaElUsuarioInactivo()
    {
        UsuarioDto creado = await _sut.CrearAsync("Juan");

        bool exito = await _sut.DarDeBajaAsync(creado.Id);

        Assert.True(exito);

        Usuario? persistido = await _db.Usuarios.FindAsync(creado.Id);
        Assert.NotNull(persistido);
        Assert.False(persistido!.Activo);
    }

    [Fact]
    public async Task ListarAsync_DevuelveElEstadoCorrectoTrasCrearEditarYDarDeBaja()
    {
        UsuarioDto creado1 = await _sut.CrearAsync("Ana");
        UsuarioDto creado2 = await _sut.CrearAsync("Pedro");

        await _sut.EditarNombreAsync(creado1.Id, "Ana María");
        await _sut.DarDeBajaAsync(creado2.Id);

        List<UsuarioDto> usuarios = await _sut.ListarAsync();

        UsuarioDto? actualizado1 = usuarios.SingleOrDefault(u => u.Id == creado1.Id);
        UsuarioDto? actualizado2 = usuarios.SingleOrDefault(u => u.Id == creado2.Id);

        Assert.NotNull(actualizado1);
        Assert.Equal("Ana María", actualizado1!.Nombre);
        Assert.True(actualizado1.Activo);

        Assert.NotNull(actualizado2);
        Assert.False(actualizado2!.Activo);
    }

    [Fact]
    public async Task EditarNombreAsync_ConIdInexistente_DevuelveNullSinLanzarExcepcion()
    {
        UsuarioDto? resultado = await _sut.EditarNombreAsync(9999, "No existe");

        Assert.Null(resultado);
    }

    [Fact]
    public async Task DarDeBajaAsync_ConIdInexistente_DevuelveFalseSinLanzarExcepcion()
    {
        bool resultado = await _sut.DarDeBajaAsync(9999);

        Assert.False(resultado);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdExistente_DevuelveElUsuario()
    {
        UsuarioDto creado = await _sut.CrearAsync("Juan");

        UsuarioDto? obtenido = await _sut.ObtenerPorIdAsync(creado.Id);

        Assert.NotNull(obtenido);
        Assert.Equal(creado.Id, obtenido!.Id);
        Assert.Equal("Juan", obtenido.Nombre);
        Assert.True(obtenido.Activo);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdInexistente_DevuelveNullSinLanzarExcepcion()
    {
        UsuarioDto? resultado = await _sut.ObtenerPorIdAsync(9999);

        Assert.Null(resultado);
    }
}
