namespace Intranet.Entity;
public sealed record AnalisisRbType(string Code,string Description);
public sealed record AnalisisRbFilterPage(IReadOnlyList<AnalisisRbRow> Rows,bool HasMore,string? NextCursor);
