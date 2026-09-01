# Jellyfin.Plugin.BlueIris

A Jellyfin server plugin that links Blue Iris security camera streams and clips.

## What it does

- Adds a **BlueIris** plugin configuration page.
- Stores the Blue Iris server URL, credentials, allowed cameras, and stream type.
- Adds a **Blue Iris Cameras** page that shows live HLS or MJPEG streams from the Blue Iris web server.
- Provides a `BlueIrisClient` that calls the Blue Iris `/json` `camlist` endpoint.
- Builds stream URLs for `/h264/{cam-short-name}/temp.m3u8` and `/mjpg/{cam-short-name}/video.mjpg`.
- Builds and fetches snapshot URLs for `/image/{cam-short-name}`.
- Includes xUnit tests with a mocked `HttpClient`.

## Build

```bash
dotnet build Jellyfin.Plugin.BlueIris.sln
dotnet test Jellyfin.Plugin.BlueIris.Tests/Jellyfin.Plugin.BlueIris.Tests.csproj
```

## Install

1. Build the plugin.
2. Zip the contents of `Jellyfin.Plugin.BlueIris/bin/Release/net8.0/` with `meta.json`.
3. Upload the zip through the Jellyfin dashboard under **Plugins > Catalog > My Plugins**.
4. Configure the plugin from **Dashboard > Plugins > BlueIris**.
5. Open **Dashboard > Plugins > Blue Iris Cameras** to view the live streams.

## Configuration

- **Blue Iris URL** — base URL of the Blue Iris web server, e.g. `http://192.168.5.5:81`
- **Username** / **Password** — Blue Iris web credentials
- **Allowed Cameras** — one camera short name per line or comma separated
- **Stream Type** — `HLS` or `MJPEG`
- **Restrict to Admins** — limit the cameras page to Jellyfin administrators

## API controller gap

A stub `BlueIrisController.cs` is included under `Api/`. It requires the `Jellyfin.Api` package to compile, which this P0-1 project does not reference. The `BlueIrisController` is therefore excluded from the build and the Blue Iris operations are exposed through `BlueIrisApiService` instead. To enable the controller later:

1. Add `<PackageReference Include="Jellyfin.Api" Version="10.9.11" />` to the main csproj.
2. Remove the `<Compile Remove="Api\BlueIrisController.cs" />` line.
3. Register `BlueIrisClient` with Jellyfin's dependency injection.

## Next Steps

- P0-2: wire `bi-mcp` for richer camera status and live snapshots.
- Add clip library folder scanning for recorded Blue Iris alerts.
- Convert `BlueIrisApiService` into the Jellyfin `IChannel` or `ILiveTvService` surface once the core integration is stable.
