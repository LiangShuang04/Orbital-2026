import jwt from "jsonwebtoken";
import User from "#models/User";

const jwtSecret = () => {
  const secret = process.env.JWT_SECRET || (process.env.NODE_ENV === "production" ? "" : "dont-die-please-local-development-secret");

  if (!secret) {
    const error = new Error("JWT_SECRET must be configured");
    error.statusCode = 500;
    throw error;
  }

  return secret;
};

const bearerToken = authorizationHeader => {
  const [scheme, token] = authorizationHeader?.split(" ") || [];
  return scheme === "Bearer" && token ? token : null;
};

const auth = async (req, res, next) => {
  try {
    const token = bearerToken(req.headers.authorization);

    if (!token) {
      const error = new Error("Authorization token is required");
      error.statusCode = 401;
      throw error;
    }

    const decoded = jwt.verify(token, jwtSecret(), {
      audience: "dont-die-please-unity",
      issuer: "dont-die-please-api"
    });

    const user = await User.findById(decoded.sub).select("_id username email");

    if (!user) {
      const error = new Error("Authenticated user no longer exists");
      error.statusCode = 401;
      throw error;
    }

    req.user = {
      id: user._id.toString(),
      username: user.username,
      email: user.email
    };

    next();
  } catch (error) {
    next(error);
  }
};

export default auth;
