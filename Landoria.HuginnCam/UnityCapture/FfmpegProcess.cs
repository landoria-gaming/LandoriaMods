using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace Landoria.HuginnCam.UnityCapture
{
    internal sealed class FfmpegProcess
    {
        private readonly Process _process;

        private FfmpegProcess(Process process)
        {
            _process = process;
        }

        internal bool HasExited => _process.HasExited;
        internal bool Succeeded => _process.HasExited && _process.ExitCode == 0;

        internal static FfmpegProcess Start(
            string videoPipe,
            string audioPipe,
            int audioSampleRate,
            int audioChannels,
            string output,
            int width,
            int height,
            int frameRate,
            string gpuVendor)
        {
            string executable = ResolveExecutable();
            string encoder = SelectEncoder(executable, gpuVendor);
            string arguments = BuildArguments(
                audioPipe,
                videoPipe,
                audioSampleRate,
                audioChannels,
                output,
                width,
                height,
                frameRate,
                encoder);
            Process process = Process.Start(CreateStartInfo(executable, arguments));
            if (process == null)
            {
                throw new InvalidOperationException("FFmpeg did not start.");
            }

            return new FfmpegProcess(process);
        }

        internal void Stop()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.StandardInput.WriteLine("q");
                    if (!_process.WaitForExit(15_000))
                    {
                        _process.Kill();
                    }
                }
            }
            catch (Exception exception)
            {
                HuginnCamPlugin.Log.LogError(exception);
            }
            finally
            {
                _process.Dispose();
            }
        }

        internal static FfmpegProcess StartCompression(string inputPath, string outputPath, string gpuVendor)
        {
            string executable = ResolveExecutable();
            string encoder = SelectHevcEncoder(executable, gpuVendor);
            string options = GetHevcOptions(encoder);
            string arguments = $"-hide_banner -y -i \"{inputPath}\" {options} " +
                               $"-c:a aac -b:a 320k -movflags +faststart \"{outputPath}\"";
            Process process = Process.Start(CreateStartInfo(executable, arguments));
            if (process == null)
            {
                throw new InvalidOperationException("FFmpeg compression did not start.");
            }

            return new FfmpegProcess(process);
        }

        public void Dispose()
        {
            _process.Dispose();
        }

        private static string ResolveExecutable()
        {
            string configuredPath = HuginnCamPreference.FfmpegPath;
            string executable = string.IsNullOrWhiteSpace(configuredPath) ? "ffmpeg.exe" : configuredPath;
            using (Process process = Process.Start(CreateStartInfo(executable, "-hide_banner -version")))
            {
                if (process == null || !process.WaitForExit(5_000) || process.ExitCode != 0)
                {
                    throw new FileNotFoundException($"FFmpeg is unavailable: {executable}");
                }
            }

            return executable;
        }

        private static ProcessStartInfo CreateStartInfo(string executable, string arguments)
        {
            return new ProcessStartInfo(executable, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
        }

        private static string BuildArguments(
            string pipe,
            string videoPipe,
            int audioSampleRate,
            int audioChannels,
            string output,
            int width,
            int height,
            int frameRate,
            string encoder)
        {
            string rate = audioSampleRate.ToString(CultureInfo.InvariantCulture);
            string channels = audioChannels.ToString(CultureInfo.InvariantCulture);
            string videoOptions = GetVideoOptions(encoder);
            return $"-hide_banner -y -use_wallclock_as_timestamps 1 -f rawvideo -pixel_format rgba " +
                   $"-video_size {width}x{height} -framerate {frameRate} -i \"{videoPipe}\" " +
                   $"-f f32le -ar {rate} -ac {channels} -i \"{pipe}\" " +
                   $"{videoOptions} -c:a pcm_f32le " +
                   $"-fps_mode cfr -f matroska \"{output}\"";
        }

        private static string SelectEncoder(string executable, string gpuVendor)
        {
            string encoders = ReadEncoderList(executable);
            string vendor = gpuVendor ?? string.Empty;
            if (vendor.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 && encoders.Contains("h264_nvenc"))
            {
                return "h264_nvenc";
            }

            if (vendor.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 && encoders.Contains("h264_amf"))
            {
                return "h264_amf";
            }

            if (vendor.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 && encoders.Contains("h264_qsv"))
            {
                return "h264_qsv";
            }

            return "h264_mf";
        }

        private static string SelectHevcEncoder(string executable, string gpuVendor)
        {
            string encoders = ReadEncoderList(executable);
            string vendor = gpuVendor ?? string.Empty;
            if (vendor.IndexOf("NVIDIA", StringComparison.OrdinalIgnoreCase) >= 0 && encoders.Contains("hevc_nvenc"))
            {
                return "hevc_nvenc";
            }

            if (vendor.IndexOf("AMD", StringComparison.OrdinalIgnoreCase) >= 0 && encoders.Contains("hevc_amf"))
            {
                return "hevc_amf";
            }

            if (vendor.IndexOf("Intel", StringComparison.OrdinalIgnoreCase) >= 0 && encoders.Contains("hevc_qsv"))
            {
                return "hevc_qsv";
            }

            throw new NotSupportedException("No compatible hardware HEVC encoder is available in FFmpeg.");
        }

        private static string ReadEncoderList(string executable)
        {
            var info = CreateStartInfo(executable, "-hide_banner -encoders");
            info.RedirectStandardOutput = true;
            using (Process process = Process.Start(info))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("FFmpeg encoder detection did not start.");
                }

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
        }

        private static string GetVideoOptions(string encoder)
        {
            switch (encoder)
            {
                case "h264_nvenc":
                    return "-c:v h264_nvenc -preset p7 -tune lossless -pix_fmt yuv444p";
                default:
                    return "-c:v ffv1 -level 3 -coder 1 -context 1 -g 1 -pix_fmt bgra";
            }
        }

        private static string GetHevcOptions(string encoder)
        {
            switch (encoder)
            {
                case "hevc_nvenc":
                    return "-c:v hevc_nvenc -preset p7 -tune hq -rc vbr -cq 16 -b:v 0";
                case "hevc_amf":
                    return "-c:v hevc_amf -quality quality -rc cqp -qp_i 16 -qp_p 18";
                case "hevc_qsv":
                    return "-c:v hevc_qsv -preset slow -global_quality 16";
                default:
                    throw new NotSupportedException($"Unsupported HEVC encoder: {encoder}");
            }
        }

    }
}
