#!/bin/bash

# Note: slice_1387.921 is pitground of pitground/UnityTempFile-201cbd57f079d244fa831efae4c2e050
# leafs are 915/916

target_folder=$1

if [ ! -d "$target_folder" ]; then
    echo "$target_folder does not exist."
    exit 1
fi

    # string Name,
    # string Author,
    # string PackVersion,
    # string CompiledGameVersion
if [ ! -f "$target_folder/package.json" ]; then
    echo "Added props"
    read -p "Enter the Name: " name
    read -p "Enter the Author: " author
    read -p "Enter the PackVersion: " pack_version
    game_version=$(curl -s https://oculusdb.rui2015.me/api/v1/versions/3262063300561328?onlydownloadable=true | jq -r '.[0].version')
    echo $game_version

    jq -n "{ Name: \"$name\", Author: \"$author\", PackVersion: \"$pack_version\", CompiledGameVersion: \"$game_version\"}" > "$target_folder/package.json"
fi

package_json=$(cat "$target_folder/package.json")
name=$(echo "$package_json" | jq -r '.Name')
cd "$target_folder"

# todo: add support for press compress bc7 for v3 packs
for file in $(find ./* -type f -name "*.png")
do
    input=mktemp 
    magick "$file" -flip "$input"
    filename="$(basename "${file%.*}").dds"
    rm "$filename"
    compressonatorcli -EncodeWith HPC -nomipmap -fd BC7 "$input" "$filename" # no mipmaps since different arrays have different counts (plus future proofing)
    rm "$input"
    # nvcompress -bc7 "$file" "$filename"
done


output="../$name.pack"
rm "$output"
zip -9 "$output" -r . -x "*.png"
rm *.dds
cd ..
echo "Hash: $(sha256sum "$name.pack" | awk '{print $1}')"

echo "Copying to $GORILLATAG_PATH"
cp "$name.pack" "$GORILLATAG_PATH/BepInEx/plugins/GorillaTextureLoader/packs/"
echo "Final size is $(du -sh "$name.pack" | awk '{print $1}')"
