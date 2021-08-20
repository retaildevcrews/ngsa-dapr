#!/bin/bash

echo "on-create start" >> ~/status

# prevent the dependency popup
dotnet restore src/app/Ngsa.Dapr.csproj

# create a network
docker network create dapr

# create local registry
k3d registry create registry.localhost --port 5000
docker network connect dapr k3d-registry.localhost

# create k3d cluster
k3d cluster create --registry-use k3d-registry.localhost:5000 --config deploy/k3d.yaml
kubectl wait node --for condition=ready --all --timeout=60s

# install dapr
dapr init -k --enable-mtls=false --wait

# install redis
helm repo add bitnami https://charts.bitnami.com/bitnami
helm repo update
helm install redis bitnami/redis

# redis config
kubectl apply -f deploy/redis.yaml

# wait for redis master to start
# kubectl wait pod redis-master-0  --for condition=ready --timeout=60s

# add the secrets
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosCollection=movies \
  --from-literal=CosmosDatabase=imdb \
  --from-literal=CosmosUrl=https://ngsa-pre-cosmos.documents.azure.com:443/ \
  --from-literal=CosmosKey=${COSMOS_KEY}

echo "on-create complete" >> ~/status
