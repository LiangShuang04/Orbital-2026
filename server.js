import dotenv from "dotenv";

dotenv.config({ quiet: true });

import cors from "cors";
import express from "express";
import swaggerUi from "swagger-ui-express";
import connectDB from "#config/db";
import createSwaggerSpec from "#config/swagger";
import errorHandler from "#middleware/errorHandler";
import authRoutes from "#routes/authRoutes";
import saveRoutes from "#routes/saveRoutes";

const app = express();
const port = Number(process.env.PORT) || 5000;
const configuredOrigins = process.env.CORS_ORIGIN?.split(",")
  .map((origin) => origin.trim())
  .filter(Boolean);
const swaggerSpec = createSwaggerSpec();

app.use(
  cors({
    origin: configuredOrigins?.length ? configuredOrigins : true,
    credentials: true,
  }),
);
app.use(express.json({ limit: "1mb" }));

app.use(
  "/api-docs",
  swaggerUi.serve,
  swaggerUi.setup(swaggerSpec, {
    explorer: true,
    customSiteTitle: "Don't Die Please API Docs",
  }),
);

app.get("/api/v1/health", (req, res) => {
  res.status(200).json({
    success: true,
    service: "dont-die-please-api",
  });
});

app.use("/api/v1/auth", authRoutes);
app.use("/api/v1/save", saveRoutes);

app.use((req, res, next) => {
  const error = new Error(`Route ${req.originalUrl} not found`);
  error.statusCode = 404;
  next(error);
});

app.use(errorHandler);

await connectDB();

const server = app.listen(port, () => {
  console.log(`Don't Die Please API listening on port ${port}`);
});

process.on("unhandledRejection", (error) => {
  console.error(`Unhandled rejection: ${error.message}`);
  server.close(() => process.exit(1));
});

process.on("SIGTERM", () => {
  server.close(() => process.exit(0));
});

export default app;
