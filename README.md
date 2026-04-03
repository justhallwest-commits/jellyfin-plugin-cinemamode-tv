# Cinema Mode TV - Jellyfin Plugin

A Jellyfin plugin that enables Cinema Mode functionality with local trailers and pre-rolls for **both Movies AND TV Shows**. This plugin uses Jellyfin's built-in `IIntroProvider` system, so it works across all clients that support Cinema Mode (Web, Android, iOS, TV apps, etc.) without any client-side modifications.

## Features

- **Movies & TV Shows Support**: Play pre-rolls and trailers before both movies AND TV show episodes
- **Universal Client Support**: Works on all Jellyfin clients that call the intro endpoint
- **Three Intro Types**:
  1. **Trailer Pre-Roll**: Plays before trailers (e.g., "Now playing on Jellyfin")
  2. **Trailers**: Configurable number of trailers from your library
  3. **Feature Pre-Roll**: Plays right before your content (e.g., "Feature Presentation")
- **Separate Trailer Counts**: Configure different trailer counts for Movies vs TV Shows
- **Smart Selection Rules**: Match pre-rolls based on:
  - Content name, year, decade
  - Genre matching
  - Studio matching
  - Seasonal tags (Halloween, Christmas, etc.)
- **Rating Enforcement**: Ensure trailers match the content's parental rating
- **User Control**: Users can disable Cinema Mode in their playback settings

## Installation

### Method 1: Add Custom Repository (Recommended)

1. In Jellyfin, go to **Dashboard** → **Plugins** → **Repositories**
2. Click the **+** button to add a new repository
3. Enter any name you like (e.g., "Cinema Mode TV")
4. Paste this URL: `https://raw.githubusercontent.com/justhallwest-commits/jellyfin-plugin-cinemamode-tv/main/manifest.json`
5. Click **Save**
6. Go to the **Catalog** tab
7. Find "Cinema Mode TV" under General
8. Click **Install**
9. **Restart Jellyfin-1** from the Unraid Docker tab (click the container icon → **Restart**)

### Method 2: Manual Installation (Unraid Docker)

These paths assume a Jellyfin Docker container named **Jellyfin-1** on Unraid. Adjust the container name if yours differs.

1. Download the latest release ZIP from the [Releases](https://github.com/justhallwest-commits/jellyfin-plugin-cinemamode-tv/releases) page
2. Open the Unraid terminal (or SSH in) and create the plugin folder:
   ```bash
   mkdir -p /mnt/user/appdata/jellyfin-1/plugins/CinemaModeTV
   ```
3. Extract the DLL into that folder:
   ```bash
   unzip cinemamode-tv-v1.0.0.0.zip -d /mnt/user/appdata/jellyfin-1/plugins/CinemaModeTV/
   ```
4. Copy the `meta.json` file from this repo into the same folder:
   ```bash
   cp meta.json /mnt/user/appdata/jellyfin-1/plugins/CinemaModeTV/
   ```
5. Your plugin folder should contain:
   ```
   /mnt/user/appdata/jellyfin-1/plugins/CinemaModeTV/
   ├── Jellyfin.Plugin.CinemaModeTV.dll
   └── meta.json
   ```
6. **Restart Jellyfin-1** from the Unraid Docker tab (click the container icon → **Restart**)

> **Tip:** If you're unsure of your appdata path, click the **Jellyfin-1** container in Unraid's Docker tab, then **Edit**. Look for the `/config` mapping — the host side is your appdata root (e.g., `/mnt/user/appdata/jellyfin-1`). The plugins folder lives inside that at `plugins/`.

## Configuration

### Setting Up Pre-Roll Libraries

1. Create separate libraries in Jellyfin for your pre-roll videos:
   - **Movies Library** type: Required since Jellyfin needs a content type
   - Name them something like "Trailer Pre-Rolls" and "Feature Pre-Rolls"

2. Tag your pre-rolls with metadata to enable smart matching:
   - **Name Tag**: Tag with movie/show name for specific matches
   - **Year Tag**: Tag with year (e.g., "2024") for year-specific pre-rolls
   - **Decade Tag**: Tag with decade (e.g., "2020s") for decade-specific pre-rolls
   - **Seasonal Tags**: Create seasonal definitions (see below)

### Plugin Settings

1. Go to **Dashboard** → **Plugins** → **Cinema Mode TV**
2. Configure content types, pre-roll libraries, trailer counts, selection rules, and seasonal tags

**Playback Order:** Trailer Pre-Roll → Trailers → Feature Pre-Roll → Your Movie/TV Show

**Skip Anytime**: Users can skip any pre-roll or trailer by pressing the **Next** button.

## Building from Source

1. Clone this repository
2. Install .NET 9.0 SDK
3. Build with:
   ```bash
   dotnet publish Jellyfin.Plugin.CinemaModeTV/Jellyfin.Plugin.CinemaModeTV.csproj --configuration Release --output bin
   ```
4. The plugin DLL will be in the `bin` folder

## Compatibility

- **Jellyfin Version**: 10.11.0+
- **Target Framework**: .NET 9.0
- **Clients**: All clients that support Cinema Mode intros

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Credits

Based on the original [jellyfin-plugin-cinemamode](https://github.com/CherryFloors/jellyfin-plugin-cinemamode) by CherryFloors.
