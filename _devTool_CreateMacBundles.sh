#!/bin/bash

IFS=$'\n'
SOLUTION_DIR="$(pwd)"
# exit if out doesn't exist
cd out || exit
for dir in *.d/
do
  tbl=${dir%/}
   pushd "$tbl" >/dev/null || exit
    APP_NAME="$(echo $tbl | rev | cut -c3- | rev)"  # Change this to your app name
    echo "Generating ${APP_NAME}.app..."
    # Configuration
    echo "    Creating directories"
    IDENTIFIER="ee.mas.${APP_NAME}"  # Change this to your bundle identifier
    APP_DIR="${APP_NAME}.app"
    mkdir "$APP_DIR"
    mkdir "${APP_DIR}/Contents"
    mkdir "${APP_DIR}/Contents/Resources"
    mkdir "${APP_DIR}/Contents/MacOS"
    echo "    Copying executables"
    mv "$APP_NAME" "${APP_DIR}/Contents/MacOS"
    mv *.dylib "${APP_DIR}/Contents/MacOS" 2>/dev/null
    mv libvlc "${APP_DIR}/Contents/MacOS" 2>/dev/null
    echo "    Generating Info.plist"
    echo '<?xml version="1.0" encoding="UTF-8"?>' > "${APP_DIR}/Contents/Info.plist"
    echo '<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">' >> "${APP_DIR}/Contents/Info.plist"
    echo '<plist version="1.0">' >> "${APP_DIR}/Contents/Info.plist"
    echo '<dict>' >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundleName</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>$APP_NAME</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundleDisplayName</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>$APP_NAME</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundleIdentifier</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>$IDENTIFIER</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundleVersion</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>1.0.0</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundleExecutable</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>$APP_NAME</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundlePackageType</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>APPL</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>CFBundleIconFile</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>AppIcon</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '      <key>LSMinimumSystemVersion</key>' >> "${APP_DIR}/Contents/Info.plist"
    echo "      <string>10.13</string>" >> "${APP_DIR}/Contents/Info.plist"
    echo '  </dict>' >> "${APP_DIR}/Contents/Info.plist"
    echo '  </plist>' >> "${APP_DIR}/Contents/Info.plist"
    cp "${SOLUTION_DIR}/${APP_NAME}.icns" "${APP_DIR}/Contents/Resources/AppIcon.icns"
    mv "$APP_DIR" "$SOLUTION_DIR/out/$APP_DIR"
   popd >/dev/null || exit
done
echo "Deleting temporary files..."
rm -rf *.d
cd ..
echo "Copying bundles..."
cp -r out/* "$HOME/.mas/Markuse asjad"
