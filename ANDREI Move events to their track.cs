using ScriptPortal.Vegas;
using System;
using System.IO;
using System.Collections.Generic;

public class EntryPoint {
    private static readonly string ScriptName = "ANDREI: Move events to their track";
    private readonly Logger _logger = new Logger();

    public void FromVegas(Vegas vegas) {
        _logger.Log("=== Script '" + ScriptName + "' start ===");

        VideoTrack sonyTrack = FindOrCreateTrack(vegas, "HLG");
        VideoTrack smartphoneTrack = FindOrCreateTrack(vegas, "Smartphone");
        VideoTrack photoTrack = FindOrCreateTrack(vegas, "Poze");

        List<MoveRequest> moves = CollectMoveRequests(sonyTrack, smartphoneTrack, photoTrack);
        MoveEventsToTheirTrack(moves);

        _logger.Log("=== Script '" + ScriptName + "' end ===");
    }

    private List<MoveRequest> CollectMoveRequests(Track sourceTrack, VideoTrack smartphoneTrack, VideoTrack photoTrack) {
        List<MoveRequest> moves = new List<MoveRequest>();
        var events = GetSelectedEvents(sourceTrack.Events);

        foreach (TrackEvent ev in events) {
            if (ev.ActiveTake == null) {
                _logger.Log("Skipped event (no ActiveTake) | Start=" + ev.Start.ToString());
                continue;
            }

            string path = ev.ActiveTake.Media.FilePath;
            string name = Path.GetFileName(path);

            if (IsPhoto(ev)) {
                moves.Add(new MoveRequest(ev, photoTrack, name));
            }
            else if (IsSmartphoneVideo(ev)) {
                moves.Add(new MoveRequest(ev, smartphoneTrack, name));
            }
        }

        return moves;
    }

    private List<TrackEvent> GetSelectedEvents(TrackEvents events) {
        List<TrackEvent> selected = new List<TrackEvent>();

        foreach (TrackEvent ev in events)
            if (ev.Selected)
                selected.Add(ev);

        return selected;
    }


    private void MoveEventsToTheirTrack(List<MoveRequest> moves) {
        foreach (MoveRequest req in moves) {
            try {
                MoveEvent(req.ev, req.targetTrack);
                _logger.Log("Moved: " + req.name + " → " + req.targetTrack.Name);
            }
            catch (Exception ex) {
                _logger.Log("ERROR moving " + req.name + ": " + ex.Message);
            }
        }
    }

    private class MoveRequest {
        public TrackEvent ev;
        public VideoTrack targetTrack;
        public string name;

        public MoveRequest(TrackEvent e, VideoTrack t, string n) {
            ev = e;
            targetTrack = t;
            name = n;
        }
    }

    private static bool IsPhoto(TrackEvent ev) {
        string name = Path.GetFileName(ev.ActiveTake.Media.FilePath).ToLower();
        return name.EndsWith(".jpg") || name.EndsWith(".jpeg");
    }

    private static bool IsVideo(TrackEvent ev) {
        return !IsPhoto(ev);
    }

    private static bool IsSmartphoneVideo(TrackEvent ev) {
        string name = Path.GetFileName(ev.ActiveTake.Media.FilePath);
        return (name.StartsWith("202") || name.StartsWith("VID_")) && IsVideo(ev);
    }

    private VideoTrack FindOrCreateTrack(Vegas vegas, string name) {
        foreach (Track t in vegas.Project.Tracks)
            if (t.Name == name) return (VideoTrack)t;

        VideoTrack newTrack = new VideoTrack(vegas.Project.Tracks.Count, name);
        vegas.Project.Tracks.Add(newTrack);
        return newTrack;
    }

    private void MoveEvent(TrackEvent ev, VideoTrack targetTrack) {
        if (ev.Track == targetTrack)
            return;

        Timecode start = ev.Start;
        Timecode length = ev.Length;

        Media media = ev.ActiveTake.Media;
        MediaStream stream = null;

        if (media.Streams.Count > 0) {
            stream = media.Streams[0];
        }
        else {
            _logger.Log("ERROR: Media has zero streams: " + media.FilePath);
            return;
        }

        // Create new event on target track
        TrackEvent newEv = targetTrack.AddVideoEvent(start, length);
        newEv.AddTake(stream);

        // Remove original event
        ev.Track.Events.Remove(ev);
    }

    private static string GetNowDate() {
        return DateTime.Now.ToString("yyyy.MM.dd");
    }

    private static string GetNow() {
        return DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
    }

    private string JoinNames(List<string> names) {
        string line = "";
        for (int i = 0; i < names.Count; i++) {
            if (i > 0) line += ", ";
            line += names[i];
        }
        return line;
    }

    public class Logger {
        private readonly string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Vegas Script Menu",
            "Logs",
            "Logs " + GetNowDate() + ".txt"
        );

        public Logger() {
            string dir = Path.GetDirectoryName(logPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public void Log(string text) {
            File.AppendAllText(logPath, GetNow() + "  " + text + "\r\n");
        }
    }
}
