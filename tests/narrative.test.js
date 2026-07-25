import assert from "node:assert/strict";
import { readFile, readdir } from "node:fs/promises";
import path from "node:path";
import test from "node:test";

const root = process.cwd();
const jsonPath = path.join(root, "Assets", "Resources", "Narrative", "narrative_mvp.json");
const narrativeRoot = path.join(root, "Assets", "Scripts", "Narrative");
const db = JSON.parse(await readFile(jsonPath, "utf8"));
const sequences = new Map(db.sequences.map(sequence => [sequence.id, sequence]));
const objectives = new Set(db.objectives.map(objective => objective.id));

async function findFiles(dir, extension) {
  const entries = await readdir(dir, { withFileTypes: true });
  const files = [];

  for (const entry of entries) {
    const fullPath = path.join(dir, entry.name);

    if (entry.isDirectory()) {
      files.push(...await findFiles(fullPath, extension));
    } else if (entry.name.endsWith(extension)) {
      files.push(fullPath);
    }
  }

  return files;
}

test("narrative IDs and dialogue links are valid", () => {
  assert.equal(sequences.size, db.sequences.length);
  assert.equal(objectives.size, db.objectives.length);
  assert.equal(db.sequences.length, 53);

  for (const sequence of db.sequences) {
    const lines = new Set(sequence.lines.map(line => line.id));
    assert.equal(lines.size, sequence.lines.length, `duplicate line in ${sequence.id}`);

    for (const line of sequence.lines) {
      if (line.nextLineId && line.nextLineId !== "END") {
        assert.ok(lines.has(line.nextLineId), `${sequence.id} points to missing ${line.nextLineId}`);
      }

      for (const choice of line.choices ?? []) {
        if (choice.nextLineId && choice.nextLineId !== "END") {
          assert.ok(lines.has(choice.nextLineId), `${sequence.id} choice points to missing ${choice.nextLineId}`);
        }
      }
    }

    if (sequence.nextObjective) {
      assert.ok(objectives.has(sequence.nextObjective), `${sequence.id} has an unknown next objective`);
    }

    if (sequence.completeObjective) {
      assert.ok(objectives.has(sequence.completeObjective), `${sequence.id} completes an unknown objective`);
    }
  }
});

test("canonical story events have runtime emitters", async () => {
  const required = [
    "TRG_COCKPIT_WAKE",
    "TRG_COCKPIT_INSPECTION",
    "TRG_CREW_DISCOVERY",
    "TRG_CAPTAIN_BADGE",
    "TRG_GENERATOR_ONLINE",
    "TRG_FIRST_ROBOT",
    "TRG_FIELD_FILTER_CRAFTED",
    "EVT_TOXIC_STORM_STORY",
    "TRG_RUINS_ENTERED",
    "TRG_COMPONENT_LENS",
    "TRG_COMPONENT_COIL",
    "TRG_COMPONENT_CORE",
    "TRG_SIGNAL_GENERATOR_CRAFTED",
    "TRG_SIGNAL_GENERATOR_INSTALLED",
    "TRG_SIGNAL_DEFENSE",
    "TRG_SIGNAL_CHARGE_100",
    "TRG_RESCUE_RESPONSE",
    "TRG_EPILOGUE"
  ];
  const files = await findFiles(narrativeRoot, ".cs");
  const source = (await Promise.all(files.map(file => readFile(file, "utf8")))).join("\n");

  for (const id of required) {
    assert.ok(sequences.has(id), `missing sequence ${id}`);
    assert.ok(source.includes(`"${id}"`), `missing runtime emitter for ${id}`);
  }
});

test("signal defense milestones are ordered by prerequisites", () => {
  const expected = new Map([
    ["TRG_SIGNAL_CHARGE_25", "signal_defense_started"],
    ["TRG_SIGNAL_CHARGE_60", "signal_charge_25"],
    ["TRG_SIGNAL_CHARGE_90", "signal_charge_60"],
    ["TRG_SIGNAL_CHARGE_100", "signal_charge_90"],
    ["TRG_RESCUE_RESPONSE", "transmission_sent"],
    ["TRG_EPILOGUE", "rescue_confirmed"]
  ]);

  for (const [id, flag] of expected) {
    assert.ok(sequences.get(id).requiredFlags?.includes(flag), `${id} must require ${flag}`);
  }
});

test("canonical progression sequences cannot replay", () => {
  for (const sequence of db.sequences) {
    if (sequence.id.startsWith("TRG_") || sequence.id.startsWith("EVT_")) {
      assert.equal(sequence.oneShot, true, `${sequence.id} must be one-shot`);
    }
  }
});

test("repeatable warnings have cooldown protection", () => {
  const repeatable = db.sequences.filter(sequence => sequence.oneShot === false);
  assert.ok(repeatable.length > 0);

  for (const sequence of repeatable) {
    assert.ok(sequence.cooldownSeconds > 0, `${sequence.id} has no cooldown`);
  }
});

test("ending B completion chain is explicit", () => {
  assert.equal(sequences.get("TRG_SIGNAL_CHARGE_100").setFlag, "transmission_sent");
  assert.equal(sequences.get("TRG_RESCUE_RESPONSE").setFlag, "rescue_confirmed");
  assert.equal(sequences.get("TRG_EPILOGUE").setFlag, "story_complete");
  assert.equal(sequences.get("TRG_EPILOGUE").signalProgress, 100);
});

test("defense source keeps production timing and persistence", async () => {
  const file = path.join(narrativeRoot, "Runtime", "NarrativeSignalDefense.cs");
  const timelineFile = path.join(narrativeRoot, "Runtime", "NarrativeDefenseTimeline.cs");
  const [source, timeline] = await Promise.all([
    readFile(file, "utf8"),
    readFile(timelineFile, "utf8")
  ]);
  assert.match(timeline, /DurationSeconds = 180f/);
  assert.match(timeline, /Progress >= 0\.25f/);
  assert.match(timeline, /Progress >= 0\.6f/);
  assert.match(timeline, /Progress >= 0\.9f/);
  assert.match(source, /signalDefenseRemainingSeconds/);
  assert.match(source, /PersistProgress\(true\)/);
});

test("dialogue queue rechecks priority and eligibility", async () => {
  const file = path.join(narrativeRoot, "Runtime", "NarrativeDirector.cs");
  const source = await readFile(file, "utf8");
  assert.match(source, /sequence\.priority > activeSequence\.priority/);
  assert.match(source, /queuedSequences\.Sort/);
  assert.match(source, /if \(!CanPlay\(sequence\)\)/);
  assert.match(source, /HasCompletedSequence\(sequence\.id\)/);
  assert.match(source, /CancelQueuedSequence/);
});

test("all player-facing narrative copy is English ASCII", () => {
  const strings = [];

  for (const objective of db.objectives) {
    strings.push(objective.title, objective.description);
  }

  for (const sequence of db.sequences) {
    for (const line of sequence.lines) {
      strings.push(line.speaker, line.text);

      for (const choice of line.choices ?? []) {
        strings.push(choice.text);
      }
    }
  }

  for (const value of strings) {
    assert.match(value, /^[\x00-\x7F]*$/);
  }
});

test("narrative production sources contain no code comments", async () => {
  const files = await findFiles(narrativeRoot, ".cs");

  for (const file of files) {
    const source = await readFile(file, "utf8");
    assert.doesNotMatch(source, /(^|\s)\/\//m, path.relative(root, file));
    assert.doesNotMatch(source, /\/\*/, path.relative(root, file));
  }
});
