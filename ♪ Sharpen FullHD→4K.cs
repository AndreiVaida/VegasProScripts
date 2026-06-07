using ScriptPortal.Vegas;
using System;
using System.Collections.Generic;
using System.IO;

namespace AndreiScripts.SharpenFullHD4K {
	public class EntryPoint {
		private static readonly string ScriptName = "Sharpen FullHD videos for 4K";
		private static readonly string SharpenEffect = "Sharpen";
		private static readonly string SharpenFullHdTo4KPreset = "FullHD > 4K";
		private readonly Logger _logger = new Logger();

		public void FromVegas(Vegas vegas) {
			try {
				_logger.Info("↓↓↓ Script '" + ScriptName + "' start ↓↓↓");

				foreach (VideoEvent @event in GetSelectedVideoEvents(vegas)) {
                    if (IsFullHdVideo(@event))
						AddSharpen(vegas, @event);
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

		private bool IsFullHdVideo(VideoEvent videoEvent) {
			if (IsPhoto(Path.GetFileName(videoEvent.ActiveTake.Media.FilePath)))
				return false;

            var take = videoEvent.ActiveTake;
            var mediaStream = take.MediaStream as VideoStream;
			if (mediaStream == null) {
				_logger.Error("Media stream is not a video stream for " + videoEvent.ActiveTake.Media.FilePath);
				return false;
			}

            return mediaStream.Width < 2000;
        }

		private void AddSharpen(Vegas vegas, VideoEvent videoEvent) {
			var originalName = Path.GetFileName(videoEvent.ActiveTake.Media.FilePath);

			try {
				if (HasEffect(videoEvent, SharpenEffect))
					return;

				AddSharpenFX(vegas, videoEvent, originalName);
				_logger.Info("Applied Sharpen '" + SharpenFullHdTo4KPreset + "' to: " + originalName);
			}
			catch (Exception e) {
				_logger.Error("cannot add Sharpen '" + SharpenFullHdTo4KPreset + "' to '" + originalName + "'. " + e.Message + " | " + e.ToString());
			}
		}

		private bool HasEffect(VideoEvent videoEvent, string effectName) {
            foreach (Effect e in videoEvent.Effects)
                if (e.Description == effectName)
                    return true;
			return false;
        }

        private void AddSharpenFX(Vegas vegas, VideoEvent videoEvent, string name) {
			var plugIn = FindPlugIn(vegas, SharpenEffect);
			if (plugIn == null) {
				_logger.Error("Sharpen plug-in not found");
				return;
			}

			var fx = videoEvent.Effects.AddEffect(plugIn);
			ApplyPresetToEffect(fx, SharpenFullHdTo4KPreset, name);
		}

		private void ApplyPresetToEffect(Effect fx, string preset, string name) {
			try {
				var t = fx.GetType();

				// 1) Try property named "Preset"
				var pProp = t.GetProperty("Preset");
				if (pProp != null && pProp.CanWrite) {
					pProp.SetValue(fx, preset, null);
					_logger.Info("Set property 'Preset' on effect for: " + name);
					return;
				}

				// 2) Try method LoadPreset(string)
				var loadMethod = t.GetMethod("LoadPreset", new Type[] { typeof(string) });
				if (loadMethod != null) {
					loadMethod.Invoke(fx, new object[] { preset });
					_logger.Info("Invoked LoadPreset(string) on effect for: " + name);
					return;
				}

				// 3) Try OFXEffect member (older OFX bridging)
				try {
					var ofxProp = t.GetProperty("OFXEffect");
					if (ofxProp != null) {
						var ofxVal = ofxProp.GetValue(fx, null);
						if (ofxVal != null) {
							var ot = ofxVal.GetType();
							var ofxLoad = ot.GetMethod("LoadPreset", new Type[] { typeof(string) });
							if (ofxLoad != null) {
								ofxLoad.Invoke(ofxVal, new object[] { preset });
								_logger.Info("Invoked OFXEffect.LoadPreset on effect for: " + name);
								return;
							}
						}
					}
				}
				catch (Exception exOfx) {
					_logger.Error("OFX preset attempt failed for: " + name + " -> " + exOfx.Message);
				}

				_logger.Error("Could not apply preset '" + preset + "' to effect for: " + name);
			}
			catch (Exception ex) {
				_logger.Error("Error applying preset to effect for: " + name + " -> " + ex.Message + " | " + ex.ToString());
			}
		}

		private PlugInNode FindPlugIn(Vegas vegas, string name) {
			foreach (var node in vegas.VideoFX) {
                if (node.Name == name)
					return node;
			}
            throw new Exception("Video Stabilization plug-in not found.");
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
}