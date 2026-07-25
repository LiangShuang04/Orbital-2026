import mongoose from "mongoose";

const connectDB = async () => {
  const uri = process.env.MONGODB_URI || process.env.MONGO_URI || "mongodb://127.0.0.1:27017/dont_die_please";

  mongoose.set("strictQuery", true);

  try {
    const db = await mongoose.connect(uri, {
      autoIndex: process.env.NODE_ENV !== "production",
      serverSelectionTimeoutMS: Number(process.env.MONGO_TIMEOUT_MS) || 5000
    });

    console.log(`MongoDB connected at ${db.connection.host}`);
  } catch (err) {
    console.error(`MongoDB connection failed: ${err.message}`);
    process.exit(1);
  }
};

export default connectDB;
