# Config.ini

![](Img/config_ini_sample.png)

This file contains data this program needs to function.

**This file won't be packed** and must be located just inside the folder you want to pack, next to all options subfolders.

![](Img/config_folder.png)

In this case, `x:\some folders\210 Narwa` was dragged to ArmorPacker.exe.

Notice how there are many subfolders (most of them containing armor options), **but config.ini is right at `x:\some folders\210 Narwa\config.ini`**.

# Variables

## modInternalPath

**_REQUIRED_**

This is the path where your files will be actually installed by Fluffy.

### Example

By assigning this variable the value `natives\STM\player\mod\f\pl210` you are telling all the files inside your mod will be installed inside that folder.

![](Img/config_ini_path_example.png)

Notice how there are no subfolders inside `x:\some folders\210 Narwa\00 Base`, only files that will be actually distributed, yet thanks to this variable the generated \*.7z file correctly adds those files to `option_name\natives\STM\player\mod\f\pl210`.

![](Img/config_ini_pathzip_example.png)

## optionsPrefix

**_REQUIRED_**

For each armor option, this prefix will be added to the folder name for that option.

This is an anti-collision measure so your mod will play nicely with Fluffy Manager.

See ==modinfo.ini== for more information on how option names are generated.

`sick gains 210`

## extensions

**_OPTIONAL_**

Any file with any of these extensions will be added to your packed file, otherwise they are ignored.

There are two exceptions to this rule:

1. `modinfo.ini` will be included no matter what, since it is needed by Fluffy.
2. The screenshot for your armor option will be taken directly from `modinfo.ini`.

### Format

Extensions are separated by comma.

      extensions=ext1,ext2,ext3...

**Notice how none of them has a period in their name**.\
You can include periods if you want, but they aren't actually required.

This works:

      extensions=.mesh,.tex

This does exactly the same, but it looks less cluttered:

      extensions=mesh,tex

### More on extensions

Because this program is designed to be used to pack armors, their related files and nothing else, **it will always search for files that have your desired extension plus a bunch of weird numbers**.

For example, this value:

      extensions=mesh

Will pack both of these files:

      my rise armor.mesh.2008058288
      my sunbreak armor.mesh.2109148288

But not this file:

      lololol.mesh

### Default

If you ommit the `extensions` variable, it will default to `mdf2,mesh,chain`: the most common type of armor files to be distributed.
