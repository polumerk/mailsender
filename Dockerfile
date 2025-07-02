FROM BUILDER_BASE_IMAGE_BACKEND AS builder
WORKDIR /app
COPY . .

RUN dotnet nuget disable source nuget.org && \
	dotnet nuget add source --name nuget_external BACKEND_NUGET_SERVER/v3/index.json

RUN cd MailSender.API/ && dotnet restore && dotnet publish -c Release -o /app/out/ --self-contained --use-current-runtime --framework net8.0

FROM RUNNER_BASE_IMAGE_BACKEND
WORKDIR /app
COPY --from=builder /app/out/ .

ENV LANG en_US.UTF-8

EXPOSE 5000/tcp
ENV ASPNETCORE_URLS=http://*:5000