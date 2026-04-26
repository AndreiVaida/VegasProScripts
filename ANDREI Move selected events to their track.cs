using ScriptPortal.Vegas;
using System;
using System.Collections.Generic;
using System.IO;

public class EntryPoint {
    private static readonly string ScriptName = "ANDREI: Move selected events to their track";
    private readonly Logger _logger = new Logger();

    public void FromVegas(Vegas vegas) {
        _logger.Info("=== Script '" + ScriptName + "' start ===");

        var sonyTrack = FindOrCreateTrack(vegas, "HLG");
        var smartphoneTrack = FindOrCreateTrack(vegas, "Smartphone");
        var photoTrack = FindOrCreateTrack(vegas, "Poze");

        var moves = GetEventsToMove(sonyTrack, smartphoneTrack, photoTrack);
        MoveEventsToTheirTrack(moves);

        _logger.Info("=== Script '" + ScriptName + "' end ===");
    }

    private List<MoveRequest> GetEventsToMove(Track sourceTrack, VideoTrack smartphoneTrack, VideoTrack photoTrack) {
        var moves = new List<MoveRequest>();
        var events = GetSelectedEvents(sourceTrack.Events);

        foreach (var ev in events) {
            if (ev.ActiveTake == null) {
                _logger.Info("Skipped event (no ActiveTake) | Start=" + ev.Start.ToString());
                continue;
            }

            var name = Path.GetFileName(ev.ActiveTake.Media.FilePath);

            if (IsPhoto(name)) {
                moves.Add(new MoveRequest(ev, photoTrack, name));
            }
            else if (IsSmartphoneVideo(ev, name)) {
                moves.Add(new MoveRequest(ev, smartphoneTrack, name));
            }
        }

        return moves;
    }

    private List<TrackEvent> GetSelectedEvents(TrackEvents events) {
        var selected = new List<TrackEvent>();

        foreach (var ev in events)
            if (ev.Selected)
                selected.Add(ev);

        return selected;
    }


    private void MoveEventsToTheirTrack(List<MoveRequest> moves) {
        foreach (var req in moves) {
            try {
                MoveEvent(req.ev, req.targetTrack);
                _logger.Info("Moved " + req.name + " → " + req.targetTrack.Name);
            }
            catch (Exception ex) {
                _logger.Error("Cannot move " + req.name + ": " + ex.Message + " | " + ex.ToString());
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

    private static bool IsPhoto(string name) {
        return name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideo(string name) {
        return !IsPhoto(name);
    }

    private static bool IsSmartphoneVideo(TrackEvent ev, string name) {
        return (name.StartsWith("202") || name.StartsWith("VID_")) && (IsVideo(name) && !IsSilverCrestVideo(ev.ActiveTake.Media.FilePath));
    }

    private static bool IsSilverCrestVideo(string filePath) {
        return filePath.Contains("SilverCrest");
    }

    private VideoTrack FindOrCreateTrack(Vegas vegas, string name) {
        foreach (var track in vegas.Project.Tracks)
            if (track.Name == name) return (VideoTrack)track;

        var newTrack = new VideoTrack(vegas.Project.Tracks.Count, name);
        vegas.Project.Tracks.Add(newTrack);
        return newTrack;
    }

    private void MoveEvent(TrackEvent ev, VideoTrack targetTrack) {
        if (ev.Track == targetTrack)
            return;

        var start = ev.Start;
        var length = ev.Length;

        var media = ev.ActiveTake.Media;
        var stream = media.Streams.Count > 0 ? media.Streams[0] : null;

        if (stream == null) {
            _logger.Info("ERROR: Media has zero streams: " + media.FilePath);
            return;
        }

        // Create new event on target track
        var newEv = targetTrack.AddVideoEvent(start, length);
        newEv.AddTake(stream);

        ev.Track.Events.Remove(ev);
    }    
}

class Logger {
    private readonly string logPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Vegas Script Menu",
        "Logs",
        "Logs " + DateUtils.GetNowDate() + ".txt"
    );

    public Logger() {
        var dir = Path.GetDirectoryName(logPath);
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public void Info(string text) {
        Log(text, "INFO");
    }

    public void Error(string text) {
        Log(text, "ERROR");
    }

    private void Log(string text, string level) {
        File.AppendAllText(logPath, DateUtils.GetNow() + "  (" + level + ") " + text + "\r\n");
    }
}

static class DateUtils {
    public static string GetNowDate() {
        return DateTime.Now.ToString("yyyy.MM.dd");
    }

    public static string GetNow() {
        return DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss");
    }
}

