import swaggerJSDoc from "swagger-jsdoc";

const createSwaggerDefinition = () => {
  const port = Number(process.env.PORT) || 5000;
  const publicApiUrl = process.env.PUBLIC_API_URL || `http://127.0.0.1:${port}`;

  return {
    openapi: "3.0.3",
    info: {
      title: "Don't Die Please API",
      version: "1.0.0",
      description:
        "Backend API for authentication and persistent survival game state for Don't Die Please.",
    },
    servers: [
      {
        url: publicApiUrl,
        description:
          process.env.NODE_ENV === "production"
            ? "Configured deployment"
            : "Local development",
      },
    ],
    tags: [
      {
        name: "Health",
        description: "Service health checks.",
      },
      {
        name: "Authentication",
        description:
          "Account registration, login, and JWT issuance for Unity clients.",
      },
      {
        name: "Save Profiles",
        description:
          "Authenticated save, load, and update operations for player game state.",
      },
    ],
    components: {
      securitySchemes: {
        bearerAuth: {
          type: "http",
          scheme: "bearer",
          bearerFormat: "JWT",
        },
      },
      schemas: {
        ErrorResponse: {
          type: "object",
          required: ["success", "error"],
          properties: {
            success: {
              type: "boolean",
              example: false,
            },
            error: {
              type: "string",
              example: "Invalid email or password",
            },
          },
        },
        User: {
          type: "object",
          required: ["username", "email", "password"],
          properties: {
            username: {
              type: "string",
              minLength: 3,
              maxLength: 32,
              pattern: "^[a-zA-Z0-9_]+$",
              example: "orbital_test_player",
            },
            email: {
              type: "string",
              format: "email",
              example: "player@example.com",
            },
            password: {
              type: "string",
              format: "password",
              minLength: 8,
              example: "StrongPassword123",
            },
          },
        },
        LoginRequest: {
          type: "object",
          required: ["email", "password"],
          properties: {
            email: {
              type: "string",
              format: "email",
              example: "player@example.com",
            },
            password: {
              type: "string",
              format: "password",
              example: "StrongPassword123",
            },
          },
        },
        AuthenticatedUser: {
          type: "object",
          required: ["id", "username", "email"],
          properties: {
            id: {
              type: "string",
              example: "665f7d30fb3f7b1df83d2e91",
            },
            username: {
              type: "string",
              example: "orbital_test_player",
            },
            email: {
              type: "string",
              format: "email",
              example: "player@example.com",
            },
          },
        },
        AuthResponse: {
          type: "object",
          required: ["success", "token", "user"],
          properties: {
            success: {
              type: "boolean",
              example: true,
            },
            token: {
              type: "string",
              example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
            },
            user: {
              $ref: "#/components/schemas/AuthenticatedUser",
            },
          },
        },
        Vector3: {
          type: "object",
          required: ["x", "y", "z"],
          properties: {
            x: {
              type: "number",
              minimum: -100000,
              maximum: 100000,
              example: 12.5,
            },
            y: {
              type: "number",
              minimum: -100000,
              maximum: 100000,
              example: 0,
            },
            z: {
              type: "number",
              minimum: -100000,
              maximum: 100000,
              example: -7.25,
            },
          },
        },
        PlayerTransform: {
          type: "object",
          properties: {
            position: {
              $ref: "#/components/schemas/Vector3",
            },
            rotation: {
              $ref: "#/components/schemas/Vector3",
            },
          },
        },
        SurvivalStats: {
          type: "object",
          properties: {
            health: {
              type: "number",
              minimum: 0,
              maximum: 100,
              example: 84,
            },
            oxygen: {
              type: "number",
              minimum: 0,
              maximum: 100,
              example: 63,
            },
            hunger: {
              type: "number",
              minimum: 0,
              maximum: 100,
              example: 42,
            },
            toxicity: {
              type: "number",
              minimum: 0,
              maximum: 100,
              example: 11,
            },
          },
        },
        InventoryItem: {
          type: "object",
          required: ["itemId", "quantity"],
          properties: {
            itemId: {
              type: "string",
              enum: ["metal_scrap", "filter_fibre"],
              example: "metal_scrap",
            },
            quantity: {
              type: "integer",
              minimum: 0,
              maximum: 9999,
              example: 18,
            },
          },
        },
        BaseModule: {
          type: "object",
          required: ["moduleId", "isActive", "position"],
          properties: {
            moduleId: {
              type: "string",
              enum: [
                "oxygen_station",
                "storage_unit",
                "power_generator",
                "signal_generator",
              ],
              example: "oxygen_station",
            },
            isActive: {
              type: "boolean",
              example: true,
            },
            position: {
              $ref: "#/components/schemas/Vector3",
            },
          },
        },
        ObjectiveTimer: {
          type: "object",
          required: ["timerId", "remainingSeconds", "isPaused"],
          properties: {
            timerId: {
              type: "string",
              example: "signal_generator_charge",
            },
            remainingSeconds: {
              type: "integer",
              minimum: 0,
              maximum: 86400,
              example: 300,
            },
            isPaused: {
              type: "boolean",
              example: false,
            },
          },
        },
        ObjectiveState: {
          type: "object",
          properties: {
            currentQuest: {
              type: "string",
              example: "build_signal_generator",
            },
            signalGeneratorProgress: {
              type: "number",
              minimum: 0,
              maximum: 100,
              example: 35,
            },
            completedObjectives: {
              type: "array",
              items: {
                type: "string",
              },
              example: ["collect_filter_fibre"],
            },
            activeTimers: {
              type: "array",
              items: {
                $ref: "#/components/schemas/ObjectiveTimer",
              },
            },
          },
        },
        SaveProfile: {
          type: "object",
          required: [
            "id",
            "userId",
            "worldSeed",
            "playerTransform",
            "survivalStats",
            "inventory",
            "baseModules",
            "objectiveState",
            "createdAt",
            "updatedAt",
          ],
          properties: {
            id: {
              type: "string",
              example: "665f7d30fb3f7b1df83d2e93",
            },
            userId: {
              type: "string",
              example: "665f7d30fb3f7b1df83d2e91",
            },
            worldSeed: {
              type: "integer",
              minimum: -2147483648,
              maximum: 2147483647,
              example: 2026,
            },
            playerTransform: {
              $ref: "#/components/schemas/PlayerTransform",
            },
            survivalStats: {
              $ref: "#/components/schemas/SurvivalStats",
            },
            inventory: {
              type: "array",
              items: {
                $ref: "#/components/schemas/InventoryItem",
              },
            },
            baseModules: {
              type: "array",
              items: {
                $ref: "#/components/schemas/BaseModule",
              },
            },
            objectiveState: {
              $ref: "#/components/schemas/ObjectiveState",
            },
            createdAt: {
              type: "string",
              format: "date-time",
              example: "2026-06-11T06:30:00.000Z",
            },
            updatedAt: {
              type: "string",
              format: "date-time",
              example: "2026-06-11T07:15:00.000Z",
            },
          },
        },
        SaveProfileResponse: {
          type: "object",
          required: ["success", "saveProfile"],
          properties: {
            success: {
              type: "boolean",
              example: true,
            },
            saveProfile: {
              $ref: "#/components/schemas/SaveProfile",
            },
          },
        },
        SaveUpdateRequest: {
          type: "object",
          properties: {
            worldSeed: {
              type: "integer",
              minimum: -2147483648,
              maximum: 2147483647,
              example: 2026,
            },
            playerTransform: {
              $ref: "#/components/schemas/PlayerTransform",
            },
            survivalStats: {
              $ref: "#/components/schemas/SurvivalStats",
            },
            inventory: {
              type: "array",
              items: {
                $ref: "#/components/schemas/InventoryItem",
              },
            },
            baseModules: {
              type: "array",
              items: {
                $ref: "#/components/schemas/BaseModule",
              },
            },
            objectiveState: {
              $ref: "#/components/schemas/ObjectiveState",
            },
          },
        },
        HealthResponse: {
          type: "object",
          required: ["success", "service"],
          properties: {
            success: {
              type: "boolean",
              example: true,
            },
            service: {
              type: "string",
              example: "dont-die-please-api",
            },
          },
        },
      },
    },
    paths: {
      "/api/v1/health": {
        get: {
          tags: ["Health"],
          summary: "Check API health",
          description:
            "Returns a lightweight confirmation that the API process is running.",
          responses: {
            200: {
              description: "The API is healthy.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/HealthResponse",
                  },
                },
              },
            },
          },
        },
      },
      "/api/v1/auth/register": {
        post: {
          tags: ["Authentication"],
          summary: "Register a new user",
          description:
            "Creates a player account and returns a JWT for immediate Unity client authentication.",
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  $ref: "#/components/schemas/User",
                },
              },
            },
          },
          responses: {
            201: {
              description: "The account was created and authenticated.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/AuthResponse",
                  },
                },
              },
            },
            400: {
              description:
                "Required registration fields are missing or invalid.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
            409: {
              description: "The username or email already exists.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
          },
        },
      },
      "/api/v1/auth/login": {
        post: {
          tags: ["Authentication"],
          summary: "Log in an existing user",
          description:
            "Validates account credentials and returns a JWT for protected save profile operations.",
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  $ref: "#/components/schemas/LoginRequest",
                },
              },
            },
          },
          responses: {
            200: {
              description: "The account was authenticated successfully.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/AuthResponse",
                  },
                },
              },
            },
            400: {
              description: "The email or password field is missing.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
            401: {
              description: "The supplied credentials are invalid.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
          },
        },
      },
      "/api/v1/save": {
        post: {
          tags: ["Save Profiles"],
          summary: "Create a blank save profile",
          description:
            "Initializes the authenticated user's save profile with default transform, survival, inventory, base, and objective state.",
          security: [
            {
              bearerAuth: [],
            },
          ],
          requestBody: {
            required: false,
            content: {
              "application/json": {
                schema: {
                  type: "object",
                  additionalProperties: false,
                  properties: {
                    worldSeed: {
                      type: "integer",
                      minimum: -2147483648,
                      maximum: 2147483647,
                      example: 2026,
                    },
                  },
                },
                example: {
                  worldSeed: 2026,
                },
              },
            },
          },
          responses: {
            201: {
              description: "A save profile was created.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/SaveProfileResponse",
                  },
                },
              },
            },
            401: {
              description: "A valid JWT is required.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
            409: {
              description: "The authenticated user already has a save profile.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
          },
        },
        get: {
          tags: ["Save Profiles"],
          summary: "Retrieve the authenticated user's save profile",
          description:
            "Loads the current persistent game state for the authenticated Unity client.",
          security: [
            {
              bearerAuth: [],
            },
          ],
          responses: {
            200: {
              description: "The save profile was found.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/SaveProfileResponse",
                  },
                },
              },
            },
            401: {
              description: "A valid JWT is required.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
            404: {
              description: "No save profile exists for the authenticated user.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
          },
        },
        put: {
          tags: ["Save Profiles"],
          summary: "Update the authenticated user's save profile",
          description:
            "Persists validated survival stats, inventory, base modules, objective progress, and player transform data.",
          security: [
            {
              bearerAuth: [],
            },
          ],
          requestBody: {
            required: true,
            content: {
              "application/json": {
                schema: {
                  $ref: "#/components/schemas/SaveUpdateRequest",
                },
                example: {
                  worldSeed: 2026,
                  playerTransform: {
                    position: {
                      x: 12.5,
                      y: 0,
                      z: -7.25,
                    },
                    rotation: {
                      x: 0,
                      y: 90,
                      z: 0,
                    },
                  },
                  survivalStats: {
                    health: 84,
                    oxygen: 63,
                    hunger: 42,
                    toxicity: 11,
                  },
                  inventory: [
                    {
                      itemId: "metal_scrap",
                      quantity: 18,
                    },
                    {
                      itemId: "filter_fibre",
                      quantity: 6,
                    },
                  ],
                  baseModules: [
                    {
                      moduleId: "oxygen_station",
                      isActive: true,
                      position: {
                        x: 4,
                        y: 0,
                        z: 9,
                      },
                    },
                  ],
                  objectiveState: {
                    currentQuest: "build_signal_generator",
                    signalGeneratorProgress: 35,
                    completedObjectives: ["collect_filter_fibre"],
                    activeTimers: [],
                  },
                },
              },
            },
          },
          responses: {
            200: {
              description: "The save profile was updated.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/SaveProfileResponse",
                  },
                },
              },
            },
            400: {
              description: "The save payload failed strict validation.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
            401: {
              description: "A valid JWT is required.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
            404: {
              description: "No save profile exists for the authenticated user.",
              content: {
                "application/json": {
                  schema: {
                    $ref: "#/components/schemas/ErrorResponse",
                  },
                },
              },
            },
          },
        },
      },
    },
  };
};

const createSwaggerSpec = () =>
  swaggerJSDoc({
    definition: createSwaggerDefinition(),
    apis: [],
  });

export default createSwaggerSpec;
