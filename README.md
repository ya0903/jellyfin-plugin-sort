# Jellyfin Rating Sort

Jellyfin plugin for sorting movies and series by external ratings.

The plugin fetches ratings from MDBList and writes them into Jellyfin's existing sortable fields:

- IMDb -> `CommunityRating` as a `0-10` value.
- Letterboxd -> `CriticRating` as a `0-100` value.

Jellyfin already supports sorting by these fields, so sorting remains server-side and works with normal pagination. The optional web script only renames the visible sort labels in Jellyfin Web.

## Requirements

- Jellyfin Server `10.11.x`.
- .NET SDK `9.0` to build.
- MDBList API key.
- Optional: File Transformation plugin for automatic Jellyfin Web label injection.

## Install From Jellyfin

Add this plugin repository in Jellyfin:

```text
https://raw.githubusercontent.com/ya0903/jellyfin-plugin-sort/main/manifest.json
```

Then install it from:

```text
Dashboard -> Plugins -> Repositories
Dashboard -> Plugins -> Catalog -> Rating Sort
```

Restart Jellyfin after installing the plugin.

## Manual Install

Download the packaged plugin zip:

```text
https://raw.githubusercontent.com/ya0903/jellyfin-plugin-sort/main/dist/RatingSort_1.0.0.0.zip
```

Extract it into a plugin folder under your Jellyfin plugins directory.

Common locations:

- Windows: `%ProgramData%\Jellyfin\Server\plugins\Rating Sort_1.0.0.0\`
- Linux: `/var/lib/jellyfin/plugins/Rating Sort_1.0.0.0/`
- Docker: `/config/plugins/Rating Sort_1.0.0.0/`

Restart Jellyfin after extracting the zip.

## Build

```powershell
dotnet test Jellyfin.Plugin.RatingSort.sln -c Release
.\scripts\package.ps1
```

The Jellyfin repository manifest points at:

```text
dist/RatingSort_1.0.0.0.zip
```

When making a new release, update `Version` in the project file, run `scripts/package.ps1`, then update `manifest.json` with the new zip name, MD5 checksum, version, and timestamp.

## Configure

Open `Dashboard -> Plugins -> Rating Sort`.

Set:

- MDBList API key.
- Movie/show libraries to include, or leave all unchecked for all movie/show libraries.
- Refresh interval and request delay.
- Web UI label mode:
  - `Auto/File Transformation`: register an in-memory `index.html` transform when File Transformation is installed.
  - `Manual JS snippet`: use the `/RatingSort/WebScript` output with JavaScript Injector or a userscript manager.
  - `Disabled`: backend sorting still works, but labels remain Jellyfin's defaults.

Then run `Refresh Now`, or run the scheduled task named `Update IMDb and Letterboxd sort ratings`.

If you also use another ratings plugin that writes to `CommunityRating` or `CriticRating`, make sure its mapping matches this plugin:

- Movies/series Community Rating: IMDb.
- Movies/series Critic Rating: Letterboxd.

## Restore

Before the plugin changes an item for the first time, it stores the original `CommunityRating` and `CriticRating` values in the plugin data folder. Use `Restore Original Ratings` from the plugin page to put those values back.

## API

- `POST /RatingSort/Refresh`
- `POST /RatingSort/Restore`
- `GET /RatingSort/Status`
- `GET /RatingSort/Libraries`
- `GET /RatingSort/WebScript`
