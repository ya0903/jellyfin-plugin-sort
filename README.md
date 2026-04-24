# Jellyfin Rating Sort

Sort Jellyfin movies and TV shows by IMDb and Letterboxd ratings.

Rating Sort fetches ratings from MDBList and writes them into Jellyfin's built-in sortable rating fields:

- IMDb rating -> Jellyfin `CommunityRating` as a `0-10` value.
- Letterboxd rating -> Jellyfin `CriticRating` as a `0-100` value.

Because the values are stored in Jellyfin's own metadata fields, sorting stays server-side and works with normal Jellyfin pagination. The optional web UI integration only changes the visible labels from `Community Rating` and `Critic Rating` to `IMDb Rating` and `Letterboxd Rating`.

Missing external ratings are written as `0`, which keeps unrated items at the bottom when sorting by rating descending.

## Requirements

- Jellyfin Server `10.11.x`.
- An MDBList API key.
- Optional: File Transformation plugin for automatic Jellyfin Web label changes.

You do not need the .NET SDK to install the plugin from Jellyfin or from a GitHub release zip. The SDK is only needed if you want to build the plugin from source.

## Install

In Jellyfin, add this plugin repository:

```text
https://raw.githubusercontent.com/ya0903/jellyfin-plugin-sort/main/manifest.json
```

Then go to:

```text
Dashboard -> Plugins -> Catalog -> Rating Sort
```

Install the plugin, then restart Jellyfin.

## Manual Install

Download the release zip:

```text
https://github.com/ya0903/jellyfin-plugin-sort/releases/download/v1.0.0/RatingSort_1.0.0.0.zip
```

Extract it to a Jellyfin plugin folder:

- Windows: `%ProgramData%\Jellyfin\Server\plugins\Rating Sort_1.0.0.0\`
- Linux: `/var/lib/jellyfin/plugins/Rating Sort_1.0.0.0/`
- Docker: `/config/plugins/Rating Sort_1.0.0.0/`

Restart Jellyfin after extracting the zip.

## Configure

Open:

```text
Dashboard -> Plugins -> Rating Sort
```

Set your MDBList API key, choose the movie/show libraries to update, then run `Refresh Now`.

If no libraries are selected, the plugin updates all movie and TV show libraries.

## Sorting In Jellyfin

After a refresh completes:

- Sort by `Community Rating` to sort by IMDb.
- Sort by `Critic Rating` to sort by Letterboxd.

If UI label integration is enabled, Jellyfin Web will show those options as `IMDb Rating` and `Letterboxd Rating` instead.

## Web UI Labels

The plugin has three label modes:

- `Auto/File Transformation`: automatically injects a small label-renaming script when File Transformation is installed.
- `Manual JS snippet`: shows a script on the plugin admin page that can be used with JavaScript Injector or a userscript manager.
- `Disabled`: sorting still works, but Jellyfin Web keeps its normal rating labels.

The web script does not add sorting logic. It only renames labels. The actual sort order comes from Jellyfin's server-side item metadata.

## Refresh Behavior

The plugin updates movies and series-level TV show entries. It does not update individual episodes.

Before changing an item for the first time, the plugin stores the original `CommunityRating` and `CriticRating` values in its plugin data folder.

If another ratings plugin also writes to these fields, make sure the mappings match:

- `CommunityRating`: IMDb.
- `CriticRating`: Letterboxd.

## Restore Original Ratings

Use `Restore Original Ratings` from the plugin page to put previously stored Jellyfin rating values back.

Restore only affects items that Rating Sort has backed up before editing.

## Troubleshooting

If sorting looks wrong, run `Refresh Now` again and wait for it to finish. Items without MDBList data should become `0` so they sort below rated items.

If labels do not change in Jellyfin Web, sorting still works. Check that File Transformation is installed and the plugin label mode is set to `Auto/File Transformation`, or use the manual JS snippet mode.

If many items are skipped, check that the items have TMDb provider IDs and that the MDBList API key is valid.

## Build From Source

Building requires the .NET SDK `9.0`.

```powershell
dotnet test Jellyfin.Plugin.RatingSort.sln -c Release
.\scripts\package.ps1
```

The packaged zip is created in `dist`.

## Release Notes

The Jellyfin repository manifest is served from:

```text
https://raw.githubusercontent.com/ya0903/jellyfin-plugin-sort/main/manifest.json
```

Each `manifest.json` version entry should point at an immutable GitHub release asset, for example:

```text
https://github.com/ya0903/jellyfin-plugin-sort/releases/download/v1.0.0/RatingSort_1.0.0.0.zip
```

When making a new release, update the project version, run `scripts/package.ps1`, create the matching GitHub release/tag, upload the zip from `dist`, then update `manifest.json` with the release asset URL, MD5 checksum, version, and timestamp.

## API

These endpoints are intended for the plugin admin page:

- `POST /RatingSort/Refresh`
- `POST /RatingSort/Restore`
- `GET /RatingSort/Status`
- `GET /RatingSort/Libraries`
- `GET /RatingSort/WebScript`
