FROM public.ecr.aws/lambda/dotnet:latest as base

RUN yum update -y && yum install -y amazon-linux-extras && \
	amazon-linux-extras install epel -y && \
    yum install chromium -y 

FROM mcr.microsoft.com/dotnet/sdk:5.0 as build

RUN mkdir -p "/function"
WORKDIR "/function" # ${FUNCTION_DIR}
COPY "DockerLambda.csproj" "/function"

RUN dotnet restore "/function/DockerLambda.csproj"
WORKDIR "/function" # ${FUNCTION_DIR}
COPY . .

RUN dotnet build "DockerLambda.csproj" --configuration Release --output /app/build

FROM build AS publish

RUN dotnet publish "DockerLambda.csproj" \
            --configuration Release \ 
            --runtime linux-x64 \
            --self-contained false \ 
            --output /app/publish \
            -p:PublishReadyToRun=true  

FROM base as final
WORKDIR /var/task

COPY --from=publish /app/publish .

WORKDIR /tmp
ADD https://storage.googleapis.com/chromium-browser-snapshots/Linux_x64/843427/chrome-linux.zip /tmp

