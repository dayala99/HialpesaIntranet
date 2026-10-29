using Intranet.Entity;
using Microsoft.Extensions.Hosting;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Diagnostics;
namespace Intranet.Service;

public sealed class ProviderService(GestionIdentity identity, IHostEnvironment environment, IAviosRepository orders, IRegistrationReadRepository reads, IConfiguration configuration, ProviderAgentTickets tickets, RequestActor actor) : ApplicationService(actor), IProviderService
{


    private async Task<string[]> PurchaseOrders(string series, string number, CancellationToken ct)
    {
        var order = await orders.FindOrder(series, number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (order is not { Class: "01" }) throw new UnauthorizedAccessException();
        var report = await reads.Print(order, Array.Empty<Intranet.Entity.OrderItem>(), ct);
        return report.ReportRows.SelectMany(row => Regex.Matches(row.Combination, @"\bPO\s*-\s*[0-9]+\b", RegexOptions.IgnoreCase).Select(m => Regex.Replace(m.Value, @"\s+", "").ToUpperInvariant())).Distinct().Order().ToArray();
    }

    public async Task<ServiceResult> Pos(string series, string number, CancellationToken ct) => Ok(await PurchaseOrders(series, number, ct));

    public async Task<ServiceResult> Prepare(ProviderSelection selection, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true || !Actor.ProviderRequested) throw new UnauthorizedAccessException();
        var order = await orders.FindOrder(selection.Series, selection.Number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (order is not { Class: "01" }) throw new UnauthorizedAccessException();
        var report = await reads.Print(order, Array.Empty<Intranet.Entity.OrderItem>(), ct);
        if (!report.ReportRows.Any(row => Regex.Matches(row.Combination, @"\bPO\s*-\s*[0-9]+\b", RegexOptions.IgnoreCase).Any(m => Regex.Replace(m.Value, @"\s+", "").Equals(selection.Po, StringComparison.OrdinalIgnoreCase)))) throw new ArgumentException("El PO seleccionado no pertenece a la OC.");
        var key = configuration["ProviderAgent:SigningPrivateKey"];
        if (string.IsNullOrWhiteSpace(key)) return BadRequest(new { title = "Falta configurar la firma del agente VINCE en el backend." });

        return File(ProviderAgentJob.Create(new { schema = "hialpesa-vince/1", issuedAt = DateTimeOffset.UtcNow, expiresAt = DateTimeOffset.UtcNow.AddMinutes(30), series = selection.Series, number = selection.Number, po = selection.Po, rows = report.ReportRows }, key), "application/octet-stream", selection.Po + ".hialpesa-vince");
    }


    public async Task<ServiceResult> PreparePoLaunch(ProviderPoSelection selection, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true || !Actor.ProviderRequested) throw new UnauthorizedAccessException();
        identity.GetUser(User);
        if (!await orders.PoExists(selection.Po, selection.ClientCode, ct)) return NotFound(new { title = "La PO no existe para el cliente seleccionado." });
        var key = configuration["ProviderAgent:SigningPrivateKey"];
        if (string.IsNullOrWhiteSpace(key)) return BadRequest(new { title = "Falta configurar la firma del agente VINCE." });
        var details = await orders.PoDetails(selection.Po, selection.ClientCode, ct);
        var rows = Intranet.Service.PoQuantitySource.Create(selection.Po, details);
        var data = ProviderAgentJob.Create(new { schema = "hialpesa-vince/1", issuedAt = DateTimeOffset.UtcNow, expiresAt = DateTimeOffset.UtcNow.AddMinutes(30), mode = "po-required", po = selection.Po, clientCode = selection.ClientCode, rows }, key);
        var ticket = tickets.Issue(data);
        return Ok(new { token = ticket.Token, expiresAt = ticket.ExpiresAt });
    }


    public async Task<ServiceResult> PrepareLaunch(ProviderSelection selection, CancellationToken ct)
    {
        var prepared = await Prepare(selection, ct);
        if (prepared.Content is not { } bytes) return prepared;
        var ticket = tickets.Issue(bytes);
        return Ok(new { token = ticket.Token, expiresAt = ticket.ExpiresAt });
    }

    public ServiceResult Redeem(ProviderAgentTicket ticket)
    {

        if (!Actor.AgentRequested) return BadRequest();
        var data = tickets.Redeem(ticket.Token);
        return data is null ? StatusCode(410, new { title = "La apertura venció o ya fue utilizada. Selecciona nuevamente el PO en la intranet." }) : File(data, "application/json");
    }

    public ServiceResult LaunchStatus(ProviderAgentTicket ticket)
    {
        if (User.Identity?.IsAuthenticated != true || !Actor.ProviderRequested) throw new UnauthorizedAccessException();
        identity.GetUser(User);
        return Ok(new { received = tickets.WasReceived(ticket.Token) });
    }
    private sealed record ProviderSecret(string Username, string Password, string LoginUrl);
    private ServiceResult? Confirmation(string? response)
    {
        try
        {
            using var json = JsonDocument.Parse(response ?? "{}");
            if (json.RootElement.TryGetProperty("status", out var status) && status.GetString() == "confirmation-required" && json.RootElement.TryGetProperty("confirmations", out var values) && values.ValueKind == JsonValueKind.Array)
                return Ok(new { status = "confirmation-required", message = "Confirma la similitud de colores. No se cambiaron cantidades.", confirmations = values.Clone() });
        }
        catch (JsonException) { }
        return null;
    }
    private static string Failure(string? response)
    {
        string? stage = null; string? detail = null;
        try { using var json = JsonDocument.Parse(response ?? "{}"); if (json.RootElement.TryGetProperty("stage", out var value)) stage = value.GetString(); if (json.RootElement.TryGetProperty("detail", out var mismatch) && mismatch.ValueKind == JsonValueKind.String) detail = mismatch.GetString(); } catch (JsonException) { }
        if (stage == "quantity-matching" && !string.IsNullOrWhiteSpace(detail)) return "No se cambiaron cantidades. " + detail[..Math.Min(detail.Length, 2000)];
        return stage switch
        {
            "login-rejected" => "El portal mantiene el formulario de acceso. Revisa si muestra un rechazo o una verificacion adicional; no se repetira el intento automaticamente.",
            "login" => "El portal no confirmó el inicio de sesión. El acceso está guardado; revisa si solicita verificación adicional.",
            "product-ordering" or "vince" => "La sesión inició, pero no se pudo abrir PRODUCT ORDERING > VINCE.",
            "session" => "No se pudo preparar la pantalla de búsqueda del proveedor. La pestaña anterior se conserva.",
            "quantity-matching" => "No se completaron cantidades: existen filas sin coincidencia única por PO, artículo, estilo, color y talla.",
            "quantity-fill" => "El llenado de cantidades no terminó. Revisa los campos del proveedor antes de continuar; no se guardó ni envió el pedido.",
            "po-field" => "No se pudo identificar o completar PO Number en el portal.",
            "search" => "No se pudo ejecutar Search en el proveedor.",
            "saved-result" => "No se encontró un resultado Saved para el PO seleccionado o la búsqueda no terminó a tiempo.",
            "opening-saved" or "loading-saved" => "El resultado Saved se seleccionó, pero su pantalla no terminó de cargar. Revisa el navegador del proveedor.",
            _ => "No se pudo completar la automatización del proveedor. Revisa el navegador y vuelve a intentarlo."
        };
    }
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Process? browserProcess;

    public async Task<ServiceResult> Login(ProviderSelection selection, CancellationToken ct)
    {
        if (!environment.IsDevelopment() || !Actor.IsLocal || User.Identity?.IsAuthenticated != true || !Actor.ProviderRequested) throw new UnauthorizedAccessException();
        identity.GetUser(User);
        var selectedOrder = await orders.FindOrder(selection.Series, selection.Number, identity.GetUser(User), ct, identity.GetReadAllWindowsUser(User));
        if (selectedOrder is not { Class: "01" }) throw new UnauthorizedAccessException();
        var report = await reads.Print(selectedOrder, Array.Empty<Intranet.Entity.OrderItem>(), ct);
        var payload = JsonSerializer.Serialize(new { po = selection.Po, series = selection.Series, number = selection.Number, rows = report.ReportRows, colorConfirmations = selection.ColorConfirmations }, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        if (!report.ReportRows.Any(row => Regex.Matches(row.Combination, @"\bPO\s*-\s*[0-9]+\b", RegexOptions.IgnoreCase).Any(m => Regex.Replace(m.Value, @"\s+", "").Equals(selection.Po, StringComparison.OrdinalIgnoreCase)))) throw new ArgumentException("El PO seleccionado no pertenece a la OC.");
        await Gate.WaitAsync(ct);
        try
        {
            if (browserProcess is { HasExited: false })
            {
                var workerTools=Path.GetFullPath(Path.Combine(environment.ContentRootPath,"../../../frontend/tools"));
                var workerStarted=browserProcess.StartTime.ToUniversalTime();
                if(new[]{"provider-login.cjs","provider-remove-zero-skus.cjs","provider-quantities.cjs","provider-order-setup.cjs","provider-color-dialog.cjs"}.Select(file=>Path.Combine(workerTools,file)).Any(file=>System.IO.File.Exists(file)&&System.IO.File.GetLastWriteTimeUtc(file)>workerStarted))
                    return BadRequest(new { title="La ventana automatizada usa una versión anterior. Cierra esa ventana de Avery/VINCE y vuelve a abrir el PO desde la intranet para cargar la actualización." });
                string? reply;
                try
                {
                    await browserProcess.StandardInput.WriteLineAsync(payload);
                    await browserProcess.StandardInput.FlushAsync();
                    reply = await ProviderBrowserReply.ReadAsync(browserProcess.StandardOutput, ct);
                }
                catch
                {
                    if (!browserProcess.HasExited) browserProcess.Kill(true);
                    browserProcess = null;
                    throw new ArgumentException("Se interrumpio la comunicacion con el navegador. Vuelve a intentarlo desde la intranet.");
                }
                if (reply?.Contains("\"cancelled\"") == true) return Ok(new { status="cancelled",message="Llenado cancelado en la ventana de Avery/VINCE. No se cambiaron cantidades." });
                if (Confirmation(reply) is { } pending) return pending;
                if (reply?.Contains("po-filled") == true) return Ok(new { status = "po-filled", message = "Cantidades de RFID y UPC Stickers completadas para " + selection.Po + ". No se ha pulsado Save Order ni se ha enviado el pedido." });
                throw new ArgumentException(Failure(reply));
            }
            if (!OperatingSystem.IsWindows()) throw new ArgumentException("El acceso local requiere Windows.");
            var secretPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "Hialpesa", "Intranet", "avery.protected");
            if (!System.IO.File.Exists(secretPath)) return BadRequest(new { title = "Configura el acceso cifrado del proveedor para este usuario Windows." });
            var encrypted = await System.IO.File.ReadAllBytesAsync(secretPath, ct);
            var clear = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            ProviderSecret credentials;
            try { credentials = JsonSerializer.Deserialize<ProviderSecret>(clear) ?? throw new InvalidOperationException("Configuración de proveedor no válida."); } finally { CryptographicOperations.ZeroMemory(clear); }
            var username = credentials.Username; var password = credentials.Password;
            var loginUrl = new Uri(credentials.LoginUrl);
            if (loginUrl.Scheme != "https" || loginUrl.Host != "webservices.averydennison.com" || loginUrl.AbsolutePath != "/specstar/login.aspx") throw new ArgumentException("La URL configurada no es el acceso autorizado del proveedor.");
            var root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "../../.."));
            var start = new ProcessStartInfo("node") { WorkingDirectory = Path.Combine(root, "frontend"), UseShellExecute = false, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            start.ArgumentList.Add(Path.Combine(root, "frontend", "tools", "provider-login.cjs"));
            start.Environment["AVERY_COLOR_CONFIRMATION_UI"] = "browser";
            start.Environment["AVERY_LOGIN_URL"] = loginUrl.AbsoluteUri;
            start.Environment["AVERY_USERNAME"] = username; start.Environment["AVERY_PASSWORD"] = password;
            var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar el navegador.");
            browserProcess = process;
            await process.StandardInput.WriteLineAsync(payload);
            await process.StandardInput.FlushAsync();
            _ = process.StandardError.ReadToEndAsync();
            string? result;
            try { result = await ProviderBrowserReply.ReadAsync(process.StandardOutput, ct); }
            catch { if (!process.HasExited) process.Kill(true); browserProcess = null; throw new ArgumentException("La automatizacion excedio el tiempo de espera o se interrumpio. Vuelve a intentarlo desde la intranet."); }
            if (result?.Contains("\"cancelled\"") == true) return Ok(new { status="cancelled",message="Llenado cancelado en la ventana de Avery/VINCE. No se cambiaron cantidades." });
            if (Confirmation(result) is { } pendingInitial) return pendingInitial;
            if (result?.Contains("signed-in") != true) { if (process.HasExited) browserProcess = null; return BadRequest(new { title = Failure(result) }); }
            return Ok(new { status = "signed-in", message = "VINCE: cantidades de RFID y UPC Stickers completadas para " + selection.Po + ". No se ha pulsado Save Order ni se ha enviado el pedido." });
        }
        finally { Gate.Release(); }
    }
}
