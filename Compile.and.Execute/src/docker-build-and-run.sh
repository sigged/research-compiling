# Build and runs docker image (not for CI/CD usage)
echo Building Dockerimage 

docker build -f Dockerfile -t sigged/insecure-csc .

#this app MUST be run on the same forward-facing port, or SignalRClientService won't work
docker run -it --rm -p 80:80 --name insecure-csc sigged/insecure-csc
