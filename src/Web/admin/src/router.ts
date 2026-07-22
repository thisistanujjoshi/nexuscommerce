import { createRouter, createWebHistory } from 'vue-router'
import CategoriesView from './views/CategoriesView.vue'
import OrdersView from './views/OrdersView.vue'
import ProductsView from './views/ProductsView.vue'

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/', redirect: '/products' },
    { path: '/products', component: ProductsView, name: 'products' },
    { path: '/categories', component: CategoriesView, name: 'categories' },
    { path: '/orders', component: OrdersView, name: 'orders' },
  ],
})
