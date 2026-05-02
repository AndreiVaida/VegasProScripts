using ScriptPortal.Vegas;
using System;
using System.Collections.Generic;
using System.IO;

namespace AndreiScripts.StabilizeEvents {
	public class EntryPoint {
		private static readonly string ScriptName = "Stabilize selected events (new)";
		private static readonly Timecode extendEventLength = Timecode.FromSeconds(0.5);
		private readonly Logger _logger = new Logger();

		public void FromVegas(Vegas vegas) {
			try {
				_logger.Info("↓↓↓ Script '" + ScriptName + "' start ↓↓↓");

				foreach (VideoEvent @event in GetSelectedVideoEvents(vegas)) {
                    if (IsPhoto(Path.GetFileName(@event.ActiveTake.Media.FilePath)))
						continue;

					AddStabilization(vegas, @event);
				}
            }
            catch (Exception e) {
                _logger.Error("Cannot stabilize. " + e.Message + " | " + e.ToString());
                throw;
            }
            finally {
                _logger.Info("↑↑↑ Script '" + ScriptName + "' end ↑↑↑");
            }
        }

		private void AddStabilization(Vegas vegas, VideoEvent videoEvent) {
			var originalName = Path.GetFileName(videoEvent.ActiveTake.Media.FilePath);

			try {
				var extendedTime = ExtendEvent(videoEvent);
				var subclip = CreateSubclip(vegas.Project, videoEvent);
				ShrinkEvent(videoEvent, extendedTime);

				ApplyStabilizationMediaFx(vegas, subclip);

				_logger.Info("Stabilized '" + originalName + "' (extended by " + extendedTime.ToString().Substring(6) + ")");
			}
			catch (Exception e) {
				_logger.Error("cannot stabilize '" + originalName + "'. " + e.Message + " | " + e.ToString());
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

		private static Timecode ExtendEvent(VideoEvent videoEvent) {
			var take = videoEvent.ActiveTake;
			var remaining = take.Media.Length - take.Offset - videoEvent.Length;

			var actualExtension = remaining < extendEventLength ? remaining : extendEventLength;
			if (actualExtension > Timecode.FromSeconds(0))
				videoEvent.Length += actualExtension;

			return actualExtension;
		}

		private static void ShrinkEvent(VideoEvent videoEvent, Timecode duration) {
			videoEvent.Length -= duration;
		}

		private static Subclip CreateSubclip(Project project, VideoEvent videoEvent) {
			var take = videoEvent.ActiveTake;
			var media = take.Media;

			var path = media.FilePath;
			var start = take.Offset;
			var length = videoEvent.Length;

			var name = Path.GetFileName(path) + " - Subclip";
			var subclip = new Subclip(project, path, start, length, false, name);

			var mediaStream = subclip.GetVideoStreamByIndex(0);
			videoEvent.Takes.Clear();
			videoEvent.AddTake(mediaStream, true, name);

			return subclip;
		}

		private void ApplyStabilizationMediaFx(Vegas vegas, Media media) {
			var plugIn = FindPlugIn(vegas, "{Svfx:de.magix:Stabilize}");				
			var fx = media.Effects.AddEffect(plugIn);
		}

		private PlugInNode FindPlugIn(Vegas vegas, string uniqueID) {
			foreach (var node in vegas.VideoFX) {
				if (node.UniqueID == uniqueID)
					return node;
			}
            throw new Exception("Video Stabilization plug-in not found. UniqueID=" + uniqueID);
        }

        private static bool IsPhoto(string name) {
            return name.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || name.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
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
}