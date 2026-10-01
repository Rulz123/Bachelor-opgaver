#!/usr/bin/env sh
set -eu

for instance in 1 2 3; do
  curl --fail --silent --no-keepalive "http://localhost:8081/health" | grep -q "article-service-$instance"
done

echo "NGINX reached all three ArticleService instances"
