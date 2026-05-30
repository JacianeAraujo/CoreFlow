#!/usr/bin/env bash
set -euo pipefail

# Build the Lambda deployment package consumed by Terraform.
#
# Output: ./artifacts/order-file-ingestion-lambda.zip
#
# Requires: .NET 8 SDK and the `Amazon.Lambda.Tools` global tool.
#   dotnet tool install -g Amazon.Lambda.Tools

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="${SCRIPT_DIR}/src/OrderFileIngestion"
ARTIFACTS_DIR="${SCRIPT_DIR}/artifacts"
PACKAGE_PATH="${ARTIFACTS_DIR}/order-file-ingestion-lambda.zip"

mkdir -p "${ARTIFACTS_DIR}"

dotnet lambda package \
  --project-location "${PROJECT_DIR}" \
  --configuration Release \
  --framework net8.0 \
  --output-package "${PACKAGE_PATH}"

echo "Built ${PACKAGE_PATH}"
