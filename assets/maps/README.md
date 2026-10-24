# Synthetic layer provenance

`synthetic-region.geojson` was authored for this project; it contains fictional feature names and simple invented geometry around longitude/latitude zero. It does not describe actual shelters, depots, roads or response facilities. No external map data, tiles or services are used.

The viewer accepts a bounded subset of [RFC 7946](https://www.rfc-editor.org/rfc/rfc7946.html): WGS84 longitude/latitude FeatureCollection, string feature IDs, a printable `properties.name`, and 2D Point, LineString or simple single-ring Polygon geometry. Foreign properties are ignored. Duplicate JSON members are rejected. Polygon winding is accepted in either direction; holes, multipart geometries, altitude, legacy CRS, self-intersections, antimeridian crossings and regions wider/taller than two degrees are rejected.

File limit: 512 KB UTF-8. At most 128 features, 4096 positions overall and 256 polygon vertices plus closure. Longitude is bounded to ±180 and latitude to ±85. Projection is regional equirectangular with longitude scaled by cosine of the region's central latitude, uniformly fitted into a separate 1200×800 display plane with north up. This is a visualization adapter, not a geodesic measurement or routing engine.

Simulation x/y coordinates retain their original local meaning and are never interpreted as geographic positions. Layer state is view-only and is not saved with runs. Loading the layer cannot change a replay checksum.
