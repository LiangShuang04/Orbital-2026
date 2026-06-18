import mongoose from "mongoose";

const { Schema } = mongoose;

const coordinate = () => ({
  type: Number,
  required: true,
  default: 0,
  min: [-100000, "Coordinate is below the allowed world boundary"],
  max: [100000, "Coordinate is above the allowed world boundary"]
});

const survivalStat = defaultValue => ({
  type: Number,
  required: true,
  default: defaultValue,
  min: [0, "Survival stat cannot be below 0"],
  max: [100, "Survival stat cannot exceed 100"]
});

const integerValue = (minimum, maximum) => ({
  type: Number,
  required: true,
  min: [minimum, `Value cannot be below ${minimum}`],
  max: [maximum, `Value cannot exceed ${maximum}`],
  validate: {
    validator: Number.isInteger,
    message: "Value must be an integer"
  }
});

const vectorSchema = new Schema(
  {
    x: coordinate(),
    y: coordinate(),
    z: coordinate()
  },
  { _id: false }
);

const playerTransformSchema = new Schema(
  {
    position: {
      type: vectorSchema,
      default: () => ({ x: 0, y: 0, z: 0 })
    },
    rotation: {
      type: vectorSchema,
      default: () => ({ x: 0, y: 0, z: 0 })
    }
  },
  { _id: false }
);

const survivalStatsSchema = new Schema(
  {
    health: survivalStat(100),
    oxygen: survivalStat(100),
    hunger: survivalStat(100),
    toxicity: survivalStat(0)
  },
  { _id: false }
);

const inventoryItemSchema = new Schema(
  {
    itemId: {
      type: String,
      required: true,
      enum: ["metal_scrap", "filter_fibre"]
    },
    quantity: integerValue(0, 9999)
  },
  { _id: false }
);

const baseModuleSchema = new Schema(
  {
    moduleId: {
      type: String,
      required: true,
      enum: ["oxygen_station", "storage_unit", "power_generator", "signal_generator"]
    },
    isActive: {
      type: Boolean,
      required: true,
      default: true
    },
    position: {
      type: vectorSchema,
      required: true,
      default: () => ({ x: 0, y: 0, z: 0 })
    }
  },
  { _id: false }
);

const activeTimerSchema = new Schema(
  {
    timerId: {
      type: String,
      required: true,
      trim: true,
      maxlength: [80, "Timer id cannot exceed 80 characters"]
    },
    remainingSeconds: integerValue(0, 86400),
    isPaused: {
      type: Boolean,
      required: true,
      default: false
    }
  },
  { _id: false }
);

const objectiveStateSchema = new Schema(
  {
    currentQuest: {
      type: String,
      required: true,
      default: "build_signal_generator",
      trim: true,
      maxlength: [80, "Current quest cannot exceed 80 characters"]
    },
    signalGeneratorProgress: {
      type: Number,
      required: true,
      default: 0,
      min: [0, "Signal generator progress cannot be below 0"],
      max: [100, "Signal generator progress cannot exceed 100"]
    },
    completedObjectives: {
      type: [
        {
          type: String,
          trim: true,
          maxlength: [80, "Objective id cannot exceed 80 characters"]
        }
      ],
      default: []
    },
    activeTimers: {
      type: [activeTimerSchema],
      default: []
    }
  },
  { _id: false }
);

const saveProfileSchema = new Schema(
  {
    userId: {
      type: Schema.Types.ObjectId,
      ref: "User",
      required: true,
      unique: true,
      index: true
    },
    worldSeed: {
      type: Number,
      required: true,
      default: 0,
      min: [-2147483648, "World seed is below the allowed integer range"],
      max: [2147483647, "World seed is above the allowed integer range"],
      validate: {
        validator: Number.isInteger,
        message: "World seed must be an integer"
      }
    },
    playerTransform: {
      type: playerTransformSchema,
      default: () => ({})
    },
    survivalStats: {
      type: survivalStatsSchema,
      default: () => ({})
    },
    inventory: {
      type: [inventoryItemSchema],
      default: []
    },
    baseModules: {
      type: [baseModuleSchema],
      default: []
    },
    objectiveState: {
      type: objectiveStateSchema,
      default: () => ({})
    }
  },
  {
    timestamps: true,
    versionKey: false
  }
);

saveProfileSchema.set("toJSON", {
  transform: (document, returnedObject) => {
    returnedObject.id = returnedObject._id.toString();
    returnedObject.userId = returnedObject.userId.toString();
    delete returnedObject._id;
    return returnedObject;
  }
});

export default mongoose.model("SaveProfile", saveProfileSchema);
