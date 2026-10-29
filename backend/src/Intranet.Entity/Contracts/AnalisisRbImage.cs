namespace Intranet.Entity;
public sealed record AnalisisRbImage(byte[] Content,string ContentType);
public interface IAnalisisRbImageRepository { Task<AnalisisRbImage?> Read(string np,CancellationToken ct); Task<AnalisisRbImage?> ReadHistory(string np,long id,CancellationToken ct); }
