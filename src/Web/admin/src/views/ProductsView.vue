<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  adjustStock,
  changePrice,
  createProduct,
  deleteProduct,
  getCategories,
  searchProducts,
} from '../api/catalog'
import type { Category, Product } from '../api/types'

const products = ref<Product[]>([])
const categories = ref<Category[]>([])
const search = ref('')
const error = ref<string | null>(null)
const showCreate = ref(false)

const draft = ref({
  sku: '',
  name: '',
  description: '',
  price: 0,
  stockQuantity: 0,
  categoryId: '',
})

const categoryName = computed(
  () => (id: string) => categories.value.find((c) => c.id === id)?.name ?? '—',
)

async function refresh() {
  try {
    const result = await searchProducts({ search: search.value || undefined })
    products.value = result.items
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load products'
  }
}

onMounted(async () => {
  await refresh()
  try {
    categories.value = await getCategories()
    if (categories.value.length > 0) draft.value.categoryId = categories.value[0].id
  } catch {
    /* categories are optional for rendering the table */
  }
})

async function run(action: () => Promise<unknown>) {
  try {
    await action()
    error.value = null
    await refresh()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Action failed'
  }
}

function onAdjustStock(product: Product, delta: number) {
  run(() => adjustStock(product.id, delta))
}

function onChangePrice(product: Product) {
  const input = prompt(`New price for ${product.name}:`, product.price.toFixed(2))
  if (input === null) return
  const price = Number(input)
  if (Number.isNaN(price)) {
    error.value = 'Price must be a number.'
    return
  }
  run(() => changePrice(product.id, price))
}

function onDelete(product: Product) {
  if (!confirm(`Delete ${product.name} (${product.sku})?`)) return
  run(() => deleteProduct(product.id))
}

function onCreate() {
  run(async () => {
    await createProduct({ ...draft.value })
    showCreate.value = false
    draft.value = { ...draft.value, sku: '', name: '', description: '', price: 0, stockQuantity: 0 }
  })
}
</script>

<template>
  <section>
    <div class="toolbar">
      <input
        v-model="search"
        type="search"
        placeholder="Search products…"
        aria-label="Search products"
        @input="refresh"
      />
      <button @click="showCreate = !showCreate">
        {{ showCreate ? 'Close' : '+ New product' }}
      </button>
    </div>

    <p v-if="error" class="error">{{ error }}</p>

    <form v-if="showCreate" class="create-form" @submit.prevent="onCreate">
      <input v-model="draft.sku" placeholder="SKU" required aria-label="SKU" />
      <input v-model="draft.name" placeholder="Name" required aria-label="Name" />
      <input v-model="draft.description" placeholder="Description" aria-label="Description" />
      <input
        v-model.number="draft.price"
        type="number"
        step="0.01"
        min="0"
        placeholder="Price"
        required
        aria-label="Price"
      />
      <input
        v-model.number="draft.stockQuantity"
        type="number"
        min="0"
        placeholder="Stock"
        required
        aria-label="Stock"
      />
      <select v-model="draft.categoryId" required aria-label="Category">
        <option v-for="c in categories" :key="c.id" :value="c.id">{{ c.name }}</option>
      </select>
      <button type="submit">Create</button>
    </form>

    <table>
      <thead>
        <tr>
          <th>SKU</th>
          <th>Name</th>
          <th>Category</th>
          <th class="num">Price</th>
          <th class="num">Stock</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="product in products" :key="product.id">
          <td class="mono">{{ product.sku }}</td>
          <td>{{ product.name }}</td>
          <td>{{ categoryName(product.categoryId) }}</td>
          <td class="num">${{ product.price.toFixed(2) }}</td>
          <td class="num" :class="{ low: product.stockQuantity < 20 }">
            {{ product.stockQuantity }}
          </td>
          <td class="actions">
            <button @click="onAdjustStock(product, 10)">+10</button>
            <button :disabled="product.stockQuantity < 1" @click="onAdjustStock(product, -1)">
              −1
            </button>
            <button @click="onChangePrice(product)">Price</button>
            <button class="danger" @click="onDelete(product)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-if="products.length === 0" class="muted">No products found.</p>
  </section>
</template>
