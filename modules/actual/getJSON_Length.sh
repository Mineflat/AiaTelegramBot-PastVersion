#!/bin/bash
# $1 - Полный путь к JSON-файлу 
# $2 - Корневой элемент JSON-массива
jq -r ".$2 | length" $1
