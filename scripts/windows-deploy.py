#!/usr/bin/env python3
"""Deploy ShopKeeperPro to UTM Windows 11 VM, build, test, and prepare desktop copy."""
import json
from pathlib import Path
import re
import subprocess
import sys

root = Path(__file__).resolve().parents[1]
ssh_config = Path('/Users/chamara/Projects/other/vmware/.local-ssh/config')
if not ssh_config.is_file():
    sys.exit('UTM SSH configuration not found at ' + str(ssh_config))

remote_build_dir = 'C:/Dev/Builds/winbuild/ShopKeeperPro'
remote_project_dir = 'C:/Dev/Projects/ShopKeeperPro'

# Collect allowlisted source files
files = [
    root / 'ShopKeeperPro.sln',
    root / 'ShopKeeperPro.slnx',
    root / 'scripts/windows-build.ps1',
    root / 'sql/schema.sql',
    root / 'sql/seed.sql'
]

for folder in ('src', 'tests'):
    for p in (root / folder).rglob('*'):
        rel_parts = p.relative_to(root).parts
        if any(bad in rel_parts for bad in ('bin', 'obj', '.vs', '.DS_Store')):
            continue
        if p.suffix in ('.cs', '.csproj', '.manifest', '.json', '.sql'):
            if p.is_file() and not p.is_symlink():
                files.append(p)

print(f"Collected {len(files)} source files to deploy.")

# Validate file names for Windows safety
seen = set()
for p in files:
    rel = p.relative_to(root).as_posix()
    if not re.fullmatch(r'[A-Za-z0-9_./-]+', rel):
        sys.exit(f"Unsafe filename: {rel}")
    if rel.casefold() in seen:
        sys.exit(f"Filename collision on Windows: {rel}")
    seen.add(rel.casefold())

# Determine directories to create remotely
folders = {p.parent.relative_to(root).as_posix() for p in files}
parents = set()
for folder in folders:
    for p in Path(folder).parents:
        if str(p) != '.':
            parents.add(p.as_posix())
folders |= parents

sftp_commands = [f'-mkdir "/{remote_build_dir}"']
for fld in sorted(folders, key=lambda p: (p.count('/'), p)):
    if fld != '.':
        sftp_commands.append(f'-mkdir "/{remote_build_dir}/{fld}"')

for p in files:
    rel = p.relative_to(root).as_posix()
    sftp_commands.append(f'put "{p}" "/{remote_build_dir}/{rel}"')

print("Uploading source files to Windows VM...")
sftp_proc = subprocess.run(
    ['sftp', '-F', str(ssh_config), '-b', '-', 'utm-winforms'],
    input='\n'.join(sftp_commands) + '\n',
    text=True,
    capture_output=True
)

if sftp_proc.returncode != 0:
    print("SFTP output:\n", sftp_proc.stdout)
    print("SFTP error:\n", sftp_proc.stderr)
    sys.exit(sftp_proc.returncode)

print("Source files transferred successfully.")

# Run build & verification script on Windows VM
print("Executing build and automated verification on Windows 11 VM...")
cmd = f'powershell.exe -NoLogo -NoProfile -NonInteractive -ExecutionPolicy RemoteSigned -File {remote_build_dir}/scripts/windows-build.ps1'
build_proc = subprocess.run(
    ['ssh', '-F', str(ssh_config), 'utm-winforms', cmd],
    text=True,
    capture_output=True
)

print(build_proc.stdout)
if build_proc.stderr:
    print(build_proc.stderr, file=sys.stderr)

if build_proc.returncode != 0:
    sys.exit(f"Remote build failed with exit code {build_proc.returncode}")

print("\nDeploying clean desktop copy to " + remote_project_dir + "...")
windows_cmd = Path('/Users/chamara/Projects/other/vmware/scripts/windows-command.py')
import tempfile
kill_ps = f'''
Get-Process ShopKeeperPro* -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
Copy-Item -Recurse -Force {remote_build_dir}/* {remote_project_dir}
'''
with tempfile.NamedTemporaryFile('w', suffix='.ps1', delete=False) as f:
    f.write(kill_ps)
    tmp_path = f.name
try:
    subprocess.run([sys.executable, str(windows_cmd), tmp_path], check=True)
finally:
    Path(tmp_path).unlink(missing_ok=True)

print(f"\n=======================================================")
print(f" SUCCESS! ShopKeeperPro deployed, built, and verified!")
print(f" Windows Build:   {remote_build_dir}")
print(f" Desktop Project: {remote_project_dir}")
print(f"=======================================================\n")
