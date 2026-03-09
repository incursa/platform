#!/usr/bin/env bash

status_emoji() {
  case "$1" in
    success) echo "✅" ;;
    skipped) echo "⚠️" ;;
    *) echo "❌" ;;
  esac
}

bool_emoji() {
  if [ "$1" = "true" ]; then
    echo "✅"
  else
    echo "❌"
  fi
}
