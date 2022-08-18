# Create Alt Armor Version

F# script that creates alternative versions of armors by copying files to another folder, so they can be worked on them.

If configured correctly it will create all relative paths for a file name.

# Usage

Drag and drop the files you want to create alternative versions of to the `drag_files_here.bat` file.

You will be asked to provide a suffix for the new directory, then all dragged files will be copied to the newly created directory with all their needed folder structure.

# Configuration

By default, this script expects your mods to be in a Mod Organizer `mods` dir.

For example, this is the kind of file structire it expects to get:

    x:\MH Rise\Mod.Organizer\mods\EBB Malzeno Armor\natives\STM\player\mod\f\pl352\f_body352.mesh.2109148288
    x:\MH Rise\Mod.Organizer\mods\EBB Malzeno Armor\natives\STM\player\mod\f\pl352\f_leg352.mesh.2109148288

If you dragged those files to `drag_files_here.bat` and added the `_Skimpy` suffix, these would be the newly copied files:

    x:\MH Rise\Mod.Organizer\mods\EBB Malzeno Armor_Skimpy\natives\STM\player\mod\f\pl352\f_body352.mesh.2109148288
    x:\MH Rise\Mod.Organizer\mods\EBB Malzeno Armor_Skimpy\natives\STM\player\mod\f\pl352\f_leg352.mesh.2109148288

If you want another folder structure to be recognized, you need to open the `script.fsx` file
