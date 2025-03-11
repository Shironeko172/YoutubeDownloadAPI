
## Youtube Downloader API

REST API to download `.mp3` and `.mp4` files.

---

### First Thing You Need
- .Net Framework 9.0

---

### How To Use
- Clone this repository
- Open Command Prompt
- Select the folder where you cloned the repo
- Insert `dotnet run` and execute it
- Copy the base URL and run it using Postman or another app

### Methods:

[Video Info](#get-video-info) |
[Download .mp4](#get-download-mp4-file) |
[Download .mp3](#get-download-mp3-file)

---

#### `GET` Video Info

**endpoint:** `BaseURL/api/youtube/info?url=value`

| Query | Type   | Description      |
| ----- | ------ | ---------------- |
| `url` | string | youtube url link |

**Response:**

```json
{
    "title": "Video title",
    "author": "Youtube Channel Name",
    "thumbnail": "Video thumbnail url",
    "qualities": [
        {
            "label": "Video Quality",
            "bitrate": "Bitrate of the video",
            "url": "URL where the video is saved"
        }
}
```

---

#### `GET` Download `mp4` file

**endpoint:** `BaseURL/api/youtube/downloadvideo?url=value&quality=value`

| Query | Type   | Description      |
| ----- | ------ | ---------------- |
| `url` | string | youtube url link |
| `quality` | string | qualities of the video, like 1080p |

**Response:**

`<video-name>.mp4` file

---

#### `GET` Download `mp3` file

**endpoint:** `BaseURL/api/youtube/downloadaudio?url=value`

| Query | Type   | Description      |
| ----- | ------ | ---------------- |
| `url` | string | youtube url link |

**Response:**

`<video-name>.mp3` file

---

_By Shironeko_
