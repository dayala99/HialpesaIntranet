using Intranet.Entity;
using Dapper;
using Microsoft.Extensions.Options;
namespace Intranet.Data;

public sealed class GestionLoginRepository(GestionConnectionFactory factory, IOptions<GestionOptions> options) : IGestionLoginRepository
{
    public async Task<GestionLoginUser?> VerifyAsync(string username, string password, CancellationToken ct)
    {
        await using var connection = await factory.OpenAsync(ct);
        const string sql = "dbo.PA_SEG_USUARIOS_S0001";
        var users = (await connection.QueryAsync<GestionLoginUser>(new CommandDefinition(sql, new
        {
            Username = new DbString { Value = username, IsAnsi = true, Length = 15 },
            Password = new DbString { Value = password, IsAnsi = true, Length = 128 }
        }, commandTimeout: options.Value.CommandTimeoutSeconds, cancellationToken: ct, commandType: System.Data.CommandType.StoredProcedure))).ToArray();
        return users.Length == 1 ? users[0] : null;
    }
}
