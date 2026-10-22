using Microsoft.Extensions.Hosting;
using Intranet.Entity;
namespace Intranet.Service;

public static class PoQuantitySource
{
    public static IReadOnlyList<object> Create(string po, IReadOnlyList<PoDetailLine> details)
    {
        if (details.Count == 0) throw new ArgumentException("La PO no tiene cantidades requeridas en Gestión.");
        static string Normalize(string value) => value.Trim().ToUpperInvariant();
        var rows = new List<object>();
        foreach (var group in details.GroupBy(r => new { Style = Normalize(r.Style), Color = Normalize(r.Color ?? r.ColorCode), Size = Normalize(r.Size.Split('/')[0]) }))
        {
            if (group.Key.Style.Length == 0 || group.Key.Color.Length == 0 || group.Key.Size.Length == 0 || group.Any(r => r.Required < 0)) throw new ArgumentException("La PO tiene estilo, color, talla o cantidad requerida inválidos.");
            var purchased = group.Sum(r => r.Purchased);
            var lots = string.Join(", ", group.Select(r => r.Lot).Distinct().Order());
            foreach (var item in new[] { "PT001-PRICE TICKET VINCEH501RFID", "ST001-STICKER VINH300" })
                rows.Add(new { item, combination = po + " COLOR: " + group.Key.Color, customerStyle = group.Key.Style, measure = group.Key.Size, productionOrders = lots, purchased });
        }
        return rows;
    }
}
