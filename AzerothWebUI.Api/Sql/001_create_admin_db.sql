-- AzerothWebUI's own admin identity store. Intentionally a separate database from
-- acore_auth/acore_characters/acore_world/acore_playerbots — admin login is a distinct
-- trust domain from WoW account credentials (see CLAUDE.md).
CREATE DATABASE IF NOT EXISTS azerothwebui
    CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

USE azerothwebui;

CREATE TABLE IF NOT EXISTS AdminUsers (
    Id INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(64) NOT NULL UNIQUE,
    PasswordHash VARCHAR(512) NOT NULL,
    CreatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);
