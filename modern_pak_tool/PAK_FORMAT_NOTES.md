# PAK TXT Sidecar Notes

## Confirmed Findings

- `engine.dll` exports `CreatePackFileShell`, `g_LoadPackageFiles`, and related package APIs.
- `CreatePackFileShell` constructs and returns a native pack-shell object. It does not accept a manifest path directly.
- The native pack-shell API can pack a folder without an input TXT by setting a root path, creating a package, adding files, and closing the package.
- Closing a package writes the PAK and a sidecar named `<pak path>.txt`. For `data.pak`, the legacy output is `data.pak.txt`.
- Existing game PAKs can be indexed without a TXT: the PAK header contains the file count and index-table offset, and each 16-byte index entry stores ID, data offset, unpacked size, and packed-size/flag bits.
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
