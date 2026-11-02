namespace Intranet.Entity;
public sealed record PcpMailDispatch(bool Processed,bool Queued,string? Error);
public interface IPcpMailRepository{Task<PcpMailDispatch> Dispatch(CancellationToken ct);}
