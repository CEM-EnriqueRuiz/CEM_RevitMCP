using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading;
using CEM_IAModeler.Utils;
using Microsoft.Win32.SafeHandles;

namespace CEM_IAModeler.Security
{
    /// <summary>
    /// The Revit socket's shared secret (decision 0022, SEC-5). The socket runs any C# through send_code_to_revit, and
    /// other local accounts (the Codex sandbox) can reach 127.0.0.1, so each listener start issues a fresh random token in
    /// <c>%LOCALAPPDATA%\Cemengal\RevitMCP\token</c>, readable only by the user and SYSTEM. The TS server, running as the
    /// user, reads it and sends it as the <see cref="Param"/> param of every request; <see cref="Matches"/> checks it.
    /// The token is never logged, traced or put in an error.
    /// </summary>
    public static class McpAuthToken
    {
        /// <summary>The JSON-RPC param that carries the token (server/src/utils/SocketClient.ts, AUTH_PARAM).</summary>
        public const string Param = "auth";

        private const int TokenBytes = 32;
        private const int MoveFileReplaceExisting = 0x1;
        private const int MoveFileWriteThrough = 0x8;
        private const int FileRenameInfoEx = 22;
        private const int FileRenameFlagReplaceIfExists = 0x1;
        private const int FileRenameFlagPosixSemantics = 0x2;
        private const uint AccessDelete = 0x00010000;
        private const uint AccessSynchronize = 0x00100000;
        private const uint ShareAll = 0x7;
        private const uint OpenExisting = 3;
        private const uint FlagOpenReparsePoint = 0x00200000;
        private const int ErrorInvalidFunction = 1;
        private const int ErrorAccessDenied = 5;
        private const int ErrorSharingViolation = 32;
        private const int ErrorNotSupported = 50;
        private const int ErrorInvalidParameter = 87;
        private const int PosixUnsupported = -1;

        // The waits between attempts while a reader that does not share delete holds the token: about 1.3 s in all.
        private static readonly int[] ReplaceBackoffMs = { 5, 10, 20, 40, 80, 160, 250, 250, 250, 250 };

        // The token of the running listener; null when it is stopped, which refuses every request.
        private static volatile string _current;

        /// <summary><c>%LOCALAPPDATA%\Cemengal\RevitMCP</c>: local and per user, never ACC, never pruned like %TEMP%.</summary>
        public static string Folder =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Cemengal", McpLocalFiles.App);

        public static string FilePath => Path.Combine(Folder, "token");

        /// <summary>
        /// Issues a new token and writes it, replacing the previous listener's. Throws when the file cannot be written
        /// safely; the listener must then not start.
        /// </summary>
        public static void Issue()
        {
            var bytes = new byte[TokenBytes];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            var hex = new StringBuilder(TokenBytes * 2);
            foreach (byte b in bytes) hex.Append(b.ToString("x2"));

            _current = null;
            Write(Folder, hex.ToString());
            _current = hex.ToString();
        }

        /// <summary>Forgets the token, and deletes its file when it still holds it (best effort).</summary>
        public static void Revoke()
        {
            string issued = _current;
            _current = null;
            if (issued == null) return;
            try
            {
                string file = FilePath;
                if (!McpLocalFiles.IsReparsePoint(file) && File.Exists(file) && FixedTimeEquals(File.ReadAllText(file).Trim(), issued))
                    File.Delete(file);
            }
            catch
            {
                // The next start replaces it.
            }
        }

        /// <summary>True when <paramref name="candidate"/> is the running listener's token, compared in constant time.</summary>
        public static bool Matches(string candidate) => FixedTimeEquals(candidate, _current);

        // net48 has no CryptographicOperations.FixedTimeEquals: every byte of the expected token is compared, whatever
        // the candidate holds.
        private static bool FixedTimeEquals(string candidate, string expected)
        {
            if (candidate == null || expected == null) return false;
            byte[] given = Encoding.UTF8.GetBytes(candidate);
            byte[] wanted = Encoding.UTF8.GetBytes(expected);
            int diff = given.Length ^ wanted.Length;
            for (int i = 0; i < wanted.Length; i++)
                diff |= wanted[i] ^ (i < given.Length ? given[i] : 0);
            return diff == 0;
        }

        // A new temp file in the protected folder, its ACL set before its content, then one move that replaces the old
        // token: a reader sees the old file or the new one, never a partial or an inherited ACL. A junction or link at a
        // folder or at the file (planted to send the token somewhere readable) is refused before and after. Issue passes
        // Folder; the folder is a parameter only so tests/plugin/runtime.test.ts can write into a scratch one.
        private static void Write(string folder, string token)
        {
            string parent = Path.GetDirectoryName(folder);
            string file = Path.Combine(folder, "token");
            string temp = Path.Combine(folder, "token." + Guid.NewGuid().ToString("N") + ".tmp");

            McpLocalFiles.RefuseReparsePoint(parent, folder, file);
            Directory.CreateDirectory(folder);
            McpLocalFiles.RefuseReparsePoint(parent, folder, file);
            RefuseForeignOwner(folder);
            new DirectoryInfo(folder).SetAccessControl(ProtectedFolder());

            try
            {
                // Created empty (it inherits the protected folder's ACL), protected by path (a write handle has no
                // WRITE_DAC), and only then given the token.
                using (new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { }
                new FileInfo(temp).SetAccessControl(ProtectedFile());
                using (var stream = new FileStream(temp, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    byte[] content = new UTF8Encoding(false).GetBytes(token);
                    stream.Write(content, 0, content.Length);
                    stream.Flush(true);
                }
                ReplaceWithRetry(temp, file);
            }
            finally
            {
                try { if (File.Exists(temp)) File.Delete(temp); } catch { /* an orphan temp file in our own folder */ }
            }

            McpLocalFiles.RefuseReparsePoint(parent, folder, file);
        }

        // Puts the temp file in place of the token in one rename, so a reader still sees the old file or the new one.
        // A reader holding the token used to make the plain replace fail with ERROR_ACCESS_DENIED, and the listener
        // then did not start (QA, 2026-11-02):
        // - POSIX semantics first (Windows 10 1709+, NTFS): the rename replaces the file even while a reader that shares
        //   delete holds it, as the TS client's readFileSync does; that reader keeps the old content.
        // - Where they are not supported, MoveFileEx(REPLACE_EXISTING | WRITE_THROUGH), as before.
        // - A reader that does not share delete (some scanners) still blocks either one for as long as it holds the
        //   file, so ERROR_ACCESS_DENIED and ERROR_SHARING_VIOLATION are retried within a bound (about 1.3 s); after
        //   that, or on any other error, the write fails and says why.
        private static void ReplaceWithRetry(string temp, string file)
        {
            for (int attempt = 0; ; attempt++)
            {
                int error = ReplacePosix(temp, file);
                if (error == PosixUnsupported)
                    error = MoveFileEx(temp, file, MoveFileReplaceExisting | MoveFileWriteThrough) ? 0 : Marshal.GetLastWin32Error();
                if (error == 0) return;
                if ((error != ErrorAccessDenied && error != ErrorSharingViolation) || attempt >= ReplaceBackoffMs.Length)
                    throw new IOException($"Could not put the MCP token file in place at {file} (Win32 error {error}).");
                Thread.Sleep(ReplaceBackoffMs[attempt]);
            }
        }

        // SetFileInformationByHandle(FileRenameInfoEx, REPLACE_IF_EXISTS | POSIX_SEMANTICS) on a handle to the temp file.
        // Returns 0, a Win32 error, or PosixUnsupported when this Windows or file system has no POSIX rename.
        private static int ReplacePosix(string temp, string file)
        {
            using (var handle = CreateFile(temp, AccessDelete | AccessSynchronize, ShareAll, IntPtr.Zero, OpenExisting,
                       FlagOpenReparsePoint, IntPtr.Zero))
            {
                if (handle.IsInvalid) return Marshal.GetLastWin32Error();

                // FILE_RENAME_INFO: Flags (padded to a pointer), RootDirectory, FileNameLength, then the full target path.
                byte[] name = Encoding.Unicode.GetBytes(file);
                int nameOffset = 2 * IntPtr.Size + 4;
                int size = nameOffset + name.Length + 2;
                IntPtr info = Marshal.AllocHGlobal(size);
                try
                {
                    for (int i = 0; i < size; i++) Marshal.WriteByte(info, i, 0);
                    Marshal.WriteInt32(info, 0, FileRenameFlagReplaceIfExists | FileRenameFlagPosixSemantics);
                    Marshal.WriteInt32(info, 2 * IntPtr.Size, name.Length);
                    Marshal.Copy(name, 0, info + nameOffset, name.Length);
                    if (SetFileInformationByHandle(handle, FileRenameInfoEx, info, size)) return 0;
                    int error = Marshal.GetLastWin32Error();
                    return error == ErrorInvalidFunction || error == ErrorNotSupported || error == ErrorInvalidParameter
                        ? PosixUnsupported
                        : error;
                }
                finally
                {
                    Marshal.FreeHGlobal(info);
                }
            }
        }

        // A folder another account created could always be re-ACL'd by it (an owner keeps that right), so it is refused.
        // The owner is the user, or SYSTEM or the Administrators group (what an elevated process creates).
        private static void RefuseForeignOwner(string folder)
        {
            var owner = new DirectoryInfo(folder).GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
            using (var user = WindowsIdentity.GetCurrent())
            {
                if (owner != null && (owner.Equals(user.User) || owner.IsWellKnown(WellKnownSidType.LocalSystemSid) ||
                                      owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid)))
                    return;
            }
            throw new UnauthorizedAccessException($"{folder} belongs to another account: delete it, then start the MCP service again.");
        }

        // Only the user and SYSTEM, nothing inherited from the profile (which grants Administrators).
        private static DirectorySecurity ProtectedFolder()
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(true, false);
            foreach (var sid in AllowedAccounts())
                security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl,
                    InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            return security;
        }

        private static FileSecurity ProtectedFile()
        {
            var security = new FileSecurity();
            security.SetAccessRuleProtection(true, false);
            foreach (var sid in AllowedAccounts())
                security.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, AccessControlType.Allow));
            return security;
        }

        private static SecurityIdentifier[] AllowedAccounts()
        {
            using (var user = WindowsIdentity.GetCurrent())
                return new[] { user.User, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null) };
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(string fileName, uint access, uint share, IntPtr security,
            uint disposition, uint flags, IntPtr template);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileInformationByHandle(SafeFileHandle file, int informationClass, IntPtr information,
            int size);
    }
}
