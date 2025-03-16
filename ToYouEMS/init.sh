#!/bin/sh

# Define the destination folder inside the mounted volume
DESTINATION="/app/Storage"

# Ensure the destination folder exists
mkdir -p "$DESTINATION"

# Check if files already exist (to avoid overwriting)
if [ -z "$(ls -A $DESTINATION)" ]; then
  echo "Copying default files to $DESTINATION..."
  cp -r /app/StorageCopy/* "$DESTINATION/"
else
  echo "Files already exist in $DESTINATION. Skipping copy."
fi

# Continue with the main application
exec "$@"

