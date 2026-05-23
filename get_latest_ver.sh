#!/bin/bash

curl -s https://oculusdb.rui2015.me/api/v1/versions/3262063300561328?onlydownloadable=true | jq -r '.[0].version'
