namespace AzerothWebUI.Core.Domain;

public record AdminLoginRequest(string Username, string Password);

public record ServerStatus(string RawOutput);

public record AdminAccountSummary(int Id, string Username, string Email, byte GmLevel, bool Banned, bool Online);

public record AwardItemRequest(string CharacterName, int ItemId, int Count, string? Subject, string? Message);

public record MotdUpdateRequest(string Content);
