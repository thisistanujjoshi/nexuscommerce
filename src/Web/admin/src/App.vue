<template>
  <div class="layout">
    <header class="site-header">
      <span class="brand">Nexus<span class="accent">Commerce</span> Admin</span>
      <nav>
        <RouterLink to="/products">Inventory</RouterLink>
        <RouterLink to="/categories">Categories</RouterLink>
        <RouterLink to="/orders">Orders</RouterLink>
      </nav>
    </header>

    <main>
      <RouterView />
    </main>

    <footer class="site-footer">
      NexusCommerce admin dashboard — Vue 3 + TypeScript, talking to the Catalog & Orders
      microservices.
    </footer>
  </div>
</template>
