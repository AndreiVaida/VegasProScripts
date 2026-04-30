# Scripts for Vegas Pro
Sony/MAGIX/BorisFX Vegas Pro supports scripting in C#. <br>
Scripts are stored in `C:\Users\<USER>\Documents\Vegas Script Menu` and can be accessed from the menu `Tools > Scripting`.

Limitations:
- Language version is limited to `3` and `.NET Framework 3.5`.
- Multiple files are not supported. All code must be in a single script file.

This repository can be placed in the `Vegas Script Menu\` folder so that Vegas can use its latest code in real time.

## Scripts
### ♪ Move selected events to their track
This script moves all selected events to their track. Working tracks:
- `HLG` - for Sony Alpha and SilverCrest videos
- `Poze` - for photos
- `Smartphone` - for smartphone videos

It assumes all selected events (photos and videos) are in the `HLG` track.
Photos and smartphone videos are moved to their tracks.
Sony videos are already on the correct track.

### ♪ Stabilize selected events
This script adds the new stabilization Media FX to the selected events.
Steps:
1. Extend the clip by 0.5 seconds.
1. Create subclip.
1. Shrink the subclip back to its original length.
1. Apply `Video Stabilization` Media FX with _"Basic"_ settings (the new stabilization effect).

Then the user needs to open the Media FX window manually and click the Apply button.

#### 💡 Create keyboard shortcut
Options → Customize Keyboard... → search for stabilization script → select Global → assign shortcut (e.g. `Alt + S`)