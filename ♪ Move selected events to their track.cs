using ScriptPortal.Vegas;
using System;
using System.Collections.Generic;
using System.IO;

namespace AndreiScripts.MoveEvents {
    public class EntryPoint {
        private static readonly string ScriptName = "Move selected events to their track";
        private readonly Logger _logger = new Logger();

        public void FromVegas(Vegas vegas) {
            try {
                _logger.Info("↓↓↓ Script '" + ScriptName + "' start ↓↓↓");

                var sonyTrack = FindOrCreateTrack(vegas, "HLG");
                var smartphoneTrack = FindOrCreateTrack(vegas, "Smartphone");
                var sonyHdrCx405Track = FindOrCreateTrack(vegas, "Sony HDR-CX405");
                var photoTrack = FindOrCreateTrack(vegas, "Poze");

                var moves = GetEventsToMove(vegas, sonyTrack, smartphoneTrack, sonyHdrCx405Track, photoTrack);
                MoveEventsToTheirTrack(moves);
            }
            catch (Exception e) {
                _logger.Error("Script error: " + e.Message + " | " + e.ToString());
                throw;
            }
            finally {
                _logger.Info("↑↑↑ Script '" + ScriptName + "' end ↑↑↑");
            }
        }

        private List<MoveRequest> GetEventsToMove(Vegas vegas, VideoTrack sonyTrack, VideoTrack smartphoneTrack, VideoTrack sonyHdrCx405Track, VideoTrack photoTrack) {
            var moves = new List<MoveRequest>();
            var events = GetSelectedVideoEvents(vegas);

            foreach (var ev in events) {
                if (ev.ActiveTake == null) {
                    _logger.Info("Skipped event (no ActiveTake) | Start=" + ev.Start.ToString());
                    continue;
                }

                var name = Path.GetFileName(ev.ActiveTake.Media.FilePath);

                if (IsPhoto(name)) {
                    moves.Add(new MoveRequest(ev, photoTrack, name));
                }
                else if (IsHlgVideo(ev, name)) {
                    moves.Add(new MoveRequest(ev, sonyTrack, name));
                }
                else if (IsSmartphoneVideo(ev, name)) {
                    moves.Add(new MoveRequest(ev, smartphoneTrack, name));
                }
                else if (IsSonyHdrCx405Video(ev)) {
                    moves.Add(new MoveRequest(ev, sonyHdrCx405Track, name));
                }
                else {
                    _logger.Warning("Unknown event '" + name + "' | Start=" + ev.Start.ToString());
                }
            }

            return moves;
        }

        private static List<VideoEvent> GetSelectedVideoEvents(Vegas vegas) {
            var list = new List<VideoEvent>();

            foreach (Track track in vegas.Project.Tracks)
                foreach (TrackEvent @event in track.Events)
                    if (@event.Selected) {
                        VideoEvent videoEvent = @event as VideoEvent;
                        if (videoEvent != null)
                            list.Add(videoEvent);
                    }

            return list;
        }

        private void MoveEventsToTheirTrack(List<MoveRequest> moves) {
            foreach (var req in moves) {
                try {
                    var isMoved = MoveEvent(req.ev, req.targetTrack);
                    _logger.Info((isMoved ? "Moved " : "Not moved ") + req.name + " → " + req.targetTrack.Name);
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

        private static bool IsHlgVideo(TrackEvent ev, string name) {
            return name.StartsWith("C") || (IsVideo(name) && IsSilverCrestVideo(ev.ActiveTake.Media.FilePath));
        }

        private static bool IsSmartphoneVideo(TrackEvent ev, string name) {
            return (name.StartsWith("202") || name.StartsWith("VID_")) && (
                IsVideo(name) &&
                !IsSilverCrestVideo(ev.ActiveTake.Media.FilePath) &&
                !IsSonyHdrCx405Video(ev)
                );
        }

        private static bool IsSonyHdrCx405Video(TrackEvent ev) {
            return ev.ActiveTake.Media.FilePath.Contains("Sony HDR-CX405");
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

        private bool MoveEvent(TrackEvent ev, VideoTrack targetTrack) {
            if (ev.Track == targetTrack)
                return false;

            var group = ev.Group;

            if (group == null || group.Count <= 1) {
                ev.Track = targetTrack;
                return true;
            }

            // Move all grouped events together, but only if they're video events
            foreach (var trackEvent in group) {
                if (trackEvent.Track != targetTrack && trackEvent is VideoEvent) {
                    trackEvent.Track = targetTrack;
                }
            }

            return true;
        }
    }

    /* Utils */

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

        public void Warning(string text) {
            Log(text, "WARNING");
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
}