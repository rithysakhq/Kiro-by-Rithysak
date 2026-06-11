# PAK TXT Sidecar Notes

## Confirmed Findings

- `engine.dll` exports `CreatePackFileShell`, `g_LoadPackageFiles`, and related package APIs.
- `CreatePackFileShell` constructs and returns a native pack-shell object. It does not accept a manifest path directly.
- The native pack-shell API can pack a folder without an input TXT by setting a root path, creating a package, adding files, and closing the package.
- Closing a package writes the PAK and a sidecar named `<pak path>.txt`. For `data.pak`, the legacy output is `data.pak.txt`.
- Existing game PAKs can be indexed without a TXT: the PAK header contains the file count and index-table offset, and each 16-byte index entry stores ID, data offset, unpacked size, and packed-size/flag bits.
- `PakEngineHost packfolder` now writes the PACK header, payloads, index table, and sidecar in managed code. The legacy `CreatePackFileShell` add-file path could crash while adding image/resource payloads, leaving a zero-entry partial PAK with raw data appended after the header.
- Managed pack output stores entries uncompressed, hashes virtual paths with the JX2 GBK path hash, writes file CRCs in the sidecar, and writes the package header CRC from the generated index table.
- Before reporting success, the managed builder refuses duplicate archive IDs, rejects paths that cannot be encoded in the JX2 GBK code page, sorts entries by archive ID, parses the generated PAK back from disk, confirms the header/index/CRC/entry bounds, and checks the sidecar rows against the planned manifest.
- The ID sort is important: a scratch pack/unpack test with both a root file and a nested file only restored the nested original path after the PAK index and sidecar rows were written in archive-ID order.
- `PakEngineHost patchpak` is the deterministic safe path for updating a small
  set of files inside an existing legacy PAK. It preserves unchanged base
  entries byte-for-byte, appends replacement payloads, redirects only matching
  archive IDs in the index, updates the header index offset/CRC, preserves the
  base package timestamp for repeatable package and sidecar output, and writes
  a sidecar. It intentionally fails when a replacement file's archive ID is not
  already present in the base PAK.
- Use `patchpak` for table-only updates to legacy packages such as
  `live_patch.pak`. A full managed `packfolder` rebuild of an old patch can
  change every entry's packing metadata from the legacy compressed form to
  uncompressed form, which is not a safe assumption for production clients.
- The legacy `PAKMAKER.exe` runtime memory contains `openFileDialog3` next to `PAK List File (*.txt)|*.txt`.
- The legacy window title is `JX2 Build PAK by Nita`.
- The sidecar format is tab separated:

```text
TotalFile:<count>    PakTime:<time>    PakTimeSave:<hex unix time>    CRC:<hex crc>
Index    ID    Time    FileName    Size    InPakSize    ComprFlag    CRC
```

- A scratch round trip packed `hello.txt` into `direct.pak`, wrote `direct.pak.txt`, and unpacked it back byte-for-byte.

## Inferred Behavior

- `openFileDialog3.txt` is probably a legacy UI/default filename or selected element-list filename, not a magic backend filename.
- The TXT is needed for reliable unpacking because the PAK table contains IDs, offsets, sizes, flags, and CRC values. The original path strings live in the sidecar manifest.
- Without the TXT, extraction can still proceed by generating synthetic names from the internal IDs, but original folder/file names are not recoverable from the PAK index alone.

## Unknowns

- The original `PAKMAKER.exe` appears packed or protected. It imports `mscoree.dll`, but the PE image does not expose normal .NET metadata, and raw string scans did not reveal `openFileDialog3`.
- Runtime memory confirms the `openFileDialog3` control name, but the exact code path that turns it into an `openFileDialog3.txt` filename was not recovered from source.
