const duplicateFieldMessage = error => {
  const [field] = Object.keys(error.keyValue || {});
  return field ? `${field} already exists` : "Duplicate resource";
};

const validationMessage = error => Object.values(error.errors).map(fieldError => fieldError.message).join(", ");

const errorHandler = (error, req, res, next) => {
  let statusCode = error.statusCode || error.status || 500;
  let message = error.message || "Internal server error";

  if (error.code === 11000) {
    statusCode = 409;
    message = duplicateFieldMessage(error);
  }

  if (error.name === "ValidationError") {
    statusCode = 400;
    message = validationMessage(error);
  }

  if (error.name === "CastError") {
    statusCode = 400;
    message = `Invalid ${error.path}`;
  }

  if (error.name === "JsonWebTokenError" || error.name === "TokenExpiredError") {
    statusCode = 401;
    message = "Invalid or expired token";
  }

  if (statusCode >= 500 && process.env.NODE_ENV === "production") {
    message = "Internal server error";
  }

  res.status(statusCode).json({
    success: false,
    error: message
  });
};

export default errorHandler;
