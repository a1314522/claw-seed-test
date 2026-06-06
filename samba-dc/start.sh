#!/bin/bash
set -e

REALM=${REALM:-TEST.LOCAL}
DOMAIN=${DOMAIN:-TEST}
ADMINPASS=${ADMINPASS:-TestPass123!}

# If not provisioned yet, provision now
if [ ! -f /etc/samba/smb.conf ]; then
    echo "Provisioning Samba AD DC..."
    samba-tool domain provision \
        --server-role=dc \
        --use-rfc2307 \
        --dns-backend=SAMBA_INTERNAL \
        --realm="$REALM" \
        --domain="$DOMAIN" \
        --adminpass="$ADMINPASS" \
        --host-ip=0.0.0.0
    cp /var/lib/samba/private/krb5.conf /etc/krb5.conf
fi

# Ensure proper permissions
chmod 750 /var/lib/samba/private/
chmod 640 /var/lib/samba/private/*.ldb 2>/dev/null || true

# Create test users if they don't exist
for user in zhangsan lisi wangwu; do
    if ! samba-tool user show "$user" >/dev/null 2>&1; then
        echo "Creating test user: $user"
        samba-tool user create "$user" "Pass1234!" || true
    fi
done

# Start Samba AD DC
exec /usr/sbin/samba -i
