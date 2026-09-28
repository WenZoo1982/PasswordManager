using Microsoft.Data.Sqlite;
using System.IO;
using System.Security.Cryptography;

namespace PasswordManager
{
    public class Database
    {
        private readonly string _databasePath;

        public Database(string databasePath)
        {
            _databasePath = databasePath;
        }

        public void Initialize()
        {
            string? directory = Path.GetDirectoryName(_databasePath);

            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                CREATE TABLE IF NOT EXISTS VaultInfo
                (
                    Id INTEGER PRIMARY KEY CHECK (Id = 1),
                    PasswordHash BLOB NOT NULL,
                    PasswordSalt BLOB NOT NULL,
                    EncryptionSalt BLOB NOT NULL
                );

                CREATE TABLE IF NOT EXISTS PasswordEntries
                (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Url TEXT NOT NULL,
                    Username TEXT NOT NULL,
                    Password BLOB NOT NULL,
                    PasswordNonce BLOB NOT NULL,
                    PasswordTag BLOB NOT NULL,
                    Notes BLOB,
                    NotesNonce BLOB,
                    NotesTag BLOB
                );
                """;

            command.ExecuteNonQuery();
        }

        public void SavePasswordHash(
            byte[] passwordHash,
            byte[] passwordSalt)
        {
            byte[] encryptionSalt =
                RandomNumberGenerator.GetBytes(16);

            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO VaultInfo
                (
                    Id,
                    PasswordHash,
                    PasswordSalt,
                    EncryptionSalt
                )
                VALUES
                (
                    1,
                    $passwordHash,
                    $passwordSalt,
                    $encryptionSalt
                );
                """;

            command.Parameters.AddWithValue(
                "$passwordHash",
                passwordHash);

            command.Parameters.AddWithValue(
                "$passwordSalt",
                passwordSalt);

            command.Parameters.AddWithValue(
                "$encryptionSalt",
                encryptionSalt);

            command.ExecuteNonQuery();
        }

        public (byte[] PasswordHash, byte[] PasswordSalt)? GetPasswordInfo()
        {
            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                SELECT PasswordHash, PasswordSalt
                FROM VaultInfo
                WHERE Id = 1;
                """;

            using SqliteDataReader reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            byte[] passwordHash =
                (byte[])reader["PasswordHash"];

            byte[] passwordSalt =
                (byte[])reader["PasswordSalt"];

            return (passwordHash, passwordSalt);
        }

        public byte[]? GetEncryptionSalt()
        {
            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                SELECT EncryptionSalt
                FROM VaultInfo
                WHERE Id = 1;
                """;

            object? result = command.ExecuteScalar();

            if (result == null)
            {
                return null;
            }

            return (byte[])result;
        }

        public void AddPasswordEntry(
            string title,
            string url,
            string username,
            byte[] encryptedPassword,
            byte[] passwordNonce,
            byte[] passwordTag,
            byte[]? encryptedNotes,
            byte[]? notesNonce,
            byte[]? notesTag)
        {
            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                INSERT INTO PasswordEntries
                (
                    Title,
                    Url,
                    Username,
                    Password,
                    PasswordNonce,
                    PasswordTag,
                    Notes,
                    NotesNonce,
                    NotesTag
                )
                VALUES
                (
                    $title,
                    $url,
                    $username,
                    $password,
                    $passwordNonce,
                    $passwordTag,
                    $notes,
                    $notesNonce,
                    $notesTag
                );
                """;

            command.Parameters.AddWithValue(
                "$title",
                title);

            command.Parameters.AddWithValue(
                "$url",
                url);

            command.Parameters.AddWithValue(
                "$username",
                username);

            command.Parameters.AddWithValue(
                "$password",
                encryptedPassword);

            command.Parameters.AddWithValue(
                "$passwordNonce",
                passwordNonce);

            command.Parameters.AddWithValue(
                "$passwordTag",
                passwordTag);

            command.Parameters.AddWithValue(
                "$notes",
                encryptedNotes ?? (object)DBNull.Value);

            command.Parameters.AddWithValue(
                "$notesNonce",
                notesNonce ?? (object)DBNull.Value);

            command.Parameters.AddWithValue(
                "$notesTag",
                notesTag ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public List<PasswordEntry> GetPasswordEntries()
        {
            List<PasswordEntry> entries =
                new List<PasswordEntry>();

            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                SELECT
                    Id,
                    Title,
                    Url,
                    Username,
                    Password,
                    PasswordNonce,
                    PasswordTag,
                    Notes,
                    NotesNonce,
                    NotesTag
                FROM PasswordEntries
                ORDER BY Id DESC;
                """;

            using SqliteDataReader reader =
                command.ExecuteReader();

            while (reader.Read())
            {
                entries.Add(
                    new PasswordEntry
                    {
                        Id = reader.GetInt64(0),
                        Title = reader.GetString(1),
                        Url = reader.GetString(2),
                        Username = reader.GetString(3),
                        Password = (byte[])reader["Password"],
                        PasswordNonce = (byte[])reader["PasswordNonce"],
                        PasswordTag = (byte[])reader["PasswordTag"],
                        Notes = reader["Notes"] == DBNull.Value
                            ? null
                            : (byte[])reader["Notes"],
                        NotesNonce = reader["NotesNonce"] == DBNull.Value
                            ? null
                            : (byte[])reader["NotesNonce"],
                        NotesTag = reader["NotesTag"] == DBNull.Value
                            ? null
                            : (byte[])reader["NotesTag"]
                    });
            }

            return entries;
        }

        public void UpdatePasswordEntry(
            long id,
            string title,
            string url,
            string username,
            byte[] encryptedPassword,
            byte[] passwordNonce,
            byte[] passwordTag,
            byte[]? encryptedNotes,
            byte[]? notesNonce,
            byte[]? notesTag)
        {
            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                UPDATE PasswordEntries
                SET
                    Title = $title,
                    Url = $url,
                    Username = $username,
                    Password = $password,
                    PasswordNonce = $passwordNonce,
                    PasswordTag = $passwordTag,
                    Notes = $notes,
                    NotesNonce = $notesNonce,
                    NotesTag = $notesTag
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue(
                "$id",
                id);

            command.Parameters.AddWithValue(
                "$title",
                title);

            command.Parameters.AddWithValue(
                "$url",
                url);

            command.Parameters.AddWithValue(
                "$username",
                username);

            command.Parameters.AddWithValue(
                "$password",
                encryptedPassword);

            command.Parameters.AddWithValue(
                "$passwordNonce",
                passwordNonce);

            command.Parameters.AddWithValue(
                "$passwordTag",
                passwordTag);

            command.Parameters.AddWithValue(
                "$notes",
                encryptedNotes ?? (object)DBNull.Value);

            command.Parameters.AddWithValue(
                "$notesNonce",
                notesNonce ?? (object)DBNull.Value);

            command.Parameters.AddWithValue(
                "$notesTag",
                notesTag ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        public void DeletePasswordEntry(long id)
        {
            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteCommand command = connection.CreateCommand();

            command.CommandText = """
                DELETE FROM PasswordEntries
                WHERE Id = $id;
                """;

            command.Parameters.AddWithValue("$id", id);

            command.ExecuteNonQuery();
        }

        public void ChangeVaultSecurity(
            byte[] newPasswordHash,
            byte[] newPasswordSalt,
            byte[] newEncryptionSalt,
            List<PasswordEntry> reencryptedEntries)
        {
            using SqliteConnection connection = new SqliteConnection(
                $"Data Source={_databasePath}");

            connection.Open();

            using SqliteTransaction transaction =
                connection.BeginTransaction();

            try
            {
                using SqliteCommand vaultCommand =
                    connection.CreateCommand();

                vaultCommand.Transaction = transaction;

                vaultCommand.CommandText = """
                    UPDATE VaultInfo
                    SET
                        PasswordHash = $passwordHash,
                        PasswordSalt = $passwordSalt,
                        EncryptionSalt = $encryptionSalt
                    WHERE Id = 1;
                    """;

                vaultCommand.Parameters.AddWithValue(
                    "$passwordHash",
                    newPasswordHash);

                vaultCommand.Parameters.AddWithValue(
                    "$passwordSalt",
                    newPasswordSalt);

                vaultCommand.Parameters.AddWithValue(
                    "$encryptionSalt",
                    newEncryptionSalt);

                vaultCommand.ExecuteNonQuery();

                foreach (PasswordEntry entry in reencryptedEntries)
                {
                    using SqliteCommand entryCommand =
                        connection.CreateCommand();

                    entryCommand.Transaction = transaction;

                    entryCommand.CommandText = """
                        UPDATE PasswordEntries
                        SET
                            Password = $password,
                            PasswordNonce = $passwordNonce,
                            PasswordTag = $passwordTag,
                            Notes = $notes,
                            NotesNonce = $notesNonce,
                            NotesTag = $notesTag
                        WHERE Id = $id;
                        """;

                    entryCommand.Parameters.AddWithValue(
                        "$id",
                        entry.Id);

                    entryCommand.Parameters.AddWithValue(
                        "$password",
                        entry.Password);

                    entryCommand.Parameters.AddWithValue(
                        "$passwordNonce",
                        entry.PasswordNonce);

                    entryCommand.Parameters.AddWithValue(
                        "$passwordTag",
                        entry.PasswordTag);

                    entryCommand.Parameters.AddWithValue(
                        "$notes",
                        entry.Notes ?? (object)DBNull.Value);

                    entryCommand.Parameters.AddWithValue(
                        "$notesNonce",
                        entry.NotesNonce ?? (object)DBNull.Value);

                    entryCommand.Parameters.AddWithValue(
                        "$notesTag",
                        entry.NotesTag ?? (object)DBNull.Value);

                    entryCommand.ExecuteNonQuery();
                }

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}