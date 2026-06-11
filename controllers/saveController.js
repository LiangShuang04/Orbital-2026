import SaveProfile from "#models/SaveProfile";

const allowedRootFields = new Set(["playerTransform", "survivalStats", "inventory", "baseModules", "objectiveState"]);
const allowedTransformFields = new Set(["position", "rotation"]);
const allowedVectorFields = new Set(["x", "y", "z"]);
const allowedSurvivalStats = new Set(["health", "oxygen", "hunger", "toxicity"]);
const allowedInventoryFields = new Set(["itemId", "quantity"]);
const allowedItemIds = new Set(["metal_scrap", "filter_fibre"]);
const allowedBaseModuleFields = new Set(["moduleId", "isActive", "position"]);
const allowedModuleIds = new Set(["oxygen_station", "storage_unit", "power_generator", "signal_generator"]);
const allowedObjectiveFields = new Set(["currentQuest", "signalGeneratorProgress", "completedObjectives", "activeTimers"]);
const allowedTimerFields = new Set(["timerId", "remainingSeconds", "isPaused"]);

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

const assertOnlyFields = (value, allowedFields, label) => {
  const invalidField = Object.keys(value).find(field => !allowedFields.has(field));

  if (invalidField) {
    throw httpError(400, `${label} contains unsupported field '${invalidField}'`);
  }
};

const assertNumber = (value, label, minimum, maximum) => {
  if (typeof value !== "number" || !Number.isFinite(value)) {
    throw httpError(400, `${label} must be a finite number`);
  }

  if (value < minimum || value > maximum) {
    throw httpError(400, `${label} must be between ${minimum} and ${maximum}`);
  }
};

const assertInteger = (value, label, minimum, maximum) => {
  if (!Number.isInteger(value)) {
    throw httpError(400, `${label} must be an integer`);
  }

  if (value < minimum || value > maximum) {
    throw httpError(400, `${label} must be between ${minimum} and ${maximum}`);
  }
};

const assertString = (value, label, maximumLength = 80) => {
  if (typeof value !== "string" || !value.trim()) {
    throw httpError(400, `${label} must be a non-empty string`);
  }

  if (value.trim().length > maximumLength) {
    throw httpError(400, `${label} cannot exceed ${maximumLength} characters`);
  }
};

const validateVector = (value, label) => {
  assertObject(value, label);
  assertOnlyFields(value, allowedVectorFields, label);

  for (const axis of allowedVectorFields) {
    assertNumber(value[axis], `${label}.${axis}`, -100000, 100000);
  }

  return {
    x: value.x,
    y: value.y,
    z: value.z
  };
};

const validateInventory = inventory => {
  if (!Array.isArray(inventory)) {
    throw httpError(400, "inventory must be an array");
  }

  const seenItemIds = new Set();

  return inventory.map((item, index) => {
    assertObject(item, `inventory[${index}]`);
    assertOnlyFields(item, allowedInventoryFields, `inventory[${index}]`);
    assertString(item.itemId, `inventory[${index}].itemId`);

    if (!allowedItemIds.has(item.itemId)) {
      throw httpError(400, `inventory[${index}].itemId is not allowed`);
    }

    if (seenItemIds.has(item.itemId)) {
      throw httpError(400, `inventory contains duplicate item '${item.itemId}'`);
    }

    seenItemIds.add(item.itemId);
    assertInteger(item.quantity, `inventory[${index}].quantity`, 0, 9999);

    return {
      itemId: item.itemId,
      quantity: item.quantity
    };
  });
};

const validateBaseModules = baseModules => {
  if (!Array.isArray(baseModules)) {
    throw httpError(400, "baseModules must be an array");
  }

  return baseModules.map((module, index) => {
    assertObject(module, `baseModules[${index}]`);
    assertOnlyFields(module, allowedBaseModuleFields, `baseModules[${index}]`);
    assertString(module.moduleId, `baseModules[${index}].moduleId`);

    if (!allowedModuleIds.has(module.moduleId)) {
      throw httpError(400, `baseModules[${index}].moduleId is not allowed`);
    }

    if (typeof module.isActive !== "boolean") {
      throw httpError(400, `baseModules[${index}].isActive must be a boolean`);
    }

    return {
      moduleId: module.moduleId,
      isActive: module.isActive,
      position: validateVector(module.position, `baseModules[${index}].position`)
    };
  });
};

const validateObjectiveState = objectiveState => {
  assertObject(objectiveState, "objectiveState");
  assertOnlyFields(objectiveState, allowedObjectiveFields, "objectiveState");

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

    sanitized.completedObjectives = objectiveState.completedObjectives.map((objectiveId, index) => {
      assertString(objectiveId, `objectiveState.completedObjectives[${index}]`);
      return objectiveId.trim();
    });
  }

  if (objectiveState.activeTimers !== undefined) {
    if (!Array.isArray(objectiveState.activeTimers)) {
      throw httpError(400, "objectiveState.activeTimers must be an array");
    }

    sanitized.activeTimers = objectiveState.activeTimers.map((timer, index) => {
      assertObject(timer, `objectiveState.activeTimers[${index}]`);
      assertOnlyFields(timer, allowedTimerFields, `objectiveState.activeTimers[${index}]`);
      assertString(timer.timerId, `objectiveState.activeTimers[${index}].timerId`);
      assertInteger(timer.remainingSeconds, `objectiveState.activeTimers[${index}].remainingSeconds`, 0, 86400);

      if (typeof timer.isPaused !== "boolean") {
        throw httpError(400, `objectiveState.activeTimers[${index}].isPaused must be a boolean`);
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
  assertOnlyFields(body, allowedRootFields, "request body");

  const update = {};

  if (body.playerTransform !== undefined) {
    assertObject(body.playerTransform, "playerTransform");
    assertOnlyFields(body.playerTransform, allowedTransformFields, "playerTransform");

    if (body.playerTransform.position !== undefined) {
      update["playerTransform.position"] = validateVector(body.playerTransform.position, "playerTransform.position");
    }

    if (body.playerTransform.rotation !== undefined) {
      update["playerTransform.rotation"] = validateVector(body.playerTransform.rotation, "playerTransform.rotation");
    }
  }

  if (body.survivalStats !== undefined) {
    assertObject(body.survivalStats, "survivalStats");
    assertOnlyFields(body.survivalStats, allowedSurvivalStats, "survivalStats");

    for (const stat of allowedSurvivalStats) {
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

export const createSaveProfile = handleAsync(async (req, res) => {
  const existingProfile = await SaveProfile.findOne({ userId: req.user.id });

  if (existingProfile) {
    throw httpError(409, "Save profile already exists");
  }

  const saveProfile = await SaveProfile.create({ userId: req.user.id });

  res.status(201).json({
    success: true,
    saveProfile
  });
});

export const getSaveProfile = handleAsync(async (req, res) => {
  const saveProfile = await SaveProfile.findOne({ userId: req.user.id });

  if (!saveProfile) {
    throw httpError(404, "Save profile not found");
  }

  res.status(200).json({
    success: true,
    saveProfile
  });
});

export const updateSaveProfile = handleAsync(async (req, res) => {
  const update = buildSaveUpdate(req.body);
  const saveProfile = await SaveProfile.findOneAndUpdate(
    { userId: req.user.id },
    { $set: update },
    {
      new: true,
      runValidators: true
    }
  );

  if (!saveProfile) {
    throw httpError(404, "Save profile not found");
  }

  res.status(200).json({
    success: true,
    saveProfile
  });
});
