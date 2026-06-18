import mongoose from "mongoose";

const connectDB = async () => {
  const mongoUri = process.env.MONGODB_URI || process.env.MONGO_URI || "mongodb://127.0.0.1:27017/dont_die_please";

  mongoose.set("strictQuery", true);

  try {
    const connection = await mongoose.connect(mongoUri, {
      autoIndex: process.env.NODE_ENV !== "production",
      serverSelectionTimeoutMS: Number(process.env.MONGO_TIMEOUT_MS) || 5000
    });

    console.log(`MongoDB connected at ${connection.connection.host}`);
  } catch (error) {
    console.error(`MongoDB connection failed: ${error.message}`);
    process.exit(1);
  }
};

export default connectDB;
