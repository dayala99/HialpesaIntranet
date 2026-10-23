namespace Intranet.Entity;
public sealed record GestionProfile(string Code,string Name);
public sealed record GestionOptionAccess(string Application,string Option);
public sealed record GestionAccess(IReadOnlyList<GestionProfile> Profiles,IReadOnlyList<GestionOptionAccess> Options);
public sealed record PortalAccess(bool IsGlobalUser,IReadOnlyList<string> AllowedOptions,IReadOnlyList<GestionProfile> Profiles);
