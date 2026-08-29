# Locates the Hollow Knight Managed folder on this machine.
# Dot-source this, then call Get-ManagedFolder.
#
# Override with the HK_MANAGED environment variable if you keep the game somewhere
# a Steam library does not cover (GOG, Xbox, a manual copy).

function Get-ManagedFolder {
    $suffix = "steamapps\common\Hollow Knight\hollow_knight_Data\Managed"

    if ($env:HK_MANAGED) {
        if (Test-Path $env:HK_MANAGED) { return $env:HK_MANAGED }
        throw "HK_MANAGED is set but does not exist: $env:HK_MANAGED"
    }

    # Steam records its install root in the registry, and every extra library folder
    # (other drives) in libraryfolders.vdf under that root.
    $steam = $null
    foreach ($key in @("HKCU:\Software\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam")) {
        if (Test-Path $key) {
            $v = (Get-ItemProperty $key -ErrorAction SilentlyContinue)
            if ($v.SteamPath)    { $steam = $v.SteamPath.Replace('/', '\'); break }
            if ($v.InstallPath)  { $steam = $v.InstallPath; break }
        }
    }

    $roots = @()
    if ($steam) { $roots += $steam }

    $vdf = if ($steam) { Join-Path $steam "steamapps\libraryfolders.vdf" } else { $null }
    if ($vdf -and (Test-Path $vdf)) {
        Select-String -Path $vdf -Pattern '"path"\s+"(.+?)"' | ForEach-Object {
            $roots += $_.Matches[0].Groups[1].Value.Replace('\\', '\')
        }
    }

    # Last resort for machines where the registry lookup failed.
    $roots += @("C:\Program Files (x86)\Steam", "C:\Program Files\Steam")

    foreach ($root in ($roots | Select-Object -Unique)) {
        $candidate = Join-Path $root $suffix
        if (Test-Path $candidate) { return $candidate }
    }

    throw "Could not find the Hollow Knight Managed folder. Set HK_MANAGED to it, e.g.`n" +
          "  `$env:HK_MANAGED = 'D:\Steam\$suffix'"
}
