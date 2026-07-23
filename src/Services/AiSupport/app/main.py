import logging

import httpx
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from .catalog import CatalogClient
from .config import Settings, settings
from .llm import LlmClient, create_llm

logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s: %(message)s")
logger = logging.getLogger("aisupport")


class ChatTurn(BaseModel):
    role: str
    content: str


class ChatRequest(BaseModel):
    message: str = Field(min_length=1, max_length=2000)
    history: list[ChatTurn] = Field(default_factory=list, max_length=20)


class ChatResponse(BaseModel):
    reply: str


def create_app(
    config: Settings | None = None,
    llm: LlmClient | None = None,
    catalog: CatalogClient | None = None,
) -> FastAPI:
    """App factory; tests inject their own llm/catalog clients."""
    config = config or settings
    llm = llm or create_llm(config.llm, config.model, config.max_tokens)
    catalog = catalog or CatalogClient(config.catalog_url)

    app = FastAPI(
        title="NexusCommerce AI Support API",
        version="1.0",
        description=(
            "Product Q&A chatbot grounded in live catalog data — "
            "part of the NexusCommerce distributed order-management platform."
        ),
    )

    # Prometheus metrics at /metrics (scraped by the OpsForge observability stack).
    from prometheus_fastapi_instrumentator import Instrumentator

    Instrumentator(excluded_handlers=["/metrics", "/health"]).instrument(app).expose(app)

    @app.get("/health")
    async def health() -> dict:
        return {"status": "Healthy", "llm": config.llm, "model": config.model}

    @app.post("/api/v1/chat", response_model=ChatResponse)
    async def chat(request: ChatRequest) -> ChatResponse:
        try:
            catalog_context = await catalog.get_context()
        except httpx.HTTPError as exc:
            logger.error("Catalog unavailable: %s", exc)
            raise HTTPException(
                status_code=503, detail="The product catalog is currently unavailable."
            )

        try:
            reply = await llm.answer(
                request.message,
                catalog_context,
                [t.model_dump() for t in request.history],
            )
        except Exception:
            logger.exception("LLM call failed")
            raise HTTPException(
                status_code=502, detail="The assistant is currently unavailable."
            )

        return ChatResponse(reply=reply)

    return app


app = create_app()
