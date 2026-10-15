// One-off codemod: bring the Retail / Technical / Accounts views in line with the
// Admin page header pattern (breadcrumbs + .page-header + .page-head__actions)
// and swap the solid _Badge status rendering for the Admin _StatusPill.
//
// Usage: node tools/panels-header-transform.js [--write]
const fs = require("fs");
const path = require("path");

const ROOT = path.join(__dirname, "..", "Nexos Network System", "Areas");
const AREAS = ["Retail", "Technical", "Accounts"];
const WRITE = process.argv.includes("--write");

const CRUMB_LABELS = {
  Customers: "Customers",
  Orders: "Orders",
  Bills: "Bills",
  Connections: "Connections",
  Payments: "Payments",
  Plans: "Plans",
  Feasibility: "Feasibility",
  Equipment: "Equipment",
  Dues: "Dues",
};

/** Finds the index just past the `</div>` that closes the `<div ...>` starting at `open`. */
function matchDivEnd(text, open) {
  let depth = 0;
  const re = /<div\b|<\/div>/g;
  re.lastIndex = open;
  let m;
  while ((m = re.exec(text))) {
    depth += m[0] === "</div>" ? -1 : 1;
    if (depth === 0) return re.lastIndex;
  }
  return -1;
}

function crumbs(area, label) {
  return `<nav class="crumbs" aria-label="Breadcrumb">
    <a asp-area="${area}" asp-controller="Dashboard" asp-action="Index">
        <svg class="svg-icon" aria-hidden="true"><use href="#i-home-clean" /></svg>
        <span>Dashboard</span>
    </a>
    <span class="crumbs__sep" aria-hidden="true">&rsaquo;</span>
    <span class="crumbs__current" aria-current="page">${label}</span>
</nav>`;
}

/** Unwraps `<div class="d-flex gap-2"> ... </div>` so the buttons sit in .page-head__actions. */
function normalizeActions(actions) {
  const trimmed = actions.trim();
  const m = trimmed.match(/^<div class="d-flex gap-2">([\s\S]*)<\/div>$/);
  return m ? m[1].trim() : trimmed;
}

function transform(text, area, controller) {
  const notes = [];
  const wrapperOpen = '<div class="d-flex justify-content-between align-items-center flex-wrap gap-2 mb-3">';
  let head = null;

  const wIdx = text.indexOf(wrapperOpen);
  if (wIdx !== -1) {
    const end = matchDivEnd(text, wIdx);
    if (end === -1) return { text, notes: ["unbalanced div"] };
    const inner = text.slice(wIdx + wrapperOpen.length, end - "</div>".length);
    const innerTrim = inner.trim();

    // First child is the title block, everything after it is the action area.
    const relOpen = innerTrim.indexOf("<div>");
    const titleEnd = matchDivEnd(innerTrim, relOpen);
    const titleBlock = innerTrim.slice(relOpen, titleEnd);
    const actions = normalizeActions(innerTrim.slice(titleEnd));

    const title = (titleBlock.match(/<h1 class="h4 mb-1">([\s\S]*?)<\/h1>/) || [, ""])[1].trim();
    const subtitle = (titleBlock.match(/<p class="text-muted mb-0">([\s\S]*?)<\/p>/) || [, ""])[1].trim();
    const label = /@|\{/.test(title) ? CRUMB_LABELS[controller] || controller : title;

    const lines = [];
    lines.push(crumbs(area, label));
    lines.push("");
    lines.push('<div class="page-header">');
    lines.push("    <div>");
    lines.push(`        <h1 class="h3">${title}</h1>`);
    lines.push(`        <p class="page-subtitle">${subtitle}</p>`);
    lines.push("    </div>");
    if (actions) {
      lines.push('    <div class="page-head__actions">');
      lines.push(
        actions
          .split(/\r?\n/)
          .map((l) => (l.trim() ? "        " + l.trim() : l))
          .join("\n")
      );
      lines.push("    </div>");
    }
    lines.push("</div>");

    head = { start: wIdx, end, block: lines.join("\n") };
    notes.push("wrapped");
  } else {
    const hIdx = text.indexOf('<h1 class="h4 mb-1">');
    if (hIdx === -1) return { text, notes: ["no header"] };
    const hEnd = text.indexOf("</h1>", hIdx) + "</h1>".length;
    const title = text.slice(hIdx + '<h1 class="h4 mb-1">'.length, hEnd - "</h1>".length).trim();

    // Optional subtitle paragraph immediately after the heading.
    let subEnd = hEnd;
    let subtitle = "";
    const subMatch = text.slice(hEnd).match(/^\r?\n    <p class="text-muted(?: mb-0)?">([\s\S]*?)<\/p>/);
    if (subMatch) {
      subtitle = subMatch[1].trim();
      subEnd = hEnd + subMatch[0].length;
    }

    const label = /@|\{/.test(title) ? CRUMB_LABELS[controller] || controller : title;
    const lines = [crumbs(area, label), "", '<div class="page-header">', "    <div>", `        <h1 class="h3">${title}</h1>`];
    if (subtitle) lines.push(`        <p class="page-subtitle">${subtitle}</p>`);
    lines.push("    </div>", "</div>");

    head = { start: hIdx, end: subEnd, block: lines.join("\n") };
    notes.push("bare");
  }

  let out = text.slice(0, head.start) + head.block + text.slice(head.end);
  const before = out;
  out = out.replace(/<partial name="_Badge"/g, '<partial name="_StatusPill"');
  if (out !== before) notes.push("badge->pill");
  return { text: out, notes };
}

let changed = 0;
for (const area of AREAS) {
  const viewsDir = path.join(ROOT, area, "Views");
  for (const dir of fs.readdirSync(viewsDir)) {
    const dirPath = path.join(viewsDir, dir);
    if (!fs.statSync(dirPath).isDirectory()) continue;
    for (const file of fs.readdirSync(dirPath)) {
      if (!file.endsWith(".cshtml") || file.startsWith("_")) continue;
      if (dir === "Dashboard") continue; // already rebuilt by hand
      const full = path.join(dirPath, file);
      const text = fs.readFileSync(full, "utf8");
      if (!text.includes('<h1 class="h4 mb-1">')) continue;
      const { text: next, notes } = transform(text, area, dir);
      if (next === text) {
        console.log(`SKIP  ${area}/${dir}/${file}  [${notes.join(", ")}]`);
        continue;
      }
      changed++;
      console.log(`EDIT  ${area}/${dir}/${file}  [${notes.join(", ")}]`);
      if (WRITE) fs.writeFileSync(full, next, "utf8");
    }
  }
}
console.log(`\n${changed} file(s) ${WRITE ? "written" : "would change"}`);
