using Microsoft.AspNetCore.Mvc; // Import ASP.NET Core MVC For API Handling
using YoutubeExplode; // Import YoutubeExplode for fetching video details
using YoutubeExplode.Videos.Streams; // Import stream utilities to access video/audio streams
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Videos;
using YoutubeExplode.Common;
using System.IO.Compression;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

[Route("api/[controller]")] //define the route for the controller
[ApiController] // Mark this class as an API Controller
public class YoutubeController : ControllerBase
{
    private string CleanFileName(string title)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            title = title.Replace(c, '_');
        return title;
    }


    [HttpGet("info")]
    public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
    {
        try
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var qualities = streamManifest.GetVideoStreams().Select(s => new { s.VideoQuality.Label, s.Bitrate, s.Url });
            var thumbnailUrl = video.Thumbnails.OrderByDescending(s => s.Resolution.Area).FirstOrDefault()?.Url;
            return Ok(new { Title = video.Title, Author = video.Author.ChannelTitle, Thumbnail = thumbnailUrl, Qualities = qualities });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    [HttpGet("downloadvideo")]
    public async Task<IActionResult> DownloadVideo([FromQuery] string url, [FromQuery] string quality)
    {
        try
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

            //parse requested highest quality
            int.TryParse(new string(quality.Where(char.IsDigit).ToArray()), out int requestedHeight);

            // Get best matching video-only stream
            var streamInfo = streamManifest.GetVideoStreams().OrderByDescending(s => s.VideoResolution.Height).FirstOrDefault(s => s.VideoResolution.Height == requestedHeight) ?? streamManifest.GetVideoOnlyStreams().OrderByDescending(s => s.VideoResolution.Height).FirstOrDefault();
            if (streamInfo == null)
            {
                return BadRequest("Video stream not found for the requested quality.");
            }

            //Get Best available audio stream
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
            if (audioStreamInfo == null)
            {
                return BadRequest("Audio stream not found.");
            }

            //Temp Paths
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            var safeTitle = CleanFileName(video.Title);
            var videoPath = Path.Combine(tempDir, $"{safeTitle}_video.{streamInfo.Container.Name}");
            var audioPath = Path.Combine(tempDir, $"{safeTitle}_audio.{audioStreamInfo.Container.Name}");
            var outputPath = Path.Combine(tempDir, $"{safeTitle}.mp4");

            //Download stream to temp files
            await youtube.Videos.Streams.DownloadAsync(streamInfo, videoPath);
            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath);

            //Merge video and audio streams using ffmpeg
            var ffmpeg = "ffmpeg";
            var ffmpegArgs = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -strict experimental \"{outputPath}\"";
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpeg,
                    Arguments = ffmpegArgs,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            if (!System.IO.File.Exists(outputPath))
            {
                return BadRequest($"FFMPEG failed to merge files. log: {errorOutput}");
            }
            var resultBytes = await System.IO.File.ReadAllBytesAsync(outputPath);
            //cleanup
            Directory.Delete(tempDir, true);
            return File(resultBytes, "video/mp4", $"{safeTitle}.mp4");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

    [HttpGet("downloadaudio")]
    public async Task<IActionResult> DownloadAudio([FromQuery] string url)
    {
        try
        {
            var youtube = new YoutubeClient();
            var video = await youtube.Videos.GetAsync(url);
            var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
            var audioStreamInfo = streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
            if (audioStreamInfo == null)
            {
                return BadRequest("No audio stream available!");
            }

            var stream = await youtube.Videos.Streams.GetAsync(audioStreamInfo);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            return File(memoryStream, "audio/mp3", $"{video.Title}.mp3");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error: {ex.Message}");
        }
    }

    [HttpPost("multidownload")]
    public async Task<IActionResult> DownloadMultiple([FromBody] DownloadRequest[] requests)
    {
        const int maxDownloads = 5;
        if(requests.Length > maxDownloads)
        {
            return BadRequest($"You can only download a maximum of {maxDownloads} videos at a time.");
        }
        var youtube = new YoutubeClient();
        var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempFolder);
        var downloadFiles = new List<string>();
        try
        {
            foreach (var req in requests)
            {
                var video = await youtube.Videos.GetAsync(req.Url);
                var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
                var safeTitle = CleanFileName(video.Title);

                if (req.Type == "audio")
                {
                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
                    if (audioStreamInfo == null)
                    {
                        continue;
                    }

                    var audioPath = Path.Combine(tempFolder, $"{safeTitle}.mp3");
                    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath);
                    downloadFiles.Add(audioPath);
                }
                else // video
                {
                    int.TryParse(new string((req.Quality ?? "").Where(char.IsDigit).ToArray()), out int requestedHeight);

                    var videoStreamInfo = streamManifest
                        .GetVideoOnlyStreams()
                        .OrderByDescending(s => s.VideoResolution.Height)
                        .FirstOrDefault(s => s.VideoResolution.Height == requestedHeight)
                        ?? streamManifest.GetVideoOnlyStreams().OrderByDescending(s => s.VideoResolution.Height).FirstOrDefault();

                    var audioStreamInfo = streamManifest.GetAudioOnlyStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();

                    if (videoStreamInfo == null || audioStreamInfo == null)
                    {
                        continue;
                    }

                    var videoPath = Path.Combine(tempFolder, $"{safeTitle}_video.{videoStreamInfo.Container.Name}");
                    var audioPath = Path.Combine(tempFolder, $"{safeTitle}_audio.{audioStreamInfo.Container.Name}");
                    var outputPath = Path.Combine(tempFolder, $"{safeTitle}.mp4");

                    await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, videoPath);
                    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, audioPath);

                    // FFmpeg merge
                    var ffmpegArgs = $"-i \"{videoPath}\" -i \"{audioPath}\" -c:v copy -c:a aac -strict experimental \"{outputPath}\"";
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "ffmpeg",
                            Arguments = ffmpegArgs,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();

                    if (System.IO.File.Exists(outputPath))
                    {
                        downloadFiles.Add(outputPath);
                    }

                    // Clean up the split files
                    if (System.IO.File.Exists(videoPath)) System.IO.File.Delete(videoPath);
                    if (System.IO.File.Exists(audioPath)) System.IO.File.Delete(audioPath);
                }
            }

            if (!downloadFiles.Any())
                return BadRequest("No files were successfully downloaded!");

            var zipPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            ZipFile.CreateFromDirectory(tempFolder, zipPath);

            var zipBytes = await System.IO.File.ReadAllBytesAsync(zipPath);
            System.IO.File.Delete(zipPath);
            Directory.Delete(tempFolder, true);

            return File(zipBytes, "application/zip", "downloads.zip");
        }
        catch (Exception ex)
        {
            if (Directory.Exists(tempFolder))
            {
                Directory.Delete(tempFolder, true);
            }
            return BadRequest($"Error: {ex.Message}");
        }
    }
}

public class DownloadRequest
{
    [Required]
    public required string Url { get; set; }
    [Required]
    public required string Type { get; set; } // "audio" or "video"
    public string? Quality { get; set; } // e.g. "720p", "1080p"
}