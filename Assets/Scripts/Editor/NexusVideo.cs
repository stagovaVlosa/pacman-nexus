using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace Nexus.EditorTools
{
    /// <summary>
    /// Records the Game view to MP4 (Unity Recorder) - used to make the gameplay trailer (21 Nov).
    /// Files go to &lt;project&gt;/Recordings/ (ignored by Git). 1920x1080, constant 30 fps, with audio.
    /// Call Start in Play mode, Stop when done.
    /// </summary>
    public static class NexusVideo
    {
        static RecorderController controller;

        public static bool IsRecording => controller != null && controller.IsRecording();

        public static string Start(string fileName, int width = 1920, int height = 1080)
        {
            Stop();
            var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = fileName;
            movie.Enabled = true;
            movie.EncoderSettings = new CoreEncoderSettings
            {
                EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
                Codec = CoreEncoderSettings.OutputCodec.MP4
            };
            movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = width, OutputHeight = height };
            movie.AudioInputSettings.PreserveAudio = true;
            string dir = System.IO.Path.GetFullPath(Application.dataPath + "/../Recordings");
            System.IO.Directory.CreateDirectory(dir);
            movie.OutputFile = dir + "/" + fileName;

            settings.AddRecorderSettings(movie);
            settings.SetRecordModeToManual();
            settings.FrameRatePlayback = FrameRatePlayback.Constant;
            settings.FrameRate = 30f;
            settings.CapFrameRate = true;

            controller = new RecorderController(settings);
            controller.PrepareRecording();
            bool ok = controller.StartRecording();
            return ok ? movie.OutputFile + ".mp4" : "failed to start";
        }

        public static void Stop()
        {
            if (controller != null && controller.IsRecording()) controller.StopRecording();
            controller = null;
        }
    }
}
