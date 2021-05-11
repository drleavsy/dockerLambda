FROM public.ecr.aws/lambda/dotnet:latest as base

RUN yum update -y && yum install -y amazon-linux-extras && \
	amazon-linux-extras install epel -y && \
    yum install chromium -y && \
    yum install unzip -y

ARG CHROME_DIR
WORKDIR ${CHROME_DIR}
#ADD https://storage.googleapis.com/chromium-browser-snapshots/Linux_x64/843427/chrome-linux.zip ${CHROME_DIR}
RUN curl -SL --output ${CHROME_DIR}/chrome-linux.zip https://storage.googleapis.com/chromium-browser-snapshots/Linux_x64/843427/chrome-linux.zip && \
    unzip ${CHROME_DIR}/chrome-linux.zip -d /tmp
COPY . .

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim as build
#FROM mcr.microsoft.com/dotnet/sdk:5.0 as build
WORKDIR /src
COPY ["DockerLambda.csproj", "DockerLambda/"]
RUN dotnet restore "DockerLambda/DockerLambda.csproj"

WORKDIR "/src/DockerLambda"
COPY . .
RUN dotnet build "DockerLambda.csproj" --configuration Release --output /app/build

FROM build AS publish
RUN dotnet publish "DockerLambda.csproj" \
            --configuration Release \ 
            --runtime linux-x64 \
            --self-contained false \ 
            --output /app/publish \
            -p:PublishReadyToRun=true  

FROM base AS final
WORKDIR /var/task
COPY --from=publish /app/publish .
CMD ["DockerLambda::DockerLambda.Functions::Get"]

