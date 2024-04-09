Write-Host ">> Generating certificate..."

# Create directory if it doesn't exist
New-Item -ItemType Directory -Path .\certs -Force

# Set permissions for the directory
icacls .\certs /grant:r "$($env:UserName):(OI)(CI)RX"

# Pull the Docker image
docker pull ghcr.io/eventstore/es-gencert-cli:1.3.0

# Create CA certificate
docker run --rm --volume ${PWD}\certs:/tmp --user (Get-Process -Id $PID).SessionId ghcr.io/eventstore/es-gencert-cli:1.3.0 create-ca -out /tmp/ca

# Create node certificate
docker run --rm --volume ${PWD}\certs:/tmp --user (Get-Process -Id $PID).SessionId ghcr.io/eventstore/es-gencert-cli:1.3.0 create-node -ca-certificate /tmp/ca/ca.crt -ca-key /tmp/ca/ca.key -out /tmp/node -ip-addresses 127.0.0.1 -dns-names localhost

# Set permissions recursively for the directory
icacls .\certs /grant:r "$($env:UserName):(OI)(CI)RX"

Import-Certificate -FilePath ".\certs\ca\ca.crt" -CertStoreLocation Cert:\CurrentUser\Root
