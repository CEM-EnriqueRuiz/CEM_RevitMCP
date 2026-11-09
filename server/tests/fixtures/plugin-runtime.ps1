# Runtime checks of two plugin classes that never touch Revit (decision 0022), for tests/plugin/runtime.test.ts:
# the Logger's escaping of caller text, and McpAuthToken's write while other handles keep reading the file. Loads the
# built net48 DLL into Windows PowerShell and prints one JSON object. The tokens are random and never printed.
param(
  [Parameter(Mandatory = $true)][string]$Dll,
  [Parameter(Mandatory = $true)][string]$Folder,
  [Parameter(Mandatory = $true)][string]$StopFile,
  [int]$Issues = 200
)
$ErrorActionPreference = 'Stop'
$asm = [Reflection.Assembly]::LoadFrom($Dll)
$static = [Reflection.BindingFlags]'NonPublic, Public, Static'
$result = [ordered]@{}

# --- Logger's argument escaping (CEM_CSharpExtended's EscapeForLog under the Logger's own EscapeArgs policy): what a
# caller sends must not become a new line, or a terminal control sequence, in mcp-*.log.
$result.logTextGone = -not [bool]$asm.GetType('CEM_IAModeler.Utils.LogText')
$logger = $asm.GetType('CEM_IAModeler.Utils.Logger')
$escapeArgs = if ($logger) { $logger.GetMethod('EscapeArgs', $static, $null, [type[]]@([object[]]), $null) }
$result.loggerEscape = [bool]$escapeArgs
if ($escapeArgs) {
  # One string argument through the Logger's policy; the result is that argument's escaped text.
  $one = { param($value) $escapeArgs.Invoke($null, @(, [object[]]@($value)))[0] }
  $nasty = "a`r`nb`tc" + [char]0x0 + [char]0x1B + [char]0x7F + [char]0x85 + [char]0x9B + [char]0x2028 + [char]0x2029 + 'd'
  $once = & $one $nasty
  $result.escaped = [int[]][char[]]$once
  $result.idempotent = ($once -ceq (& $one $once))
  # Built from char codes: Windows PowerShell reads a script without a BOM in the ANSI code page.
  $plain = 'send_code_to_revit Revisi' + [char]0xF3 + 'n ' + [char]0xF1 + ' ' + [char]0x20AC + ' \u000A'
  $result.plain = [int[]][char[]](& $one $plain)
  $result.plainIn = [int[]][char[]]$plain
  # A null argument stays null (string.Format prints it as empty); an object whose text is null escapes to "".
  Add-Type -TypeDefinition 'public sealed class NullText { public override string ToString() { return null; } }'
  $mixed = $escapeArgs.Invoke($null, @(, [object[]]@($null, [NullText]::new(), [char]0x1B)))
  $result.nullArgIsNull = ($null -eq $mixed[0])
  $result.nullTextEscaped = $mixed[1]
  $result.charEscaped = [string]$mixed[2]
  $result.nullArgs = ($null -eq $escapeArgs.Invoke($null, @(, $null)))
  $formatArgs = $escapeArgs.Invoke($null, @(, [object[]]@("x`ny", 1.5, [DateTime]::new(2026, 11, 9))))
  $result.formattable = ($formatArgs[1] -is [double]) -and ($formatArgs[2] -is [DateTime])
  $result.formatted = [string]::Format([Globalization.CultureInfo]::InvariantCulture, '{0}|{1:N1}|{2:yyyy-MM-dd}', $formatArgs)
}

# --- McpAuthToken.Write(folder, token) under two readers:
# - the TS client's readFileSync in a tight loop, in a Node process (it shares delete): the POSIX rename's case;
# - here, a scanner-like .NET reader that does NOT share delete (FileShare.Read), holding each open for about 2 ms
#   every 10 ms: the bounded retry's case. One that never lets go cannot be outwaited; the write then fails clearly.
Add-Type -TypeDefinition @"
public static class TokenReader
{
    public static volatile bool Stop;
    public static int Reads;
    public static void Start(string path)
    {
        var thread = new System.Threading.Thread(() =>
        {
            var buffer = new byte[256];
            while (!Stop)
            {
                try
                {
                    using (var s = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read))
                    {
                        s.Read(buffer, 0, buffer.Length);
                        System.Threading.Thread.Sleep(2);
                    }
                    Reads++;
                }
                catch (System.IO.IOException) { }
                catch (System.UnauthorizedAccessException) { }
                System.Threading.Thread.Sleep(10);
            }
        });
        thread.IsBackground = true;
        thread.Start();
    }
}
"@
$tokenType = $asm.GetType('CEM_IAModeler.Security.McpAuthToken')
$write = $tokenType.GetMethod('Write', $static, $null, [type[]]@([string], [string]), $null)
$result.writeSeam = [bool]$write
$file = Join-Path $Folder 'token'
$failures = 0
$firstError = $null
$last = $null
$rng = [Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 32
$clock = [Diagnostics.Stopwatch]::StartNew()
if ($write) {
  [TokenReader]::Start($file)
  for ($i = 0; $i -lt $Issues; $i++) {
    $rng.GetBytes($bytes)
    $hex = -join ($bytes | ForEach-Object { $_.ToString('x2') })
    try { $write.Invoke($null, @($Folder, $hex)) | Out-Null; $last = $hex }
    catch { $failures++; if (-not $firstError) { $firstError = $_.Exception.InnerException.Message } }
  }
  [TokenReader]::Stop = $true
}
Set-Content -Path $StopFile -Value 'stop'
Start-Sleep -Milliseconds 100
$result.ms = $clock.ElapsedMilliseconds
$result.issues = $Issues
$result.failures = $failures
$result.firstError = $firstError
$result.dotnetReads = [TokenReader]::Reads
if (Test-Path $file) {
  $result.lastMatches = ([IO.File]::ReadAllText($file) -ceq $last)
  $acl = Get-Acl $file
  $result.protected = $acl.AreAccessRulesProtected
  $result.identities = @($acl.Access | ForEach-Object { $_.IdentityReference.Translate([Security.Principal.SecurityIdentifier]).Value })
}
$result.entries = @(Get-ChildItem $Folder -Force -ErrorAction SilentlyContinue | ForEach-Object { $_.Name })
$result.user = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
$result | ConvertTo-Json -Compress -Depth 4
