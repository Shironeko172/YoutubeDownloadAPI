using Microsoft.AspNetCore.Mvc; // Import ASP.NET Core MVC For API Handling
using YoutubeExplode; // Import YoutubeExplode for fetching video details
using YoutubeExplode.Videos.Streams; // Import stream utilities to access video/audio streams
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode.Videos;
using YoutubeExplode.Common;

[Route("api/[controller]")] //define the route for the controller
[ApiController] // Mark this class as an API Controller
public class YoutubeController : ControllerBase
{
    private readonly YoutubeClient _YoutubeClient = new YoutubeClient(); // Create a new instance of YoutubeClient

    [HttpGet("info")]
    public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
    {
        try
        {
            var video = await _YoutubeClient.Videos.GetAsync(url);
            var streamManifest = await _YoutubeClient.Videos.Streams.GetManifestAsync(video.Id);
            var qualities = streamManifest.GetVideoStreams().Select(s => new { s.VideoQuality.Label, s.Bitrate, s.Url});
            var thumbnailUrl = video.Thumbnails.OrderByDescending(s => s.Resolution.Area).FirstOrDefault()?.Url;
            return Ok(new { Title = video.Title, Author = video.Author.ChannelTitle, Thumbnail = thumbnailUrl, Qualities = qualities });
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message});
        }
    }

    [HttpGet("downloadvideo")]
    public async Task<IActionResult> DownloadVideo([FromQuery] string url, [FromQuery] string quality)
    {
        try
        {
            var video = await _YoutubeClient.Videos.GetAsync(url);
            var streamManifest = await _YoutubeClient.Videos.Streams.GetManifestAsync(video.Id);
            var streamInfo = streamManifest.GetVideoStreams().FirstOrDefault(s => s.VideoQuality.Label == quality);
            if (streamInfo == null)
            {
                return BadRequest("Quality not found");
            }
            var stream = await _YoutubeClient.Videos.Streams.GetAsync(streamInfo);
            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            return File(memoryStream, "video/mp4", $"{video.Title}.mp4");
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
            var video = await _YoutubeClient.Videos.GetAsync(url);
            var streamManifest = await _YoutubeClient.Videos.Streams.GetManifestAsync(video.Id);
            var audioStreamInfo = streamManifest.GetAudioStreams().OrderByDescending(s => s.Bitrate).FirstOrDefault();
            if (audioStreamInfo == null)
            {
                return BadRequest("No audio stream available!");
            }

            var stream = await _YoutubeClient.Videos.Streams.GetAsync(audioStreamInfo);
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
}