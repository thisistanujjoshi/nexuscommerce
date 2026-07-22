# NexusCommerce infrastructure (Terraform)

Provisions the Azure infrastructure the platform runs on: an AKS cluster,
virtual network, Azure Container Registry, Key Vault, and a managed
PostgreSQL flexible server.

## Usage

```bash
export TF_VAR_subscription_id="<your-subscription-id>"
export TF_VAR_postgres_admin_password="<a-strong-password>"

terraform init
terraform plan  -var environment=dev
terraform apply -var environment=dev
```

Then wire the cluster and registry into the Helm deploy:

```bash
az aks get-credentials --resource-group "$(terraform output -raw resource_group)" \
  --name "$(terraform output -raw aks_cluster_name)"

helm upgrade --install nexus ../helm/nexuscommerce \
  -f ../helm/nexuscommerce/values-dev.yaml \
  --set image.registry="$(terraform output -raw acr_login_server)"
```

## Notes

- **State backend** is commented out in `versions.tf` so `terraform init`
  works locally. Uncomment and point it at an Azure Storage account for
  shared, locked state in CI.
- **Secrets** never live in `.tfvars`. The DB password comes from
  `TF_VAR_postgres_admin_password`; application secrets land in Key Vault and
  are surfaced to pods via the Secrets Store CSI driver.
- `environment` (`dev`/`staging`/`prod`) drives naming and sizing — prod uses
  Premium ACR, a larger Postgres SKU, and Key Vault purge protection.
