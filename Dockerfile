ARG FUNCTION_DIR="/src"
ARG CHROME_DIR="/tmp"

FROM public.ecr.aws/lambda/dotnet:latest as base

RUN yum update -y && yum install -y amazon-linux-extras && \
	amazon-linux-extras install epel -y && \
    yum install chromium -y 

ARG CHROME_DIR
WORKDIR ${CHROME_DIR}
ADD https://storage.googleapis.com/chromium-browser-snapshots/Linux_x64/843427/chrome-linux.zip ${CHROME_DIR}
COPY . .

ARG FUNCTION_DIR
FROM mcr.microsoft.com/dotnet/sdk:5.0 as sdk-base-image
WORKDIR ${FUNCTION_DIR}
COPY "DockerLambda.csproj" ${FUNCTION_DIR}
RUN dotnet restore ${FUNCTION_DIR}/DockerLambda.csproj

WORKDIR ${FUNCTION_DIR}
COPY . .
RUN dotnet build "DockerLambda.csproj" --configuration Release --output ${FUNCTION_DIR}/build

FROM sdk-base-image AS publish
RUN dotnet publish "DockerLambda.csproj" \
            --configuration Release \ 
            --runtime linux-x64 \
            --self-contained false \ 
            --output ${FUNCTION_DIR}/publish \
            -p:PublishReadyToRun=true  

FROM base as final
WORKDIR /var/task
COPY --from=publish ${FUNCTION_DIR}/publish .



# This COPY command copies the .NET Lambda project's build artifacts from the host machine into the image. 
# The source of the COPY should match where the .NET Lambda project publishes its build artifacts. If the Lambda function is being built 
# with the AWS .NET Lambda Tooling, the `--docker-host-build-output-dir` switch controls where the .NET Lambda project
# will be built. The .NET Lambda project templates default to having `--docker-host-build-output-dir`
# set in the aws-lambda-tools-defaults.json file to "bin/Release/net5.0/linux-x64/publish".
#
# Alternatively Docker multi-stage build could be used to build the .NET Lambda project inside the image.
# For more information on this approach checkout the project's README.md file.
#COPY "bin/Release/net5.0/linux-x64/publish"  ${FUNCTION_DIR}
