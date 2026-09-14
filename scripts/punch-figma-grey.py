"""Knock out the solid grey Figma MCP flattens behind every PNG export."""

from collections import deque
from pathlib import Path

import numpy as np
from PIL import Image

ASSETS = Path(r"C:\Users\jules\source\repos\RestlessQoL\src\RestlessQoL\Assets")
TOL = 8
TOL_FOR = {
    "status-well.png": 2,
    "status-ring.png": 2,
}
# Only open rings/arrows have a grey disc trapped inside the art.
HOLE_FILES = {
    "map-compass.png",
    "map-ring.png",
    "arrow-left.png",
    "arrow-right.png",
}


def punch(path: Path) -> None:
    image = Image.open(path).convert("RGBA")
    arr = np.array(image)
    height, width = arr.shape[:2]
    rgb = arr[:, :, :3].astype(np.int16)
    bg = np.median(
        [arr[0, 0, :3], arr[0, -1, :3], arr[-1, 0, :3], arr[-1, -1, :3]],
        axis=0,
    ).astype(np.int16)
    tol = TOL_FOR.get(path.name, TOL)

    def near(y: int, x: int, tol: int = tol) -> bool:
        return int(np.max(np.abs(rgb[y, x] - bg))) <= tol

    seen = np.zeros((height, width), dtype=bool)
    queue: deque[tuple[int, int]] = deque()

    def seed(x: int, y: int) -> None:
        if seen[y, x] or not near(y, x):
            return
        seen[y, x] = True
        queue.append((x, y))

    for x in range(width):
        seed(x, 0)
        seed(x, height - 1)
    for y in range(height):
        seed(0, y)
        seed(width - 1, y)

    while queue:
        x, y = queue.popleft()
        if x > 0:
            seed(x - 1, y)
        if x + 1 < width:
            seed(x + 1, y)
        if y > 0:
            seed(x, y - 1)
        if y + 1 < height:
            seed(x, y + 1)

    out = arr.copy()
    mask = seen
    if path.name in HOLE_FILES:
        mask = seen | (np.max(np.abs(rgb - bg), axis=2) <= tol)
    out[mask, 3] = 0
    seen = mask

    # Soften the fringe so torn edges do not keep a grey halo.
    fringe = np.zeros((height, width), dtype=bool)
    fringe[:, 1:] |= seen[:, :-1]
    fringe[:, :-1] |= seen[:, 1:]
    fringe[1:, :] |= seen[:-1, :]
    fringe[:-1, :] |= seen[1:, :]
    fringe &= ~seen
    dist = np.max(np.abs(rgb - bg), axis=2)
    soft = fringe & (dist <= tol * 2)
    alpha = out[:, :, 3].astype(np.float32)
    alpha[soft] *= np.clip((dist[soft] - tol) / float(max(tol, 1)), 0.0, 1.0)
    out[:, :, 3] = alpha.astype(np.uint8)

    Image.fromarray(out, "RGBA").save(path)
    clear = int(np.mean(out[:, :, 3] == 0) * 100)
    print(f"{path.name:22} bg={tuple(int(v) for v in bg)} clear={clear}%")


def main() -> None:
    skip = {"map-full.png", "map-back.png", "btn-small.png", "status-well-armor.png"}
    for path in sorted(ASSETS.glob("*.png")):
        if path.name in skip:
            continue
        punch(path)


if __name__ == "__main__":
    main()
