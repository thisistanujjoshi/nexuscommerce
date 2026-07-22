resource "azurerm_key_vault" "main" {
  name                       = replace("${local.prefix}-kv", "_", "-")
  location                   = azurerm_resource_group.main.location
  resource_group_name        = azurerm_resource_group.main.name
  tenant_id                  = data.azurerm_client_config.current.tenant_id
  sku_name                   = "standard"
  purge_protection_enabled   = var.environment == "prod"
  soft_delete_retention_days = 7
  tags                       = local.tags

  # Let the AKS cluster read secrets via its managed identity (consumed by the
  # Secrets Store CSI driver, which surfaces them to pods).
  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = azurerm_kubernetes_cluster.main.kubelet_identity[0].object_id

    secret_permissions = ["Get", "List"]
  }
}

resource "azurerm_key_vault_secret" "orders_db" {
  name         = "orders-db-connection"
  value        = "Host=${azurerm_postgresql_flexible_server.main.fqdn};Database=nexus_orders;Username=${var.postgres_admin_login};Password=${var.postgres_admin_password}"
  key_vault_id = azurerm_key_vault.main.id
}
