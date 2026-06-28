import SaveProfile from "#models/SaveProfile";

const rootFields = new Set(["worldSeed", "playerTransform", "survivalStats", "inventory", "baseModules", "objectiveState"]);
const transformFields = new Set(["position", "rotation"]);
const vecFields = new Set(["x", "y", "z"]);
const statFields = new Set(["health", "oxygen", "hunger", "toxicity"]);
const inventoryFields = new Set(["itemId", "quantity"]);
const itemIds = new Set(["metal_scrap", "filter_fibre"]);
const moduleFields = new Set(["moduleId", "isActive", "position"]);
const moduleIds = new Set(["oxygen_station", "storage_unit", "power_generator", "signal_generator"]);
const objectiveFields = new Set(["currentQuest", "signalGeneratorProgress", "completedObjectives", "activeTimers"]);
const timerFields = new Set(["timerId", "remainingSeconds", "isPaused"]);

const handleAsync = action => (req, res, next) => Promise.resolve(action(req, res, next)).catch(next);

const httpError = (statusCode, message) => {
  const error = new Error(message);
  error.statusCode = statusCode;
  return error;
};

const assertObject = (value, label) => {
  if (!value || typeof value !== "object" || Array.isArray(value)) {
    throw httpError(400, `${label} must be an object`);
  }
};

const assertOnlyFields = (value, allowed, label) => {
  const badField = Object.keys(value).find(field => !allowed.has(field));

  if (badField) {
    throw httpError(400, `${label} contains unsupported field '${badField}'`);
  }
};

const assertNumber = (value, label, min, max) => {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    throw httpError(400, `${label} must be a finite number`);
  }

  if (value < min || value > max) {
    throw httpError(400, `${label} must be between ${min} and ${max}`);
  }
};

const assertInteger = (value, label, min, max) => {
  if (!Number.isInteger(value)) {
    throw httpError(400, `${label} must be an integer`);
  }

  if (value < min || value > max) {
    throw httpError(400, `${label} must be between ${min} and ${max}`);
  }
};

const assertString = (value, label, maxLen = 80) => {
  if (typeof value !== "string" || !value.trim()) {
    throw httpError(400, `${label} must be a non-empty string`);
  }

  if (value.trim().length > maxLen) {
    throw httpError(400, `${label} cannot exceed ${maxLen} characters`);
  }
};

const validateVector = (value, label) => {
  assertObject(value, label);
  assertOnlyFields(value, vecFields, label);

  for (const axis of vecFields) {
    assertNumber(value[axis], `${label}.${axis}`, -100000, 100000);
  }

  return {
    x: value.x,
    y: value.y,
    z: value.z
  };
};

const validateInventory = inv => {
  if (!Array.isArray(inv)) {
    throw httpError(400, "inventory must be an array");
  }

  const seenIds = new Set();

  return inv.map((item, idx) => {
    assertObject(item, `inventory[${idx}]`);
    assertOnlyFields(item, inventoryFields, `inventory[${idx}]`);
    assertString(item.itemId, `inventory[${idx}].itemId`);

    if (!itemIds.has(item.itemId)) {
      throw httpError(400, `inventory[${idx}].itemId is not allowed`);
    }

    if (seenIds.has(item.itemId)) {
      throw httpError(400, `inventory contains duplicate item '${item.itemId}'`);
    }

    seenIds.add(item.itemId);
    assertInteger(item.quantity, `inventory[${idx}].quantity`, 0, 9999);

    return {
      itemId: item.itemId,
      quantity: item.quantity
    };
  });
};

const validateBaseModules = modules => {
  if (!Array.isArray(modules)) {
    throw httpError(400, "baseModules must be an array");
  }

  return modules.map((module, idx) => {
    assertObject(module, `baseModules[${idx}]`);
    assertOnlyFields(module, moduleFields, `baseModules[${idx}]`);
    assertString(module.moduleId, `baseModules[${idx}].moduleId`);

    if (!moduleIds.has(module.moduleId)) {
      throw httpError(400, `baseModules[${idx}].moduleId is not allowed`);
    }

    if (typeof module.isActive !== "boolean") {
      throw httpError(400, `baseModules[${idx}].isActive must be a boolean`);
    }

    return {
      moduleId: module.moduleId,
      isActive: module.isActive,
      position: validateVector(module.position, `baseModules[${idx}].position`)
    };
  });
};

const validateObjectiveState = objectiveState => {
  assertObject(objectiveState, "objectiveState");
  assertOnlyFields(objectiveState, objectiveFields, "objectiveState");

  const sanitized = {};

  if (objectiveState.currentQuest !== undefined) {
    assertString(objectiveState.currentQuest, "objectiveState.currentQuest");
    sanitized.currentQuest = objectiveState.currentQuest.trim();
  }

  if (objectiveState.signalGeneratorProgress !== undefined) {
    assertNumber(objectiveState.signalGeneratorProgress, "objectiveState.signalGeneratorProgress", 0, 100);
    sanitized.signalGeneratorProgress = objectiveState.signalGeneratorProgress;
  }

  if (objectiveState.completedObjectives !== undefined) {
    if (!Array.isArray(objectiveState.completedObjectives)) {
      throw httpError(400, "objectiveState.completedObjectives must be an array");
    }

    sanitized.completedObjectives = objectiveState.completedObjectives.map((objectiveId, idx) => {
      assertString(objectiveId, `objectiveState.completedObjectives[${idx}]`);
      return objectiveId.trim();
    });
  }

  if (objectiveState.activeTimers !== undefined) {
    if (!Array.isArray(objectiveState.activeTimers)) {
      throw httpError(400, "objectiveState.activeTimers must be an array");
    }

    sanitized.activeTimers = objectiveState.activeTimers.map((timer, idx) => {
      assertObject(timer, `objectiveState.activeTimers[${idx}]`);
      assertOnlyFields(timer, timerFields, `objectiveState.activeTimers[${idx}]`);
      assertString(timer.timerId, `objectiveState.activeTimers[${idx}].timerId`);
      assertInteger(timer.remainingSeconds, `objectiveState.activeTimers[${idx}].remainingSeconds`, 0, 86400);

      if (typeof timer.isPaused !== "boolean") {
        throw httpError(400, `objectiveState.activeTimers[${idx}].isPaused must be a boolean`);
      }

      return {
        timerId: timer.timerId.trim(),
        remainingSeconds: timer.remainingSeconds,
        isPaused: timer.isPaused
      };
    });
  }

  return sanitized;
};

const buildSaveUpdate = body => {
  assertObject(body, "request body");
  assertOnlyFields(body, rootFields, "request body");

  const update = {};

  if (body.worldSeed !== undefined) {
    assertInteger(body.worldSeed, "worldSeed", -2147483648, 2147483647);
    update.worldSeed = body.worldSeed;
  }

  if (body.playerTransform !== undefined) {
    assertObject(body.playerTransform, "playerTransform");
    assertOnlyFields(body.playerTransform, transformFields, "playerTransform");

    if (body.playerTransform.position !== undefined) {
      update["playerTransform.position"] = validateVector(body.playerTransform.position, "playerTransform.position");
    }

    if (body.playerTransform.rotation !== undefined) {
      update["playerTransform.rotation"] = validateVector(body.playerTransform.rotation, "playerTransform.rotation");
    }
  }

  if (body.survivalStats !== undefined) {
    assertObject(body.survivalStats, "survivalStats");
    assertOnlyFields(body.survivalStats, statFields, "survivalStats");

    for (const stat of statFields) {
      if (body.survivalStats[stat] !== undefined) {
        assertNumber(body.survivalStats[stat], `survivalStats.${stat}`, 0, 100);
        update[`survivalStats.${stat}`] = body.survivalStats[stat];
      }
    }
  }

  if (body.inventory !== undefined) {
    update.inventory = validateInventory(body.inventory);
  }

  if (body.baseModules !== undefined) {
    update.baseModules = validateBaseModules(body.baseModules);
  }

  if (body.objectiveState !== undefined) {
    const objectiveState = validateObjectiveState(body.objectiveState);

    for (const [key, value] of Object.entries(objectiveState)) {
      update[`objectiveState.${key}`] = value;
    }
  }

  if (!Object.keys(update).length) {
    throw httpError(400, "At least one save field is required");
  }

  return update;
};

const buildSaveCreate = body => {
  assertObject(body, "request body");
  assertOnlyFields(body, new Set(["worldSeed"]), "request body");

  if (body.worldSeed === undefined) {
    return {};
  }

  assertInteger(body.worldSeed, "worldSeed", -2147483648, 2147483647);
  return {
    worldSeed: body.worldSeed
  };
};

export const createSaveProfile = handleAsync(async (req, res) => {
  const existing = await SaveProfile.findOne({ userId: req.user.id });

  if (existing) {
    throw httpError(409, "Save profile already exists");
  }

  const payload = buildSaveCreate(req.body ?? {});
  const save = await SaveProfile.create({
    userId: req.user.id,
    ...payload
  });

  res.status(201).json({
    success: true,
    saveProfile: save
  });
});

export const getSaveProfile = handleAsync(async (req, res) => {
  const save = await SaveProfile.findOne({ userId: req.user.id });

  if (!save) {
    throw httpError(404, "Save profile not found");
  }

  res.status(200).json({
    success: true,
    saveProfile: save
  });
});

export const updateSaveProfile = handleAsync(async (req, res) => {
  const update = buildSaveUpdate(req.body);
  const save = await SaveProfile.findOneAndUpdate(
    { userId: req.user.id },
    { $set: update },
    {
      new: true,
      runValidators: true
    }
  );

  if (!save) {
    throw httpError(404, "Save profile not found");
  }

  res.status(200).json({
    success: true,
    saveProfile: save
  });
});
