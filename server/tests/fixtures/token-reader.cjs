// The TS client's read of the token (readFileSync, utf8), in a loop until the stop file appears; for
// tests/plugin/runtime.test.ts. Prints what it saw. A read must give the old token or the new one, never a partial one,
// and the file must never vanish once written (decision 0022 §3). The token is never printed.
const fs = require("node:fs");
const [file, stop] = process.argv.slice(2);
let reads = 0;
let partial = 0;
let enoentAfterFirst = 0;
let seen = false;
const errors = {};
while (!fs.existsSync(stop)) {
  try {
    const text = fs.readFileSync(file, "utf8");
    reads++;
    seen = true;
    if (!/^[0-9a-f]{64}$/.test(text)) partial++;
  } catch (error) {
    if (error.code === "ENOENT") {
      if (seen) enoentAfterFirst++;
    } else errors[error.code] = (errors[error.code] ?? 0) + 1;
  }
}
console.log(JSON.stringify({ reads, partial, enoentAfterFirst, errors }));
