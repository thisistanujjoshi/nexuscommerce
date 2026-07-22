from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    """Service configuration, overridable via AISUPPORT_* environment variables.

    llm: which language-model client answers questions.
        "anthropic" - the Claude API (reads ANTHROPIC_API_KEY from the environment)
        "stub"      - deterministic offline client (local dev / tests, no key needed)
    """

    llm: str = "anthropic"
    model: str = "claude-opus-4-8"
    max_tokens: int = 1024

    catalog_url: str = "http://localhost:5101"

    model_config = {"env_prefix": "AISUPPORT_"}


settings = Settings()
