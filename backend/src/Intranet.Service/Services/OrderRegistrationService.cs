using Microsoft.Extensions.Hosting;
using Intranet.Entity;

namespace Intranet.Service;

public sealed class OrderRegistrationService(IOrderWriteGateway gateway)
{
    public async Task RemoveRequirements(OrderReference order, string sequence, IReadOnlyList<string> keys, string gestionUser, string windowsUser, CancellationToken ct)
    {
        if (keys.Count is < 1 or > 500 || keys.Distinct().Count() != keys.Count) throw new ArgumentException("Selecciona requerimientos sin duplicados.");
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct); var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Retira requerimientos únicamente de una OC de avíos Planeada.");
        await session.RemoveRequirements(order, sequence, keys, ct); await session.Commit(ct);
    }
    public async Task ChangeItem(OrderReference order, string sequence, UpdateOrderItemRequest? request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct); var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Modifica detalles únicamente en OC de avíos Planeadas.");
        await session.ChangeItem(order, sequence, request, ct); await session.Commit(ct);
    }
    public async Task Update(OrderReference order, UpdateOrderRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Edita únicamente OC de avíos Planeadas.");
        await session.UpdateHeader(order, request, ct); await session.Commit(ct);
    }
    public Task AddGroups(OrderReference order, IReadOnlyList<string> groups, string gestionUser, string windowsUser, CancellationToken ct) => ChangeGroups(order, groups, Array.Empty<string>(), gestionUser, windowsUser, ct);
    public async Task ChangeGroups(OrderReference order, IReadOnlyList<string> groups, IReadOnlyList<string> removed, string gestionUser, string windowsUser, CancellationToken ct)
    {
        if (groups.Count + removed.Count is < 1 or > 100 || groups.Concat(removed).Any(g => string.IsNullOrWhiteSpace(g) || g.Trim().Length > 8)) throw new ArgumentException("Selecciona grupos válidos.");
        var added = groups.Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var deleted = removed.Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (added.Intersect(deleted, StringComparer.OrdinalIgnoreCase).Any()) throw new ArgumentException("Un grupo no puede añadirse y eliminarse a la vez.");
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var header = await session.LockOrder(order, ct);
        if (header.ClassCode != "01" || header.Status != "P") throw new RegistrationConflictException("Modifica grupos únicamente en una OC de avíos Planeada.");
        foreach (var group in deleted) await session.RemoveGroup(order, group, ct);
        foreach (var group in added) await session.LinkGroup(order, group, ct);
        await session.Commit(ct);
    }
    public async Task<OrderReference> Create(CreateOrderRequest request, string gestionUser, string windowsUser, CancellationToken ct)
    {
        await using var session = await gateway.Begin(gestionUser, windowsUser, ct);
        var order = await session.CreateHeader(request, ct);
        if (string.IsNullOrWhiteSpace(order.Series) || string.IsNullOrWhiteSpace(order.Number))
            throw new RegistrationConflictException("Gestión no devolvió el número de OC; la operación se revirtió.");
        foreach (var group in new[] { request.ManufacturingGroup }.Concat(request.AdditionalManufacturingGroups).Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            await session.LinkGroup(order, group, ct);
        await session.Commit(ct);
        return order;
    }
    public Task<DetailsSavedResponse> Add(OrderReference order, AddOrderDetailsRequest request, string gestionUser, string windowsUser, CancellationToken ct)
        =>AddBatch(order,new AddOrderDetailsBatchRequest{Lines=[request]},gestionUser,windowsUser,ct);
    public async Task<DetailsSavedResponse> AddBatch(OrderReference order,AddOrderDetailsBatchRequest request,string gestionUser,string windowsUser,CancellationToken ct)
    {
        if(request.Lines is null||request.Lines.Count is <1 or >500||request.Lines.Any(x=>x is null||x.UnitPrice is null or <0 or >99999999||x.RequirementKeys is null||x.RequirementKeys.Count is <1 or >500))
            throw new ArgumentException("Selecciona requerimientos e ingresa un precio válido para cada ítem.");
        foreach(var line in request.Lines){line.ProductionOrder=(line.ProductionOrder??"").Trim().ToUpperInvariant();line.ItemCode=(line.ItemCode??"").Trim().ToUpperInvariant();
            if(!System.Text.RegularExpressions.Regex.IsMatch(line.ProductionOrder,"^[A-Z0-9]{5}$")||!System.Text.RegularExpressions.Regex.IsMatch(line.ItemCode,"^[A-Z0-9]{8}$"))throw new ArgumentException("Revisa las NP y los códigos de ítem del lote.");}
        var keys=request.Lines.SelectMany(x=>x.RequirementKeys).ToList();
        if(keys.Count>500||keys.Any(string.IsNullOrWhiteSpace)||keys.Distinct(StringComparer.Ordinal).Count()!=keys.Count||request.Lines.Select(x=>(x.ProductionOrder,x.ItemCode)).Distinct().Count()!=request.Lines.Count)
            throw new ArgumentException("Selecciona hasta 500 requerimientos sin duplicados.");
        if(request.Lines.GroupBy(x=>x.ItemCode).Any(g=>g.Select(x=>x.UnitPrice).Distinct().Count()!=1))throw new ArgumentException("Cada ítem debe tener un único precio unitario en el lote.");
        await using var session=await gateway.Begin(gestionUser,windowsUser,ct);
        var header=await session.LockOrder(order,ct);
        if(header.ClassCode!="01"||header.Status!="P")throw new RegistrationConflictException("Solo se puede agregar detalle a una OC de avíos en estado Planeada.");
        // Acquire demand locks in a stable order; validate the entire selection before writing.
        var selected=new List<PendingRequirement>();
        foreach(var line in request.Lines.OrderBy(x=>x.ProductionOrder,StringComparer.Ordinal).ThenBy(x=>x.ItemCode,StringComparer.Ordinal)){
            var pending=await session.Pending(header,line.ProductionOrder,line.ItemCode,ct);
            foreach(var key in line.RequirementKeys){var matches=pending.Where(x=>x.Key==key).ToList();
                if(matches.Count!=1||matches[0].Required<=0||matches[0].ProductionOrder!=line.ProductionOrder||matches[0].ItemCode!=line.ItemCode)
                    throw new RegistrationConflictException("Los saldos cambiaron o la selección ya fue incorporada. Vuelve a consultar los requerimientos del lote.");
                selected.Add(matches[0]);
            }
        }
        var before=await session.Items(order,ct);var created=new List<SavedItem>();
        // Keep all NPs for the same item together so Gestión preserves its existing grouping rules.
        foreach(var group in selected.GroupBy(x=>x.ItemCode).OrderBy(x=>x.Key,StringComparer.Ordinal)){
            var previous=before.Select(x=>x.Sequence).Order(StringComparer.Ordinal).LastOrDefault()??"000";
            foreach(var row in group)await session.InsertRequirement(order,previous,row,ct);
            var after=await session.Items(order,ct);var prior=before.Select(x=>x.Sequence).ToHashSet(StringComparer.Ordinal);
            var added=after.Where(x=>!prior.Contains(x.Sequence)).ToList();
            if(added.Count==0)throw new RegistrationConflictException("No se generaron secuencias nuevas. Se revirtió el lote completo.");
            var price=request.Lines.First(x=>x.ItemCode==group.Key).UnitPrice!.Value;
            foreach(var item in added)await session.SetPurchase(order,item.Sequence,QuantityCalculator.RoundRequiredQuantity(item.Required),price,ct);
            created.AddRange(added);before=after;
        }
        await session.Commit(ct);
        return new(order.Series,order.Number,selected.Count,created.Count,created.Sum(x=>x.Required),created.Sum(x=>QuantityCalculator.RoundRequiredQuantity(x.Required)));
    }
}
