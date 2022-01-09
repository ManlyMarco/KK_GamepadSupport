if ($PSScriptRoot -match '.+?\\bin\\?') {
    $dir = $PSScriptRoot + "\"
}
else {
    $dir = $PSScriptRoot + "\bin\"
}

$copy = $dir + "\copy\BepInEx\plugins\GamepadSupport" 

foreach ($prefix in 'KK', 'KKS') 
{
    $ver = "v" + (Get-ChildItem -Path ($dir + "\"+$prefix+"\") -Filter ($prefix + "_GamepadSupport.dll") -Recurse -Force)[0].VersionInfo.FileVersion.ToString() -replace "^([\d+\.]+?\d+)[\.0]*$", '${1}'

    Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue
    New-Item -ItemType Directory -Force -Path $copy

    Copy-Item -Path ($dir + "\"+$prefix+"\*") -Destination $copy -Recurse -Force

    Compress-Archive -Path ($dir + "\copy\BepInEx") -Force -CompressionLevel "Optimal" -DestinationPath ($dir + $prefix+"_GamepadSupport_" + $ver + ".zip")
}

Remove-Item -Force -Path ($dir + "\copy") -Recurse -ErrorAction SilentlyContinue