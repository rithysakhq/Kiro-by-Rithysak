# PAK Name Recovery Notes

## Scope

This note investigates why sidecar-less unpacking produces files named `_ID_<hash>` with no extension instead of original names such as `.ini`, `.txt`, `.lua`, `.spr`, and similar game resource paths.

Reference data was inspected read-only from:

```text
C:\Users\mike\Downloads\JX2 Sabay\JXII-V.16.0 Client\Data
```

## Short Answer

The modern app is not converting `.ini`, `.txt`, `.lua`, or resource files into a generic `File` type. The extracted payload bytes are still present.

The missing piece is the original path string. A JX2 PAK stores each file under a numeric ID/hash in the archive index. The original path and filename are preserved by the matching TXT sidecar manifest. If that sidecar is missing, the extractor can still recover file contents, but it cannot reliably recover the original filename or extension from the PAK index alone.

Windows shows the extracted entries as type `File` because their fallback names have no extension.

The current app now improves that sidecar-less fallback in two stages. First it looks for a reference `_unpacked_pak\<pak name>\_manifest.tsv` near the selected client data and uses that to restore exact known paths. If no reference manifest exists, it keeps the truthful `_ID_<hash>` stem and adds conservative inferred extensions only when content signatures or text patterns are clear.

## Confirmed Findings

- No matching TXT sidecar manifests were found beside the PAKs in the inspected `Data` folder.
- Existing extracted folders in `Data` already use `_ID_<hash>` names with no file extensions.
- The PAK header starts with magic `PACK`.
- The PAK header/index exposes file count, index offset, package CRC/time, and per-file records.
- Each index entry stores an ID/hash, data offset, unpacked size, packed size, and flags.
- The PAK index does not store original path strings.
- The current modern tool intentionally writes a temporary synthetic TXT manifest when no real TXT exists.
- That synthetic manifest uses names like `\_ID_01714fdf`, allowing the legacy extractor to recover contents without pretending it knows the original names.
- After synthetic extraction, the app first tries exact recovery from `_manifest.tsv`, for example moving `_ID_01714fdf` to `settings\item\lifeskill\zhuangtai.txt`.
- The preflight panel reports how many manifest entries have known paths and how many are manifest-unmapped before extraction starts.
- Exact recovery stages generated-ID extraction in a temp folder, then writes only final recovered files and manifest-unmapped `_unknown_by_id` entries into the requested output.
- If exact recovery data is unavailable, the app inspects generated-ID files and renames only high-confidence matches, for example `_ID_01714fdf.txt`, `_ID_00aa5df3.lua`, `_ID_022986ad.spr`, or `_ID_0c4a456c.asf`.
- `engine.dll` itself contains a legacy fallback string matching the same idea: `\_-ID-_%08x`.
- `engine.dll` exports hash-related APIs such as `g_FileNameHash`, `g_StringHash`, and `g_StringLowerHash`.

## Client Data Snapshot

PAK index counts:

| PAK | PAK bytes | Index file count | Index valid |
| --- | ---: | ---: | --- |
| `font.pak` | 1,748,418 | 2 | Yes |
| `image.pak` | 580,897,553 | 17,903 | Yes |
| `maps_c.pak` | 72,209,886 | 54,866 | Yes |
| `music.pak` | 69,655,057 | 31 | Yes |
| `resource.pak` | 217,527,463 | 1,816 | Yes |
| `script_c.pak` | 630,297 | 952 | Yes |
| `settings_c.pak` | 954,135 | 243 | Yes |
| `sound.pak` | 52,459,082 | 754 | Yes |
| `spr.pak` | 955,239 | 211 | Yes |
| `Ui.pak` | 607,407 | 394 | Yes |
| `UImage.pak` | 21,643,801 | 2,280 | Yes |
| `Update_c.pak` | 854,450,641 | 18,754 | Yes |

Existing extracted folder checks:

| Folder | Files | Extensionless files | Folder bytes | Matching PAK bytes | Expansion |
| --- | ---: | ---: | ---: | ---: | ---: |
| `font` | 2 | 2 | 4,989,048 | 1,748,418 | 2.85x |
| `settings_c` | 243 | 243 | 6,495,125 | 954,135 | 6.81x |
| `Update_c` | 18,754 | 18,754 | 1,335,418,537 | 854,450,641 | 1.56x |

The expanded folders being larger than the PAKs is expected. PAK files store compressed payloads. The folders contain unpacked payload bytes.

## Content Checks

The extracted files are not blank or converted into a different internal format:

- `settings_c\_ID_01714fdf` begins with tab-delimited text columns such as `ItemLevel`, `ExpPower`, and `ComposeOdds`.
- `settings_c\_ID_03d22805` begins with tab-delimited item data and includes resource references such as `\image\item\shoe\...spr`.
- `font\_ID_0c4a456c` starts with `ASF`, which looks like a proprietary binary font/resource signature.
- Some `Update_c` files are binary resources and may include embedded path references, but those embedded strings are not proof of that file's own original filename.

This supports the conclusion that extraction is recovering payload data, while original filenames/extensions are unavailable.

## Inferred Behavior

Confidence: high.

The numeric ID in each PAK entry is likely produced by the legacy filename hashing routine, probably through `g_FileNameHash` or a closely related path normalization plus hash function. That hash lets the game locate resources by requested path at runtime, but it is effectively one-way for recovery purposes.

For example, if the original path was something like:

```text
\settings\item\example.txt
```

the PAK only needs the resulting numeric ID to serve the file. Once packed without keeping the sidecar manifest, the original path text is no longer present in the PAK index.

## Hypotheses

Confidence: medium.

- The earlier `openFileDialog3.txt` behavior is probably a legacy UI/default manifest-list filename, not a universal magic file.
- That TXT was important because it carried the file list/path layer for the archive operation.
- A valid sidecar can restore real names because it maps each archive ID back to a path string.
- A made-up sidecar can only choose made-up names, which is what the modern tool does with `_ID_<hash>`.

## Unknowns

- The exact original paths for the sidecar-less Sabay client PAKs are not recoverable from the inspected PAK files alone.
- The exact path normalization rules used before hashing still need deeper verification if we build a dictionary-based resolver.
- Some file extensions can be guessed from content signatures, but that does not recover original folders or filenames.
- A guessed extension can be wrong, especially for legacy Chinese game assets and byte-sensitive config/script files.

## What Would Make Original Names Work

Best case:

- Place the original matching sidecar beside the archive.
- The modern app already checks for `<archive>.pak.txt`.
- It also accepts the alternate `<archive>.txt` form.
- With a real sidecar, the legacy extractor can write original paths instead of `_ID_<hash>` fallback names.

Good recovery case:

- Find another client/build/source tree where the same resources exist as loose files or where matching TXT manifests still exist.
- Hash candidate paths with the same legacy hash routine.
- Match candidate hashes against PAK IDs.
- Rebuild a high-confidence manifest from those matches.

Weak recovery case:

- Guess extensions from file content.
- This can produce nicer-looking files, but it does not prove original names.
- This should be treated as an optional report or copy-to-new-folder workflow, not an automatic rename of existing extracted files.

## Implemented App Direction

The current fallback behavior is technically conservative:

- If a real manifest exists, use it and recover original names.
- If no real TXT manifest exists, look for a reference `_manifest.tsv` from a prior verified unpack.
- If a reference manifest exists, restore known original paths directly after extraction.
- If no reference manifest exists, extract bytes using `_ID_<hash>` names and add inferred extensions only for known signatures or clear text patterns.
- Leave manifest-unmapped files under `_unknown_by_id`, or extensionless in fallback mode, instead of inventing fake names.
- Write a recovery report only for extension-inference fallback, not for exact-reference recovery.

A future exact "Name Recovery" feature would still need external evidence:

- `Manifest mode`: use exact TXT sidecar names.
- `Dictionary mode`: recover names only when a candidate path hashes to the exact PAK ID.
- `Signature report mode`: identify probable content types without renaming files.
- `Export recovered copy`: write recovered/guessed full names to a separate output folder.

## Practical Conclusion

The app is not failing to unpack the files. It is correctly recovering file contents but lacks the missing filename manifest for the Sabay client archives.

Without the sidecar TXT or a verified external candidate path list, original filenames are unknown. With the provided `_unpacked_pak` reference tree, the app can now recover known original paths for matching archives and only falls back to `_ID_<hash>.<inferred extension>` when no exact mapping is available.

## Validation Snapshot

The exact-recovery path was tested against the reference `_unpacked_pak` tree. Smaller and medium archives were fully SHA-256 checked; large archives were checked by full path/count/size plus deterministic sample hashes.

| PAK | Result |
| --- | --- |
| `settings_c.pak` | 243 payload files, 239 named after one verified override, 4 unmapped, no root `_ID_*`, no mismatches in checked files. |
| `script_c.pak` | 952 payload files, exact match to reference, no root `_ID_*`, no report/manifest traces. |
| `Ui.pak` | 394 payload files, exact match to reference, no root `_ID_*`, no report/manifest traces. |
| `font.pak` | 2 payload files, exact match to reference, preserved as manifest-unmapped IDs. |
| `spr.pak` | 211 payload files, exact match to reference, preserved as manifest-unmapped IDs. |
| `UImage.pak` | 2,280 payload files, exact match to reference, no root `_ID_*`, no report/manifest traces. |
| `resource.pak` | 1,816 payload files, exact match to reference, no root `_ID_*`, no report/manifest traces. |
| `music.pak`, `sound.pak`, `image.pak`, `maps_c.pak` | Full path/count/size checks passed; deterministic hash samples matched; no root `_ID_*`, no report/manifest traces. |
| `Update_c.pak` | 18,754 payload files, 5,512 named, 13,242 manifest-unmapped, full path/count/size checks passed, 225 deterministic hash samples matched, no root `_ID_*`, no report/manifest traces. |
