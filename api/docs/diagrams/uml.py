#!/usr/bin/env python3
"""Génère des diagrammes Mermaid de l'API C# : types, signatures et dépendances réelles.

Les dépendances sont lues dans les constructeurs (l'injection ASP.NET), pas dans les `using` :
c'est ce qui dit vraiment qui appelle qui. Sortie en flowchart et non en classDiagram, seul
format que Whimsical sait importer.

    python3 api/docs/diagrams/uml.py   # régénère les diagrammes, en place
"""
import os, re, subprocess, sys
from html import escape
from pathlib import Path
from collections import defaultdict

OUT = Path(__file__).resolve().parent
ROOT = OUT.parent.parent.parent
SRC = ROOT / "api" / "Modules"

DECL = re.compile(
    r'^(?:public|internal)\s+(?:sealed\s+|abstract\s+|static\s+|partial\s+)*'
    # La liste des bases court parfois sur plusieurs lignes, quand un constructeur primaire est
    # mis en forme sur plusieurs lignes : on lit jusqu'à l'accolade du corps, ou au « ; » d'un
    # record positionnel sans corps.
    r'(class|record|interface|enum)\s+(\w+)([^{;]*)', re.M)
IDENT = re.compile(r'\b([A-Z]\w+)\b')
# Constructeur primaire : le nom du type est suivi de sa liste de paramètres.
PRIMARY = re.compile(r'(?:class|record)\s+{name}\s*\(([^)]*)\)')
CTOR = re.compile(r'public\s+{name}\s*\(([^)]*)\)')
# Dans une classe, une méthode publique porte son modificateur. Dans une interface, les membres
# n'en ont pas — mais ils se terminent par « ; », ce qui les distingue des appels du corps d'une
# méthode (if(...), CreatedAtAction(...)) qu'un motif plus permissif capturait à tort.
METHOD = re.compile(
    r'^\s+public\s+(?:static\s+|async\s+|virtual\s+|override\s+|sealed\s+)*'
    r'([\w<>,\[\]\?\. ]+?)\s+(\w+)\s*\(([^)]*)\)', re.M)
INTERFACE_METHOD = re.compile(
    r'^\s+([\w<>,\[\]\?\. ]+?)\s+(\w+)\s*\(([^)]*)\)\s*;', re.M)
# Un type injecté peut être enveloppé : IEnumerable<IFoo>, IOptions<Bar>…
WRAPPED = re.compile(r'(?:IEnumerable|IReadOnlyList|IList|IOptions|ILogger|Lazy)<([\w\.]+)>')


def module_of(path: Path) -> str:
    """Le module racine : Core, EliteDangerous. Un fichier de diagramme par module."""
    return path.relative_to(SRC).parts[0]


def area_of(path: Path) -> str:
    """Le sous-module : Auth, GameServers, RoadToRiches… Sert de groupe dans le diagramme."""
    parts = path.relative_to(SRC).parts
    return parts[1] if len(parts) > 2 else ""


def layer_of(path: Path) -> str:
    for part in path.relative_to(SRC).parts:
        if part in ("Api", "Application", "Infrastructure", "Domain"):
            return part
    return "Autre"


def simplify(params: str) -> list[str]:
    """« IFoo foo, CancellationToken ct » -> ['IFoo', 'CancellationToken']"""
    out, depth, current = [], 0, ""
    for ch in params:
        if ch == "<":
            depth += 1
        elif ch == ">":
            depth -= 1
        if ch == "," and depth == 0:
            out.append(current.strip())
            current = ""
        else:
            current += ch
    if current.strip():
        out.append(current.strip())
    types = []
    for param in out:
        param = param.split("=")[0].strip()
        # Les attributs ASP.NET ([FromServices], [FromBody]) précèdent le type sans en faire partie.
        param = re.sub(r"\[[^\]]*\]", "", param).strip()
        if not param:
            continue
        words = param.split()
        # Le dernier mot est le nom du paramètre ; tout ce qui précède est le type, qui peut
        # contenir des espaces (IReadOnlyDictionary<string, string>).
        types.append(" ".join(words[:-1]) if len(words) > 1 else words[0])
    return types


def scan() -> dict:
    types = {}
    for file in SRC.rglob("*.cs"):
        if any(p in file.parts for p in ("bin", "obj")):
            continue
        text = file.read_text(encoding="utf8", errors="ignore")
        # Chaque déclaration délimite son propre bloc : sans ça, les DTO déclarés à la suite d'un
        # contrôleur héritaient de ses méthodes.
        marks = [(m.start(), m.group(1), m.group(2), m.group(3)) for m in DECL.finditer(text)]
        for index, (start, kind, name, tail) in enumerate(marks):
            end = marks[index + 1][0] if index + 1 < len(marks) else len(text)
            body = text[start:end]
            if name in types:
                continue
            params = ""
            m = re.search(PRIMARY.pattern.format(name=name), body)
            if m:
                params = m.group(1)
            else:
                m = re.search(CTOR.pattern.format(name=name), body)
                if m:
                    params = m.group(1)

            # Les bases et interfaces sont ce qui suit « : » — mais seulement en dehors des
            # paramètres du constructeur primaire et des commentaires, qui peuvent eux aussi
            # contenir un « : » et faisaient conclure à une implémentation inexistante.
            cleaned = re.sub(r"//[^\n]*", "", tail)
            depth, outside = 0, []
            for char in cleaned:
                if char == "(":
                    depth += 1
                elif char == ")":
                    depth -= 1
                elif depth == 0:
                    outside.append(char)
            outside = "".join(outside)
            implements = IDENT.findall(outside.split(":", 1)[1]) if ":" in outside else []

            deps = []
            for raw in simplify(params):
                wrapped = WRAPPED.search(raw)
                deps.append(wrapped.group(1).split(".")[-1] if wrapped else raw.split("<")[0])

            methods, uses = [], set()
            pattern = INTERFACE_METHOD if kind == "interface" else METHOD
            for ret, mname, mparams in pattern.findall(body):
                for injected in re.findall(r'\[FromServices\]\s*([\w<>\.]+)', mparams):
                    deps.append(injected.split("<")[0].split(".")[-1])
                if mname in (name, "Dispose", "DisposeAsync"):
                    continue
                args = [a for a in simplify(mparams) if a not in ("CancellationToken", "this")]
                methods.append(f"{mname}({', '.join(args)})")
                # Types manipulés par la surface publique : c'est ce qui relie un use case à son
                # entité de domaine ou à sa vue, alors qu'il ne les injecte pas.
                for token in [ret] + args:
                    uses.update(IDENT.findall(token))


            types[name] = {
                "kind": kind, "file": str(file.relative_to(ROOT)),
                "module": module_of(file), "area": area_of(file), "layer": layer_of(file),
                "deps": deps, "implements": implements, "uses": sorted(uses),
                "methods": methods[:6], "body": body,
            }

    # Seconde passe : les entités de domaine sont manipulées dans le corps des use cases, pas dans
    # leurs signatures — sans ça, on ne voit jamais qui les utilise. Le balayage est restreint à la
    # couche Domain pour ne pas noyer le diagramme.
    # Les énumérations du domaine (RoleCode, ModuleCode…) sont citées par la moitié du code : les
    # relier ajoute 60 flèches et zéro information. Seules les entités et objets-valeurs comptent.
    types = {n: i for n, i in types.items() if i["area"]}
    domain = {n for n, i in types.items() if i["layer"] == "Domain" and i["kind"] != "enum"}
    for name, info in types.items():
        mentioned = set(IDENT.findall(info["body"])) & domain
        info["uses"] = sorted(((set(info["uses"]) & domain) | mentioned) - {name})
        info["deps"] = sorted(set(info["deps"]))
        del info["body"]
    return types


def esc(text: str) -> str:
    return text.replace('"', "'").replace("<", "&lt;").replace(">", "&gt;")


def node(name: str, info: dict) -> str:
    shape = ("([", "])") if info["kind"] == "interface" else ("[", "]")
    return f'  {name}{shape[0]}"{esc(name)}"{shape[1]}'


def diagram(types: dict, members: dict) -> str:
    lines = ["flowchart LR"]
    by_layer = defaultdict(list)
    for name, info in members.items():
        by_layer[info["layer"]].append(name)

    # Groupé par couche : c'est ce qui fait lire le sens des flèches (Api -> Application ->
    # Infrastructure via les ports).
    for layer in ("Api", "Application", "Domain", "Infrastructure", "Autre"):
        if layer not in by_layer:
            continue
        lines.append(f'  subgraph {layer}')
        for name in sorted(by_layer[layer]):
            lines.append("  " + node(name, members[name]).strip())
        lines.append("  end")

    for name, info in sorted(members.items()):
        drawn = set()
        for dep in info["deps"]:
            if dep in members and dep != name:
                lines.append(f"  {name} --> {dep}")
                drawn.add(dep)
        for port in info["implements"]:
            if port in members and port != name and port not in drawn:
                lines.append(f"  {name} -.-> {port}")
                drawn.add(port)
        for used in info["uses"]:
            if used in members and used != name and used not in drawn:
                lines.append(f"  {name} --> {used}")
                drawn.add(used)
    return "\n".join(lines)


def modules_diagram(types: dict) -> str:
    # Vue d'ensemble : les sous-modules, plus parlants que les deux modules racine.
    edges = set()
    for info in types.values():
        for dep in info["deps"]:
            target = types.get(dep)
            if target and target["area"] != info["area"]:
                edges.add((info["area"], target["area"]))
    lines = ["flowchart LR"]
    ids = {a: re.sub(r"\W", "_", a) for a in sorted({i["area"] for i in types.values()})}
    for area, ident in ids.items():
        count = sum(1 for i in types.values() if i["area"] == area)
        lines.append(f'  {ident}["{area}<br/>{count} types"]')
    for source, target in sorted(edges):
        lines.append(f"  {ids[source]} --> {ids[target]}")
    return "\n".join(lines)


LAYERS = ("Api", "Application", "Domain", "Infrastructure", "Autre")
BOX_W, BOX_H, GAP_Y, COL_W, COL_GAP, HEADER, LEGEND_H = 200, 40, 30, 240, 200, 40, 90


def layout(members: dict) -> dict:
    """Positions calculées par Graphviz (dot), en pixels, origine en haut à gauche."""
    edges = []
    for name, info in members.items():
        for target in set(info["deps"] + info["uses"]):
            if target in members and target != name:
                edges.append(f'  "{name}" -> "{target}";')
        # L'implémentation est inversée pour le seul calcul des rangs : elle pointe de
        # l'adaptateur vers le port, ce qui rangeait l'infrastructure à gauche, au niveau des use
        # cases. En l'inversant, l'adaptateur se place après le port qu'il réalise. La flèche
        # affichée, elle, garde son sens.
        for target in set(info["implements"]):
            if target in members and target != name:
                edges.append(f'  "{target}" -> "{name}" [style=invis];')

    # rankdir=LR suit le sens de lecture des dépendances ; les tailles sont données en pouces,
    # Graphviz travaillant à 72 points par pouce.
    source = "\n".join([
        "digraph {", "  rankdir=LR;", "  splines=ortho;", "  nodesep=0.45;", "  ranksep=1.4;",
        f'  node [shape=box, fixedsize=true, width={BOX_W / 72:.3f}, height={BOX_H / 72:.3f}];',
        *[f'  "{name}";' for name in members], *edges, "}"])

    result = subprocess.run(["dot", "-Tplain"], input=source, capture_output=True, text=True)
    if result.returncode != 0:
        return {}

    positions, height = {}, 0.0
    for line in result.stdout.splitlines():
        parts = line.split()
        if parts[0] == "graph":
            height = float(parts[3])
        elif parts[0] == "node":
            # Graphviz centre ses nœuds et compte de bas en haut : on repasse en coin haut-gauche.
            x, y = float(parts[2]), float(parts[3])
            positions[parts[1].strip('"')] = (
                round(x * 72 - BOX_W / 2), round((height - y) * 72 - BOX_H / 2))
    return positions


def drawio(members: dict, title: str) -> str:
    """Produit un fichier draw.io éditable : une colonne par couche, un nœud par type.

    Le XML est écrit en clair (draw.io accepte les deux) pour rester lisible en diff.
    """
    positions = layout(members)
    cells = []

    # Repère visuel de la couche, à défaut de cadre : la couche est déjà lisible dans le placement
    # de Graphviz, qui range les dépendances de gauche à droite.
    fills = {"Api": "#dae8fc", "Application": "#d5e8d4", "Domain": "#ffe6cc",
             "Infrastructure": "#f8cecc", "Autre": "#f5f5f5"}

    # Légende en haut à gauche : sans elle, rien ne dit ce que signifient les couleurs quand on
    # ouvre un fichier au hasard. Le graphe est décalé d'autant vers le bas.
    used = [layer for layer in LAYERS if any(i["layer"] == layer for i in members.values())]
    legend_x = 0
    for layer in used:
        cells.append(
            f'        <mxCell id="legend_{layer}" value="{layer}" '
            f'style="rounded=0;whiteSpace=wrap;html=1;fillColor={fills[layer]};'
            f'strokeColor=#666666;fontSize=11;" vertex="1" parent="1">\n'
            f'          <mxGeometry x="{legend_x}" y="0" width="140" height="30" as="geometry" />\n'
            f'        </mxCell>')
        legend_x += 150
    cells.append(
        f'        <mxCell id="legend_port" value="Port (interface)" '
        f'style="rounded=1;arcSize=40;whiteSpace=wrap;html=1;fillColor=#ffffff;'
        f'strokeColor=#666666;fontSize=11;dashed=0;" vertex="1" parent="1">\n'
        f'          <mxGeometry x="{legend_x}" y="0" width="140" height="30" as="geometry" />\n'
        f'        </mxCell>')

    for name, info in sorted(members.items()):
        x, y = positions.get(name, (40, 40))
        y += LEGEND_H
        shape = "rounded=1;arcSize=40;" if info["kind"] == "interface" else "rounded=0;"
        fill = fills.get(info["layer"], "#ffffff")
        cells.append(
            f'        <mxCell id="{name}" value="{escape(name)}" '
            f'style="{shape}whiteSpace=wrap;html=1;fillColor={fill};strokeColor=#666666;" '
            f'vertex="1" parent="1">\n'
            f'          <mxGeometry x="{x}" y="{y}" width="{BOX_W}" height="{BOX_H}" as="geometry" />\n'
            f'        </mxCell>')

    index = 0
    for name, info in sorted(members.items()):
        drawn = set()
        for target, dashed in ([(d, False) for d in info["deps"]]
                               + [(i, True) for i in info["implements"]]
                               + [(u, False) for u in info["uses"]]):
            if target not in members or target == name or target in drawn:
                continue
            drawn.add(target)
            index += 1
            # Les ancrages suivent la position réelle des deux boîtes : une flèche qui remonte
            # vers la gauche doit sortir par la gauche, sinon elle contourne tout le nœud.
            sx, sy = positions.get(name, (0, 0))
            tx, ty = positions.get(target, (0, 0))
            if tx > sx:
                anchors = "exitX=1;exitY=0.5;entryX=0;entryY=0.5;"
            elif tx < sx:
                anchors = "exitX=0;exitY=0.5;entryX=1;entryY=0.5;"
            elif ty > sy:
                anchors = "exitX=0.5;exitY=1;entryX=0.5;entryY=0;"
            else:
                anchors = "exitX=0.5;exitY=0;entryX=0.5;entryY=1;"
            style = ("edgeStyle=orthogonalEdgeStyle;rounded=1;html=1;jettySize=auto;"
                     + anchors + "exitDx=0;exitDy=0;entryDx=0;entryDy=0;strokeColor=#666666;"
                     + ("dashed=1;" if dashed else ""))
            cells.append(
                f'        <mxCell id="edge{index}" style="{style}" edge="1" parent="1" '
                f'source="{name}" target="{target}">\n'
                f'          <mxGeometry relative="1" as="geometry" />\n'
                f'        </mxCell>')

    body = "\n".join(cells)
    return (f'<mxfile host="Electron">\n'
            f'  <diagram name="{escape(title)}">\n'
            f'    <mxGraphModel dx="1200" dy="800" grid="1" gridSize="10" guides="1" '
            f'connect="1" arrows="1" fold="1" page="1" pageScale="1" math="0" shadow="0">\n'
            f'      <root>\n'
            f'        <mxCell id="0" />\n'
            f'        <mxCell id="1" parent="0" />\n{body}\n'
            f'      </root>\n'
            f'    </mxGraphModel>\n'
            f'  </diagram>\n'
            f'</mxfile>\n')


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)

    # La compilation déclenche cette génération une fois par projet construit : sans verrou, deux
    # passes se marchent dessus (l'une efface un dossier que l'autre est en train de remplir).
    lock = OUT / ".uml.lock"
    try:
        handle = os.open(lock, os.O_CREAT | os.O_EXCL | os.O_WRONLY)
    except FileExistsError:
        return
    os.close(handle)

    try:
        generate()
    finally:
        lock.unlink(missing_ok=True)


def generate() -> None:
    types = scan()

    written_paths: set[Path] = set()

    def write(folder: Path, name: str, title: str, body: str, members: dict | None = None) -> None:
        folder.mkdir(parents=True, exist_ok=True)
        written_paths.add(folder / f"{name}.md")
        if members is not None:
            written_paths.add(folder / f"{name}.drawio")
        (folder / f"{name}.md").write_text(
            f"# {title}\n\n"
            "> Boîte arrondie = port (interface). Trait plein = dépend de. "
            "Trait pointillé = implémente.\n\n"
            f"```mermaid\n{body}\n```\n", encoding="utf8")
        if members is not None:
            (folder / f"{name}.drawio").write_text(drawio(members, title), encoding="utf8")

    write(OUT, "00-modules", "Dépendances entre sous-modules", modules_diagram(types))

    written = []
    for module in sorted({i["module"] for i in types.values()}):
        areas = sorted({i["area"] for i in types.values() if i["module"] == module})
        # Core est découpé par sous-module, sinon le diagramme est illisible ; les modules métier
        # tiennent en un seul fichier.
        for area in areas:
            members = {n: i for n, i in types.items()
                       if i["module"] == module and i["area"] == area}
            folder = OUT / module / area
            write(folder, area.lower(), f"{module} / {area}", diagram(types, members), members)
            written.append((f"{module}/{area}", len(members)))

    # Ménage après coup, et non avant : tant qu'on n'a pas écrit, les fichiers existants restent
    # valides, et rien ne disparaît si la génération échoue en route.
    keep = {OUT / "README.md"}
    for path in list(OUT.rglob("*.drawio")) + list(OUT.rglob("*.md")):
        if path not in keep and path not in written_paths:
            path.unlink()
    for folder in sorted(OUT.rglob("*"), reverse=True):
        if folder.is_dir() and not any(folder.iterdir()):
            folder.rmdir()

    print(f"{len(types)} types -> {OUT.relative_to(ROOT)}/")
    for filename, count in written:
        print(f"  {filename:<34} {count:>3} types")


if __name__ == "__main__":
    main()
