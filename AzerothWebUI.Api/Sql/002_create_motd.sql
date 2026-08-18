-- Single admin-editable Markdown announcement block shown on the public landing page.
-- Deliberately a singleton (one row, Id fixed at 1) rather than a notice board with multiple
-- posts - matches the config editor's "current value is the only value that matters" approach.
USE azerothwebui;

CREATE TABLE IF NOT EXISTS Motd (
    Id TINYINT UNSIGNED NOT NULL PRIMARY KEY,
    Content TEXT NOT NULL,
    UpdatedAt TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

INSERT IGNORE INTO Motd (Id, Content) VALUES (1, '');
