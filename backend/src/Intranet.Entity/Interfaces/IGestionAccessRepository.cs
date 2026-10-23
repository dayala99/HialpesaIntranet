namespace Intranet.Entity;
public interface IGestionAccessRepository {Task<GestionAccess> Read(string user,string company,CancellationToken ct);}
