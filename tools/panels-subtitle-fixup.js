// Follow-up pass for the panel header codemod:
//  1. move intro paragraphs that sat below the converted header into .page-header
//  2. restore the original CRLF line endings (the codemod inserted LF-only blocks)
// Usage: node tools/panels-subtitle-fixup.js [--write]
const fs = require("fs");
const path = require("path");

const ROOT = path.join(__dirname, "..", "Nexos Network System", "Areas");
const AREAS = ["Retail", "Technical", "Accounts"];
const WRITE = process.argv.includes("--write");

const RE =
  /(        <h1 class="h3">[\s\S]*?<\/h1>)\r?\n    <\/div>\r?\n<\/div>\r?\n+<p class="text-muted">([\s\S]*?)<\/p>/;

let moved = 0;
let normalized = 0;
for (const area of AREAS) {
  for (const dir of fs.readdirSync(path.join(ROOT, area, "Views"))) {
    const dirPath = path.join(ROOT, area, "Views", dir);
    if (!fs.statSync(dirPath).isDirectory()) continue;
    for (const file of fs.readdirSync(dirPath)) {
      if (!file.endsWith(".cshtml") || file.startsWith("_")) continue;
      const full = path.join(dirPath, file);
      const original = fs.readFileSync(full, "utf8");
      let next = original;

      if (RE.test(next)) {
        next = next.replace(RE, (_m, head, sub) => {
          const subLines = sub
            .trim()
            .split(/\r?\n/)
            .map((l, i) => (i === 0 ? l.trim() : "            " + l.trim()))
            .join("\n");
          return `${head}\n        <p class="page-subtitle">${subLines}</p>\n    </div>\n</div>`;
        });
        moved++;
      }

      // The codemod emitted LF inside otherwise CRLF files; make each file consistent again.
      if (original.includes("\r\n") && /(?<!\r)\n/.test(next)) {
        next = next.replace(/\r\n|\n/g, "\r\n");
        normalized++;
      }

      if (next !== original) {
        console.log(`EDIT  ${area}/${dir}/${file}`);
        if (WRITE) fs.writeFileSync(full, next, "utf8");
      }
    }
  }
}
console.log(`\nsubtitle moved in ${moved}, line endings normalized in ${normalized} ${WRITE ? "(written)" : "(dry run)"}`);
