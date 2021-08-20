.PHONY: help build check logs jumpbox

help :
	@echo "Usage:"
	@echo "   make build         - build the app"
	@echo "   make check         - check the app endpoints"
	@echo "   make logs          - check the app logs"
	@echo "   make jumpbox       - deploy a 'jumpbox' pod"

build :
	# building and pushing docker image
	@docker system prune -f
	@docker build . -t k3d-registry.localhost:5000/ngsa-dapr:local
	@docker push k3d-registry.localhost:5000/ngsa-dapr:local

check :
	# curl all of the endpoints
	http localhost:30080/version

jumpbox :
	@# start a jumpbox pod
	@-kubectl delete pod jumpbox --ignore-not-found=true

	@kubectl run jumpbox --image=alpine --restart=Always -- /bin/sh -c "trap : TERM INT; sleep 9999999999d & wait"
	@kubectl wait pod jumpbox --for condition=ready --timeout=30s
	@kubectl exec jumpbox -- /bin/sh -c "apk update && apk add bash curl py-pip" > /dev/null
	@kubectl exec jumpbox -- /bin/sh -c "pip3 install --upgrade pip setuptools httpie" > /dev/null
	@kubectl exec jumpbox -- /bin/sh -c "echo \"alias ls='ls --color=auto'\" >> /root/.profile && echo \"alias ll='ls -lF'\" >> /root/.profile && echo \"alias la='ls -alF'\" >> /root/.profile && echo 'cd /root' >> /root/.profile" > /dev/null

	#
	# use kje <command>
	# kje http ngsa-memory:8080/version
	# kje bash -l

logs :
	kubectl logs --selector=app=ngsa -c app

secrets :
	@kubectl delete secret ngsa-secrets --ignore-not-found

	@kubectl create secret generic ngsa-secrets \
	  --from-literal=CosmosCollection=movies \
	  --from-literal=CosmosDatabase=imdb \
	  --from-literal=CosmosUrl=https://ngsa-pre-cosmos.documents.azure.com:443/ \
	  --from-literal=CosmosKey=${COSMOS_KEY}
