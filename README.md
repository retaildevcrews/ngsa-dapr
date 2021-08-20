# NGSA App with dapr

## Using Visual Studio Codespaces

- Open this repo with GitHub Codespaces
- From the Codespace zsh window
- Wait for the cluster and Redis to start

    ```bash

    # bug - todo - Codespaces usually runs onCreate in a blocking mode
    # it's currently not blocking - need to debug and see if that behavior has changed

    kubectl get pods

    ```

- Results

  ```text

    NAME               READY   STATUS    RESTARTS   AGE
  redis-replicas-0   1/1     Running   0          100s
  redis-master-0     1/1     Running   0          100s
  redis-replicas-1   1/1     Running   0          56s
  redis-replicas-2   1/1     Running   0          28s

  ```

### Build, deploy and validate the app

```bash

# build the app
make build

# deploy the app
make deploy

# check the logs for the secrets
# you may have to retry as the app starts
make logs

# check the endpoint
make check

# run a web validate test
make test

```

### Engineering Docs

- Team Working [Agreement](.github/WorkingAgreement.md)
- Team [Engineering Practices](.github/EngineeringPractices.md)
- CSE Engineering Fundamentals [Playbook](https://github.com/Microsoft/code-with-engineering-playbook)

## How to file issues and get help  

This project uses GitHub Issues to track bugs and feature requests. Please search the existing issues before filing new issues to avoid duplicates. For new issues, file your bug or feature request as a new issue.

For help and questions about using this project, please open a GitHub issue.

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us the rights to use your contribution. For details, visit <https://cla.opensource.microsoft.com>

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services.

Authorized use of Microsoft trademarks or logos is subject to and must follow [Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).

Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.

Any use of third-party trademarks or logos are subject to those third-party's policies.
