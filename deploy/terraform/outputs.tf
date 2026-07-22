output "resource_group" {
  value       = azurerm_resource_group.main.name
  description = "Resource group holding all NexusCommerce infrastructure."
}

output "aks_cluster_name" {
  value       = azurerm_kubernetes_cluster.main.name
  description = "AKS cluster name — feed to 'az aks get-credentials'."
}

output "acr_login_server" {
  value       = azurerm_container_registry.main.login_server
  description = "Container registry login server — the Helm image.registry value."
}

output "key_vault_name" {
  value       = azurerm_key_vault.main.name
  description = "Key Vault holding application secrets."
}

output "postgres_fqdn" {
  value       = azurerm_postgresql_flexible_server.main.fqdn
  description = "PostgreSQL flexible server FQDN."
}
