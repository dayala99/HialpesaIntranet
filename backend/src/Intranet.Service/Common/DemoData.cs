using Microsoft.Extensions.Hosting;
using Intranet.Entity;

namespace Intranet.Service;

public static class DemoData
{
    public static OrderDetail Order => new(new("002", "000001", "Proveedor de ejemplo", "P", "01", "Cliente de ejemplo", new DateTime(2026, 10, 16)), [
        new("001", "AV-DEMO-01", "Etiqueta de identificación", "S", "EST-001", 21, 21, 0),
        new("002", "AV-DEMO-02", "Sticker de empaque", "S", "EST-001", 21, 21, 0)
    ]);
    public static IReadOnlyList<Requirement> Requirements => [new("001", "NP-DEMO", "1", "Negro", "ET", "AV-DEMO-01", "EST-001", "", "S", "", 21)];
}
