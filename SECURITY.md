# Security Policy

## Supported Versions

This project doesn't yet publish tagged releases — only the latest commit on `master` is
supported. Always run the most recent `master` when self-hosting.

## Reporting a Vulnerability

Please report security vulnerabilities privately using GitHub's
[private vulnerability reporting](../../security/advisories/new) (Security tab → "Report a
vulnerability") rather than opening a public issue.

This is a self-hosted admin tool for a private game server, so please pay particular attention to
reports involving:

- Anything that lets a public/registration or player-login request reach admin-only endpoints or
  the AzerothCore SOAP command interface — admin auth, player auth, and WoW account credentials
  are meant to be three separate trust domains that never substitute for one another
- SQL injection in any of the direct MySQL access in `AzerothWebUI.Core/Data`
- Authentication/session handling issues in either cookie scheme (`Auth/AdminAuthService.cs`,
  `Auth/PlayerAuthService.cs`)
- Credential or secret exposure (SOAP password, database connection strings, admin/player
  session cookies)

I'll acknowledge reports as soon as I can and aim to have a fix or mitigation out promptly for
anything confirmed — this is a small, actively-developed solo project rather than a team with a
formal SLA, so please be patient, but reports won't be ignored.

If you're not sure whether something is a security issue or just a bug, err on the side of
reporting it privately first.
