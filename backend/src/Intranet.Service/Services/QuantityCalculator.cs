using Microsoft.Extensions.Hosting;
using Intranet.Entity;

namespace Intranet.Service;

public sealed class QuantityCalculator
{
    public static decimal RoundRequiredQuantity(decimal required) => QuantityRules.RoundRequiredQuantity(required);
    public PreviewResponse Calculate(PreviewRequest request)
    {
        if (request.Rows.Count is < 1 or > 500 || request.Articles.Count is < 1 or > 10)
            throw new ArgumentException("Se requieren de 1 a 500 filas y de 1 a 10 artículos.");
        if (request.Articles.Any(a => string.IsNullOrWhiteSpace(a.ItemCode) || string.IsNullOrWhiteSpace(a.Description) || a.Multiple != 5))
            throw new ArgumentException("Revisa el código y descripción del artículo. El calculo usa multiplos de 5 y la regla de decimales acordada.");
        if (request.Articles.Select(a => a.ItemCode.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Articles.Count)
            throw new ArgumentException("No se puede repetir el código de un artículo.");
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var r in request.Rows)
        {
            if (string.IsNullOrWhiteSpace(r.ProductionOrder) || string.IsNullOrWhiteSpace(r.Style) || string.IsNullOrWhiteSpace(r.Color) || string.IsNullOrWhiteSpace(r.Size) || r.Quantity < 0 || r.Quantity > 100000000)
                throw new ArgumentException("Cada fila requiere NP, estilo, color, talla y una cantidad válida.");
            // Length-prefix encoding avoids collisions when source values contain separators.
            var key = string.Concat(new[] { r.Factory, r.ProductionOrder, r.Style, r.PresentationCode, r.Color, r.Size, r.Destination }.Select(v => $"{v.Trim().Length}:{v.Trim()}"));
            if (!keys.Add(key)) throw new ArgumentException("Hay filas duplicadas para la misma fábrica, NP, estilo, presentación, color, talla y destino.");
        }
        var calculated = request.Rows.SelectMany(r => request.Articles.Select(a =>
        {
            var purchase = RoundRequiredQuantity(r.Quantity);
            return new CalculatedRow(r, a.ItemCode.Trim(), a.Description.Trim(), a.Multiple, r.Quantity, purchase, purchase - r.Quantity);
        })).ToList();
        return new(calculated, request.Rows.Sum(r => r.Quantity), calculated.Sum(r => r.Purchase), calculated.Sum(r => r.Extra));
    }
}
