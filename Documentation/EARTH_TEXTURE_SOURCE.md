# Earth texture provenance
Natural Earth II NE2_LR_LC, version 3.2.0. Natural Earth raster and vector data are public domain.
- Terms: https://www.naturalearthdata.com/about/terms-of-use/
- Raster: https://naturalearth.s3.amazonaws.com/10m_raster/NE2_LR_LC.zip
- Coastline/land polygons: https://raw.githubusercontent.com/nvkelso/natural-earth-vector/master/geojson/ne_10m_land.geojson
- Overview: https://www.naturalearthdata.com/downloads/10m-raster-data/10m-natural-earth-2/

Output: Assets/MonetGarden/Rendering/Earth_Impressionist_Albedo.png, 4096×2048 PNG, full -180° to +180° longitude, north at top. Geographical land polygons supply the mask; the source raster supplies ecological colour variation. Deterministic multilayer impasto strokes recolour land gold/green/ochre and ocean indigo/blue/teal without crossing coastlines. Strokes wrap periodically; first and last columns are identical.

This is programmatic image processing, not AI image generation. The user's perspective Earth reference supplies artistic palette direction, not sphere UV data. Fine impasto detail is a procedural approximation and does not reproduce all the reference's hand-painted richness.
Importer: sRGB, max size 4096, uncompressed, mipmaps, trilinear, U repeat / V clamp, anisotropic 4. Existing Unity sphere UV and longitude mapping retained. Pins remain independent child objects.
Original painting is separate and untouched; no generated painting substitutes it.
Reproducible source script is Documentation/Tools/round2_earth.py. Its downloaded source files remain in the Codex work folder.
