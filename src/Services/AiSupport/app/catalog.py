"""Read-side client for the Catalog service.

The demo catalog is small, so grounding fetches the full product list and
lets the model reason over it directly. At real catalog scale this would
become a retrieval step (search endpoint or embeddings) — see ADR 0004.
"""

import httpx


class CatalogClient:
    def __init__(self, base_url: str, client: httpx.AsyncClient | None = None) -> None:
        self._client = client or httpx.AsyncClient(base_url=base_url, timeout=5.0)

    async def get_context(self) -> str:
        """Render categories and products as plain text for the model prompt."""
        categories = (await self._client.get("/api/v1/categories")).raise_for_status().json()
        products = (
            (await self._client.get("/api/v1/products", params={"pageSize": 100}))
            .raise_for_status()
            .json()["items"]
        )

        category_names = {c["id"]: c["name"] for c in categories}

        lines = ["Product catalog:"]
        for p in products:
            stock = f"{p['stockQuantity']} in stock" if p["stockQuantity"] > 0 else "OUT OF STOCK"
            lines.append(
                f"- {p['name']} (SKU {p['sku']}, category: {category_names.get(p['categoryId'], 'Unknown')}): "
                f"${p['price']:.2f}, {stock}. {p['description']}"
            )

        lines.append("\nCategories: " + ", ".join(sorted(category_names.values())))
        return "\n".join(lines)

    async def aclose(self) -> None:
        await self._client.aclose()
