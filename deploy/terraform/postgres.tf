resource "azurerm_postgresql_flexible_server" "main" {
  name                          = "${local.prefix}-pg"
  resource_group_name           = azurerm_resource_group.main.name
  location                      = azurerm_resource_group.main.location
  version                       = "16"
  administrator_login           = var.postgres_admin_login
  administrator_password        = var.postgres_admin_password
  storage_mb                    = 32768
  sku_name                      = var.environment == "prod" ? "GP_Standard_D2s_v3" : "B_Standard_B1ms"
  public_network_access_enabled = false
  zone                          = "1"
  tags                          = local.tags
}

resource "azurerm_postgresql_flexible_server_database" "orders" {
  name      = "nexus_orders"
  server_id = azurerm_postgresql_flexible_server.main.id
  charset   = "UTF8"
  collation = "en_US.utf8"
}
