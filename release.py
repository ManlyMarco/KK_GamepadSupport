import os
import shutil
import zipfile
from pathlib import Path
import subprocess

def get_version():
    try:
        result = subprocess.run(["git", "describe", "--tags", "--abbrev=0"], capture_output=True, text=True, check=True)
        version = result.stdout.strip()
        parts = version.lstrip('v').split('.')
        while len(parts) < 4:
            parts.append('0')
        formatted_version = 'v' + '.'.join(parts)
        return formatted_version
    except subprocess.CalledProcessError:
        raise RuntimeError("Failed to get the latest tag from the commit history.")

def compress_directory(src_dir, dest_file, exclude_files):
    with zipfile.ZipFile(dest_file, 'w', zipfile.ZIP_DEFLATED) as zipf:
        for root, _, files in os.walk(src_dir):
            for file in files:
                if file in exclude_files:
                    continue
                file_path = os.path.join(root, file)
                arcname = os.path.relpath(file_path, src_dir)
                zipf.write(file_path, arcname)

def main():
    script_root = Path(__file__).resolve().parent
    dir_path = script_root if '\\bin\\' in str(script_root) else script_root / 'bin'
    dir_path = dir_path.resolve()

    copy_dir = dir_path / 'copy' / 'BepInEx' / 'plugins' / 'GamepadSupport'

    for prefix in ['KK', 'KKS']:
        plugin_dir = dir_path / prefix
        dll_file = next(plugin_dir.rglob(f"{prefix}_GamepadSupport.dll"), None)

        if dll_file is None:
            print(f"DLL not found for prefix {prefix}, skipping.")
            continue

        version = get_version().lstrip('v')
        zip_file = dir_path / f"{prefix}_GamepadSupport_v{version}.zip"

        # Cleanup and prepare the copy directory
        if copy_dir.parent.parent.exists():
            shutil.rmtree(copy_dir.parent.parent)
        copy_dir.mkdir(parents=True, exist_ok=True)

        # Ensure the root BepInEx folder is created
        (copy_dir.parent.parent).mkdir(parents=True, exist_ok=True)

        # Copy files
        for item in plugin_dir.glob('**/*'):
            dest_path = copy_dir / item.relative_to(plugin_dir)
            if item.is_dir():
                dest_path.mkdir(parents=True, exist_ok=True)
            else:
                shutil.copy2(item, dest_path)

        # Include the additional file in the same folder as the KK DLL
        additional_file_src = script_root / 'lib' / 'XInputDotNet-x64-v2017.04-2' / 'XInputInterface.lib'
        additional_file_dest = copy_dir / 'XInputInterface.lib'
        shutil.copy2(additional_file_src, additional_file_dest)

        # Compress the directory including the root BepInEx folder
        exclude_files = {'BepInEx.dll', 'ExtensibleSaveFormat.dll', 'KK_GamepadSupport.pdb', 'KKAPI.dll'}
        compress_directory(copy_dir.parent.parent.parent, zip_file, exclude_files)

if __name__ == "__main__":
    main()
