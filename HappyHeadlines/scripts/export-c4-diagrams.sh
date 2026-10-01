#!/usr/bin/env sh
set -eu

root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
architecture="$root/docs/architecture"
diagrams="$architecture/diagrams"
temporary=$(mktemp -d)
trap 'rm -rf "$temporary"' EXIT INT TERM

docker run --rm --user "$(id -u):$(id -g)" -v "$architecture:/workspace:ro" -v "$temporary:/output" structurizr/cli:2025.11.09 \
  export -workspace /workspace/workspace.dsl -format plantuml/c4plantuml -output /output

render_view() {
  view_key=$1
  source_file="$temporary/structurizr-$view_key.puml"
  test -f "$source_file"
  cp "$source_file" "$temporary/$view_key.puml"
  docker run --rm -v "$temporary:/work" plantuml/plantuml:1.2024.6 \
    -tpng "/work/$view_key.puml"
  test -s "$temporary/$view_key.png"
  install -m 0644 "$temporary/$view_key.png" "$diagrams/.$2.tmp"
  mv -f "$diagrams/.$2.tmp" "$diagrams/$2"
}

render_view SystemContext system-context.png
render_view Containers container.png