FROM alpine:3.21

RUN apk add --no-cache \
    imagemagick \
    imagemagick-jpeg \
    imagemagick-webp

WORKDIR /data

ENTRYPOINT ["magick"]
