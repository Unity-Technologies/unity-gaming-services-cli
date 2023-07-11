FROM mcr.microsoft.com/dotnet/runtime-deps:7.0.4-alpine3.16-amd64

ARG UGS_VERSION=latest

RUN apk add --no-cache curl ncurses

# Set the URL based on the version
RUN if [ "$UGS_VERSION" = "latest" ]; then \
      UGS_URL="https://github.com/Unity-Technologies/unity-gaming-services-cli/releases/latest/download/ugs-linux-musl-x64"; \
    else \
      UGS_URL="https://github.com/Unity-Technologies/unity-gaming-services-cli/releases/download/$UGS_VERSION/ugs-linux-musl-x64"; \
    fi \
    && echo "Installing UGS cli version \"$UGS_VERSION\" from \"$UGS_URL\""  \
    && curl -f -L "$UGS_URL" -o /bin/ugs

RUN chmod +x /bin/ugs

# Add some color to it
ENV TERM=xterm-256color
