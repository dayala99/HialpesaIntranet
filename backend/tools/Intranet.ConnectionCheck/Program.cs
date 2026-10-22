using Intranet.Entity;
using Intranet.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

if (args.Length != 3 || !System.Text.RegularExpressions.Regex.IsMatch(args[0], "^[A-Za-z0-9_]{1,15}$") || !System.Text.RegularExpressions.Regex.IsMatch(args[1], "^[0-9]{3}$") || !System.Text.RegularExpressions.Regex.IsMatch(args[2], "^[0-9]{6}$"))
{
    Console.Error.WriteLine("Uso: dotnet run --project backend/tools/Intranet.ConnectionCheck -- usuario_gestion serie numero");
    return 2;
}
var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();
var repository = new AviosRepository(new GestionConnectionFactory(configuration), Options.Create(new GestionOptions()));
try
{
    using var cancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));
    var order = await repository.FindOrder(args[1], args[2], args[0], cancellation.Token);
    if (order is null) { Console.WriteLine("Sin resultados o sin permiso existente para esta OC."); return 3; }
    var items = await repository.Items(args[1], args[2], cancellation.Token);
    var item = items.FirstOrDefault();
    var requirements = item is null ? [] : await repository.Requirements(args[1], args[2], item.Sequence, cancellation.Token);
    Console.WriteLine($"OK · OC {order.Series}-{order.Number} · Clase {order.Class} · {items.Count} ítems · {requirements.Count} requerimientos en la primera secuencia.");
    return 0;
}
catch (Exception ex) { Console.Error.WriteLine($"Fallo de integración ({ex.GetType().Name}): {ex.Message}"); return 1; }
