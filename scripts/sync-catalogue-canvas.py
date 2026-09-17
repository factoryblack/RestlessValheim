# Regenerates the Cursor plan canvas from catalogue.yaml.
# Source of truth is the yaml. Do not edit the canvas rows by hand.
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
text = (root / "catalogue.yaml").read_text(encoding="utf-8")
items = []
cur = None
for line in text.splitlines():
    if line.startswith("  - id:"):
        if cur:
            items.append(cur)
        cur = {"id": line.split(":", 1)[1].strip()}
    elif cur is not None and line.startswith("    ") and ":" in line:
        key, val = line.strip().split(":", 1)
        val = val.strip()
        if len(val) >= 2 and val[0] == val[-1] and val[0] in "\"'":
            val = val[1:-1]
        cur[key] = val
if cur:
    items.append(cur)


def ts_str(s: str) -> str:
    return json.dumps(s, ensure_ascii=False)


rows = []
for i in items:
    bite = i.get("bite")
    bite_ts = f", bite: {int(bite)}" if bite else ""
    rows.append(
        "  { "
        f"id: {ts_str(i['id'])}, "
        f"name: {ts_str(i['name'])}, "
        f"decision: {ts_str(i['decision'])}, "
        f"in_mod: {ts_str(i['in_mod'])}"
        f"{bite_ts}, "
        f"restless: {ts_str(i['restless'])}, "
        f"note: {ts_str(i['note'])} "
        "},"
    )

tsx = '''import {
  Callout,
  Grid,
  H1,
  Pill,
  Row,
  Select,
  Stack,
  Stat,
  Table,
  Text,
  TextInput,
  useCanvasState,
} from "cursor/canvas";

type Decision = "dll" | "loader" | "sidecar" | "vanilla" | "skip";
type InMod = "n/a" | "none" | "stub" | "partial" | "done";

type Item = {
  id: string;
  name: string;
  decision: Decision;
  in_mod: InMod;
  bite?: 1 | 2 | 3;
  restless: string;
  note: string;
};

const ITEMS: Item[] = [
''' + "\n".join(rows) + '''
];

const DECISION: Record<
  Decision,
  { label: string; tone: "success" | "info" | "warning" | "neutral" | "deleted" }
> = {
  dll: { label: "In the DLL", tone: "success" },
  loader: { label: "Loader", tone: "info" },
  sidecar: { label: "Optional sidecar", tone: "warning" },
  vanilla: { label: "Vanilla 1.0", tone: "neutral" },
  skip: { label: "Skip", tone: "deleted" },
};

const INMOD: Record<
  InMod,
  { label: string; tone: "success" | "info" | "warning" | "neutral" | "deleted" }
> = {
  done: { label: "Built", tone: "success" },
  partial: { label: "Partial", tone: "warning" },
  stub: { label: "Stub", tone: "warning" },
  none: { label: "Not built", tone: "deleted" },
  "n/a": { label: "—", tone: "neutral" },
};

function n(pred: (i: Item) => boolean) {
  return ITEMS.filter(pred).length;
}

export default function RestlessQolCatalogue() {
  const [query, setQuery] = useCanvasState("cat-q", "");
  const [decision, setDecision] = useCanvasState("cat-d", "all");
  const [inMod, setInMod] = useCanvasState("cat-m", "all");

  const filtered = ITEMS.filter((i) => {
    if (decision !== "all" && i.decision !== decision) return false;
    if (inMod !== "all" && i.in_mod !== inMod) return false;
    if (!query.trim()) return true;
    const q = query.toLowerCase();
    return (
      i.name.toLowerCase().includes(q) ||
      i.id.toLowerCase().includes(q) ||
      i.restless.toLowerCase().includes(q) ||
      i.note.toLowerCase().includes(q)
    );
  });

  const rowTone = filtered.map((i) => {
    if (i.decision === "dll" && i.in_mod === "done") return "success" as const;
    if (i.decision === "dll" && (i.in_mod === "partial" || i.in_mod === "stub"))
      return "warning" as const;
    return undefined;
  });

  return (
    <Stack gap={20}>
      <Stack gap={6}>
        <H1>RestlessCore catalogue</H1>
        <Text tone="secondary">
          Master list is catalogue.yaml in the repo. This canvas is the same rows.
          One behaviour per row — not a Thunderstore pack, not a replacement pass
          beside a Restless shopping list. Built means the patch looks complete.
          Playtest status is in the note, not the pill.
        </Text>
      </Stack>

      <Callout tone="info" title="Product">
        One plugin. Papercuts go in the DLL. PlanBuild, skills, drawers, collectors,
        portals, Auga, ValheimPlus stay out. BepInEx + Jötunn are the loader.
        No sidecars. Extra slots live in the DLL. MyLittleUI is skip.
      </Callout>

      <Grid columns={5} gap={12}>
        <Stat
          value={String(n((i) => i.decision === "dll" && i.in_mod === "done"))}
          label="DLL built"
          tone="success"
        />
        <Stat
          value={String(
            n((i) => i.decision === "dll" && (i.in_mod === "partial" || i.in_mod === "stub"))
          )}
          label="DLL partial / stub"
          tone="warning"
        />
        <Stat
          value={String(n((i) => i.decision === "dll" && i.in_mod === "none"))}
          label="DLL not built"
          tone="danger"
        />
        <Stat value={String(n((i) => i.decision === "sidecar"))} label="Optional sidecar" />
        <Stat value={String(n((i) => i.decision === "skip"))} label="Skip" tone="danger" />
      </Grid>

      <Row gap={10} wrap align="center">
        <TextInput
          value={query}
          onChange={setQuery}
          placeholder="Filter by name, old Restless package, note…"
          style={{ minWidth: 280, flex: 1 }}
        />
        <Select
          value={decision}
          onChange={setDecision}
          options={[
            { value: "all", label: "All decisions" },
            { value: "dll", label: "In the DLL" },
            { value: "loader", label: "Loader" },
            { value: "sidecar", label: "Optional sidecar" },
            { value: "vanilla", label: "Vanilla 1.0" },
            { value: "skip", label: "Skip" },
          ]}
        />
        <Select
          value={inMod}
          onChange={setInMod}
          options={[
            { value: "all", label: "All build states" },
            { value: "done", label: "Built" },
            { value: "partial", label: "Partial" },
            { value: "stub", label: "Stub" },
            { value: "none", label: "Not built" },
            { value: "n/a", label: "n/a" },
          ]}
        />
        <Text size="small" tone="tertiary">
          {filtered.length} shown
        </Text>
      </Row>

      <Table
        striped
        stickyHeader
        headers={["Feature", "Decision", "In the mod", "Bite", "Old Restless", "Notes"]}
        rowTone={rowTone}
        rows={filtered.map((i) => [
          <Stack gap={2} key={i.id + "-n"}>
            <Text weight="semibold" as="span">
              {i.name}
            </Text>
            <Text size="small" tone="tertiary" as="span">
              {i.id}
            </Text>
          </Stack>,
          <Pill key={i.id + "-d"} size="sm" tone={DECISION[i.decision].tone} active>
            {DECISION[i.decision].label}
          </Pill>,
          <Pill key={i.id + "-m"} size="sm" tone={INMOD[i.in_mod].tone} active>
            {INMOD[i.in_mod].label}
          </Pill>,
          <Text size="small" as="span">
            {i.bite ?? "—"}
          </Text>,
          <Text size="small" tone="secondary" as="span">
            {i.restless}
          </Text>,
          <Text size="small" tone="secondary" as="span">
            {i.note}
          </Text>,
        ])}
        emptyMessage="No rows match that filter."
        style={{ maxHeight: 640 }}
      />

      <Text size="small" tone="tertiary">
        Source of truth: catalogue.yaml. Update that file when a feature ships,
        stubs, or gets skipped; keep this canvas in sync via scripts/sync-catalogue-canvas.py.
      </Text>
    </Stack>
  );
}
'''

dest = Path.home() / ".cursor/projects/c-Users-jules-source-repos-RestlessQoL/canvases/restlessqol-1.0-matrix.canvas.tsx"
dest.parent.mkdir(parents=True, exist_ok=True)
dest.write_text(tsx, encoding="utf-8")
print(f"wrote {dest} ({len(items)} rows)")
