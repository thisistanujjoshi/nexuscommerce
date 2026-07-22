<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getOrders, transitionOrder, type OrderTransition } from '../api/orders'
import type { Order, OrderStatus } from '../api/types'

const orders = ref<Order[]>([])
const statusFilter = ref<'' | OrderStatus>('')
const error = ref<string | null>(null)

/** Which lifecycle actions are valid from each status (mirrors the Orders domain). */
const NEXT_ACTIONS: Record<OrderStatus, OrderTransition[]> = {
  Pending: ['confirm', 'cancel'],
  Confirmed: ['ship', 'cancel'],
  Shipped: ['deliver'],
  Delivered: [],
  Cancelled: [],
}

async function refresh() {
  try {
    const result = await getOrders({ status: statusFilter.value || undefined })
    orders.value = result.items
    error.value = null
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Failed to load orders'
  }
}

onMounted(refresh)

async function onTransition(order: Order, transition: OrderTransition) {
  try {
    await transitionOrder(order.id, transition)
    error.value = null
    await refresh()
  } catch (e) {
    error.value = e instanceof Error ? e.message : 'Transition failed'
  }
}
</script>

<template>
  <section>
    <div class="toolbar">
      <select v-model="statusFilter" aria-label="Filter by status" @change="refresh">
        <option value="">All statuses</option>
        <option>Pending</option>
        <option>Confirmed</option>
        <option>Shipped</option>
        <option>Delivered</option>
        <option>Cancelled</option>
      </select>
      <button @click="refresh">Refresh</button>
    </div>

    <p v-if="error" class="error">{{ error }}</p>

    <table>
      <thead>
        <tr>
          <th>Order</th>
          <th>Customer</th>
          <th>Placed</th>
          <th>Items</th>
          <th class="num">Total</th>
          <th>Status</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="order in orders" :key="order.id">
          <td class="mono">#{{ order.id.slice(0, 8) }}</td>
          <td>{{ order.customerEmail }}</td>
          <td>{{ new Date(order.placedAtUtc).toLocaleString() }}</td>
          <td class="muted">
            {{ order.items.map((i) => `${i.quantity}× ${i.productName}`).join(', ') }}
          </td>
          <td class="num">${{ order.total.toFixed(2) }}</td>
          <td>
            <span class="status" :class="order.status.toLowerCase()">{{ order.status }}</span>
          </td>
          <td class="actions">
            <button
              v-for="action in NEXT_ACTIONS[order.status]"
              :key="action"
              :class="{ danger: action === 'cancel' }"
              @click="onTransition(order, action)"
            >
              {{ action }}
            </button>
          </td>
        </tr>
      </tbody>
    </table>
    <p v-if="orders.length === 0" class="muted">No orders yet.</p>
  </section>
</template>
