// Every plugin log call's message is a .NET composite format string that Logger passes to string.Format with its
// arguments. A malformed one (an unbalanced brace, or an index with no argument) throws FormatException on the line
// that should have logged, often a rare one: CommandManager's unsupported-version warning had "({1}}". The plugin
// runs only inside Revit (exempt, 0016), so this reads the source and checks each call with .NET's parsing rules.
import { describe, expect, it } from "vitest";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repo = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "..");
function csharp(folder: string): string[] {
  const found: string[] = [];
  for (const name of readdirSync(folder)) {
    const path = join(folder, name);
    if (statSync(path).isDirectory()) {
      if (!["bin", "obj"].includes(name)) found.push(...csharp(path));
    } else if (name.endsWith(".cs")) found.push(path);
  }
  return found;
}

// A C# regular string literal starting at text[at] (a quote): its value and the index after it.
function literal(text: string, at: number): { value: string; end: number } {
  let value = "";
  let i = at + 1;
  for (; text[i] !== '"'; i++) {
    if (i >= text.length) throw new Error("unterminated string literal");
    if (text[i] === "\\") {
      const next = text[++i];
      value += next === "n" ? "\n" : next === "r" ? "\r" : next === "t" ? "\t" : next;
    } else value += text[i];
  }
  return { value, end: i + 1 };
}

// The arguments after the message: top-level commas up to the call's closing parenthesis.
function argumentCount(text: string, from: number): number {
  let depth = 0;
  let count = 0;
  for (let i = from; i < text.length; i++) {
    const c = text[i];
    if (c === '"') {
      i = literal(text, i).end - 1;
    } else if (c === "(" || c === "[" || c === "{") depth++;
    else if (c === ")" || c === "]" || c === "}") {
      if (depth === 0) return count;
      depth--;
    } else if (c === "," && depth === 0) count++;
  }
  throw new Error("unterminated call");
}

// .NET's composite format rules: "{{" and "}}" are literal braces, an item is {index[,alignment][:format]}, and any
// other brace is a FormatException. Returns the problem, or null.
function formatProblem(format: string, args: number): string | null {
  for (let i = 0; i < format.length; i++) {
    const c = format[i];
    if (c === "}") {
      if (format[i + 1] !== "}") return `a lone "}" at ${i}`;
      i++;
    } else if (c === "{") {
      if (format[i + 1] === "{") {
        i++;
        continue;
      }
      const item = /^\{(\d+)\s*(,\s*-?\d+\s*)?(:[^{}]*)?\}/.exec(format.slice(i));
      if (!item) return `a malformed item at ${i}`;
      if (Number(item[1]) >= args) return `{${item[1]}} with ${args} argument(s)`;
      i += item[0].length - 1;
    }
  }
  return null;
}

const calls = csharp(join(repo, "plugin")).flatMap((file) => {
  const text = readFileSync(file, "utf8");
  const found: { where: string; format: string; args: number }[] = [];
  for (const call of text.matchAll(/^[^\n]*?\b_logger\.(?:Info|Warning|Error|Debug)\(\s*/gm)) {
    if (/^\s*\/\//.test(call[0])) continue; // commented out
    const start = call.index! + call[0].length;
    const where = `${file.slice(repo.length + 1).split("\\").join("/")}:${text.slice(0, start).split("\n").length}`;
    expect(text[start], `${where}: the message must be a string literal`).toBe('"');
    const message = literal(text, start);
    found.push({ where, format: message.value, args: argumentCount(text, message.end) });
  }
  return found;
});

describe("plugin log messages", () => {
  it("the parser reads .NET's rules, including the bug it guards against", () => {
    expect(formatProblem("version ({1}} skipped", 2)).toMatch(/lone/);
    expect(formatProblem("{0} and {1}", 1)).toMatch(/argument/);
    expect(formatProblem("{0", 1)).toMatch(/malformed/);
    expect(formatProblem("{{literal}} {0,-5:N1} {1}", 2)).toBeNull();
  });

  it("finds the plugin's log calls", () => {
    expect(calls.length).toBeGreaterThan(20);
  });

  it("every message is a valid format string for its arguments", () => {
    const bad = calls
      .map((c) => ({ ...c, problem: formatProblem(c.format, c.args) }))
      .filter((c) => c.problem)
      .map((c) => `${c.where}: ${c.problem}`);
    expect(bad).toEqual([]);
  });
});
