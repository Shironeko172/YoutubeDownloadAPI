
## Youtube Downloader API

REST API to download `.mp3` and `.mp4` files.

---

### Methods:

[Video Info](#get-video-info) |
[Download .mp3](#get-download-mp3-file) |
[Download .mp4](#get-download-mp4-file)

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
