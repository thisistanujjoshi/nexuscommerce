terraform {
  required_version = ">= 1.6.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Real deployments store state in an Azure Storage backend so it is shared
  # and locked across the team and CI. Left commented so `terraform init`
  # works locally without a storage account.
  # backend "azurerm" {
  #   resource_group_name  = "nexuscommerce-tfstate"
  #   storage_account_name = "nexuscommercetfstate"
  #   container_name       = "tfstate"
  #   key                  = "nexuscommerce.tfstate"
  # }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}
