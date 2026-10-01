# Maintaining the public pages

Thunderstore is the package/discovery surface. GitHub hosts the player guides and source. There is no separate public Vercel site in this documentation system.

## Edit the right source

| Content | Source |
| --- | --- |
| Package introductions | `thunderstore/<package>/README.md` |
| Short bios and guide URLs | `thunderstore/public.json` |
| Player help | `docs/player/` |
| Food recipes and values | `cook.yaml` |
| Package and dependency versions | `versions.yaml` |
| Player-facing release history | `thunderstore/<package>/CHANGELOG.md` |
| Pack membership | `versions.yaml`, pack members |
| Screenshot instructions | [screenshots.md](screenshots.md) |

After editing metadata, run `python scripts/sync-public-docs.py`. It stamps both manifest and TOML fields and rebuilds the pack's marked roster. Leave surrounding prose editorial. Check its separate-Works wording if pack membership changes.

Run `python scripts/sync-cook-readme.py` after food changes. Despite its historical filename, it generates only recipe-reference pages, never the package README. Keep biome pages, the vanilla-change table and the artwork gallery together.

Run `python scripts/sync-versions.py` after version edits. Its loader/version responsibilities remain unchanged.

## Check before a release

```sh
python scripts/sync-public-docs.py --check
python scripts/sync-cook-readme.py --check
python scripts/sync-versions.py --check
python scripts/check-public-docs.py
```

Use real current behaviour. Put untagged changes under **Unreleased**, then move them into the actual bumped version before publishing. Preserve old version headings and historical facts; do not retrospectively imply that a later UI shipped in the initial plumbing release.

Write changes in player terms. Mention dependency changes where needed. Build notes and internal field names belong in developer documentation.

`scripts/release-notes.py <package-folder> <version> --output <path>` extracts only the named version. The Core release workflow uses this instead of unrelated monorepo PR titles.

## Screenshot placeholders

Use a visible blockquote: **(screenshot coming)** followed by the view to capture. There are no missing-image URLs. Replace it with an image and a factual caption when the requested capture arrives. Keep full screenshots available, produce sensible web-sized exports, and add meaningful alt text.

## Publication boundaries

Merging documentation does not publish a new Thunderstore package. README/bio/changelog changes ship with the corresponding package release. Do not bump plugin versions solely to hide that distinction.

The GitHub connector cannot edit repository About metadata or Thunderstore's independently stored wiki pages in this workflow. The new guides are version-controlled here and linked from all package pages. If separate Thunderstore Wiki pages already exist, point them to the relevant guide or replace them with guide excerpts rather than keeping a second hand-written source.

Repository About should read: **Valheim mods with a shared interface and quality-of-life foundation, plus cooking, farming, storage and workshop expansions.** Clear the accidental `restlessvalheim.vercel.app` Website field, or point it at the current Thunderstore pack. No maintained file links to that site.

The older RestlessValheim and RestlessQOL listings are already deprecated. Do not rename/reactivate them or include them in current navigation. A short legacy note in installation guidance is sufficient.

## Rendering and stability

Pages use ordinary Markdown tables, links and screenshot blockquotes for Thunderstore/GitHub compatibility. Check the real package preview when publishing.

Guide links follow `main` and say so. Existing custom artwork remains at its original URLs; it is labelled reference art, not an in-game capture. For a release needing frozen documentation, link the appropriate tagged revision.

The repository does not claim every controller path or multiplayer ownership scenario has been tested. Keep limitations precise and adjacent to the relevant feature.

