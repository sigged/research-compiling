@ECHO OFF
REM Build and pushes to dockerhub (not for CI/CD usage)
ECHO Building Dockerimage
docker login
docker build -f Dockerfile -t sigged/insecure-csc .
docker push sigged/insecure-csc
PAUSE