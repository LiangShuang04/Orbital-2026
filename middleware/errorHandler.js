const duplicateFieldMessage = error => {
  const [field] = Object.keys(error.keyValue || {});
  return field ? `${field} already exists` : "Duplicate resource";
};

const validationMessage = error => Object.values(error.errors).map(err => err.message).join(", ");

const errorHandler = (error, req, res, next) => {
  let status = error.statusCode || error.status || 500;
  let msg = error.message || "Internal server error";

  if (error.code === 11000) {
    status = 409;
    msg = duplicateFieldMessage(error);
  }

  if (error.name === "ValidationError") {
    status = 400;
    msg = validationMessage(error);
  }

  if (error.name === "CastError") {
    status = 400;
    msg = `Invalid ${error.path}`;
  }

  if (error.name === "JsonWebTokenError" || error.name === "TokenExpiredError") {
    status = 401;
    msg = "Invalid or expired token";
  }

  if (status >= 500 && process.env.NODE_ENV === "production") {
    msg = "Internal server error";
  }

  res.status(status).json({
    success: false,
    error: msg
  });
};

export default errorHandler;
