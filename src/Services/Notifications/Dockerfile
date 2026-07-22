# Notifications service (Python / FastAPI).
# Build context is this service directory.
FROM python:3.12-slim AS base

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1

WORKDIR /app

COPY pyproject.toml ./
COPY app ./app
RUN pip install --no-cache-dir .

# Run as a non-root user.
RUN useradd --create-home --uid 10001 appuser
USER appuser

EXPOSE 8080
CMD ["python", "-m", "uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8080"]
