# FolderThumbnailFix

[![image](https://github.com/LesFerch/WinSetView/assets/79026235/0188480f-ca53-45d5-b9ff-daafff32869e)Download the zip file](https://github.com/LesFerch/FolderThumbnailFix/releases/download/1.0.7/FolderThumbnailFix.zip)

## How to Download and Run

1. Download the zip file using the link above.
2. Extract all files (**FolderThumbnailFix.exe** and **AppParts**).
3. Right-click **FolderThumbnailFix.exe**, select Properties, check Unblock, and click OK.
4. Optionally move the files to a folder of your choice.
5. Double-click **FolderThumbnailFix.exe** to change the Windows 11 folder thumbnail style.
6. If you skipped step 3, then, in the SmartScreen window, click More info and then Run anyway.

**Note**: Some antivirus software may falsely detect the download as a virus. This can happen any time you download a new executable and may require extra steps to whitelist the file.

## What it does

![image](https://github.com/user-attachments/assets/e5be5692-889c-457d-8e9c-0dddeb651c2d)

This tool patches Windows 11 so that folder thumbnails are the full size of the folder icon.

You can also use the tool to undo the change and switch back to the default half-covered thumbnails.

FolderThumbnailFix does not install any software. It simply replaces the mask icon with a transparent icon in the file `C:\Windows\SystemResources\imageres.dll.mun`

**Note**: If the thumbnail does not change, sign out, sign in (don't restart) and try again. If the icon still does not change, start up Windows in [Safe Mode](https://www.elevenforum.com/t/boot-to-safe-mode-in-windows-11.538/) and run the tool again. In Safe Mode, there will be no apps running that could lock out the resource that needs to be replaced.

**Note**: In order to make the change, the Explorer process is killed, so you will see a black screen until Explorer restarts. Windows 11 can sometimes take a long time to restart Explorer (I've observed delays over 30 seconds). If Explorer has not restarted after a minute you can hit `Ctrl-Alt-Del` (and then restart it via Task Manager or sign out and sign in).

## Command line

In addition to the GUI shown above, you can also change the folder mask icon from the command line. Use the `/install` argument to apply the transparent icon. Use the `/remove` argument to return to the half-cover icon.

## Credits

[Resource Hacker](https://www.angusj.com/resourcehacker/) is used to replace the folder thumbnail mask icon. It's included in this package with the permission of the developer Angus Johnson.
\
\
\
[![image](https://github.com/LesFerch/WinSetView/assets/79026235/63b7acbc-36ef-4578-b96a-d0b7ea0cba3a)](https://github.com/LesFerch/FolderThumbnailFix)
