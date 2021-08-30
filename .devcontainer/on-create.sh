#!/bin/bash

echo "on-create start" >> ~/status

# prevent the dependency popup
dotnet restore src/app/Ngsa.Dapr.csproj

# redis dir
sudo mkdir -p /data/data
sudo chown -R 1001:1001 /data

# create a network
docker network create dapr

# create local registry
k3d registry create registry.localhost --port 5000
docker network connect dapr k3d-registry.localhost

# create k3d cluster
k3d cluster create --registry-use k3d-registry.localhost:5000 --config deploy/k3d.yaml

# build and push the image
docker build . -t k3d-registry.localhost:5000/ngsa-dapr:local
docker push k3d-registry.localhost:5000/ngsa-dapr:local

kubectl wait node --for condition=ready --all --timeout=60s

# install dapr
dapr init -k --enable-mtls=false --wait

# deploy redis
kubectl apply -f deploy/redis.yaml

# add the secrets
kubectl create secret generic ngsa-secrets \
  --from-literal=CosmosCollection=movies \
  --from-literal=CosmosDatabase=imdb \
  --from-literal=CosmosUrl=https://ngsa-pre-cosmos.documents.azure.com:443/ \
  --from-literal=CosmosKey=${COSMOS_KEY}

# wait for redis to start
kubectl wait pod -l app=redis --for condition=ready --timeout=60s

echo "on-create complete" >> ~/status
