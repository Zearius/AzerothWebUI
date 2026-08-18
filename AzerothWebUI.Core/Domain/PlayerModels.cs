namespace AzerothWebUI.Core.Domain;

public record PlayerLoginRequest(string Username, string Password);

public record AccountCredentials(int Id, byte[] Salt, byte[] Verifier, bool Banned);
