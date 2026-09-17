# Regenerates the RestlessCook canvas from cook.yaml.
# Source of truth is the yaml. Do not edit canvas rows by hand.
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
text = (root / "cook.yaml").read_text(encoding="utf-8")
items = []
cur = None
use = None
mode = None
for raw in text.splitlines():
    line = raw.rstrip()
    if line.startswith("  - id:"):
        if cur:
            items.append(cur)
        cur = {"id": line.split(":", 1)[1].strip(), "feeds": [], "uses": []}
        use = None
        mode = None
        continue
    if cur is None:
        continue
    if line.startswith("    feeds:"):
        mode = "feeds"
        if "[]" in line:
            mode = None
        continue
    if line.startswith("    uses:"):
        mode = "uses"
        continue
    if mode == "feeds" and line.startswith("      - "):
        cur["feeds"].append(line.split("-", 1)[1].strip())
        continue
    if mode == "uses" and line.startswith("      - item:"):
        use = {"item": line.split(":", 1)[1].strip()}
        cur["uses"].append(use)
        continue
    if mode == "uses" and use is not None and line.startswith("        amount:"):
        use["amount"] = int(line.split(":", 1)[1].strip())
        continue
    if line.startswith("    ") and ":" in line and not line.startswith("      "):
        mode = None
        key, val = line.strip().split(":", 1)
        val = val.strip()
        if len(val) >= 2 and val[0] == val[-1] and val[0] in "\"'":
            val = val[1:-1]
        if key in ("station_level", "output_amount"):
            cur[key] = int(val)
        else:
            cur[key] = val

if cur:
    items.append(cur)

assert len(items) == 81, len(items)
ids = {i["id"] for i in items}
assert len(ids) == 81
for i in items:
    assert i["uses"], i["id"]
    for f in i["feeds"]:
        assert f in ids, f"{i['id']} -> {f}"


def n(pred):
    return sum(1 for i in items if pred(i))


ops = n(lambda i: i["operation"] in ("add", "rewrite"))
assert ops == 51, ops
assert n(lambda i: i["operation"] == "add") == 35
assert n(lambda i: i["operation"] == "rewrite") == 16
assert n(lambda i: i["operation"] == "reference") == 30
assert n(lambda i: i["kind"] == "meal" and i["source"] == "custom" and i["operation"] == "add") == 17
assert n(lambda i: i["kind"] == "feast" and i["source"] == "custom" and i["operation"] == "add") == 16
assert n(lambda i: i["kind"] == "sideboard") == 2
assert n(lambda i: i["kind"] == "meal" and i["operation"] == "reference") == 24
assert n(lambda i: i["kind"] == "feast" and i["operation"] == "reference") == 6

payload = json.dumps(items, ensure_ascii=False, indent=2)

tsx = '''import {
  Callout,
  H1,
  Pill,
  Row,
  Select,
  Stack,
  Stat,
  Table,
  Text,
  TextInput,
  computeDAGLayout,
  useCanvasState,
  useHostTheme,
} from "cursor/canvas";

type Kind = "meal" | "feast" | "sideboard";
type Source = "custom" | "vanilla";
type Operation = "add" | "rewrite" | "reference";
type Tier =
  | "meadows"
  | "blackforest"
  | "swamp"
  | "ocean"
  | "mountain"
  | "plains"
  | "mistlands"
  | "ashlands";

type Use = { item: string; amount: number };

type Item = {
  id: string;
  name: string;
  kind: Kind;
  source: Source;
  operation: Operation;
  tier: Tier;
  prefab: string;
  recipe_id?: string;
  clone_from?: string;
  feeds: string[];
  uses: Use[];
  output_amount: number;
  station: string;
  station_level: number;
};

const ITEMS: Item[] = ''' + payload + ''';

const BY_ID = Object.fromEntries(ITEMS.map((i) => [i.id, i]));

const KIND: Record<Kind, { label: string; tone: "success" | "info" | "warning" | "neutral" }> = {
  meal: { label: "Meal", tone: "info" },
  feast: { label: "Feast", tone: "success" },
  sideboard: { label: "Sideboard", tone: "warning" },
};

const OP: Record<Operation, { label: string; tone: "success" | "warning" | "neutral" }> = {
  add: { label: "Add", tone: "success" },
  rewrite: { label: "Rewrite", tone: "warning" },
  reference: { label: "Reference", tone: "neutral" },
};

const TIER: Record<Tier, string> = {
  meadows: "Meadows",
  blackforest: "Black Forest",
  swamp: "Swamp",
  ocean: "Ocean",
  mountain: "Mountain",
  plains: "Plains",
  mistlands: "Mistlands",
  ashlands: "Ashlands",
};

const TIERS: Tier[] = [
  "meadows",
  "blackforest",
  "swamp",
  "ocean",
  "mountain",
  "plains",
  "mistlands",
  "ashlands",
];

function count(pred: (i: Item) => boolean) {
  return ITEMS.filter(pred).length;
}

function feastPath(start: Item): boolean {
  const seen = new Set<string>();
  const stack = [start.id];
  while (stack.length) {
    const id = stack.pop()!;
    if (seen.has(id)) continue;
    seen.add(id);
    const row = BY_ID[id];
    if (!row) continue;
    if (row.kind === "feast" && id !== start.id) return true;
    stack.push(...row.feeds);
  }
  return start.kind === "feast";
}

function usesLine(i: Item) {
  return i.uses.map((u) => `${u.amount} ${u.item}`).join(" + ");
}

function Graph({ rows }: { rows: Item[] }) {
  const theme = useHostTheme();
  const ids = new Set(rows.map((r) => r.id));
  const extra = new Set<string>();
  for (const row of rows) {
    for (const feed of row.feeds) extra.add(feed);
  }
  const nodes = [
    ...rows.map((r) => ({ id: r.id })),
    ...[...extra].filter((id) => !ids.has(id) && BY_ID[id]).map((id) => ({ id })),
  ];
  const nodeIds = new Set(nodes.map((n) => n.id));
  const edges = rows.flatMap((r) =>
    r.feeds.filter((f) => nodeIds.has(f)).map((f) => ({ from: r.id, to: f }))
  );
  if (nodes.length === 0) return null;
  const layout = computeDAGLayout({
    nodes,
    edges,
    direction: "horizontal",
    nodeWidth: 168,
    nodeHeight: 44,
    rankGap: 56,
    nodeGap: 12,
    padding: 8,
  });
  const pos = Object.fromEntries(layout.nodes.map((n) => [n.id, n]));
  return (
    <div
      style={{
        position: "relative",
        width: layout.width,
        height: layout.height,
        maxWidth: "100%",
        overflow: "auto",
        background: theme.bg.editor,
      }}
    >
      <svg
        width={layout.width}
        height={layout.height}
        style={{ position: "absolute", inset: 0 }}
      >
        {layout.edges.map((e) => (
          <line
            key={e.from + "->" + e.to}
            x1={e.sourceX}
            y1={e.sourceY}
            x2={e.targetX}
            y2={e.targetY}
            stroke={e.isBackEdge ? theme.stroke.tertiary : theme.stroke.secondary}
            strokeDasharray={e.isBackEdge ? "4 4" : undefined}
          />
        ))}
      </svg>
      {layout.nodes.map((n) => {
        const row = BY_ID[n.id];
        return (
          <div
            key={n.id}
            style={{
              position: "absolute",
              left: n.x,
              top: n.y,
              width: 168,
              height: 44,
              padding: "6px 8px",
              background: theme.bg.elevated,
              border: `1px solid ${theme.stroke.secondary}`,
              boxSizing: "border-box",
            }}
          >
            <Text size="small" weight="semibold" as="span">
              {row?.name ?? n.id}
            </Text>
            <Text size="small" tone="tertiary" as="span">
              {row ? `${KIND[row.kind].label} · ${OP[row.operation].label}` : n.id}
            </Text>
          </div>
        );
      })}
    </div>
  );
}

export default function RestlessCookGraph() {
  const [query, setQuery] = useCanvasState("cook-q", "");
  const [tier, setTier] = useCanvasState("cook-t", "all");
  const [operation, setOperation] = useCanvasState("cook-o", "all");
  const [kind, setKind] = useCanvasState("cook-k", "all");

  const filtered = ITEMS.filter((i) => {
    if (tier !== "all" && i.tier !== tier) return false;
    if (operation !== "all" && i.operation !== operation) return false;
    if (kind !== "all" && i.kind !== kind) return false;
    if (!query.trim()) return true;
    const q = query.toLowerCase();
    return (
      i.name.toLowerCase().includes(q) ||
      i.id.toLowerCase().includes(q) ||
      i.prefab.toLowerCase().includes(q) ||
      usesLine(i).toLowerCase().includes(q)
    );
  });

  const mealsMissingFeast = ITEMS.filter((i) => i.kind === "meal" && !feastPath(i));
  const graphRows =
    tier === "all"
      ? ITEMS.filter((i) => i.feeds.length > 0)
      : ITEMS.filter((i) => i.tier === tier || i.feeds.some((f) => BY_ID[f]?.tier === tier));

  return (
    <Stack gap={20}>
      <Stack gap={6}>
        <H1>RestlessCook graph</H1>
        <Text tone="secondary">
          cook.yaml is the recipe list. v0.1 is Meadows through Ashlands: 51
          recipe operations inside 81 rows. Cook the haul, make meals, turn
          meals into feasts. Every prepared meal reaches a feast.
        </Text>
      </Stack>

      <Callout tone="info" title="v0.1 lock">
        17 custom meals, 16 custom feasts, 2 sideboards, 14 cooked-first
        rewrites, 2 vanilla feast reroutes. Deep North waits. Vanilla spices
        stay vanilla. Magecap Tart sits on Mistwalker&apos;s Garden Table.
      </Callout>

      <Row gap={12} wrap>
        <Stat value="51" label="Recipe operations" tone="success" />
        <Stat value="35" label="New game recipes" />
        <Stat value="16" label="Vanilla rewrites" tone="warning" />
        <Stat value="30" label="Graph-only rows" />
        <Stat value="81" label="cook.yaml rows" />
      </Row>

      {mealsMissingFeast.length > 0 ? (
        <Callout tone="danger" title="Meal without a feast">
          {mealsMissingFeast.map((i) => i.name).join(", ")}
        </Callout>
      ) : (
        <Callout tone="success" title="Feast law">
          Every meal row reaches at least one feast, including vanilla sinks.
        </Callout>
      )}

      <Row gap={10} wrap align="center">
        <TextInput
          value={query}
          onChange={setQuery}
          placeholder="Filter by name, prefab, ingredients…"
          style={{ minWidth: 280, flex: 1 }}
        />
        <Select
          value={tier}
          onChange={setTier}
          options={[
            { value: "all", label: "All tiers" },
            ...TIERS.map((t) => ({ value: t, label: TIER[t] })),
          ]}
        />
        <Select
          value={operation}
          onChange={setOperation}
          options={[
            { value: "all", label: "All operations" },
            { value: "add", label: "Add" },
            { value: "rewrite", label: "Rewrite" },
            { value: "reference", label: "Reference" },
          ]}
        />
        <Select
          value={kind}
          onChange={setKind}
          options={[
            { value: "all", label: "All kinds" },
            { value: "meal", label: "Meal" },
            { value: "feast", label: "Feast" },
            { value: "sideboard", label: "Sideboard" },
          ]}
        />
        <Text size="small" tone="tertiary">
          {filtered.length} shown
        </Text>
      </Row>

      <Stack gap={8}>
        <Text weight="semibold">Feeds graph</Text>
        <Text size="small" tone="secondary">
          Pick a biome to keep the layout readable. All-tiers shows every edge.
        </Text>
        <Graph rows={graphRows} />
      </Stack>

      <Table
        striped
        stickyHeader
        headers={["Dish", "Kind", "Op", "Tier", "Feeds", "Uses"]}
        rows={filtered.map((i) => [
          <Stack gap={2} key={i.id + "-n"}>
            <Text weight="semibold" as="span">
              {i.name}
            </Text>
            <Text size="small" tone="tertiary" as="span">
              {i.id} · {i.prefab}
            </Text>
          </Stack>,
          <Pill key={i.id + "-k"} size="sm" tone={KIND[i.kind].tone} active>
            {KIND[i.kind].label}
          </Pill>,
          <Pill key={i.id + "-o"} size="sm" tone={OP[i.operation].tone} active>
            {OP[i.operation].label}
          </Pill>,
          <Text size="small" as="span">
            {TIER[i.tier]}
          </Text>,
          <Text size="small" tone="secondary" as="span">
            {i.feeds.length ? i.feeds.map((f) => BY_ID[f]?.name ?? f).join(", ") : "—"}
          </Text>,
          <Text size="small" tone="secondary" as="span">
            {usesLine(i)}
          </Text>,
        ])}
        emptyMessage="No rows match that filter."
        style={{ maxHeight: 640 }}
      />

      <Text size="small" tone="tertiary">
        Source of truth: cook.yaml. Keep this canvas in sync via
        scripts/sync-cook-canvas.py. 51 operations is not the yaml row count.
      </Text>
    </Stack>
  );
}
'''

dest = Path.home() / ".cursor/projects/c-Users-jules-source-repos-RestlessQoL/canvases/restless-cook.canvas.tsx"
dest.parent.mkdir(parents=True, exist_ok=True)
dest.write_text(tsx, encoding="utf-8")
print(f"wrote {dest} ({len(items)} rows)")
