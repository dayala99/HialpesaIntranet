using Intranet.Entity;
using Microsoft.Data.SqlClient;

namespace Intranet.Data;

public sealed class GestionConnectionFactory(IConfiguration configuration)
{
    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var value = configuration.GetConnectionString("Gestion");
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("Configura ConnectionStrings:Gestion antes de activar el modo SqlServer.");
        var connection = new SqlConnection(value);
        try { await connection.OpenAsync(cancellationToken); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }
}
