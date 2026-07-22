"""Language-model clients behind a common protocol.

AnthropicLlm calls the Claude API via the official SDK; StubLlm gives
deterministic offline answers so local dev and tests need no API key.
"""

from typing import Protocol

SYSTEM_PROMPT = """\
You are the product support assistant for NexusCommerce, an online store.

Answer customer questions using ONLY the product catalog provided below.
Rules:
- Recommend products from the catalog when relevant; always mention price.
- Mention stock only when it matters (low or out of stock, or the customer asks).
- If the catalog has nothing relevant, say so honestly and suggest the closest category.
- Politely decline questions unrelated to the store or its products.
- Keep answers short and conversational - this is a chat widget, not an essay.

{catalog_context}
"""


class LlmClient(Protocol):
    async def answer(self, question: str, catalog_context: str, history: list[dict]) -> str: ...


class AnthropicLlm:
    def __init__(self, model: str, max_tokens: int) -> None:
        # Trust the OS certificate store so outbound TLS works behind a
        # corporate/AV proxy whose root CA isn't in certifi's bundle.
        try:
            import truststore

            truststore.inject_into_ssl()
        except ImportError:
            pass

        from anthropic import AsyncAnthropic

        self._client = AsyncAnthropic()
        self._model = model
        self._max_tokens = max_tokens

    async def answer(self, question: str, catalog_context: str, history: list[dict]) -> str:
        messages = [
            {"role": m["role"], "content": m["content"]}
            for m in history
            if m.get("role") in ("user", "assistant") and m.get("content")
        ]
        messages.append({"role": "user", "content": question})

        response = await self._client.messages.create(
            model=self._model,
            max_tokens=self._max_tokens,
            system=SYSTEM_PROMPT.format(catalog_context=catalog_context),
            messages=messages,
        )

        if response.stop_reason == "refusal":
            return "I'm sorry, I can't help with that. Is there anything about our products I can help you find?"

        return "".join(block.text for block in response.content if block.type == "text").strip()


class StubLlm:
    """Offline stand-in: finds catalog lines matching words from the question."""

    async def answer(self, question: str, catalog_context: str, history: list[dict]) -> str:
        words = {w.strip("?,.!").lower() for w in question.split() if len(w) > 3}
        product_lines = [l for l in catalog_context.splitlines() if l.startswith("- ")]
        matches = [l for l in product_lines if any(w in l.lower() for w in words)]

        if matches:
            listing = "\n".join(matches[:3])
            return f"[stub] Based on our catalog, you might like:\n{listing}"
        return (
            "[stub] I couldn't find anything matching that in our catalog. "
            "We carry: " + ", ".join(l[2:].split(" (")[0] for l in product_lines[:6])
        )


def create_llm(kind: str, model: str, max_tokens: int) -> LlmClient:
    if kind == "anthropic":
        return AnthropicLlm(model, max_tokens)
    if kind == "stub":
        return StubLlm()
    raise ValueError(f"Unknown llm '{kind}'")
