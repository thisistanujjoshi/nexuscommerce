<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { createCategory, deleteCategory, getCategories, updateCategory } from '../api/catalog'
import type { Category } from '../api/types'

const categories = ref<Category[]>([])
const error = ref<string | null>(null)
const name = ref('')
const description = ref('')

async function refresh() {
  try {
    categories.value = await getCategories()
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load categories'
  }
}

onMounted(refresh)

async function run(action: () => Promise<unknown>) {
  try {
    await action()
    error.value = null
    await refresh()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Action failed'
  }
}

function onCreate() {
  run(async () => {
    await createCategory(name.value, description.value)
    name.value = ''
    description.value = ''
  })
}

function onRename(category: Category) {
  const newName = prompt('Category name:', category.name)
  if (newName === null) return
  run(() => updateCategory(category.id, newName, category.description))
}

function onDelete(category: Category) {
  if (!confirm(`Delete category "${category.name}"?`)) return
  run(() => deleteCategory(category.id))
}
</script>

<template>
  <section>
    <p v-if="error" class="error">{{ error }}</p>

    <form class="create-form" @submit.prevent="onCreate">
      <input v-model="name" placeholder="New category name" required aria-label="Category name" />
      <input v-model="description" placeholder="Description" aria-label="Category description" />
      <button type="submit">Add category</button>
    </form>

    <table>
      <thead>
        <tr>
          <th>Name</th>
          <th>Description</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="category in categories" :key="category.id">
          <td>{{ category.name }}</td>
          <td class="muted">{{ category.description }}</td>
          <td class="actions">
            <button @click="onRename(category)">Rename</button>
            <button class="danger" @click="onDelete(category)">Delete</button>
          </td>
        </tr>
      </tbody>
    </table>
  </section>
</template>
