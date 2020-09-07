if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$ver = "v" + (Get-ChildItem -Path ($dir + "\BepInEx\") -Filter "KK_GamepadSupport.dll" -Recurse -Force)[0].VersionInfo.FileVersion.ToString()

Compress-Archive -Path ($dir + "\BepInEx\") -Force -CompressionLevel "Optimal" -DestinationPath ($dir + "KK_GamepadSupport_" + $ver + ".zip")
