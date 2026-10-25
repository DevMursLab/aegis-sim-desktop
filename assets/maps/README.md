# Synthetic layer provenance

`synthetic-region.geojson` was authored for this project; it contains fictional feature names and simple invented geometry around longitude/latitude zero. It does not describe actual shelters, depots, roads or response facilities. No external map data, tiles or services are used.

The viewer accepts a bounded subset of [RFC 7946](https://www.rfc-editor.org/rfc/rfc7946.html): WGS84 longitude/latitude FeatureCollection, string feature IDs, a printable `properties.name`, and 2D Point, LineString or simple single-ring Polygon geometry. Foreign properties are ignored. Duplicate JSON members are rejected. Polygon winding is accepted in either direction; holes, multipart geometries, altitude, legacy CRS, self-intersections, antimeridian crossings and regions wider/taller than two degrees are rejected.

File limit: 512 KB UTF-8. At most 128 features, 4096 positions overall and 256 polygon vertices plus closure. Longitude is bounded to ±180 and latitude to ±85. Projection is regional equirectangular with longitude scaled by cosine of the region's central latitude, uniformly fitted into a separate 1200×800 display plane with north up. This is a visualization adapter, not a geodesic measurement or routing engine.

Simulation x/y coordinates retain their original local meaning and are never interpreted as geographic positions. Layer state is view-only and is not saved with runs. Loading the layer cannot change a replay checksum.


## Integrated exercise (3.0)

`road-exercise.geojson` is also invented around zero longitude/latitude. Unlike the separate layer viewer, import of this exercise creates a schema 4 simulation. A root `exercise` object defines name/capacity/stock/fleetSize/metersPerTick. Point properties declare `kind` as depot, junction or shelter; shelter properties add demand/priority. Road LineStrings declare from/to point feature IDs and must match their endpoints. Exactly one depot and a connected path to every shelter are required. No polygon is accepted in an exercise.

Road meters are the ceiling of summed spherical segment distances (Earth radius 6371000 m). Dijkstra uses positive integer edge weights, ordinal node/road IDs for deterministic ties and source shelter order for equal priorities. Roads are undirected; paths are frozen at import. Geographic inputs retain the viewer's 512 KB/128-feature/4096-position bounds; compiled JSON is capped at 256 KB and each journey at 20 ticks. Choose metersPerTick accordingly. Full projected paths, distance metadata and decoded UTF-8 source SHA-256 enter the saved scenario and state hash. Exported XML stores the geographic metadata as JSON text inside a Geography element; it is not GML.

The projection is for display only; distance calculation uses the original WGS84 positions before projection. No external map data or service is used. [Bangla lesson 16](../../docs/learning/16-road-network.bn.md).
