# Research evidence for DocumentedWindowsTweaks

These are local research snapshots retrieved from Microsoft Learn during the 15–16 November 2026 audit. They are evidence, not runtime dependencies. `NEW_TWEAK_SOURCES.md` contains the source URLs and applicability caveats.

`tmp_new_tweaks_selfcheck.json` independently matches all 50 new entries against the local Windows administrative templates by hive scope, key and value name, and includes matching policy XML. Repeated matches in the JSON are duplicate extraction of the same policy's root value name and element value name; they are not duplicate catalogue entries. Numeric values were checked against the ADMX enabled/disabled values or enum/range. In particular, SettingSync disable values are 2, AppPrivacy ForceDeny is 2, and PreventIndexingUncachedExchangeFolders is inverted (1 disables indexing).

`tmp_admx.json` contains selected Windows policy mappings used during research. Other JSON/TXT files are stripped Microsoft Learn page content. Edge pages were investigated but no Edge policies were added because version/profile applicability requires additional product detection. Some investigated policy pages were excluded entirely.

The Delivery Optimization source explicitly defines the background bandwidth limit in KB/s, minimum RAM in GB, monthly Internet-peer upload cap in GB, and absolute cache limit in GB. The 1024, 8, 1 and 5 values therefore use these respective units.

These checks prove the schema and documented intent. They do not prove an FPS benefit, actual component presence, policy precedence, or runtime consumption of the value on every Windows build. No system tuning was applied during the audit.
