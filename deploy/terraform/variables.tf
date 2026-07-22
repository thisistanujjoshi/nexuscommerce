variable "subscription_id" {
  type        = string
  description = "Azure subscription ID to deploy into."
  default     = "00000000-0000-0000-0000-000000000000"
}

variable "environment" {
  type        = string
  description = "Deployment environment (dev, staging, prod). Drives naming and sizing."
  default     = "dev"

  validation {
    condition     = contains(["dev", "staging", "prod"], var.environment)
    error_message = "environment must be one of: dev, staging, prod."
  }
}

variable "location" {
  type        = string
  description = "Azure region."
  default     = "westeurope"
}

variable "node_count" {
  type        = number
  description = "AKS default node pool size."
  default     = 2
}

variable "node_vm_size" {
  type        = string
  description = "AKS node VM size."
  default     = "Standard_B2s"
}

variable "postgres_admin_login" {
  type        = string
  description = "PostgreSQL administrator login."
  default     = "nexusadmin"
}

variable "postgres_admin_password" {
  type        = string
  description = "PostgreSQL administrator password. Supply via TF_VAR_postgres_admin_password, never commit."
  sensitive   = true
  default     = null
}

variable "tags" {
  type        = map(string)
  description = "Tags applied to every resource."
  default = {
    project = "nexuscommerce"
    owner   = "tanuj"
  }
}
