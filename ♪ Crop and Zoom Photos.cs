using ScriptPortal.Vegas;
using System;
using System.Collections.Generic;
using System.IO;

namespace AndreiScripts.CropAndZoomPhotos {
    public class EntryPoint {
        private static readonly string ScriptName = "Crop and zoom photos";
        private readonly Logger _logger = new Logger();

        public void FromVegas(Vegas vegas) {
            try {
                _logger.Info("↓↓↓ Script '" + ScriptName + "' start ↓↓↓");

                var selectedEvents = GetSelectedVideoEvents(vegas);
                var plugIn = FindPlugInByName(vegas, "Picture In Picture");

                foreach (var @event in selectedEvents) {
                    try {
                        CropToMatchOutputAspect(vegas, @event);
                        AddZoom(@event, plugIn);
                    }
                    catch (Exception e) {
                        var name = Path.GetFileName(@event.ActiveTake.Media.FilePath);
                        _logger.Error("Cannot crop/zoom " + name + ": " + e.Message + " | " + e.ToString());
                    }
                }
            }
            catch (Exception e) {
                _logger.Error("Script error: " + e.Message + " | " + e.ToString());
                throw;
            }
            finally {
                _logger.Info("↑↑↑ Script '" + ScriptName + "' end ↑↑↑");
            }
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

        private void CropToMatchOutputAspect(Vegas vegas, VideoEvent videoEvent) {
            var name = Path.GetFileName(videoEvent.ActiveTake.Media.FilePath);
            if (!HasHorizontalAspectRatio(videoEvent)) {
                _logger.Info("Skipped vertical photo: " + name);
                return;
            }

            VideoMotion vm = videoEvent.VideoMotion;
            VideoMotionKeyframe kf = vm.Keyframes[0];
            VideoMotionBounds rect = kf.Bounds;

            // Compute current width/height
            float width = rect.TopRight.X - rect.TopLeft.X;
            float height = rect.BottomLeft.Y - rect.TopLeft.Y;

            // Compute center
            float cx = (rect.TopLeft.X + rect.TopRight.X) / 2f;
            float cy = (rect.TopLeft.Y + rect.BottomLeft.Y) / 2f;

            // Aspect ratios
            double projectAR = (double)vegas.Project.Video.Width / vegas.Project.Video.Height;
            double mediaAR = (double)width / height;

            if (mediaAR > projectAR) {
                // Media is wider → crop width
                float newWidth = (float)(height * projectAR);
                float half = newWidth / 2f;

                rect.TopLeft.X = cx - half;
                rect.BottomLeft.X = cx - half;
                rect.TopRight.X = cx + half;
                rect.BottomRight.X = cx + half;
            }
            else {
                // Media is taller → crop height
                float newHeight = (float)(width / projectAR);
                float half = newHeight / 2f;

                rect.TopLeft.Y = cy - half;
                rect.TopRight.Y = cy - half;
                rect.BottomLeft.Y = cy + half;
                rect.BottomRight.Y = cy + half;
            }

            // Assign back
            kf.Bounds = rect;

            _logger.Info("Match output aspect: " + name);
        }

        private bool HasHorizontalAspectRatio(VideoEvent ev) {
            var take = ev.ActiveTake;
            var mediaStream = take.MediaStream as VideoStream;
            return mediaStream != null && mediaStream.Width > mediaStream.Height;
        }

        private void AddZoom(VideoEvent videoEvent, PlugInNode plugIn) {
            var name = Path.GetFileName(videoEvent.ActiveTake.Media.FilePath);

            var fx = videoEvent.Effects.AddEffect(plugIn);
            var ofx = fx.OFXEffect;
            var scaleX = GetScaleParameter(ofx);

            scaleX.IsAnimated = true;
            scaleX.SetValueAtTime(Timecode.FromSeconds(0), 1.0);
            scaleX.SetValueAtTime(Timecode.FromSeconds(10), 1.1);

            _logger.Info("Added zoom animation to: " + name);
        }

        private OFXDoubleParameter GetScaleParameter(OFXEffect ofx) {
            foreach (OFXParameter param in ofx.Parameters)
                if (param.Name == "Scale")
                    return param as OFXDoubleParameter;
            throw new Exception("Scale parameter not found.");
        }

        private PlugInNode FindPlugInByName(Vegas vegas, string name) {
            foreach (var node in vegas.VideoFX) {
                if (node.Name == name)
                    return node;
            }
            throw new KeyNotFoundException("Picture In Picture plug-in not found");
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