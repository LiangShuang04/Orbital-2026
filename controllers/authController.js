import jwt from "jsonwebtoken";
import User from "#models/User";

const handleAsync = action => (req, res, next) => Promise.resolve(action(req, res, next)).catch(next);

const httpError = (statusCode, message) => {
  const error = new Error(message);
  error.statusCode = statusCode;
  return error;
};

const jwtSecret = () => {
  const secret = process.env.JWT_SECRET || (process.env.NODE_ENV === "production" ? "" : "dont-die-please-local-development-secret");

  if (!secret) {
    throw httpError(500, "JWT_SECRET must be configured");
  }

  return secret;
};

const createToken = user =>
  jwt.sign(
    {
      sub: user._id.toString(),
      username: user.username
    },
    jwtSecret(),
    {
      expiresIn: process.env.JWT_EXPIRES_IN || "7d",
      audience: "dont-die-please-unity",
      issuer: "dont-die-please-api"
    }
  );

const publicUser = user => ({
  id: user._id.toString(),
  username: user.username,
  email: user.email
});

const normalizedCredentials = (body = {}) => ({
  username: body.username?.trim(),
  email: body.email?.trim().toLowerCase(),
  password: body.password
});

export const register = handleAsync(async (req, res) => {
  const { username, email, password } = normalizedCredentials(req.body);

  if (!username || !email || !password) {
    throw httpError(400, "Username, email, and password are required");
  }

  const existingUser = await User.findOne({
    $or: [{ username }, { email }]
  });

  if (existingUser) {
    throw httpError(409, "Username or email already exists");
  }

  const user = await User.create({ username, email, password });

  res.status(201).json({
    success: true,
    token: createToken(user),
    user: publicUser(user)
  });
});

export const login = handleAsync(async (req, res) => {
  const { email, password } = normalizedCredentials(req.body);

  if (!email || !password) {
    throw httpError(400, "Email and password are required");
  }

  const user = await User.findOne({ email }).select("+password");
  const passwordMatches = user ? await user.comparePassword(password) : false;

  if (!passwordMatches) {
    throw httpError(401, "Invalid email or password");
  }

  res.status(200).json({
    success: true,
    token: createToken(user),
    user: publicUser(user)
  });
});
