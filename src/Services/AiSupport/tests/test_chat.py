import httpx

from app.catalog import CatalogClient
from app.config import Settings
from app.llm import StubLlm
from app.main import create_app

CATEGORIES = [
    {"id": "c1", "name": "Electronics", "description": ""},
    {"id": "c2", "name": "Books", "description": ""},
]
PRODUCTS = {
    "items": [
        {
            "id": "p1", "sku": "ELEC-KB-001", "name": "Mechanical Keyboard",
            "description": "87-key hot-swappable mechanical keyboard",
            "price": 89.99, "stockQuantity": 42, "categoryId": "c1",
        },
        {
            "id": "p2", "sku": "BOOK-CS-001", "name": "Clean Architecture",
            "description": "Robert C. Martin on software structure",
            "price": 31.99, "stockQuantity": 0, "categoryId": "c2",
        },
    ]
}


def fake_catalog(fail: bool = False) -> CatalogClient:
    def handler(request: httpx.Request) -> httpx.Response:
        if fail:
            return httpx.Response(500)
        if request.url.path == "/api/v1/categories":
            return httpx.Response(200, json=CATEGORIES)
        if request.url.path == "/api/v1/products":
            return httpx.Response(200, json=PRODUCTS)
        return httpx.Response(404)

    transport = httpx.MockTransport(handler)
    return CatalogClient(
        "http://catalog", httpx.AsyncClient(transport=transport, base_url="http://catalog")
    )


def make_client(fail_catalog: bool = False) -> httpx.AsyncClient:
    app = create_app(
        config=Settings(llm="stub"),
        llm=StubLlm(),
        catalog=fake_catalog(fail=fail_catalog),
    )
    return httpx.AsyncClient(transport=httpx.ASGITransport(app=app), base_url="http://test")


async def test_health():
    async with make_client() as client:
        response = await client.get("/health")

    assert response.status_code == 200
    assert response.json()["status"] == "Healthy"


async def test_catalog_context_includes_prices_and_stock():
    catalog = fake_catalog()
    context = await catalog.get_context()

    assert "Mechanical Keyboard" in context
    assert "$89.99" in context
    assert "OUT OF STOCK" in context
    assert "Electronics" in context


async def test_chat_answers_grounded_in_catalog():
    async with make_client() as client:
        response = await client.post(
            "/api/v1/chat", json={"message": "Do you sell a keyboard?"}
        )

    assert response.status_code == 200
    assert "Mechanical Keyboard" in response.json()["reply"]


async def test_chat_handles_no_match():
    async with make_client() as client:
        response = await client.post(
            "/api/v1/chat", json={"message": "Do you sell lawnmowers?"}
        )

    assert response.status_code == 200
    assert "couldn't find" in response.json()["reply"]


async def test_chat_rejects_empty_message():
    async with make_client() as client:
        response = await client.post("/api/v1/chat", json={"message": ""})

    assert response.status_code == 422


async def test_chat_returns_503_when_catalog_down():
    async with make_client(fail_catalog=True) as client:
        response = await client.post(
            "/api/v1/chat", json={"message": "Do you sell keyboards?"}
        )

    assert response.status_code == 503
