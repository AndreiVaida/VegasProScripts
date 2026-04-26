# Scripts for Vegas Pro
Sony/MAGIX/BorisFX Vegas Pro supports scripting in C#. <br>
Scripts are stored in `C:\Users\<USER>\Documents\Vegas Script Menu` and can be accessed from the menu `Tools > Scripting`.

Limitations:
- Language version is limited to `3` and `.NET Framework 3.5`.
- Multiple files are not supported. All code must be in a single script file.

This repository can be placed in the `Vegas Script Menu` folder so that Vegas can use their latest code in real time.

## Scripts
### ANDREI Move selected events to their track
This script moves all selected events to their track. Working tracks:
- `HLG` - for Sony Alpha and SilverCrest videos
- `Poze` - for photos
- `Smartphone` - for smartphone videos

It assumes all selected events (photos and vieos) are in the `HLG` track.
Photos and smartphone videos are moved to their tracks.
Sony videos are already on the correct track.