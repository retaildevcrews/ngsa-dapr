#!/bin/bash

echo "post-start start" >> ~/status

# this runs in background each time the container starts

unset COSMOS_KEY

echo "post-start complete" >> ~/status
