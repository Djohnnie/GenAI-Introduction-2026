# Tools

Utility scripts for maintaining and publishing the AI4Dev Workshop content.

---

## svg_to_pptx.py

Converts a folder of `slide-*.svg` files into a single Microsoft PowerPoint (`.pptx`) file. Each SVG becomes one full-bleed slide in a 16:9 widescreen presentation.

### Why this exists

Slide content is authored and version-controlled as SVG files (one file per slide, stored alongside the chapter README and exercise descriptions). SVG keeps diffs readable and avoids binary blob churn in git. When a presenter or attendee needs a `.pptx` file, this script produces one on demand without storing it in the repo.

### Requirements

- **Python 3.8+**
- **python-pptx** — `pip install python-pptx`
- **Microsoft Edge** — used as a headless renderer to convert SVG → PNG before embedding in the deck. Edge ships with Windows 11 and is available at its standard install location; if yours differs, update `EDGE_PATHS` near the top of the script.

### Usage

```
python tools/svg_to_pptx.py <svg_dir> [output.pptx]
```

| Argument | Description |
|---|---|
| `svg_dir` | Path to a directory containing `slide-*.svg` files |
| `output.pptx` | *(optional)* Output path. Defaults to `<svg_dir>/<dirname>.pptx` |

### Examples

```bash
# 2-day workshop, chapter 1 — output written to content/2-day/chapter-01/chapter-01.pptx
python tools/svg_to_pptx.py content/2-day/chapter-01

# Explicit output path
python tools/svg_to_pptx.py content/2-day/chapter-04 decks/chapter-04.pptx

# 1-day variant
python tools/svg_to_pptx.py content/1-day/chapter-01
```

### How it works

1. Collects all `slide-*.svg` files in the given directory, sorted by filename.
2. Launches Edge in headless mode for each SVG and captures a 1920×1080 PNG screenshot.
3. Embeds every PNG as a full-bleed image on a blank slide in a new Presentation object.
4. Saves the resulting `.pptx` to the output path.

> **Note:** The generated `.pptx` files are not committed to the repository. Add your output path to `.gitignore` if you generate them inside the `content/` tree.
