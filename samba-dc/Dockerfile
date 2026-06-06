FROM ubuntu:22.04

ENV DEBIAN_FRONTEND=noninteractive
RUN apt-get update && apt-get install -y \
    samba \
    samba-dsdb-modules \
    samba-vfs-modules \
    winbind \
    libpam-winbind \
    krb5-user \
    ldb-tools \
    iproute2 \
    net-tools \
    dnsutils \
    nano \
    && rm -rf /var/lib/apt/lists/*

# Expose AD ports
EXPOSE 53/tcp 53/udp 88/tcp 88/udp 135/tcp 137/udp 138/udp 139/tcp 389/tcp 389/udp 445/tcp 464/tcp 464/udp 636/tcp 3268/tcp 3269/tcp 123/udp

# Start script (provisions at runtime)
COPY start.sh /start.sh
RUN chmod +x /start.sh

CMD ["/start.sh"]
