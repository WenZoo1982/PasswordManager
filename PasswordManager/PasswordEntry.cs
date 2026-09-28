namespace PasswordManager
{
    public class PasswordEntry
    {
        public long Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public byte[] Password { get; set; } = [];

        public byte[] PasswordNonce { get; set; } = [];

        public byte[] PasswordTag { get; set; } = [];

        public byte[]? Notes { get; set; }

        public byte[]? NotesNonce { get; set; }

        public byte[]? NotesTag { get; set; }
    }
}