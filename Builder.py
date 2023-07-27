def output(format, message, linesbefore=0, linesafter=0):
    for i in range(0, linesbefore):
        print()
    print(format.format(message))
    for i in range(0, linesafter):
        print()


def success(message, linesbefore=0, linesafter=0):
    output("\033[92m{}\033[0m", message, linesbefore, linesafter)


def error(message, linesbefore=0, linesafter=0):
    output("\033[91m{}\033[0m", message, linesbefore, linesafter)


# Import Basic Packages
import os
import shutil
import subprocess
from tkinter import filedialog

# Enable Console Colors
os.system('color')

# Import External Packages
try:
    import psutil
except Exception as e:
    error(e, 1, 1)
    exit(1)


# Mod ID
modSteamId = "3005289691"  # Replace with your actual Steam ID

# Mod Resources
source_dir = "ModData"
destination_dir = f"Build/{modSteamId}"

# Mod C# and DLLs
source_solution = "Source/Client/GameClient.csproj"
dll_output_dir = "Source/Client/bin/Debug/net472/"
dll_destination_dir = os.path.join(destination_dir, "Current/Assemblies/")
dll_names = ["GameClient.dll", "AsyncIO.dll", "NetMQ.dll"]

# RimWorld Directory
rimworld_dir_file = "rimworld_dir.txt"
rimworld_dir = ""


def handle_rim_world_path(possible_rimworld_dir):
    global rimworld_dir

    print(f"Checking {possible_rimworld_dir}")

    # Append the Mods folder to the RimWorld directory
    mod_path = os.path.join(possible_rimworld_dir, "Mods")

    if os.path.exists(mod_path):
        mod_specific_path = os.path.join(mod_path, modSteamId)

        # Clear the specific mod directory if it already exists
        if os.path.exists(mod_specific_path):
            rimworld_dir = possible_rimworld_dir
            shutil.rmtree(mod_specific_path)

        # Copy mod to mod_path
        shutil.copytree(destination_dir, mod_specific_path)

        success(f"{possible_rimworld_dir} is a valid RimWorld directory")

        return True

    return False


def build():
    print("Building DLLs")

    # Remove destination_dir for a fresh build
    if os.path.exists(destination_dir):
        shutil.rmtree(destination_dir)

    # Copy files from source_dir to destination_dir
    shutil.copytree(source_dir, destination_dir)

    # Build the C# project
    subprocess.run(["dotnet", "build", source_solution, "--configuration", "Debug"])

    # If the destination path doesn't exist, create it
    os.makedirs(dll_destination_dir, exist_ok=True)

    # Iterate over DLL names
    for dll_name in dll_names:
        # Create full paths for each DLL
        dll_output_path = os.path.join(dll_output_dir, dll_name)
        dll_destination_path = os.path.join(dll_destination_dir, dll_name)

        # Copy the DLL file
        shutil.copy2(dll_output_path, dll_destination_path)

    success("Completed building DLLs")


def run():
    print("Finding RimWorld")

    default_rimworld_dirs = [
        "C:/Games/Rimworld",
        "C:/Program Files (x86)/Steam/steamapps/common/RimWorld"
    ]

    if (os.path.isfile(rimworld_dir_file)):
        f = open(rimworld_dir_file, "r")
        saved_rimworld_dir = f.read()
        if (saved_rimworld_dir != ""):
            default_rimworld_dirs.insert(0, saved_rimworld_dir)

    # Copy mod to each path if it exists
    for default_rimworld_dir in default_rimworld_dirs:
        handle_rim_world_path(default_rimworld_dir)

    if rimworld_dir == "":
        print("RimWorld not found, requesting installation directory")
        directory = filedialog.askdirectory(initialdir=None, mustexist=True, title="Select RimWorld Directory")

        # If no directory provided, end here
        if directory == "":
            error("No RimWorld directory provided", 1, 1)
            exit(2)

        handle_rim_world_path(directory)

    # If no directory found, end here
    if rimworld_dir == "":
        error("Failed to find RimWorld Installation Directory", 1, 1)
        exit(2)

    f = open(rimworld_dir_file, "w")
    f.write(rimworld_dir)
    f.close()

    answer = input("Open RimWorld? (Y/N, Default:Y)")
    no = answer == "n" or answer == 'N'

    if (no):
        exit(0)

    success("Now opening RimWorld")

    norm_exe_path = os.path.normcase(os.path.realpath(os.path.join(rimworld_dir, "RimWorldWin64.exe")))

    # Check if the process is already running
    for proc in psutil.process_iter(['pid', 'name', 'exe']):
        # Check whether the process name matches
        if proc.info['exe'] and os.path.normcase(os.path.realpath(proc.info['exe'])) == norm_exe_path:
            proc.kill()  # If so, kill the process

    subprocess.Popen(norm_exe_path)


try:
    build()
    run()
except Exception as e:
    error(e, 1, 1)
    exit(1)

exit(0)
